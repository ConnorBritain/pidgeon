# Agent Session Initialization Protocol
**Purpose**: Single command to fully initialize AI agents for Pidgeon development  
**Usage**: "Read `docs/SESSION_INIT.md` and follow its initialization protocol"  
**Result**: Agent understands current state, rules, priorities, and can begin productive work immediately  

---

## 🚨 **MANDATORY INITIALIZATION SEQUENCE**

### **Step 1: Read Core Rules & Context** ⭐
**Command**: Read these documents in order:
1. **`docs/RULES.md`** - Sacred principles, anti-patterns, validation rules
2. **`docs/arch_reviews/ARCH_FIX.md`** - Current rehabilitation roadmap with specific tasks
3. **`docs/DEVELOPMENT.md`** - Strategic direction and P0 feature priorities

### **Step 2: Assess Current State** ⭐
**Commands**:
```bash
git status                    # Check working directory state
git log --oneline -5         # Recent development activity
grep -c "TODO:\|FIXME:" src/**/*.cs  # Current technical debt count
```

### **Step 3: Validate Understanding** ⭐
**Confirm you understand**:
- **Current Phase**: Foundation rehabilitation (Week 1-3 of ARCH_FIX.md)
- **Blocker Status**: P0 domain violations must be fixed before any feature work
- **Architecture**: Four-domain model with plugin architecture
- **Error Protocol**: STOP-THINK-ACT methodology is mandatory

---

## 📊 **CURRENT STATE SUMMARY**
*Last Updated: September 2, 2025*

### **Project Status**
- **Phase**: Architectural rehabilitation sprint (Week 1 of 3-4)
- **Health Score**: 87/100 (Strong foundation with critical violations)
- **Build Status**: ✅ Clean (0 errors, 0 warnings)
- **P0 MVP Status**: ❌ BLOCKED by architectural violations

### **Critical Path**
1. **CURRENT PRIORITY**: P0 domain boundary violations (21 files)
2. **Next Week**: Code duplication patterns (600+ duplicate lines)
3. **Week 3**: P0-blocking TODOs and validation
4. **Gate to P0**: Health score >95/100, foundation ready

### **Business Context**
- **Model**: Core+ (Free algorithmic core + Paid AI/GUI/Cloud)
- **Target Users**: Healthcare developers, consultants, informaticists, administrators
- **Mission**: AI-augmented universal healthcare standards platform (HL7, FHIR, NCPDP)

---

## 🎯 **IMMEDIATE ACTION GUIDANCE**

### **If User Asks for Feature Work**:
**Response**: "I need to fix critical architectural violations first. We have 21 domain boundary violations blocking P0 development. Would you like me to start with those?"

### **If User Reports Errors**:
**Response**: Follow STOP-THINK-ACT protocol from `docs/RULES.md`. Don't fix immediately - analyze first.

### **If User Wants Architecture Changes**:
**Response**: Reference `docs/roadmap/INIT.md` sacred principles. Propose changes that align with four-domain architecture.

### **If User Wants New Standards Support**:
**Response**: Implement as plugin following existing HL7v23Plugin pattern. Never modify core services.

---

## 📚 **ESSENTIAL READING LIST**

### **🚨 CRITICAL - Read Every Session**
1. **`docs/RULES.md`** - Development rules and sacred principles
2. **`docs/arch_reviews/ARCH_FIX.md`** - Current rehabilitation tasks
3. **`docs/DEVELOPMENT.md`** - Strategic roadmap and P0 features

### **🔍 REFERENCE - Read When Relevant**
4. **`docs/roadmap/INIT.md`** - Sacred architectural principles (for architecture questions)
5. **`docs/agent_steering/error-resolution-methodology.md`** - Error handling protocol
6. **`docs/ARCHITECTURE.md`** - Four-domain architecture details
7. **`docs/LEDGER.md`** - Decision history and rollback procedures

### **📋 USER STORIES - Read for Feature Work**
8. **`docs/user_stories/BACKLOG.md`** - P0 feature validation and priority
9. **`docs/user_stories/MVP_VALIDATION.md`** - Business model alignment

### **🏢 BUSINESS - Read for Strategy Questions**
10. **`docs/founding_plan/business_model.md`** - Core+ strategy details

---

## 🚀 **PRODUCTIVITY PATTERNS**

