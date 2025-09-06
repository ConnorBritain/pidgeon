# Pidgeon Development Rules
**Purpose**: Essential guidelines for AI agents during architectural rehabilitation and feature development  
**Enforcement**: These rules prevent rework and ensure consistency with the established codebase  
**Review Required**: Every significant change must validate against these rules  

---

## 🚨 **MANDATORY ERROR RESPONSE**

When encountering ANY compilation errors, build failures, or runtime exceptions:

### **STOP-THINK-ACT Protocol**
1. **STOP**: Do not immediately attempt fixes
2. **THINK**: Perform root cause analysis (see `docs/agent_steering/error-resolution-methodology.md`)
3. **ACT**: Make minimal, principled changes that address the root cause

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

**Violation**: Any error fixing without this protocol violates senior-level development standards.

---

## 🏗️ **SACRED ARCHITECTURAL PRINCIPLES** 
*From `docs/roadmap/INIT.md` - IMMUTABLE for first 90 days*

### **1. Four-Domain Architecture (Never Mix)**
```csharp
// ✅ CORRECT: Each domain has single responsibility
Pidgeon.Core.Domain.Clinical/     → Healthcare concepts (Patient, Prescription)
Pidgeon.Core.Domain.Messaging/    → Wire formats (HL7_ORM_Message, FHIR_Bundle)  
Pidgeon.Core.Domain.Configuration/→ Vendor patterns (VendorConfiguration)
Pidgeon.Core.Domain.Transformation/→ Mapping rules (MappingRule)

// ❌ FORBIDDEN: Cross-domain imports
using Pidgeon.Core.Domain.Clinical;  // In Messaging domain files
using Pidgeon.Core.Domain.Messaging; // In Clinical domain files
```

### **2. Plugin Architecture (Never Break Existing Code)**
```csharp
// ✅ CORRECT: New standards are plugins
services.AddStandardPlugin<HL7v23Plugin>();
services.AddStandardPlugin<FHIRPlugin>();     // Added without touching HL7
services.AddStandardPlugin<NCPDPPlugin>();    // Added without touching FHIR

// ❌ FORBIDDEN: Hardcoded standard logic in core services
if (messageType.StartsWith("ADT")) { ... }  // NEVER in core services
```

### **3. Dependency Injection Throughout**
```csharp
// ✅ CORRECT: Injectable services
public class MessageService {
    public MessageService(IValidationService validator) { }
}

// ❌ FORBIDDEN: Static classes for business logic
public static class MessageUtils { }

// ✅ EXCEPTION ALLOWED: Constants and pure functions only
public static class HL7Constants {
    public const string FieldSeparator = "|";
    public static bool IsValidDate(string date) => date?.Length == 8; // No state
}
```

### **4. Result<T> Pattern (No Exceptions for Control Flow)**
```csharp
// ✅ CORRECT: Explicit error handling
public Result<Message> ProcessMessage(string input)
    => Parse(input)
        .Bind(Validate)
        .Bind(Transform);

// ❌ FORBIDDEN: Exceptions for business logic
if (!IsValid(input)) throw new ArgumentException("Invalid");

// ✅ EXCEPTION ALLOWED: Framework violations only
if (input == null) throw new ArgumentNullException(nameof(input));
```

---

## 💼 **PROFESSIONAL CODE STANDARDS**

### **❌ NEVER: Development Artifacts in Code**
- **Meta-commentary**: Comments like "ELIMINATES TECHNICAL DEBT", "AVOIDS ARCHITECTURAL ISSUES"
- **Justification comments**: Explaining WHY changes were made instead of WHAT the code does
- **Conversation references**: Mentions of "Claude Code", "AI-generated", architectural discussions
- **Code graveyards**: Commenting out old code with "LEGACY" or "OLD APPROACH" markers

