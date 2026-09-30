# Multi-Tenant Inventory Platform

A SaaS inventory platform where several organizations (tenants) share one application and one database, and **no organization can ever read or change another organization's data**.

## How tenant isolation works

```
React ──► JWT (who are you?) ──► X-Tenant-ID header (which organization?)
      ──► TenantMiddleware: tenant exists? active? are YOU a member?  (else 400/403/404)
      ──► TenantContext (scoped, one per request)
      ──► EF Core global query filter: WHERE "TenantId" = current tenant
      ──► PostgreSQL
```

- The `X-Tenant-ID` header is **never trusted on its own**: `TenantMiddleware` checks the `UserTenants` table for the logged-in user on every request.
- `ApplicationDbContext` has a global query filter on `Product`, so `_db.Products.ToListAsync()` only ever returns the current tenant's rows. Controllers never add `.Where(p => p.TenantId == …)` themselves.
- New products get `TenantId` from `TenantContext`, never from the request body. `SaveChanges` also blocks any cross-tenant write or tenant change.
- S3 image keys are built on the server: `tenants/{tenantId}/products/{productId}/{filename}`. The bucket stays private; images are shown through short-lived pre-signed URLs.

## Tech stack

.NET 8 · ASP.NET Core Web API · EF Core 8 + Npgsql · PostgreSQL · JWT bearer auth · Swagger · AWS S3 (AWSSDK.S3) · React + Vite + Axios

## Project structure

```
backend/MultiTenantInventory.Api/
  Controllers/    AuthController, TenantsController, ProductsController, HealthController
  Data/           ApplicationDbContext (global query filter), DbSeeder
  Models/         Tenant, User, UserTenant, Product
  DTOs/           request/response shapes (no TenantId accepted from clients)
  Interfaces/     ITenantContext, ITokenService, IS3Service
  Services/       TenantContext, TokenService, S3Service
  Middleware/     TenantMiddleware, [RequireTenant] attribute
  Configuration/  JWT setup, settings classes, Swagger X-Tenant-ID filter
  Migrations/     InitialCreate, AddUsersAndProducts
frontend/multi-tenant-inventory/src/
  api.js          Axios client (adds Authorization + X-Tenant-ID automatically)
  components/     LoginPage, Dashboard, TenantSelector, SummaryCards, InventoryTable, ProductForm, IsolationCheck
scripts/tenant-isolation-test.ps1
```

## Run it

### Backend
```powershell
cd backend\MultiTenantInventory.Api
# one-time: DB password (skip if already set in Phase 1)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=multitenant_inventory;Username=postgres;Password=YOUR_PASSWORD"
dotnet tool restore
dotnet restore
dotnet build
dotnet ef database update
dotnet run --launch-profile http
```
The Development JWT key is in `appsettings.Development.json` (dev only). For anything else set `Jwt:Key` (32+ chars) via user-secrets or the `Jwt__Key` environment variable.

### Frontend
```powershell
cd frontend\multi-tenant-inventory
npm install
npm run dev
```

## URLs
- React: http://localhost:5173
- Swagger: http://localhost:5000/swagger (click **Authorize**, paste the token; tenant endpoints show an `X-Tenant-ID` box)

## Demo accounts
| Email | Password | Organizations |
|---|---|---|
| demo@example.com | Demo@123 | ABC Corporation, XYZ Technologies, Demo Industries |
| abc@example.com | Abc@123 | ABC Corporation only (used for the 403 test) |

## API
| Method | Path | Auth | X-Tenant-ID |
|---|---|---|---|
| POST | /api/auth/login | – | – |
| GET | /api/tenants/my-tenants | JWT | – |
| GET | /api/tenants/current | JWT | required |
| GET | /api/products | JWT | required |
| GET | /api/products/{id} | JWT | required |
| POST | /api/products | JWT | required |
| PUT | /api/products/{id} | JWT | required |
| DELETE | /api/products/{id} | JWT | required |
| POST | /api/products/{id}/image | JWT | required |
| GET | /api/health | – | – |

Tenant errors: missing/invalid header **400**, unknown tenant **404**, inactive tenant or not a member **403**, no/expired token **401**.

## AWS S3 (optional)
Everything except image upload works without AWS. Configure with user-secrets (never in appsettings.json):
```powershell
cd backend\MultiTenantInventory.Api
dotnet user-secrets set "AWS:BucketName" "your-private-bucket"
dotnet user-secrets set "AWS:Region" "eu-north-1"
dotnet user-secrets set "AWS:S3:AccessKey" "AKIA..."
dotnet user-secrets set "AWS:S3:SecretKey" "..."
```
- **Development:** the S3 client is built with `BasicAWSCredentials` from `AWS:S3:AccessKey` / `AWS:S3:SecretKey`. If they're missing, uploads fail with a clear message (no fallback to EC2 metadata).
- **Production:** leave the keys unset and the SDK uses the default AWS credential chain (e.g. an IAM role). Environment variables `AWS__S3__AccessKey` / `AWS__S3__SecretKey` also work.

The IAM user needs `s3:PutObject`, `s3:GetObject`, `s3:DeleteObject` on `arn:aws:s3:::your-private-bucket/tenants/*`. Keep "Block all public access" ON — images are shown via pre-signed URLs.
Without bucket/region, `POST /api/products/{id}/image` returns **503**.

## Tenant isolation test
With the API running:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\tenant-isolation-test.ps1
```
