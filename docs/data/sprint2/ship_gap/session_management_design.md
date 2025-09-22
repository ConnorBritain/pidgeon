# Session Management Infrastructure Design
**Document Version**: 2.0
**Date**: September 21, 2025
**Status**: Design Approved - Ready for Implementation
**Priority**: Critical UX Fix for Quality Ship Sprint

---

## 🔍 **CURRENT PROBLEM ANALYSIS**

### **Progressive Disclosure Violation**
The current `--use-lock session_name` approach violates the core CLI principle of "make simple things simple":
- **Forces users to think about sessions** when they just want to generate related messages
- **Requires explicit session management** for what should be default workflow behavior
- **Creates "flag hell"** when users need consistent data across message types

### **Missing Mental Model**
Users lack a clear understanding of when sessions are active/inactive, leading to:
- **Forgotten session cleanup** resulting in orphaned sessions
- **Unclear state** - "Which session am I in?" confusion
- **Manual coordination** between commands instead of automatic flow

## 🎯 **SOLUTION: SMART SET WITH AUTO-SESSION MANAGEMENT**

### **Core Design Philosophy: "Simple Commands, Smart Behavior"**
The `set` command automatically manages sessions behind the scenes. Users focus on what they want (field values), not how it's managed (sessions). Default generation remains completely random until users set values.

### **1. DEFAULT BEHAVIOR: PURE RANDOMNESS (NO SESSIONS)**

```bash
# Default case - completely random, independent generations
pidgeon generate "ADT^A01"
# MSH|^~\&|PIDGEON|FACILITY|...
# PID|1||MR789123^^^PIDGEON^MR||Smith^John||19850315|M

pidgeon generate "ADT^A01"
# MSH|^~\&|PIDGEON|FACILITY|...
# PID|1||MR456789^^^PIDGEON^MR||Johnson^Sarah||19920722|F
# ↑ Completely different patient - no automatic continuity, no sessions
```

### **2. SMART SET WITH AUTO-SESSION CREATION**

```bash
# User sets a value - session created automatically
pidgeon set patient.mrn "JOHN123456"
# ✅ Created session: caring_crane_2025 (expires in 24h unless saved)
# ✅ Set patient.mrn = "JOHN123456"
# 💡 Continue setting values or generate messages with these locked fields

# Additional values added to existing session
pidgeon set patient.dob "1985-03-15"
# ✅ Set patient.dob = "1985-03-15" in session: caring_crane_2025

# Generation automatically uses current session
pidgeon generate "ADT^A01"
# PID|1||JOHN123456^^^PIDGEON^MR||Smith^John||19850315|M

pidgeon generate "ORU^R01"
# PID|1||JOHN123456^^^PIDGEON^MR||Smith^John||19850315|M
# ↑ Same locked values, random data for other fields
```

### **3. PROGRESSIVE FIELD SETTING**

```bash
# Build up a complete patient scenario incrementally
pidgeon set patient.mrn "JOHN123456"
# ✅ Created session: caring_crane_2025 (expires in 24h unless saved)
# ✅ Set patient.mrn = "JOHN123456"

pidgeon set patient.sex "M"
# ✅ Set patient.sex = "M" in session: caring_crane_2025

pidgeon set patient.dob "1985-03-15"
# ✅ Set patient.dob = "1985-03-15" in session: caring_crane_2025

# Now all generation uses this complete patient profile
pidgeon generate "ADT^A01"  # Uses all set values
pidgeon generate "ORU^R01"  # Same patient, same values
pidgeon generate "RDE^O11"  # Same patient, prescription order
```

### **4. SESSION PERSISTENCE & NAMING**

```bash
# User wants to save this temporary session permanently
pidgeon session save diabetes_patient
# ✅ Saved session: caring_crane_2025 → diabetes_patient
# ℹ️  Permanent sessions persist until manually deleted

# Later, switch between scenarios
pidgeon session use diabetes_patient
# ✅ Current session: diabetes_patient (3 fields set)
pidgeon generate "ADT^A01"  # Uses diabetes_patient values

# Or create new named session directly
pidgeon session create emergency_admission
# ✅ Created and switched to session: emergency_admission
pidgeon set patient.mrn "EMRG999888"
pidgeon set encounter.type "Emergency"
```

