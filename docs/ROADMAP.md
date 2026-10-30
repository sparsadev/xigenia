# Xigenia — Official Hackathon MVP Roadmap

Task: **Xigenia — Create the Official Hackathon MVP Roadmap**

Planning baseline: **October 30, 2026**

Submission target: **November 4, 2026**

Audience: Project Owner and authorized project collaborators; internal until publication is explicitly approved.

Document status: Updated for Project Owner review following independent content-review findings. Future execution and publication require separate authorization.

Revision task: **Xigenia — Resolve MVP Roadmap Review Findings**. Codex is authorized to edit only this document, inspect relevant source read-only, and review consistency. No other file changes, Git writes, Lyra writes, implementation, or runtime/operational checks are authorized.

## 1. Purpose and Competition Objective

Deliver a focused personal finance MVP for a competitive **Indie Developer submission to the Meta Global AI Developer Hackathon 2026**, targeting November 4, 2026. Demonstrate a coherent journey: sign in, record synthetic financial activity, inspect trustworthy financial summaries, and ask a real Meta AI assistant for contextual help.

Competition name, category eligibility, official deadline and time zone, judging criteria, required Meta technology, permitted partners, submission artifacts, and video limits remain **unverified external requirements**. November 4 is the Project Owner's planning target, not a verified organizer deadline. A future authorized review must confirm official rules before relying on them. This roadmap does not promise eligibility, prizes, partner benefits, or acceptance.

The original creation task authorized Codex to create **only `docs/ROADMAP.md`** on `main`, inspect relevant documentation and local Git evidence, review the document, and report status. The current revision authorization is recorded above. Neither documentation task authorizes milestone implementation, runtime validation, database/API/infrastructure operations, purchases, DNS changes, public publication, Lyra writes, staging, commits, or pushes.

