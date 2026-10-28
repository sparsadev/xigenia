
# Xigenia — Agent Instructions

## Project
Xigenia is a personal finance MVP built with C#, ASP.NET Core,
Blazor WebAssembly, Entity Framework Core, SQL Server,
Docker, and Meta Model API.

Preserve the existing architecture. Do not recreate the solution.

## Working Rules
- Implement only the specific task requested.
- Make the smallest readable and maintainable change.
- Work in small, independently reviewable steps.
- Change only files necessary for the requested task.
- Do not refactor unrelated code.
- Do not add packages, frameworks, or projects without approval.
- Do not create, edit, or delete files before authorization
  when the request is for analysis or planning.
- Ask before destructive or irreversible operations.
- Stop after completing the requested step.

## Execution Policy
- Do not run builds, tests, migrations, servers, containers,
  HTTP requests, or application startup commands.
- Do not run integration tests or call external APIs.
- The developer performs all runtime validation.
- Do not claim code was tested or verified.
- Report any recommended manual checks briefly.

## Security
- Never read, display, print, or log .env files, API keys,
  passwords, tokens, certificates, or connection secrets.
- Never send real personal or financial data to AI models.
- Use synthetic data for examples.
- Enforce authentication and authorization on the server.
- Never allow direct model access to the database.
- Validate model output before using it in financial records.

## Engineering
- Follow existing project conventions.
- Prefer .NET built-in features before adding dependencies.
- Keep financial calculations deterministic in C#.
- Use decimal for financial monetary calculations.
- Use asynchronous APIs when appropriate.
- Preserve database and API compatibility unless approved.
- Treat model-generated financial data as unverified.
- Keep the code clear; avoid premature abstractions.

## Context Efficiency
- Inspect only relevant files.
- Do not scan the entire repository for routine changes.
- Do not read build artifacts or generated files.
- Do not repeat large code blocks in responses.
- Do not provide lengthy explanations unless requested.
- Do not autonomously begin additional tasks.

## Completion Format
Respond briefly with:
1. Files changed.
2. What was implemented.
3. What was executed (normally: nothing).
4. Limitations or manual validation needed.

Stop and wait for the next instruction.