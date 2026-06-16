# DocVault Frontend — CLAUDE.md

## Project Overview

**DocVault** is a React + TypeScript document management system with role-based access control (Admin / User). Users upload, manage, and track documents organized by financial year and department. Admins manage users, search across all documents, view analytics, and export/merge PDFs.

- **Dev server**: `npm run dev` → http://localhost:5173
- **Build**: `npm run build` (TypeScript compile + Vite)
- **Backend proxy**: `/api/*` → `https://localhost:44339` (IIS Express, .NET backend, self-signed cert)
- **Base API path**: `/api/v1`
- **Deployed on**: Vercel (SPA rewrite via `vercel.json`)

---

## Tech Stack

| Layer | Library | Version |
|---|---|---|
| Framework | React | 19.x |
| Language | TypeScript | 6.x |
| Build | Vite | 8.x |
| Routing | React Router v7 | 7.x |
| State / Data | Redux Toolkit + RTK Query | 2.x |
| UI components | Custom (ui.tsx) + MUI | 9.x |
| Styling | Tailwind CSS v4 | 4.x |
| Forms | React Hook Form + Zod | 7.x / 4.x |
| Charts | Recharts | 3.x |
| File upload | react-dropzone | 15.x |
| HTTP | axios (not used for RTK — only for manual fetches) | 1.x |

---

## Directory Structure

```
src/
├── app/
│   ├── apiSlice.ts          # RTK Query base API + JWT refresh interceptor
│   └── store.ts             # Redux store (api + auth reducers)
│
├── features/                # Feature modules by domain
│   ├── auth/
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   ├── PendingPage.tsx
│   │   ├── authSlice.ts     # JWT state in Redux + localStorage
│   │   └── authApi.ts       # login, register, logout, refresh endpoints
│   │
│   ├── admin/
│   │   ├── dashboard/AdminDashboard.tsx
│   │   ├── users/AdminUsers.tsx
│   │   ├── documents/AdminDocuments.tsx   # tabs: active/deleted, checkbox select, merge PDF
│   │   ├── analytics/AdminAnalytics.tsx
│   │   └── activity/AdminActivityLogs.tsx # audit trail with IST timestamp fix
│   │
│   └── user/
│       ├── dashboard/UserDashboard.tsx    # KPIs + bar chart for current FY
│       ├── documents/UserDocuments.tsx    # list with search/filter/soft-delete/restore
│       ├── documents/UploadDocument.tsx
│       └── profile/UserProfile.tsx        # avatar upload + mobile/WhatsApp edit
│
├── shared/
│   ├── api/
│   │   ├── documentsApi.ts   # user + admin document CRUD
│   │   ├── adminApi.ts       # users, dashboard summary, analytics
│   │   ├── profileApi.ts     # GET/PUT /me, POST /me/photo
│   │   └── referenceApi.ts   # departments, documentTypes, financialYears
│   │
│   ├── components/
│   │   ├── ui.tsx            # full component library (see below)
│   │   ├── Icons.tsx         # ~30 inline SVG icons
│   │   └── ProtectedRoute.tsx
│   │
│   └── layouts/
│       ├── UserLayout.tsx    # sidebar (sky/blue theme) + header
│       └── AdminLayout.tsx   # sidebar (violet/indigo theme) + header
│
└── App.tsx                   # React Router route definitions
```

---

## Authentication & Auth State

**Redux slice** (`authSlice.ts`) — persisted to `localStorage` as `"auth"`:
```ts
{
  accessToken:  string | null,
  refreshToken: string | null,
  userId:       string | null,
  email:        string | null,
  fullName:     string | null,
  role:         string | null  // "Admin" | "User"
}
```

**Token refresh** (`apiSlice.ts`): A shared `refreshLock` promise prevents duplicate refresh calls when multiple requests 401 simultaneously. On successful refresh, `setCredentials` is dispatched. On failure, `clearCredentials` + redirect to `/login`.