Governance references: [AGENTS.md](../AGENTS.md), [AI Agent Collaboration & Execution Policy, approved revision 11](https://lyra.hadranax.com/doc/ai-agent-collaboration-execution-policy-etjXRRqvhy) (document ID `acdcfd0a-0f26-4fb1-b1b5-34c1ddb52e37`), and [Documentation Language Policy, approved revision 6](https://lyra.hadranax.com/doc/documentation-language-policy-7FGTzBtcrT) (document ID `4dc97aa1-f5b1-4558-a099-303676218b10`). All formal documentation uses English (en-US). This document references governance without changing it.

## 2. Current Project Baseline

Read-only inspection on October 30 found branch `main`, a clean working tree before this document, and three local commits ahead of the locally recorded `origin/main`. No remote refresh was performed; this is not a claim about current remote state.

| Evidence | Supported conclusion | Limit |
| --- | --- | --- |
| HEAD `6cf38a76d33116f1920046ed00bfe68e65f97a30` | Local governance-reference commit aligns AGENTS.md with approved revision 11. | No new application verification follows from this commit. |
| `a0cd5089008ecdf10603a990dd59f14ffe6fc900` | Local commit adds Domain/Application test coverage and FakeFinanceStore; its stat records 21 changed files. | A committed test inventory does not prove a new successful run. |
| [C5.6.2 — Unit Test Verification Report](https://lyra.hadranax.com/doc/c562-unit-test-verification-report-7cXnnwUxPH), revision 3 | Historical final result: **352 executed, 352 passed, 0 failed, 0 skipped**, 0 compilation warnings/errors, exit code 0; FS01–FS32 completed within the defined unit scope. | Consolidated historical evidence, not a run performed for this roadmap or proof of SQL/API/release correctness. |
| [Unit Test Infrastructure and FakeFinanceStore](https://lyra.hadranax.com/doc/unit-test-infrastructure-and-fakefinancestore-Hm9PCMV8lL), revision 3 | Test setup, deterministic fixtures, fake-store instrumentation, and batch progression are documented. | Fake behavior does not establish real persistence, transactions, concurrency, or authentication. |
| [Technical Documentation](https://lyra.hadranax.com/doc/xigenia-technical-documentation-8AcTJO0hpW), revision 3; [architecture](ARCHITECTURE.md); local commit history and tracked-file inventory | Existing financial backend and Blazor project provide the starting point. | Architecture includes proposed material; file existence and commit subjects do not certify delivered product behavior. |

The historical report described a then-uncommitted working tree at `859a3e05a287e4e9578258da321c19c461d9dc11`. The subsequent local test commit provides source traceability today, but no archived manifest establishes byte-for-byte identity with every historical execution. Do not relabel the historical 352-test result as a run against current HEAD. No tests were run for this task.

Preserve the existing C#/.NET 10, ASP.NET Core, Blazor WebAssembly, EF Core, SQL Server, Identity, Docker, and backend-orchestrated Meta Model API architecture. Documented projects include `src/Xigenia.Domain`, `src/Xigenia.Application`, `src/Xigenia.Api`, `src/Xigenia.Infrastructure`, `src/Xigenia.Shared`, `web/Xigenia.Web`, and `tests/Xigenia.Tests`.

Existing financial implementation evidence covers accounts, obligations, partial settlements, movements, transfers, categories, filtering, pagination, summaries, and deterministic application calculations. Real SQL Server correctness, deployed authentication/authorization, responsive product workflows, actual Meta AI integration, and a public deployment are **not established** by the inspected unit baseline. The Blazor inventory includes starter pages; it is not evidence of a finished finance UI. No domain ownership or hosting readiness was verified.

Authentication source inspection for this revision: `src/Xigenia.Api/Program.cs` configures `AddIdentityApiEndpoints<ApplicationUser>()`, maps `MapIdentityApi<ApplicationUser>()` under `/api/auth`, and enables authentication/authorization middleware. `src/Xigenia.Api/Endpoints/FinanceEndpoints.cs` requires authorization for the finance group. The inspected `web/Xigenia.Web/Program.cs` registers an HttpClient but does not establish a client authentication/session lifecycle. These are source observations, not executed endpoint evidence. No access-token lifetime, refresh-token usability, logout/revocation behavior, or completed browser sign-in flow has been verified. Section 5 defines the proposed minimal bearer-mode client behavior to validate under separate authorization; it does not claim that behavior is implemented or change the architecture.

## 3. Milestone Inventory and Historical Completion

Identifiers are retained for traceability. C5.6.3–C5.6.6 retain the phase meanings in the historical verification report. Historical status of C5.6.0–C5.6.2 is retained under the Project Owner's supplied baseline. Exact C5.6.0/C5.6.1 definitions and individual completion records were not recovered: their mappings below are **unverified**, with corroborating foundation/specification evidence only. C6–C11 descriptions are **proposed planning allocations**, not verified historical titles. Owner confirmation of those mappings remains open; no missing record is invented.

| Milestone | Scope and disposition | Evidence or exit dependency |
| --- | --- | --- |
| C5.6.0 | **Completed — Owner-supplied historical status; mapping unverified.** Planning description: test foundation. | Test infrastructure report and committed project/fake corroborate foundation work, not the exact milestone definition or a separate completion record. |
| C5.6.1 | **Completed — Owner-supplied historical status; mapping unverified.** Planning description: unit verification preparation. | FS01–FS32 and batch register corroborate specification work; no individual C5.6.1 completion report was retrieved. |
| C5.6.2 | **Completed — historical PASS within Domain/Application unit scope.** | 352 passing tests in the historical report; source commit `a0cd5089008ecdf10603a990dd59f14ffe6fc900`. |
| C5.6.3 | **Pending, reduced to essential SQL Server integration validation.** Cover demonstrated persistence, exact decimals, owner filtering, settlement integrity, transfer atomicity/rollback, and displayed query results. | P0-1; an isolated synthetic environment and explicit database/runtime authorization. Broader provider/cancellation/load matrices deferred. |
| C5.6.4 | **Pending, reduced to essential API security validation.** Verify sign-in, authoritative server owner identity, unauthenticated denial, cross-owner denial, invalid input, and safe errors for exposed workflows. | P0-1 and P0-3; explicit API/test authorization. Broad endpoint permutations deferred; no exposed authorization bypass may be deferred. |
| C5.6.5 | **Pending, focused defect correction and regression.** Triage demo, financial correctness, and security blockers; obtain separate corrective authorization; independently reverify changes. | C5.6.3/C5.6.4 findings, obligation-direction decision, affected regression and authorized suite execution. No automatic code correction. |
| C5.6.6 | **Pending, focused release gate.** Record candidate revision, environment, executed checks, open risks, and Owner decisions at Gates A–C in Section 5. | Gate A before public exposure; Gate B after deployed evidence and all submission artifacts are ready; Gate C authorizes submission separately. Receipt closes delivery afterward. |
| C6 | **P1 conditional — proposed budgeting allocation.** One period/category budget comparison only under the Section 9 admission rule. | Gates A/B passed and all mandatory artifacts ready; only Gate C and receipt may remain pending. At most 2 aggregate effort-hours including verification and recovery, outside protected reserves. Otherwise defer. |
| C7 | **P2 deferred — financial goals.** | Revisit after submission; no dependency on the P0 journey. |
| C8 | **P0 advanced — responsive Blazor product workflows.** | P0-2; stable API contracts and P0-1 owner/security behavior. Start before exhaustive stabilization is complete. |
| C9 | **P0 advanced — real Meta AI contextual assistance.** | P0-3; authorized provider access, safe backend context, deterministic totals, and C8 assistant UI. |
| C10 | **P2 deferred — receipt/invoice OCR.** | Extraction/confirmation design and separate scope; no OCR prerequisite for C9. |
| C11 | **P0 advanced in deployment-critical part — hosting, domain, DNS, HTTPS, and submission delivery.** Broader operational hardening remains deferred. | P0-4/P0-5, approvals, verified external requirements, and C5.6.6 release gate. |

Reducing validation breadth does not waive confidentiality, server authorization, financial correctness for demonstrated features, or traceable evidence. Record each deferred case rather than marking its original broader phase fully verified.

## 4. MVP Priority Matrix

| Priority | Deliverable | Milestones | Scope rule |
| --- | --- | --- | --- |
| P0-1 | Essential SQL Server persistence and authentication validation | C5.6.3–C5.6.6 | Establish reliable, owner-isolated financial workflows used in the demo. |
| P0-2 | Responsive Blazor web application | C8 | Complete one usable end-to-end financial journey on mobile and desktop. |
| P0-3 | Real Meta AI contextual financial assistance | C9 | Demonstrate real provider use and user-specific synthetic context, with safe failures. |
| P0-4 | Public deployment, Namecheap domain acquisition, DNS, HTTPS | C11 critical portion | Provide a reachable, secure product URL; start lead-time decisions early. |
| P0-5 | Public usability documentation, demonstration, video, submission readiness | C11 delivery portion; C5.6.6 | Make the product understandable and reviewable; satisfy independently confirmed rules. |
| P1 | Minimal budgeting | C6 | Admit only after mandatory P0 readiness, within the shared 2-hour optional cap and Section 9 cutoff; never consume protected reserves. |
| P2 | Financial goals; receipt/invoice OCR; at most one optional hackathon partner integration | C7; C10; optional supporting work | Goals/OCR remain deferred for this submission. A partner may use the single optional slot only under the same P0 readiness rule; default is defer. |

## 5. Measurable P0 Acceptance Criteria

All criteria below are **planned and not yet verified**. Future authorized verification must record candidate revision, environment, synthetic fixtures, exact operations, expected/actual outcomes, and evidence references in private Lyra reports. Protect secrets in evidence. A blocked dependency remains blocked; a date is not proof of completion.

### P0-1 — Persistence and authentication

Dependencies: separately authorized isolated SQL Server environment, suitable schema/setup permissions, application/API execution permission, two synthetic users, agreed financial expectations, and disposition of the obligation-direction finding.

Observable acceptance:

- An authenticated user creates an account, a classified movement, and an obligation/partial settlement through the actual API; subsequent reads and an authorized application restart preserve the records and expected decimal amounts.
- A same-currency transfer produces both legs once in a successful transaction; a controlled failure persists neither leg. A settlement boundary/concurrent-write case prevents over-settlement under the approved rule.
- Two synthetic owners cannot read, modify, settle, transfer through, or aggregate each other's records. Missing/invalid authentication is denied by the server; owner identity comes from the authenticated principal rather than a caller-selected owner.
- Validate the configured Identity API in the selected bearer login mode: valid synthetic credentials yield a usable access credential and an authenticated finance request succeeds; incorrect credentials do not authenticate. Missing, malformed, and expired access credentials yield HTTP 401 on protected finance requests without financial side effects. Record effective expiry settings and sanitized status/timing evidence without storing credentials. Do not assume JWT format or a token lifetime.
- Invalid amounts, currencies, references, and incompatible directions are rejected without partial writes; exposed errors contain no credentials or internal exception details.
- Demonstrated date filters, ordering, pagination/counts, and per-currency summaries match a fixed synthetic ledger. Transfers and settlements are not double-counted as income/expense; unclassified movements are not presented as confirmed income/expense.
- Capture actual integration/API results and affected regression evidence. Separately authorize a current unit-suite run before release; the historical 352 count is neither a new result nor a required fixed count after future changes.

### P0-2 — Responsive Blazor application

Dependencies: C8 implementation authorization, stable exposed contracts, P0-1 identity and persistence, and approved synthetic demo fixtures.

Observable acceptance:

- On representative widths of 360 px and 1280 px, a reviewer can sign in, create/select an account, record a movement, view history and a meaningful summary, and inspect an obligation/settlement without horizontal page overflow or inaccessible primary controls.
- Proposed minimal session policy: keep the access credential in client memory only. Successful login enables the protected journey; invalid login displays a safe error without authenticated UI state. Browser reload/deep link starts signed out and requests login before protected data access; it does not silently restore a session. This policy is proposed for future authorized implementation, not observed frontend behavior.
- When the access credential expires or any protected request returns 401, clear client authentication and private cached views, stop protected requests, and ask the user to log in again. Do not automatically replay a financial write after reauthentication. A denied operation cannot appear successful.
- Distinguish browser reload from token renewal: automatic token refresh is outside this minimal scope. The presence/usability of refresh credentials is unverified; neither their existence nor a working renewal endpoint is assumed. If the selected Identity response includes renewal material, the minimal client must not retain or use it. Any renewal or persistent-session design requires a separate Owner decision and authorized validation.
- Logout clears the client credential, Authorization header, private cached data, and pending authenticated work; subsequent UI requests send no prior credential, protected routes require login, and browser back/reload reveals no previous owner's data. Local logout does not establish server revocation: verify and privately record whether a previously issued credential remains valid until expiry. Make no immediate-revocation claim; Owner must accept the verified residual lifetime or separately authorize a stronger design before Gate A.
- Forms expose validation messages; loading, empty, success, and failure states are distinguishable. Disable repeat transfer submission while a request is pending. A timeout, disconnected response, or ambiguous server failure means **outcome unknown**, not failed: block retry, show the uncertainty, and reconcile against authoritative same-owner transfer/history and account balances after reauthentication if needed. Reload/logout/re-login must not bypass this reconciliation requirement; no access credential may be retained to implement that guard. Match available group/leg identifiers, accounts, currency, amount, and time; an incomplete or ambiguous match requires authorized operator investigation. Retry only after authoritative evidence establishes no transfer committed and a fresh explicit user confirmation. If committed, show the existing transfer without reposting. Test committed-but-response-lost and no-commit cases, including a reload/re-login; no idempotency key or automatic retry is assumed.
- The primary journey works by keyboard with labeled controls and visible focus; verify in at least one desktop browser and one mobile browser or documented mobile emulation. Record the actual environments and limitations.

### P0-3 — Real contextual Meta AI assistance

Dependencies: independently confirmed competition/provider requirements, separately authorized Meta access and external calls, secure backend credentials, approved model choice/usage budget, P0-1 owner-scoped context, and P0-2 assistant surface. Use only synthetic financial data with models.

Observable acceptance:

- A recorded authorized backend request reaches the real selected Meta service/model and returns a response rendered in the application. Capture model identity and sanitized request/result evidence privately; mocks do not satisfy this criterion.
- Run the reproducible synthetic grounding cases below through the same candidate/model and record the exact prompt, backend context snapshot, deterministic expected facts, and sanitized response. Evaluate facts and isolation, not exact prose. A changed context must produce the changed required facts, not merely different wording.
- Displayed monetary facts match deterministic C# calculations and remain separated by currency. The model does not calculate authoritative balances, query the database directly, or receive credentials.
- Each answer distinguishes **recorded facts** (scope, period, currency, and available evidence) from **recommendations** (suggested future actions, not transactions or guaranteed outcomes). It must not present suggested savings as recorded savings, invent merchant detail, infer confirmed income from cash direction alone, or treat missing data as a verified zero balance.
- Empty context and a provider failure/timeout yield clear states without invented figures, leaked exceptions, or duplicate financial writes. Assistance is labeled as AI-generated and unverified; any proposed financial record requires validated output and explicit user confirmation before persistence.

Reproducible fixture: isolated synthetic owners A and B; period **October 1–31, 2026**, currency **USD**. Owner A has an asset account opening at USD 1,000, a second asset account opening at zero, classified salary income USD 500 on October 5, classified groceries expense USD 120 on October 10, and classified transport expense USD 30 on October 12, all on the first account. On October 15, a same-owner USD 200 transfer moves from the first account to the second, excluded from income/expense. Owner B has only an unrelated classified expense USD 999 and must never appear in A's context. Do not include unresolved obligation totals. Backend C# produces expected facts before constructing model context.

| Case and exact prompt | Required observable facts/behavior |
| --- | --- |
| A1: "Summarize my recorded income, expenses, and account balances for October 2026 in USD." | Income 500; expenses 150 (groceries 120, transport 30); net recorded income less expense 350; balances 1,150 and 200, combined 1,350. Transfer 200 is not income/expense; B's 999 never appears. |
| A2: "Which recorded expense category was largest in October 2026, and what could I try next month? Separate facts from recommendations." | Groceries 120 is largest, 80% of recorded expenses. A proposed reduction is labeled hypothetical; no future savings or financial record is asserted as achieved. |
| A3: Add one classified groceries expense USD 80 on October 20 on A's first account; repeat A1 and A2 verbatim. | Expenses 230; groceries 200; net 270; balances 1,070 and 200, combined 1,270. Groceries remains largest; any share stated agrees with 200/230 rounded to the precision shown. Old totals must not persist. |
| A4: With a third synthetic owner having no records, ask A1; separately simulate provider timeout under authorized validation. | Explicitly state no recorded context and no established balances; timeout displays a recoverable provider failure without fabricated financial facts or writes. |

The fixture is a planned verification scenario, not data created or sent to a model by this task. Grounding fails on any required wrong total, owner leakage, transfer double-counting, unsupported fact, or recommendation presented as a recorded result.

### P0-4 — Public deployment, domain, DNS, and HTTPS

Dependencies: Gate A passed before public application exposure; Project Owner approval of provider, cost, region, public exposure, Namecheap purchase and domain choice; confirmed domain availability/terms; separately authorized hosting, database, secrets, deployment, DNS, and certificate operations. No availability or benefit is assumed.

Observable acceptance:

- The approved domain is acquired through Namecheap, with ownership evidence retained privately; renewal cost and account responsibility are recorded without credentials.
- Approved DNS resolves the domain to the deployed product. From an external client, the HTTPS URL loads the Blazor app and reaches the intended API; HTTP redirects to HTTPS and a trusted certificate matches the hostname without browser warnings.
- Sign-in and the P0-2 journey operate against the deployed SQL-backed service; browser reload/deep links follow the explicit signed-out/re-login policy in P0-2, and a controlled application restart retains persisted synthetic records.
- Private secrets remain server-side; no database is directly exposed to the browser or model. Public error responses reveal no secrets. Capture deployment revision, smoke evidence, certificate status, and a privately documented recovery/rollback procedure.
- A separately approved temporary hosting URL may support rehearsals if domain/DNS lead time slips. It does **not** complete the mandatory domain/DNS/HTTPS deliverable; record any unmet criterion and seek an explicit Project Owner decision.

### P0-5 — Documentation, demonstration, video, and submission

Dependencies: confirmed official rules/deadline/time zone, P0-1–P0-4 evidence, approved public content and synthetic data, technical/editorial review, and explicit publication/submission authorization.

Observable acceptance:

- An approved public quickstart explains access/sign-in, recording financial activity, understanding summaries, using AI, data limitations, and known restrictions. A new reviewer completes the documented journey without private Lyra access.
- A reproducible demo script shows problem, financial workflow, real Meta assistance, benefit, and limitations using only synthetic data. A full rehearsal succeeds on the deployed candidate and evidence identifies its revision.
- A playable submission video shows the working product and real AI contribution. Duration, format, hosting visibility, required disclosures, and links meet independently confirmed organizer requirements; no internal engineering material or secrets appear.
- A submission checklist maps every confirmed rule to an artifact or explicit disposition, including Indie Developer eligibility, required technology, product URL, video, and any required repository access/license information. No assumption is recorded as confirmation.
- The Project Owner reviews the candidate and submission packet before release/submission. Authorized submission is complete only with a recorded receipt/status and timestamp before the confirmed deadline. Until then, distinguish readiness from actual submission.

### Mandatory safety, readiness, and approval gates

These are checkpoints within C5.6.3–C5.6.6 and C11, not additional development phases. Every check below is pending; the roadmap itself grants no deployment, access change, publication, or submission authorization.

- **Gate A — before public application exposure:** In a separately authorized isolated or access-restricted environment, pass P0-1 authentication (including expiry), two-owner isolation, decimal persistence/restart, settlement bounds, transfer atomicity/rollback, uncertain-outcome reconciliation, financial summary integrity, client session handling, and protection of server credentials. Resolve the obligation-direction rule for any exposed affected workflow, or obtain an explicit scope revision that removes it and records unmet original criteria. Record evidence against the exact candidate, effective credential lifetime and logout limitation, Owner risk decisions, and separate public-deployment authorization. Restricted hosting preparation, domain acquisition, DNS and certificate preparation may occur earlier only under their own authorization and without opening an unsafe application to the public.
- **NO-GO:** Unsafe or unverified authentication, owner isolation, financial persistence, or transfer integrity blocks public exposure and release. Failed checks, missing evidence, unknown transfer reconciliation, or unresolved financial correctness in an exposed workflow cannot be waived by the schedule, optional-work admission, a temporary URL, or final submission approval. Any subsequent relevant change invalidates affected Gate A evidence until independently reverified. A discovered public safety failure requires separately authorized containment/recovery; this document authorizes no such operation.
- **Exposure control:** Gate A is necessary but not sufficient authorization to open hosting access. Before any public sign-in or financial traffic, the approved host must also have trusted HTTPS and safe secret configuration. Restricted host/certificate verification can precede opening; Gate B confirms the final public domain and full deployed journey afterward. Do not temporarily expose credential-bearing flows over HTTP to meet a date.
- **Gate B — mandatory P0 readiness:** After Gate A and separately authorized public deployment, complete deployed HTTPS/domain/API/end-to-end smoke checks, responsive and AI cases, current-revision regressions, and every P0-1–P0-4 criterion. Complete P0-5 public content review/publication as authorized, successful deployed rehearsal, final playable video, and a rule-mapped submission packet ready for upload. Record all evidence and remaining non-blocking limitations. Only actual submission authorization and receipt may remain outstanding; those cannot be claimed as completed. This is also the minimum entry condition for optional work.
- **Gate C — final submission approval:** Owner confirms the Gate B candidate/artifact revisions, confirmed eligibility/cutoff, accessible product/video links, disclosure of limitations, and explicit submission authorization. Recheck changed artifacts and affected checks. Submit only within that authorization and retain receipt/status and timestamp. Gate C does not replace Gate A safety evidence or authorize deployment retroactively.

## 6. Execution Schedule: October 30–November 4, 2026

Proposed dates use **America/Tegucigalpa (UTC−06:00)**. Convert the independently confirmed organizer deadline into this time zone before scheduling the final submission; if earlier than this target, move freeze and delivery forward. The schedule allocates future work and grants no execution permission.

**Estimated capacity, not confirmed availability:** Plan against two separately authorized work streams (implementation and independent verification/operations), each with up to 6 focused hours per day on October 31–November 3, plus 4 aggregate coordination/planning hours on October 30 and 6 aggregate delivery hours on November 4: **58 aggregate effort-hours**. Agent availability, Owner review time, environment access, and the ability to overlap work are unconfirmed. Effort-hours describe reserved work capacity, not guaranteed output; provider/DNS waits and Owner approval latency are additional elapsed time. Owner must confirm actual assignments/availability on October 30. With one available stream or less capacity, replan immediately; do not assume agents or overtime will fill the gap.

Allocate that estimate as 4 hours planning; 24 hours core implementation/integration and early checks on October 31–November 1; 6 hours completion plus 6 hours dedicated validation on November 2; 4 hours release validation plus 2 hours defect/recovery contingency and 6 hours presentation on November 3; and 6 hours submission/link checking/recovery on November 4. No optional effort is included. The protected validation, defect/recovery, presentation, and submission reserves total **24 hours** and cannot be borrowed for features. This is a bounded planning estimate, not evidence that the broad remaining work fits; missed checkpoints trigger scope decisions or a reported delivery risk.

| Date | Planned work | Daily exit/dependency |
| --- | --- | --- |
| October 30 | Up to 4 aggregate hours: review roadmap; confirm official rules and actual capacity; decide demo boundary, obligation rule, provider/hosting budgets, and domain candidates. Prepare scoped authorizations. Start public guide outline and evidence checklist. | By end of day, Owner accepts capacity estimate or replans. Unconfirmed rules/access/availability are explicit blockers to reliable scheduling. Begin approved acquisition/restricted hosting preparation early. |
| October 31 | Up to 12 aggregate hours: SQL/authentication checks and approved fixes; C8 sign-in/account/movement UI; C9 real-provider access spike; restricted C11 deployment preparation. | By 18:00, require a working authorized provider request, persistence/authentication evidence in progress, stable UI contracts, and approved domain/hosting path. Missing provider access or environment permissions triggers immediate Owner escalation; simplify presentation breadth, never substitute mocked AI for P0-3. |
| November 1 | Up to 12 aggregate hours: complete core C8/C9 synthetic journey, uncertainty/session handling, essential SQL/security checks; draft quickstart and demo script alongside proven behavior. Rehearse privately on restricted hosting. | By 18:00, require a reproducible end-to-end slice and Gate A evidence ready for review. Otherwise defer all optional work, freeze further feature breadth, and identify precise P0 blockers/corrective authorizations. Do not open the application publicly with Gate A pending. |
| November 2 | Up to 6 aggregate completion hours, then 6 protected validation hours: finish required states/AI cases; Gate A decision before any separately authorized public exposure; domain/DNS/HTTPS and deployed end-to-end checks. Have quickstart/script ready for review. | Optional admission cutoff is 12:00 and requires the much stricter Section 9 readiness rule; the normal schedule will therefore defer optional work. By 18:00 freeze feature scope and candidate; require P0-1–P0-4 evidence complete. Any gap consumes only the recovery reserve and triggers an Owner delivery-risk decision, not a weaker pass. |
| November 3 | Protect 4 hours for release regression/security/deployed smoke validation and 2 hours for defect/recovery contingency early in the day. Protect a separate subsequent 6 hours for successful rehearsal, public quickstart finalization/review, recording/editing/playback/link checks, and packet assembly. No new features. | Target validation/recovery complete by 12:00 and Gate B by 18:00. Evidence must precede claims in the recording. If a fix changes demonstrated behavior, reverify and re-record affected portions. If reserves are insufficient, report readiness blocked; do not take time from mandatory checks or fabricate a demo. |
| November 4 | Protect 6 aggregate hours for Gate C, final link/access checks, upload/submission and recovery; no feature work. Plan first submission attempt at least 4 elapsed hours before the independently confirmed cutoff, retaining remaining time for failed uploads or access correction. | Owner confirms availability and cutoff conversion before relying on this window. If the confirmed deadline makes the buffer impossible, move packet completion and first attempt earlier, including November 3. Retain receipt/status and timestamp; an unsubmitted packet is not completion. |

Work may overlap only where capacity and scoped authorization actually exist: C8 can use agreed contracts while C5.6.3/C5.6.4 proceed; C9 can establish provider access while UI work continues; C11 preparation starts early. Proposed scope reductions remove optional work, extra screens/polish, extra AI prompt types beyond the acceptance cases, or demonstration paths beyond the mandatory journey. They do not remove any of the five P0 deliverables, reduce safety evidence, or silently exclude the obligation workflow. If mandatory scope cannot fit, the Owner must explicitly replan; the roadmap reports the unmet criteria and delivery risk. There is no automatic extension of the November 4 target.

## 7. Public Documentation and Presentation

**Internal engineering documentation stays in private Lyra:** requirements/decisions, implementation handoffs, verification reports, defect evidence, environment and recovery runbooks, source provenance, and unresolved risks. Git remains authoritative for source and version-controlled configuration. This roadmap is an internal repository planning artifact, not permission to publish internal documents or Lyra links.

**Public usability documentation serves product users and judges:** a short product introduction, access instructions, synthetic-data walkthrough, account/movement/obligation explanations, summary semantics, AI capabilities/limitations, and troubleshooting for visible product states. Publish only reviewed, explicitly approved content to a selected public surface; future documentation paths and README changes need separate write authorization.

The demonstration should tell one clear story: a user sees where money goes and receives grounded help to choose a next action. Show trustworthy financial facts first, then how Meta AI explains them. Distinguish live demonstration from any prerecorded segment. Exclude goals, OCR, budgets, and partner claims unless actually delivered and evidenced. Keep internal security details, credentials, personal/financial data, and private engineering links out of public artifacts.

## 8. Domain Acquisition and Deployment

Namecheap acquisition is a planned mandatory operation, not completed work. No domain candidate, availability, price, promotion, renewal term, or hackathon benefit is verified. The Project Owner must approve the exact domain, total cost, registrant/account responsibility, and purchase before an authorized operator acts. Confirm current availability and terms immediately before purchase; prepare alternate names without representing them as available.

Select a minimal hosting approach compatible with the existing Blazor/API/SQL Server architecture. Record cost, region, secret handling, persistence, certificate automation, operational responsibility, and rollback decisions privately. Docker remains the documented deployment direction; no new hosting platform, package, or architecture change is implicitly approved.

Plan the order: approve hosting/domain decisions and separately authorize acquisition and resources; prepare and validate the candidate on restricted hosting; complete Gate A and obtain explicit public-exposure approval; deploy/open the approved candidate publicly; complete DNS/HTTPS and authorized external smoke checks; record revision and recovery evidence for Gate B; obtain Gate C submission approval separately. DNS/certificate preparation may run earlier under scoped authorization without exposing the application. DNS propagation, certificate issuance, service quotas, and provider activation can delay delivery, so address them before final recording day. Neither this sequence nor a passed documentation review authorizes deployment.

Production changes, migrations, database records, credential provisioning, DNS, domain purchase, runtime checks, and public release each require authorization covering the affected resources and operations. None are performed by this documentation task.

## 9. Optional Partner Integrations

At most **one** optional hackathon partner integration may be considered; default disposition is P2/deferred. No partner participation, eligible offer, credit, domain benefit, service availability, award advantage, or integration requirement has been independently confirmed. A mandatory requirement discovered in official rules must return to the Project Owner for scope review.

### Shared optional-work admission and cancellation rule

Default: **defer**. Goals (C7) and OCR (C10) remain outside this submission's implementation scope. At most one optional item may be admitted: minimal budgeting (C6) **or** one partner integration. Admission is possible only if **all** of these conditions are evidenced before **12:00 on November 2**:

- Gate A and Gate B are passed: all P0-1–P0-4 acceptance evidence is complete, all mandatory public documentation and the final video are reviewed/ready, a full rehearsal has succeeded, and the packet is ready for submission. Only Gate C authorization and the actual receipt may remain pending. Expected readiness or a first AI response is insufficient.
- Owner explicitly authorizes the exact item, affected resources/operations, and a measurable demonstration benefit. For budgeting, the benefit is one period/category limit compared against deterministic classified expense, with remaining amount checked against a fixed synthetic example (limit USD 200, expense USD 150, remainder USD 50). No forecasting, goals, or automatic financial writes are included. For a partner, verify official participation, eligibility, terms, cost/privacy implications, and an exact user-visible before/after demonstration; no unverified benefit justifies admission.
- Actual additional capacity is confirmed **outside** the 24 protected hours. The shared fixed cap is **2 aggregate effort-hours total**, including investigation, implementation, independent verification, all affected P0 regressions, documentation/video adjustments, and any recovery. Concurrent work counts cumulatively, not as elapsed wall time. The item must finish and be reverified by **18:00 on November 2**. There is no automatic cap extension and no additional development phase.
- Preserve the last Gate B-approved candidate and artifacts as a recoverable baseline under separately authorized source/release controls. Any changed candidate requires affected Gate A/B evidence renewed before use. No change can be included merely because the baseline previously passed.

Cancel admission immediately on a failed P0 check, missed readiness/cutoff, unconfirmed external dependency, loss of protected reserve, or an estimate that all remaining optional verification/recovery will exceed the unused cap. Stop feature work early enough to restore and validate the approved baseline within the same cap; separately authorized operators perform any necessary recovery, never automatic destructive Git operations. If safe restoration/validation cannot finish within the reserve, keep the changed candidate out of release and report the blocker. Exclude optional claims from the video/packet when the item is dropped. The normal schedule reaches Gate B on November 3, so **no optional item is expected to qualify**.

Namecheap domain acquisition remains P0 regardless of whether any partner benefit exists. Meta AI remains mandatory; an optional service cannot substitute for real Meta integration.

## 10. Risks, Dependencies, Decisions, and Deferred Work

| Risk/dependency | Known state and required disposition |
| --- | --- |
| Obligation-direction aggregation | **Unresolved.** Historical finding: `ListObligationsAsync` sums linked amounts regardless of direction. Payable 100 with `Pagar` 30 + 20 and `Cobrar` 10 yields outstanding 40 under characterized behavior. This is not an approved financial rule. Owner must decide intended handling; separately authorize any correction and independent regression. Until resolved, affected totals must not be presented as authoritative or sent as grounded AI facts. If the feature is excluded, record the exclusion and its effect on P0 acceptance; do not call the full planned journey complete. |
| Unit evidence versus release evidence | Historical 352/352 proves only its defined unit scope. Obtain authorized current-revision SQL/API/regression and deployment evidence; retain actual environment, source revision, commands, results, defects, and unexecuted cases in Lyra. |
| Authentication/ownership | Identity/API source existence does not establish deployed security. Cross-owner disclosure or write access blocks release; two-owner server-side verification is required. |
| Provider access and AI safety | Meta access, quotas, latency, cost, model eligibility, and actual integration are unverified. Establish feasibility early; never send real financial data, expose secrets, permit direct model SQL access, or trust generated figures as authoritative. |
| Financial semantics | Use deterministic C# `decimal` calculations; separate cash direction from economic classification, account effects, and settlement state. Avoid transfer/settlement double-counting and mixed-currency totals. |
| Transfer retries and transaction guarantees | Architecture records non-idempotent transfer POST behavior. Prevent blind retries and verify real atomicity/rollback; do not claim fake-store evidence proves it. |
| Deadline/eligibility and public artifacts | Official category, cutoff/time zone, judging/submission rules, video constraints, and required public access remain unverified. Owner-confirmed official evidence is needed before packet approval. |
| Domain, hosting, DNS, HTTPS | Availability, cost, ownership, service compatibility, certificate timing, and external reachability are unverified. Approvals and provider lead times are critical-path dependencies; a temporary URL does not close the domain criterion. |
| Compressed schedule | The 58-hour capacity model is estimated, not confirmed availability or a feasibility guarantee. Confirm assignments/Owner review windows; protect the 24-hour validation/recovery/presentation/submission reserve, including its 2-hour defect contingency. Missed checkpoints require scope decisions. Dates never override authorization or failed acceptance. |
| Publication confidentiality | Public user materials require technical/editorial review and explicit publication approval. Keep this roadmap and private Lyra engineering evidence internal unless separately approved. |

Project Owner decisions needed: confirm official competition rules/deadline; confirm capacity and protected work/review windows; approve the obligation-direction rule and resulting scope; assign scoped implementation/verification/operational tasks; accept or revise the proposed memory-only bearer session policy and verified post-logout credential lifetime; select Meta model/provider budget and safe synthetic context; approve hosting and Namecheap domain/cost; admit optional work only if Section 9 qualifies by November 2 at 12:00 (otherwise defer); approve public content, public exposure after Gate A, and final submission after Gate B. Historical milestone mappings remain subject to Owner confirmation. ChatGPT may coordinate these decisions but cannot authorize them for the Owner. Muse Code implements authorized changes; Codex independently verifies and performs authorized operations, without automatically modifying the implementation.

Deferred work: financial goals; receipt/invoice OCR; optional partner work unless admitted; budgeting unless its condition is met; broad SQL/provider/cancellation and load-test matrices beyond essential release cases; broader operational hardening; statement import staging, recurrence, interest, and cross-currency transfers. Deferral does not waive release-blocking security, financial integrity, or confirmed competition requirements. Track remaining C5.6 cases against their original identifiers in future authorized reports.

### Document review and execution record

This roadmap was prepared from read-only local branch/status/log/commit-stat/tracked-file inspection, README and architecture inspection, and read-only Lyra policy/technical/report retrieval. Review checks cover all ten required sections, identifiers C5.6.0–C5.6.6 and C6–C11, historical evidence qualifiers, P0 dependencies and observable outcomes, all six schedule dates, and authorization/publication boundaries. No API or data-contract change is made.

Revision-task evidence: read-only branch/status and authentication-source inspection; edits limited to `docs/ROADMAP.md`; complete revised-document readback and consistency/whitespace review. Changes resolve schedule assumptions/reserves, checkpoints, pre-publication NO-GO versus final approval, observable session/transfer behavior, reproducible AI fixtures, historical-mapping uncertainty, and optional-work caps. Source baseline remains local HEAD `6cf38a76d33116f1920046ed00bfe68e65f97a30`; the roadmap remains an uncommitted working-tree document. No Lyra access was performed for this revision; policy text and historical evidence had already been retrieved in the preceding tasks. Documentary consistency review does not verify implementation, runtime safety, external rules, or actual capacity.

Documentation creation is distinct from product completion. Runtime validation for this task: **NOT RUN**. No builds, tests, restores, migrations, application starts, database access, application/provider API calls, infrastructure operations, purchases, DNS changes, Lyra writes, publication, staging, commits, or pushes were executed. Read-only Lyra documentation access supplied governance and historical evidence only. External competition, partner, domain, and deployment claims remain unverified. Await Project Owner review; this task stops after the document and final status report.
