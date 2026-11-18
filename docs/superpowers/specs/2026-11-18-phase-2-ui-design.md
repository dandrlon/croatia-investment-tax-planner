# Phase 2: Web UI – Design

*Date: 18 November 2026. Agreed between the author and Claude. Builds on `docs/design.md` (phase 2: "Angular UI: file upload, dividends by year, positions with their exemption dates").*

## Intent

Phase 1 delivered a Web API that you can only use through Swagger. Phase 2 adds a browser UI, so the author can open one page, see where he stands with his taxes, and upload a newer Trading 212 export whenever he has one.

For the portfolio, it shows employers that the author can build an Angular front end against his own ASP.NET Core API and ship both with Docker Compose and CI. Angular is common in the .NET job ads the author is targeting (GDi, Combis and others).

## The moment it is used

The author opens the app. The first thing he wants to see is the last CSV he uploaded (file name, date, what it brought in) and the numbers that come from it. If he has a newer export, he clicks one button and uploads it. The rest of the page then refreshes with the new numbers.

## Goals

A single page, top to bottom (the author chose this over a tabbed layout because there isn't much content):

1. **Last import bar.** It shows:
   - the file name of the last uploaded CSV,
   - the date and time it was uploaded,
   - how many rows were new, how many were already imported (duplicates), how many were skipped and how many had errors,
   - an **Upload new CSV** button.

   An error count above zero is highlighted as a warning, because a row with an error is a transaction missing from the tax figures.
2. **Upload result.** Right after an upload, a short summary appears under the bar, for example "37 new, 375 already imported, 2 skipped, 1 error". "Skipped" means rows that are not trades or dividends (deposits, interest); errors are counted separately. If some rows had errors, it lists them with their line numbers and messages. If the server rejected the whole file (400), it shows the server's message. The rest of the page then reloads its data.
3. **Warnings panel.** It collects every warning the reports return and starts collapsed, showing a count ("⚠ 2 warnings"). Clicking the count opens the list. Examples are an incomplete history from FIFO matching and a year with no verified tax rate.
4. **Three summary tiles:**
   - dividend tax due for the current year,
   - dividend tax due for the previous year (the one the author files next),
   - the next lot to become tax-free from capital gains tax, as ticker and date, e.g. "VHYL · 3 Mar 2027".
5. **Dividends by year.** A table with one row per year:
   - columns: gross, withheld abroad, Croatian tax and tax due;
   - clicking a year expands its per-position breakdown;
   - a year without a verified rate shows "—" instead of the tax and is marked with a warning.
6. **Positions.** A table with ticker, name, shares and a **Next tax-free date** column:
   - shows "all exempt" when every lot already is;
   - otherwise shows the earliest future exemption date and how many shares become exempt on it, e.g. "3 Mar 2027 (12 sh)". This is the same rule as the summary tile, as in the mockup the author approved;
   - clicking a position expands its individual lots: acquired date, shares, cost and exempt-from date, with the lots that are already exempt marked.
7. **Disclaimer.** The page footer always shows the disclaimer text that the API returns.
8. **Empty state.** If nothing has been imported yet, the page shows only an upload area, short instructions for exporting history from Trading 212, and the disclaimer.
9. **Tiles with no data.** A year with no dividends shows "€0.00". A year without a verified rate shows "—" with a warning. When no lot has a future exemption date, the tile says "All lots are tax-free".

## Non-goals

- The "estimated tax if sold today" feature. It needs current prices typed in by hand, and the phase 2 design does not ask for it. It stays available through Swagger.
- A history of past imports. Only the last import is shown.
- Editing or deleting imported data.
- Login, users, Croatian translations and charts.
- Everything from the non-goals in `docs/design.md`.

## Anti-goals

- **A number in the UI that differs from the API.** The UI only formats what the API returns and must never calculate tax itself. The one exception is choosing which values to show:
  - the current and previous year, chosen by the browser's date;
  - the earliest future exemption date.
- **Hiding a warning or the disclaimer.**
- **Front-end code the author cannot explain.** Use plain Angular without extra state-management libraries.

## Decisions (made by Claude, accepted by the author)

- **Framework.** Angular, latest stable version, with standalone components, signals and the built-in `HttpClient`. No NgRx or other state libraries.
- **Components.** Angular Material for buttons, tables, expansion panels, cards and the progress indicator.
- **Language.** All UI text is in English, like the rest of the repository.
- **Formatting.**
  - Amounts in EUR with 2 decimals, e.g. "€14.62".
  - Dates as "3 Mar 2027", which is unambiguous.
  - The page must stay readable on a phone-width screen.
- **API addition.** `GET /api/imports/latest` returns the last `ImportBatch`:
  - fields: `id`, `fileName`, `importedAtUtc`, `importedCount`, `duplicateCount`, `skippedCount` and `errorCount`;
  - returns `204 No Content` when nothing has been imported;
  - comes with xUnit integration tests like the other endpoints.

  `ImportBatch` gets a new `ErrorCount` column, with an EF Core migration, set by `ImportService` from the parser's row errors. Without it, a reload would hide that rows failed, which is an anti-goal. *(Claude's call, from the builder check.)* The details of skipped and failed rows are not stored, so the UI shows them only right after an upload, from the upload response. After a page reload, only the counts remain.
