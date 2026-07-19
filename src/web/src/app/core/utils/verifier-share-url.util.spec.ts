import { describe, expect, it } from 'vitest';

import { buildVerifierShareUrl } from './verifier-share-url.util';

describe('buildVerifierShareUrl', () => {
  const credentialId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
  const origin = 'https://sovereignid.example.com';

  it('builds absolute verifier URL with credentialId query param', () => {
    expect(buildVerifierShareUrl(credentialId, origin)).toBe(
      `${origin}/verifier?credentialId=${credentialId}`,
    );
  });

  it('trims whitespace from credentialId', () => {
    expect(buildVerifierShareUrl(`  ${credentialId}  `, origin)).toBe(
      `${origin}/verifier?credentialId=${credentialId}`,
    );
  });

  it('encodes special characters in credentialId', () => {
    const encodedId = '00000000-0000-0000-0000-000000000000';
    const url = buildVerifierShareUrl(encodedId, origin);
    const parsed = new URL(url);

    expect(parsed.pathname).toBe('/verifier');
    expect(parsed.searchParams.get('credentialId')).toBe(encodedId);
  });
});
