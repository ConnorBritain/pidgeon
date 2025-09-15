# Pidgeon GUI Implementation Plan

**Version**: 1.0  
**Created**: September 14, 2025  
**Purpose**: Technical implementation roadmap for Pidgeon Testing Suite GUI  
**Location**: `/pidgeon_gui` (top-level directory)

---

## 🎯 **Project Overview**

Build a modern, intuitive GUI for the Pidgeon Testing Suite that transforms healthcare message testing from a CLI-only experience to a visual, interactive workspace. This GUI will be the primary driver for Professional tier conversions ($29/month).

**Success Criteria**:
- New users can generate and validate messages in <2 minutes
- Power users find it faster than CLI for complex workflows
- Professional and polished enough for enterprise healthcare

---

## 🛠️ **Technology Stack**

### **Desktop Application Architecture**
- **Electron**: Cross-platform desktop app framework (primary deployment)
- **Next.js 14**: React framework for UI development and prototyping
- **TypeScript**: Type safety throughout
- **Tailwind CSS**: Utility-first styling
- **CSS Modules**: Component-specific styles

### **AI Integration (Local Models)**
- **Local OSS Models**: BioMistral, gpt-oss-20b, phi-3 (shared with CLI)
- **Model Management**: Shared download/registry system with CLI
- **Inference Engine**: Local processing, zero cloud dependencies
- **Model Sync**: CLI-GUI model availability synchronization

### **UI Components**
- **Radix UI**: Unstyled, accessible primitives
- **Framer Motion**: Smooth animations
- **Monaco Editor**: VS Code editor for message editing

### **State & Data**
- **Zustand**: Lightweight state management
- **TanStack Query**: Local data caching (no server state)
- **React Hook Form**: Form handling
- **Zod**: Runtime validation

### **Desktop Features**
- **File System Access**: Direct model downloads and management
- **Inter-Process Communication**: CLI backend integration
- **Native Notifications**: Healthcare workflow alerts
- **Offline Operation**: Complete airgapped functionality

### **Developer Experience**
- **Storybook**: Component development
- **Jest & React Testing Library**: Testing
- **Electron Builder**: Desktop app packaging
- **ESLint & Prettier**: Code quality

---

## 📁 **Project Structure**

