# Error Resolution Methodology

**Purpose**: Guide AI agents toward senior-level error handling and build failure resolution  
**Principle**: Measure twice, cut once - understand before fixing  
**Anti-Pattern**: Reactive fix-and-pray development

---

## 🚨 **When Errors Occur: The STOP-THINK-ACT Framework**

### **STOP: Pause and Assess**
When encountering build failures, runtime errors, or unexpected behavior:

1. **Don't immediately fix** - Resist the urge to jump into code changes
2. **Read the full error** - Don't just scan for keywords, understand the complete message
3. **Note all error locations** - Are there multiple related failures?
4. **Identify the trigger** - What change caused this error to surface?

### **THINK: Root Cause Analysis**

#### **Architecture-Level Questions:**
- Is this error a symptom of a deeper design issue?
- Does this violate any of our architectural principles (DDD, Plugin Architecture, DI, Result<T>)?
- Are we trying to force incompatible patterns together?
- Is the error pointing to missing abstractions or leaky abstractions?

#### **Dependency Analysis:**
- What components depend on the failing code?
- What does the failing code depend on?
- If we fix this one way, what else might break?
- Are there cascade effects we need to consider?

#### **Context Understanding:**
- Why was the original code written this way?
- What constraints led to the current design?
- Is this a fundamental mismatch or a surface-level syntax issue?

### **ACT: Measured Resolution**

#### **Plan Before Coding:**
1. **Define the minimal fix** that addresses the root cause
2. **Consider alternative approaches** - don't just pick the first solution
3. **Validate against principles** - does this maintain our architectural integrity?
4. **Anticipate side effects** - what might this change break?

#### **Implementation Strategy:**
1. **Start with the simplest change** that could work
2. **Make one logical change at a time** - don't compound fixes
3. **Test incrementally** - verify each change works before moving on
4. **Document the reasoning** - why did we choose this approach?

---

## 📋 **Error Category Response Patterns**

### **Type 1: Compilation Errors**

#### **Symptoms:**
- Missing references, type mismatches, signature conflicts
- Build fails with specific compiler errors

#### **Senior Developer Response:**
1. **Understand the type system** - what is the compiler actually telling us?
2. **Check interface contracts** - are we violating expected signatures?
3. **Consider generic constraints** - nullable reference types, variance, etc.
4. **Look for architectural misalignment** - forcing incompatible patterns?

#### **Example from Our Session:**
```
Error: 'NumericField' does not implement inherited abstract member 'FormatTypedValue(decimal)'
```

**❌ Reactive Approach:**
- Immediately try different method signatures until it compiles

**✅ Senior Developer Approach:**
- Understand: Why does the compiler expect `decimal` when we have `decimal?`?
- Analyze: How do nullable value types work in generic constraints?
- Design: Should we use `decimal?` as the generic type parameter instead?
- Implement: Make the minimal change that aligns the type system properly

### **Type 2: Runtime Errors**

#### **Symptoms:**
- Exceptions, null reference errors, invalid operations
- Code compiles but fails at runtime

#### **Senior Developer Response:**
1. **Trace the execution path** - how did we get to this state?
2. **Validate assumptions** - what did we assume that isn't true?
3. **Check error handling** - are we properly using Result<T> patterns?
4. **Consider state management** - is there invalid state transition?

### **Type 3: Integration Errors**

#### **Symptoms:**
- Services not found, dependency injection failures
- Plugin loading issues, configuration problems

#### **Senior Developer Response:**
1. **Map the dependency graph** - what needs what?
2. **Verify registration** - are all services properly registered?
3. **Check lifetimes** - singleton vs scoped vs transient issues?
4. **Validate configuration** - are we following DI best practices?

---

## 🔧 **Troubleshooting Methodology**

### **Phase 1: Information Gathering (Don't Skip This!)**

#### **For Build Failures:**
```bash
# Get verbose build output
dotnet build --verbosity detailed

# Check specific project dependencies
dotnet list package
dotnet restore --verbosity detailed
```

#### **Questions to Ask:**
- What was the last working state?
- What changes were made since then?
- Are there multiple errors that might be related?
- Do we have test coverage for this area?

### **Phase 2: Hypothesis Formation**

#### **Create Competing Theories:**
1. **Surface-level fix:** "Just change the signature"
2. **Design issue:** "The abstraction is wrong"
3. **Integration problem:** "Components aren't compatible"

#### **Rank by Likelihood:**
- Which theory explains the most symptoms?
- Which aligns with our architectural principles?
- Which has the least risk of unintended consequences?

### **Phase 3: Minimal Viable Fix**

#### **Test Each Theory:**
1. **Start with lowest-risk change**
2. **Make one change at a time**
3. **Validate after each change**
4. **Roll back if the theory is wrong**

---

## 🏗️ **Architecture-Specific Guidelines**

### **Domain-Driven Design Violations**
**Symptom:** Domain models coupling to infrastructure  
**Root Cause:** Leaky abstractions, wrong dependency direction  
**Fix Strategy:** Invert dependencies, add missing abstractions

### **Plugin Architecture Issues**
**Symptom:** Plugins can't be loaded, interface mismatches  
**Root Cause:** Interface evolution, missing extension points  
**Fix Strategy:** Versioned interfaces, adapter patterns

### **Dependency Injection Problems**
**Symptom:** Services not found, circular dependencies  
**Root Cause:** Lifetime mismatches, missing registrations  
**Fix Strategy:** Service audit, lifetime review, factory patterns

### **Result<T> Pattern Violations**
**Symptom:** Exceptions for control flow, unhandled errors  
**Root Cause:** Bypassing explicit error handling  
**Fix Strategy:** Convert exceptions to Results, add error mapping

---

## 📚 **Error Resolution Checklist**

### **Before Making Any Code Changes:**
- [ ] Do I understand what the error is actually telling me?
- [ ] Have I identified the root cause, not just the symptom?
- [ ] Do I know what other code might be affected by my fix?
- [ ] Have I considered alternative approaches?
- [ ] Does my proposed fix align with our architectural principles?
- [ ] Am I making the minimal change that solves the problem?

### **During Implementation:**
- [ ] Am I making one logical change at a time?
- [ ] Can I explain why this fix will work?
- [ ] Have I tested this change in isolation?
- [ ] Am I introducing any new technical debt?

### **After the Fix:**
- [ ] Does the entire solution still build?
- [ ] Have I broken any existing functionality?
- [ ] Is this fix sustainable and maintainable?
- [ ] Should this be documented in LEDGER.md?

---

## 🎯 **Success Metrics**

### **Indicators of Good Error Resolution:**
- **Fewer iterations** - Fix works on first or second try
- **No cascade failures** - Fix doesn't introduce new errors
- **Principle alignment** - Solution fits our architecture
- **Future-proof** - Won't need to be revised soon

### **Red Flags:**
- **Trial-and-error fixes** - Randomly trying solutions
- **Compound changes** - Multiple unrelated fixes at once
- **Principle violations** - Shortcuts that compromise architecture
- **Band-aid solutions** - Fixes that don't address root cause

---

## 💡 **Remember: Code is Communication**

Every fix is a statement about:
- How we understand the problem
- What we value in our architecture  
- How we handle complexity and uncertainty

**Wise developers fix the root cause.  
Excellent developers prevent the problem class entirely.**

---

**When in doubt, ask:**
- "What is this error really telling me?"
- "What would a senior developer do here?"
- "How does this fit our overall architectural vision?"