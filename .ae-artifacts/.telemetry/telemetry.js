#!/usr/bin/env node
"use strict";

const path = require("path");
const fs = require("fs");
const crypto = require("crypto");

const KNOWN_EVENTS = new Set([
  "feature_created",
  "phase_started",
  "hitl_decision",
  "phase_completed",
  "phase_abandoned",
  "feature_completed",
]);

function silentExit() {
  process.exit(0);
}

function parseArgs(argv) {
  const positional = [];
  const flags = {};
  let i = 0;
  while (i < argv.length) {
    const arg = argv[i];
    if (arg.startsWith("--")) {
      const key = arg.slice(2);
      const value = argv[i + 1];
      if (value !== undefined && !value.startsWith("--")) {
        flags[key] = value;
        i += 2;
      } else {
        flags[key] = true;
        i += 1;
      }
    } else {
      positional.push(arg);
      i += 1;
    }
  }
  return { positional, flags };
}

function resolveSessionId() {
  const env = process.env.CLAUDE_CODE_SESSION_ID;
  if (env && env.trim()) return env.trim();
  return crypto.randomUUID();
}

function resolveClaudeProjectFolder(cwd) {
  return cwd.replace(/[:\\/]/g, "-");
}

function readSessionMetrics(sessionId, cwd) {
  try {
    const homeDir = require("os").homedir();
    const projectFolder = resolveClaudeProjectFolder(cwd);
    const jsonlPath = path.join(
      homeDir,
      ".claude",
      "projects",
      projectFolder,
      sessionId + ".jsonl",
    );

    if (!fs.existsSync(jsonlPath)) return null;

    const lines = fs.readFileSync(jsonlPath, "utf8").split("\n");
    let input_tokens = 0;
    let output_tokens = 0;
    let cache_read_input_tokens = 0;
    let cache_creation_input_tokens = 0;
    let model = "unknown model";
    let message_count = 0;
    const seen = new Set();

    /**
     * Root Filtering algorithm. Register all possible roots and mark them non-roots when another entry references
     * them as a parent via parentUuid.
     */
    const candidates = new Map();

    for (const line of lines) {
      if (!line.trim()) continue;

      let record;
      try {
        record = JSON.parse(line);
      } catch (_) {
        continue;
      }

      if (
        record.type === "system" &&
        record.subtype === "turn_duration" &&
        typeof record.messageCount === "number"
      ) {
        if (record.messageCount > message_count)
          message_count = record.messageCount;
        continue;
      }

      if (record.type !== "assistant" || !record.message?.usage) continue;
      if (record.uuid && seen.has(record.uuid)) continue;
      if (record.uuid) seen.add(record.uuid);

      // If this entry's parent is already a candidate, demote the parent.
      if (record.parentUuid && candidates.has(record.parentUuid)) {
        candidates.get(record.parentUuid).isRoot = false;
      }

      candidates.set(record.uuid, {
        u: record.message.usage,
        model: record.message.model,
        isRoot: true,
      });
    }

    for (const { u, model: m, isRoot } of candidates.values()) {
      if (!isRoot) continue;
      if (m) model = m;
      input_tokens += u.input_tokens || 0;
      output_tokens += u.output_tokens || 0;
      cache_read_input_tokens += u.cache_read_input_tokens || 0;
      cache_creation_input_tokens += u.cache_creation_input_tokens || 0;
    }

    const total_token_usage =
      input_tokens +
      output_tokens +
      cache_read_input_tokens +
      cache_creation_input_tokens;

    const no_cache_total_token_usage = input_tokens + output_tokens;

    return {
      input_tokens,
      output_tokens,
      message_count,
      cache_read_input_tokens,
      cache_creation_input_tokens,
      no_cache_total_token_usage,
      total_token_usage,
      model,
    };
  } catch (_) {
    return null;
  }
}

function resolveProjectRoot(startDir) {
  let dir = startDir;
  while (true) {
    if (fs.existsSync(path.join(dir, ".ae-artifacts"))) return dir;
    if (fs.existsSync(path.join(dir, ".git"))) return dir;
    const parent = path.dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }
  return null;
}