```
pidgeon_gui/
├── src/
│   ├── app/                          # Next.js app router
│   │   ├── layout.tsx               # Root layout with providers
│   │   ├── page.tsx                 # Main workspace
│   │   └── globals.css              # Global styles with Pidgeon Health tokens
│   │
│   ├── electron/                     # Electron main process
│   │   ├── main.ts                  # Electron app entry point
│   │   ├── preload.ts               # Secure IPC bridge
│   │   ├── ipc-handlers.ts          # CLI backend communication
│   │   └── model-manager.ts         # Local AI model management
│   │
│   ├── lib/
│   │   ├── electron/                # Electron-specific utilities
│   │   │   ├── cli-bridge.ts       # CLI process communication
│   │   │   ├── model-registry.ts   # Shared model management
│   │   │   └── file-system.ts      # Safe file operations
│   │   ├── ai/                     # Local AI integration
│   │   │   ├── model-loader.ts     # Local model inference
│   │   │   ├── healthcare-prompts.ts # Medical AI prompts
│   │   │   └── phi-safety.ts       # Local PHI detection
│
├── components/
│   ├── ui/                      # Base UI components
│   │   ├── Button/
│   │   │   ├── Button.tsx
│   │   │   ├── Button.module.css
│   │   │   └── Button.stories.tsx
│   │   ├── Card/
│   │   ├── Input/
│   │   ├── Dialog/
│   │   ├── Dropdown/
│   │   ├── Toast/
│   │   └── Tabs/
│   │
│   ├── workspace/               # Main workspace components
│   │   ├── Layout/
│   │   │   ├── Layout.tsx
│   │   │   └── Layout.module.css
│   │   ├── ActivityBar/
│   │   │   ├── ActivityBar.tsx
│   │   │   ├── ActivityBarItem.tsx
│   │   │   └── ActivityBar.module.css
│   │   ├── Canvas/
│   │   │   ├── Canvas.tsx
│   │   │   ├── MessageView.tsx
│   │   │   └── Canvas.module.css
│   │   ├── Library/
│   │   │   ├── Library.tsx
│   │   │   ├── RecentItems.tsx
│   │   │   └── Templates.tsx
│   │   ├── ToolPalette/
│   │   │   ├── ToolPalette.tsx
│   │   │   └── PropertyPanel.tsx
│   │   └── ActionBar/
│   │       ├── ActionBar.tsx
│   │       └── StatusIndicator.tsx
│   │
│   ├── ai/                      # AI Assistant components
│   │   ├── AIPanel/
│   │   │   ├── AIPanel.tsx
│   │   │   ├── AIPanel.module.css
│   │   │   └── AIPanel.stories.tsx
│   │   ├── ChatMode/
│   │   │   ├── ChatMode.tsx
│   │   │   ├── MessageBubble.tsx
│   │   │   └── ChatInput.tsx
│   │   ├── DraftMode/
│   │   │   ├── DraftMode.tsx
│   │   │   ├── TemplateSelector.tsx
│   │   │   └── FieldSuggestions.tsx
│   │   └── AnalyzeMode/
│   │       ├── AnalyzeMode.tsx
│   │       ├── ComplianceReport.tsx
│   │       └── PatternAnalysis.tsx
│   │
│   ├── message/                 # Message handling components
│   │   ├── Editor/
│   │   │   ├── MessageEditor.tsx
│   │   │   ├── MonacoWrapper.tsx
│   │   │   └── SyntaxHighlight.tsx
│   │   ├── Validation/
│   │   │   ├── ValidationPanel.tsx
│   │   │   ├── ErrorList.tsx
│   │   │   └── ErrorDetail.tsx
│   │   ├── Field/
│   │   │   ├── FieldInspector.tsx
│   │   │   └── FieldEditor.tsx
│   │   └── Tree/
│   │       ├── SegmentTree.tsx
│   │       └── TreeNode.tsx
│   │
│   ├── workflow/                # Workflow components
│   │   ├── Designer/
│   │   │   ├── WorkflowDesigner.tsx
│   │   │   ├── StepCard.tsx
│   │   │   └── ConnectionLine.tsx
│   │   ├── Execution/
│   │   │   ├── ExecutionMonitor.tsx
│   │   │   └── ProgressBar.tsx
│   │   └── Templates/
│   │       └── WorkflowTemplates.tsx
│   │
│   └── common/                  # Shared components
│       ├── CommandPalette/
│       │   └── CommandPalette.tsx
│       ├── ThemeToggle/
│       │   └── ThemeToggle.tsx
│       └── ErrorBoundary/
│           └── ErrorBoundary.tsx
│
├── lib/
│   ├── api/                     # API client layer
│   │   ├── client.ts           # Base API client
│   │   ├── messages.ts         # Message operations
│   │   ├── workflows.ts        # Workflow operations
│   │   └── websocket.ts        # Real-time connections
│   │
│   ├── ai/                      # AI service layer
│   │   ├── aiService.ts        # Healthcare AI service
│   │   ├── contextManager.ts   # Message context tracking
│   │   ├── promptTemplates.ts  # Healthcare-specific prompts
│   │   └── responseParser.ts   # AI response processing
│   │
│   ├── hooks/                   # Custom React hooks
│   │   ├── useMessage.ts
│   │   ├── useValidation.ts
│   │   ├── useWorkflow.ts
│   │   ├── useAI.ts            # AI integration hook
│   │   ├── useActivityBar.ts   # Activity bar state
│   │   └── useKeyboardShortcuts.ts
│   │
│   ├── utils/                   # Utility functions
│   │   ├── messageParser.ts
│   │   ├── validators.ts
│   │   ├── formatters.ts
│   │   ├── contextExtractor.ts # Message context extraction
│   │   └── storage.ts
│   │
│   └── types/                   # TypeScript definitions
│       ├── message.types.ts
│       ├── workflow.types.ts
│       ├── validation.types.ts
│       ├── ai.types.ts         # AI service types
│       ├── panel.types.ts      # Panel management types
│       └── api.types.ts
│
├── store/                        # Zustand stores
│   ├── messageStore.ts          # Message state
│   ├── workflowStore.ts         # Workflow state
│   ├── aiStore.ts               # AI assistant state
│   ├── panelStore.ts            # Panel management state
│   ├── uiStore.ts               # UI preferences
│   └── projectStore.ts          # Project management
│
├── styles/
│   ├── globals.css              # Global styles
│   ├── variables.css            # CSS variables (from brand)
│   └── themes/
│       ├── light.css
│       └── dark.css
│
├── public/                       # Static assets
│   ├── images/
│   ├── fonts/
│   └── icons/
│
├── tests/
│   ├── unit/                    # Unit tests
│   ├── integration/             # Integration tests
│   └── e2e/                     # End-to-end tests
│
├── config/
│   ├── jest.config.js
│   ├── playwright.config.ts
│   ├── electron-builder.yml        # Electron packaging config
│   └── .storybook/
│       ├── main.js
│       └── preview.js
│
├── dist/                           # Electron build output
│   ├── win/                       # Windows installer
│   ├── mac/                       # macOS app bundle
│   └── linux/                     # Linux AppImage
│
└── models/                         # Shared model directory (development)
    ├── registry.json              # Model metadata and status
    └── [model-directories]/       # Downloaded model files
```

