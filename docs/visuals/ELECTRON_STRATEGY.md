# Electron Packaging Strategy
**Purpose**: Desktop application deployment strategy for healthcare professionals  
**Target**: Windows-first, macOS-second, Linux CLI-only approach  
**Compliance**: Airgapped healthcare workstation deployment  

---

## 🎯 **Desktop Deployment Strategy**

### **Platform Priorities**
1. **Windows 10/11** (Primary) - 70% of healthcare workstations
2. **macOS** (Secondary) - 25% of healthcare environments  
3. **Linux** (CLI-only) - 5% specialized environments

### **Healthcare Workstation Requirements**
- **Offline Operation**: Complete functionality without internet
- **Professional Installation**: MSI installer with IT admin controls
- **Security Compliance**: Code signing, virus scanning compatibility
- **Performance**: Responsive on typical healthcare workstation specs

---

## 📦 **Electron Configuration**

### **electron-builder.yml**
```yaml
appId: com.pidgeonhealth.testingsuite
productName: Pidgeon Testing Suite
copyright: Copyright © 2025 Pidgeon Health

# Build directories
directories:
  output: dist
  buildResources: build-resources

# Source files
files:
  - from: .next/standalone
    to: app
  - from: .next/static
    to: app/.next/static
  - from: public
    to: app/public
  - from: src/electron
    to: .
  - node_modules/**/*

# Windows configuration
win:
  target:
    - target: nsis
      arch: [x64]
    - target: msi  
      arch: [x64]
  icon: build-resources/icon.ico
  publisherName: Pidgeon Health
  verifyUpdateCodeSignature: true
  
nsis:
  oneClick: false
  allowToChangeInstallationDirectory: true
  createDesktopShortcut: true
  createStartMenuShortcut: true
  shortcutName: Pidgeon Testing Suite
  uninstallDisplayName: Pidgeon Testing Suite
  license: LICENSE.txt
  warningsAsErrors: false

msi:
  oneClick: false
  createDesktopShortcut: true
  createStartMenuShortcut: true

# macOS configuration  
mac:
  target:
    - target: dmg
      arch: [x64, arm64]
  icon: build-resources/icon.icns
  category: public.app-category.healthcare-fitness
  entitlements: build-resources/entitlements.mac.plist
  hardenedRuntime: true
  notarize: true

dmg:
  title: Pidgeon Testing Suite
  icon: build-resources/icon.icns
  background: build-resources/dmg-background.png
  contents:
    - x: 130
      y: 220
    - x: 410
      y: 220
      type: link
      path: /Applications

# Code signing (production)
codeSign:
  certificateFile: certificates/pidgeon-health.p12
  certificatePassword: ${CERTIFICATE_PASSWORD}

# Auto-updater (future)
publish:
  provider: generic
  url: https://releases.pidgeonhealth.com/
  channel: stable
```

### **package.json Scripts**
```json
{
  "scripts": {
    "electron": "concurrently \"npm run next:dev\" \"wait-on http://localhost:3000 && electron .\"",
    "electron:pack": "npm run next:build && electron-builder",
    "electron:dist": "npm run next:build && electron-builder --publish=never",
    "electron:dist:win": "npm run next:build && electron-builder --win",
    "electron:dist:mac": "npm run next:build && electron-builder --mac", 
    "next:dev": "next dev",
    "next:build": "next build",
    "preelectron:pack": "npm run next:build"
  }
}
```

---

## 🏗️ **Electron Main Process Architecture**

