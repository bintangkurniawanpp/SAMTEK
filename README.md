# SAMTEK — System Auth Management Technology

Middleware .NET 9 yang menjembatani IdAMan (Pertamina SSO/LDAP) dengan ARIS dan APPA. User login sekali via IdAMan, SAMTEK memetakan ke shared role credentials, lalu auto-redirect ke APPA tanpa login ulang.

## Prasyarat

| Komponen | Versi |
|---|---|
| .NET SDK | 9.0+ |
| PostgreSQL | 14+ |
| APPA API | berjalan di port 5007 |
| APPA Frontend | berjalan di port 3000 |

## Instalasi

### 1. Clone repo
```bash
git clone https://github.com/Rehan2405/SAMTEK.git
cd SAMTEK
```

### 2. Buat config development
Buat file `SAMTEK.Web/appsettings.Development.json` (tidak di-commit karena berisi credentials):

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

> Sesuaikan `Port`, `Password` PostgreSQL, dan URL APPA dengan environment kamu.

### 3. Jalankan
```bash
cd SAMTEK.Web
dotnet run
```

Database akan auto-migrate dan auto-seed admin default saat pertama kali jalan.

App tersedia di: **http://localhost:5000**

---

## Akun Default

| Tipe | Username | Password | Halaman |
|---|---|---|---|
| Admin SAMTEK | `admin` | `admin123` | http://localhost:5000/admin-login |
| Regular User (dev) | `designer1` | `pass123` | http://localhost:5000/login |
| Regular User (dev) | `viewer1` | `pass123` | http://localhost:5000/login |

> Regular users dari `MockUsers` di config (hanya untuk development, tanpa koneksi ke LDAP IdAMan).

---

## Setup Awal (Laptop Baru)

### Langkah 1 — Setup Admin & Role
1. Buka http://localhost:5000/admin-login
2. Login dengan `admin` / `admin123`
3. Buka **ARIS Roles** → klik **+ Add Role**
4. Isi:
   - **Role Code**: `DESIGNER`
   - **Username**: username shared account APPA/ARIS
   - **Password**: password shared account APPA/ARIS
   - **Role Type**: `Designer`
5. Ulangi untuk role `VIEWER` jika perlu

### Langkah 2 — Assign User ke Role
1. Buka **User Mappings** → klik **+ Assign Role**
2. Isi username IdAMan (atau MockUser untuk dev), pilih role
3. Klik **Assign**

### Langkah 3 — Test Login Regular User
1. Buka http://localhost:5000/login (atau langsung http://localhost:5000)
2. Login dengan MockUser (contoh: `designer1` / `pass123`)
3. Seharusnya langsung redirect ke APPA landing page

---

## Arsitektur

```
SAMTEK.Domain          → Entities, Enums
SAMTEK.Application     → Interfaces, DTOs
SAMTEK.Infrastructure  → Database (EF Core), Identity (Mock/LDAP), Migrations
SAMTEK.Web             → Blazor Server UI, REST API Controllers, Startup
```

### Alur Login Regular User
```
User → /login → IdAMan/Mock auth → cek mapping → ambil shared credentials
     → call APPA API login (server-side) → dapat APPA token
     → redirect APPA frontend dengan ?token=...&user=...&aris_user=...&aris_pwd=...
     → APPA simpan ke localStorage + sessionStorage → user masuk APPA
```

### Alur Login Admin
```
Admin → /admin-login → validasi BCrypt dari tabel admin_users
      → SamtekSession.IsAdminAuthenticated = true
      → redirect /admin/mappings
```

---

## Switching ke LDAP (Production)

Di `appsettings.json`, ubah:
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

---

## Komponen Terkait

### APPA React (APPADraft1)
Frontend yang menjadi tujuan SSO. SSO params dibaca di `src/main.jsx`. Pastikan sudah berjalan sebelum test login.

### Keycloak
Lokasi: `C:\keycloak\keycloak-26.2.5`
```bash
bin\kc.bat start-dev
# Tersedia di http://localhost:8080
```

---

## Troubleshooting

| Masalah | Solusi |
|---|---|
| `Connection refused` saat startup | Pastikan PostgreSQL jalan di port yang sesuai config |
| User login tapi redirect ke `/no-access` | User belum di-assign role di Admin → User Mappings |
| APPA tidak auto-login setelah redirect | Pastikan APPA API berjalan di port 5007 dan credentials shared account benar |
| Tombol "Login ke ARIS" tidak muncul di APPA | Login ulang via SAMTEK (sessionStorage bersih kalau tab ditutup) |
| Admin page muncul tanpa login | Hard refresh browser (Ctrl+Shift+R) |
