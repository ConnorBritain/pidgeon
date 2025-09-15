# Pidgeon GUI Development Rules
**Purpose**: Essential guidelines for AI agents during GUI development and component architecture  
**Enforcement**: These rules ensure consistent healthcare UX and maintain design system integrity  
**Review Required**: Every significant GUI change must validate against these rules  

---

## 🚨 **MANDATORY UI ERROR RESPONSE**

When encountering ANY UI compilation errors, layout issues, accessibility violations, or UX inconsistencies:

### **STOP-THINK-ACT Protocol**
1. **STOP**: Do not immediately attempt fixes
2. **THINK**: Perform design system analysis (see `docs/visuals/GUI_DESIGN.md` principles)
3. **ACT**: Make minimal, principled changes that maintain healthcare UX consistency

### **Required Response Format**
```
🚨 GUI METHODOLOGY CHECKPOINT

STOP: [Brief description of UI issue encountered]

THINK - Design System Analysis:
- UX Pattern Issue: [What healthcare interaction pattern is involved?]
- Brand Impact: [Does this affect Pidgeon Health visual identity?]
- Accessibility Impact: [Are WCAG 2.1 AA requirements met?]
- User Impact: [How does this affect healthcare professional workflows?]

ACT - Healthcare-Focused Fix Plan:
1. [Specific design system correction]
2. [Accessibility verification step]
3. [Healthcare UX validation step]
```

**Violation**: Any UI fixing without this protocol violates healthcare UX standards.

---

## 🏗️ **SACRED GUI ARCHITECTURAL PRINCIPLES** 
*From `docs/visuals/GUI_DESIGN.md` - IMMUTABLE for healthcare UX integrity*

### **1. Healthcare Mental Model First**
```tsx
// ✅ CORRECT: Healthcare professional workflow drives design
const WorkflowSteps = {
  CREATE_MESSAGE: "Generate realistic test message",
  VALIDATE_COMPLIANCE: "Check against vendor patterns", 
  IDENTIFY_ISSUES: "Spot integration problems early",
  FIX_CONFIDENTLY: "Apply fixes with healthcare context"
};

// ❌ FORBIDDEN: Generic developer tools patterns
const GenericSteps = {
  INPUT: "Enter data",
  PROCESS: "Run operation", 
  OUTPUT: "View results"
};
```

### **2. Progressive Disclosure Architecture**
```tsx
// ✅ CORRECT: Start simple, scale to complex
interface UserExperienceMode {
  QUICK: "One-click common scenarios",
  GUIDED: "Step-by-step with healthcare explanations", 
  EXPERT: "Full field-level control for power users"
}

// ❌ FORBIDDEN: Exposing all complexity immediately
<MessageEditor 
  showAllFields={true}
  showAdvancedOptions={true}
  showDebugInfo={true} 
/>
```

### **3. AI Panel Integration (Never Block Core Workflow)**
```tsx
// ✅ CORRECT: AI enhances, doesn't replace core functions
<Workspace>
  <MessageCanvas /> {/* Primary workflow */}
  <AIPanel position="right" collapsible={true} />
</Workspace>

// ❌ FORBIDDEN: AI-first interfaces that hide core functionality
<AIOnlyInterface>
  <ChatBot />
  <HiddenWorkspace /> {/* Core functionality buried */}
</AIOnlyInterface>
```

### **4. Brand-Consistent Visual Hierarchy**
```tsx
// ✅ CORRECT: Pidgeon Health brand variables
const ButtonStyles = {
  primary: "bg-ph-gradient-wing text-ph-white",
  success: "bg-ph-gradient-success text-ph-white",
  danger: "bg-ph-gradient-danger text-ph-white"
};

// ❌ FORBIDDEN: Generic colors not from brand system
const GenericStyles = {
  primary: "bg-blue-500 text-white",
  success: "bg-green-500 text-white" 
};
```

---

