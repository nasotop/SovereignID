import { InstitutionMembership } from '../models/auth.models';

export interface JwtAuthClaims {
  readonly platformAdmin: boolean;
  readonly holder: boolean;
  readonly userId: string | null;
  readonly memberships: readonly InstitutionMembership[];
}

const PLATFORM_ADMIN_CLAIM = 'platform_admin';
const HOLDER_CLAIM = 'holder';
const USER_ID_CLAIM = 'user_id';
const MEMBERSHIP_CLAIM = 'membership';

export function parseJwtPayload(jwt: string): Record<string, unknown> | null {
  const segments = jwt.split('.');
  if (segments.length < 2) {
    return null;
  }

  try {
    const normalized = segments[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(
      normalized.length + ((4 - (normalized.length % 4)) % 4),
      '=',
    );
    const json = atob(padded);
    const payload = JSON.parse(json) as unknown;
    return payload && typeof payload === 'object'
      ? (payload as Record<string, unknown>)
      : null;
  } catch {
    return null;
  }
}

export function extractAuthClaimsFromJwt(jwt: string): JwtAuthClaims | null {
  const payload = parseJwtPayload(jwt);
  if (!payload) {
    return null;
  }

  return {
    platformAdmin: readBooleanClaim(payload, PLATFORM_ADMIN_CLAIM),
    holder: readBooleanClaim(payload, HOLDER_CLAIM),
    userId: readStringClaim(payload, USER_ID_CLAIM),
    memberships: readMembershipClaims(payload),
  };
}

function readBooleanClaim(
  payload: Record<string, unknown>,
  claim: string,
): boolean {
  const value = payload[claim];
  return value === true || value === 'true';
}

function readStringClaim(
  payload: Record<string, unknown>,
  claim: string,
): string | null {
  const value = payload[claim];
  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  return value;
}

function readMembershipClaims(
  payload: Record<string, unknown>,
): InstitutionMembership[] {
  const raw = payload[MEMBERSHIP_CLAIM];
  if (!raw) {
    return [];
  }

  const values = Array.isArray(raw) ? raw : [raw];
  const memberships: InstitutionMembership[] = [];

  for (const value of values) {
    const parsed = parseMembershipClaim(value);
    if (parsed) {
      memberships.push(parsed);
    }
  }

  return memberships;
}

function parseMembershipClaim(value: unknown): InstitutionMembership | null {
  if (typeof value !== 'string') {
    return null;
  }

  const separatorIndex = value.indexOf(':');
  if (separatorIndex <= 0 || separatorIndex >= value.length - 1) {
    return null;
  }

  const institutionId = value.slice(0, separatorIndex).trim();
  const role = value.slice(separatorIndex + 1).trim().toLowerCase();
  if (!institutionId || !role) {
    return null;
  }

  return { institutionId, role };
}
