const MAX_CONCURRENT_REQUESTS = 4;
const TOKEN_CAPACITY = 10;
const TOKENS_PER_SECOND = 10;
const MILLISECONDS_PER_TOKEN = 1000 / TOKENS_PER_SECOND;
const CONCURRENCY_RETRY_AFTER_MS = 100;
const MAX_RETRY_AFTER_MS = 1000;

type Admission = { accepted: true; release(): void } | { accepted: false; retryAfterMs: number };

// Each server owns one gate. Rejected requests never queue or consume a token;
// an accepted handler releases its concurrency slot in finally, on every outcome.
export function createCatalogAdmission(now: () => number) {
  let active = 0;
  let tokens = TOKEN_CAPACITY;
  let lastRefill = now();

  return {
    tryAcquire(): Admission {
      const current = Math.max(lastRefill, now());
      tokens = Math.min(TOKEN_CAPACITY, tokens + (current - lastRefill) / MILLISECONDS_PER_TOKEN);
      lastRefill = current;
      if (active >= MAX_CONCURRENT_REQUESTS) return { accepted: false, retryAfterMs: CONCURRENCY_RETRY_AFTER_MS };
      if (tokens < 1) {
        const retryAfterMs = Math.min(MAX_RETRY_AFTER_MS, Math.max(1, Math.ceil((1 - tokens) * MILLISECONDS_PER_TOKEN)));
        return { accepted: false, retryAfterMs };
      }
      tokens -= 1;
      active += 1;
      return { accepted: true, release: () => { active -= 1; } };
    },
  };
}
