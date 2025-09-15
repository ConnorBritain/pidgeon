# Shared Model Management System
**Purpose**: Unified AI model management between CLI and GUI applications  
**Status**: Design specification for desktop application integration  
**Compliance**: Airgapped, local-only processing for healthcare PHI safety  

---

## 🎯 **System Overview**

### **Healthcare Compliance Requirements**
- **Airgapped Operation**: Zero internet dependencies after model download
- **Local Processing**: All AI inference happens on user's machine
- **PHI Safety**: No healthcare data leaves the local environment
- **Professional Trust**: Desktop app with clear model governance

### **CLI-GUI Model Synchronization**
- **Shared Storage**: Single model directory used by both applications
- **Real-time Sync**: Model availability updates immediately between apps
- **Unified Management**: Download once, use in both CLI and GUI
- **Consistent Interface**: Same model capabilities and performance

---

## 📁 **Shared Model Directory Structure**

### **Default Locations**
```bash
# Windows
%USERPROFILE%\.pidgeon\models\

# macOS
~/.pidgeon/models/

# Linux
~/.pidgeon/models/
```

### **Directory Structure**
```
~/.pidgeon/models/
├── registry.json                    # Model metadata and status
├── biomedical-mistral-7b/           # Healthcare-specialized model
│   ├── model.onnx                  # ONNX format for fast inference
│   ├── tokenizer.json              # Tokenizer configuration
│   ├── config.json                 # Model configuration
│   └── metadata.json               # Healthcare-specific metadata
├── gpt-oss-20b/                    # General-purpose large model
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   └── metadata.json
├── phi-3-mini/                     # Lightweight model for basic tasks
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   └── metadata.json
└── downloads/                      # Temporary download staging
    └── [partial-downloads]/
```

---

## 🗃️ **Model Registry Schema**

### **registry.json Structure**
```json
{
  "version": "1.0.0",
  "lastUpdated": "2025-09-14T23:30:00Z",
  "models": {
    "biomedical-mistral-7b": {
      "name": "BioMedical Mistral 7B",
      "description": "Healthcare-specialized model for clinical text understanding",
      "status": "downloaded",
      "version": "1.0.0",
      "size": 4294967296,
      "sizeFormatted": "4.0 GB",
      "downloadedAt": "2025-09-14T20:15:00Z",
      "path": "biomedical-mistral-7b/",
      "capabilities": [
        "medical-qa",
        "clinical-text-analysis", 
        "drug-interaction-checking",
        "symptom-analysis"
      ],
      "performance": {
        "minRAM": "8 GB",
        "recommendedRAM": "16 GB",
        "inferenceSpeed": "fast",
        "accuracy": "high"
      },
      "healthcare": {
        "specialties": ["general-medicine", "pharmacy", "nursing"],
        "training": "Medical literature + clinical notes (de-identified)",
        "validation": "Peer-reviewed medical accuracy testing"
      },
      "technical": {
        "format": "ONNX",
        "precision": "float16",
        "contextWindow": 4096,
        "maxTokens": 2048
      }
    },
    "gpt-oss-20b": {
      "name": "GPT-OSS 20B",
      "description": "Large general-purpose model for complex reasoning",
      "status": "not-downloaded",
      "version": "2.1.0", 
      "size": 12884901888,
      "sizeFormatted": "12.0 GB",
      "downloadUrl": "https://huggingface.co/gpt-oss/gpt-oss-20b",
      "path": null,
      "capabilities": [
        "general-purpose",
        "code-generation",
        "complex-reasoning",
        "multi-step-analysis"
      ],
      "performance": {
        "minRAM": "16 GB",
        "recommendedRAM": "32 GB", 
        "inferenceSpeed": "medium",
        "accuracy": "very-high"
      },
      "healthcare": {
        "specialties": ["all"],
        "training": "General internet + medical literature",
        "validation": "General accuracy + medical subset testing"
      },
      "technical": {
        "format": "ONNX",
        "precision": "float16",
        "contextWindow": 8192,
        "maxTokens": 4096
      }
    },
    "phi-3-mini": {
      "name": "Phi-3 Mini",
      "description": "Lightweight model for basic healthcare assistance",
      "status": "downloading",
      "version": "1.0.0",
      "size": 2147483648,
      "sizeFormatted": "2.0 GB",
      "downloadProgress": 0.65,
      "downloadSpeed": "5.2 MB/s",
      "estimatedCompletion": "2025-09-14T23:45:00Z",
      "path": null,
      "capabilities": [
        "basic-qa",
        "simple-text-analysis",
        "quick-responses"
      ],
      "performance": {
        "minRAM": "4 GB",
        "recommendedRAM": "8 GB",
        "inferenceSpeed": "very-fast",
        "accuracy": "medium"
      },
      "healthcare": {
        "specialties": ["basic-medical-qa"],
        "training": "Curated medical Q&A datasets",
        "validation": "Basic medical accuracy testing"
      },
      "technical": {
        "format": "ONNX",
        "precision": "float16",
        "contextWindow": 2048,
        "maxTokens": 1024
      }
    }
  }
}
```

