# Pidgeon Testing Suite - GUI Design North Star

**Version**: 1.0  
**Created**: September 14, 2025  
**Purpose**: Comprehensive UX/UI design principles and implementation guide for Pidgeon Testing Suite GUI  
**Status**: Living document - update as design evolves

---

## 🎯 **Design Vision**

Transform healthcare message testing from a technical burden into an intuitive, visual, and even delightful experience. The Pidgeon Testing Suite GUI should feel like a professional workshop where healthcare integration engineers can craft, validate, and perfect their message flows with confidence and speed.

**Core Design Promise**: "See your messages, understand your errors, fix with confidence"

---

## 🧠 **Mental Models & User Psychology**

### **Primary Mental Model: "Healthcare Message Workshop"**

Users don't think in terms of individual tools - they think in workflows. Our GUI reflects this by providing a unified workspace where messages flow through natural stages:

```
Create/Import → Validate → Transform → Test → Export
```

**Design Implications**:
- Single workspace, not multiple pages
- Messages persist across operations
- Visual flow from left to right
- Clear sense of progression

### **Secondary Mental Models**:

1. **"Pipeline" Model**: Messages flow through stages like a factory
2. **"Canvas" Model**: Messages are objects to be shaped and refined
3. **"Debugging" Model**: Problems are puzzles to solve interactively

---

## 🎨 **Core UX Principles**

### **1. Progressive Disclosure**
Start simple for new users, scale to complex for power users.

**Implementation**:
- **Quick Mode**: One-click common scenarios
- **Guided Mode**: Step-by-step wizards with explanations
- **Expert Mode**: Full field-level control, raw editing

**Mode Persistence**: User's choice remembered across sessions

### **2. Visual Feedback**
Make abstract healthcare messages tangible and understandable.

**Implementation**:
- Messages as visual "cards"
- Color coding: Blue (processing) → Green (valid) → Red (errors)
- Smooth animations for transformations
- Split-pane view for context

### **3. Smart Defaults with Memory**
Reduce repetitive work through intelligent suggestions.

**Implementation**:
- Remember last 10 workflows
- Auto-suggest next actions
- "Recently used" sections everywhere
- One-click replay of workflows

### **4. Error Recovery as First-Class UX**
Errors are learning opportunities, not failures.

**Implementation**:
- Inline error display
- Click-to-fix interactions
- "Fix All Similar" capabilities
- Comprehensive undo/redo

### **5. Continuous Validation**
Never let users be surprised by errors.

**Implementation**:
- Real-time validation as you type
- Visual indicators on every field
- Proactive warnings for common issues
- Vendor-specific quirk detection

---

## 🏗️ **Information Architecture**

