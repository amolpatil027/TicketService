# Ticket Age API - Multiple Excel Upload + Merge

.NET 8 ASP.NET Core Web API with Swagger and ClosedXML.

## Main workflow

1. Upload multiple `.xlsx` files in one request.
2. Each file is validated and read.
3. All rows are merged into one in-memory collection.
4. Duplicate `Number` values are replaced by the record from the later uploaded file.
5. Age is calculated against `DateTime.Today` (server/system date).
6. `GET /api/tickets/age-summary` returns the four age groups.
7. `GET /api/tickets/older-than-7/download` creates ONE merged Excel file containing all tickets older than 7 days.

## Required Excel columns

Number
Opened
Short description
Caller
Location
Employee
Maxim
Priority
State
Service
Service offering
Assignment group
Assigned to

## Swagger

Run:

```bash
dotnet restore
dotnet run
```

Open:

http://localhost:5080/swagger

### Multiple file upload

Use:

`POST /api/tickets/import`

Click **Try it out**. The `files` field supports selecting multiple `.xlsx` files.

Example result:

```json
{
  "systemDate": "2026-08-28T00:00:00",
  "filesProcessed": 3,
  "importedRows": 55,
  "mergedUniqueTickets": 48
}
```

### Summary

`GET /api/tickets/age-summary`

Example:

```json
{
  "systemDate": "2026-08-28T00:00:00",
  "totalTickets": 48,
  "age0To3Days": 12,
  "age4To5Days": 8,
  "age6To7Days": 7,
  "moreThan7Days": 21
}
```

### Download merged >7-day tickets

`GET /api/tickets/older-than-7/download`

This returns a SINGLE Excel workbook containing all merged tickets where:

```text
AgeDays > 7
```

Columns:

```text
Number
Opened
Short description
Caller
Location
Employee
Maxim
Priority
State
Service
Service offering
Assignment group
Assigned to
```

### Clear merged data

`DELETE /api/tickets/clear`

Useful before starting a new batch.

## Age calculation

The API evaluates:

```csharp
AgeDays = Math.Max(0, (DateTime.Today - ticket.Opened.Date).Days);
```

Buckets:

- 0-3 days
- 4-5 days
- 6-7 days
- More than 7 days

The date is the **server/system date**, not a hard-coded date.

## Docker

```bash
docker build -t ticket-age-api .
docker run --rm -p 5080:8080 ticket-age-api
```

Swagger:

http://localhost:5080/swagger

## Storage

The sample uses in-memory storage. Restarting the API clears the merged data. For production, replace it with SQL Server/database storage.