**Route protection**: `ProtectedRoute` reads `auth.role` to guard admin vs user routes.

---

## RTK Query API Slices

All slices inject into the single `apiSlice` from `app/apiSlice.ts`. Tag types: `Document`, `User`, `Notification`, `Profile`.

### `documentsApi.ts`
| Hook | Method | Endpoint |
|---|---|---|
| `useGetMyDocumentsQuery` | GET | `/documents?page&pageSize&financialYearId&documentTypeId&searchTerm` |
| `useUploadDocumentMutation` | POST | `/documents` (FormData) |
| `useDeleteDocumentMutation` | DELETE | `/documents/:id` |
| `useRestoreDocumentMutation` | POST | `/documents/:id/restore` |
| `useAdminSearchDocumentsQuery` | GET | `/admin/documents` (any params) |
| `useAdminRestoreDocumentMutation` | POST | `/admin/documents/:id/restore` |
| `useAdminPurgeDeletedMutation` | DELETE | `/admin/documents/deleted` |

Also: `verifyIntegrity`, `lockFY`, `exportFY` (not exported as hooks — used inline).

### `profileApi.ts`
| Hook | Method | Endpoint |
|---|---|---|
| `useGetMyProfileQuery` | GET | `/me` |
| `useUpdateMyProfileMutation` | PUT | `/me` (body: `MobileNumber`, `WhatsAppNumber`) |
| `useUploadProfilePhotoMutation` | POST | `/me/photo` (FormData with `photo` field) |

Profile data shape: `{ profilePhotoUrl, mobileNumber, whatsAppNumber, ... }` under `data.data`.

`useGetMyProfileQuery` is called with `refetchOnMountOrArgChange: true` in UserLayout, AdminLayout, and UserProfile to ensure fresh data on every mount (avoids stale cache showing blank fields).

### `adminApi.ts`
| Hook | Endpoint |
|---|---|
| `useGetUsersQuery` | `/admin/users` |
| `useApproveUserMutation` | `/admin/users/:id/approve` |
| `useRevokeUserMutation` | `/admin/users/:id/revoke` |
| `useGetDashboardSummaryQuery` | `/admin/dashboard` |
| `useGetAnalyticsQuery` | `/admin/analytics?financialYearId=` |

### `referenceApi.ts`
`useGetDepartmentsQuery`, `useGetDocumentTypesQuery`, `useGetFinancialYearsQuery` — all provide lookup/dropdown data.

---

## Routes (App.tsx)

```
/login, /register, /pending                  (public)

/admin/dashboard   → AdminDashboard           (role: Admin)
/admin/users       → AdminUsers
/admin/documents   → AdminDocuments
/admin/analytics   → AdminAnalytics
/admin/activity    → AdminActivityLogs

/dashboard         → UserDashboard            (role: User)
/documents         → UserDocuments
/documents/upload  → UploadDocument
/profile           → UserProfile

*                  → redirect /login
```

---

## Shared Component Library (`ui.tsx`)

| Component | Key Props |
|---|---|
| `Button` | `variant` (primary/danger/success/warning/ghost/outline), `size` (xs/sm/md/lg), `loading`, `icon` |
| `Badge` | `color` (9 options incl. blue/green/red/yellow/purple/orange/gray) |
| `Card` / `CardHeader` | `className`, `title`, `subtitle` |
| `Input` | `label`, `error`, `icon`, standard HTML attrs |
| `Select` | `label`, `error`, `placeholder`, children `<option>` |
| `Alert` | `type` (error/success/warning/info) |
| `Modal` | `open`, `onClose`, `title` |
| `KpiCard` | `label`, `value`, `sub`, `gradient` (blue/emerald/violet/pink), `icon` |
| `Spinner` | `label` |
| `Table`, `Th`, `Td`, `Tr` | table building blocks |
| `Pagination` | `page`, `total`, `pageSize`, `onChange` |

---

## Design System