### **✅ ALWAYS: Professional Documentation**
```csharp
// ❌ BAD - Meta-commentary and justification
/// <summary>
/// ELIMINATES TECHNICAL DEBT: Uses adapter pattern to avoid conversion issues
/// following our STOP-THINK-ACT methodology and four-domain architecture.
/// </summary>

// ✅ GOOD - Clear, professional documentation  
/// <summary>
/// Analyzes HL7 messages to extract field population patterns.
/// Converts field positions to configuration domain paths (e.g., "PID.5").
/// </summary>
/// <param name="messages">HL7 messages to analyze</param>
/// <returns>Field patterns with population statistics</returns>
```

### **File Management Standards**
- **Edit existing files directly** - No "_Refactored.cs" or "_New.cs" variants
- **Delete dead code decisively** - Don't comment out with explanatory notes
- **Use git for rollbacks** - Version control handles safety, not code comments
- **Commit architectural changes** - Be decisive about implementation approach

---

## 🚧 **TECHNICAL DEBT MARKERS**

### **MANDATORY: Use Markers Instead of Hacks**
```csharp
// TODO: Description of what needs to be implemented/fixed
// FIXME: Description of broken/problematic code that needs repair  
// HACK: Description of temporary workaround that needs proper solution
// BUG: Description of defect that needs fixing
```

### **When to Add Markers**:
1. **Major refactoring** - Mark incomplete implementations
2. **Interface changes** - Mark methods needing downstream updates
3. **Domain migrations** - Mark cross-domain dependencies to resolve
4. **Pattern violations** - Mark code violating sacred architectural principles
5. **Missing implementations** - Use placeholders with TODO instead of hacks

### **TODO/FIXME Over Hacks Principle**:
```csharp
// ✅ CORRECT: Professional placeholder with clear TODO
public Result<DeIdentificationContext> CreateContext(DeIdentificationOptions options)
{
    var context = new DeIdentificationContext
    {
        Salt = options.Salt ?? Guid.NewGuid().ToString(),
        DateShift = options.DateShift ?? TimeSpan.Zero
        // TODO: Initialize ID mappings from persistence if available
        // TODO: Load custom field mappings from configuration
    };
    return Result<DeIdentificationContext>.Success(context);
}

// ❌ FORBIDDEN: Hacky workaround that violates principles
public Result<DeIdentificationContext> CreateContext(DeIdentificationOptions options)
{
    // HACK: Just hardcode some values to make it compile
    dynamic context = new ExpandoObject();
    context.Salt = "hardcoded-salt";
    context.DateShift = 30; // Random number of days
    return Result<DeIdentificationContext>.Success((DeIdentificationContext)context);
}
```

### **Tracking**: Use `grep -r "TODO:\|FIXME:\|HACK:\|BUG:" .` to find all markers

---

## 🔍 **CORE SERVICE RULES**

### **❌ CRITICAL VIOLATION: Standard-Specific Logic in Core**
```csharp
// ❌ NEVER DO THIS in core services
public class ConfidenceCalculator {
    if (fieldPatterns.MessageType?.StartsWith("ADT") == true) // HL7-specific!
    {
        var expectedSegments = new[] { "MSH", "EVN", "PID", "PV1" }; // HL7-specific!
    }
}
```

### **✅ CORRECT: Standard-Agnostic Core with Plugin Delegation**
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

### **Core Service Checklist**
Before creating/modifying ANY file in `Pidgeon.Core/Services/`:
- [ ] **Zero standard-specific strings** (no "MSH", "ADT", "PID", etc.)
- [ ] **Zero standard-specific logic** (no segment expectations, field positions)
- [ ] **Plugin delegation for standard operations** (use plugin registry)
- [ ] **Standard-agnostic interfaces** (work with any healthcare standard)

---

## 🔗 **DEPENDENCY RULES**

### **Clean Architecture Dependency Flow**
```
Domain ← Application ← Infrastructure
  ↑         ↑            ↑
  └── Never depends on outer layers
```

