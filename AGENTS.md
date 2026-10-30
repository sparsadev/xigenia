# Xigenia — Agent Instructions

## Project
Xigenia is a personal finance MVP built with C#, ASP.NET Core,
Blazor WebAssembly, Entity Framework Core, SQL Server, Docker,
and Meta Model API.

Preserve the existing architecture. Do not recreate the solution.

## Governance
Follow the approved AI Agent Collaboration & Execution Policy:
https://lyra.hadranax.com/doc/ai-agent-collaboration-execution-policy-etjXRRqvhy

Document ID: acdcfd0a-0f26-4fb1-b1b5-34c1ddb52e37
Approved revision: 10.

Lyra is the central knowledge base. Git is authoritative for source
code and version-controlled configuration.

Formal documentation must use English (en-US) and follow the
Documentation Language Policy. Internal documentation must remain
private unless external publication is explicitly approved.

Do not silently modify governance documents. Policy changes and
exceptions require explicit Project Owner approval. Report conflicts
with these instructions and request clarification before proceeding
with the affected operation.

## Authorization and Roles
- Explicit Project Owner authorization must identify the agent, task,
  permitted operations, affected resources, and expected deliverables.
- Record authorization and its scope in the task conversation or
  approved project documentation.
- Authorization applies only to the specified task and operations.
  It does not automatically extend to additional resources, subsequent
  tasks, or future sessions.
- ChatGPT coordinates requirements, architecture, acceptance criteria,
  and handoffs. It cannot authorize operations for the Project Owner.
- Muse Code performs explicitly authorized implementation work and
  prepares implementation handoffs.
- Codex performs explicitly authorized independent verification and
  operational work. During independent verification, Codex must not
  modify Muse Code implementations without separate corrective
  authorization.
- Read-only or planning authorization does not permit file changes.
  Authorization to validate does not permit database changes,
  migrations, deployment, or infrastructure changes.
- When authorization is ambiguous or incomplete, stop the affected
  operation, report it as blocked, and request clarification.

## Working Rules
- Perform only the authorized task and stop when it is complete.
- Make the smallest readable, maintainable change in independently
  reviewable steps. Change only necessary files.
- Do not refactor unrelated code or add packages, frameworks, or
  projects without explicit approval.
- Do not autonomously begin additional tasks.

## Execution Controls
- Run builds, tests, integration checks, HTTP requests, external API
  calls, servers, containers, or application startup commands only
  when explicitly authorized for the task.
- Git writes, database schema or record changes, migrations,
  deployments, infrastructure or access-permission changes, and
  destructive or irreversible operations require explicit
  authorization covering those operations and resources.
- Changes to production environments require explicit approval.
- Report blocked operations rather than bypassing authorization.
- Never claim an operation completed or code passed validation
  without execution evidence. Distinguish inspection from runtime
  validation and report unexecuted checks explicitly.

## Security
- Never read, display, print, or log .env files, API keys, passwords,
  tokens, certificates, or connection secrets.
- Never send real personal or financial data to AI models.
- Use synthetic data for examples and tests.
- Enforce authentication and authorization on the server.
- Never allow direct model access to the database.
- Validate model output before using it in financial records.
- Treat model-generated financial data as unverified.
- Follow least-privilege principles and avoid unrelated systems.

## Engineering
- Follow existing project conventions.
- Prefer .NET built-in features before adding dependencies.
- Keep financial calculations deterministic in C#.
- Use decimal for monetary calculations.
- Use asynchronous APIs when appropriate.
- Preserve database and API compatibility unless explicitly approved.
- Keep code clear and avoid premature abstractions.

## Context Efficiency
- Inspect only relevant files. Avoid repository-wide scans for
  routine changes.
- Do not read build artifacts or generated files.
- Keep reports concise and avoid repeating large code blocks.

## Completion Reports
Follow governance Section 5. Include only supported claims and
distinguish observed results from assumptions, recommendations,
and unverified claims.

Muse Code implementation handoffs must include:
- Task identifier and authorized scope.
- Summary of changes and files created or modified.
- API or data-contract changes, or an explicit statement of none.
- Assumptions, known limitations, and recommended validation.
- Operations actually executed and operations not executed.
- Relevant evidence and repository references when available.

Codex verification reports must include:
- Task identifier and authorized scope.
- Tested revision or commit, when applicable.
- Environment and dependencies.
- Commands or tools actually executed.
- Test cases, observed results, and evidence supporting each result.
- Defects and security findings, or an explicit statement that none
  were identified.
- Operations not executed.
- Final status: PASS, FAIL, BLOCKED, or NOT RUN.

Implementation handoffs must also include a final status using those
labels; distinguish implementation completion from validation results.

Documentation updates must reference the relevant task, source
revision, and verification evidence whenever applicable.
