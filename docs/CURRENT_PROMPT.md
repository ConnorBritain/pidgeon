# Claude Code Agent Initialization Prompt

**Purpose**: Reliable initialization prompt for new Claude Code instances to understand project context and be ready for productive data sprint work.

**Usage**: Copy and paste this prompt into a new Claude Code session to initialize an agent for Pidgeon development.

---

## 🚀 **INITIALIZATION PROMPTS**

### PROMPT 1: SESSION INITIALIZATION
```
Please read docs/SESSION_INIT.md and follow the complete initialization protocol it defines. After reading all required documents, provide me with:

1. Current repository status summary
2. Active data sprint phase and priorities
3. Available development paths for next work
4. Your understanding of the template compliance requirements
5. Recommended next action based on MVP critical path

Then await my specific instructions on which data development path you should pursue.

Be concise but thorough - I need to understand where we are and what our options are before directing your work.
```

### PROMPT 2: NEW TEMPLATE DEVELOPMENT (Ground Truth Process)
```
I want you specifically to work on beginning development (both migration and new development, once preexisting versions are ported over from the backup to the new template-following approach) of [CATEGORY].

MANDATORY PROCESS - Zero Tolerance for Skipping Steps:

1. **RESEARCH FIRST**: Use `node dev-tools/research-hl7-dictionary.js [category] [name]` BEFORE creating any template
   - Never start writing templates without understanding the official structure
   - Use the ground truth library data, not assumptions

2. **CREATE WITH TEMPLATE**: Follow _TEMPLATES/ patterns exactly based on research

3. **VALIDATE ALWAYS**: Use `node scripts/validate-against-hl7-dictionary.js [category] [name]` AFTER creating template
   - Fix any validation errors before proceeding
   - Zero tolerance for hallucinations or inaccuracies

4. **FOLLOW PRIORITY ORDER**: Use HL7_PRIORITY_ITEMS.md to structure your development path

Categories supported by library (80% coverage):
- datatypes ✅ (86 types)
- segments ✅ (140+ segments)
- tables ✅ (500+ tables)
- messages ✅ (50+ messages)
- triggerevents ❌ (manual HL7 docs required)

Let's get these [CATEGORY] going according to the ground truth library process and _TEMPLATES schema.
```

### PROMPT 3: EXISTING DATA VALIDATION (Bring to Truth)
```
I need you to validate and correct our existing data files using the ground truth library process.

MISSION: Bring all existing templates up to 100% accuracy using our validation tools.

CURRENT STATE: We have 31 existing data files that need validation:
- **7 datatypes**: ST, ID, NM, DT, TM, CX, XPN
- **5 segments**: MSH, PID, PV1, OBR, OBX
- **6 tables**: 0001, 0002, 0004, 0006, 0007, 0023
- **8 messages**: ADT_A01-A04, ADT_A08, ORM_O01, ORU_R01, RDE_O11
- **5 triggerevents**: A01, A03, A04, A08, R01

PROCESS:
1. **AUDIT EXISTING FILES**: Confirm current inventory:
   ```bash
   find pidgeon/data/standards/hl7v23/ -name "*.json" -not -path "*/_backup/*" -not -path "*/_TEMPLATES/*" | sort
   ```

2. **VALIDATE EACH FILE**: Run validation against library (80% coverage):
   ```bash
   # LIBRARY SUPPORTED (24 files):
   node scripts/validate-against-hl7-dictionary.js datatype ST
   node scripts/validate-against-hl7-dictionary.js datatype CX
   # (continue for all 7 datatypes)

   node scripts/validate-against-hl7-dictionary.js segment MSH
   node scripts/validate-against-hl7-dictionary.js segment PID
   # (continue for all 5 segments)

   node scripts/validate-against-hl7-dictionary.js table 1
   node scripts/validate-against-hl7-dictionary.js table 2
   # (continue for all 6 tables: 1, 2, 4, 6, 7, 23)

   node scripts/validate-against-hl7-dictionary.js message ADT_A01
   # (continue for all 8 messages)

   # MANUAL VALIDATION REQUIRED (5 triggerevents):
   # Reference HL7 v2.3 docs manually for A01, A03, A04, A08, R01
   ```

3. **RESEARCH AND CORRECT**: For any validation failures:
   - Research: `node dev-tools/research-hl7-dictionary.js [category] [name]`
   - Compare official structure vs our current template
   - Fix discrepancies to match library ground truth
   - Re-validate until 100% accurate

4. **REPORT RESULTS**: Document what you found and fixed

PRIORITY ORDER: Start with datatypes (foundation), then segments, then tables, then messages.

ZERO TOLERANCE: Every file must pass validation before moving to the next. No shortcuts, no "close enough" - perfect accuracy required.

Ready to audit and bring our existing data up to ground truth standards?
```

