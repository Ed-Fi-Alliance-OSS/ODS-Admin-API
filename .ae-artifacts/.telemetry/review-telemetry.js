#!/usr/bin/env node
"use strict";

const path = require("path");
const fs = require("fs");

function resolveProjectRoot(startDir) {
  let dir = startDir;
  while (true) {
    if (fs.existsSync(path.join(dir, ".ae-artifacts"))) return dir;
    const parent = path.dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }
  return null;
}

function generateTelemetryReport() {
  try {
    const projectRoot = resolveProjectRoot(process.cwd());
    if (!projectRoot) {
      console.error(
        "Could not resolve project root. Please run from a project directory.",
      );
      process.exit(1);
    }

    const telemetryPath = path.join(
      projectRoot,
      ".ae-artifacts",
      ".telemetry",
      "events.jsonl",
    );

    if (!fs.existsSync(telemetryPath)) {
      console.error(`Telemetry file not found: ${telemetryPath}`);
      process.exit(1);
    }

    const lines = fs.readFileSync(telemetryPath, "utf8").split("\n");
    const events = [];

    for (const line of lines) {
      if (!line.trim()) continue;
      let record;
      try {
        record = JSON.parse(line);
        events.push(record);
      } catch (_) {
        continue;
      }
    }

    if (events.length === 0) {
      console.error("No telemetry events found in the file.");
      process.exit(1);
    }

    const targetSlug = process.argv[2] || null;
    const metrics = generateReport(events, targetSlug);
    const markdown = formatMarkdownReport(metrics);

    const reportsDir = path.join(
      projectRoot,
      ".ae-artifacts",
      ".telemetry",
      "reports",
    );
    fs.mkdirSync(reportsDir, { recursive: true });

    const now = new Date();
    const dateString = now.toISOString().replace(/[:.]/g, "-");
    const reportPath = path.join(
      reportsDir,
      `telemetry-report-${dateString}.md`,
    );
    fs.writeFileSync(reportPath, markdown, "utf8");

    console.log(markdown);
    console.log(`\nReport saved to: ${reportPath}`);
  } catch (err) {
    console.error("Error reading telemetry events:", err);
    process.exit(1);
  }
}

function deltaMetrics(end, start) {
  return {
    input_tokens: (end.input_tokens || 0) - (start.input_tokens || 0),
    output_tokens: (end.output_tokens || 0) - (start.output_tokens || 0),
    cache_read_input_tokens:
      (end.cache_read_input_tokens || 0) - (start.cache_read_input_tokens || 0),
    total_token_usage:
      (end.total_token_usage || 0) - (start.total_token_usage || 0),
  };
}

function calculatePhaseTokens(events) {
  // session_metrics are cumulative — use phase_completed minus phase_started delta.
  const startByFeaturePhase = {};
  const endByFeaturePhase = {};

  for (const event of events) {
    if (!event.phase || !event.session_metrics || !event.feature_slug) continue;
    const key = `${event.feature_slug}::${event.phase}`;
    if (event.event === "phase_started") {
      startByFeaturePhase[key] = event.session_metrics;
    } else if (event.event === "phase_completed") {
      endByFeaturePhase[key] = event.session_metrics;
    }
  }

  const phaseTokens = {};

  for (const key of Object.keys(endByFeaturePhase)) {
    const phase = key.split("::")[1];
    const end = endByFeaturePhase[key];
    const start = startByFeaturePhase[key] || {
      input_tokens: 0,
      output_tokens: 0,
      cache_read_input_tokens: 0,
      total_token_usage: 0,
    };
    const delta = deltaMetrics(end, start);

    if (!phaseTokens[phase]) {
      phaseTokens[phase] = {
        input_tokens: 0,
        output_tokens: 0,
        cache_read_input_tokens: 0,
        total_token_usage: 0,
        event_count: 0,
      };
    }
    phaseTokens[phase].input_tokens += delta.input_tokens;
    phaseTokens[phase].output_tokens += delta.output_tokens;
    phaseTokens[phase].cache_read_input_tokens += delta.cache_read_input_tokens;
    phaseTokens[phase].total_token_usage += delta.total_token_usage;
    phaseTokens[phase].event_count += 1;
  }

  return phaseTokens;
}

