# Agent Session Initialization Protocol
**Purpose**: Single command to fully initialize AI agents for Pidgeon development  
**Usage**: "Read `docs/SESSION_INIT.md` and follow its initialization protocol"  
**Result**: Agent understands current state, rules, priorities, and can begin productive work immediately  

---

## 🚨 **MANDATORY INITIALIZATION SEQUENCE**

### **Step 1: Read Core Rules & Current Roadmap** ⭐
**Command**: Read these documents in order:
1. **`docs/roadmap/PIDGEON_ROADMAP.md`** - Master P0-P2 roadmap with success criteria
2. **`docs/roadmap/features/NORTHSTAR.md`** - North star vision and product focus
3. **`docs/roadmap/CLI_REFERENCE.md`** - Complete CLI structure for P0 development
4. **`docs/DEVELOPMENT.md`** - Current status (foundation complete) and P0 features
5. **`docs/RULES.md`** - Sacred architectural principles (still enforced)

### **Step 2: Assess Current State** ⭐
**Commands**:
```bash
git status                    # Check working directory state
git log --oneline -5         # Recent development activity
grep -c "TODO:\|FIXME:" src/**/*.cs  # Current technical debt count
```

### **Step 3: Validate Understanding** ⭐
**Confirm you understand**:
- **Current Phase**: P0 MVP development (foundation complete at 100/100 health)
- **North Star**: "Realistic scenario testing without PHI compliance nightmare"
- **P0 Features**: 6 core features in embryonic development sequence (generate→deident→validate→config→workflow→diff)
- **CLI-First**: One engine, two frontends strategy with Free/Pro/Enterprise gating
- **Architecture**: Four-domain model with plugin architecture (fully implemented)
- **Error Protocol**: STOP-THINK-ACT methodology still mandatory

---

## 📊 **CURRENT STATE SUMMARY**
*Last Updated: September 5, 2025*

### **Project Status**
- **Phase**: P0 MVP Development (foundation rehabilitation complete)
- **Health Score**: 100/100 (Perfect architectural foundation)
- **Build Status**: ✅ Clean (0 errors, 43 tests passing)
- **P0 MVP Status**: ✅ READY for feature development

### **P0 Embryonic Development Plan (8 weeks)**
**Rationale**: Compound growth sequence where each feature builds on previous intelligence
1. **Weeks 1-2**: Generation Engine (foundational heartbeat - creates test data supply)
2. **Week 3**: De-identification (major differentiation - unlocks real data segment)  
3. **Week 4**: Validation Engine (quality control - creates feedback loops)
4. **Week 5**: Vendor Pattern Detection (network effects - builds proprietary intelligence)
5. **Week 6**: Workflow Wizard [Pro] (natural revenue conversion using compound intelligence)
6. **Weeks 7-8**: Diff + AI Triage [Pro] (advanced troubleshooting - ultimate compound feature)

### **Business Context**
- **North Star**: "Realistic scenario testing without PHI compliance nightmare"
- **Model**: CLI-first with GUI harmony (Free CLI + Pro/Enterprise features)
- **Target Users**: Healthcare developers, consultants, informaticists, administrators
- **Mission**: Transform healthcare testing through synthetic data & de-identification

---

## 🎯 **IMMEDIATE ACTION GUIDANCE**

### **If User Asks for Feature Work**:
**Response**: "Foundation is complete! We're ready for P0 development. Which P0 feature should we work on: generate, validate, deident, config, workflow, or diff?"

### **If User Reports Errors**:
**Response**: Follow STOP-THINK-ACT protocol from `docs/RULES.md`. Don't fix immediately - analyze first.

### **If User Wants CLI Commands**:
**Response**: Reference `docs/roadmap/features/cli_baseline.md` for exact command structure. Implement with clear Free/Pro gating.

### **If User Wants New Standards Support**:
**Response**: Implement as plugin following existing HL7v23Plugin pattern. Never modify core services.

### **If User Wants GUI Features**:
**Response**: Follow CLI-GUI harmonization strategy - one engine, two frontends. GUI operations export CLI equivalents.

---

## 📚 **ESSENTIAL READING LIST**