### **Session Start Checklist**
- [ ] Read RULES.md (understand constraints)
- [ ] Read ARCH_FIX.md (understand current priorities)
- [ ] Check git status (understand working state)
- [ ] Identify current sprint week (Week 1/2/3 of rehabilitation)
- [ ] Confirm task alignment with current phase

### **Before Any Code Changes**
- [ ] Validate against `docs/RULES.md` principles
- [ ] Check if task is P0-priority in `docs/arch_reviews/ARCH_FIX.md`
- [ ] Ensure no architectural violations will be introduced
- [ ] Plan testing approach per healthcare scenarios

### **Error Handling Protocol**
- [ ] STOP: Don't immediately fix
- [ ] THINK: Root cause analysis per `error-resolution-methodology.md`
- [ ] ACT: Minimal principled changes
- [ ] Document in required format

---

## 💡 **COMMON SESSION WORKFLOWS**

### **Architecture Fix Session**
1. Read SESSION_INIT.md (this document)
2. Identify current P0-P1 priority from ARCH_FIX.md
3. Read specific file violations listed
4. Plan fix strategy using RULES.md patterns
5. Implement with STOP-THINK-ACT for any errors
6. Update ARCH_FIX.md checkboxes when complete

### **Feature Development Session**
1. Read SESSION_INIT.md (this document)
2. Confirm foundation is ready (ARCH_FIX.md P0 complete)
3. Read relevant user stories from BACKLOG.md
4. Implement using four-domain architecture
5. Follow testing philosophy from test-philosophy.md
6. Validate against business model tiers

### **Debugging Session**
1. Read SESSION_INIT.md (this document)  
2. Apply STOP-THINK-ACT methodology immediately
3. Reference RULES.md for pattern compliance
4. Use root cause analysis before fixing
5. Document decision in LEDGER.md if architectural

---

## 🎯 **SUCCESS CRITERIA FOR INITIALIZATION**

### **Agent is Ready When They Can Answer**:
- "What's the current development phase?" → *Foundation rehabilitation week X*
- "What blocks P0 development?" → *21 domain boundary violations*  
- "How do I handle errors?" → *STOP-THINK-ACT methodology*
- "Can I add HL7-specific logic to core services?" → *No, use plugin delegation*
- "What's our business model?" → *Core+ with free core and subscription tiers*

### **Agent Can Begin Work When**:
- [ ] Understands current priorities from ARCH_FIX.md
- [ ] Knows sacred principles from RULES.md
- [ ] Recognizes anti-patterns to avoid
- [ ] Can validate architectural compliance
- [ ] Follows error resolution methodology

---

## ⚡ **QUICK START COMMANDS**

### **Initialization One-Liner**
```
Read docs/SESSION_INIT.md, follow its initialization protocol, then tell me what phase we're in and what you recommend we work on next.
```

### **Status Check Commands**
```bash
# Architecture health check
grep -c "TODO:\|FIXME:" src/**/*.cs

# Domain violation check  
grep -r "using.*Clinical" src/Pidgeon.Core/Domain/Messaging/

# Current git state
git status && git log --oneline -3
```

### **Ready-to-Work Confirmation**
**Agent should respond with**:
"Ready! We're in architectural rehabilitation Week X. Current priority is [specific P0 task from ARCH_FIX.md]. Shall I begin with [specific action]?"

---

## 🔄 **SESSION EFFICIENCY NOTES**

### **This Document Replaces**:
- ❌ "Read CLAUDE.md, DEVELOPMENT.md, ARCHITECTURE.md, user stories, git logs..."
- ❌ "Tell me where we are and what needs done..."
- ❌ "Here's the architectural context you need to understand..."

### **With Single Command**:
- ✅ "Read docs/SESSION_INIT.md and follow its protocol"
- ✅ Agent immediately understands current state
- ✅ Agent knows rules and can begin productive work
- ✅ Zero repeated context explanation needed

### **Agent Response Format**:
```
✅ Initialization Complete

Current State:
- Phase: [X from ARCH_FIX.md]
- Priority: [Current P0 task]
- Health Score: [X/100]

Ready to work on: [Specific recommendation]
Shall I proceed?
```

---

**The Goal**: Transform agent initialization from 5-10 minutes of context-setting to 30 seconds of focused protocol execution, enabling immediate productive work on current priorities.

**Usage**: Simply tell any new agent: "Read `docs/SESSION_INIT.md` and follow its initialization protocol"