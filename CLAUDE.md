# Pidgeon Healthcare Interoperability Platform - Agent Context

## 🎯 **Strategic Direction: Safe Healthcare Testing Without PHI**

**North Star**: "Realistic scenario testing without the legal/compliance nightmare of using real patient data"  
**Status**: Foundation complete (100/100 health) - Ready for P0 MVP development  
**Mission**: Transform healthcare integration testing through synthetic data, de-identification, and AI-assisted debugging  
**Approach**: CLI-first with complementary GUI, plugin architecture, Free/Pro/Enterprise tiers

---

## 📋 **Essential Context Documents**

### **🚨 CRITICAL - Must Read for Every Session**
1. **[`docs/roadmap/PIDGEON_ROADMAP.md`](docs/roadmap/PIDGEON_ROADMAP.md)** - **MASTER ROADMAP: Complete P0-P2 plan with all details**
2. **[`docs/roadmap/features/NORTHSTAR.md`](docs/roadmap/features/NORTHSTAR.md)** - **PRODUCT VISION: Core value proposition and user focus**
3. **[`docs/roadmap/CLI_REFERENCE.md`](docs/roadmap/CLI_REFERENCE.md)** - **CLI GUIDE: Complete command structure and examples**
4. **[`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)** - **CURRENT STATUS: Foundation complete, P0 feature development ready**
5. **[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)** - **ARCHITECTURAL SPEC: Four-domain architecture + P0 features**
6. **[`docs/roadmap/features/cli_gui_harmonization.md`](docs/roadmap/features/cli_gui_harmonization.md)** - **PLATFORM STRATEGY: One engine, two frontends approach**
7. **[`docs/LEDGER.md`](docs/LEDGER.md)** - Recent development decisions and rollback procedures

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

## 💰 **Updated Business Model - CLI + GUI Harmony**

### **🆓 CLI CORE (Always Free)**
- **All 6 P0 features** with procedural generation mode
- **Full HL7/FHIR/NCPDP** standards engines  
- **De-identification** engine (on-premises)
- **Vendor pattern detection** with basic profiles
- **25 medications, 50 names** - covers 70% of test cases

### **🔒 PROFESSIONAL TIER ($29/month)**
- **Workflow Wizard**: Guided multi-step scenario creation
- **Diff + AI Triage**: Field-level comparison with AI suggestions  
- **Local AI Models**: Enhanced realistic data generation
- **Enhanced Datasets**: More medications, edge cases
- **HTML Reports**: Validation and diff reporting

### **🆕 ENTERPRISE TIER ($199/seat)**
- **Team Workspaces**: Projects, roles, SSO integration
- **Audit Trails**: Complete governance and compliance
- **Private AI Models**: Custom healthcare model hosting
- **Advanced Profiles**: Full IG validation matrix
- **Unlimited Usage**: No generation or validation limits

---

## 🎯 **P0 MVP DEVELOPMENT - READY FOR FEATURE WORK**

> **STATUS**: Foundation complete (100/100 health) - Ready for P0 feature development

### **🧬 P0 Embryonic Development Sequence (8 weeks)**
**Critical Insight**: Like biological development, the sequence of feature development creates compound intelligence rather than scattered capabilities.

**Optimal Development Order**:
1. **Weeks 1-2: Message Generation Engine** 🆓 **[Foundational Heartbeat]** - Creates "blood supply" (test data) that feeds all other systems
2. **Week 3: On-Premises De-identification** 🆓 **[Major Differentiation]** - Unlocks real data segment, proves complexity handling
3. **Week 4: Message Validation Engine** 🆓 **[Quality Control]** - Works on synthetic + de-identified data, creates feedback loops
4. **Week 5: Vendor Pattern Detection** 🆓 **[Network Effects]** - Benefits from ALL previous data creation, builds proprietary intelligence
5. **Week 6: Workflow Wizard** 🔒 **[Pro - Natural Revenue Conversion]** - Uses compound intelligence, natural upgrade trigger
6. **Weeks 7-8: Diff + AI Triage** 🔒 **[Pro - Advanced Troubleshooting]** - Ultimate compound feature with maximum intelligence stack

### **CLI-First Development Approach**
- **Primary Interface**: CLI commands for all core functionality
- **Feature Gating**: Clear Free/Pro/Enterprise markers in help text
- **GUI Complement**: One engine, two frontends strategy
- **Export/Import Symmetry**: CLI artifacts viewable in GUI, GUI operations export to CLI
- **Growth Path**: Free CLI → Pro features → Enterprise collaboration

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

### **📌 TODO/FIXME Pattern - Professional Placeholder Implementation**
**MANDATORY**: When encountering implementation challenges, use TODO/FIXME markers instead of hacks:

#### **When to Use TODO/FIXME Instead of Hacks:**
- **Missing dependencies**: Use placeholder implementations with TODO markers
- **Complex logic not yet designed**: Return simple defaults with FIXME notes
- **Cross-cutting concerns**: Mark with TODO rather than violating architecture
- **Performance optimizations needed**: Simple implementation with TODO for optimization
- **External integrations pending**: Mock responses with TODO for real implementation

#### **TODO/FIXME Best Practices:**
```csharp
// ✅ GOOD: Clear TODO with simple placeholder
public async Task<Result<PhiDetectionResult>> DetectPhiAsync(string message)
{
    await Task.Yield();
    
    // TODO: Implement actual PHI detection using pattern matching and NLP
    // TODO: Integrate with infrastructure PHI detection plugins
    var result = new PhiDetectionResult
    {
        DetectedPhi = Array.Empty<PhiDetectionItem>(),
        OverallConfidence = 0.95,
        Statistics = new PhiDetectionStatistics
        {
            TotalFieldsScanned = 0, // TODO: Track actual fields scanned
            PotentialPhiFound = 0,   // TODO: Count detected PHI items
            PhiByType = new Dictionary<IdentifierType, int>(),
            ScanTime = TimeSpan.Zero
        }
    };
    
    return Result<PhiDetectionResult>.Success(result);
}

// ❌ BAD: Hacky workaround that violates architecture
public async Task<Result<PhiDetectionResult>> DetectPhiAsync(string message)
{
    // HACK: Just regex for SSN pattern, ignore everything else
    var ssnPattern = @"\d{3}-\d{2}-\d{4}";
    var matches = Regex.Matches(message, ssnPattern);
    
    // Faking statistics to make it compile
    var fakeStats = new PhiDetectionStatistics
    {
        TotalFieldsScanned = message.Length / 50, // Random calculation
        PotentialPhiFound = matches.Count * 3,     // Multiply by arbitrary number
        // ... more hacks
    };
}
```

#### **TODO/FIXME Documentation Requirements:**
1. **Be specific** about what needs implementation
2. **Reference tickets/issues** if they exist
3. **Indicate priority** (Critical, High, Medium, Low)
4. **Describe the proper solution** briefly
5. **Never leave vague TODOs** like "// TODO: Fix this"

### **When Creating New Features:**
1. **Start with domain model** - What's the healthcare concept?
2. **Create standard adapters** - How does each standard serialize it?
3. **Register plugins** - Add to DI container
4. **Write behavior tests first** - Healthcare scenarios, not code coverage
5. **Use Result<T>** - No exceptions for business logic control flow
6. **Use TODO/FIXME markers** - Never hack around problems or violate architecture

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

## 📝 **P0 Feature Development Guidelines**

### **CLI Command Structure** 
*Follow [`docs/roadmap/features/cli_baseline.md`](docs/roadmap/features/cli_baseline.md) exactly*

```bash
# Core Commands (Free)
pidgeon generate message --type ADT^A01 --count 10
pidgeon validate --file labs.hl7 --mode compatibility  
pidgeon deident --in ./samples --out ./synthetic --date-shift 30d
pidgeon config analyze --samples ./inbox --save epic_er.json

# Pro Commands (Gated)
pidgeon workflow wizard  # Interactive guided scenarios
pidgeon diff --left ./envA --right ./envB --report diff.html
```

### **Feature Implementation Priorities**
1. **Week 1-2**: Core engines (generate, validate) with CLI interface
2. **Week 3**: De-identification engine (new core feature)
3. **Week 4**: Vendor pattern detection and config management
4. **Week 5**: Workflow Wizard (Pro feature)
5. **Week 6**: Diff + AI Triage (Pro feature)

### **CLI-GUI Harmonization Rules**
- **One Engine**: All business logic in Core services, CLI/GUI are frontends
- **Export Symmetry**: GUI operations export CLI commands
- **Import Symmetry**: CLI artifacts (reports, configs) viewable in GUI
- **Consistent Mental Model**: `pidgeon generate` = GUI "Generate Messages" panel
- **Feature Gating**: Pro/Enterprise markers visible in CLI help

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