### **Allowed Dependencies**
```csharp
// ✅ CORRECT: Domain depends on nothing
namespace Pidgeon.Core.Domain.Clinical {
    public record Patient(string MRN, PersonName Name);
}

// ✅ CORRECT: Application depends only on Domain
namespace Pidgeon.Core.Application.Services {
    using Pidgeon.Core.Domain.Clinical;
    public class PatientService(IPatientRepository repo) { }
}

// ✅ CORRECT: Infrastructure depends on Application interfaces
namespace Pidgeon.Core.Infrastructure.Data {
    using Pidgeon.Core.Application.Interfaces;
    public class SqlPatientRepository : IPatientRepository { }
}
```

### **❌ FORBIDDEN Dependencies**
```csharp
// ❌ Domain depending on Infrastructure
using Pidgeon.Core.Infrastructure.Standards.Common.HL7;

// ❌ Domain depending on Application
using Pidgeon.Core.Application.Services;

// ❌ Application depending on Infrastructure concrete types
using Pidgeon.Core.Infrastructure.Data.SqlPatientRepository;
```

---

## 📦 **NAMESPACE & IMPORT RULES**

### **Namespace Standards**
```csharp
// ✅ CORRECT: Specific namespace imports
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Application.Services.Generation;

// ❌ FORBIDDEN: Broad namespace imports
using Pidgeon.Core;  // Too broad
using System;        // Specific types preferred: using System.Collections.Generic;
```

### **Domain Boundary Enforcement**
```csharp
// ✅ Each service depends on exactly ONE domain
public class ClinicalService {
    // Uses: Pidgeon.Core.Domain.Clinical only
}

public class MessagingService {
    // Uses: Pidgeon.Core.Domain.Messaging only
}

// ✅ Cross-domain communication through adapters
public interface IClinicalToMessagingAdapter {
    HL7_ORM_Message CreateOrder(Prescription prescription);
}
```

---

## 🧪 **TESTING RULES**

### **Test-First for Critical Logic**
```csharp
// ✅ REQUIRED: Test healthcare logic first
[Fact(DisplayName = "Pharmacy system should reject invalid DEA numbers")]
public void Should_Reject_Invalid_DEA_Numbers() {
    var provider = new Provider { DEANumber = "INVALID123" };
    var result = provider.ValidateDEA();
    result.IsFailure.Should().BeTrue();
}
```

### **Test Naming Convention**
```csharp
// ✅ PATTERN: Should_ExpectedBehavior_When_StateUnderTest
public void Should_Generate_Valid_HL7_Message_When_All_Required_Fields_Present() { }
public void Should_Return_Validation_Error_When_Patient_ID_Missing() { }
```

### **Coverage Priorities**
- **Domain Models**: 95%+ (Core business logic)
- **Healthcare Validation**: 90%+ (Patient safety critical)
- **Message Generation**: 90%+ (Must produce valid messages)
- **CLI Commands**: 60%+ (Manual testing acceptable)

---

## 🎯 **BUSINESS MODEL BOUNDARIES**

### **Core+ Strategy Enforcement**
```csharp
// ✅ FREE CORE (MPL 2.0)
- Algorithmic generation (25 medications, 50 names)
- Complete HL7/FHIR/NCPDP standards engines
- CLI interface, domain models
- Deterministic testing with seeds

// 💼 SUBSCRIPTION ONLY
- Live datasets (150+ medications)
- AI enhancement (BYOK for Professional, unlimited for Enterprise)
- Desktop GUI, cloud API
- Vendor-specific templates
```

### **Feature Placement Rules**
- **Core features → MPL 2.0**: Standards engines, basic generation, CLI
- **Professional features → Proprietary**: AI enhancement, live datasets, GUI
- **Enterprise features → Proprietary**: Unlimited AI, custom datasets, collaboration

---

## 📝 **CODE PATTERNS**

