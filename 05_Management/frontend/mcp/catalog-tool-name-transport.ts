import { INVALID_PARAMS, isJSONRPCRequest, McpServer, type Transport } from '@modelcontextprotocol/server';
import { inputSchemas } from './catalog-schemas.js';

// The SDK's unknown-tool lookup reflects the name. Use its public connect /
// Transport boundary to refuse that request before lookup, leaving registered
// tools' schema validation, response projection and cancellation with the SDK.
class CatalogToolNameTransport implements Transport {
  onclose: Transport['onclose'];
  onerror: Transport['onerror'];
  onmessage: Transport['onmessage'];

  constructor(private readonly inner: Transport) {}

  get sessionId() { return this.inner.sessionId; }
  get hasPerRequestStream() { return this.inner.hasPerRequestStream === true; }
  setProtocolVersion(version: string) { this.inner.setProtocolVersion?.(version); }
  setSupportedProtocolVersions(versions: string[]) { this.inner.setSupportedProtocolVersions?.(versions); }
  send(...args: Parameters<Transport['send']>) { return this.inner.send(...args); }
  close() { return this.inner.close(); }

  start() {
    this.inner.onclose = () => this.onclose?.();
    this.inner.onerror = error => this.onerror?.(error);
    this.inner.onmessage = (message, extra) => {
      if (isJSONRPCRequest(message) && message.method === 'tools/call'
        && typeof message.params?.name === 'string' && !Object.hasOwn(inputSchemas, message.params.name)) {
        void this.inner.send({
          jsonrpc: '2.0', id: message.id,
          error: { code: INVALID_PARAMS, message: 'Unknown tool name.' },
        }, { relatedRequestId: message.id }).catch(error => {
          this.onerror?.(error instanceof Error ? error : new Error('Tool refusal transport error.'));
        });
        return;
      }
      this.onmessage?.(message, extra);
    };
    return this.inner.start();
  }
}

export class CatalogMcpServer extends McpServer {
  override connect(transport: Transport): Promise<void> {
    return super.connect(new CatalogToolNameTransport(transport));
  }
}