---

## 🚀 **Implementation Phases**

### **Phase 1: Foundation (Week 1)**

#### **Day 1-2: Desktop Project Setup**
```bash
# Next.js foundation (already complete)
npx create-next-app@latest pidgeon_gui --typescript --tailwind --app

# Desktop application dependencies
npm install electron electron-builder
npm install @electron/rebuild        # Native module support
npm install electron-store          # Desktop storage
npm install cross-env               # Cross-platform scripts

# Core UI dependencies
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-tabs
npm install @radix-ui/react-tooltip @radix-ui/react-switch @radix-ui/react-separator
npm install zustand framer-motion clsx tailwind-merge

# Local AI integration dependencies
npm install @huggingface/transformers  # Local model loading
npm install onnxruntime-node           # ONNX model inference
npm install cmdk                       # Command palette
npm install @monaco-editor/react      # Code editor

# Development dependencies
npm install -D @types/node @types/electron
npm install -D concurrently           # Run Next.js + Electron
```

#### **Day 3: Design System**
- [ ] Import brand CSS variables
- [ ] Set up Tailwind config with brand colors
- [ ] Create base component library
- [ ] Implement theme switching

#### **Day 4: Layout Structure**
- [ ] Build main workspace layout
- [ ] Implement collapsible sidebars
- [ ] Create responsive grid system
- [ ] Add persistent layout state

#### **Day 5: Storybook Setup**
- [ ] Configure Storybook
- [ ] Create stories for base components
- [ ] Set up visual testing
- [ ] Document component usage

### **Phase 2: Core Message Features (Week 2)**

#### **Day 1-2: Message Editor**
```typescript
// components/message/Editor/MessageEditor.tsx
import { Monaco } from '@monaco-editor/react';

export function MessageEditor({ 
  value, 
  onChange, 
  standard 
}: MessageEditorProps) {
  return (
    <Monaco
      value={value}
      language={standard === 'hl7' ? 'hl7' : 'json'}
      onChange={onChange}
      options={{
        minimap: { enabled: false },
        fontSize: 13,
        fontFamily: 'JetBrains Mono',
        theme: 'pidgeon-theme'
      }}
    />
  );
}
```

#### **Day 3-4: Validation Integration**
- [ ] Connect to backend validation API
- [ ] Build validation results panel
- [ ] Implement inline error display
- [ ] Add fix suggestions UI

#### **Day 5: Field Inspector**
- [ ] Create field detail view
- [ ] Add HL7/FHIR tooltips
- [ ] Implement field editing
- [ ] Build copy/paste system

### **Phase 3: Workflow Engine (Week 3)**

#### **Day 1-2: Workflow Designer**
- [ ] Build drag-and-drop interface
- [ ] Create step configuration
- [ ] Implement visual connections
- [ ] Add save/load functionality

#### **Day 3-4: Execution Engine**
- [ ] Build execution monitor
- [ ] Add progress visualization
- [ ] Implement error handling
- [ ] Create results display

