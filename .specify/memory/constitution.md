<!--
Sync Impact Report
- Version change: 0.0.0 -> 1.0.0
- Modified principles: Initial constitution scaffold established for ContosoDashboard
- Added sections: Core Principles, Additional Constraints, Development Workflow, Governance
- Removed sections: none
- Deferred items: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
This repository exists for education and demonstration. All features and changes must remain within the training scope of a local, offline, mock-authentication sample application. Any requirement that depends on production-grade security, external cloud services, or live operational data must be labeled as out of scope or intentionally simulated.

### II. Secure-by-Default Learning
Authentication, authorization, and data access rules must protect users and demonstrate sound engineering practice without implying production readiness. Role-based access, user isolation, and explicit authorization checks are mandatory for any protected page or service. Mock implementations must not be represented as valid production identity, credential handling, or operational security controls.

### III. Spec-Driven Delivery
Every meaningful feature or behavior change must begin with a clear requirement, a bounded scope, and verifiable acceptance criteria. The project documentation in .specify must remain aligned with implementation, and a change is not considered complete until its intent is traceable from requirement to code and validation.

### IV. Test-First and Verifiable Behavior
New behavior must be validated before or alongside implementation. For user-facing or security-sensitive work, the team must define the failing condition, demonstrate the gap, and then implement the minimal fix. Regression checks must verify the real behavior of the application rather than only mocked assumptions.

### V. Simplicity, Clarity, and Maintainability
The codebase must favor small, understandable components, explicit data flows, and local abstractions over hidden complexity. New features must be easy to explain, reason about, and test. Unnecessary frameworks, external dependencies, and speculative architecture are not permitted without a documented business or technical justification.

## Additional Constraints

The project must remain compatible with the training goals of the ContosoDashboard sample:
- Target runtime: ASP.NET Core 9 with Blazor Server and local EF Core storage.
- Default data and configuration must operate offline without external services.
- Authentication is a mock training implementation and must not be treated as production identity.
- Security controls must be explicit and observable: authorization attributes, service checks, and user isolation rules.
- New files and patterns must preserve the sample app's clarity and educational value.
- Any migration path to Azure or production infrastructure must remain optional and clearly documented as a future step, not the default runtime assumption.

## Development Workflow

- Requirements, scope, and acceptance criteria must be recorded in the Spec Kit workflow before implementation begins.
- Changes must be small, traceable, and reviewable.
- Design and code decisions must preserve the project's training-only scope and security posture.
- Every feature must pass the relevant validation step before merge, including UI and security behavior when applicable.
- Documentation changes that affect behavior or setup must remain in sync with the code.

## Governance

This constitution governs the working standards for ContosoDashboard. It supersedes informal local practices whenever there is a conflict. Any deviation must be justified in writing, reviewed, and approved before the work is merged.

Amendments require all of the following:
- a clear rationale for the change,
- a version bump using Semantic Versioning,
- review by the responsible maintainer or project lead,
- documentation of any migration impact or follow-up work.

Versioning policy:
- MAJOR: incompatible rule or principle removal or reinterpretation
- MINOR: new principle or materially expanded guidance
- PATCH: clarifying language, typo fixes, or non-semantic improvements

Compliance review expectations:
- PRs must confirm that the relevant constitutional rule or workflow was followed.
- Security and authorization changes require explicit review of the user impact and training-scope implications.
- The project lead or reviewer may request revision when behavior is ambiguous, unverified, or inconsistent with the constitution.

**Version**: 1.0.0 | **Ratified**: 2026-09-11 | **Last Amended**: 2026-09-11
