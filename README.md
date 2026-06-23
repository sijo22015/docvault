# DocVault — Departmental Document Management System

A role-based document management system for colleges and companies to manage departmental documentation across financial years, with long-term auditability and tamper verification.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10.0 Web API |
| ORM | Entity Framework Core 10 |
| Micro-ORM | Dapper (reports, dashboards) |
| Auth | JWT (access + refresh) + ASP.NET Identity |
| API Docs | Swagger / Swashbuckle |
| Logging | Serilog → file + console |
| Validation | FluentValidation |
| File Storage | Local disk (`/storage/{fy}/{dept}/{userId}/`) |
| Frontend | React 19 + Vite + TypeScript |
| State | Redux Toolkit + RTK Query |
| Styling | Tailwind CSS v4 + MUI v9 |
| Charts | Recharts |
| Database | PostgreSQL 16 |

## Quick Start (Docker)

**Prerequisites:** Docker Desktop

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| Web UI | http://localhost:3000 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| PostgreSQL | localhost:5432 |

Default admin credentials: `admin@docvault.local` / `Admin@12345`

## Local Development

### Prerequisites
- .NET 10 SDK
- Node.js 22+
- PostgreSQL 16

### Backend

1. Create the database and run the schema:
   ```bash
   psql -U postgres -c "CREATE DATABASE docvault;"
   psql -U postgres -d docvault -f db/01_schema.sql
   ```

2. Update connection string in `src/DocVault.Api/appsettings.json` if needed.

3. Run the API:
   ```bash
   cd src/DocVault.Api
   dotnet run
   ```
   API starts at http://localhost:5080. Swagger at http://localhost:5080/swagger.

### Frontend

```bash
cd frontend
npm install
npm run dev
```
UI starts at http://localhost:5173.

## Project Structure

```
DocVault/
├── db/
│   └── 01_schema.sql          # PostgreSQL DDL + seed data
├── src/
│   ├── DocVault.Api/           # Controllers, Program.cs, Middleware
│   ├── DocVault.Application/   # DTOs, Validators, Service Interfaces
│   ├── DocVault.Domain/        # Entities, Domain Interfaces
│   └── DocVault.Infrastructure/# EF Core DbContext, Services, Dapper queries
├── tests/
│   └── DocVault.Tests/
├── frontend/
│   └── src/
│       ├── app/                # Redux store, RTK Query base
│       ├── features/
│       │   ├── auth/           # Login, Register, auth slice
│       │   ├── admin/          # Dashboard, Users, Documents, Analytics, Logs
│       │   └── user/           # Dashboard, Documents, Upload, Profile
│       └── shared/             # Layouts, ProtectedRoute, API slices
├── docker-compose.yml
└── README.md
```

## Roles & Flows

### User Flow
1. Register → status = `PENDING`
2. Admin approves → status = `APPROVED`
3. User logs in, uploads documents (PDF/DOC/DOCX/TXT, max 25 MB)
4. Documents are SHA-256 hashed, stored per FY/dept/userId
5. Soft delete with 30-day restore window

### Admin Flow
1. Approve / revoke users
2. Browse all documents with advanced filters
3. View analytics by financial year (department progress, monthly trend, top contributors)
4. Run integrity verification (recomputes SHA-256, flags mismatches)
5. Lock a financial year (prevents new uploads/deletes)
6. Export a financial year as a ZIP with manifest CSV

## API Reference

Base URL: `/api/v1`

| Group | Key Endpoints |
|---|---|
| Auth | POST /auth/register, /auth/login, /auth/refresh, /auth/logout, /auth/forgot-password, /auth/reset-password |
| Documents | POST /documents, GET /documents, GET /documents/{id}/download, DELETE /documents/{id} |
| Admin Users | GET /admin/users, POST /admin/users/{id}/approve, /admin/users/{id}/revoke |
| Admin Docs | GET /admin/documents, GET /admin/documents/verify-integrity |
| Analytics | GET /admin/dashboard/summary, GET /admin/dashboard/analytics |
| Activity | GET /admin/activity-logs, POST /admin/activity-logs/delete, POST /admin/activity-logs/delete-selected |
| Export | GET /admin/export/fy/{fyId} |
| Reference | GET /reference/departments, /reference/document-types, /reference/financial-years |

Full Swagger docs available at `/swagger` when the API is running.

## Security Notes

- Passwords: min 10 chars, requires upper/lower/digit/special
- JWT: HS256, 15-min access tokens, 7-day refresh tokens with rotation
- Rate limiting: 5 login attempts/min, 60 API calls/min
- File upload: magic-byte validation + SHA-256 integrity hash
- Activity logs: admin can delete all, selected, or logs within a date range (user-defined trigger disabled at startup to allow admin deletes)
- Financial year lock: prevents writes on closed years

## Configuration

Key settings in `src/DocVault.Api/appsettings.json`:

```json
{
  "Jwt": { "SigningKey": "change-me-in-production" },
  "Storage": { "RootPath": "D:\\DocVault\\storage", "MaxFileSizeMB": "25" },
  "Seed": { "AdminEmail": "admin@docvault.local", "AdminPassword": "Admin@12345" }
}
```

Frontend environment variable (`.env` in `frontend/`):

```env
VITE_GROQ_API_KEY=your_groq_api_key_here   # free key — https://console.groq.com
```