### **src/electron/main.ts**
```typescript
import { app, BrowserWindow, ipcMain, shell, dialog } from 'electron';
import { join } from 'path';
import { ModelManager } from './model-manager';
import { CLIBridge } from './cli-bridge';
import { updateElectronApp } from 'update-electron-app';

// Healthcare-focused security settings
const SECURITY_CONFIG = {
  webSecurity: true,
  nodeIntegration: false,
  contextIsolation: true,
  enableRemoteModule: false,
  allowRunningInsecureContent: false
};

class PidgeonElectronApp {
  private mainWindow: BrowserWindow | null = null;
  private modelManager: ModelManager;
  private cliBridge: CLIBridge;

  constructor() {
    this.modelManager = new ModelManager();
    this.cliBridge = new CLIBridge();
    this.setupApp();
  }

  private setupApp() {
    // Single instance enforcement for healthcare environments
    const gotTheLock = app.requestSingleInstanceLock();
    if (!gotTheLock) {
      app.quit();
      return;
    }

    app.whenReady().then(() => {
      this.createMainWindow();
      this.setupIpcHandlers();
      this.setupAutoUpdater();
    });

    app.on('window-all-closed', () => {
      if (process.platform !== 'darwin') {
        app.quit();
      }
    });
  }

  private createMainWindow() {
    this.mainWindow = new BrowserWindow({
      width: 1400,
      height: 900,
      minWidth: 1024,
      minHeight: 768,
      icon: join(__dirname, '../build-resources/icon.png'),
      title: 'Pidgeon Testing Suite',
      titleBarStyle: 'default',
      webPreferences: {
        ...SECURITY_CONFIG,
        preload: join(__dirname, 'preload.js')
      }
    });

    // Load the Next.js app
    const isDev = process.env.NODE_ENV === 'development';
    const url = isDev 
      ? 'http://localhost:3000'
      : `file://${join(__dirname, '../app/index.html')}`;
    
    this.mainWindow.loadURL(url);

    // Healthcare professional experience
    this.mainWindow.setMenuBarVisibility(false); // Clean interface
    this.mainWindow.on('closed', () => {
      this.mainWindow = null;
    });
  }

  private setupIpcHandlers() {
    // Model management IPC
    ipcMain.handle('model:list', () => {
      return this.modelManager.listModels();
    });

    ipcMain.handle('model:download', async (event, modelId: string) => {
      return this.modelManager.downloadModel(modelId);
    });

    ipcMain.handle('model:remove', async (event, modelId: string) => {
      return this.modelManager.removeModel(modelId);
    });

    // CLI integration IPC
    ipcMain.handle('cli:execute', async (event, command: string, args: string[]) => {
      return this.cliBridge.executeCommand(command, args);
    });

    ipcMain.handle('cli:validate', async (event, message: string, standard: string) => {
      return this.cliBridge.validateMessage(message, standard);
    });

    // File system access for healthcare workflows
    ipcMain.handle('file:selectHL7', async () => {
      const result = await dialog.showOpenDialog(this.mainWindow!, {
        title: 'Select HL7 Message File',
        filters: [
          { name: 'HL7 Files', extensions: ['hl7', 'txt'] },
          { name: 'All Files', extensions: ['*'] }
        ],
        properties: ['openFile']
      });
      return result;
    });

    ipcMain.handle('file:saveReport', async (event, content: string) => {
      const result = await dialog.showSaveDialog(this.mainWindow!, {
        title: 'Save Validation Report',
        defaultPath: 'pidgeon-report.html',
        filters: [
          { name: 'HTML Reports', extensions: ['html'] },
          { name: 'All Files', extensions: ['*'] }
        ]
      });
      return result;
    });
  }

  private setupAutoUpdater() {
    // Healthcare environments need controlled updates
    if (process.env.NODE_ENV === 'production') {
      updateElectronApp({
        updateInterval: '24 hours',
        notifyUser: true,
        logger: console
      });
    }
  }
}

new PidgeonElectronApp();
```

### **src/electron/preload.ts**
```typescript
import { contextBridge, ipcRenderer } from 'electron';

// Secure API exposure for healthcare environment
const electronAPI = {
  // Model management
  models: {
    list: () => ipcRenderer.invoke('model:list'),
    download: (modelId: string) => ipcRenderer.invoke('model:download', modelId),
    remove: (modelId: string) => ipcRenderer.invoke('model:remove', modelId),
    onProgress: (callback: (progress: ModelProgress) => void) => {
      ipcRenderer.on('model:progress', (_, progress) => callback(progress));
    }
  },

  // CLI integration
  cli: {
    execute: (command: string, args: string[]) => 
      ipcRenderer.invoke('cli:execute', command, args),
    validate: (message: string, standard: string) =>
      ipcRenderer.invoke('cli:validate', message, standard)
  },

  // File system (healthcare workflows)
  files: {
    selectHL7: () => ipcRenderer.invoke('file:selectHL7'),
    saveReport: (content: string) => ipcRenderer.invoke('file:saveReport', content),
    readFile: (path: string) => ipcRenderer.invoke('file:read', path)
  },

  // System information
  system: {
    platform: process.platform,
    version: process.versions.electron,
    isPackaged: process.env.NODE_ENV === 'production'
  }
};

// Type-safe API exposure
contextBridge.exposeInMainWorld('electronAPI', electronAPI);

// Type definitions for renderer process
export type ElectronAPI = typeof electronAPI;
```

---

## 🔒 **Security & Compliance Configuration**

### **entitlements.mac.plist**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>com.apple.security.cs.allow-jit</key>
  <true/>
  <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
  <true/>
  <key>com.apple.security.cs.disable-library-validation</key>
  <true/>
  <key>com.apple.security.network.client</key>
  <false/>  <!-- No network access after install -->
  <key>com.apple.security.network.server</key>
  <false/>  <!-- No server functionality -->
  <key>com.apple.security.files.user-selected.read-write</key>
  <true/>   <!-- User can select HL7 files -->
</dict>
</plist>
```

