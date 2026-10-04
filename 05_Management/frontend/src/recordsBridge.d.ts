import type { CatalogBridge } from '../electron/catalog-contract';
import type { GuideBridge } from '../electron/system-guide-contract';
declare global {
  interface Window { systemRecords?: CatalogBridge; systemGuide?: GuideBridge; }
}
export {};