### **Error Handling Pattern**
```csharp
// ✅ ALWAYS: Use Result<T> for business operations
public Result<Patient> ValidatePatient(PatientData data) {
    if (string.IsNullOrEmpty(data.MRN))
        return Result<Patient>.Failure(PatientErrors.MissingMRN);
        
    return Result<Patient>.Success(new Patient(data));
}

// ❌ NEVER: Exceptions for business logic
public Patient ValidatePatient(PatientData data) {
    if (string.IsNullOrEmpty(data.MRN))
        throw new ArgumentException("MRN required"); // FORBIDDEN
}
```

### **Plugin Registration Pattern**
```csharp
// ✅ ALWAYS: Convention-based plugin registration
services.AddPlugin<HL7v23Plugin>();
services.AddPlugin<FHIRPlugin>();

// ❌ NEVER: Manual service registration explosion
services.AddScoped<IHL7Parser, HL7Parser>();
services.AddScoped<IHL7Generator, HL7Generator>();
// ... 22 more identical registrations
```

### **Documentation Pattern**
```csharp
// ✅ CORRECT: Clear, concise, professional
/// <summary>
/// Validates HL7 messages against vendor-specific configuration patterns.
/// </summary>
/// <param name="message">HL7 message to validate</param>
/// <param name="config">Vendor configuration for validation rules</param>
/// <returns>Validation result with conformance score and detected deviations</returns>

// ❌ FORBIDDEN: Meta-commentary
/// <summary>
/// FOLLOWS CLEAN ARCHITECTURE: This service delegates to plugins
/// avoiding architectural violations per our four-domain approach.
/// </summary>
```

---

## 🚫 **ANTI-PATTERNS TO AVOID**

### **Code Structure Anti-Patterns**
```csharp
// ❌ NEVER: Code graveyards
// LEGACY APPROACH - keeping for reference
// public static class OldMessageUtils { ... }

// ❌ NEVER: Hedged implementations  
public class MessageService {
    // New approach (preferred)
    var result = NewMethod();
    // Fallback to old approach if needed
    if (result.IsFailure) result = OldMethod();
}

// ❌ NEVER: Development conversation artifacts
// Claude suggested using this pattern instead of the previous approach
// Updated per architectural review recommendations
```

### **Dependency Anti-Patterns**
```csharp
// ❌ NEVER: Service location
var service = ServiceLocator.Get<IMessageService>();

// ❌ NEVER: Static dependencies in domain
public class Patient {
    public void Save() {
        DatabaseHelper.Save(this); // Static dependency
    }
}

// ❌ NEVER: Infrastructure imports in domain
using Pidgeon.Core.Infrastructure.Standards.Common.HL7; // In domain file
```

### **Error Handling Anti-Patterns**
```csharp
// ❌ NEVER: Exception-driven business logic
try {
    ProcessMessage(message);
} catch (InvalidMessageException) {
    return "Message invalid"; // Control flow via exceptions
}

// ❌ NEVER: Swallowing errors
try {
    RiskyOperation();
} catch {
    // Silent failure
}
```

---

## 📋 **PRE-COMMIT VALIDATION**

### **Before ANY Commit, Verify**:
- [ ] **Zero domain boundary violations** (Messaging importing Clinical, etc.)
- [ ] **Zero Infrastructure dependencies in Domain/Application** 
- [ ] **No standard-specific logic in core services** (no "ADT", "PID", "MSH" in core)
- [ ] **Result<T> used for all business operations** (no ArgumentException for validation)
- [ ] **No static classes with business logic** (constants/pure functions OK)
- [ ] **No meta-commentary or development artifacts**
- [ ] **Professional documentation only** (what it does, not why changed)
- [ ] **Plugin architecture respected** (new standards as plugins)
- [ ] **Service registration uses convention** (not manual explosion)

