# Segmint Healthcare Interoperability Platform - Agent Context

## 🎯 **Strategic Direction: Core+ Architecture & Multi-Standard Excellence**

**Status**: Building clean architecture from day one with Core+ business model  
**Mission**: AI-augmented universal healthcare standards platform (HL7, FHIR, NCPDP)
**Approach**: Domain-driven design with plugin architecture for scale without refactoring

---

## 📋 **Essential Context Documents**

### **Agent Steering Documentation (READ FIRST)**
1. **[`docs/agent_steering/product.md`](docs/agent_steering/product.md)** - Product vision and Core+ strategy
2. **[`docs/agent_steering/structure.md`](docs/agent_steering/structure.md)** - Clean architecture principles
3. **[`docs/agent_steering/tech.md`](docs/agent_steering/tech.md)** - Technology stack and patterns
4. **[`docs/agent_steering/agent-reflection.md`](docs/agent_steering/agent-reflection.md)** - Decision framework

### **Founding Strategy Documents**
5. **[`docs/founding_plan/core_plus_strategy.md`](docs/founding_plan/core_plus_strategy.md)** - Business model details
6. **[`docs/founding_plan/1_founder.md`](docs/founding_plan/1_founder.md)** - Technical founder perspective
7. **[`docs/founding_plan/2_consultant.md`](docs/founding_plan/2_consultant.md)** - Healthcare consultant insights
8. **[`docs/founding_plan/3_investor.md`](docs/founding_plan/3_investor.md)** - Investor business case

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

### **🆓 FREE CORE (MPL 2.0)**
- **HL7 v2.3**: Complete engine with all message types
- **FHIR R4**: Basic resources (Patient, Observation, MedicationRequest)
- **NCPDP SCRIPT**: Basic messages (NewRx, Refill, Cancel)
- **Universal**: CLI, domain models, validation

### **💼 PROFESSIONAL ($299 one-time)**
- Advanced message types and features
- Desktop GUI application
- AI features with BYOK
- Visual designers and tools

### **🏢 ENTERPRISE ($99-199/month per seat)**
- Cross-standard transformation
- Team collaboration and governance
- Cloud services with SLA
- Unlimited AI features

---

## 🚧 **Development Guidelines**

### **When Creating New Features:**
1. **Start with domain model** - What's the healthcare concept?
2. **Create standard adapters** - How does each standard serialize it?
3. **Register plugins** - Add to DI container
4. **Write tests first** - Domain, then adapters
5. **Use Result<T>** - No exceptions for control flow

### **When Adding New Standards:**
1. Create new namespace: `Segmint.Core.Standards.{Standard}`
2. Implement `IStandardAdapter<T>` interfaces
3. Add plugin registration
4. Keep existing code unchanged

### **Before Any Commit:**
- [ ] Domain logic has zero infrastructure dependencies
- [ ] New features are plugins, not core changes
- [ ] All services use dependency injection
- [ ] Error handling uses Result<T> pattern
- [ ] Core+ boundary respected (free vs paid features)
- [ ] Tests pass and cover new functionality

### **AI Feature Development:**
- [ ] BYOK for Professional tier (user provides OpenAI key)
- [ ] Usage tracking for Enterprise tier
- [ ] Algorithmic fallback when AI unavailable (Tier 2)
- [ ] Token limits to control costs

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
1. Architecture adherence
2. Standards compliance
3. Core+ strategy alignment
4. AI cost management
5. Testing strategy

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