## 💼 **HEALTHCARE UX STANDARDS**

### **❌ NEVER: Consumer App Patterns in Healthcare**
- **Casual language**: "Awesome!", "Sweet!", "Let's go!" in professional healthcare context
- **Gaming elements**: Achievement badges, streaks, "level up" language
- **Social media patterns**: Likes, shares, comments on medical data
- **Consumer onboarding**: Multi-step welcome wizards for experienced professionals

### **✅ ALWAYS: Professional Healthcare UX**
```tsx
// ❌ BAD - Consumer app language
<Toast>
  🎉 Awesome! Your message is totally valid!
</Toast>

// ✅ GOOD - Professional healthcare language  
<ValidationResult>
  ✅ Message validates against Epic 2023 configuration
  Compliance: HL7 v2.3 | 14 segments verified
</ValidationResult>
```

### **Error Communication Standards**
```tsx
// ✅ CORRECT: Healthcare professional error communication
interface HealthcareError {
  severity: "Critical" | "Warning" | "Information";
  context: "Patient Safety" | "Data Integrity" | "System Integration";
  action: "Fix Required" | "Review Recommended" | "Informational Only";
  explanation: string; // Clear, actionable healthcare context
}

// ❌ FORBIDDEN: Generic developer error patterns
interface GenericError {
  level: "error" | "warn" | "info";
  message: "Something went wrong"; // Vague, unhelpful
}
```

### **Accessibility Requirements (WCAG 2.1 AA+)**
- **Color Contrast**: 4.5:1 minimum for healthcare professional environments
- **Touch Targets**: 44x44px minimum for clinical workstation use
- **Keyboard Navigation**: Complete workflow accessible without mouse
- **Screen Reader Support**: Full semantic markup for assistive technology
- **Focus Indicators**: Always visible for keyboard navigation

---

## 🎨 **COMPONENT ARCHITECTURE RULES**

### **❌ CRITICAL VIOLATION: Inline Styles Breaking Design System**
```tsx
// ❌ NEVER DO THIS - breaks brand consistency
<Button style={{
  background: "#3B82F6", // Random blue
  padding: "12px 24px",  // Non-standard spacing
  fontSize: "16px"       // Doesn't match typography scale
}}>
  Generate Message
</Button>
```

### **✅ CORRECT: Design System Component Usage**
```tsx
// ✅ Components use design system tokens
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

<Button 
  variant="primary" 
  size="md"
  className={cn("healthcare-action-button")}
>
  Generate HL7 Message
</Button>
```

### **Component Hierarchy Standards**
```tsx
// ✅ CORRECT: Clear component boundaries with healthcare context
interface HealthcareWorkspace {
  activityBar: ActivityBarComponent;    // Feature navigation
  messageCanvas: MessageCanvasComponent; // Primary work area  
  aiPanel: AIPanelComponent;            // Intelligent assistance
  statusBar: StatusBarComponent;        // System feedback
}

// ❌ FORBIDDEN: Generic layout without healthcare purpose
interface GenericLayout {
  sidebar: SidebarComponent;
  main: MainComponent;
  aside: AsideComponent;
}
```

---

## 🔍 **HEALTHCARE UX PATTERNS**

### **Progressive Disclosure Implementation**
```tsx
// ✅ CORRECT: Healthcare complexity management
const MessageEditor = ({ mode }: { mode: UXMode }) => {
  switch (mode) {
    case 'QUICK':
      return <QuickMessageTemplate />;
    case 'GUIDED':  
      return <StepByStepWizard showExplanations={true} />;
    case 'EXPERT':
      return <FullMessageEditor showAllFields={true} />;
  }
};

// ❌ FORBIDDEN: One-size-fits-all complexity
const OverwhelminqEditor = () => (
  <div>
    {/* All 47 HL7 segment types visible at once */}
    <ComplexForm fields={allPossibleFields} />
  </div>
);
```