---

## 🔄 **CLI-GUI Synchronization Protocol**

### **File System Watching**
```typescript
// lib/electron/model-registry.ts
import { FSWatcher, watch } from 'fs';
import { EventEmitter } from 'events';

export class ModelRegistry extends EventEmitter {
  private watcher: FSWatcher;
  private registryPath: string;
  
  constructor(modelsDir: string) {
    super();
    this.registryPath = path.join(modelsDir, 'registry.json');
    this.watcher = watch(this.registryPath, this.onRegistryChange.bind(this));
  }
  
  private onRegistryChange() {
    // Emit event to update GUI model status
    this.emit('registry-updated', this.loadRegistry());
  }
  
  async downloadModel(modelId: string): Promise<void> {
    // Download with progress updates
    const model = await this.startDownload(modelId);
    this.updateRegistry(modelId, { 
      status: 'downloading',
      downloadProgress: 0
    });
  }
}
```

### **Cross-Application Messaging**
```typescript
// CLI writes status updates
await updateModelRegistry('phi-3-mini', {
  status: 'downloading',
  downloadProgress: 0.45,
  downloadSpeed: '3.2 MB/s'
});

// GUI receives immediate updates
modelRegistry.on('model-updated', (modelId, status) => {
  updateModelManagerUI(modelId, status);
});
```

---

## 🎨 **GUI Model Manager Interface**

### **Model Library Component**
```tsx
// components/ai/ModelManager/ModelManager.tsx
import { useState, useEffect } from 'react';
import { modelRegistry } from '@/lib/electron/model-registry';

interface ModelCardProps {
  model: ModelInfo;
  onDownload: (modelId: string) => void;
  onRemove: (modelId: string) => void;
}

const ModelCard = ({ model, onDownload, onRemove }: ModelCardProps) => (
  <Card className="model-card">
    <CardHeader>
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">{model.name}</h3>
        <ModelStatusBadge status={model.status} />
      </div>
      <p className="text-sm text-gray-600">{model.description}</p>
    </CardHeader>
    
    <CardContent>
      <div className="model-specs">
        <div className="spec-row">
          <span>Size:</span>
          <span>{model.sizeFormatted}</span>
        </div>
        <div className="spec-row">
          <span>RAM Required:</span>
          <span>{model.performance.minRAM}</span>
        </div>
        <div className="spec-row">
          <span>Specialties:</span>
          <span>{model.healthcare.specialties.join(', ')}</span>
        </div>
      </div>
      
      <div className="capabilities">
        <h4>Capabilities:</h4>
        <div className="capability-tags">
          {model.capabilities.map(cap => (
            <Badge key={cap} variant="secondary">{cap}</Badge>
          ))}
        </div>
      </div>
    </CardContent>
    
    <CardFooter>
      {model.status === 'not-downloaded' && (
        <Button onClick={() => onDownload(model.id)} className="w-full">
          Download Model
        </Button>
      )}
      
      {model.status === 'downloading' && (
        <div className="download-progress">
          <Progress value={model.downloadProgress * 100} />
          <div className="download-info">
            <span>{Math.round(model.downloadProgress * 100)}%</span>
            <span>{model.downloadSpeed}</span>
          </div>
        </div>
      )}
      
      {model.status === 'downloaded' && (
        <div className="model-actions">
          <Button variant="outline" onClick={() => onRemove(model.id)}>
            Remove
          </Button>
          <Button className="ml-2">
            ✅ Ready to Use
          </Button>
        </div>
      )}
    </CardFooter>
  </Card>
);
```

