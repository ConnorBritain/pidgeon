# Segmint Healthcare Interoperability Platform - Agent Context

## 🎯 **Strategic Direction: Core+ Architecture & Multi-Standard Excellence**

**Status**: Building clean architecture from day one with Core+ business model  
**Mission**: AI-augmented universal healthcare standards platform (HL7, FHIR, NCPDP)
**Approach**: Domain-driven design with plugin architecture for scale without refactoring

---

## 📋 **Essential Context Documents**

### **🚨 CRITICAL - Must Read for Every Session**
1. **[`docs/roadmap/INIT.md`](docs/roadmap/INIT.md)** - 90-day foundation roadmap with SACRED architectural principles
2. **[`docs/LEDGER.md`](docs/LEDGER.md)** - Complete development decision log and rollback procedures
3. **[`docs/agent_steering/test-philosophy.md`](docs/agent_steering/test-philosophy.md)** - Testing strategy and behavior-driven approach

### **Agent Steering Documentation (READ FIRST)**
4. **[`docs/agent_steering/product.md`](docs/agent_steering/product.md)** - Product vision and Core+ strategy
5. **[`docs/agent_steering/structure.md`](docs/agent_steering/structure.md)** - Clean architecture principles
6. **[`docs/agent_steering/tech.md`](docs/agent_steering/tech.md)** - Technology stack and patterns
7. **[`docs/agent_steering/agent-reflection.md`](docs/agent_steering/agent-reflection.md)** - Decision framework
8. **[`docs/agent_steering/error-resolution-methodology.md`](docs/agent_steering/error-resolution-methodology.md)** - STOP-THINK-ACT error handling framework

### **Founding Strategy Documents**
9. **[`docs/founding_plan/business_model.md`](docs/founding_plan/business_model.md)** - **NEW: Crystallized subscription-first revenue model**
10. **[`docs/founding_plan/generation_considerations.md`](docs/founding_plan/generation_considerations.md)** - **NEW: Generation service architecture & AI integration**
11. **[`docs/founding_plan/core_plus_strategy.md`](docs/founding_plan/core_plus_strategy.md)** - Original business model details
12. **[`docs/founding_plan/1_founder.md`](docs/founding_plan/1_founder.md)** - Technical founder perspective
13. **[`docs/founding_plan/2_consultant.md`](docs/founding_plan/2_consultant.md)** - Healthcare consultant insights
14. **[`docs/founding_plan/3_investor.md`](docs/founding_plan/3_investor.md)** - Investor business case

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
namespace Segmint.Core.Domain {
    public record Prescription(Patient Patient, Medication Drug, Provider Prescriber);
}

// ✅ Standards as adapters
namespace Segmint.Core.Standards {
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
1. Create new namespace: `Segmint.Core.Standards.{Standard}`
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

## 💡 **Remember: We're Building for Scale**

**Not building**: An HL7 generator that happens to support FHIR
**Actually building**: A healthcare data platform with multiple standard serializations

**Not building**: A better Mirth Connect
**Actually building**: The next-generation AI-augmented interoperability platform

Every architectural decision should support going from 1 user to 10,000 users without major refactoring. Every feature should either drive adoption (Core) or revenue (Professional/Enterprise).

---

**The Goal**: Become the dominant platform for healthcare standards development and testing by combining the best of open source adoption with commercial success.

*Build for the platform you'll need in 5 years, not the MVP you need next month.*