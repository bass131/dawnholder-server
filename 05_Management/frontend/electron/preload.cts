import electron = require('electron');
const { contextBridge, ipcRenderer } = electron;

contextBridge.exposeInMainWorld('systemRecords', {
  readCatalog: () => ipcRenderer.invoke('system-records:read'),
  saveCatalog: (input: { text: string; expectedVersion: string | null }) => ipcRenderer.invoke('system-records:save', input),
});
contextBridge.exposeInMainWorld('systemGuide', {
  readGuide: () => ipcRenderer.invoke('system-guide:read'),
});