### **Model Selection Interface**
```tsx
// components/ai/ModelSelector/ModelSelector.tsx
const ModelSelector = ({ onModelSelect }: ModelSelectorProps) => {
  const { models } = useModelRegistry();
  const availableModels = models.filter(m => m.status === 'downloaded');
  
  return (
    <Select onValueChange={onModelSelect}>
      <SelectTrigger>
        <SelectValue placeholder="Select AI model..." />
      </SelectTrigger>
      <SelectContent>
        {availableModels.map(model => (
          <SelectItem key={model.id} value={model.id}>
            <div className="model-option">
              <span className="model-name">{model.name}</span>
              <span className="model-perf">
                {model.performance.inferenceSpeed} • {model.performance.accuracy}
              </span>
            </div>
          </SelectItem>
        ))}
        
        {availableModels.length === 0 && (
          <SelectItem disabled value="none">
            No models downloaded. Visit Model Library to download.
          </SelectItem>
        )}
      </SelectContent>
    </Select>
  );
};
```

---

## 🔐 **Security & Safety Features**

### **Local-Only Processing**
```typescript
// lib/ai/local-inference.ts
export class LocalAIInference {
  private model: ONNXModel | null = null;
  
  async loadModel(modelPath: string): Promise<void> {
    // Load model entirely from local files
    this.model = await ONNXModel.fromFile(modelPath);
    
    // Verify no network access required
    if (this.model.requiresNetwork) {
      throw new Error('Model requires network access - not allowed');
    }
  }
  
  async generateResponse(
    prompt: string, 
    context: HealthcareContext
  ): Promise<string> {
    // Ensure PHI detection before processing
    const phiDetected = await detectPHI(prompt);
    if (phiDetected.hasPHI) {
      throw new Error('PHI detected in prompt - inference blocked');
    }
    
    // Local inference only
    return await this.model.generate(prompt, {
      maxTokens: context.maxTokens,
      temperature: context.temperature
    });
  }
}
```

### **Model Integrity Verification**
```typescript
// Verify model downloads haven't been tampered with
export class ModelIntegrity {
  async verifyModelChecksum(modelPath: string): Promise<boolean> {
    const expectedHash = await this.getExpectedHash(modelPath);
    const actualHash = await this.calculateHash(modelPath);
    
    return expectedHash === actualHash;
  }
  
  async quarantineCorruptModel(modelId: string): Promise<void> {
    // Move corrupt model to quarantine directory
    await this.moveToQuarantine(modelId);
    
    // Update registry to mark as corrupted
    await this.updateModelStatus(modelId, 'corrupted');
    
    // Notify user of corruption
    this.notifyCorruption(modelId);
  }
}
```

---

## 📊 **Model Performance Monitoring**

### **Usage Analytics (Local Only)**
```typescript
// Track model performance without sending data anywhere
interface ModelUsageStats {
  modelId: string;
  totalRequests: number;
  averageResponseTime: number;
  averageTokens: number;
  lastUsed: Date;
  userSatisfactionRating?: number; // Optional user feedback
}

export class ModelAnalytics {
  async recordUsage(
    modelId: string, 
    responseTime: number,
    tokenCount: number
  ): Promise<void> {
    // Store locally only - never transmitted
    const stats = await this.getLocalStats(modelId);
    stats.totalRequests++;
    stats.averageResponseTime = this.updateAverage(
      stats.averageResponseTime,
      responseTime,
      stats.totalRequests
    );
    
    await this.saveLocalStats(modelId, stats);
  }
}
```

---

## 🚀 **Implementation Strategy**

### **Phase 1: Basic Model Management**
- [ ] Shared model directory structure
- [ ] Registry file format and validation
- [ ] CLI model download integration
- [ ] GUI model status display

### **Phase 2: Advanced Features**
- [ ] Real-time sync between CLI and GUI
- [ ] Download progress tracking
- [ ] Model performance comparison
- [ ] Integrity verification

### **Phase 3: Intelligence Features**
- [ ] Usage-based model recommendations
- [ ] Automatic model updates
- [ ] Performance optimization suggestions
- [ ] Healthcare-specific model curation

---

## 🎯 **Success Metrics**

### **Technical Performance**
- Model loading time: <10 seconds for 7B models
- Registry sync delay: <1 second between CLI and GUI
- Download interruption recovery: 100% success rate
- Memory efficiency: Models consume <150% of file size in RAM

### **User Experience**
- Model selection clarity: Users understand model tradeoffs
- Download confidence: Progress and time estimates accurate
- Sync transparency: Users see immediate model availability
- Professional trust: Healthcare professionals confident in local processing

---

**The Goal**: Seamless AI model management that feels like a professional desktop application while maintaining the security and compliance requirements of healthcare environments.