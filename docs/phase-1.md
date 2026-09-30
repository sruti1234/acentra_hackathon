# Phase 1 — Database Foundation

## Scope

Included:
- EF Core 8 + Npgsql PostgreSQL provider, `ApplicationDbContext`
- Connection string from configuration (user-secrets / environment variable), no password in `appsettings.json`
- `Tenant` entity and the `InitialCreate` migration
- Development seed data: three tenants, inserted at startup when the table is empty
- `GET /api/tenants` (development/testing only — lists every tenant, no isolation yet)
- `GET /api/health/database` connectivity check (from Phase 0)

Not included (later phases): tenant middleware, tenant context, global query filters, products, JWT/login, S3, React inventory UI.

## Database structure

One table, `Tenants`:

| Column      | PostgreSQL type            | Rules |
|-------------|----------------------------|-------|
| `Id`        | `integer`                  | Primary key, identity (auto-increment) |
| `Name`      | `character varying(200)`   | Required |
| `Subdomain` | `character varying(63)`    | Required, **unique** (`IX_Tenants_Subdomain`) |
| `IsActive`  | `boolean`                  | Required, defaults to `true` in C# |
| `CreatedAt` | `timestamp with time zone` | Required, set to UTC now in C# |

EF Core also creates `__EFMigrationsHistory`, which records applied migrations.

Table and column names are PascalCase, so quote them in SQL: `SELECT * FROM "Tenants";`

### Why seed at startup instead of in the migration?

Seeding with `HasData` inserts explicit IDs (1, 2, 3) without advancing PostgreSQL's identity sequence, so the first tenant created later through the API would fail with a duplicate key. Seeding through `DbSeeder` lets PostgreSQL assign the IDs normally.

## Migration commands

Run from `backend/MultiTenantInventory.Api`:

```bash
dotnet tool restore                 # first time only
dotnet ef database update           # apply migrations
dotnet ef migrations list           # show migrations and whether they're applied
```

For reference, the migration was created with:

```bash
dotnet ef migrations add InitialCreate
```

Start over (drops all data):

```bash
dotnet ef database drop --force
dotnet ef database update
```

## Test checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet ef database update` prints `Done.`
- [ ] `dotnet ef migrations list` shows `20260930064000_InitialCreate` (not marked Pending)
- [ ] In psql/pgAdmin: `\dt` (or the table list) shows `Tenants` and `__EFMigrationsHistory`
- [ ] `dotnet run --launch-profile http` starts; the console shows `Seeded 3 development tenants.` on first run
- [ ] `SELECT * FROM "Tenants";` returns 3 rows (abc, xyz, demo)
- [ ] Restart the API: no duplicate rows are created
- [ ] `GET /api/health/database` returns `{"database":"Connected"}`
- [ ] `GET /api/tenants` returns the 3 tenants
- [ ] Swagger at http://localhost:5000/swagger lists Health and Tenants endpoints
- [ ] Unique subdomain check: `INSERT INTO "Tenants" ("Name","Subdomain","IsActive","CreatedAt") VALUES ('Dup','abc',true,now());` fails with a unique-constraint error
- [ ] React app still shows **Connected** (Phase 0 unaffected)