### **Error Recovery as First-Class UX**
```tsx
// ✅ CORRECT: Healthcare error recovery patterns
interface ErrorRecoveryProps {
  error: ValidationError;
  onFix: (fix: AutoFix) => void;
  onLearnMore: () => void;
}

const ErrorWithRecovery = ({ error, onFix, onLearnMore }: ErrorRecoveryProps) => (
  <Card className="border-ph-danger">
    <CardHeader>
      <AlertTriangle className="text-ph-danger" />
      <h3>HL7 Validation Issue</h3>
    </CardHeader>
    <CardContent>
      <p>{error.explanation}</p>
      <div className="actions">
        <Button onClick={() => onFix(error.suggestedFix)}>
          Fix Automatically
        </Button>
        <Button variant="secondary" onClick={onLearnMore}>
          Learn About HL7 PID Segment
        </Button>
      </div>
    </CardContent>
  </Card>
);
```

---

## 📱 **RESPONSIVE & DEVICE STANDARDS**

### **Healthcare Environment Priorities**
```tsx
// ✅ CORRECT: Healthcare device priority order
const DevicePriorities = {
  DESKTOP_WORKSTATION: "Primary - full feature set",
  TABLET_LANDSCAPE: "Secondary - simplified workflows", 
  MOBILE_PORTRAIT: "Viewing only - no editing"
};

// ❌ FORBIDDEN: Mobile-first for healthcare professionals
const MobileFirst = {
  MOBILE: "Primary interface", // Wrong for healthcare workstations
  DESKTOP: "Enhanced version"
};
```

### **Responsive Implementation**
```tsx
// ✅ CORRECT: Desktop-first with healthcare context
const ResponsiveMessageEditor = () => (
  <div className={cn(
    "message-editor",
    "grid grid-cols-12 gap-4", // Desktop: full grid
    "lg:grid-cols-8",          // Tablet: simplified
    "md:block"                 // Mobile: single column view-only
  )}>
    <MessageCanvas className="col-span-8 lg:col-span-6" />
    <ValidationPanel className="col-span-4 lg:col-span-2 md:hidden" />
  </div>
);
```

---

## 🔌 **CLI-GUI HARMONIZATION RULES**

### **❌ CRITICAL VIOLATION: GUI Operations Not Exportable to CLI**
```tsx
// ❌ NEVER DO THIS - GUI-only operations
const generateMessage = () => {
  // Magic GUI logic that can't be replicated in CLI
  const result = blackBoxGUIFunction();
  updateUI(result);
};
```

### **✅ CORRECT: Service Layer Integration**
```tsx
// ✅ GUI operations export CLI commands
const MessageGenerationPanel = () => {
  const [cliCommand, setCLICommand] = useState('');
  
  const generateMessage = async (options: GenerationOptions) => {
    // Build CLI command
    const command = `pidgeon generate --type ${options.type} --count ${options.count}`;
    setCLICommand(command);
    
    // Use same service layer as CLI
    const result = await messageService.generate(options);
    
    return { result, cliCommand: command };
  };
  
  return (
    <div>
      <GenerationForm onSubmit={generateMessage} />
      <CLIExport command={cliCommand} />
    </div>
  );
};
```

### **CLI-GUI Feature Parity Rules**
```tsx
// ✅ REQUIRED: All CLI operations accessible in GUI
interface CLIGUIMapping {
  'pidgeon generate': MessageGenerationPanel;
  'pidgeon validate': ValidationPanel;
  'pidgeon deident': DeidentificationPanel;
  'pidgeon config': ConfigurationPanel;
  'pidgeon workflow': WorkflowWizard;      // Pro feature
  'pidgeon diff': DiffAnalysisPanel;       // Pro feature
}
```

---

## 🤖 **AI INTEGRATION STANDARDS**

