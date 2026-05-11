# Library Management System

C# WPF desktop application based on the guide in `Library-Management-System.docx`.

## Requirements

- Visual Studio 2022 or later
- .NET 6 Desktop Development workload

## Run

1. Open `LibraryManagementSystem.sln` in Visual Studio.
2. Restore/build the solution.
3. Start the project.
4. Sign in with the default administrator account.

## Default Account

- Admin: `admin` / `admin123`

## Included Features

- Role-based login for Admin and Librarian users
- Book management with search and availability filter
- Patron management with search and membership/status filter
- Checkout and return workflow with automatic overdue fine calculation
- Reports for overdue books, checked out books, history, and patron activity
- Report date range filtering
- CSV, Excel, and PDF report export
- Fine tracking with mark-as-paid workflow
- Selected patron borrowing history
- Settings screen with language/culture selection and deployment information
- Admin-only user management and password reset
- Admin backup and restore for the local data file

Data is stored locally in SQLite under the current Windows user profile so the app can run without an external database server.
