# SAMTEK — Claude Code Context

## Apa ini?
SAMTEK (System Auth Management Technology) adalah middleware .NET 9 Blazor Server yang menjembatani IdAMan (Pertamina SSO/LDAP) dengan aplikasi ARIS dan APPA. User login sekali via IdAMan → SAMTEK mapping ke shared role credentials → auto-redirect ke APPA (SSO tanpa login ulang).

## Arsitektur — Clean Architecture
```
SAMTEK.Domain          → Entities, Enums (tidak ada dependency ke project lain)
SAMTEK.Application     → Interfaces, DTOs (hanya dependency ke Domain)
SAMTEK.Infrastructure  → EF Core, Persistence, Identity providers (implements Application)
SAMTEK.Web             → Blazor Server, Controllers, Pages (startup + UI)
```

## Tech Stack
- .NET 9, ASP.NET Core, Blazor Server
- EF Core 9 + Npgsql (PostgreSQL)
- BCrypt.Net-Next (admin password hashing)
- System.DirectoryServices.Protocols (LDAP — untuk IdAMan production)
- JWT Bearer auth (untuk API controllers)
- Bootstrap 5 + Open Iconic (CSS)

## Cara Jalankan

### Prerequisites
- .NET 9 SDK
- PostgreSQL berjalan (default dev: port 8000, password: `123`)
- (Opsional) APPA React frontend di port 3000, API di port 5007

### Setup pertama kali
```powershell
cd SAMTEK.Web
dotnet run
```
- Auto-migrate database saat startup
- Auto-seed admin default: **username: `admin`, password: `admin123`**
- App berjalan di http://localhost:5000

### Config dev (WAJIB dibuat manual, tidak di-commit)
Buat file `SAMTEK.Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=8000;Database=samtek;Username=postgres;Password=123"
  },
  "Jwt": {
    "Key": "sopldap-dev-secret-key-change-in-production-32chars"
  },
  "Appa": {
    "ApiUrl": "http://localhost:5007",
    "FrontendUrl": "http://localhost:3000"
  },
  "MockUsers": [
    { "Username": "designer1", "Password": "pass123", "DisplayName": "Designer User" },
    { "Username": "viewer1",   "Password": "pass123", "DisplayName": "Viewer User" },
    { "Username": "rehan",     "Password": "pass123", "DisplayName": "Rehan" }
  ]
}
```

## Alur Auth

### Regular User (IdAMan/Mock)
1. User buka `/login` → masukkan username/password IdAMan
2. `IIdentityProvider.AuthenticateAsync` — dev pakai `MockIdentityProvider` (dari config `MockUsers`)
3. `IMappingService.GetCredentialsAsync` → cek apakah user sudah di-assign role di DB
4. Jika tidak ada mapping → redirect ke `/no-access`
5. Jika ada → SAMTEK call APPA API `/api/auth/login` server-side (pakai shared credentials)
6. Dapat APPA token → redirect ke APPA frontend dengan params:
   - `?token=<APPA_JWT>&user=<BASE64_USER_JSON>&aris_user=<BASE64_USERNAME>&aris_pwd=<BASE64_PASSWORD>`
7. APPA `main.jsx` baca params → simpan ke localStorage + sessionStorage → Zustand store update

### Admin SAMTEK
1. Buka `/admin-login` → username/password dari tabel `admin_users` (BCrypt)
2. Set `SamtekSession.IsAdminAuthenticated = true` (scoped per Blazor circuit)
3. Redirect ke `/admin/mappings`
4. Logout → clear session → redirect ke `/admin-login`

### ARIS SSO (dari APPA)
- APPA simpan `aris_credentials` di `sessionStorage` (format: `{ u: btoa(username), p: btoa(password) }`)
- Navbar APPA cek `arisService.hasCredentials()` → tampilkan tombol "Login ke ARIS"
- Klik → POST ke ARIS Cloud token API → buka tab baru

## Entities Penting
```
LdapUser          → username, displayName, lastLoginAt
ArisRole          → roleCode, arisUsername, arisPasswordHash (stored plaintext!), roleType, canPrint, canDownload
UserRoleMapping   → username (FK LdapUser), arisRoleId (FK ArisRole), appaRole, isApproved
AdminUser         → username, passwordHash (BCrypt), isActive
AuditLog          → username, action, detail, ipAddress, timestamp
```

## Key Services
| Interface | Implementation | Keterangan |
|---|---|---|
| `IIdentityProvider` | `MockIdentityProvider` / `LdapIdentityProvider` | Dikontrol via `appsettings: IdentityProvider: "Mock"/"Ldap"` |
| `IMappingService` | `MappingService` | CRUD mapping user→role |
| `IAdminAuthService` | `AdminAuthService` | BCrypt validate admin |
| `IAuditService` | `AuditService` | Log semua login events |

## Switching ke LDAP
Di `appsettings.json` atau `appsettings.Development.json`:
```json
{
  "IdentityProvider": "Ldap",
  "Ldap": {
    "Host": "idaman.pertamina.com",
    "Port": 389,
    "BaseDn": "dc=pertamina,dc=com"
  }
}
```

## Halaman & Routes
| Route | Keterangan |
|---|---|
| `/login` | Login regular user (IdAMan/Mock) |
| `/admin-login` | Login admin SAMTEK |
| `/no-access` | Ditampilkan jika user tidak punya mapping |
| `/admin/mappings` | Kelola mapping user → role (protected: admin only) |
| `/admin/aris-roles` | Kelola shared role accounts (protected: admin only) |
| `/api/auth/login` | REST endpoint login (dipakai APPA API call dari SAMTEK) |
| `/api/auth/credentials` | REST endpoint get ARIS credentials (JWT protected) |

## Database
- EF Core Migrations di `SAMTEK.Infrastructure/Persistence/Migrations/`
- Auto-migrate saat startup (`db.Database.Migrate()` di `Program.cs`)
- Tabel: `ldap_users`, `aris_roles`, `user_role_mappings`, `admin_users`, `audit_logs`

## Catatan Penting
- `ArisPasswordHash` di entity `ArisRole` sebenarnya menyimpan **plaintext password** (bukan hash BCrypt). Nama field misleading — di production harus dienkripsi dengan app key.
- `SamtekSession` adalah scoped service (per Blazor circuit = per browser tab). Jangan inject sebagai singleton.
- Auth guard di admin pages menggunakan `OnAfterRenderAsync` (bukan `OnInitializedAsync`) untuk menghindari Blazor prerendering bypass.
- `appsettings.Development.json` tidak di-commit (ada di `.gitignore`). Harus dibuat manual di laptop baru.
- APPA React app terpisah di repo lain (`APPADraft1`). SSO params dibaca di `main.jsx`.

## Proyek Terkait
- **APPA** (`APPADraft1/appa-ui-react`) — Frontend React yang menjadi tujuan SSO redirect
- **Keycloak** di `C:\keycloak\keycloak-26.2.5` — jalankan dengan `bin\kc.bat start-dev` (port 8080)
