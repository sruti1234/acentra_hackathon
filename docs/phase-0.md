# Phase 0 — Project Initialization

## Scope

Included:
- ASP.NET Core 8 Web API with controllers, Swagger, DI, CORS
- EF Core + PostgreSQL registered (`AppDbContext`), no tables
- `GET /api/health` → `{ "status": "API is running" }`
- `GET /api/health/database` → checks PostgreSQL connectivity
- React + Vite page with a **Check API** button

Not included (later phases): tenant middleware, global query filters, inventory CRUD, JWT auth, AWS S3.

## Ports

| App      | URL |
|----------|-----|
| API      | http://localhost:5000 (set in `Properties/launchSettings.json`) |
| Swagger  | http://localhost:5000/swagger |
| React    | http://localhost:5173 (fixed in `vite.config.js`) |

If you change either port, update `Cors:AllowedOrigins` in `appsettings.json` and/or `VITE_API_BASE_URL` in the frontend `.env`.

## Test checklist

- [ ] `dotnet build` in `backend/` succeeds with no errors
- [ ] `dotnet run --launch-profile http` starts the API on port 5000
- [ ] http://localhost:5000/swagger opens and lists both Health endpoints
- [ ] "Try it out" on `GET /api/health` returns 200 and `{"status":"API is running"}`
- [ ] `GET /api/health/database` returns `{"database":"Connected"}` with PostgreSQL running
- [ ] Stop PostgreSQL (or use a wrong password) → `/api/health/database` returns 503 `Not Connected`
- [ ] `npm run dev` starts React on port 5173
- [ ] Page shows the title and **Backend API: Connected** on load
- [ ] Clicking **Check API** shows the JSON response
- [ ] Stop the backend, click **Check API** → **Not Connected** with an error message
- [ ] Start the backend again, click **Check API** → back to **Connected**
- [ ] Browser DevTools console shows no CORS errors