### **Content Security Policy**
```typescript
// Security headers for healthcare compliance
const CSP_POLICY = [
  "default-src 'self'",
  "script-src 'self' 'unsafe-inline'", // Required for Next.js
  "style-src 'self' 'unsafe-inline'",  // Required for Tailwind
  "img-src 'self' data:",
  "font-src 'self'",
  "connect-src 'none'",               // No external connections
  "frame-src 'none'",
  "object-src 'none'"
].join('; ');
```

---

## 📋 **Healthcare Installation Requirements**

### **Windows Enterprise Deployment**
```powershell
# Silent installation for IT administrators
PidgeonTestingSuite-Setup.msi /quiet /norestart

# Registry keys for enterprise management
HKLM\SOFTWARE\PidgeonHealth\TestingSuite
  - InstallPath: String
  - Version: String  
  - ModelsPath: String
  - DisableUpdates: DWORD (0/1)
```

### **macOS Enterprise Deployment**
```bash
# Package installation
sudo installer -pkg PidgeonTestingSuite.pkg -target /

# Preferences for enterprise management
sudo defaults write /Library/Preferences/com.pidgeonhealth.testingsuite \
  ModelsPath -string "/usr/local/share/pidgeon/models"
```

### **Digital Signatures & Trust**
- **Windows**: Authenticode signature with trusted certificate
- **macOS**: Notarized app bundle with Developer ID
- **Verification**: Users can verify signatures before installation

---

## 🚀 **Build Pipeline**

### **GitHub Actions Workflow**
```yaml
name: Build Desktop Apps

on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      matrix:
        os: [windows-latest, macos-latest]
    
    runs-on: ${{ matrix.os }}
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          cache: 'npm'
          
      - name: Install dependencies
        run: npm ci
        
      - name: Build Next.js app
        run: npm run next:build
        
      - name: Build Electron app (Windows)
        if: runner.os == 'Windows'
        run: npm run electron:dist:win
        env:
          CERTIFICATE_PASSWORD: ${{ secrets.WIN_CERT_PASSWORD }}
          
      - name: Build Electron app (macOS)
        if: runner.os == 'macOS'
        run: npm run electron:dist:mac
        env:
          APPLE_ID: ${{ secrets.APPLE_ID }}
          APPLE_ID_PASSWORD: ${{ secrets.APPLE_ID_PASSWORD }}
          
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: desktop-apps-${{ runner.os }}
          path: dist/
```

### **Local Development Workflow**
```bash
# Start development environment
npm run electron:dev

# Test production build locally
npm run electron:pack

# Build for specific platform
npm run electron:dist:win
npm run electron:dist:mac
```

---

## 📊 **Performance Optimization**

### **Bundle Size Optimization**
- **Next.js Static Export**: Pre-built HTML/JS reduces startup time
- **Electron Tree Shaking**: Exclude unused Node.js modules
- **Model Compression**: ONNX quantization for smaller models
- **Asset Optimization**: Compressed images and fonts

### **Memory Management**
```typescript
// Efficient model loading for healthcare workstations
class MemoryOptimizedModelLoader {
  private modelCache = new Map<string, WeakRef<Model>>();
  
  async loadModel(modelId: string): Promise<Model> {
    // Check if model is already in memory
    const cached = this.modelCache.get(modelId)?.deref();
    if (cached) return cached;
    
    // Load model with memory constraints
    const model = await this.loadWithConstraints(modelId, {
      maxMemory: 2048 * 1024 * 1024, // 2GB limit
      enableSwapping: true
    });
    
    // Cache with weak reference for automatic cleanup
    this.modelCache.set(modelId, new WeakRef(model));
    return model;
  }
}
```

---

## 🎯 **Success Criteria**

### **Installation Success**
- Windows installer: <5 minutes on healthcare workstation
- macOS installer: <3 minutes with notarization verification
- Zero network requirements after installation
- Compatible with hospital antivirus software

### **Performance Targets**
- App startup: <10 seconds cold start
- Model loading: <15 seconds for 7B models
- UI responsiveness: <100ms for all healthcare workflows
- Memory usage: <4GB with largest model loaded

### **Healthcare Compliance**
- No data transmission after installation
- Code signing verification passes
- Compatible with hospital security policies
- Professional appearance for clinical environments

---

**The Goal**: Desktop application that healthcare professionals trust and IT administrators can deploy confidently in secure healthcare environments.