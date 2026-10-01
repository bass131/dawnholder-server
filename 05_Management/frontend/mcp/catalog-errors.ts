export const ERROR_MESSAGES = {
  INVALID_ARGUMENT: '조회 인자 형식과 범위를 확인하세요.',
  CATALOG_MISSING: '기록 카탈로그가 없습니다.',
  CATALOG_UNREADABLE: '기록 카탈로그를 읽지 못했습니다. 다시 요청하세요.',
  CATALOG_TOO_LARGE: '기록 카탈로그가 2 MiB 한도를 넘습니다.',
  CATALOG_INVALID: '기록 카탈로그의 JSON, 스키마 또는 ID가 올바르지 않습니다.',
  CATALOG_REFERENCE_BROKEN: '기록 카탈로그에 끊긴 참조가 있습니다.',
  CATALOG_CHANGED_DURING_READ: '읽는 동안 기록 카탈로그가 변경됐습니다. 다시 요청하세요.',
  NOT_FOUND: '요청한 ID가 기록 카탈로그에 없습니다.',
  VERSION_CONFLICT: '카탈로그 버전이 다릅니다. 새 목록에서 버전을 선택하세요.',
  VERSION_REQUIRED: '뒤 페이지 조회에는 앞 응답의 expectedHash가 필요합니다.',
  RESPONSE_TOO_LARGE: '조회 결과가 응답 한도를 넘습니다. 본문을 반환하지 않습니다.',
  RATE_LIMITED: '동시 처리 또는 호출 빈도 한도를 넘었습니다. 잠시 후 다시 요청하세요.',
  REQUEST_CANCELLED: '조회 요청이 취소됐습니다.',
} as const;

export type CatalogErrorCode = keyof typeof ERROR_MESSAGES;
export type ErrorDetails = Record<string, string | number | boolean | string[]>;

export class CatalogReadError extends Error {
  constructor(readonly code: CatalogErrorCode, readonly details?: ErrorDetails) {
    super(ERROR_MESSAGES[code]);
  }
}

export function checkCancelled(signal: AbortSignal): void {
  if (signal.aborted) throw new CatalogReadError('REQUEST_CANCELLED');
}

export function errorIsRetryable(code: CatalogErrorCode): boolean {
  return ['CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ', 'RATE_LIMITED'].includes(code);
}
