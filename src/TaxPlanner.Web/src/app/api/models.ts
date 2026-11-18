// Types for the JSON the API returns. They mirror the C# records in TaxPlanner.Api and
// TaxPlanner.Core (camelCase). A C# `decimal?` is `number | null`; dates are ISO strings.

export interface ImportSummary {
  id: number;
  fileName: string;
  importedAtUtc: string;
  importedCount: number;
  duplicateCount: number;
  skippedCount: number;
  errorCount: number;
}

export interface SkippedRow {
  line: number;
  action: string;
  reason: string;
}

export interface ParseError {
  line: number;
  message: string;
}

export interface ImportResult {
  importId: number;
  imported: number;
  duplicates: number;
  skipped: SkippedRow[];
  errors: ParseError[];
}

export interface DividendPosition {
  isin: string;
  ticker: string;
  name: string;
  grossEur: number;
  withheldEur: number;
  croatianTaxEur: number | null;
  foreignTaxCreditEur: number | null;
  taxDueEur: number | null;
}

export interface DividendYear {
  year: number;
  rate: number | null;
  grossEur: number;
  withheldEur: number;
  croatianTaxEur: number | null;
  foreignTaxCreditEur: number | null;
  taxDueEur: number | null;
  positions: DividendPosition[];
  warnings: string[];
}

export interface DividendsResponse {
  disclaimer: string;
  years: DividendYear[];
}

export interface Lot {
  acquiredOn: string;
  quantity: number;
  costEur: number;
  exemptFrom: string;
  isExemptToday: boolean;
}

export interface Position {
  isin: string;
  ticker: string;
  name: string;
  quantity: number;
  costEur: number;
  lots: Lot[];
}

export interface PositionsResponse {
  disclaimer: string;
  positions: Position[];
  warnings: string[];
}