| Context | Color Scheme | Sidebar background |
|---|---|---|
| User layout | sky / blue | `#030712 → #0c1a3a` |
| Admin layout | violet / indigo | `#0f0f1a → #1a0a2e` |

Active nav items use `bg-gradient-to-r` with a small indicator dot. Avatar: profile photo if available, otherwise initials on a gradient circle. Both layouts use `useGetMyProfileQuery` to show the photo in sidebar + header.

---

## Key Patterns & Conventions

**File downloads** — manual `fetch` with `Authorization` header (RTK Query can't stream blobs):
```ts
fetch(`/api/v1/documents/${id}/download`, { headers: { Authorization: `Bearer ${accessToken}` } })
  .then(r => r.blob())
  .then(b => { const url = URL.createObjectURL(b); const a = document.createElement('a'); a.href = url; a.download = name; a.click() })
```

**Admin merge PDF** — same pattern but POST with JSON body `{ documentIds?, searchTerm?, departmentId?, financialYearId?, documentTypeId? }` to `/admin/documents/merge-pdf`.

**Timestamp fix** (ActivityLogs) — server stores IST directly, so timestamps are parsed with a regex to avoid browser timezone conversion:
```ts
function formatTime(raw: string): string {
  const m = raw.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})/)
  ...
}
```

**Soft deletes** — documents have `isDeleted: boolean`. Deleted rows show Restore instead of Delete, opacity-50 styling. 30-day retention window mentioned in delete confirmation modal.

**Search debounce** — 400ms `setTimeout` in `useEffect` before updating the `searchTerm` sent to the query.

**Inline API endpoints** — `AdminActivityLogs.tsx` injects its own `getActivityLogs` endpoint directly inside the component file (not in a shared api slice). This is intentional for small one-off endpoints.

---

## Latest Changes (as of last commit)

### Recently Modified Files
- `src/app/apiSlice.ts` — JWT refresh lock logic
- `src/shared/api/documentsApi.ts` — full document CRUD + admin endpoints
- `src/shared/api/profileApi.ts` — **new file** (GET/PUT `/me`, POST `/me/photo`)
- `src/shared/components/Icons.tsx` — icon additions (CameraIcon, PhoneIcon, SpinnerIcon, XIcon, FileIcon, etc.)
- `src/shared/layouts/AdminLayout.tsx` — profile photo in sidebar/header via `useGetMyProfileQuery`
- `src/shared/layouts/UserLayout.tsx` — same profile photo integration
- `src/features/admin/activity/AdminActivityLogs.tsx` — IST timestamp display fix
- `src/features/admin/documents/AdminDocuments.tsx` — checkbox multi-select + merge PDF + purge deleted tab
- `src/features/user/dashboard/UserDashboard.tsx` — KPI cards + bar chart with current FY auto-detect
- `src/features/user/documents/UserDocuments.tsx` — search/filter bar + debounce + soft-delete/restore
- `src/features/user/profile/UserProfile.tsx` — full profile page with avatar upload + lightbox + edit contact numbers

### Key Feature State (current)
- Profile photo upload working (JPEG/PNG/WebP, max 5MB), shown in sidebar avatars
- Admin Documents: active/deleted tabs, checkbox select for merge PDF, purge all deleted
- User Documents: debounced title search + financial year + document type filters, soft-delete with 30-day restore
- User Dashboard: auto-detects current financial year, shows KPIs + document-type bar chart
- Activity Logs: IST timestamps displayed correctly without browser timezone offset

---

## Development Notes

- Proxy target is `https://localhost:44339` with `secure: false` — this is the local .NET IIS Express backend
- All API responses wrap data as `{ data: ... }` — access with `res.data` or `res?.data?.items`
- Pagination responses: `{ data: { items: [], totalCount: number } }`
- Profile endpoint returns `{ data: { profilePhotoUrl, mobileNumber, whatsAppNumber, ... } }`
- TypeScript strict mode: `noUnusedLocals` and `noUnusedParameters` are ON — fix before building
- No test suite currently configured