### **🚨 CRITICAL - Read Every Session**
1. **`docs/roadmap/PIDGEON_ROADMAP.md`** - Complete P0-P2 roadmap with all success metrics
2. **`docs/roadmap/features/NORTHSTAR.md`** - Core value proposition and user focus
3. **`docs/roadmap/CLI_REFERENCE.md`** - Complete CLI command structure and examples
4. **`docs/DEVELOPMENT.md`** - Current status and P0 development plan
5. **`docs/RULES.md`** - Sacred architectural principles and error handling

### **🔍 REFERENCE - Read When Relevant**
6. **`docs/roadmap/features/cli_gui_harmonization.md`** - Platform strategy for CLI+GUI
7. **`docs/roadmap/INIT.md`** - Sacred architectural principles (for architecture questions)
8. **`docs/ARCHITECTURE.md`** - Four-domain architecture + P0 feature architecture
9. **`docs/agent_steering/error-resolution-methodology.md`** - STOP-THINK-ACT protocol
10. **`docs/LEDGER.md`** - Recent development decisions and rollback procedures
11. **`docs/_archive/`** - Historical documents (foundation work, old roadmaps)

### **📋 USER STORIES - Read for Feature Work**
8. **`docs/user_stories/BACKLOG.md`** - P0 feature validation and priority
9. **`docs/user_stories/MVP_VALIDATION.md`** - Business model alignment

### **🏢 BUSINESS - Read for Strategy Questions**
10. **`docs/founding_plan/business_model.md`** - Core+ strategy details

---

## 🚀 **PRODUCTIVITY PATTERNS**

### **Session Start Checklist**
- [ ] Read PIDGEON_ROADMAP.md (complete P0-P2 roadmap)
- [ ] Read NORTHSTAR.md (understand core value proposition)
- [ ] Check git status (understand working state)
- [ ] Identify P0 feature priorities (6 features over 6 weeks)
- [ ] Confirm task alignment with CLI-first development approach

### **Before Any Code Changes**
- [ ] Validate against `docs/RULES.md` principles (still enforced)
- [ ] Check CLI command structure in `CLI_REFERENCE.md`
- [ ] Ensure Free/Pro/Enterprise feature gating is correct
- [ ] Plan testing approach per healthcare scenarios
- [ ] Verify one-engine-two-frontends pattern compliance

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
- "What's the current development phase?" → *P0 MVP development (foundation complete)*
- "What's our north star?" → *Realistic scenario testing without PHI compliance nightmare*  
- "What are the P0 core features?" → *6 features: generate, validate, deident, config, workflow, diff*
- "How do I handle errors?" → *STOP-THINK-ACT methodology*
- "Can I add HL7-specific logic to core services?" → *No, use plugin delegation*
- "What's our CLI-GUI strategy?" → *One engine, two frontends with export/import symmetry*

### **Agent Can Begin Work When**:
- [ ] Understands P0 feature roadmap from pidgeon_feature_plan.md
- [ ] Knows sacred architectural principles from RULES.md (still enforced)
- [ ] Understands CLI command structure from cli_baseline.md
- [ ] Can identify Free vs Pro vs Enterprise feature boundaries
- [ ] Follows CLI-GUI harmonization strategy
- [ ] Follows STOP-THINK-ACT error resolution methodology

---

## ⚡ **QUICK START COMMANDS**

### **Initialization One-Liner**
```
Read docs/SESSION_INIT.md, follow its initialization protocol, then tell me what phase we're in and what you recommend we work on next.
```

### **Status Check Commands**
```bash
# Technical debt check (should be <20 items)
find pidgeon -name "*.cs" -exec grep -l "TODO:\|FIXME:" {} \; | wc -l

# P0 feature progress check
ls pidgeon/Pidgeon.CLI/Commands/  # Check CLI commands implemented

# Current git state
git status && git log --oneline -3

# Build health check
dotnet build --no-restore  # Should be clean
```

### **Ready-to-Work Confirmation**
**Agent should respond with**:
```
✅ Initialization Complete

Current State:
- Phase: P0 MVP Development (Week X of 6)
- Health: 100/100 (Foundation complete)
- Priority: [Current P0 feature - generate/validate/deident/config/workflow/diff]

Ready to work on: [Specific CLI command or feature implementation]
Shall I proceed?
```

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