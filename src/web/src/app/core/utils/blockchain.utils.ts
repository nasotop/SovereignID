export function guidToBytes32(guid: string): string {
  const normalized = guid.replace(/-/g, '').toLowerCase();
  return `0x${normalized.padStart(64, '0')}`;
}

export async function sha256HexFromString(value: string): Promise<string> {
  const data = new TextEncoder().encode(value);
  const digest = await crypto.subtle.digest('SHA-256', data);
  const bytes = Array.from(new Uint8Array(digest));
  return `0x${bytes.map((byte) => byte.toString(16).padStart(2, '0')).join('')}`;
}

export function canonicalizeJson(value: unknown): string {
  return JSON.stringify(value, Object.keys(value as object).sort());
}