### **Enhanced Layout Structure with AI Integration**

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Control Bar                                     [🔍] [📊] [🤖] [×] [□] │
│  [Mode] [Project ▼] [|||]         [Share] [Export] [Settings] [Profile] │
├──────┬─────────────────────────────────────────────────┬───────────────┤
│      │                                                 │               │
│ Act. │              Message Canvas                     │   AI Panel    │
│ Bar  │                                                 │ (Collapsible) │
│      │   ┌───────────────────────────────────────┐    │               │
│ [📁] │   │                                       │    │ ┌───────────┐ │
│ [🔍] │   │         Message Editor                │    │ │Chat│Draft │ │
│ [⚗️] │   │                                       │    │ ├───────────┤ │
│ [📊] │   └───────────────────────────────────────┘    │ │💬 How can │ │
│ [🧬] │                                                 │ │I help with│ │
│ [🤖] │   ┌───────────────────────────────────────┐    │ │healthcare │ │
│ [⚙️] │   │       Validation Results              │    │ │messages?  │ │
│      │   └───────────────────────────────────────┘    │ │           │ │
│      │                                                 │ │[_______]  │ │
│      │                                                 │ │  [Send]   │ │
│      │                                                 │ └───────────┘ │
├──────┴─────────────────────────────────────────────────┴───────────────┤
│  Status Bar: [◀ Undo] [Redo ▶] Ready | 1 Error | AI: Online [Run ▶]   │
└─────────────────────────────────────────────────────────────────────────┘
```

### **Activity Bar Feature Specification**

**Primary Features** (Icon → Function):
- **📁 Explorer**: File management, project navigation, recent items
- **🔍 Search**: Message search, field inspection, cross-message queries  
- **⚗️ Generator**: Message creation, templates, scenarios, bulk operations
- **📊 Datasets**: Building blocks (medications, names, addresses) for customization
- **🧬 De-identification**: PHI detection, masking, synthetic replacement
- **🤖 AI Assistant**: Healthcare message intelligence (panel toggle)
- **⚙️ Settings**: Configuration, themes, shortcuts, integrations

**Panel Management Strategy**:
- **Left Panel Content**: Changes based on Activity Bar selection
- **Right Panel (AI)**: Independent toggle from top-right controls  
- **VSCode-Style Controls**: Panel visibility toggles in top-right corner
- **Context Awareness**: AI panel adapts to current Activity Bar context

### **AI Panel Architecture**

**Three Operating Modes**:
1. **Chat Mode**: Conversational healthcare message assistance
   - Message troubleshooting and debugging
   - Standard specification clarification
   - Workflow guidance and best practices
   
2. **Draft Mode**: Intelligent message composition
   - Template suggestions based on context
   - Field completion and validation
   - Multi-message scenario building
   
3. **Analyze Mode**: Deep message inspection
   - PHI detection and compliance review
   - Vendor pattern analysis
   - Cross-standard compatibility checks

**Panel Controls**:
- **Tab Selection**: Chat | Draft | Analyze with active state indicators
- **Collapse/Expand**: VSCode-style panel toggle (top-right corner)
- **Context Sync**: Automatically adapts to selected message/field
- **History Access**: Previous conversations and analysis results

### **Activity Bar System Design**

**Icon Design Principles**:
- **Healthcare Context**: Icons convey medical/technical purpose instantly
- **Visual Hierarchy**: Active state uses gradient accent from brand palette
- **Accessibility**: 44x44px touch targets, high contrast ratios
- **Consistency**: Uniform spacing, sizing, and interaction patterns

**Activity States & Visual Feedback**:
```css
/* Inactive state */
.activity-item {
  opacity: 0.6;
  color: var(--ph-graphite-500);
  transition: all 0.2s ease;
}

/* Hover state */
.activity-item:hover {
  opacity: 0.8;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 6px;
}

