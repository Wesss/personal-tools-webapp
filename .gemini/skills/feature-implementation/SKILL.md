---
name: feature-implementation
description: Mandatory implementation loop for developing features. Use this when tasked with implementing or developing a new feature to ensure a structured approach with user checkpoints for planning, interface design, and implementation.
---

# Feature Implementation Workflow

This skill defines the mandatory implementation loop for developing features in this repository.

## Workflow Phases

### Phase 1: Research and Planning
1. **Research**: Systematically map the codebase and validate assumptions.
2. **Implementation Plans**: Propose **three** distinct implementation plans to the user.
3. **Wait for Input**: Present the plans and **STOP**. Wait for the user to pick a plan or provide follow-up planning/investigation. Do not proceed until a plan is chosen.

### Phase 2: Interface and Test Skeleton
1. **Design**: Write or edit top-level interfaces, signatures, and classes based on the accepted plan.
2. **Testing Strategy**: Create new or updated unit tests according to the change (skeleton/failing tests).
3. **Wait for Approval**: Present the interfaces and tests to the user and **STOP**. Wait for explicit approval. Do not proceed until the design and tests are approved.

### Phase 3: Implementation and Validation
1. **Implementation**: Take the accepted plan and interface skeleton and implement the logic.
2. **Compile**: Compile the project(s) to ensure they build correctly.
3. **Unit Tests**: Run unit tests in the backend unit test projects.
4. **Testing Suite**: Run the full testing suite.
5. **Resolve Errors**: If any errors occur, resolve them.
6. **Wait for Approval**: Present the implementation and test results to the user and **STOP**. Wait for explicit approval. Do not proceed until the implementation is approved.

### Phase 4: Delivery
1. **Git Operations**: Add, commit, and push the changes to the repository.
