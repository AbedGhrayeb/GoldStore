/** Host (platform admin) endpoints live under /host/api/v1 and use cookie auth, never bearer. */
export function isHostRequest(url: string): boolean {
  return url.startsWith('/host/') || /^https?:\/\/[^/]+\/host\//.test(url);
}