### **Architecture Validation Commands**
```bash
# Check for domain violations
grep -r "using.*Clinical" src/Pidgeon.Core/Domain/Messaging/
grep -r "using.*Infrastructure" src/Pidgeon.Core/Domain/
grep -r "using.*Infrastructure" src/Pidgeon.Core/Application/

# Check for standard-specific logic in core
grep -r "ADT\|PID\|MSH\|RDE" src/Pidgeon.Core/Application/Services/
grep -r "FHIR\|Patient\|Observation" src/Pidgeon.Core/Application/Services/

# Check for exception anti-pattern
grep -r "throw new ArgumentException" src/Pidgeon.Core/Domain/
grep -r "throw new ArgumentException" src/Pidgeon.Core/Application/
```

---

## 🔧 **REFACTORING RULES**

### **When Fixing Architectural Violations**:
1. **Fix the deepest violation first** (Domain boundaries before duplication)
2. **Make one type of fix at a time** (Don't mix domain fixes with DI fixes)
3. **Test after each logical fix** (Don't compound changes)
4. **Update documentation immediately** (Don't defer doc updates)

### **Duplication Elimination Strategy**:
1. **Extract base classes/interfaces first** (Foundation for deduplication)
2. **Move repeated logic to base** (Common functionality)
3. **Specialize derived classes** (Only unique behavior)
4. **Remove redundant code** (Clean up after extraction)

### **Service Refactoring Rules**:
```csharp
// ✅ CORRECT: Single domain dependency
public class ClinicalService {
    // Only imports from Pidgeon.Core.Domain.Clinical
}

// ✅ CORRECT: Cross-domain via adapters
public class TransformationService {
    private readonly IClinicalToMessagingAdapter _adapter;
    
    public HL7Message Transform(Prescription rx) => _adapter.CreateOrder(rx);
}
```

---

## 🎯 **QUALITY GATES**

### **Code Review Checklist**
- [ ] **Architecture**: Follows four-domain boundaries
- [ ] **Dependencies**: Clean Architecture dependency flow respected
- [ ] **Patterns**: Uses Result<T>, dependency injection, plugin delegation
- [ ] **Standards**: No hardcoded standard logic in core
- [ ] **Documentation**: Professional, clear, no meta-commentary
- [ ] **Tests**: Behavior-driven for healthcare scenarios
- [ ] **Performance**: <50ms for message transformation operations

### **Health Score Validation**
After any significant change, validate:
- [ ] **Domain violations**: Should remain 0
- [ ] **Infrastructure violations**: Should remain 0  
- [ ] **Critical FIXMEs**: Should decrease or remain manageable
- [ ] **Code duplication**: Should trend downward
- [ ] **Test coverage**: Should maintain >90% on critical paths

---

## 📖 **DECISION FRAMEWORK**

### **For Every Significant Change, Ask**:
1. **Architecture**: Does this follow our four-domain model?
2. **Standards**: Is this compliant with healthcare standards?
3. **Business Model**: Is this in the right tier (Core/Professional/Enterprise)?
4. **Scalability**: Will this work with 10,000 users?
5. **Testing**: How do we prove this works for healthcare scenarios?

### **Required Documentation**:
- **LEDGER.md**: For architectural decisions or breaking changes
- **This Document**: If rules evolve or exceptions are needed
- **agent-reflection.md**: For consistency validation after changes

---

## 🚀 **SUCCESS CRITERIA**

### **Code is Ready When**:
- [ ] Compiles cleanly with zero warnings
- [ ] Follows all sacred architectural principles
- [ ] No anti-patterns or development artifacts present
- [ ] Professional documentation quality
- [ ] Tests prove healthcare scenarios work
- [ ] Performance meets <50ms targets
- [ ] Architecture health score maintained/improved

### **Sprint is Ready When**:
- [ ] All critical violations resolved
- [ ] P0 features unblocked
- [ ] Foundation supports rapid development
- [ ] Technical debt under control
- [ ] Team can focus on feature development

---

**Remember**: Every line of code is permanent until explicitly changed. Write code that reflects the professionalism and technical excellence our healthcare platform demands.

**The Goal**: Code so clean and consistent that any senior developer can immediately understand and extend it without architectural violations.