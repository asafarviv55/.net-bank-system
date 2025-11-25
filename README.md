# Bank System

ASP.NET Core banking application with user authentication, loans management, credit expenses tracking, and transaction history.

## Features

- User Authentication (ASP.NET Identity + Google OAuth)
- Loan Management
- Credit Card Expenses Tracking
- Transaction History (PassBack Operations)
- ATM Interface

## Prerequisites

- .NET 6.0 SDK
- SQL Server
- Google OAuth credentials (optional)

## Setup

1. Clone the repository
2. Copy `appsettings.Example.json` to `appsettings.Development.json`
3. Update connection string and OAuth credentials

```bash
cd Bank
dotnet restore
dotnet ef database update
dotnet run
```

## Docker

```bash
# Create .env file with:
# DB_PASSWORD=YourStrongPassword123!
# GOOGLE_CLIENT_ID=your-client-id
# GOOGLE_CLIENT_SECRET=your-client-secret

docker-compose up -d
```

## Configuration

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:BankContext` | SQL Server connection string |
| `Authentication:Google:ClientId` | Google OAuth Client ID |
| `Authentication:Google:ClientSecret` | Google OAuth Client Secret |

## Project Structure

```
Bank/
├── Areas/Identity/     # Authentication pages
├── Data/               # Database context
├── Models/             # Domain models
├── Pages/              # Razor pages
│   ├── ATM/
│   ├── CreditExpenses/
│   ├── Loans/
│   └── PassBackOperations/
└── wwwroot/           # Static files
```
