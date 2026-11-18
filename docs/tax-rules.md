# Tax rules

> **This is not tax advice.** Verify all amounts with the Croatian Tax Administration (Porezna uprava) before filing.

This page lists every rule the app applies and the one place in the code where it lives. Rules marked **To verify** are working assumptions that have not yet been checked against an official source. When one is checked, add the source URL and the date it was checked.

## Rules

| # | Rule | Status | Code |
|---|------|--------|------|
| A1 | Capital income (dividends and capital gains) is taxed at 12% from 2024. Years before 2024 have no configured rate, so the app shows no tax for them and gives a warning. | To verify | `TaxRates.CapitalIncomeRate` |
| A2 | Tax withheld abroad is credited per dividend payment, up to the Croatian tax on that payment: `credit = min(withheld, gross × rate)`. | To verify | `DividendTaxCalculator` |
| A3 | A lot is exempt from capital gains tax when sold **more than** two years after it was bought. The first exempt day is `acquiredOn + 2 years + 1 day`. | To verify | `HoldingPeriod` |
| A4 | The acquisition date and the dividend payment date are the date in Croatia (`Europe/Zagreb`) of Trading 212's UTC timestamp. | To verify | `CroatianCalendar` |
| A5 | In a Trading 212 dividend row, `Total` is the net amount in EUR and `Withholding tax` is the amount withheld in `Currency (Withholding tax)`. Gross = net + withholding in EUR. | To verify | `Trading212CsvParser` |
| A6 | Trading 212's `Exchange rate` is units of the instrument currency per 1 EUR, so a USD amount converts as `usd / rate`. | To verify | `Trading212CsvParser` |
| A7 | EUR amounts come from Trading 212's own `Total`, which uses its own exchange rate and includes its FX fee. No HNB/ECB reference rate is applied. | Known limitation | `Trading212CsvParser` |
| A8 | The cost basis of a buy is its `Total` in EUR. The proceeds of a sell are its `Total` in EUR. | To verify | `FifoLotMatcher` |
| — | Sales are matched to purchases first-in, first-out (FIFO), per ISIN. | From the design | `FifoLotMatcher` |
| — | Within a calendar year, gains and losses on non-exempt sales offset each other. Exempt lots are left out. A net loss gives zero tax, not a refund. | From the design | `SaleEstimator` |

## Open questions

- Which form is used to report foreign dividends, and what is the deadline? The app does not generate forms.
- How exactly is the foreign tax credit applied (A2), for example per payment, per country or per year?

## Known limitations

- **Exchange rates (A7).** Amounts use Trading 212's conversion, not the official reference rate on the payment date.
- **Years before 2024.** No tax is calculated for them until their rate is verified and added to `TaxRates`.
- **EUR accounts only.** A row whose `Currency (Total)` is not EUR is reported as an error.
- **Trading 212 only.** Other brokers are not supported.
- **Corporate actions.** Stock splits, mergers and other actions are not applied. They appear in the import result's `skipped` list, so the shares held may be wrong for an instrument that had one.
- **Incomplete history.** If the export starts after the first purchase, a sale can be bigger than the shares on record. The app matches what it has and returns a warning.
- **Current prices.** The "if sold today" estimate uses prices you supply in the request. The app does not fetch market data.
