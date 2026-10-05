export const MAX_SVG_BYTES = 256 * 1024;
export const DIAGRAM_TIMEOUT_MS = 5000;
export const byteLength = (value: string): number => new TextEncoder().encode(value).byteLength;

export function diagramPolicyError(source: string): string | null {
  if (byteLength(source) > 4096 || !source.trim()) return '원천은 비어 있지 않은 UTF-8 4 KiB 이하 텍스트여야 합니다.';
  const first = source.trim().split(/\r?\n/, 1)[0] ?? '';
  if (!/^(flowchart\s+(?:TD|TB|BT|LR|RL)|sequenceDiagram|stateDiagram-v2)\s*$/.test(first)) return '호출·패킷·상태 도식 3종만 표시합니다.';
  if (/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(source)) return '제어 문자는 허용하지 않습니다.';
  if (/<|&(?:#\w+|[a-z]+);|%%\s*\{|^\s*---|https?:|file:|data:|javascript:|ftp:|\/\/|[`$@]/im.test(source)) return 'HTML·설정·외부 URL·Markdown 문자열·수식은 허용하지 않습니다.';
  if (/\b(?:click|classDef|linkStyle|style|css|image|img|href|callback|accTitle|accDescr)\b|:::|\b(?:init|config|layout|elk)\s*[:=]/i.test(source)) return '클릭·이미지·사용자 CSS·설정 지시문은 허용하지 않습니다.';
  return null;
}
