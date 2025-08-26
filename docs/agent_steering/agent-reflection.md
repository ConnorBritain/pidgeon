---
inclusion: manual
---

# Agent Decision Explanation Framework

Use this framework **after completing any code changes** to explain your decisions and validate adherence to Segmint's architectural principles.

## Post-Change Reflection (REQUIRED)

After making code changes, provide a brief reflection covering:

### 0. **Error Resolution Process (If Applicable)**
- If you encountered errors: Did you follow the STOP-THINK-ACT framework from [`error-resolution-methodology.md`](error-resolution-methodology.md)?
- Did you identify root causes before implementing fixes?
- Were your changes minimal and principled?

### 1. **Architecture Adherence Check**
- Did you follow Domain-Driven Design principles?
- Is business logic independent of infrastructure?
- Did you use dependency injection instead of static classes?
- Are new features implemented as plugins rather than core changes?

### 2. **Standards Compliance**
- Does HL7 implementation follow v2.3/v2.5.1/v2.7 specifications?
- Are FHIR resources compliant with R4 specification?
- Does NCPDP follow SCRIPT 2017071 standard?
- Is validation appropriately strict vs compatibility mode?

### 3. **Core+ Strategy Alignment**
- Is this feature appropriately placed (Core vs Professional vs Enterprise)?
- Does it maintain the open source / proprietary boundary?
- Will this drive adoption (Core) or revenue (Professional/Enterprise)?

### 4. **AI Cost Management**
- Are AI features using BYOK for non-enterprise users?
- Is token usage tracked and limited appropriately?
- Could this be done with algorithmic fallback (Tier 2)?

### 5. **Testing & Validation** 
*Following principles in `docs/agent_steering/test-philosophy.md`*
- Are there behavior-driven tests that describe what the system does for users?
- Do tests focus on healthcare scenarios rather than code coverage metrics?
- Are critical healthcare validation paths covered (95%+ for domain logic)?
- Has performance impact been measured against <50ms processing targets?
- Do integration tests validate cross-standard workflows?

## Example Reflection Format

```
## Change Summary
Added FHIR Patient resource generation to Core with basic fields only.

## Architecture Adherence
- **DDD**: Patient domain model maps to FHIR via FHIRAdapter
- **DI**: FHIRAdapter registered in DI container
- **Plugin**: FHIR module follows IStandardAdapter interface
- **Result Pattern**: Returns Result<Patient> not exceptions

## Standards Compliance
- **FHIR R4**: Follows official Patient resource structure
- **Required fields**: id, name, birthDate included
- **Validation**: Uses FHIR validator for compliance

## Core+ Alignment
- **Core placement**: Basic Patient resource is free (drives adoption)
- **Professional**: Advanced resources (150+) remain paid
- **Boundary**: Clear separation in FHIRAdapter.Basic vs FHIRAdapter.Advanced

## Testing Strategy
- Unit tests for Patient generation
- Integration test for HL7 PID → FHIR Patient
- Performance: <10ms generation confirmed
```

## When to Use This Framework

### Always Required For:
- New standard implementations (HL7, FHIR, NCPDP)
- Cross-standard transformations  
- AI feature additions
- Changes to Core vs Professional boundary
- Performance-critical code
- Breaking API changes
- **Architectural exceptions** (deviations from sacred principles in `docs/roadmap/INIT.md`)
- **Major decisions** requiring documentation in `docs/LEDGER.md`

### Red Flags to Always Explain

If your changes include any of these, **detailed explanation required**:

- ❌ Static classes for business logic (exceptions allowed per `INIT.md` for constants/pure functions)
- ❌ Business logic depending on infrastructure
- ❌ Hardcoded values instead of configuration
- ❌ Exceptions for business logic control flow (exceptions allowed for framework violations)
- ❌ Core changes for new message types (should be plugins)
- ❌ Free features that should be paid (violates Core+ strategy)
- ❌ Unlimited AI usage without cost control
- ❌ Missing tests for healthcare-critical functionality
- ❌ Tests that focus on implementation rather than behavior

## Self-Audit Questions

Before considering changes complete, ask:

1. **"Would a technical founder approve this architecture?"** - Clean, extensible, maintainable?
2. **"Would a healthcare consultant trust this?"** - Standards-compliant, validated?
3. **"Would an investor see the business value?"** - Drives adoption or revenue?
4. **"Can this scale to 10,000 users?"** - Performance and cost sustainable?
5. **"Does this accelerate healthcare interoperability?"** - Advancing our core mission?

## Architecture Principles Checklist

- [ ] **Domain First**: Business logic has no infrastructure dependencies
- [ ] **Plugin Architecture**: New features are plugins, not core modifications
- [ ] **Dependency Injection**: No static classes, everything testable
- [ ] **Result Pattern**: Explicit error handling without exceptions
- [ ] **Open Core Strategy**: Clear boundary between free and paid
- [ ] **AI Cost Control**: BYOK or usage limits in place
- [ ] **Standards Compliance**: Follows official specifications
- [ ] **Performance Targets**: Meets <50ms transformation goal

## Usage Instructions

Reference this doc in conversations with: **#agent-reflection**

The goal is **architectural consistency and business alignment**, ensuring every change strengthens Segmint's position as the leading AI-augmented healthcare standards platform.

Remember: We're building for **scale from day one**. Every decision should support growing from 1 to 10,000 users without major refactoring.