function resolveFeatureSlug(projectRoot) {
  if (!projectRoot) return "unknown";
  try {
    const activePath = path.join(
      projectRoot,
      ".ae-artifacts",
      "ACTIVE_FEATURE",
    );
    const content = fs.readFileSync(activePath, "utf8").trim();
    if (content) return content;
  } catch (_) {
    // ignore
  }
  return "unknown";
}

function toMillisIso(date) {
  return date.toISOString();
}

async function postOtlp(endpoint, envelope) {
  const url = endpoint.replace(/\/$/, "") + "/v1/logs";
  const timeUnixNano = String(BigInt(envelope.ts_ms) * 1000000n);

  const body = JSON.stringify({
    resourceLogs: [
      {
        resource: {
          attributes: [
            { key: "service.name", value: { stringValue: "gap-ai-dev-kit" } },
          ],
        },
        scopeLogs: [
          {
            scope: { name: "gap-ai-dev-kit.telemetry" },
            logRecords: [
              {
                timeUnixNano,
                severityNumber: 9,
                severityText: "INFO",
                body: { stringValue: JSON.stringify(envelope) },
                attributes: [
                  { key: "event.name", value: { stringValue: envelope.event } },
                  {
                    key: "feature.slug",
                    value: { stringValue: envelope.feature_slug },
                  },
                  {
                    key: "session.id",
                    value: { stringValue: envelope.session_id },
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  });

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 2000);
  try {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body,
      signal: controller.signal,
    });
    const text = await res.text();
  } finally {
    clearTimeout(timer);
  }
}

async function main() {
  try {
    const { positional, flags } = parseArgs(process.argv.slice(2));

    const event_name = positional[0];
    if (!event_name || !KNOWN_EVENTS.has(event_name)) {
      silentExit();
    }

    const now = new Date();
    const ts = toMillisIso(now);
    const ts_ms = now.getTime();

    const session_id = resolveSessionId();

    const projectRoot = resolveProjectRoot(process.cwd());
    const feature_slug = resolveFeatureSlug(projectRoot);

    const envelope = {
      schema_version: "1.0",
      event: event_name,
      ts,
      session_id,
      feature_slug,
    };

    if (flags.phase !== undefined && flags.phase !== true) {
      envelope.phase = flags.phase;
    }
    if (flags.decision !== undefined && flags.decision !== true) {
      envelope.decision = flags.decision;
    }
    if (flags.iteration !== undefined && flags.iteration !== true) {
      envelope.iteration = Number(flags.iteration);
    }
    if (flags.verdict !== undefined && flags.verdict !== true) {
      envelope.verdict = flags.verdict;
    }
    if (
      flags.manual_development_duration_estimate !== undefined &&
      flags.manual_development_duration_estimate !== true
    ) {
      envelope.manual_development_duration_estimate = Number(
        flags.manual_development_duration_estimate,
      );
    }

    const sessionMetrics = readSessionMetrics(
      envelope.session_id,
      projectRoot || process.cwd(),
    );

    if (sessionMetrics) {
      envelope.session_metrics = sessionMetrics;
    }

    // Append to events.jsonl
    const telemetryDir = projectRoot
      ? path.join(projectRoot, ".ae-artifacts", ".telemetry")
      : path.join(process.cwd(), ".ae-artifacts", ".telemetry");

    try {
      fs.mkdirSync(telemetryDir, { recursive: true });
      fs.appendFileSync(
        path.join(telemetryDir, "events.jsonl"),
        JSON.stringify(envelope) + "\n",
        "utf8",
      );
    } catch (_) {
      silentExit();
    }

    // Optional OTLP export
    const otlpEndpoint = process.env.GAP_EXPORTER_OTLP_ENDPOINT;
    if (otlpEndpoint) {
      try {
        await postOtlp(otlpEndpoint, { ...envelope, ts_ms });
      } catch (_) {
        // Fails silently on any error
      }
    }
  } catch (_) {
    // In case of any error, fail silently
  }

  process.exit(0);
}

main();