#### **Day 5: Templates**
- [ ] Create template library
- [ ] Build template selector
- [ ] Add custom template saving
- [ ] Implement import/export

### **Phase 4: AI Integration & Advanced Features (Week 4)**

#### **Day 1: AI Panel Foundation**
```typescript
// components/ai/AIPanel/AIPanel.tsx
import { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

type AIMode = 'chat' | 'draft' | 'analyze';

export function AIPanel({ isOpen, onToggle }: AIPanelProps) {
  const [activeMode, setActiveMode] = useState<AIMode>('chat');
  const [context, setContext] = useState<MessageContext | null>(null);
  
  return (
    <div className={cn('ai-panel', { 'ai-panel--open': isOpen })}>
      <div className="ai-panel__header">
        <Tabs value={activeMode} onValueChange={setActiveMode}>
          <TabsList>
            <TabsTrigger value="chat">💬 Chat</TabsTrigger>
            <TabsTrigger value="draft">✏️ Draft</TabsTrigger>
            <TabsTrigger value="analyze">🔍 Analyze</TabsTrigger>
          </TabsList>
        </Tabs>
        <Button variant="ghost" size="sm" onClick={onToggle}>
          ✕
        </Button>
      </div>
      
      <TabsContent value="chat">
        <ChatMode context={context} />
      </TabsContent>
      <TabsContent value="draft">
        <DraftMode context={context} />
      </TabsContent>
      <TabsContent value="analyze">
        <AnalyzeMode context={context} />
      </TabsContent>
    </div>
  );
}
```

#### **Day 2: AI Service Integration**
```typescript
// lib/ai/aiService.ts
import { OpenAI } from 'openai';

export class HealthcareAIService {
  private openai: OpenAI;
  
  constructor(apiKey: string) {
    this.openai = new OpenAI({ apiKey });
  }

  async analyzeFHIRMessage(message: string): Promise<AIAnalysisResult> {
    const completion = await this.openai.chat.completions.create({
      model: 'gpt-4',
      messages: [
        {
          role: 'system',
          content: `You are a healthcare interoperability expert. Analyze this FHIR message for:
          - Compliance with FHIR R4 specification
          - Potential PHI exposure
          - Vendor-specific patterns
          - Validation issues and fixes`
        },
        {
          role: 'user',
          content: `Analyze this FHIR message:\n\n${message}`
        }
      ],
    });

    return this.parseAIResponse(completion.choices[0].message.content);
  }

  async suggestMessageCompletion(partial: string, context: MessageContext): Promise<string[]> {
    // AI-powered field completion and validation
  }

  async explainValidationError(error: ValidationError, message: string): Promise<string> {
    // AI-powered error explanation and fix suggestions
  }
}
```

#### **Day 3: Activity Bar System**
```typescript
// components/workspace/ActivityBar/ActivityBar.tsx
import { ActivityBarItem } from './ActivityBarItem';

const ACTIVITY_ITEMS = [
  { id: 'explorer', icon: '📁', label: 'Explorer', shortcut: 'Cmd+Shift+E' },
  { id: 'search', icon: '🔍', label: 'Search', shortcut: 'Cmd+Shift+F' },
  { id: 'generator', icon: '⚗️', label: 'Generator', shortcut: 'Cmd+Shift+G' },
  { id: 'datasets', icon: '📊', label: 'Datasets', shortcut: 'Cmd+Shift+D' },
  { id: 'deident', icon: '🧬', label: 'De-identification', shortcut: 'Cmd+Shift+H' },
  { id: 'ai', icon: '🤖', label: 'AI Assistant', shortcut: 'Cmd+Shift+A' },
  { id: 'settings', icon: '⚙️', label: 'Settings', shortcut: 'Cmd+,' },
];

export function ActivityBar({ activeItem, onItemChange }: ActivityBarProps) {
  return (
    <nav className="activity-bar">
      {ACTIVITY_ITEMS.map(item => (
        <ActivityBarItem
          key={item.id}
          {...item}
          isActive={activeItem === item.id}
          onClick={() => onItemChange(item.id)}
        />
      ))}
    </nav>
  );
}
```

