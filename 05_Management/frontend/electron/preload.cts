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
