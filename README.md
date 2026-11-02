# CloudEngineerFunction

Eine serverlose REST-API für Support-Tickets, gebaut mit **.NET 10**, **Azure Functions (Isolated Worker)** und **Entity Framework Core** gegen eine **Azure SQL Database**.

## Überblick

Dieses Projekt demonstriert eine vollständige CRUD-API auf Basis von Azure Functions:

| Methode | Route              | Beschreibung                       |
|---------|--------------------|-------------------------------------|
| POST    | `/api/tickets`     | Neues Ticket anlegen                |
| GET     | `/api/tickets`     | Alle Tickets auflisten              |
| GET     | `/api/tickets/{id}`| Ein Ticket per ID abrufen           |
| PUT     | `/api/tickets/{id}`| Bestehendes Ticket aktualisieren    |
| DELETE  | `/api/tickets/{id}`| Ticket löschen                      |

## Architektur

```
Client (z. B. Postman / Frontend)
		│ HTTPS
		▼
Azure Functions (Isolated Worker, .NET 10)
		│ EF Core (DbContext)
		▼
Azure SQL Database
```

- **Hosting**: Azure Functions, Isolated Process Model, ASP.NET Core Integration (`ConfigureFunctionsWebApplication`)
- **Datenzugriff**: Entity Framework Core mit `Microsoft.EntityFrameworkCore.SqlServer`
- **Datenbank**: Azure SQL Database (Serverless Tier)
- **Telemetrie**: OpenTelemetry (vorbereitet für Azure Monitor / Application Insights)

## Projektstruktur

```
CloudEngineerFunction/
├── Program.cs           # Host-Konfiguration, DI, DB-Migration beim Start
├── Function1.cs          # HTTP-Trigger-Funktionen (CRUD) + SupportTicket-Modell
├── AppDBContext.cs        # EF Core DbContext
├── host.json              # Azure Functions Host-Konfiguration
└── local.settings.json    # Lokale Konfiguration (NICHT eingecheckt, siehe unten)
```

## Setup

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Eine Azure SQL Database (oder lokaler SQL Server für Tests)

### Lokale Konfiguration

Dieses Repository enthält **keine** echten Zugangsdaten. Stattdessen:

1. Kopiere die Vorlage:
   ```powershell
   Copy-Item CloudEngineerFunction/local.settings.json.example CloudEngineerFunction/local.settings.json
   ```
2. Trage in `local.settings.json` deine eigene `SqlConnectionString` ein.
3. Starte das Projekt lokal (F5 in Visual Studio oder `func start`).

Beim ersten Start wird die Datenbank samt Tabellen automatisch per `EnsureCreated()` angelegt, falls sie noch nicht existiert.

### Testen

Die Endpunkte lassen sich z. B. mit Postman oder `curl` testen:

```powershell
curl -X POST http://localhost:7098/api/tickets -H "Content-Type: application/json" -d '{"title":"Test","description":"Beschreibung","user":"Max"}'
curl http://localhost:7098/api/tickets
```

## Sicherheit & Secrets

- `local.settings.json` ist in `.gitignore` ausgeschlossen und wird niemals eingecheckt.
- Für produktive Umgebungen werden Connection Strings **nicht** in Konfigurationsdateien, sondern über **Azure Key Vault** bzw. **Managed Identity** oder App Service/Function App Application Settings bereitgestellt.

## Tech-Stack

- .NET 10
- Azure Functions (Isolated Worker, v4)
- Entity Framework Core 10 (SQL Server Provider)
- Azure SQL Database
- OpenTelemetry / Azure Monitor