#### **Day 4: Panel Management System**
```typescript
// store/panelStore.ts
import { create } from 'zustand';

interface PanelState {
  // Left panel (Activity Bar content)
  leftPanelVisible: boolean;
  activeActivity: string;
  
  // Right panel (AI Assistant)
  rightPanelVisible: boolean;
  aiMode: 'chat' | 'draft' | 'analyze';
  
  // Panel controls
  toggleLeftPanel: () => void;
  toggleRightPanel: () => void;
  setActiveActivity: (activity: string) => void;
  setAIMode: (mode: 'chat' | 'draft' | 'analyze') => void;
}

export const usePanelStore = create<PanelState>((set) => ({
  leftPanelVisible: true,
  rightPanelVisible: false,
  activeActivity: 'explorer',
  aiMode: 'chat',
  
  toggleLeftPanel: () => set(state => ({ 
    leftPanelVisible: !state.leftPanelVisible 
  })),
  toggleRightPanel: () => set(state => ({ 
    rightPanelVisible: !state.rightPanelVisible 
  })),
  setActiveActivity: (activity) => set({ activeActivity: activity }),
  setAIMode: (mode) => set({ aiMode: mode }),
}));
```

#### **Day 5: Command Palette & Polish**
```typescript
// components/common/CommandPalette/CommandPalette.tsx
import { Command } from 'cmdk';

const AI_COMMANDS = [
  { id: 'ai-analyze', label: 'AI: Analyze Current Message', shortcut: 'Cmd+Shift+A' },
  { id: 'ai-draft', label: 'AI: Draft New Message', shortcut: 'Cmd+Shift+D' },
  { id: 'ai-explain', label: 'AI: Explain Validation Error', shortcut: 'Cmd+Shift+E' },
  { id: 'ai-suggest', label: 'AI: Suggest Field Completion', shortcut: 'Cmd+Shift+S' },
];

export function CommandPalette() {
  return (
    <Command.Dialog open={open} onOpenChange={setOpen}>
      <Command.Input placeholder="Type a command or ask AI..." />
      <Command.List>
        <Command.Group heading="Message Operations">
          <Command.Item onSelect={() => generateMessage()}>
            Generate Message
          </Command.Item>
          <Command.Item onSelect={() => validateMessage()}>
            Validate Message
          </Command.Item>
        </Command.Group>
        
        <Command.Group heading="AI Assistant">
          {AI_COMMANDS.map(cmd => (
            <Command.Item key={cmd.id} onSelect={() => executeAICommand(cmd.id)}>
              {cmd.label}
              <kbd>{cmd.shortcut}</kbd>
            </Command.Item>
          ))}
        </Command.Group>
      </Command.List>
    </Command.Dialog>
  );
}
```

---

## 🔌 **API Integration**

### **Backend Connection Architecture**

```typescript
// lib/api/client.ts
import { z } from 'zod';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

class ApiClient {
  private async request<T>(
    endpoint: string, 
    options?: RequestInit
  ): Promise<T> {
    const response = await fetch(`${API_BASE}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });

    if (!response.ok) {
      throw new ApiError(response.status, await response.text());
    }

    return response.json();
  }

  messages = {
    generate: (type: string, options: GenerationOptions) =>
      this.request<Message>('/api/messages/generate', {
        method: 'POST',
        body: JSON.stringify({ type, options }),
      }),

    validate: (message: string, standard: string) =>
      this.request<ValidationResult>('/api/messages/validate', {
        method: 'POST',
        body: JSON.stringify({ message, standard }),
      }),
  };

  workflows = {
    execute: (workflow: Workflow) =>
      this.request<WorkflowResult>('/api/workflows/execute', {
        method: 'POST',
        body: JSON.stringify(workflow),
      }),
  };
}

export const api = new ApiClient();
```

### **WebSocket for Real-time Updates**

```typescript
// lib/api/websocket.ts
export class MessageWebSocket {
  private ws: WebSocket | null = null;
  private reconnectTimer: NodeJS.Timeout | null = null;

  connect() {
    this.ws = new WebSocket('ws://localhost:5000/ws');
    
    this.ws.onopen = () => {
      console.log('WebSocket connected');
    };

    this.ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      this.handleMessage(data);
    };