---

## 📋 **Expected Agent Response Format**

A properly initialized agent should respond with something like:

```
✅ Initialization Complete

📊 Repository Status:
- Phase: P0 MVP Data Foundation Critical Gap
- CLI Effectiveness: 40% (tables excellent, segment fields missing)
- Health: 85/100 (architecture solid, data gaps block progress)
- Build Status: Clean

🎯 Active Data Sprint:
- Status: Template-based migration in progress
- Backed up: 1,025 files in _backup/ directories
- Clean slate: All originals removed, working from templates
- Current focus: High-priority segments and tables per HL7_PRIORITY_ITEMS.md

🛣️ Available Development Paths:
1. **Tier 1 Core Segments** (MSH, PID, PV1, OBR, OBX) - Foundation blockers
2. **Tier 1 Universal Tables** (0001, 0002, 0004, 0005, 0006) - Referenced everywhere
3. **Tier 1 Core Data Types** (ST, ID, NM, CX, XPN) - Everything depends on these

📋 Template Compliance Understanding:
- MANDATORY: All data files must follow _TEMPLATES/ structure exactly
- Ground Truth Process: Research with library → Create → Validate with library
- Library Coverage: 80% complete (datatypes/segments/tables/messages supported)
- Zero Tolerance: No template creation without research, no validation failures allowed
- Tools: dev-tools/research-hl7-dictionary.js (before) + scripts/validate-against-hl7-dictionary.js (after)
- Progress: Delete backup file only after successful library validation

💡 Recommended Next Action:
Start with MSH segment (Message Header) - foundation for all messages, Tier 1 priority in HL7_PRIORITY_ITEMS.md.
MUST use ground truth process: research-hl7-dictionary.js → create template → validate-against-hl7-dictionary.js → fix issues → re-validate until perfect.

Ready for your specific work assignment.
```

---

## 🔧 **Troubleshooting Common Issues**

### **If Agent Doesn't Find SESSION_INIT.md**
The agent should look in `/docs/SESSION_INIT.md` from the project root. If not found, guide them to:
1. Check current directory with `pwd`
2. Navigate to project root if needed
3. List docs directory contents to confirm structure

### **If Agent Skips Required Reading**
Emphasize that the initialization protocol requires reading ALL listed documents in SESSION_INIT.md. No shortcuts allowed - each document provides critical context.

### **If Agent Doesn't Understand Data Sprint**
Point them specifically to:
- `docs/DATA_SPRINT.md` - Current sprint process
- `docs/HL7_LIBRARY_PROCESS.md` - MANDATORY ground truth workflow
- `docs/roadmap/data_integrity/HL7_PRIORITY_ITEMS.md` - Development priorities
- `pidgeon/data/standards/hl7v23/_TEMPLATES/` - Template patterns

### **If Agent Skips Library Research/Validation**
Emphasize the zero-tolerance policy:
- NEVER create templates without `node dev-tools/research-hl7-dictionary.js` first
- NEVER commit templates without `node scripts/validate-against-hl7-dictionary.js` passing
- Point them to `docs/HL7_LIBRARY_PROCESS.md` for complete workflow
- Remind them: 80% of data has library support - use it!

### **If Agent Wants to Start Work Immediately**
Remind them they must await your specific assignment after reporting status. The initialization is for understanding context, not beginning work.

---

## ✅ **Success Criteria**

The agent is properly initialized when they can:

- **Summarize current project status** accurately
- **Identify the active data sprint** phase and priorities
- **List available development paths** based on HL7_PRIORITY_ITEMS.md
- **Explain template compliance** requirements
- **Understand ground truth process** (research → create → validate workflow)
- **Know library coverage** (80% supported: datatypes/segments/tables/messages)
- **Commit to zero-tolerance policy** (no research skipping, no validation failures)
- **Recommend specific next actions** following MVP critical path
- **Await further instructions** rather than starting work

**Ready State**: Agent understands context and awaits your work assignment direction.

---

## 🎯 **Usage Notes**

- **Use at session start** for any new Claude Code instance
- **Paste exactly as written** - the prompt is optimized for consistent results
- **Wait for complete initialization** before giving work assignments
- **Verify understanding** through their status summary before proceeding

This prompt ensures every agent starts with full context and is ready for coordinated, high-quality data sprint work following our established processes and priorities.