### **AI Panel Architecture Requirements**
```tsx
// ✅ CORRECT: Healthcare AI assistant patterns
interface HealthcareAIPanel {
  modes: {
    CHAT: "Conversational healthcare message assistance";
    DRAFT: "Intelligent message composition";
    ANALYZE: "Deep message inspection and compliance";
  };
  context: MessageContext;
  privacy: "On-premises processing for PHI safety";
}

// ❌ FORBIDDEN: Generic chatbot patterns
interface GenericChatbot {
  chat: "General purpose conversation";
  context: "Unknown";
  privacy: "Cloud processing assumed";
}
```

### **AI Context Management**
```tsx
// ✅ CORRECT: Healthcare-specific AI context
interface MessageContext {
  standard: "HL7v2" | "FHIR" | "NCPDP";
  messageType: string;
  vendorConfig?: VendorConfiguration;
  validationErrors?: ValidationError[];
  phiStatus: "Identified" | "DeIdentified" | "Synthetic";
}

const AIPanel = ({ context }: { context: MessageContext }) => {
  const aiPrompt = buildHealthcarePrompt(context);
  
  return (
    <Panel>
      <AIChat 
        systemPrompt={aiPrompt}
        context={context}
        safeguards={PHI_SAFETY_RULES}
      />
    </Panel>
  );
};
```

---

## 🔐 **SECURITY & PHI SAFETY RULES**

### **PHI Handling in GUI**
```tsx
// ✅ CORRECT: PHI-safe GUI patterns
interface PHIMask {
  field: string;
  originalValue: string;
  maskedValue: string;
  deidentified: boolean;
}

const SecureMessageDisplay = ({ message, phiMasks }: {
  message: string;
  phiMasks: PHIMask[];
}) => {
  const displayMessage = applyPHIMasks(message, phiMasks);
  
  return (
    <div className="secure-message-container">
      <PHIWarningBanner visible={hasPHI(phiMasks)} />
      <MessageEditor value={displayMessage} readOnly={hasPHI(phiMasks)} />
    </div>
  );
};
```

### **AI Safety Guardrails**
```tsx
// ✅ REQUIRED: PHI safety in AI interactions
const aiSafetyRules = {
  noPHIInPrompts: true,
  onPremisesProcessing: true,
  auditTrail: true,
  userConsent: "explicit"
};

const SafeAIInteraction = ({ message }: { message: string }) => {
  const phiDetected = detectPHI(message);
  
  if (phiDetected) {
    return <PHIWarning />;
  }
  
  return <AIAssistance message={deidentify(message)} />;
};
```

---

## 📊 **PERFORMANCE & ACCESSIBILITY RULES**

### **Healthcare Workstation Performance**
```tsx
// ✅ REQUIRED: Clinical workstation performance targets
const PerformanceTargets = {
  firstContentfulPaint: "<1.5s",
  timeToInteractive: "<3s", 
  messageValidation: "<200ms",
  aiResponse: "<2s",
  keyboardNavigation: "<50ms"
};

// ❌ FORBIDDEN: Blocking UI operations
const SlowMessageProcessor = () => {
  const processMessage = (message: string) => {
    setLoading(true); // Blocks entire UI
    // 5+ second operation without progress
    const result = heavyProcessing(message);
    setLoading(false);
  };
};

// ✅ CORRECT: Progressive loading with healthcare context
const ResponsiveMessageProcessor = () => {
  const processMessage = async (message: string) => {
    setProgress({ step: "Parsing HL7 structure", percent: 0 });
    const parsed = await parseMessage(message);
    
    setProgress({ step: "Validating segments", percent: 40 });
    const validated = await validateMessage(parsed);
    
    setProgress({ step: "Checking vendor patterns", percent: 80 });
    const analyzed = await analyzeVendorPatterns(validated);
    
    setProgress({ step: "Complete", percent: 100 });
    return analyzed;
  };
};
```

---

## 🧪 **GUI TESTING RULES**

