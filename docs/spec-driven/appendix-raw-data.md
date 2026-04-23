# Appendix: Raw Evaluation Data

## Test Environment

- Test date: April 21, 2026
- Evaluation model: GPT-5.3 Codex
- Use case: .NET 8 -> .NET 10 application upgrade
- Specialized agent: Admin API Expert

Evaluation controls:
- Consistent prompt across tools
- Single execution pass (no retries)
- No human intervention during execution
- Time/token/outcome metrics captured from runs

## Premium Request Calculations

Baseline:
- Initial token percentage: 53.8%
- Final token percentage: 56.5%
- Total available budget: 46.2 percentage points

Per-tool consumption:

```text
Plan Mode:
  Start: 54.8%
  End: 55.5%
  Consumption: 0.7%
  Percentage of total: (0.7 / 46.2) * 100 = 1.5% of total quota

Speckit:
  Start: 53.8%
  End: 54.8%
  Consumption: 1.0%
  Percentage of total: (1.0 / 46.2) * 100 = 2.2% of total quota

OpenSpec:
  Start: 55.8%
  End: 56.5%
  Consumption: 0.7%
  Percentage of total: (0.7 / 46.2) * 100 = 1.5% of total quota

Superpowers:
  Start: 55.5%
  End: 55.8%
  Consumption: 0.3%
  Percentage of total: (0.3 / 46.2) * 100 = 0.6% of total quota
```

## Execution Time Breakdown

Plan Mode:
- Plan creation: ~10 minutes (54.8% -> 55.1%)
- Execution: ~10 minutes (55.1% -> 55.5%)
- Failure/debugging: <5 minutes
- Total: 25 minutes

Speckit:
- Constitution: ~10 minutes (53.8% -> 53.9%)
- Specify: ~10 minutes (53.9% -> 54.0%)
- Plan: ~15 minutes (54.0% -> 54.3%)
- Tasks: ~10 minutes (54.3% -> 54.5%)
- Implement attempt: ~15 minutes (54.5% -> 54.8%)
- Total: 80 minutes

OpenSpec:
- Proposal generation: ~3-4 minutes (55.8% -> 56.0%)
- Change application: ~30-35 minutes (56.0% -> 56.5%)
- Build/failure: <5 minutes
- Total: 40 minutes

Superpowers:
- Brainstorming: ~5 minutes (55.5% -> 55.6%)
- Git worktrees: ~5 minutes (55.6% -> 55.6%)
- Writing plans: ~5 minutes (55.6% -> 55.7%)
- Subagent development: ~50 minutes (55.7% -> 55.75%)
- Code review: ~30 minutes (55.75% -> 55.78%)
- Finish branch: ~70 minutes (55.78% -> 55.8%)
- Total: 165 minutes (2 hours 45 minutes)

## Error Documentation

Plan Mode error:

```text
Error: Cannot load library libgssapi_krb5.so.2
Full Message: Error loading shared library libgssapi_krb5.so.2: No such file or directory
Classification: Environmental/Infrastructure
Recovery: Requires system package installation
```

Speckit error:

```text
Error: Cannot load library libgssapi_krb5.so.2
Full Message: Error loading shared library libgssapi_krb5.so.2: No such file or directory
Classification: Environmental/Infrastructure
Recovery: Requires system package installation
Note: Identical to Plan Mode error despite higher complexity
```

OpenSpec error:

```text
Error: Docker image build failure
Full Message: Build process terminated with error (exact error not captured)
Classification: Generated Changes Correctness
Recovery: Requires proposal revision or manual intervention
Risk: High (code generation problem, not environment)
```

Superpowers result:

```text
Status: SUCCESS
Primary Deliverable: .NET 8 -> .NET 10 upgrade completed
Additional Improvements:
  - Dockerfile updated to follow admin platform standards
  - Vendor dependency bug detected and fixed
  - All code reviewed and verified
  - Complete test coverage with passing tests

No errors or failures in execution
Clean, reproducible process
Production-ready output
```
