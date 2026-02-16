# Feature Plans for AI Agents

This directory contains detailed implementation plans for features in the Ed-Fi ODS Admin API, specifically designed for AI agents like Claude Sonnet 4.5.

## Purpose

These feature plans provide:
- **Step-by-step implementation instructions**
- **Code examples and patterns**
- **File locations and reference paths**
- **Testing requirements**
- **Definition of Done checklists**

## How to Use

### For AI Agents (Claude Sonnet 4.5, etc.)

1. **Read the feature plan**: Each markdown file contains complete implementation instructions
2. **Follow the phases sequentially**: Don't skip ahead - each phase builds on the previous
3. **Reference agent-skills.md**: Common patterns and skills are documented in `../.github/agent-skills.md`
4. **Check copilot-instructions.md**: Code style guidelines are in `../.github/copilot-instructions.md`
5. **Update tests**: Always update unit, integration, and E2E tests
6. **Verify Definition of Done**: Complete all checklist items before marking as done

### For Human Developers

These documents can also serve as:
- Implementation guides for complex features
- Onboarding documentation for new developers
- Reference for consistent code patterns
- Architecture decision records

## Structure

Each feature plan follows this structure:

```markdown
# [Feature Name]

## AI Agent Instructions
- Specific guidance for AI implementation

## Feature Overview
- Objective
- Affected components
- Design references

## Requirements Summary
- Current state
- Expected state
- Key changes

## Implementation Plan
### Phase 1: [Name]
- Task-by-task breakdown
- Code examples
- File locations

### Phase 2: [Name]
...

## Code Style Guidelines
- Reference to existing style guides

## Reference Files
- Key files to understand

## Common Pitfalls
- Things to avoid

## Definition of Done
- Completion checklist
```

## Available Feature Plans

- **ADMINAPI-1357-education-organization-restructure.md** - Restructure education organization endpoints to use nested response models

## Related Documents

- `../.github/agent-skills.md` - Reusable development patterns and skills
- `../.github/copilot-instructions.md` - Code style and formatting guidelines
- `../../docs/design/` - Feature design documents

## Adding New Feature Plans

When creating a new feature plan:

1. **Create a new markdown file** in this directory
2. **Name it descriptively**: `[TICKET-ID]-[feature-name].md`
3. **Follow the structure** outlined above
4. **Include code examples** with actual file paths
5. **Add to this README** in the "Available Feature Plans" section
6. **Link from the design doc** in `/docs/design/`

### Template

```markdown
# [TICKET-ID]: [Feature Name]

## AI Agent Instructions for Claude Sonnet 4.5
[Specific guidance]

## 🎯 Feature Overview
[Objective and scope]

## 📋 Requirements Summary
[Current vs. Expected state]

## 🏗️ Implementation Plan
### Phase 1: [Name]
#### Task X.X: [Task name]
**File**: `path/to/file.cs`
**Actions**: [Specific steps]

## 🎨 Code Style Guidelines
[Reference to copilot-instructions.md]

## 📚 Reference Files
[List of key files]

## ⚠️ Common Pitfalls
[Things to watch out for]

## ✅ Definition of Done
- [ ] Checklist items

## 🚀 Future Considerations
[Future implications]
```

## Best Practices

1. **Be specific**: Include exact file paths and line numbers where possible
2. **Show examples**: Include before/after code snippets
3. **Explain the why**: Don't just say what to change, explain why
4. **Reference existing patterns**: Point to similar code in the codebase
5. **Include tests**: Always document test requirements
6. **Keep updated**: Update the plan if implementation reveals issues

## Feedback

If you're an AI agent and find these instructions unclear or incomplete, please:
1. Complete the implementation to the best of your ability
2. Document any ambiguities or issues encountered
3. Suggest improvements to the feature plan format

---

**Last Updated**: February 10, 2026  
**Maintainer**: Ed-Fi Alliance
