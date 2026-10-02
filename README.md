🌐 [English](README.md) | [Українська](README.uk.md)

# Inflation Monitor API (MVP)

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-9-512BD4?logo=dotnet)](https://learn.microsoft.com/en-us/ef/core/)
[![SQLite](https://img.shields.io/badge/DB-SQLite-003B57?logo=sqlite)](https://www.sqlite.org/)
[![OpenAPI](https://img.shields.io/badge/API%20Docs-Swagger%2FOpenAPI-85EA2D?logo=swagger)](https://swagger.io/)
[![License: CC BY-NC-ND 4.0](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-nd/4.0/deed.uk)

A REST API that shows how the purchasing power of a fixed amount in Ukrainian hryvnias (UAH) changed over a chosen period compared with inflation and various financial instruments for passive savings (see section 1, "Supported instruments").  

**Status:** early working version (MVP).

---

## 1. Purpose

The service answers the question: "What happened to my money if I just kept it in hryvnias, and how does that look against the alternatives?". You specify an amount in hryvnias and a period, and in response you get:

| Response field | What it means |
| :--- | :--- |
| `cashGrivna` | The nominal amount, which has not changed ("money under the mattress"). |
| `inflationEquivalent` | How many hryvnias would be needed at the end of the period to buy the same thing that the initial amount could buy at the beginning of the period. |
| `usdEquivalent` | How many hryvnias you would get if, at the beginning of the period, the entire amount had been exchanged for dollars, and at the end exchanged back into hryvnias. |
| `eurEquivalent` | The same for euros. |

**How to assess the result.** If `usdEquivalent` is higher than `inflationEquivalent`, then keeping money in dollars protected capital from inflation better than the nominal hryvnia did. If `cashGrivna` is lower than `inflationEquivalent`, then the money "under the mattress" lost purchasing power.

### Supported instruments

| Category | Code | Description | Data available from |
| :--- | :--- | :--- | :--- |
| `Inflation` | `CPI` | Consumer Price Index | January 2000 |
| `Currencies` | `USD` | US dollar, commercial UAH/USD exchange rate | September 1996 |
| `Currencies` | `EUR` | Euro, commercial UAH/EUR exchange rate | January 1999 |

**Data currency by instrument:** up to and including 2026-06. 

---

## 2. Quick start

### Requirements

* **.NET SDK 9.0**

### Running 

From the repository root:
```bash
dotnet run --project InflationMonitor.WebApi --launch-profile https
```

No additional configuration is needed: the SQLite connection string is already set in `appsettings.json`, and on the first run the database is created and populated with data automatically.

* **Swagger UI:** `https://localhost:7241/swagger`
* **Profile without HTTPS:** `--launch-profile http`, address `http://localhost:5180/swagger`.
* On the first run over HTTPS you may need to trust the dev certificate: `dotnet dev-certs https --trust`.
* The profiles set `ASPNETCORE_ENVIRONMENT=Development`, so Swagger, migrations, and seeding are enabled. If you run without a profile (`--no-launch-profile`) or from a published build, the environment will be `Production`: Swagger and automatic database population will not work.

### Running the tests

```bash
dotnet test
```

---

## 3. API

### `GET /api/calculator/compare`

#### Query parameters

| Parameter | Type | Required | Description | Example |
| :--- | :--- | :--- | :--- | :--- |
| startDate | string (yyyy&#8209;MM&#8209;dd) | yes | Start of the period. Only the year and month are taken into account; the date is normalized to the 1st. | 2023&#8209;01&#8209;01 |
| endDate | string (yyyy&#8209;MM&#8209;dd) | yes | End of the period (also only the year and month). | 2023&#8209;02&#8209;01 |
| amount | decimal | yes | Initial amount in UAH, strictly greater than 0. | 1000 |

#### Validation rules (`400` error)

* `amount` > 0;
* `startDate` ≤ `endDate`;
* `startDate` not earlier than September 1996 (a common boundary for all instruments, the introduction of the hryvnia);
* `endDate` not later than the current month (UTC).

> Boundaries for individual instruments (EUR from 1999-01, CPI from 2000-01) do **not** result in an error. In that case the response is `200`, the instrument's value is `null`, and the reason is given in `warnings`.

#### Example request

```http
GET /api/calculator/compare?startDate=2011-01-01&endDate=2011-02-01&amount=1000 HTTP/1.1
Host: localhost:7241
```

#### Successful response (`200 OK`)



```json
{
  "startDate": "2011-01-01",
  "endDate": "2011-02-01",
  "initialAmount": 1000,
  "summary": {
    "cashGrivna": 1000,
    "inflationEquivalent": 1019.09,
    "usdEquivalent": 997.5,
    "eurEquivalent": 1028.38
  },
  "warnings": []
}
```

Control calculation:

| Instrument | Value at<br>start date | Value at<br>end date | Calculation / Result |
| --- | --- | --- | --- |
| **CPI** | 1,01  | 1,009 | <nobr>1000*1,01*1,009=1019,09</nobr>|
| **USD** | 7,99  | 7,97  | <nobr>1000/7,99*7,97=997,50</nobr>|
| **EUR** | 10,57 | 10,87 | <nobr>1000/10,57*10,87=1028,38</nobr>|

#### Response with warnings (partial data)

If the requested period goes beyond the available data for an instrument, the corresponding `summary` field is `null`, and the reason is given in `warnings`.

```json
{
  "startDate": "1997-01-01",
  "endDate": "2023-01-01",
  "initialAmount": 10000,
  "summary": {
    "cashGrivna": 10000,
    "inflationEquivalent": null,
    "usdEquivalent": 213421.05,
    "eurEquivalent": null
  },
  "warnings": [
    "Historical inflation data for 'CPI' is available only starting from 2000-01, but 1997-01 was requested.",
    "Historical exchange rate data for 'EUR' is available only starting from 1999-01, but 1997-01 was requested."
  ]
}
```

#### Error Handling

Errors are returned in the [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807) format (`ProblemDetails`).

| Status | `title` | When it occurs |
| :--- | :--- | :--- |
| `400` | `Validation Error` | Validation rules were violated. Details are in the `errors` field. |
| `400` | `Domain Error` | A domain rule was violated (the text is in `detail`). |
| `400` | (standard ASP.NET Core) | A parameter could not be parsed, for example, an incorrect date format. The body format is standard for ASP.NET Core (`ValidationProblemDetails`). |
| `500` | `Server Error` | An unexpected error. Internal details are not passed to the client. |

Example of a `400 Validation Error` for `amount=0`:

```json
{
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "instance": "/api/calculator/compare",
  "errors": {
    "Amount": ["Amount must be greater than zero."]
  }
}
```

---

## 4. Calculation methodology and data

### 4.1. Formulas

| Metric | Formula |
| :--- | :--- |
| CPI (`inflationEquivalent`) | `amount × ∏ rate`, the product of monthly coefficients for all months in the range |
| Currency (`usdEquivalent`, `eurEquivalent`) | `amount ÷ rate_start × rate_end`, where `rate` is the exchange rate in hryvnias per unit of currency |

Results are rounded to 2 decimal places.

Example on test data: `1000 × 1.01 × 1.02 = 1030.20` for CPI and `1000 ÷ 36.5 × 37.0 = 1013.70` for USD.

**Which months are included in the period.** For CPI, the coefficients of every month in the range are applied, **including the start and end months**. For currencies, two values are taken: the rate of the start month and the rate of the end month. Therefore, when `startDate = endDate`, the currency equivalent equals the initial amount, and the CPI equivalent equals the amount multiplied by that month's coefficient. This is a description of the current implementation.

### 4.2. Data format

| Data | `Rate` field | Example |
| :--- | :--- | :--- |
| Inflation | Monthly multiplier relative to the previous month | `1.012` means +1.2% per month |
| Exchange rates | Hryvnias per 1 unit of currency | `36.5` means 36.5 UAH per 1 USD |

All dates are stored normalized to the first day of the month (`yyyy-MM-01`). 
The "currency + month" pair and the inflation month are unique (unique indexes in the DB). Duplicates during data import are rejected with an error.
The datasets are continuous: there is a value for every month within the available range. The calculation relies on this condition; if a gap does occur, CPI will return `null` with a warning about incomplete data rather than calculating from an incomplete series.

### 4.3. Methodological decisions

**Average monthly rates.** Currency calculations are based on average monthly rates rather than daily quotes. Regardless of the day in the request, the rate for the whole month is applied. For a macro-level analysis of the purchasing power of savings, storing daily quotes is excessive, and monthly data is enough to assess long-term trends.

**Commercial, not official rate.** Commercial (market) rates are used rather than the official NBU rate. In Ukraine's history there were periods of notable divergence between the official and the real exchange rate, and it is the market rate that reflects the rate at which currency could actually be exchanged.

**Two levels of date restrictions:**

1. *Common boundary (400 error).* A start date earlier than September 1996 is rejected by the validator before the calculation runs. The domain entities `ExchangeRate` and `InflationRate` additionally guard the same boundaries when records are created, for example, during seeding.
2. *Instrument boundary (warning).* If the requested period goes beyond the data for a specific instrument, no calculation is performed for it, the value is `null`, and the reason is stated in `warnings`. The remaining instruments are calculated as usual.

---

## 5. Architecture

The project is built as a layered application in the spirit of Clean Architecture. The Application layer works with data through the `IApplicationDbContext` abstraction on top of EF Core. This is a pragmatic compromise: DbContext already implements the Unit of Work and Repository patterns, so a separate repository layer was not introduced.

### 5.1. Solution structure

```
InflationMonitor/
├── InflationMonitor.Domain/        # Entities, domain rules (invariants), domain exceptions
├── InflationMonitor.Application/   # Queries and handlers (MediatR), calculation strategies, factory,
│                                   # DTOs, validation (FluentValidation), caching
├── InflationMonitor.Persistence/   # EF Core, entity configurations, SQLite, seeding from JSON
├── InflationMonitor.WebApi/        # Controllers, Swagger, global error handler, Program.cs
└── InflationMonitor.Tests/         # Unit and integration tests (xUnit)
```

Direction of dependencies: `Domain` ← `Application` ← `Persistence` and `WebApi`.

### 5.2. Request path

```
HTTP GET → ComparisonController
         → MediatR → ValidationBehavior (FluentValidation) → CalculateComparisonQueryHandler
         → FinancialInstrumentFactory → InflationStrategy / BatchCurrencyStrategy
         → IMemoryCache → (on a miss) SQLite via EF Core
```

Any exception is intercepted by `GlobalExceptionHandler` and converted into `ProblemDetails`.

### 5.3. Technology stack

| Technology | Role |
| :--- | :--- |
| .NET / ASP.NET Core | Web API |
| MediatR | Request dispatching, cross-cutting concerns via pipeline behaviors |
| FluentValidation | Request validation before the handler is called (fail-fast) |
| Entity Framework Core + SQLite | Storage of historical data, configurations via `IEntityTypeConfiguration<T>` |
| `IMemoryCache` | Caching of historical data |
| Swagger / OpenAPI | Interactive documentation (in the Development environment) |
| xUnit, Moq, FluentAssertions | Testing |

Only the "read" part of CQRS (calculations) is implemented in the queries; there are no commands that modify data.

### 5.4. Key decisions

#### Strategies and factory

Each instrument category implements `IBatchFinancialInstrumentStrategy` (`InflationStrategy`, `BatchCurrencyStrategy`). `FinancialInstrumentFactory` selects the required strategy by the category key. A new category is added by a new strategy and its registration in DI. At the same time, the response structure (`FinancialComparisonSummaryDto`) and the query handler still know about the instruments explicitly, so adding a new instrument affects them as well (see section 7, "Known limitations").

#### Caching

To reduce the load on SQLite, `IMemoryCache` is used with the following parameters:

* limit: 22,000 entries, the weight of each entry equals 1;
* when the limit is reached, 20% of the entries are evicted from the cache (compaction);
* expired entries are scanned once every 15 minutes;
* entries with historical values (CPI, exchange rates) live for up to 10 days (absolute lifetime);
* the date of the latest available data for a currency is cached separately for 12 hours.

<details>
<summary>Rationale for the 22,000 limit</summary>

This is headroom for future expansion: up to 30 instruments × 50 years × 12 months = 18,000 entries plus a technical reserve. Currently (CPI and two currencies over several decades) the cache holds about a thousand entries.

At an estimate of about 300 bytes per entry, the full dataset would take up about 6 MB. This is a rough estimate; actual consumption depends on the `MemoryCache` overhead. Thus, the entire historical slice should fit in memory, and consumption remains modest.

</details>

<details>
<summary>Why the entries have this lifetime</summary>

Historical values for past months by design do not change: data is only accumulated (append-only), so long-term storage in the cache is safe and speeds up calculations. The cache is stored in the memory of a single process.

</details>

#### Error handling

`GlobalExceptionHandler` logs exceptions and converts them into `ProblemDetails`: validation errors and domain errors become a `400` response, everything else a `500` response with a generic message so as not to expose internal details. Parameter binding errors (for example, an incorrect date format) are handled by ASP.NET Core itself.

---

## 6. Testing

* **Unit tests:** domain entities, the request validator, `ValidationBehavior`, the query handler, the strategy factory, both calculation strategies (including the cache, warnings about data boundaries, and request cancellation), `GlobalExceptionHandler`, the seeder.
* **Integration tests:** end-to-end HTTP scenarios via `WebApplicationFactory` (successful calculation, partial data, validation errors, a 500 response) and verification of DB constraints (unique indexes) on in-memory SQLite.

Run: `dotnet test`.

---

## 7. Known limitations and plans

### Known limitations

* **Accuracy is one month.** Days within a month are not taken into account, even though the parameter format includes the day.
* **No data updates.** Data is loaded from JSON only into empty tables. There is no automatic import of new months, so the data has to be updated manually.
* **Migrations and seeding only in Development.** For other environments, the database deployment procedure has to be organized separately.
* **The cache is not shared between instances.** It is local to the process. After data in the DB is updated, values in the cache may be stale for up to 10 days.
* **Extending instruments requires contract changes.** A new instrument means a new strategy, as well as a change to the response DTO and the query handler.
* **Missing parameters.** A missing parameter is not rejected at the binding stage; a default value is substituted, and the regular validation kicks in. The error text in that case may be non-obvious.

### Ideas for the future

These are ideas, not commitments:

* government bonds (`Bonds`);
* precious metals (`PreciousMetals`);
* bank deposits (`Deposits`), for example, the average rate on one-year deposits of individuals (`DEP_UAH_1Y`);
* stock indices (`MarketIndices`);
* cryptocurrencies;
* automatic data updates.

---

## 8. Data sources

1. [National Bank of Ukraine](https://bank.gov.ua/)
2. [Minfin: inflation in Ukraine](https://index.minfin.com.ua/economy/index/inflation/)
3. [Minfin: currency exchange rates](https://index.minfin.com.ua/ua/currency/)
4. [Finance.ua: currency exchange rates](https://finance.ua/currency)

---

## 9. Disclaimer

The service is for informational purposes only and is **not financial advice**. The calculations are based on averaged monthly data and may differ from the real results of transactions with assets.

---

## 10. License and contacts

The project is distributed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0) license.

**Permitted:**

* studying the source code;
* running the project locally for yourself, for learning, and for non-commercial purposes;
* distributing unmodified copies with attribution and a link to the license.

**Prohibited without the author's written permission:**

* any commercial use, including paid access to a deployed API for third parties;
* distribution of modified versions and derivative works.

**Data.** The license applies to the source code. The historical data in `Seeding/Data` was obtained on the basis of third-party sources (see section 8), and the terms of use of those sources apply to it.

**Contacts:** [Oleksii Fodorov](https://github.com/AleFF88).