# Croatian Investment Tax Planner

[![CI](https://github.com/dandrlon/croatia-investment-tax-planner/actions/workflows/ci.yml/badge.svg)](https://github.com/dandrlon/croatia-investment-tax-planner/actions/workflows/ci.yml)

A Web API for Croatian retail investors who use Trading 212. Import your transaction history CSV export to get:

- **Dividends** per year and per position, with the 12% Croatian tax owed after credit for tax already withheld abroad.
- **The exemption date of every purchase lot.** Gains on shares held for more than two years are exempt from capital gains tax. Lots are matched FIFO.
- **The estimated capital gains tax if you sold everything today**, at prices you supply.

> **This is not tax advice. Verify all amounts with the Croatian Tax Administration (Porezna uprava) before filing.**

## Tech stack

- ASP.NET Core Web API on .NET 10, with OpenAPI and Swagger UI
- Entity Framework Core 10 with SQL Server 2022
- xUnit v3 unit tests, plus integration tests against a real SQL Server started with Testcontainers
- Docker Compose to run the API and the database with one command
- GitHub Actions to build and test on every push

## Quick start

You need [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
cp .env.example .env        # then edit the password in .env
docker compose up --build
```

Open <http://localhost:8080/swagger>. In Swagger:

1. `POST /api/imports`: upload your Trading 212 CSV export.
2. `GET /api/dividends`: dividends and tax per year. Add `?year=2025` for one year.
3. `GET /api/positions`: open positions, their lots and each lot's exemption date.
4. `POST /api/positions/tax-estimate`: send current prices in EUR per share, keyed by ISIN:

   ```json
   { "pricesEur": { "IE00B3XXRP09": 101.25, "US7561091049": 52.10 } }
   ```

Importing the same file again, or a newer export that overlaps an older one, is safe: rows that are already stored are counted as duplicates and skipped. Data is kept in a Docker volume between runs.

To try it without your own data, upload `tests/TaxPlanner.Core.Tests/Fixtures/sample-export.csv`.

## Exporting from Trading 212

In the Trading 212 app, go to **History → Export history**, choose the full date range, include orders and dividends, and download the CSV. Your account currency must be EUR.

## Running the tests

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), and Docker running for the integration tests.

```bash
dotnet test
```

## Project structure

```
src/
  TaxPlanner.Core            Domain types, tax rules and the CSV parser. No database or web dependencies.
  TaxPlanner.Infrastructure  EF Core DbContext, mappings and migrations.
  TaxPlanner.Api             Controllers, import service, OpenAPI and Swagger.
tests/
  TaxPlanner.Core.Tests      Unit tests for every tax rule and the parser.
  TaxPlanner.Api.Tests       HTTP-level tests against SQL Server in Docker.
```

The tax rules live in `Core` as plain static functions over lists of transactions, so each rule can be tested in isolation and read without knowing anything about the web or the database. The API stores only the imported transactions and computes every report from them on request.

## Tax rules

All rules, their status and their known limitations are documented in [docs/tax-rules.md](docs/tax-rules.md).

## Roadmap

1. **Phase 1 (this version):** Web API, CSV import, dividend tax, exemption dates, tax estimate, tests, Docker Compose, CI.
2. **Phase 2:** an Angular UI with file upload, dividends by year, and positions with their exemption dates.
3. **Phase 3:** deployment to Azure.
