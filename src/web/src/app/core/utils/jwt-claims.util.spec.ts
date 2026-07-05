import { describe, expect, it } from 'vitest';

import {
  extractAuthClaimsFromJwt,
  parseJwtPayload,
} from './jwt-claims.util';

function encodePayload(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
  return `${header}.${body}.signature`;
}

describe('jwt-claims.util', () => {
  it('parses platform admin and holder claims', () => {
    const jwt = encodePayload({
      sub: '0xabc',
      platform_admin: 'true',
      holder: 'false',
    });

    expect(extractAuthClaimsFromJwt(jwt)).toEqual({
      platformAdmin: true,
      holder: false,
      userId: null,
      memberships: [],
    });
  });

  it('parses user id and membership claims', () => {
    const jwt = encodePayload({
      user_id: '11111111-1111-1111-1111-111111111111',
      membership: [
        '11111111-1111-1111-1111-111111111111:admin',
        '22222222-2222-2222-2222-222222222222:issuer',
      ],
    });

    expect(extractAuthClaimsFromJwt(jwt)).toEqual({
      platformAdmin: false,
      holder: false,
      userId: '11111111-1111-1111-1111-111111111111',
      memberships: [
        {
          institutionId: '11111111-1111-1111-1111-111111111111',
          role: 'admin',
        },
        {
          institutionId: '22222222-2222-2222-2222-222222222222',
          role: 'issuer',
        },
      ],
    });
  });

  it('returns null for malformed tokens', () => {
    expect(parseJwtPayload('not-a-jwt')).toBeNull();
    expect(extractAuthClaimsFromJwt('not-a-jwt')).toBeNull();
  });
});