    this.ws.onclose = () => {
      this.reconnect();
    };
  }

  private handleMessage(data: WebSocketMessage) {
    switch (data.type) {
      case 'validation':
        useMessageStore.setState({ 
          validationResults: data.payload 
        });
        break;
      case 'workflow-progress':
        useWorkflowStore.setState({ 
          progress: data.payload 
        });
        break;
    }
  }

  private reconnect() {
    this.reconnectTimer = setTimeout(() => {
      this.connect();
    }, 5000);
  }
}
```

---

## 🧪 **Testing Strategy**

### **Test Structure**
```
tests/
├── unit/
│   ├── components/      # Component logic tests
│   ├── hooks/           # Custom hook tests
│   └── utils/           # Utility function tests
├── integration/
│   ├── api/            # API integration tests
│   └── workflows/      # Workflow tests
└── e2e/
    ├── message-creation.spec.ts
    ├── validation.spec.ts
    └── workflow-execution.spec.ts
```

### **Testing Commands**
```json
{
  "scripts": {
    "test": "jest",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage",
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui"
  }
}
```

---

## 📊 **Performance Targets**

### **Core Web Vitals**
- **LCP** (Largest Contentful Paint): <2.5s
- **FID** (First Input Delay): <100ms
- **CLS** (Cumulative Layout Shift): <0.1

### **Application Metrics**
- **Bundle Size**: <200KB initial JS
- **Time to Interactive**: <3s
- **Message Validation**: <200ms
- **Workflow Execution**: Real-time feedback

### **Optimization Strategies**
- Code splitting by route
- Lazy loading heavy components
- Virtual scrolling for large lists
- Memoization of expensive operations
- Service Worker for offline support

---

## 🚢 **Deployment**

### **Development**
```bash
# Local development
npm run dev

# With backend API
API_URL=http://localhost:5000 npm run dev
```

### **Production Build**
```bash
# Build for production
npm run build

# Test production build
npm run start
```

### **Deployment Options**

#### **Option 1: Vercel (Recommended)**
```bash
# Install Vercel CLI
npm i -g vercel

# Deploy
vercel --prod
```

#### **Option 2: Docker**
```dockerfile
# Dockerfile
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:18-alpine
WORKDIR /app
COPY --from=builder /app/package*.json ./
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
RUN npm ci --production
EXPOSE 3000
CMD ["npm", "start"]
```

#### **Option 3: Static Export**
```bash
# next.config.js
module.exports = {
  output: 'export',
}

# Build static files
npm run build

# Serve with any static host
npx serve out
```

---

## 📈 **Success Metrics**

### **User Experience**
- Time to first successful message: <2 minutes
- Error resolution time: <1 minute average
- Workflow completion rate: >90%
- User satisfaction: >4.5/5

### **Technical**
- Crash rate: <0.1%
- API response time: <200ms p95
- Client-side errors: <1 per session
- Browser support: Chrome, Firefox, Safari, Edge

### **Business**
- Free to Pro conversion: >10%
- Daily active usage: >60%
- Feature adoption: >50% use workflows
- Support tickets: <5% of users

---

## 🎯 **Milestone Schedule**

### **Week 1**: Foundation ✓
- Project setup complete
- Design system implemented
- Basic layout working
- Storybook configured

### **Week 2**: Core Features
- Message editor functional
- Validation integrated
- Field inspector working
- Basic workflows

### **Week 3**: Advanced Features
- Workflow designer complete
- Execution engine working
- Templates available
- Command palette

### **Week 4**: Polish & Launch
- Performance optimized
- All tests passing
- Documentation complete
- Production deployed

---

## 📝 **Next Actions**

1. **Create project directory**
   ```bash
   mkdir pidgeon_gui
   cd pidgeon_gui
   ```

2. **Initialize Next.js**
   ```bash
   npx create-next-app@latest . --typescript --tailwind --app
   ```

3. **Install dependencies**
   ```bash
   npm install @radix-ui/react-dialog zustand @tanstack/react-query
   ```

4. **Copy brand assets**
   - Transfer CSS variables
   - Set up theme provider
   - Configure Tailwind

5. **Start development**
   ```bash
   npm run dev
   ```

---

*This plan serves as the technical blueprint for GUI implementation. Update as development progresses and new requirements emerge.*