### **5. TEMPLATE IMPORT/EXPORT WORKFLOW**

```bash
# Import a pre-built scenario template
pidgeon session import epic_er_workflow.yaml
# ✅ Imported session: epic_er_workflow (12 fields loaded)
# ✅ Current session: epic_er_workflow

# Export current session for reuse
pidgeon session export my_test_scenario.yaml
# ✅ Exported session: caring_crane_2025 → my_test_scenario.yaml

# Template marketplace workflow (future)
pidgeon session download diabetes_mgmt_workflow
# ✅ Downloaded template: diabetes_mgmt_workflow (official)
# ✅ Current session: diabetes_mgmt_workflow
```

### **6. SESSION STATUS & VISIBILITY**

```bash
# Quick status check
pidgeon session
# Current session: caring_crane_2025 (3 values set, expires in 23h)
# Available sessions: diabetes_patient (permanent), emergency_admission (permanent)

# Show current session details
pidgeon session show
# 🔒 Session: caring_crane_2025
#    patient.mrn = "JOHN123456"
#    patient.sex = "M"
#    patient.dob = "1985-03-15"
#    Expires: 2025-09-22 14:23:45 (23h remaining)
#    💡 Save this session: pidgeon session save <name>

# Clear/reset current session
pidgeon session clear
# ✅ Cleared session. Next set command will create new session.
```

### **7. ERROR GUIDANCE & HELPER TEXT**

```bash
# Attempting to set without active session in new context
$ pidgeon set patient.mrn "MR999"
# ⚠️  No active session found
# ✅ Created session: happy_horse_2025 (expires in 24h unless saved)
# ✅ Set patient.mrn = "MR999"
# 💡 Continue setting values or generate messages with these locked fields

# Clear indication when switching contexts
$ pidgeon session use epic_testing
# ✅ Current session: epic_testing (5 fields set, permanent)
# 💡 Use 'pidgeon session show' to see all field values

# Helpful reminders about persistence
$ pidgeon session
# Current session: temp_session_123 (2 fields set, expires in 3h)
# ⚠️  Session expires soon! Save it: pidgeon session save <name>
```

## 🏗️ **TECHNICAL IMPLEMENTATION DESIGN**

### **Session Helper Architecture**
Following the `TemplateConfigHelper.cs` pattern:

```csharp
public class SessionHelper
{
    private readonly ILockStorageProvider _lockStorage;
    private readonly IHostEnvironment _environment;

    // Core session state management
    public async Task<string?> GetCurrentSessionAsync();
    public async Task SetCurrentSessionAsync(string sessionName);
    public async Task ClearCurrentSessionAsync();

    // Smart session creation for set command
    public async Task<string> GetOrCreateSessionAsync();
    public async Task<bool> IsTemporarySessionAsync(string sessionName);

    // Session lifecycle
    public async Task<string> CreateSessionAsync(string name, bool isTemporary = false);
    public async Task<string> SaveTemporarySessionAsync(string currentName, string newName);
    public async Task<bool> SessionExistsAsync(string name);
    public async Task DeleteSessionAsync(string name);

    // Cleanup
    public async Task CleanupExpiredSessionsAsync();
}
```

### **Enhanced SetCommand Implementation**

```csharp
public class SetCommand : CommandBuilderBase
{
    public async Task<int> ExecuteAsync(SetOptions options)
    {
        // Smart session management
        var currentSession = await _sessionHelper.GetCurrentSessionAsync();

        if (currentSession == null)
        {
            // Auto-create temporary session with friendly name
            var sessionName = GenerateFriendlyName(); // e.g., "caring_crane_2025"
            await _sessionHelper.CreateSessionAsync(sessionName, isTemporary: true);
            await _sessionHelper.SetCurrentSessionAsync(sessionName);

            Console.WriteLine($"✅ Created session: {sessionName} (expires in 24h unless saved)");
            currentSession = sessionName;
        }

        // Set the value
        var result = await _lockSessionService.SetValueAsync(
            currentSession,
            options.FieldPath,
            options.Value,
            options.Reason);

        if (result.IsSuccess)
        {
            Console.WriteLine($"✅ Set {options.FieldPath} = \"{options.Value}\" in session: {currentSession}");

            // Show helpful hints for new users
            if (await IsFirstValueInSessionAsync(currentSession))
            {
                Console.WriteLine("💡 Continue setting values or generate messages with these locked fields");
            }
        }

        return result.IsSuccess ? 0 : 1;
    }
}
```