function generateReport(events, targetSlug = null) {
  const overallStats = {
    total_input_tokens: 0,
    total_output_tokens: 0,
    total_cache_read_input_tokens: 0,
    total_token_usage: 0,
    decision_distribution: {},
    sessions: new Set(),
    features: new Set(),
    models: new Set(),
  };

  const featureStats = {};
  const lastEventByFeature = {};
  let lastFeature = null;

  for (const event of events) {
    if (event.session_id) overallStats.sessions.add(event.session_id);
    if (event.session_metrics?.model)
      overallStats.models.add(event.session_metrics.model);
    if (event.feature_slug) {
      overallStats.features.add(event.feature_slug);
      lastFeature = event.feature_slug;
      lastEventByFeature[event.feature_slug] = event;

      if (!featureStats[event.feature_slug]) {
        featureStats[event.feature_slug] = {
          created_at: null,
          completed_at: null,
          last_event_at: null,
          phases: {},
          decisions: {},
          model: null,
          message_count: 0,
          last_session_metrics: null,
          manual_development_duration_estimate: null,
        };
      }

      if (
        event.event === "feature_created" &&
        !featureStats[event.feature_slug].created_at
      ) {
        featureStats[event.feature_slug].created_at = event.ts;
      }

      if (event.event === "feature_completed") {
        featureStats[event.feature_slug].completed_at = event.ts;
      }

      if (
        event.event === "phase_completed" &&
        event.phase === "ae-generate-plan" &&
        event.manual_development_duration_estimate != null
      ) {
        featureStats[event.feature_slug].manual_development_duration_estimate =
          Number(event.manual_development_duration_estimate);
      }

      featureStats[event.feature_slug].last_event_at = event.ts;

      if (event.session_metrics) {
        featureStats[event.feature_slug].last_session_metrics =
          event.session_metrics;
        if (event.session_metrics.model)
          featureStats[event.feature_slug].model = event.session_metrics.model;
      }

      if (event.phase && event.session_metrics) {
        const phase = event.phase;
        const phaseEntry = featureStats[event.feature_slug].phases;
        if (!phaseEntry[phase]) {
          phaseEntry[phase] = {
            start_metrics: null,
            input_tokens: 0,
            output_tokens: 0,
            cache_read_input_tokens: 0,
            total_token_usage: 0,
          };
        }
        if (event.event === "phase_started") {
          phaseEntry[phase].start_metrics = event.session_metrics;
        } else if (event.event === "phase_completed") {
          const start = phaseEntry[phase].start_metrics || {
            input_tokens: 0,
            output_tokens: 0,
            cache_read_input_tokens: 0,
            total_token_usage: 0,
          };
          const d = deltaMetrics(event.session_metrics, start);
          phaseEntry[phase].input_tokens = d.input_tokens;
          phaseEntry[phase].output_tokens = d.output_tokens;
          phaseEntry[phase].cache_read_input_tokens = d.cache_read_input_tokens;
          phaseEntry[phase].total_token_usage = d.total_token_usage;
        }
      }

      if (event.decision) {
        if (!featureStats[event.feature_slug].decisions[event.decision]) {
          featureStats[event.feature_slug].decisions[event.decision] = 0;
        }
        featureStats[event.feature_slug].decisions[event.decision]++;
      }
    }

    if (event.decision) {
      if (!overallStats.decision_distribution[event.decision]) {
        overallStats.decision_distribution[event.decision] = 0;
      }
      overallStats.decision_distribution[event.decision]++;
    }
  }

  // Use last event for each feature (cumulative values)
  const featureDurationsMinutes = [];
  const manualEstimatesMinutes = [];

  for (const feature of Object.keys(featureStats)) {
    const lastMetrics = featureStats[feature].last_session_metrics;
    if (lastMetrics) {
      overallStats.total_input_tokens += lastMetrics.input_tokens || 0;
      overallStats.total_output_tokens += lastMetrics.output_tokens || 0;
      overallStats.total_cache_read_input_tokens +=
        lastMetrics.cache_read_input_tokens || 0;
      overallStats.total_token_usage += lastMetrics.total_token_usage || 0;
    }

    const fs_ = featureStats[feature];
    if (fs_.created_at && fs_.completed_at) {
      const diffMs =
        new Date(fs_.completed_at).getTime() -
        new Date(fs_.created_at).getTime();
      featureDurationsMinutes.push(diffMs / 60000);
    }
    if (fs_.manual_development_duration_estimate != null) {
      manualEstimatesMinutes.push(fs_.manual_development_duration_estimate);
    }
  }

  const avg = (arr) =>
    arr.length === 0 ? null : arr.reduce((a, b) => a + b, 0) / arr.length;

  const phaseTokens = calculatePhaseTokens(events);
  const maxPhaseEntry = Object.entries(phaseTokens).reduce(
    (prev, current) =>
      (prev[1]?.total_token_usage || 0) > (current[1]?.total_token_usage || 0)
        ? prev
        : current,
    [null, { total_token_usage: 0 }],
  );

  const resolvedSlug =
    targetSlug && featureStats[targetSlug] ? targetSlug : lastFeature;
  const resolvedSlugSource =
    targetSlug && featureStats[targetSlug] ? "specified" : "last";

  if (targetSlug && !featureStats[targetSlug]) {
    console.warn(
      `Warning: feature slug "${targetSlug}" not found in telemetry. Falling back to last feature.`,
    );
  }

  const lastFeatureStats = featureStats[resolvedSlug] || {};
  const lastFeaturePhases = lastFeatureStats.phases || {};
  const lastFeatureMaxPhaseEntry =
    Object.entries(lastFeaturePhases).length > 0
      ? Object.entries(lastFeaturePhases).reduce((prev, current) =>
          (prev[1]?.total_token_usage || 0) >
          (current[1]?.total_token_usage || 0)
            ? prev
            : current,
        )
      : [null, null];

  const lastFeatureEndTs =
    lastFeatureStats.completed_at || lastFeatureStats.last_event_at;
  const lastFeatureDurationMinutes =
    lastFeatureStats.created_at && lastFeatureEndTs
      ? (new Date(lastFeatureEndTs).getTime() -
          new Date(lastFeatureStats.created_at).getTime()) /
        60000
      : null;
  const lastFeatureDurationInProgress = !lastFeatureStats.completed_at;

  const metrics = {
    overall_usage: {
      total_input_tokens: overallStats.total_input_tokens,
      total_output_tokens: overallStats.total_output_tokens,
      total_cache_read_input_tokens: overallStats.total_cache_read_input_tokens,
      total_token_usage: overallStats.total_token_usage,
      no_cache_total_token_usage:
        overallStats.total_input_tokens + overallStats.total_output_tokens,
      session_count: overallStats.sessions.size,
      feature_count: overallStats.features.size,
      models_used: Array.from(overallStats.models),
      decision_distribution: overallStats.decision_distribution,
      avg_feature_duration_minutes: avg(featureDurationsMinutes),
      avg_manual_development_duration_estimate_minutes: avg(
        manualEstimatesMinutes,
      ),
      phase_with_most_tokens: {
        phase: maxPhaseEntry[0],
        total_token_usage: maxPhaseEntry[1].total_token_usage,
      },
      phase_breakdown: Object.fromEntries(
        Object.entries(phaseTokens).map(([phase, stats]) => [
          phase,
          {
            total_token_usage: stats.total_token_usage,
            input_tokens: stats.input_tokens,
            output_tokens: stats.output_tokens,
            cache_read_input_tokens: stats.cache_read_input_tokens,
            event_count: stats.event_count,
          },
        ]),
      ),
    },
    last_feature: {
      feature_slug: resolvedSlug,
      slug_source: resolvedSlugSource,
      created_at: lastFeatureStats.created_at || null,
      completed_at: lastFeatureStats.completed_at || null,
      last_updated_at: lastFeatureStats.last_event_at || null,
      current_phase: Object.keys(lastFeaturePhases).pop() || null,
      model: lastFeatureStats.model || null,
      duration_minutes: lastFeatureDurationMinutes,
      duration_in_progress: lastFeatureDurationInProgress,
      manual_development_duration_estimate_minutes:
        lastFeatureStats.manual_development_duration_estimate ?? null,
      total_input_tokens:
        lastFeatureStats.last_session_metrics?.input_tokens || 0,
      total_output_tokens:
        lastFeatureStats.last_session_metrics?.output_tokens || 0,
      total_cache_read_input_tokens:
        lastFeatureStats.last_session_metrics?.cache_read_input_tokens || 0,
      total_token_usage:
        lastFeatureStats.last_session_metrics?.total_token_usage || 0,
      no_cache_total_token_usage:
        (lastFeatureStats.last_session_metrics?.input_tokens || 0) +
        (lastFeatureStats.last_session_metrics?.output_tokens || 0),
      phase_with_most_tokens: lastFeatureMaxPhaseEntry[0]
        ? {
            phase: lastFeatureMaxPhaseEntry[0],
            total_token_usage: lastFeatureMaxPhaseEntry[1].total_token_usage,
          }
        : null,
      phase_breakdown: lastFeaturePhases,
      decisions: lastFeatureStats.decisions || {},
    },
  };

  return metrics;
}

