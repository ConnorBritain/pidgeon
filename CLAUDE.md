# Pidgeon Healthcare Interoperability Platform - Agent Context

## 🎯 **Strategic Direction: Core+ Architecture & Multi-Standard Excellence**

**Status**: Building clean architecture from day one with Core+ business model  
**Mission**: AI-augmented universal healthcare standards platform (HL7, FHIR, NCPDP)
**Approach**: Domain-driven design with plugin architecture for scale without refactoring

---

## 📋 **Essential Context Documents**

### **🚨 CRITICAL - Must Read for Every Session**
1. **[`docs/pidgeon_feature_plan.md`](docs/pidgeon_feature_plan.md)** - **MASTER ROADMAP: Definitive stage-gate plan with success criteria**
2. **[`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)** - **CURRENT PLAN: Foundation repair through P0 MVP delivery**
3. **[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)** - **ARCHITECTURAL SPEC: Four-domain architecture and implementation status**
4. **[`docs/arch_reviews/PIDGEON_AR_FINAL.md`](docs/arch_reviews/PIDGEON_AR_FINAL.md)** - **FOUNDATION CONTEXT: Critical for executing repairs correctly**
5. **[`docs/LEDGER.md`](docs/LEDGER.md)** - Complete development decision log and rollback procedures
6. **[`docs/roadmap/INIT.md`](docs/roadmap/INIT.md)** - Sacred architectural principles and constraints

### **Agent Steering Documentation**
7. **[`docs/agent_steering/product.md`](docs/agent_steering/product.md)** - Product vision and Core+ strategy
8. **[`docs/agent_steering/structure.md`](docs/agent_steering/structure.md)** - Clean architecture principles
9. **[`docs/agent_steering/tech.md`](docs/agent_steering/tech.md)** - Technology stack and patterns
10. **[`docs/agent_steering/agent-reflection.md`](docs/agent_steering/agent-reflection.md)** - Decision framework
11. **[`docs/agent_steering/error-resolution-methodology.md`](docs/agent_steering/error-resolution-methodology.md)** - STOP-THINK-ACT error handling framework
12. **[`docs/agent_steering/test-philosophy.md`](docs/agent_steering/test-philosophy.md)** - Testing strategy and behavior-driven approach

### **User-Centered Development**
13. **[`docs/user_stories/BACKLOG.md`](docs/user_stories/BACKLOG.md)** - Prioritized user stories and MVP validation
14. **[`docs/user_stories/MVP_VALIDATION.md`](docs/user_stories/MVP_VALIDATION.md)** - Business model alignment validation
15. **[`docs/user_stories/developer/core_stories.md`](docs/user_stories/developer/core_stories.md)** - Developer workflows and pain points
16. **[`docs/user_stories/consultant/core_stories.md`](docs/user_stories/consultant/core_stories.md)** - Consultant engagement scenarios
17. **[`docs/user_stories/informaticist/core_stories.md`](docs/user_stories/informaticist/core_stories.md)** - Healthcare IT operational needs
18. **[`docs/user_stories/administrator/core_stories.md`](docs/user_stories/administrator/core_stories.md)** - Strategic and compliance requirements

### **Business Strategy Documents**
19. **[`docs/founding_plan/business_model.md`](docs/founding_plan/business_model.md)** - Crystallized subscription-first revenue model
20. **[`docs/founding_plan/generation_considerations.md`](docs/founding_plan/generation_considerations.md)** - Generation service architecture & AI integration
21. **[`docs/founding_plan/core_plus_strategy.md`](docs/founding_plan/core_plus_strategy.md)** - Original business model details

---

## 🚨 **MANDATORY ERROR RESPONSE PROTOCOL**

**TRIGGER**: When encountering ANY compilation errors, build failures, or runtime exceptions:

### **STOP-THINK-ACT Requirement**
1. **STOP**: Do not immediately attempt fixes
2. **Reference methodology**: Always consult `docs/agent_steering/error-resolution-methodology.md`
3. **Document analysis**: Explain your STOP-THINK-ACT process before implementing any solution
4. **Get alignment**: For architectural decisions affecting base classes or abstractions, confirm approach before coding

### **Error Categories Requiring Methodology**
- **Compilation errors** (missing methods, type mismatches, interface violations)
- **Build failures** (project reference issues, dependency problems)
- **Runtime exceptions** (null reference, invalid operations, casting errors)
- **Architectural conflicts** (abstract method implementations, inheritance issues)

### **Trigger Phrases for User**
If the user says any of these phrases, immediately follow the error methodology:
- "compilation error"
- "build failed"  
- "doesn't implement"
- "method not found"
- "follow error methodology"

### **Required Response Format**
```
🚨 ERROR METHODOLOGY CHECKPOINT

STOP: [Brief description of error encountered]

THINK - Root Cause Analysis:
- Architecture Issue: [What design pattern is involved?]
- Dependency Impact: [What else might be affected?]  
- Cascade Effect: [Are there related errors?]

ACT - Systematic Fix Plan:
1. [Specific step 1]
2. [Specific step 2]
3. [Verification step]
```

**Enforcement**: Any error-fixing without following this protocol violates our senior-level development standards.

---

## 🏗️ **Core Architectural Principles**

### **1. Domain-Driven Design First**
```csharp
// ✅ Standards-agnostic domain models
namespace Pidgeon.Core.Domain {
    public record Prescription(Patient Patient, Medication Drug, Provider Prescriber);
}

// ✅ Standards as adapters
namespace Pidgeon.Core.Standards {
    public interface IStandardAdapter<T> {
        string Generate(T domain);
        T Parse(string message);
    }
}
```

### **2. Plugin Architecture (Never Break Existing Code)**
```csharp
// ✅ New standards are plugins
services.AddPlugin<HL7v23Plugin>();
services.AddPlugin<FHIRPlugin>();     // Added without touching HL7
services.AddPlugin<NCPDPPlugin>();    // Added without touching FHIR
```

### **3. Dependency Injection Throughout**
```csharp
// ❌ NEVER static classes
public static class Utils { }

// ✅ ALWAYS injectable services
public interface IValidationService { }
public class ValidationService : IValidationService { }
```

### **4. Result<T> Pattern (No Exceptions for Control Flow)**
```csharp
// ✅ Explicit error handling
public Result<Message> ProcessMessage(string input)
    => Parse(input)
        .Bind(Validate)
        .Bind(Transform);
```

---

## 🎯 **Core+ Business Model** 
*See [`docs/founding_plan/business_model.md`](docs/founding_plan/business_model.md) for complete details*

### **🆓 FREE CORE (MPL 2.0)**
- **Algorithmic Generation**: 25 medications, 50 names (70% common case coverage)
- **HL7 v2.3**: Complete engine with all message types  
- **FHIR R4**: Basic resources (Patient, Observation, MedicationRequest)
- **NCPDP SCRIPT**: Basic messages (NewRx, Refill, Cancel)
- **CLI Interface**: Full command-line functionality
- **Deterministic Testing**: Seeds for reproducible generation

### **💼 SUBSCRIPTION TIERS (Primary Revenue)**
- **Professional ($29/month)**: Live datasets, AI enhancement (BYOK), cloud API
- **Professional + Vendor Templates ($49/month)**: Epic/Cerner specific formatting  
- **Team ($99/month)**: Collaboration features, team management, priority support
- **Enterprise ($199/month per seat)**: Unlimited AI, custom datasets, SSO

### **🎁 ONE-TIME RESCUE OFFER ($299 - Downsell Only)**  
- **Desktop GUI**: Full-featured application
- **"B-Level" Datasets**: 150+ medications (static, 6+ months old)
- **Batch Processing**: CSV export, bulk generation
- **No Subscriptions**: Offline-focused, community support only

---

## 🎯 **MVP-FOCUSED DEVELOPMENT (PRODUCT-001)**

> **CRITICAL**: All development must prioritize validated user stories. Reference `docs/user_stories/BACKLOG.md` for P0 features.

### **P0 MVP Features (Build These First)**
1. **Message Generation** - All user types need realistic test messages daily
2. **Message Validation** - Critical for quality assurance and debugging  
3. **Vendor Pattern Detection** - Our unique differentiator from generic tools
4. **Format Error Debugging** - Reduces troubleshooting time by 75%
5. **Synthetic Test Data** - Safe testing without PHI exposure

### **User-First Development Principle**
- **Check User Stories First**: Every feature must map to validated user stories
- **80/20 Rule**: Focus on P0 features that provide 80% of user value
- **Business Model Alignment**: Verify features align with Core/Professional/Enterprise tiers
- **User Workflow Support**: Ensure features fit real healthcare workflows

---

## 🚧 **Development Guidelines**

> **MANDATORY PROCESS**: Every significant change must follow the workflow in `docs/roadmap/INIT.md` and be logged in `docs/LEDGER.md`

### **Development Workflow:**
1. **Check INIT.md** - Verify change aligns with sacred architectural principles
2. **Document decision** - Add LEDGER entry if architectural significance
3. **Write tests first** - Follow behavior-driven approach from `test-philosophy.md`
4. **Implement solution** - Use domain-first, plugin-based patterns
5. **Validate with agent-reflection.md** - Ensure consistency
6. **Update documentation** - LEDGER, roadmap, or other docs as needed

### **⚠️ When Encountering Errors - STOP-THINK-ACT**
**CRITICAL**: Follow [`docs/agent_steering/error-resolution-methodology.md`](docs/agent_steering/error-resolution-methodology.md)
- **STOP**: Don't immediately fix - understand the full error context
- **THINK**: Perform root cause analysis, consider architectural implications
- **ACT**: Make minimal, principled changes that address the root cause
- **AVOID**: Trial-and-error fixes, compound changes, band-aid solutions

### **When Creating New Features:**
1. **Start with domain model** - What's the healthcare concept?
2. **Create standard adapters** - How does each standard serialize it?
3. **Register plugins** - Add to DI container
4. **Write behavior tests first** - Healthcare scenarios, not code coverage
5. **Use Result<T>** - No exceptions for business logic control flow

### **When Adding New Standards:**
1. Create new namespace: `Pidgeon.Core.Standards.{Standard}`
2. Implement `IStandardAdapter<T>` interfaces
3. Add plugin registration
4. Keep existing code unchanged

### **Before Any Commit:**
- [ ] **Follows sacred principles** in `INIT.md` (or exception documented in LEDGER)
- [ ] **Domain logic has zero infrastructure dependencies**
- [ ] **New features are plugins, not core changes**
- [ ] **All services use dependency injection** (except allowed static utilities)
- [ ] **Error handling uses Result<T>** for business logic
- [ ] **Core+ boundary respected** (free vs paid features)
- [ ] **Tests focus on behavior** not implementation details
- [ ] **Agent reflection completed** for significant changes

### **Generation Service Development:**
*See [`docs/founding_plan/generation_considerations.md`](docs/founding_plan/generation_considerations.md) for architecture details*
- [ ] **Two-Tier Architecture**: Algorithmic (free) + AI enhancement (subscription)
- [ ] **BYOK for Professional**: User provides OpenAI/Anthropic keys
- [ ] **Algorithmic Fallback**: Always works without AI dependencies
- [ ] **Usage Tracking**: Token counting and cost allocation for Enterprise
- [ ] **Dataset API**: Live specialty datasets for subscription tiers

---

## 🎯 **Success Metrics**

### **Technical Quality**
- Clean architecture maintained (zero infrastructure in domain)
- Plugin system enables rapid standard additions
- Performance targets: <50ms message transformation
- Test coverage: >90% on core functionality

### **Business Impact**
- Free adoption drives awareness and feedback
- Professional conversion validates GUI/AI value
- Enterprise expansion proves collaboration value
- Cross-standard usage demonstrates platform power

---

## 🚨 **Critical Reminders**

### **Architecture Red Lines - NEVER:**
- ❌ Static classes or methods (kills testability)
- ❌ Domain models depending on infrastructure
- ❌ Exceptions for control flow (use Result<T>)
- ❌ Core changes for new message types (use plugins)
- ❌ Breaking changes to public APIs

### **Business Model Red Lines - NEVER:**
- ❌ Put paid features in open source core
- ❌ Unlimited AI usage without cost control
- ❌ Enterprise features in Professional tier
- ❌ Break the free tier (it drives adoption)

### **Always Use Agent Reflection**
After any significant change, reference `agent-reflection.md` and explain:
1. **Architecture adherence** - Follows sacred principles in INIT.md?
2. **Standards compliance** - HL7/FHIR/NCPDP specifications met?
3. **Core+ strategy alignment** - Correct tier placement?
4. **AI cost management** - BYOK/usage limits in place?
5. **Testing strategy** - Behavior-driven tests covering healthcare scenarios?
6. **LEDGER documentation** - Major decisions properly logged?

---

## 🚨 **CRITICAL PLUGIN ARCHITECTURE RULE - NO STANDARD-SPECIFIC HARDCODING**

### **MANDATORY: Core Services Must Be Standard-Agnostic**

**NEVER put standard-specific logic in core services**. This violates our sacred plugin architecture principle and single responsibility principle.

#### **❌ FORBIDDEN - Hardcoded Standard Logic in Core**
```csharp
// ❌ NEVER DO THIS in core services
public class ConfidenceCalculator {
    if (fieldPatterns.MessageType?.StartsWith("ADT") == true) // HL7-specific!
    {
        var expectedSegments = new[] { "MSH", "EVN", "PID", "PV1" }; // HL7-specific!
    }
}
```

#### **✅ CORRECT - Standard Logic Lives in Plugins**
```csharp
// ✅ Core service delegates to plugins
public interface IStandardConfidencePlugin {
    bool CanHandle(string standard);
    Task<double> CalculateCoverageAsync(FieldPatterns patterns);
}

// ✅ HL7-specific logic in HL7 plugin only
public class HL7ConfidencePlugin : IStandardConfidencePlugin {
    public async Task<double> CalculateCoverageAsync(FieldPatterns patterns) {
        // HL7-specific logic HERE, not in core
        if (patterns.MessageType?.StartsWith("ADT") == true) { ... }
    }
}
```

### **Architecture Enforcement Checklist**
Before creating or modifying ANY file in `Pidgeon.Core/Services/`:
- [ ] **Zero standard-specific strings** (no "MSH", "ADT", "PID", etc.)
- [ ] **Zero standard-specific logic** (no segment expectations, field positions)
- [ ] **Plugin delegation for standard operations** (use plugin registry)
- [ ] **Standard-agnostic interfaces** (work with any healthcare standard)

### **Updated Domain Architecture Rules**
**CRITICAL UPDATE**: We now use a **Four-Domain Architecture** (see `docs/arch_planning/pidgeon_domain_model.md`):

- `Pidgeon.Core/Domain/Clinical/` → Healthcare business concepts (Patient, Prescription)
- `Pidgeon.Core/Domain/Messaging/` → Wire format structures (HL7_ORM_Message, FHIR_Bundle)  
- `Pidgeon.Core/Domain/Configuration/` → Vendor patterns (VendorConfiguration, FieldPattern)
- `Pidgeon.Core/Domain/Transformation/` → Mapping rules (MappingRule, TransformationSet)
- `Pidgeon.Core/Adapters/` → Interfaces between domains (IClinicalToMessagingAdapter)

**NEW RULE**: Each service should depend on exactly ONE domain. Cross-domain operations happen through Domain Adapter interfaces.

### **Documentation Synchronization Status**
All key documents updated to reflect Four-Domain Architecture (August 27, 2025):
- ✅ `WAVEZERO.md`: Updated from dual-domain to four-domain decision
- ✅ `LEDGER.md`: Added ARCH-019 entry documenting final architectural decision
- ✅ `082725.md`: Updated to reference new architecture (document superseded)
- ✅ `INIT.md`: Updated sacred principles to show four domains
- ✅ `pidgeon_domain_model.md`: Complete four-domain specification created

---

## 📝 **Code Marker Standards for Technical Debt Tracking**

### **MANDATORY: Use Markers for Identified Issues**
When making sweeping changes, architectural migrations, or identifying problems, **ALWAYS add markers** for future tracking:

```csharp
// TODO: Description of what needs to be implemented/fixed
// FIXME: Description of broken/problematic code that needs repair  
// HACK: Description of temporary workaround that needs proper solution
// BUG: Description of defect that needs fixing
```

### **When to Add Markers**:
1. **During major refactoring** - Mark incomplete implementations
2. **Interface changes** - Mark methods that need updating downstream
3. **Domain migrations** - Mark cross-domain dependencies to resolve
4. **Service relocations** - Mark services in wrong architectural locations
5. **Plugin modifications** - Mark plugin responsibilities that should move to adapters
6. **Pattern violations** - Mark code that violates sacred architectural principles

### **Marker Examples**:
```csharp
public class ExampleService : IExampleService {
    // TODO: Implement proper Result<T> pattern instead of throwing exceptions
    public void ProcessData(string input) {
        throw new NotImplementedException();
    }
    
    // FIXME: This creates dependency on multiple domains - violates single-domain rule
    private readonly IConfigurationService _config;
    private readonly ITransformationService _transform;
    
    // HACK: Hard-coded HL7 logic in core service - should be in plugin
    if (messageType.StartsWith("ADT")) { /* ... */ }
    
    // BUG: Null reference possible if fieldPatterns.SegmentPatterns is empty
    return fieldPatterns.SegmentPatterns.First().Value;
}
```

### **Tracking and Resolution**:
- Use `grep -r "TODO:\|FIXME:\|HACK:\|BUG:" .` to find all markers
- Include marker count in technical debt assessments  
- Remove markers only when properly resolved, not just commented out
- Include marker context in architectural decision documentation

This ensures **comprehensive tracking** during major changes and prevents issues from being forgotten during fast development cycles.

**No competing architectural ideologies remain in documentation.**

---

## 🎯 **Professional Code Standards & Agent Behavior**

### **Clean, Senior-Level Code Practices**
**CRITICAL**: Write professional, maintainable code without meta-commentary or development artifacts.

#### **❌ AVOID: Development Artifacts in Code**
- **Meta-commentary**: Comments like "ELIMINATES TECHNICAL DEBT", "AVOIDS ARCHITECTURAL ISSUES"
- **Justification comments**: Explaining WHY changes were made instead of WHAT the code does
- **Conversation references**: Mentions of "Claude Code", "AI-generated", architectural discussions
- **Code graveyards**: Commenting out old code with "LEGACY" or "OLD APPROACH" markers

#### **✅ DO: Professional Documentation**
- **Clear, concise summaries**: What the code does and how to use it
- **Business context**: Healthcare-specific requirements and constraints  
- **Technical specifications**: Parameters, return values, error conditions
- **Usage examples**: For complex interfaces and patterns

#### **Example - Bad vs Good Comments**:
```csharp
// ❌ BAD - Meta-commentary and justification
/// <summary>
/// ELIMINATES TECHNICAL DEBT: Uses adapter pattern to avoid Dictionary<int,FieldFrequency> 
/// to Dictionary<string,FieldFrequency> conversion utilities that were causing compilation 
/// errors. This follows our STOP-THINK-ACT methodology and four-domain architecture.
/// </summary>

// ✅ GOOD - Clear, professional documentation  
/// <summary>
/// Analyzes HL7 messages to extract field population patterns.
/// Converts field positions to configuration domain paths (e.g., "PID.5").
/// </summary>
/// <param name="messages">HL7 messages to analyze</param>
/// <returns>Field patterns with population statistics</returns>
```

#### **File Management Standards**
- **Edit existing files directly** - Don't create "_Refactored.cs" or "_New.cs" variants
- **Delete dead code decisively** - Don't comment out with explanatory notes
- **Use git for rollbacks** - Version control handles safety, not code comments
- **Commit architectural changes** - Be decisive about implementation approach

#### **Decision-Making Approach**
- **Senior-level decisions**: Make clean, decisive changes without hedging
- **Architectural commitment**: Fully implement chosen patterns without fallback options  
- **Clean implementation**: Remove old approaches when implementing new ones
- **Professional confidence**: Write code that demonstrates technical competence

### **Code Review Standards**
All code should pass the "senior developer review" test:
- ✅ **Clear purpose**: Any developer can understand intent from documentation
- ✅ **Clean structure**: No development artifacts or meta-commentary
- ✅ **Decisive implementation**: No hedged approaches or "just in case" code
- ✅ **Professional quality**: Represents our platform's technical excellence

---

## 💡 **Remember: We're Building for Scale**

**Not building**: An HL7 generator that happens to support FHIR
**Actually building**: A healthcare data platform with multiple standard serializations

**Not building**: A better Mirth Connect
**Actually building**: The next-generation AI-augmented interoperability platform

Every architectural decision should support going from 1 user to 10,000 users without major refactoring. Every feature should either drive adoption (Core) or revenue (Professional/Enterprise).

---

**The Goal**: Become the dominant platform for healthcare standards development and testing by combining the best of open source adoption with commercial success.

*Build for the platform you'll need in 5 years, not the MVP you need next month.*