### **Session State Persistence**
```json
// ~/.pidgeon/current_session.json
{
  "currentSession": "caring_crane_2025",
  "lastActivity": "2025-09-21T14:23:45Z",
  "isTemporary": true,
  "expiresAt": "2025-09-22T14:23:45Z"
}
```

### **Command Integration Pattern**
```csharp
// In GenerateCommand.cs
public class GenerateCommand : ICommand
{
    private readonly SessionHelper _sessionHelper;

    public async Task<int> ExecuteAsync(GenerateOptions options)
    {
        // 1. Determine session to use
        string? sessionName = await DetermineSessionAsync(options);

        // 2. Use session for generation (if exists)
        LockSession? lockSession = null;
        if (sessionName != null)
        {
            lockSession = await _lockService.GetSessionAsync(sessionName);
        }

        // 3. Generate with optional session context
        var result = await _generationService.GenerateAsync(options, lockSession);

        return result.IsSuccess ? 0 : 1;
    }

    private async Task<string?> DetermineSessionAsync(GenerateOptions options)
    {
        // Priority order:
        // 1. Explicit --session flag (power user override)
        // 2. Explicit --no-session flag (force random)
        // 3. Current session context (if exists)
        // 4. Pure random (no session)

        if (options.NoSession)
            return null;

        if (!string.IsNullOrEmpty(options.SessionName))
            return options.SessionName;

        return await _sessionHelper.GetCurrentSessionAsync();
    }
}
```

## 🔧 **COMMAND STRUCTURE REDESIGN**

### **Primary Command: Smart Set**
```bash
pidgeon set <field> <value>              # Auto-creates session if needed
pidgeon set <field> <value> --reason "text"  # Document why this value
pidgeon set --list                       # Show all values in current session
pidgeon set <field> --remove            # Remove a specific field from session
```

### **Session Management Commands**
```bash
pidgeon session                    # Show current session status
pidgeon session save <name>       # Save temporary session permanently
pidgeon session create <name>     # Create new named session and switch to it
pidgeon session use <name>        # Switch to existing session
pidgeon session list              # List all sessions
pidgeon session show [name]       # Show session details (current if no name)
pidgeon session clear             # Clear current session
pidgeon session remove <name>     # Remove specific session
pidgeon session export <file>     # Export session as template
pidgeon session import <file>     # Import session template
```

### **Updated Generate Command**
```bash
pidgeon generate "ADT^A01"                    # Uses current session if exists
pidgeon generate "ADT^A01" --session <name>   # Override to specific session
pidgeon generate "ADT^A01" --no-session       # Force no session (pure random)
```

### **Deprecated Commands**
```bash
# DEPRECATED - Will be removed in v2.0
pidgeon lock create <name>       # Use: pidgeon session create <name>
pidgeon lock list                # Use: pidgeon session list
pidgeon lock show <name>         # Use: pidgeon session show <name>
pidgeon lock remove <name>       # Use: pidgeon session remove <name>

# Legacy generate flag - shows deprecation warning
pidgeon generate "ADT^A01" --use-lock <name>
# ⚠️  --use-lock is deprecated. Use --session instead
```

## 📋 **USER EXPERIENCE FLOWS**

### **Flow 1: New User (Zero Configuration)**
```bash
$ pidgeon generate "ADT^A01"
# MSH|^~\&|PIDGEON|FACILITY|...
# PID|1||MR789123^^^PIDGEON^MR||Smith^John||19850315|M

$ pidgeon generate "ORU^R01"
# MSH|^~\&|PIDGEON|FACILITY|...
# PID|1||MR456789^^^PIDGEON^MR||Johnson^Sarah||19920722|F
# ↑ Completely different patient - pure randomness, no sessions
```

### **Flow 2: First Time User (Progressive Disclosure)**
```bash
$ pidgeon set patient.mrn "MR123456"
# ✅ Created session: caring_crane_2025 (expires in 24h unless saved)
# ✅ Set patient.mrn = "MR123456"
# 💡 Continue setting values or generate messages with these locked fields

$ pidgeon set patient.dob "1985-03-15"
# ✅ Set patient.dob = "1985-03-15" in session: caring_crane_2025

$ pidgeon generate "ADT^A01"
# Using session: caring_crane_2025 (2 fields set)
# MSH|^~\&|PIDGEON|FACILITY|...
# PID|1||MR123456^^^PIDGEON^MR||Smith^John||19850315|M

$ pidgeon session save my_test_patient
# ✅ Saved session: caring_crane_2025 → my_test_patient (now permanent)
```

