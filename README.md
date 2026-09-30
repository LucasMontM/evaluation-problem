# DeveloperStore — Sales API

Implementation of the **Sales** API prototype for the DeveloperStore evaluation, built on top of the provided
.NET 8 template (DDD layers, MediatR, AutoMapper, FluentValidation, EF Core + PostgreSQL, xUnit + NSubstitute + Bogus).

The original challenge statement is kept in [`.doc/`](.doc/overview.md).

## Contents

- [DeveloperStore — Sales API](#developerstore--sales-api)
  - [Contents](#contents)
  - [Quick start](#quick-start)
  - [What was implemented](#what-was-implemented)
  - [Business rules](#business-rules)
  - [Architecture](#architecture)
  - [Prerequisites](#prerequisites)
  - [Running the API](#running-the-api)
    - [Option 1 — Everything in Docker](#option-1--everything-in-docker)
    - [Option 2 — PostgreSQL in Docker, API local (CLI)](#option-2--postgresql-in-docker-api-local-cli)
    - [Option 3 — PostgreSQL in Docker, API in Visual Studio](#option-3--postgresql-in-docker-api-in-visual-studio)
    - [Database and migrations](#database-and-migrations)
    - [Stopping](#stopping)
  - [Tests](#tests)
    - [Unit tests](#unit-tests)
    - [End-to-end tests](#end-to-end-tests)
    - [Manual tests](#manual-tests)
  - [API reference](#api-reference)
    - [Create](#create)
    - [Update](#update)
    - [Cancel](#cancel)
    - [List](#list)
    - [Errors](#errors)
  - [Events](#events)
  - [Troubleshooting](#troubleshooting)
  - [Design decisions and assumptions](#design-decisions-and-assumptions)
  - [Changes to the template](#changes-to-the-template)

## Quick start

```bash
cd template/backend

# 1. Unit tests (no database needed)
dotnet test Ambev.DeveloperEvaluation.sln

# 2. API + PostgreSQL in Docker
docker compose up --build
# Swagger: http://localhost:8080/swagger

# 3. End-to-end tests against the running API (Windows PowerShell / PowerShell 7)
powershell -ExecutionPolicy Bypass -File ./scripts/sales-api-tests.ps1 -BaseUrl http://localhost:8080
```

## What was implemented

- Complete CRUD for sales: create, get by id, list (pagination / ordering / filtering), update, delete.
- Sale cancellation and single item cancellation.
- Quantity-based discount rules enforced in the domain.
- `External Identities` pattern: customer, branch and product are referenced by id with their names denormalized.
- Events `SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled` published through MediatR notifications and written to the application log.
- EF Core mappings + migration, unit tests (domain rules and application handlers) and an end-to-end test script.

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
├── tests
│   └── Ambev.DeveloperEvaluation.Unit          Domain and application tests
└── scripts
    └── sales-api-tests.ps1                     End-to-end tests against a running API
```

Request flow: `SalesController` → request validation → AutoMapper → MediatR command → handler
(command validation, aggregate operation, repository, event publish) → result → API response.

The Sales feature follows the same structure as the template's Users feature
(`Features/<Entity>/<UseCase>` in WebApi and `<Entity>/<UseCase>` in Application).

> In Visual Studio the Solution Explorer shows **solution folders** (Adapters, Core, Crosscutting, Tests), not the
> physical `src/` folder. Example: `SalesController.cs` is under
> `Adapters → Drivers → WebApi → Ambev.DeveloperEvaluation.WebApi → Features → Sales`. Use `Ctrl+T` to open files by name.

## Prerequisites

| Tool | Needed for | Notes |
|---|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | build, tests, running locally | A newer SDK (e.g. 9) also builds the `net8.0` projects |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | PostgreSQL (and optionally the API) | On Windows it requires WSL 2. **Docker Desktop must be open** (status *Engine running*) before any `docker` command |
| Visual Studio 2022 / Rider / VS Code | optional | |

Check the installation:

```bash
dotnet --version
docker --version
```

## Running the API

All commands below run from `template/backend`.

### Option 1 — Everything in Docker

```bash
docker compose up --build
```

| | URL |
|---|---|
| Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |

### Option 2 — PostgreSQL in Docker, API local (CLI)

```bash
docker compose up -d ambev.developerevaluation.database
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

| | URL |
|---|---|
| Swagger | http://localhost:5119/swagger |

### Option 3 — PostgreSQL in Docker, API in Visual Studio

1. Start the database: `docker compose up -d ambev.developerevaluation.database`
2. Open `template/backend/Ambev.DeveloperEvaluation.sln` (**File → Open → Project/Solution**).
3. In the toolbar, set the startup project to **Ambev.DeveloperEvaluation.WebApi** (dropdown next to the gear icon)
   and the launch profile to **http** or **https** (dropdown next to the green ▶).
4. Press `F5`. Swagger opens automatically:
   - `http` profile: http://localhost:5119/swagger
   - `https` profile: https://localhost:7181/swagger (the API also listens on http://localhost:5119)

### Database and migrations

- The connection string is in `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json` (`ConnectionStrings:DefaultConnection`)
  and matches the compose database:

  | Host | Port | Database | User | Password |
  |---|---|---|---|---|
  | localhost | 5432 | developer_evaluation | developer | ev@luAt10n |

  It can be overridden with the environment variable `ConnectionStrings__DefaultConnection`.
- In the `Development` environment, **pending migrations are applied automatically on startup** (tables `Users`, `Sales`, `SaleItems`).
- To apply manually or create new migrations:

  ```bash
  dotnet tool install --global dotnet-ef --version 8.0.10
  dotnet ef database update --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
  ```

### Stopping

```bash
docker compose down        # stop containers (data is kept in the container until it is removed)
docker compose down -v     # stop and remove volumes
```

## Tests

### Unit tests

```bash
dotnet test Ambev.DeveloperEvaluation.sln
```

> The solution file must be specified because the folder also contains `docker-compose.dcproj`
> (otherwise: `MSB1011: Specify which project or solution file to use`).
> In Visual Studio: **Test → Test Explorer → Run All**.

Coverage report (template scripts):

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

### End-to-end tests

`scripts/sales-api-tests.ps1` runs **88 checks** against a running API and prints `PASS` / `FAIL` for each one:
health check, every discount tier boundary (3, 4, 5, 9, 10, 12, 20 items), all validation errors, 404/409 cases,
pagination, ordering, every filter (wildcards, ranges, cancelled flag), update synchronization, item and sale
cancellation rules, and delete.

```powershell
# API running locally (Visual Studio or dotnet run)
powershell -ExecutionPolicy Bypass -File .\scripts\sales-api-tests.ps1

# API running in Docker
powershell -ExecutionPolicy Bypass -File .\scripts\sales-api-tests.ps1 -BaseUrl http://localhost:8080

# Remove the sales created by the test run at the end
powershell -ExecutionPolicy Bypass -File .\scripts\sales-api-tests.ps1 -Cleanup
```

Each run uses unique sale numbers (`T<time>-N`) and only asserts on its own data, so it can be executed repeatedly
against a database that already has other sales. Expected output ends with `ALL 88 CHECKS PASSED`.

### Manual tests

- **Swagger**: `/swagger` (see [API reference](#api-reference) for payloads).
- **HTTP file**: `src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.http`
  (Visual Studio, Rider or VS Code REST Client) — requests are chained, the created sale id is reused automatically.

## API reference

Base path: `/api/sales`. Responses use the template envelope `{ success, message, data, errors }`.

| Method | Route | Description | Success |
|---|---|---|---|
| POST | `/api/sales` | Create a sale | 201 |
| GET | `/api/sales/{id}` | Get a sale | 200 |
| GET | `/api/sales` | List sales (paginated) | 200 |
| PUT | `/api/sales/{id}` | Update a sale | 200 |
| DELETE | `/api/sales/{id}` | Delete a sale | 200 |
| PATCH | `/api/sales/{id}/cancel` | Cancel a sale | 200 |
| PATCH | `/api/sales/{id}/items/{itemId}/cancel` | Cancel one item | 200 |

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
    { "productId": "22222222-2222-2222-2222-222222222222", "productName": "Skol 350ml",   "quantity": 5,  "unitPrice": 4.00 },
    { "productId": "33333333-3333-3333-3333-333333333333", "productName": "Stella 330ml", "quantity": 12, "unitPrice": 6.90 }
  ]
}
```

Response `201` (excerpt), with `Location: /api/Sales/{id}`:

```json
{
  "success": true,
  "message": "Sale created successfully",
  "data": {
    "id": "a2dd3e06-3905-4924-a781-ac5f968bdb61",
    "saleNumber": "S-0001",
    "totalAmount": 97.74,
    "isCancelled": false,
    "items": [
      { "productName": "Brahma 350ml", "quantity": 3,  "unitPrice": 4.5, "discountPercentage": 0,   "discount": 0,     "totalAmount": 13.5,  "isCancelled": false },
      { "productName": "Skol 350ml",   "quantity": 5,  "unitPrice": 4,   "discountPercentage": 0.1, "discount": 2,     "totalAmount": 18,    "isCancelled": false },
      { "productName": "Stella 330ml", "quantity": 12, "unitPrice": 6.9, "discountPercentage": 0.2, "discount": 16.56, "totalAmount": 66.24, "isCancelled": false }
    ]
  },
  "errors": []
}
```

Discounts and totals are always calculated by the API; they are not accepted as input.

### Update

`PUT /api/sales/{id}` takes the same body as create, without `saleNumber` (immutable).
Items are synchronized by `productId`:

- existing products are updated and keep their item id;
- new products are added;
- products not sent are removed;
- cancelled items are kept as history.

### Cancel

- `PATCH /api/sales/{id}/items/{itemId}/cancel` — marks the item as cancelled and recalculates the sale total. The sale stays active.
- `PATCH /api/sales/{id}/cancel` — marks the sale as cancelled. After that the sale cannot be updated or have items cancelled.

### List

Follows [General API](.doc/general-api.md) conventions:

| Parameter | Example |
|---|---|
| `_page`, `_size` (1–100, default 1 / 10) | `_page=2&_size=20` |
| `_order` | `_order=saleDate desc, saleNumber` — fields: `saleNumber`, `saleDate`, `customerName`, `branchName`, `totalAmount`, `isCancelled`, `createdAt`. Default: `saleDate desc` |
| `saleNumber`, `customerName`, `branchName` | exact match, or partial with `*`: `customerName=John*`, `branchName=*Savassi`, `branchName=*Centro*` (case-insensitive) |
| `customerId`, `branchId`, `isCancelled` | `isCancelled=false` |
| `_minSaleDate`, `_maxSaleDate` | `_minSaleDate=2026-01-01T00:00:00Z` |
| `_minTotalAmount`, `_maxTotalAmount` | `_minTotalAmount=100` |

Response:

```json
{
  "currentPage": 1,
  "totalPages": 1,
  "totalCount": 2,
  "data": [ { "id": "…", "saleNumber": "S-0001", "…": "…" } ],
  "success": true,
  "message": "",
  "errors": []
}
```

### Errors

| Situation | Status |
|---|---|
| Request / command validation (quantity > 20 or < 1, duplicated product line, no items, empty required fields, invalid paging) | 400 |
| Domain rule violation (modifying or re-cancelling a cancelled sale, re-cancelling an item) | 400 |
| Sale or item not found | 404 |
| Duplicated sale number | 409 |

Error body:

```json
{
  "success": false,
  "message": "Validation Failed",
  "errors": [ { "error": "LessThanOrEqualValidator", "detail": "It is not possible to sell more than 20 identical items." } ]
}
```

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
Failed operations do not publish events.

Example log line:

```
[INF] [Ambev.DeveloperEvaluation.Application.Sales.Events.SaleEventsLogHandler] Event SaleCreatedEvent published: {"SaleId": "…", "SaleNumber": "S-0001", …}
```

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `docker : The term 'docker' is not recognized` | Docker Desktop is not installed, or the terminal was opened before the installation — install it and reopen the terminal / Visual Studio |
| `failed to connect to the docker API at npipe:////./pipe/docker_engine` | Docker Desktop is not running — open it and wait for *Engine running* |
| Docker Desktop asks for WSL / virtualization | Run `wsl --install` in an elevated PowerShell and reboot; enable virtualization (VT-x / SVM) in the BIOS if required |
| `MSB1011: Specify which project or solution file to use` | Use `dotnet test Ambev.DeveloperEvaluation.sln` |
| Visual Studio shows "Select Startup Item" or runs `ORM` | Open the `.sln` and set **Ambev.DeveloperEvaluation.WebApi** as startup project |
| Swagger shows `Failed to fetch` | The API is not running (stopped or restarted after an edit) — start it again; if using https, try http://localhost:5119/swagger |
| API returns 500 on every call | PostgreSQL is not running — `docker ps` must list `ambev_developer_evaluation_database`; start it with `docker compose up -d ambev.developerevaluation.database` |
| Port 5432 already in use | Another PostgreSQL is running locally — stop it, or change the host port in `docker-compose.yml` and the connection string |
| Script blocked by execution policy | Run it with `powershell -ExecutionPolicy Bypass -File …` |

Known warnings (from the template, not related to the Sales feature): `NU1903` for AutoMapper 13.0.1, `CS8604` in
`JwtTokenGenerator.cs`, and "No test is available" for the empty Functional and Integration test projects.

## Design decisions and assumptions

- **"Above 4 items" vs "4+ items"**: the statement uses both; the explicit tier list (4+ → 10%) was adopted, so exactly 4 items get 10%.
- **Sale number** is provided by the client and must be unique (unique index + check in the handler).
- **Delete** is a hard delete (CRUD requirement). **Cancel** is the business operation that keeps the record.
- **Ids** of `Sale` and `SaleItem` are generated in the domain (`ValueGeneratedNever`), so items added to a tracked sale are inserted by EF Core instead of being treated as existing rows.
- **Dates** are normalized to UTC (required by Npgsql for `timestamp with time zone`).
- **Money** uses `decimal` / `numeric(18,2)`; discount rate is stored as `numeric(5,4)`.
- A single `SaleResult` / `SaleResponse` is shared by the use cases that return a sale, instead of one identical DTO per use case.
- Items have no position field, so their order in responses is not guaranteed.

## Changes to the template

Fixes needed for the template to run end-to-end:

- `appsettings.json`: connection string was SQL Server style with different credentials; now points to the compose PostgreSQL.
- `docker-compose.yml`: fixed host port mapping (`8080:8080`, `5432:5432`), API connection string, `depends_on` with a health check; removed the HTTPS port that required a Windows-mounted certificate.
- Migration `AddSales` also adds `Users.CreatedAt` / `Users.UpdatedAt`, which exist in the `User` entity but were missing from the initial migration (user inserts would fail).
- Design-time `DbContext` factory now uses the ORM assembly for migrations, same as `Program.cs`.
- `BaseController.Ok<T>` wraps its argument in `ApiResponseWithData<T>`; `OkPaginated` and the Sales endpoints return their envelopes directly to avoid a double-wrapped response.
- `ExceptionHandlingMiddleware` maps not found / domain / conflict exceptions to 404 / 400 / 409 instead of 500.