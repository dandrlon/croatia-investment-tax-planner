# Croatian Investment Tax Planner – Design

*Date: 18 November 2026. Author: David Andrlon (github.com/dandrlon), agreed with Claude.*

## Purpose

This is a GitHub portfolio project. Its main goal is to show employers (.NET roles in Croatia and remote, e.g. GDi, DEKRA, Abysalto, Combis) that the author can work with a modern .NET stack: ASP.NET Core, EF Core, automated tests, Docker and CI/CD. These are the skills currently missing from his CV.

The domain is tax for Croatian retail investors who use Trading 212. The author holds ETFs and stocks long term and does not plan to sell, but he receives dividends (VUSA, VHYL, Realty Income, General Mills) that he has not reported yet. Dividends are therefore the main feature, not sale planning.

## Goals

- The user imports a Trading 212 transaction history CSV export.
- The app shows **dividends** per year and per position, with the 12% Croatian tax owed on them.
- For every purchase (lot), the app shows **the date it becomes exempt from capital gains tax** (held for more than 2 years) and the estimated tax if the position were sold today (12% on gains from lots held 2 years or less, FIFO).
- The code is clean and easy to explain in a job interview.

## Non-goals

- Generating official tax forms (JOPPD, INO-DOH) or HUB-3A payment slips. Existing services such as prijavidobit.com already do this.
- Brokers other than Trading 212 (IBKR, Revolut, eToro) in the first version.
- Multiple users, registration or login. The app has a single user.
- Commercialisation.
- HIR (the Croatian tax-free investment account planned from 2027).

## Anti-goals (what would make this a failure)

- Wrong tax figures without a warning. The app must clearly state that it is not tax advice and that amounts should be checked with the Croatian Tax Administration (Porezna uprava).
- Code the author does not understand well enough to explain in an interview.
- A project that never reaches a usable version because the scope is too large.

## Constraints

- The author has about **2 hours per week**, sometimes more, sometimes less. Work is split into phases, and after each phase the repository must be in a working, presentable state.
- The author knows C#, SQL Server and Windows Forms well and ASP.NET Core at a basic level. EF Core, Docker, Angular and CI/CD are new to him.
- Everything in the repository (code, comments, commit messages, README, docs) is in **English**. Croatian terms are kept only where they are proper names (JOPPD, Porezna uprava, eRačun).

## Technology (proposed by Claude, accepted by the author)

- **Backend:** ASP.NET Core Web API on .NET 10, with OpenAPI/Swagger.
- **Database:** Entity Framework Core with SQL Server.
- **Tests:** xUnit, with a focus on the tax rules.
- **Docker:** Docker Compose that starts the API, the database and (from phase 2) the frontend with one command.
- **CI:** GitHub Actions that runs the build and tests on every push.
- **Frontend (phase 2):** Angular. Can be replaced with something simpler if it proves too much.
- **Later (phase 3):** deployment to Azure.

## Phases

1. **Phase 1 (about 6–8 weeks):** Web API, Trading 212 CSV import, transaction model in the database, dividend and dividend tax calculation, exemption date for every lot (FIFO), estimated tax if sold today, xUnit tests for all rules, Docker Compose, GitHub Actions, and a README with a description, run instructions and Swagger screenshots.
2. **Phase 2:** Angular UI: file upload, dividends by year, positions with their exemption dates.
3. **Phase 3:** Azure deployment and polish.

## Tax rules (verify before implementing)

- Dividends: taxed at 12%, with no exemption after 2 years.
- Capital gains on financial assets: taxed at 12% if sold within 2 years of acquisition; gains on assets held for more than 2 years are exempt.
- Gains and losses within the same calendar year are offset against each other.
- Lots are matched on sale using FIFO.
- **Open for research:** how to credit dividend tax already withheld abroad (e.g. in the US), and the deadline and form for reporting foreign dividends. Check the Croatian Tax Administration website before implementing.
- For Irish-domiciled ETFs (VUSA, VHYL) Trading 212 usually withholds no tax, while for US stocks (O, GIS) it does. The CSV import needs to tell these apart.

## Left to the builder

- Solution structure (projects, layers) and database schema.
- The exact Trading 212 CSV format: check against the author's real export.
- Currencies: Trading 212 exports amounts in the account currency and the instrument currency. Decide which exchange rate is used to convert to EUR and document it.

## Open question for the author

- Who writes the code: Claude writes most of it and the author reviews and runs it, or the author writes it with Claude's guidance? This affects both pace and how much the author learns. Claude's recommendation: Claude writes, and after each step the author asks for explanations until he can explain the code himself.