function formatDecisionDistribution(decisions) {
  return Object.entries(decisions)
    .map(([decision, count]) => `- **${decision}:** ${count}`)
    .join("\n");
}

function formatPhaseBreakdown(phases) {
  return Object.entries(phases)
    .map(
      ([phase, stats]) =>
        `| ${phase} | ${stats.input_tokens.toLocaleString()} | ${stats.output_tokens.toLocaleString()} | ${stats.cache_read_input_tokens.toLocaleString()} | ${stats.total_token_usage.toLocaleString()} |`,
    )
    .join("\n");
}

function formatLastFeaturePhaseBreakdown(phases) {
  return Object.entries(phases)
    .map(
      ([phase, stats]) =>
        `| ${phase} | ${stats.input_tokens.toLocaleString()} | ${stats.output_tokens.toLocaleString()} | ${stats.cache_read_input_tokens.toLocaleString()} | ${stats.total_token_usage.toLocaleString()} |`,
    )
    .join("\n");
}

function formatMarkdownReport(metrics) {
  const overall = metrics.overall_usage;
  const lastFeature = metrics.last_feature;

  const dateGenerated = new Date().toISOString().split("T")[0];

  return `  
# Developer Telemetry Report

Generation date: ${dateGenerated}

*This report provides an overview of your telemetry metrics using the GAP AI Dev Kit. If you want to know how much are you spending when using the kit, you can find Antrhopic's pricing details here: https://platform.claude.com/docs/en/about-claude/pricing*

## Overall Usage Metrics

| Metric | Value |
|--------|-------|
| Total Token Usage | ${overall.total_token_usage.toLocaleString()} |
| Input Tokens | ${overall.total_input_tokens.toLocaleString()} |
| Output Tokens | ${overall.total_output_tokens.toLocaleString()} |
| Cache Read Tokens | ${overall.total_cache_read_input_tokens.toLocaleString()} |
| No-Cache Total | ${overall.no_cache_total_token_usage.toLocaleString()} |
| Sessions | ${overall.session_count.toLocaleString()} |
| Features | ${overall.feature_count.toLocaleString()} |
| Avg Feature Duration | ${overall.avg_feature_duration_minutes != null ? overall.avg_feature_duration_minutes.toFixed(1) + " min" : "N/A"} |
| Avg Manual Dev Estimate | ${overall.avg_manual_development_duration_estimate_minutes != null ? overall.avg_manual_development_duration_estimate_minutes.toFixed(1) + " min" : "N/A"} |

*Don't be scared by the cache read tokens! A high number of cache reads indicates that the kit is effectively leveraging past interactions to optimize performance. Also, they are priced at 10% of input tokens, so they don't represent that big of cost.*

### Models Used
${overall.models_used.map((m) => `- ${m}`).join("\n")}

### Decision Distribution
${formatDecisionDistribution(overall.decision_distribution)}

### Phase with Most Tokens
- **Phase:** ${overall.phase_with_most_tokens.phase}
- **Token Usage:** ${overall.phase_with_most_tokens.total_token_usage.toLocaleString()} tokens

### Phase Breakdown

| Phase | Input Tokens | Output Tokens | Cache Tokens | Total Tokens |
|-------|-------|--------|-------|-------|
${formatPhaseBreakdown(overall.phase_breakdown)}

## ${lastFeature.slug_source === "specified" ? "Feature" : "Last Feature"}: ${lastFeature.feature_slug}

### Timeline
- **Created:** ${lastFeature.created_at}
- **Completed:** ${lastFeature.completed_at || "In Progress"}
- **Last Updated:** ${lastFeature.last_updated_at}
- **Duration:** ${lastFeature.duration_minutes != null ? lastFeature.duration_minutes.toFixed(1) + " min" + (lastFeature.duration_in_progress ? " (in progress)" : "") : "N/A"}
- **Manual Dev Estimate:** ${lastFeature.manual_development_duration_estimate_minutes != null ? lastFeature.manual_development_duration_estimate_minutes.toFixed(1) + " min" : "N/A"}
- **Time saved (vs manual estimate):** ${
    lastFeature.duration_minutes != null &&
    lastFeature.manual_development_duration_estimate_minutes != null
      ? (
          lastFeature.manual_development_duration_estimate_minutes -
          lastFeature.duration_minutes
        ).toFixed(1) + " min"
      : "N/A"
  }
- **Current Phase:** ${lastFeature.current_phase}
- **Model:** ${lastFeature.model}

### Token Usage (Cumulative)

| Metric | Value |
|--------|-------|
| Total Usage | ${lastFeature.total_token_usage.toLocaleString()} |
| Input Tokens | ${lastFeature.total_input_tokens.toLocaleString()} |
| Output Tokens | ${lastFeature.total_output_tokens.toLocaleString()} |
| Cache Read | ${lastFeature.total_cache_read_input_tokens.toLocaleString()} |
| No-Cache Total | ${lastFeature.no_cache_total_token_usage.toLocaleString()} |

### Phase with Most Tokens
- **Phase:** ${lastFeature.phase_with_most_tokens?.phase || "N/A"}
- **Token Usage:** ${lastFeature.phase_with_most_tokens?.total_token_usage?.toLocaleString() || "N/A"}

### Phase Breakdown

| Phase | Input Tokens | Output Tokens | Cache Tokens | Total Tokens |
|-------|-------|--------|-------|-------|
${formatLastFeaturePhaseBreakdown(lastFeature.phase_breakdown)}

### Decisions
${formatDecisionDistribution(lastFeature.decisions)}

---

_Created by EBU Labs - GAP AI Dev Kit - ${new Date().getFullYear()}_
`;
}

function main() {
  try {
    generateTelemetryReport();
  } catch (err) {
    console.error("Error generating telemetry report:", err);
    process.exit(1);
  }
}

main();
