import electron = require('electron');
const { contextBridge, ipcRenderer } = electron;

contextBridge.exposeInMainWorld('systemRecords', {
  readCatalog: () => ipcRenderer.invoke('system-records:read'),
  readSection: (sourceId: string) => ipcRenderer.invoke('system-records:read-section', sourceId),
  readCheckout: () => ipcRenderer.invoke('system-records:read-checkout'),
});
contextBridge.exposeInMainWorld('systemGuide', {
  readGuide: () => ipcRenderer.invoke('system-guide:read'),
});
contextBridge.exposeInMainWorld('systemBacklog', {
  readBacklog: () => ipcRenderer.invoke('system-backlog:read'),
});
contextBridge.exposeInMainWorld('serverOperations', {
  readConnection: () => ipcRenderer.invoke('server-operations:connection'),
  connect: () => ipcRenderer.invoke('server-operations:connect'),
  readStatus: () => ipcRenderer.invoke('server-operations:status'),
  startServer: () => ipcRenderer.invoke('server-operations:start'),
  stopServer: () => ipcRenderer.invoke('server-operations:stop'),
  forceStopServer: () => ipcRenderer.invoke('server-operations:force-stop'),
  readLogs: (query: unknown) => ipcRenderer.invoke('server-operations:logs', query),
  readReleaseCandidate: () => ipcRenderer.invoke('server-operations:release-candidate'),
  buildRelease: (input: unknown) => ipcRenderer.invoke('server-operations:release-build', input),
  selectRelease: (input: unknown) => ipcRenderer.invoke('server-operations:release-select', input),
});