### **Flow 3: Power User (Multiple Projects)**
```bash
$ pidgeon session create epic_testing
# ✅ Created and switched to session: epic_testing

$ pidgeon set patient.mrn "EPIC123456"
$ pidgeon set facility.id "EPIC_MAIN"

$ pidgeon session create cerner_testing
# ✅ Created and switched to session: cerner_testing

$ pidgeon set patient.mrn "CERNER789123"
$ pidgeon set facility.id "CERNER_WEST"

$ pidgeon session list
  epic_testing (2 fields)
→ cerner_testing (2 fields) [current]
  caring_crane_2025 (3 fields, expires in 22h)

$ pidgeon session use epic_testing
$ pidgeon generate "ADT^A01"     # Uses EPIC patient and facility
```

### **Flow 4: Template-Based Workflow**
```bash
$ pidgeon session import diabetes_workflow.yaml
# ✅ Imported session: diabetes_workflow (8 fields loaded)
# ✅ Current session: diabetes_workflow
#    patient.mrn = "DM_TEMPLATE"
#    patient.condition = "Diabetes Type 2"
#    medication.name = "Metformin"
#    ... (5 more fields)

$ pidgeon generate "ADT^A01"     # Admission
$ pidgeon generate "ORU^R01"     # Lab results
$ pidgeon generate "RDE^O11"     # Prescription

$ pidgeon session export my_diabetes_test.yaml
# ✅ Exported current session to: my_diabetes_test.yaml
```

## 🎯 **SUCCESS CRITERIA**

### **User Experience Goals**
- [ ] **Zero Configuration**: `pidgeon generate` works immediately without sessions
- [ ] **Smart Continuity**: `set` command auto-creates sessions transparently
- [ ] **Clear State**: Helper text always shows session status when relevant
- [ ] **No Flag Hell**: Common workflows require zero session-related flags
- [ ] **Progressive Disclosure**: Session complexity revealed only when needed
- [ ] **Power User Control**: Explicit session management available but optional

### **Technical Goals**
- [ ] **Clean Architecture**: `SessionHelper.cs` following established patterns
- [ ] **Backward Compatibility**: `lock` commands work with deprecation warnings
- [ ] **Performance**: Session lookup adds <10ms to command execution
- [ ] **Cleanup**: Temporary sessions cleaned up after 24 hours
- [ ] **Persistence**: Named sessions persist indefinitely until removed

## 🏭 **IMPLEMENTATION PLAN**

### **Phase 1: Core Infrastructure (Week 1)**
1. Create `SessionHelper.cs` service
2. Enhance `SetCommand.cs` with auto-session logic
3. Implement session state persistence
4. Add friendly name generation (adjective_animal_date)

### **Phase 2: Session Management (Week 1)**
1. Create `SessionCommand.cs` with subcommands
2. Implement save/create/use/list/show/clear/remove
3. Add import/export functionality
4. Update `GenerateCommand.cs` to use current session

### **Phase 3: Migration & Deprecation (Week 2)**
1. Add deprecation warnings to `LockCommand.cs`
2. Create migration guide for existing users
3. Update all documentation
4. Add comprehensive tests

### **Phase 4: Polish & UX (Week 2)**
1. Enhance helper text and error messages
2. Add session expiry warnings
3. Implement template marketplace hooks
4. Performance optimization

## ❓ **RESOLVED DESIGN QUESTIONS**

1. **Single verb confusion resolved**: `set` for everything, no `lock` command
2. **Auto-session creation**: Yes, with clear helper text
3. **Session switching**: Explicit via `session use` command
4. **Cross-terminal handling**: Current session per terminal context
5. **TTL**: 24 hours for temporary, indefinite for named sessions

---

**This design maximizes simplicity while preserving power. The `set` command becomes the workhorse with smart session management, while `session` commands provide explicit control when needed. The deprecation of `lock` eliminates verb confusion entirely.**