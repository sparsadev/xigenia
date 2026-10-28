
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
