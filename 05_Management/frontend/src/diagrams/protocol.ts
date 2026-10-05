export interface DiagramIdentity { requestId: string; generation: string; token: string; }
export type ChildMessage = DiagramIdentity & (
  { type: 'diagram:ready' } | { type: 'diagram:result'; svg: string } |
  { type: 'diagram:shown'; height: number } | { type: 'diagram:failed'; message: string } |
  { type: 'diagram:focus' }
);
export type ParentMessage = DiagramIdentity & (
  { type: 'diagram:render'; source: string; title: string; description: string } |
  { type: 'diagram:approve' } | { type: 'diagram:init' }
);
function record(value: unknown): value is Record<string, unknown> { return typeof value === 'object' && value !== null && !Array.isArray(value); }
export function sameIdentity(value: unknown, identity: DiagramIdentity): value is Record<string, unknown> & DiagramIdentity {
  return record(value) && value.requestId === identity.requestId && value.generation === identity.generation && value.token === identity.token;
}
export function isChildMessage(value: unknown, identity: DiagramIdentity): value is ChildMessage {
  if (!sameIdentity(value, identity)) return false;
  const common = ['type', 'requestId', 'generation', 'token'];
  const type = value.type;
  const extras = type === 'diagram:result' ? ['svg'] : type === 'diagram:shown' ? ['height'] : type === 'diagram:failed' ? ['message'] : [];
  if (Object.keys(value).some(key => ![...common, ...extras].includes(key))) return false;
  if (type === 'diagram:ready' || type === 'diagram:focus') return true;
  if (type === 'diagram:result') return typeof value.svg === 'string';
  if (type === 'diagram:shown') return typeof value.height === 'number' && Number.isFinite(value.height) && value.height >= 120 && value.height <= 720;
  return type === 'diagram:failed' && typeof value.message === 'string' && value.message.length <= 512;
}
export function isParentMessage(value: unknown, identity?: DiagramIdentity): value is ParentMessage {
  if (!record(value) || !['requestId', 'generation', 'token'].every(key => typeof value[key] === 'string' && /^[a-z0-9-]{1,64}$/.test(value[key] as string))) return false;
  if (identity && !sameIdentity(value, identity)) return false;
  const common = ['type', 'requestId', 'generation', 'token'];
  const extras = value.type === 'diagram:render' ? ['source', 'title', 'description'] : [];
  if (Object.keys(value).some(key => ![...common, ...extras].includes(key))) return false;
  if (value.type === 'diagram:init' || value.type === 'diagram:approve') return true;
  return value.type === 'diagram:render' && typeof value.source === 'string' && typeof value.title === 'string' && typeof value.description === 'string' && value.title.length <= 4096 && value.description.length <= 2048;
}