/* Active state */
.activity-item--active {
  opacity: 1;
  color: var(--ph-white);
  background: var(--ph-gradient-wing);
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

/* Focus state (keyboard navigation) */
.activity-item:focus-visible {
  outline: 2px solid var(--ph-blue-400);
  outline-offset: 2px;
}
```

**Panel Content Mapping**:
- **📁 Explorer** → File tree, recent projects, message history
- **🔍 Search** → Global search, field finder, message queries
- **⚗️ Generator** → Message templates, bulk operations, scenarios  
- **📊 Datasets** → Medication library, name generators, address pools
- **🧬 De-identification** → PHI scanner, masking tools, synthetic replacement
- **🤖 AI Assistant** → Healthcare intelligence panel toggle
- **⚙️ Settings** → Preferences, themes, shortcuts, integrations

**Panel State Management**:
```typescript
interface ActivityBarState {
  activePanel: ActivityPanel;
  panelVisible: boolean;
  panelWidth: number;
  aiPanelVisible: boolean;
  aiPanelWidth: number;
}

type ActivityPanel = 'explorer' | 'search' | 'generator' | 'datasets' | 'deident' | 'settings';
```

**Keyboard Navigation Support**:
- **Cmd+Shift+E**: Explorer panel
- **Cmd+Shift+F**: Search panel  
- **Cmd+Shift+G**: Generator panel
- **Cmd+Shift+D**: Datasets panel
- **Cmd+Shift+H**: De-identification panel
- **Cmd+Shift+A**: AI Assistant toggle
- **Cmd+,**: Settings panel

**Key Layout Features**:
- **Activity Bar (Left)**: Icon-based feature navigation with active state indicators
- **Main Canvas (Center)**: Primary workspace for message editing and validation
- **AI Panel (Right)**: Intelligent assistant with Chat/Draft/Analyze modes
- **Panel Controls (Top-Right)**: VSCode-style toggles for search, datasets, AI panel
- **Status Bar (Bottom)**: Contextual information and primary actions

### **Component Hierarchy**

```
App
├── ControlBar
│   ├── ModeSelector
│   ├── ProjectManager
│   └── GlobalActions
├── Workspace
│   ├── Library (Collapsible)
│   │   ├── RecentItems
│   │   ├── SavedMessages
│   │   └── Templates
│   ├── Canvas
│   │   ├── MessageEditor
│   │   └── ValidationPanel
│   └── ToolPalette
│       ├── Actions
│       └── Properties
└── ActionBar
    ├── History
    ├── Status
    └── PrimaryAction
```

---

## 💫 **Interaction Patterns**

### **Direct Manipulation**
Everything that looks interactive should be interactive.

- **Drag & Drop**: Fields between messages, messages between stages
- **Right-Click**: Context menus on all elements
- **Double-Click**: Inline editing of values
- **Hover**: Rich tooltips with spec details

### **Keyboard Navigation**
Power users should never need the mouse.

- **Cmd/Ctrl+K**: Universal command palette
- **Tab**: Navigate through fields
- **Arrow Keys**: Navigate tree structures
- **Shortcuts**: Customizable key bindings

### **Visual Hierarchy**
Guide the eye to what matters most.

```
Hierarchy Levels:
1. Primary Actions (largest, blue gradient)
2. Current Focus (highlighted border)
3. Secondary Actions (medium, white)
4. Contextual Info (small, gray)
5. Disabled/Inactive (muted, low contrast)
```

### **Feedback Timing**
Every action gets appropriate feedback.

- **Instant** (<50ms): Hover states, clicks
- **Fast** (<200ms): Validation, simple operations
- **Progressive** (>200ms): Show progress for long operations
- **Chunked**: Large operations show incremental progress

---

## 🎨 **Visual Design System**

### **Color Palette**
Based on Pidgeon Health brand identity.

```css
/* Primary - Blues & Teals */
--primary-gradient: linear-gradient(135deg, #69d6ff 0%, #2ea2ff 60%, #0f63d6 100%);
--primary-500: #157ef3;
--primary-hover: #2ea2ff;

/* Semantic Colors */
--success: #2fbf71;
--success-gradient: linear-gradient(to bottom, #7be7b0 0%, #2fbf71 55%, #19995a 100%);
--danger: #e75a47;
--danger-gradient: linear-gradient(to bottom, #ff8e7a 0%, #e75a47 55%, #cc3a2a 100%);
--warning: #f5a623;
--info: #10b7c4;

/* Neutrals */
--black: #0a0a0a;
--gray-900: #1e2730;
--gray-700: #2a3642;
--gray-500: #6b7280;
--gray-300: #d1d5db;
--gray-100: #f3f4f6;
--white: #ffffff;

/* Surfaces */
--surface-primary: #ffffff;
--surface-secondary: #f9fafb;
--surface-tertiary: #f3f4f6;
```

### **Typography Scale**

```css
/* Font Family */
--font-sans: 'Inter', system-ui, -apple-system, sans-serif;
--font-mono: 'JetBrains Mono', 'Courier New', monospace;

/* Type Scale */
--text-xs: 12px/16px;    /* Labels, hints */
--text-sm: 14px/20px;    /* Body text */
--text-base: 16px/24px;  /* Default */
--text-lg: 18px/28px;    /* Subheadings */
--text-xl: 20px/28px;    /* Section headers */
--text-2xl: 24px/32px;   /* Page headers */
--text-3xl: 30px/36px;   /* Display */

/* Font Weights */
--font-normal: 400;
--font-medium: 500;
--font-semibold: 600;
--font-bold: 700;
```

### **Spacing System**
All spacing based on 4px grid.

```css
--space-1: 4px;
--space-2: 8px;
--space-3: 12px;
--space-4: 16px;
--space-5: 20px;
--space-6: 24px;
--space-8: 32px;
--space-10: 40px;
--space-12: 48px;
--space-16: 64px;
```

### **Component Patterns**

#### **Buttons**
```css
.button {
  padding: 10px 16px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 0.2s;
}

.button--primary {
  background: var(--primary-gradient);
  color: white;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.button--secondary {
  background: white;
  border: 1px solid var(--gray-300);
  color: var(--gray-900);
}
```

#### **Cards**
```css
.card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  padding: var(--space-4);
}

.card--interactive {
  transition: all 0.2s;
  cursor: pointer;
}

.card--interactive:hover {
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  transform: translateY(-2px);
}
```

#### **Input Fields**
```css
.input {
  background: white;
  border: 1px solid var(--gray-300);
  border-radius: 6px;
  padding: 8px 12px;
  transition: all 0.2s;
}

.input:focus {
  outline: none;
  border-color: var(--primary-500);
  box-shadow: 0 0 0 3px rgba(21, 126, 243, 0.1);
}
```

---

## 🔄 **State Management**

### **Application States**

```typescript
interface AppState {
  // Mode State
  mode: 'quick' | 'guided' | 'expert';
  
  // Project State
  currentProject: Project;
  recentProjects: Project[];
  
  // Message State
  activeMessage: Message | null;
  messageHistory: Message[];
  validationResults: ValidationResult[];
  
  // UI State
  leftPanelOpen: boolean;
  rightPanelOpen: boolean;
  theme: 'light' | 'dark';
  
  // Workflow State
  activeWorkflow: Workflow | null;
  workflowStep: number;
  workflowResults: any[];
}
```

### **Persistence Strategy**

1. **Local Storage**: User preferences, recent items
2. **Session Storage**: Active message, undo history
3. **IndexedDB**: Saved messages, projects
4. **Cloud Sync**: Pro accounts only (future)

---

## 🎬 **Animation & Motion**

### **Animation Principles**

1. **Purpose**: Every animation has a functional purpose
2. **Performance**: Never sacrifice performance for animation
3. **Subtlety**: Enhance, don't distract
4. **Consistency**: Same actions have same animations

### **Standard Animations**

```css
/* Micro-interactions */
--transition-fast: 150ms ease-out;
--transition-base: 200ms ease-out;
--transition-slow: 300ms ease-out;

/* Enter/Exit */
@keyframes slideIn {
  from { transform: translateX(-100%); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* Feedback */
@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.05); }
}
```

---

## 🔌 **API Integration Architecture**

### **Service Layer Design**

```typescript
// Core Services (align with CLI backend)
interface MessageService {
  generate(type: string, options: GenerationOptions): Promise<Message>;
  validate(message: Message, mode: ValidationMode): Promise<ValidationResult>;
  transform(message: Message, target: string): Promise<Message>;
}

interface WorkflowService {
  create(workflow: WorkflowDefinition): Promise<Workflow>;
  execute(workflow: Workflow): Promise<WorkflowResult>;
  save(workflow: Workflow): Promise<void>;
}

interface ProjectService {
  load(id: string): Promise<Project>;
  save(project: Project): Promise<void>;
  list(): Promise<ProjectSummary[]>;
}
```

### **Real-time Updates**
- WebSocket for live validation
- Server-Sent Events for workflow progress
- Polling fallback for compatibility

---

## 📱 **Responsive Design**

### **Breakpoints**

```css
--screen-sm: 640px;   /* Mobile landscape */
--screen-md: 768px;   /* Tablet portrait */
--screen-lg: 1024px;  /* Tablet landscape */
--screen-xl: 1280px;  /* Desktop */
--screen-2xl: 1536px; /* Wide desktop */
```

### **Responsive Strategy**

1. **Desktop First**: Primary use case is desktop developers
2. **Tablet Support**: Simplified layout, hidden sidebars
3. **Mobile Viewing**: Read-only message viewing (no editing)

---

## ♿ **Accessibility Requirements**

### **WCAG 2.1 AA Compliance**

1. **Color Contrast**: 4.5:1 for normal text, 3:1 for large text
2. **Keyboard Navigation**: All features keyboard accessible
3. **Screen Readers**: Proper ARIA labels and roles
4. **Focus Indicators**: Visible focus states on all interactive elements
5. **Error Messages**: Clear, actionable error descriptions

### **Healthcare-Specific Accessibility**

1. **High Contrast Mode**: For clinical environments
2. **Large Touch Targets**: 44x44px minimum
3. **Clear Typography**: No font smaller than 12px
4. **Color Independence**: Never rely on color alone

---

## 🚀 **Implementation Roadmap**

### **Phase 1: Foundation (Week 1-2)**
1. Set up Next.js project with TypeScript
2. Implement design system (colors, typography, spacing)
3. Create base layout components
4. Set up state management (Zustand/Redux)
5. Implement theme switching

### **Phase 2: Core Components (Week 3-4)**
1. Message editor component
2. Validation panel
3. Field inspector
4. Error display system
5. Command palette

### **Phase 3: Workflows (Week 5-6)**
1. Workflow designer
2. Step execution engine
3. Progress visualization
4. Results display
5. Export functionality

### **Phase 4: Polish (Week 7-8)**
1. Animations and transitions
2. Keyboard shortcuts
3. Performance optimization
4. Error handling
5. User onboarding

---

## 📊 **Success Metrics**

### **Performance Targets**
- First Contentful Paint: <1.5s
- Time to Interactive: <3s
- Response to input: <50ms
- Validation feedback: <200ms

### **Usability Targets**
- New user to first success: <5 minutes
- Common tasks: <3 clicks
- Error resolution: <1 minute average
- User satisfaction: >4.5/5 stars

---

## 🔍 **Design Validation**

### **User Testing Protocol**

1. **Prototype Testing**: Clickable Figma prototype with 5 users
2. **Alpha Testing**: Functional MVP with 10 power users
3. **Beta Testing**: Full feature set with 50+ users
4. **Continuous Feedback**: In-app feedback widget

### **Key Validation Questions**

1. Can users create a valid message in <2 minutes?
2. Do users understand validation errors immediately?
3. Can users build a multi-step workflow intuitively?
4. Do power users find keyboard shortcuts discoverable?
5. Does the tool feel professional and trustworthy?

---

## 📝 **Design Principles Checklist**

Before implementing any feature, verify:

- [ ] **Intuitive**: Would a new user understand this?
- [ ] **Efficient**: Does this save time for power users?
- [ ] **Consistent**: Does this match our existing patterns?
- [ ] **Accessible**: Can everyone use this feature?
- [ ] **Performant**: Will this stay fast at scale?
- [ ] **Delightful**: Does this feel good to use?

---

## 🎯 **North Star Experience**

The perfect user session:

1. **Launch**: App opens instantly, remembers where you left off
2. **Create**: Generate a complex message with 2 clicks
3. **Validate**: See issues immediately with clear fixes
4. **Fix**: Apply fixes with confidence
5. **Test**: Run through scenarios smoothly
6. **Export**: Get exactly the format needed
7. **Feel**: Professional, powerful, and oddly enjoyable

**Ultimate Success**: Users choose Pidgeon not because they have to, but because they want to.

---

*This document serves as our north star for all GUI design decisions. When in doubt, refer back to these principles. When learning something new, update this document.*