- **Existing endpoints used as they are:**
  - `POST /api/imports` (multipart form field `file`), which returns counts plus `skipped` and `errors`;
  - `GET /api/dividends`, which returns `disclaimer` and `years[]` with `positions[]` and `warnings[]`;
  - `GET /api/positions`, which returns `disclaimer`, `positions[]` with `lots[]` (`exemptFrom`, `isExemptToday`) and `warnings[]`.
- **Hosting.** A new `web` service in `compose.yaml` runs nginx:
  - nginx serves the built Angular app and forwards `/api/`, `/swagger` and `/openapi` to the `api` service;
  - the UI and the API therefore share one origin, so CORS is not needed;
  - `web` is published on `localhost:8080`, so the existing README links to `/swagger` keep working, and `api` no longer publishes a host port;
  - nginx's request size limit (`client_max_body_size`) is raised to 11 MB, because its default of 1 MB would reject exports the API accepts (up to 10 MB);
  - one `docker compose up --build` still starts everything.
- **Local development.** `ng serve` with a proxy configuration that forwards `/api` to `http://localhost:8080`, i.e. to the running Compose stack.
- **Tests.**
  - Unit tests for components and services, using the Angular CLI's default test runner.
  - Services are tested against the `HttpClient` testing utilities.
  - Components are tested for the behaviours listed in Goals: empty state, "—" for a year without a rate, expanded rows, upload error shown, and the warnings count.
- **CI.** A new GitHub Actions job installs Node dependencies with `npm ci`, runs the unit tests headless and builds the production bundle. The existing `docker` job also builds the `web` image.
- **Docs.** The README gains a screenshot of the page, the new run instructions (the UI at `localhost:8080`, Swagger still at `/swagger`) and a short description of the front-end structure.
- **Who writes the code.** Claude writes it, as in phase 1, and the author reviews it and asks questions until he can explain it.

## Constraints

- The author has about 2 hours a week. After each step the repository must build, pass CI and be presentable.
- The author is new to Angular, so keep the code small, conventional and close to the official Angular documentation.
- Do not change any tax rule or existing API response shape. Phase 2 only adds `GET /api/imports/latest` and the `ErrorCount` column behind it.

## Left to the builder

- The folder for the Angular workspace (for example `src/TaxPlanner.Web`) and its internal component structure.
- The exact Angular, Angular Material, Node and nginx versions, provided they are current stable releases at least two weeks old.
- The Material theme and colours, spacing and the wording of empty and loading states.
- How the UI shows loading and how it handles a network error (any clear message plus a retry is fine).
- Whether e2e tests are added. They are not required.
