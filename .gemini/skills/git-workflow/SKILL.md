---
name: git-workflow
description: Specialized workflow for staging, committing, and pushing changes in this repository. Use this to ensure commit messages follow specific formatting constraints.
---

# Git Workflow

This skill defines the standards for source control operations in this repository.

## Commit Message Standards
When preparing a commit message, strictly adhere to the following rules:
- **Maximum Length**: 150 characters.
- **Tone**: Short and direct.
- **No Prefixes**: DO NOT use conventional commit prefixes like `feat:`, `fix:`, `test:`, `docs:`, etc.
- **No Unicode**: Use only standard ASCII characters. No emojis or special symbols.

## Workflow
1. **Gather Context**: Run `git status`, `git diff HEAD`, and `git log -n 3` to understand the changes and match the project's direct style.
2. **Stage**: Add the relevant files.
3. **Commit**: Apply a message that follows the standards above.
4. **Push**: Sync changes to the remote.
