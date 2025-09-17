# Claude Code Agent Initialization Prompt

**Purpose**: Reliable initialization prompt for new Claude Code instances to understand project context and be ready for productive data sprint work.

**Usage**: Copy and paste this prompt into a new Claude Code session to initialize an agent for Pidgeon development.

---

## 🚀 **INITIALIZATION PROMPT**

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
- Research: backup + HL7_PRIORITY_ITEMS.md + official HL7 standard + template
- Validation: Complete checklist from CLEANUP.md before completion
- Progress: Delete backup file only after successful validation

💡 Recommended Next Action:
Start with MSH segment (Message Header) - foundation for all messages, Tier 1 priority in HL7_PRIORITY_ITEMS.md. Follow DATA_SPRINT.md 4-step process.

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
- `docs/roadmap/data_integrity/HL7_PRIORITY_ITEMS.md` - Development priorities
- `pidgeon/data/standards/hl7v23/_TEMPLATES/` - Template patterns

### **If Agent Wants to Start Work Immediately**
Remind them they must await your specific assignment after reporting status. The initialization is for understanding context, not beginning work.

---

## ✅ **Success Criteria**

The agent is properly initialized when they can:

- **Summarize current project status** accurately
- **Identify the active data sprint** phase and priorities
- **List available development paths** based on HL7_PRIORITY_ITEMS.md
- **Explain template compliance** requirements
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