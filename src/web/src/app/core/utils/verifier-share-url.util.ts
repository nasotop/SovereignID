export function buildVerifierShareUrl(
  credentialId: string,
  origin: string = window.location.origin,
): string {
  const url = new URL('/verifier', origin);
  url.searchParams.set('credentialId', credentialId.trim());
  return url.toString();
}
