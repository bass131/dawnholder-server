import type { CatalogResult } from '../electron/catalog-contract';
import type { BacklogBridge } from '../electron/backlog-contract';
import type { CheckoutResult } from '../electron/checkout-contract';
import type { SourceSectionResult } from '../electron/source-section-contract';
import type { GuideBridge } from '../electron/system-guide-contract';

export interface RecordsBridge {
  readCatalog(): Promise<CatalogResult>;
  readSection(sourceId: string): Promise<SourceSectionResult>;
  readCheckout(): Promise<CheckoutResult>;
}

declare global {
  interface Window {
    systemRecords?: RecordsBridge;
    systemGuide?: GuideBridge;
    systemBacklog?: BacklogBridge;
  }
}
export {};
