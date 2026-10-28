
# Xigenia Architecture

## Stack
- Runtime: .NET 10 
Microsoft.AspNetCore.App 10.0.13 [/var/snap/dotnet/common/dotnet/shared/Microsoft.AspNetCore.App]
Microsoft.NETCore.App 10.0.13 [/var/snap/dotnet/common/dotnet/shared/Microsoft.NETCore.App]
SDK 10.0.113 [/var/snap/dotnet/common/dotnet/sdk]
- Backend: ASP.NET Core
- Frontend: Blazor WebAssembly
- Persistence: Entity Framework Core and SQL Server
- Authentication: ASP.NET Core Identity
- AI integration: Meta Model API
- Deployment: Docker

## Existing Projects
Document the actual projects and their responsibilities here.

## Main Boundaries
- The frontend calls the backend API.
- Business rules reside in backend/application code.
- SQL Server is accessed through the backend.
- External AI calls are orchestrated by the backend.
- The frontend never receives private API credentials.

## Important Decisions
- Obligations represent amounts payable (`PorPagar`) or receivable (`PorCobrar`).
- MoneyMovement directions (`Pagar`/`Cobrar`) represent cash leaving or entering.
- Cash direction alone does not determine income or expense; linked settlements are never re-recognized.
- Financial accounts and economic classification (income, expense, settlement, transfer) arrive incrementally.
- No available-balance reporting is permitted before financial accounts exist.

## Known Limitations
Record incomplete functionality and technical constraints here.

## Financial Accounts Architecture (proposed, pre-implementation)

### Decisions
- `FinancialAccount` is an owner-scoped store of value: identity, `OwnerId`, user label, type (`Cash`, `BankAccount`, `DigitalWallet`, `CreditCard`), single currency, opening balance, active flag, creation timestamp. Multi-currency holdings are separate account records.
- Asset accounts (`Cash`, `BankAccount`, `DigitalWallet`) hold funds; balance is computed as opening balance plus inflows minus outflows. The MVP does not enforce non-negativity; overdraft policy is deferred.
- `CreditCard` models a credit line, never cash: credit limit, statement closing day, and payment due day. Outstanding debt is computed (posted purchases minus payments); available credit equals limit minus outstanding debt. Limits are never presented as balances.
- Card purchases increase card debt and recognize the expense once, at purchase time, against the card account. Paying the card bill is a transfer (bank to card) that reduces debt and is never an expense. The alternative pattern (monthly card statement as a `PorPagar` obligation plus linked payments) is allowed, but the same spend must never be recorded under both patterns.
- A transfer is an atomic pair of same-owner, same-currency legs (`Pagar` out of the source plus `Cobrar` into the destination) sharing a transfer group id. Transfers are excluded from income/expense by group membership. Cross-currency transfers are out of scope until FX exists.
- Accounts, operations, and methods are separate concepts: an account stores value, an operation is the economic event, a method is the rail used (cash handover, bank transfer, card swipe). Cardless ATM withdrawal via a bank-generated PIN is a future method on a bank-to-cash transfer; withdrawal PINs are ephemeral bank credentials and are never stored.
- Existing standalone movements and obligation settlements remain valid without accounts. Account linkage arrives as optional references on movements, so history stays untouched and opening balances absorb unlinked history as the reconciliation mechanism.
- Economic classification is independent from cash flow direction and obligation linkage: income and expense are never inferred solely from `Pagar`/`Cobrar` or from whether a movement is linked. Every analysis distinguishes cash flow direction, economic classification (income, expense, transfer, loan principal, liability settlement, refund, adjustment), account balance effects, and obligation settlement status as four separate facts.
- Reference examples: salary received is inflow plus income; a loan received is inflow plus a liability increase, not income; loan principal repayment is outflow plus liability reduction, not ordinary expense; a card purchase recognizes expense and increases card debt; a card payment reduces cash and card debt without re-recognizing the expense; a wallet-to-bank transfer changes account balances with no income or expense.
- For the MVP, classification may remain optional or pending, but reports must never present unclassified movements as confirmed income or expense.
- Credit card purchases and monthly statements must not create duplicate liabilities: a card purchase recorded directly and the same spend recorded via a statement obligation are mutually exclusive representations of one liability.
- A nullable external transaction reference is reserved on movements in the accounts migration to enable future statement matching; everything else import-related stays deferred.
- Balances are always computed with `decimal` arithmetic, never stored as mutable state.

### Statement imports (future pipeline, staged design)
- Upload raw CSV, Excel, or PDF statements; parse into a staged batch of raw lines carrying row hashes and external references.
- Users preview staged lines with AI-suggested categorization explicitly marked unverified; nothing posts without explicit batch or partial-line approval.
- Confirmation is idempotent (unique row hash per account) and auditable (batch id, user, timestamp); duplicates are flagged for reconciliation and never auto-merged. AI extraction and categorization always require user approval before creating financial records.

### Assumptions
- One currency per account; personal-scale volumes (C# aggregation acceptable); single-user ownership with no shared accounts.

### Open questions
- Should the MVP enforce non-negative asset balances, or only report them?
- Is the card-statement-as-obligation pattern needed at all, or is purchase-direct sufficient for all card users?
- Where do uploaded statement files live (database blob reference versus external storage)?

### Risks
- Double-counting if users mix the purchase-direct and statement-as-obligation card patterns; mitigation is documentation plus, later, detection.
- Concurrent transfer-pair creation needs the same serializable-transaction treatment as linked settlements, or partial pairs become possible.
- Opening balances set incorrectly will silently skew every computed balance; the balance endpoint must surface the opening balance and linked-movement count for user verification.

### Implementation slices
- Slice A (accounts MVP): `FinancialAccount` entity with create/list/archive, optional account references on movements, computed-balances endpoint, reserved external reference; one additive migration.
- Slice B: atomic same-currency transfers between same-owner accounts.
- Slice C: credit-card limit/closing/due fields with available-credit computation and documented purchase/payment patterns.
- Slice D (post-MVP): statement import staging pipeline, then AI assist, categories, recurrence, and interest.
