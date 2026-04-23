# Spec-Driven Development Tools Comparison

Document Version: 1.1  
Date: April 23, 2026  
Classification: Technical Assessment  
Audience: Engineering leadership, architecture reviewers, SDLC decision makers

## Purpose

This document provides a simplified decision-oriented summary of the tool evaluation for a .NET 8 -> .NET 10 upgrade scenario. Detailed findings are split into per-tool files and a shared appendix.

## Evaluated Tools

- Plan Mode (Copilot built-in planning workflow)
- Speckit (AI-driven specification workflow tool)
- OpenSpec (specification proposal and application tool)
- Superpowers (skill-based development framework)

## Primary Decision

Primary recommendation: Superpowers  
Recommendation confidence: High

Reason for recommendation:
- Only evaluated option that completed successfully end-to-end
- Lowest token consumption in this run (0.3%)
- Integrated quality gates reduced risk
- Minimal repository intrusion via isolated workflow
- Delivered additional value (Dockerfile improvements and bug detection)

## At-a-Glance Metrics

| Metric | Superpowers | Runner-up | Notes |
|---|---|---|---|
| Execution Success Rate | 100% | 0% | Only successful tool in this evaluation |
| Premium Request Efficiency | 0.3% | 0.7% | 57% lower than Plan Mode/OpenSpec |
| Quality Gates | 7 | 0 | Strong built-in validation |
| Repository Intrusion | Minimal | High | Speckit introduced highest artifact footprint |

## Risk Assessment

| Tool | Distribution Channel | Primary Risk Driver | Failure Detection Timing | Recovery Mode | Security Suitability | Overall Risk |
|---|---|---|---|---|---|---|
| Plan Mode | AI Assistant (Copilot platform) | Environment dependency gaps | Runtime | Manual environment fix and rerun | Medium (acceptable with hardened runtime, least-privilege execution, and mandatory review gates) | Medium |
| Speckit | Python package ecosystem | Environment dependency gaps plus package supply-chain exposure and high repo artifacts | Runtime | Manual environment fix plus artifact cleanup | Medium-Low (larger artifact footprint and Python dependency surface increase governance overhead) | Medium |
| OpenSpec | npm package ecosystem | Generated change correctness/build compatibility plus npm supply-chain exposure | Build/apply phase | Manual proposal revision and validation | Low (higher risk of unsafe generated changes without strong validation controls) | High |
| Superpowers | AI Assistant (Copilot platform) + plugin ecosystem | Process complexity (mitigated by quality gates) and plugin trust chain | Early and continuous | Isolated workflow with staged validation | High (worktree isolation plus multi-stage review/testing lowers chance of insecure changes reaching mainline) | Low |

Risk-based conclusion:
- Lowest operational, production, and security risk in this evaluation: Superpowers.
- Conditional security posture: Plan Mode, if runtime dependencies are hardened and policy checks are enforced.
- Highest combined risk in this evaluation: OpenSpec due to generated-change correctness failure and weaker built-in validation.

Security scope note:
- This section reflects observed security suitability from workflow controls and failure modes during evaluation; it is not a substitute for a formal security assessment or penetration test.

### Supply-Chain Vulnerability Considerations

These tools can include vulnerabilities inherited from community or vendor ecosystems (for example Python packages, npm packages, and editor/plugin dependencies).

Recommended minimum controls before organization-wide adoption:
1. Pin versions and lock dependencies for Python/npm tooling where supported.
2. Require software composition analysis (SCA) and CVE scanning in CI for tool dependencies.
3. Prefer signed releases, trusted publishers, and checksum/signature verification.
4. Run tools in isolated environments with least privilege and restricted network access.
5. Gate generated changes with static analysis, secret scanning, and mandatory human review.
6. Establish patch cadence and rapid upgrade playbooks for vulnerable tool components.

## Recommendations

1. Adopt Superpowers as the primary spec-driven workflow for complex or production-impacting changes.
2. Keep Plan Mode as a conditional backup after environment dependency hardening.
3. Do not adopt OpenSpec as a primary path for production-critical work without additional validation gates.
4. Do not adopt Speckit as a primary path in this team context due to setup overhead and repository intrusion.

## Per-Tool Details

- [Plan Mode details](tools/plan-mode.md)
- [Speckit details](tools/speckit.md)
- [OpenSpec details](tools/openspec.md)
- [Superpowers details](tools/superpowers.md)

## Shared Data Appendix

- [Raw data, timing, token calculations, and error logs](appendix-raw-data.md)

## Notes and Limits

- Evaluation used a single pass per tool with no retries.
- Two failures (Plan Mode, Speckit) were classified as environmental due to the same missing runtime library.
- OpenSpec failure was classified as generated-change correctness risk because the run failed during build application.
