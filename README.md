<<<<<<< HEAD
# Lab3
=======
# Rental Property Management System

A Windows desktop application (C# / WinForms, .NET 8) with a SQLite database, built with the **MVC architecture** for Lab 3.

## 1. How to run

**Requirements:** Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Visual Studio 2022 (workload ".NET desktop development"), or just the .NET 8 SDK.

**Visual Studio:** open `RentalPropertyManagement.sln` -> press **F5**. NuGet restores `Microsoft.Data.Sqlite` automatically (needs internet the first time).

**Command line:**
```
cd RentalPropertyManagement
dotnet run
```

No database setup is needed. On first start the app creates the SQLite database at
`%LocalAppData%\RentalPropertyManagement\rental.db` with all tables and some sample data.
To reset everything, close the app and delete that file.

### Demo accounts (created on first run)

| Username   | Password       | Role     | Can do |
|------------|----------------|----------|--------|
| `admin`    | `Admin@123`    | Admin    | Everything, including user accounts |
| `landlord` | `Landlord@123` | Landlord | Properties, tenants, payments, dashboard, reports |
| `alice`    | `Tenant@123`   | Tenant   | Read-only: own payments/balance + browse properties |

## 2. MVC structure

```
RentalPropertyManagement/
  Models/        Plain data classes (Property, Tenant, Payment, User, TenantBalance, DashboardStats, constants)
  Views/         WinForms screens - only UI code, no SQL, no business rules
  Controllers/   All logic: validation, business rules, role checks, SQL via the Data layer
  Data/          SQLite connection + schema (Database), PBKDF2 password hashing, payment-status rules
  Session.cs     Logged-in user + role permission checks
  Program.cs     Entry point: login -> main window -> logout loop
```

Flow: **View** (user clicks Save) -> **Controller** (validates, applies rules, checks role) -> **Database** -> returns **Model** objects -> View displays them. Views never talk to the database directly.

## 3. Requirements traceability

| Requirement | Where it is implemented |
|---|---|
| 1.1 Secure login / logout | `LoginForm`, `AuthController`, `MainForm` (Logout button); passwords hashed with salted PBKDF2 (`PasswordHasher`) |
| 1.1 Admin creates, views, updates, deactivates users | `UsersView`, `UserEditForm`, `UserController` |
| 1.1 Role-based access (Admin / Landlord / Tenant) | Menu differs per role in `MainForm`; **also enforced in controllers** (`Session.RequireManager/RequireAdmin`) so it cannot be bypassed from the UI |
| 1.2 Add / view / update / delete properties | `PropertiesView`, `PropertyEditForm`, `PropertyController` |
| 1.2 Property details (name, location, type, description, rooms, rent, availability) | `Properties` table + `Property` model |
| 1.2 Available vs occupied shown | `Status` column (green = Available, blue = Occupied); updated automatically when a tenant is assigned / removed |
| 1.2 Search and filter | Search box + Status + Type filters in `PropertiesView` |
| 1.3 Register / view / update / delete tenants | `TenantsView`, `TenantEditForm`, `TenantController` |
| 1.3 Tenant details (name, phone, email, ID) | `Tenants` table; phone/email format validated |
| 1.4 Record rent payments with tenant, property, amount, date, method, status | `PaymentsView`, `PaymentEditForm`, `PaymentController` |
| 1.4 Payment history per tenant | "Payment History" tab with tenant filter |
| 1.4 Outstanding rent calculation | `PaymentController.GetBalances` + `PaymentRules` ("Outstanding Rent" tab) |
| 1.4 Paid / Partially Paid / Outstanding | `PaymentRules.StatusFor` / `BalanceStatus`, recalculated on every add/edit/delete and when a property's rent changes |
| 1.5 Dashboard (totals, available/occupied, collected, outstanding) | `DashboardView`, `ReportController.GetDashboard` |
| 1.5 Property, tenant and payment reports; view and export | `ReportsView`, `ReportController` (export to CSV, opens in Excel) |
| 2 Security | Hashed passwords, parameterised SQL (no SQL injection), role checks, same error for wrong user/password, CSV formula-injection guard |
| 2 Performance | Local SQLite, indexed queries, no network calls |
| 2 Usability | Consistent layout, colour-coded statuses, confirmation before deleting, search on every list |
| 2 Accuracy / error messages | Input validation with clear messages, database transactions, foreign keys + CHECK constraints, unexpected errors caught and shown instead of crashing |

## 4. Business rules (assumptions you should know)

* **Rent month.** Every payment is recorded against a rent month (e.g. "November 2026").
* **Payment status.** For a tenant + month, total paid >= monthly rent -> **Paid**; some but less -> **Partially Paid**.
* **Outstanding rent** = monthly rent x number of months from the lease start month up to and including the current month - everything the tenant has paid. A tenant who has paid nothing is **Outstanding**.
* One tenant per property. Assigning a tenant marks the property **Occupied**; removing or deleting the tenant frees it.
* Properties/tenants that have payment history cannot be deleted (to keep financial records accurate). Unassign the tenant from the property instead.
* The last active administrator cannot be deactivated or demoted.
* Money is shown without a currency symbol, so it works for any currency.

## 5. Quick test checklist

1. Log in as `admin`; the Dashboard shows 5 properties, 3 tenants, 2 available, 3 occupied, outstanding rent.
2. **Properties:** add one, edit it, search by name, filter by status, try deleting an occupied one (should be refused).
3. **Tenants:** register a tenant and assign "Studio 5" -> Properties now shows it as Occupied.
4. **Payments:** record a payment lower than the rent -> status "Partially Paid"; record the remainder for the same month -> both become "Paid". Check the "Outstanding Rent" tab.
5. **Reports:** generate each report, export to CSV, open in Excel.
6. **Users:** create a landlord, deactivate them, confirm they can no longer log in. Try deactivating the only admin (should be refused).
7. Log in as `alice`: only "My Rent & Payments" and "Available Properties" appear, all read-only, and only her own data is visible.

## 6. Uploading to GitHub (required by the lab)

```
cd RentalPropertyManagement          # the folder that contains the .sln
git init
git add .
git commit -m "Rental Property Management System - Lab 3"
git branch -M main
git remote add origin https://github.com/<your-username>/<your-repo>.git
git push -u origin main
```
Create the empty repository on github.com first (no README), copy its URL into the commands above, and submit that repository link in the form.
(`.gitignore` already excludes `bin/`, `obj/` and database files.)
>>>>>>> 364440b (Initial project commit)