### **Healthcare Scenario Testing**
```tsx
// ✅ REQUIRED: Test healthcare professional workflows
describe('Message Generation Workflow', () => {
  test('Should allow quick ADT message creation for ER workflow', async () => {
    const user = userEvent.setup();
    render(<MessageWorkspace mode="QUICK" />);
    
    // Healthcare professional quick workflow
    await user.click(screen.getByRole('button', { name: /Quick ADT/i }));
    await user.selectOptions(screen.getByLabelText(/Message Type/i), 'ADT^A01');
    await user.click(screen.getByRole('button', { name: /Generate/i }));
    
    // Validate healthcare-specific output
    expect(screen.getByText(/Valid HL7 ADT\^A01 message/i)).toBeInTheDocument();
    expect(screen.getByText(/MSH\|/)).toBeInTheDocument(); // HL7 structure
  });
  
  test('Should export CLI command for reproducibility', async () => {
    // Test CLI-GUI harmonization
    const { cliCommand } = await generateMessage({ type: 'ADT^A01' });
    expect(cliCommand).toBe('pidgeon generate --type "ADT^A01" --count 1');
  });
});
```

### **Accessibility Testing Requirements**
```tsx
// ✅ REQUIRED: Accessibility testing for healthcare professionals
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

test('Message Editor meets WCAG 2.1 AA for healthcare use', async () => {
  const { container } = render(<MessageEditor />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
  
  // Healthcare-specific accessibility tests
  expect(screen.getByRole('textbox')).toHaveAccessibleName();
  expect(screen.getByRole('button', { name: /Validate/i })).toBeVisible();
});
```

---

## 🎯 **BUSINESS MODEL INTEGRATION**

### **Professional Tier Feature Gating**
```tsx
// ✅ CORRECT: Clear Professional tier boundaries in GUI
const WorkflowWizard = () => {
  const { isPro } = useSubscription();
  
  if (!isPro) {
    return (
      <ProfessionalFeatureGate
        feature="Workflow Wizard"
        description="Guided multi-step healthcare scenarios"
        ctaText="Upgrade to Professional"
      />
    );
  }
  
  return <WorkflowWizardComponent />;
};

// ❌ FORBIDDEN: Hidden paywalls or unclear feature boundaries
const UnclearFeatureGating = () => {
  const handleClick = () => {
    if (!isPro) {
      // Hidden paywall - poor UX
      showUpgradeModal();
    }
  };
};
```

### **Free Tier Experience Excellence**
```tsx
// ✅ REQUIRED: Excellent free tier that drives conversion
const FreeMessageGeneration = () => (
  <FeaturePanel>
    <h2>Message Generation</h2>
    <p>Generate realistic HL7, FHIR, and NCPDP messages</p>
    
    {/* Full functionality in free tier */}
    <MessageGeneratorForm 
      standards={["HL7v2", "FHIR", "NCPDP"]}
      messageTypes={CORE_MESSAGE_TYPES}
      datasetSize="basic" // 25 meds, 50 names
    />
    
    {/* Clear upgrade value proposition */}
    <UpgradeHint>
      Professional: 150+ medications, AI enhancement, unlimited scenarios
    </UpgradeHint>
  </FeaturePanel>
);
```

---

## 📋 **PRE-COMMIT GUI VALIDATION**

### **Before ANY GUI Commit, Verify**:
- [ ] **Design system compliance** (Uses Pidgeon Health brand tokens)
- [ ] **Healthcare UX patterns** (Professional language, appropriate complexity)
- [ ] **Accessibility requirements** (WCAG 2.1 AA compliance verified)
- [ ] **CLI-GUI harmonization** (Operations export CLI commands)
- [ ] **Progressive disclosure** (Quick/Guided/Expert modes available)
- [ ] **AI integration safety** (PHI handling, on-premises processing)
- [ ] **Performance targets** (Message operations <200ms)
- [ ] **Professional tier boundaries** (Clear free/pro separation)

