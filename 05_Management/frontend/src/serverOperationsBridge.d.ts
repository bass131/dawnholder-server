import type { ServerOperationsBridge } from '../electron/server-operations-contract';

declare global {
  interface Window {
    serverOperations?: ServerOperationsBridge;
  }
}
export {};
