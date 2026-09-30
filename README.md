# DeveloperStore — Sales API

Implementation of the **Sales** API prototype for the DeveloperStore evaluation, built on top of the provided
.NET 8 template (DDD layers, MediatR, AutoMapper, FluentValidation, EF Core + PostgreSQL, xUnit + NSubstitute + Bogus).

The original challenge statement is kept in [`.doc/`](.doc/overview.md).

## Contents

- [What was implemented](#what-was-implemented)
- [Business rules](#business-rules)
- [Architecture](#architecture)
- [Configure and run](#configure-and-run)
- [Tests](#tests)
- [API reference](#api-reference)
- [Events](#events)
- [Design decisions and assumptions](#design-decisions-and-assumptions)
- [Changes to the template](#changes-to-the-template)

## What was implemented

- Complete CRUD for sales: create, get by id, list (pagination / ordering / filtering), update, delete.
- Sale cancellation and single item cancellation.
- Quantity-based discount rules enforced in the domain.
- `External Identities` pattern: customer, branch and product are referenced by id with their names denormalized.
- Events `SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled` published through MediatR notifications and written to the application log.
- EF Core mappings + migration, unit tests for domain rules and application handlers.

Each sale exposes: sale number, sale date, customer, total sale amount, branch, and per item: product, quantity,
unit price, discount, total amount; plus the cancelled flag for the sale and for each item.

## Business rules

| Identical items (same product) | Discount |
|---|---|
| 1 – 3 | none |
| 4 – 9 | 10% |
| 10 – 20 | 20% |
| above 20 | **not allowed** (request rejected) |

- Rules live in `Domain/Services/QuantityDiscountPolicy.cs` and are applied by `SaleItem` whenever quantity or price changes.
- The same product cannot appear in two lines of a sale; otherwise the 20-item limit could be bypassed by splitting lines.
- `Discount = round(quantity × unitPrice × rate, 2)`, `ItemTotal = quantity × unitPrice − Discount`.
- `SaleTotal` = sum of the totals of **non-cancelled** items.
- A cancelled sale cannot be updated, cancelled again or have items cancelled.

## Architecture

```
template/backend
├── src
│   ├── Ambev.DeveloperEvaluation.Domain        Sale aggregate, SaleItem, discount policy, events, repository contract
│   ├── Ambev.DeveloperEvaluation.Application   Use cases (MediatR commands/handlers/validators/profiles), event log handler
│   ├── Ambev.DeveloperEvaluation.ORM           EF Core mappings, SaleRepository, migrations (PostgreSQL)
│   ├── Ambev.DeveloperEvaluation.IoC           Dependency registration
│   ├── Ambev.DeveloperEvaluation.Common        Cross-cutting (logging, validation, security, health checks)
│   └── Ambev.DeveloperEvaluation.WebApi        SalesController, request/response contracts, middlewares
└── tests
    └── Ambev.DeveloperEvaluation.Unit          Domain and application tests
```

Request flow: `SalesController` → request validation → AutoMapper → MediatR command → handler
(command validation, aggregate operation, repository, event publish) → result → API response.

The Sales feature follows the same structure as the template's Users feature
(`Features/<Entity>/<UseCase>` in WebApi and `<Entity>/<UseCase>` in Application).

## Configure and run

### Prerequisites

- .NET 8 SDK
- Docker (for PostgreSQL, or to run everything with compose)

### Option 1 — Docker Compose (API + database)

```bash
cd template/backend
docker compose up --build
```

- Swagger: http://localhost:8080/swagger
- The API runs in `Development`, so pending migrations are applied automatically on startup.

### Option 2 — Local API + PostgreSQL in Docker

```bash
cd template/backend
docker compose up -d ambev.developerevaluation.database
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

- Swagger: http://localhost:5119/swagger
- Connection string: `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (`ConnectionStrings:DefaultConnection`),
  already matching the compose database (`developer` / `ev@luAt10n`, database `developer_evaluation`, port `5432`).
  It can be overridden with the environment variable `ConnectionStrings__DefaultConnection`.

### Migrations

Applied automatically in `Development`. To apply manually or create new ones:

```bash
dotnet tool install --global dotnet-ef --version 8.0.10
cd template/backend
dotnet ef database update --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

## Tests

```bash
cd template/backend
dotnet test
```

Coverage report (uses the template scripts):

```bash
./coverage-report.sh        # Linux/macOS
coverage-report.bat         # Windows
```

Covered:

- `QuantityDiscountPolicyTests` — every discount tier boundary and the invalid quantities.
- `SaleTests` — item totals and discounts, 20-item limit, duplicate product lines, item cancellation and total
  recalculation, sale cancellation, cancelled sale immutability, update synchronization of items, UTC date normalization.
- Handler tests (NSubstitute for the repository and `IMediator`, real AutoMapper profiles, Bogus data) — create, get,
  list, update, delete, cancel sale and cancel item, including not found, validation and event publishing.

Manual requests are available in `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`.

## API reference

Base path: `/api/sales`. Responses use the template envelope `{ success, message, data, errors }`.

| Method | Route | Description |
|---|---|---|
| POST | `/api/sales` | Create a sale (201) |
| GET | `/api/sales/{id}` | Get a sale |
| GET | `/api/sales` | List sales (paginated) |
| PUT | `/api/sales/{id}` | Update a sale |
| DELETE | `/api/sales/{id}` | Delete a sale |
| PATCH | `/api/sales/{id}/cancel` | Cancel a sale |
| PATCH | `/api/sales/{id}/items/{itemId}/cancel` | Cancel one item |

### Create

```http
POST /api/sales
Content-Type: application/json

{
  "saleNumber": "S-0001",
  "saleDate": "2026-09-30T10:00:00Z",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "John Doe",
  "branchId": "9b2f1c3e-1a2b-4c5d-8e9f-0a1b2c3d4e5f",
  "branchName": "Belo Horizonte - Savassi",
  "items": [
    { "productId": "11111111-1111-1111-1111-111111111111", "productName": "Brahma 350ml", "quantity": 3,  "unitPrice": 4.50 },
    { "productId": "33333333-3333-3333-3333-333333333333", "productName": "Stella 330ml", "quantity": 12, "unitPrice": 6.90 }
  ]
}
```

Response (excerpt):

```json
{
  "success": true,
  "message": "Sale created successfully",
  "data": {
    "id": "…",
    "saleNumber": "S-0001",
    "totalAmount": 79.74,
    "isCancelled": false,
    "items": [
      { "productName": "Brahma 350ml", "quantity": 3,  "unitPrice": 4.50, "discountPercentage": 0,   "discount": 0,     "totalAmount": 13.50, "isCancelled": false },
      { "productName": "Stella 330ml", "quantity": 12, "unitPrice": 6.90, "discountPercentage": 0.2, "discount": 16.56, "totalAmount": 66.24, "isCancelled": false }
    ]
  }
}
```

Discounts and totals are always calculated by the API; they are not accepted as input.

### Update

`PUT /api/sales/{id}` takes the same body as create, without `saleNumber` (immutable).
Items are synchronized by `productId`: existing products are updated (keeping their item id), new products are added,
products not sent are removed. Cancelled items are kept as history.

### List

Follows [General API](.doc/general-api.md) conventions:

| Parameter | Example |
|---|---|
| `_page`, `_size` (1–100) | `_page=2&_size=20` |
| `_order` | `_order=saleDate desc, saleNumber` — fields: `saleNumber`, `saleDate`, `customerName`, `branchName`, `totalAmount`, `isCancelled`, `createdAt`. Default: `saleDate desc` |
| `saleNumber`, `customerName`, `branchName` | exact match, or partial with `*`: `customerName=John*`, `branchName=*Savassi` (case-insensitive) |
| `customerId`, `branchId`, `isCancelled` | `isCancelled=false` |
| `_minSaleDate`, `_maxSaleDate` | `_minSaleDate=2026-01-01` |
| `_minTotalAmount`, `_maxTotalAmount` | `_minTotalAmount=100` |

Response: `{ data: [...], currentPage, totalPages, totalCount, success }`.

### Errors

| Situation | Status |
|---|---|
| Request / command validation (e.g. quantity > 20, duplicated product line, missing fields) | 400 |
| Domain rule violation (e.g. modifying a cancelled sale) | 400 |
| Sale or item not found | 404 |
| Duplicated sale number | 409 |

## Events

Handlers publish MediatR notifications after the change is persisted:

| Event | Raised by |
|---|---|
| `SaleCreatedEvent` | Create |
| `SaleModifiedEvent` | Update |
| `SaleCancelledEvent` | Cancel sale |
| `ItemCancelledEvent` | Cancel item |

`Application/Sales/Events/SaleEventsLogHandler.cs` writes them to the application log (as allowed by the challenge).
Publishing to a broker (e.g. Rebus) only requires another `INotificationHandler` for these events — no change to the use cases.

Example log line:

```
[INF] Ambev.DeveloperEvaluation.Application.Sales.Events.SaleEventsLogHandler Event SaleCreatedEvent published: {"SaleId": "…", "SaleNumber": "S-0001", …}
```

## Design decisions and assumptions

- **"Above 4 items" vs "4+ items"**: the statement uses both; the explicit tier list (4+ → 10%) was adopted, so exactly 4 items get 10%.
- **Sale number** is provided by the client and must be unique (unique index + check in the handler).
- **Delete** is a hard delete (CRUD requirement). **Cancel** is the business operation that keeps the record.
- **Ids** of `Sale` and `SaleItem` are generated in the domain (`ValueGeneratedNever`), so items added to a tracked sale are inserted by EF Core instead of being treated as existing rows.
- **Dates** are normalized to UTC (required by Npgsql for `timestamp with time zone`).
- **Money** uses `decimal` / `numeric(18,2)`; discount rate is stored as `numeric(5,4)`.
- A single `SaleResult` / `SaleResponse` is shared by the use cases that return a sale, instead of one identical DTO per use case.

## Changes to the template

Fixes needed for the template to run end-to-end:

- `appsettings.json`: connection string was SQL Server style with different credentials; now points to the compose PostgreSQL.
- `docker-compose.yml`: fixed host port mapping (`8080:8080`, `5432:5432`), API connection string, `depends_on` with a health check; removed the HTTPS port that required a Windows-mounted certificate.
- Migration `AddSales` also adds `Users.CreatedAt` / `Users.UpdatedAt`, which exist in the `User` entity but were missing from the initial migration (user inserts would fail).
- Design-time `DbContext` factory now uses the ORM assembly for migrations, same as `Program.cs`.
- `BaseController.OkPaginated` called the generic `Ok<T>` helper, wrapping the response twice; it now calls `base.Ok`.
- `ExceptionHandlingMiddleware` maps not found / domain / conflict exceptions to 404 / 400 / 409 instead of 500.
