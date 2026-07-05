export type ReportSource = 'snapshot' | 'live' | 'hybrid';

export interface ReportPeriod {
  from: string;
  to: string;
}

export interface TimeSeriesPoint {
  date: string;
  value: number;
}

export interface TimeSeriesReport {
  period: ReportPeriod;
  source: ReportSource;
  total: number;
  series: readonly TimeSeriesPoint[];
}

export interface VerificationOutcomePoint {
  date: string;
  valid: number;
  invalid: number;
}

export interface VerificationOutcomesReport {
  period: ReportPeriod;
  source: ReportSource;
  validTotal: number;
  invalidTotal: number;
  series: readonly VerificationOutcomePoint[];
}

export interface CredentialsPerStudentReport {
  asOf: string;
  totalStudents: number;
  totalCredentials: number;
  averageCredentialsPerStudent: number;
}

export interface PlatformRankingItem {
  institutionId: string;
  displayName: string;
  total: number;
}

export interface PlatformCredentialsByInstitutionReport {
  period: ReportPeriod;
  source: ReportSource;
  items: readonly PlatformRankingItem[];
}

export interface PlatformStudentsItem {
  institutionId: string;
  displayName: string;
  totalStudents: number;
}

export interface PlatformStudentsByInstitutionReport {
  asOf: string;
  items: readonly PlatformStudentsItem[];
}

export type ReportPeriodPreset = 7 | 30 | 90;

export interface ReportPeriodRange {
  from: string;
  to: string;
}

export function formatDateOnly(date: Date): string {
  return date.toISOString().slice(0, 10);
}

/** Inclusive calendar-day range ending today (local UTC date slice). */
export function computeReportPeriod(preset: ReportPeriodPreset): ReportPeriodRange {
  const to = new Date();
  const from = new Date(to);
  from.setDate(from.getDate() - (preset - 1));
  return { from: formatDateOnly(from), to: formatDateOnly(to) };
}
