import type { CatalogBridge } from '../electron/catalog-contract';
declare global {
  interface Window { systemRecords?: CatalogBridge; }
}
export {};