### **GUI Architecture Validation Commands**
```bash
# Check for inline styles breaking design system
grep -r "style={{" src/components/

# Check for non-brand colors
grep -r "#[0-9a-fA-F]\{6\}" src/ | grep -v "ph-"

# Check for consumer app language
grep -ri "awesome\|sweet\|amazing" src/components/

# Check accessibility issues
npm run test:a11y

# Check CLI export functionality
grep -r "cliCommand\|exportCommand" src/components/
```

---

## 🔧 **HEALTHCARE UX REFACTORING RULES**

### **When Fixing UX Violations**:
1. **Fix accessibility first** (Healthcare professionals depend on assistive tech)
2. **Maintain healthcare context** (Don't genericize medical workflows)
3. **Preserve CLI harmonization** (Don't break CLI-GUI feature parity)
4. **Test with healthcare scenarios** (Not just technical functionality)

### **Design System Evolution Strategy**:
1. **Extend components, don't replace** (Maintain existing healthcare workflows)
2. **Add variants for new use cases** (Emergency, routine, administrative)
3. **Document healthcare-specific patterns** (Why certain patterns work in clinical settings)
4. **Version design tokens carefully** (Breaking changes affect all components)

---

## 🎯 **GUI QUALITY GATES**

### **Healthcare UX Review Checklist**
- [ ] **Professional Language**: Medical terminology used correctly
- [ ] **Workflow Efficiency**: Reduces clicks for common healthcare tasks
- [ ] **Error Prevention**: Prevents common clinical integration mistakes
- [ ] **Context Awareness**: Adapts to different healthcare environments
- [ ] **Brand Consistency**: Maintains Pidgeon Health visual identity
- [ ] **Accessibility**: Works with assistive technology
- [ ] **Performance**: Meets clinical workstation speed requirements

### **GUI Health Score Validation**
After any significant GUI change, validate:
- [ ] **Design system violations**: Should remain 0
- [ ] **Accessibility violations**: Should remain 0  
- [ ] **Performance regressions**: Should maintain <200ms targets
- [ ] **CLI-GUI harmony**: All GUI operations exportable
- [ ] **Healthcare UX patterns**: Consistent with professional workflows

---

## 📖 **GUI DECISION FRAMEWORK**

### **For Every GUI Change, Ask**:
1. **Healthcare UX**: Does this improve healthcare professional workflows?
2. **Accessibility**: Can all users access this feature effectively?
3. **Brand Consistency**: Does this maintain Pidgeon Health identity?
4. **CLI Harmony**: Can users replicate this action via CLI?
5. **Performance**: Does this maintain clinical workstation responsiveness?

### **Required GUI Documentation**:
- **GUI_DESIGN.md**: For UX pattern changes or new interaction models
- **LEDGER.md**: For significant component architecture decisions
- **Storybook**: For all new components and variants

---

## 🚀 **GUI SUCCESS CRITERIA**

### **Component is Ready When**:
- [ ] Follows Pidgeon Health design system consistently
- [ ] Meets WCAG 2.1 AA accessibility requirements
- [ ] Supports healthcare professional workflows efficiently
- [ ] Exports equivalent CLI operations
- [ ] Performs within clinical workstation targets
- [ ] Documented in Storybook with healthcare scenarios

### **GUI Feature is Ready When**:
- [ ] All healthcare user scenarios tested
- [ ] Free/Professional tier boundaries clear
- [ ] CLI-GUI feature parity maintained
- [ ] AI integration follows PHI safety rules
- [ ] Performance meets healthcare environment needs
- [ ] Component patterns reusable across features

---

**Remember**: Every GUI component serves healthcare professionals performing critical integration work. Visual design must support accuracy, efficiency, and confidence in high-stakes healthcare environments.

**The Goal**: GUI components so intuitive and robust that healthcare IT teams can confidently test integration scenarios without fear of errors or inefficiency.