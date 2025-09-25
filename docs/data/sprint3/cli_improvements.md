# CLI Improvements for Beta Launch

## Overview
Post-barrier removal improvements to enhance CLI usability and consistency based on user feedback.

## 1. Generation Mode Flags Rename

**Current**: `pidgeon generate --mode {local-ai | api-ai}`
**New**: `pidgeon generate --mode {model | api}`

### Rationale
- "local-ai" → "model" is clearer and shorter
- "api-ai" → "api" is more direct
- Aligns with industry conventions (model = local, api = remote)

### Implementation
- Update `GenerationModes` array in `HealthcareCompletions.cs`
- Update mode parsing logic in `GenerateCommand.cs`
- Update help text and documentation

## 2. Interactive Flag Shortening

**Current**: `pidgeon lookup --interactive`
**New**: `pidgeon lookup --inter`

### Rationale
- "interactive" is verbose for frequent usage
- "--inter" maintains clarity while improving ergonomics
- Consistent with common CLI patterns

### Implementation
- Update option definition in `LookupCommand.cs`
- Maintain backward compatibility if possible

## 3. Workflow Command Analysis

**Current Available Subcommands**:
- `pidgeon workflow wizard` - Interactive guided workflow creation
- `pidgeon workflow list` - List available workflows
- `pidgeon workflow run --name <workflow>` - Execute a workflow scenario

### Options Assessment
The workflow command has multiple subcommands, so keeping the structure makes sense. The main command acts as a namespace for workflow-related operations.

## 4. AI Command Rename: suggest-value → suggest

**Current**: `pidgeon ai suggest-value`
**New**: `pidgeon ai suggest`

### Rationale
- Shorter and more intuitive
- "suggest" is clear in the AI context
- Improves command ergonomics

### Implementation
- Update command creation in `AiCommand.cs`
- Update help text and examples

## 5. Universal Short Flag Implementation

### Current State Analysis
- Only `--dev` flag has short option (`-d`) in AI commands
- All other options lack short flags
- Inconsistent user experience

### Proposed Short Flag Standards

#### Core Generate Command
- `--count` → `-c`
- `--output` → `-o`
- `--format` → `-f`
- `--mode` → `-m`
- `--seed` → `-s`
- `--vendor` → `-v`

#### Lookup Command
- `--search` → `-s`
- `--standard` → `-t` (s-t-andard)
- `--format` → `-f`
- `--inter` → `-i`

#### Diff Command
- `--output` → `-o`
- `--format` → `-f`
- `--ignore` → `-i`
- `--severity` → `-s`

#### Validate Command
- `--file` → `-f`
- `--mode` → `-m`
- `--output` → `-o`

#### AI Command
- `--output` → `-o`
- `--model` → `-m`
- `--temperature` → `-t`

#### Session Commands
- `--session` → `-s`
- `--list` → `-l`
- `--remove` → `-r`

#### Set Command
- `--reason` → `-r`
- `--list` → `-l`
- `--remove` → `--rm` (avoid conflict with reason)
- `--session` → `-s`

### Short Flag Collision Resolution Strategy
When multiple options want the same short flag:
1. **Prioritize by frequency of use** (most common gets preferred letter)
2. **Use mnemonics** (--standard → -t for sTandard)
3. **Use alternative letters** (--remove → --rm for multi-char)
4. **Context-specific assignments** (different commands can reuse same short flags)

## 6. Deprecated Flag Cleanup

### Remove --dev Flags
**Target Commands**: AI commands currently using `--dev` or `-d`
**Action**: Remove completely since Pro barriers are lifted
**Reason**: No longer needed for beta launch approach

### Implementation Files to Update
- `AiCommand.cs` - Remove skipProCheck parameters and related logic
- `WorkflowCommand.cs` - Remove skip-pro-check flags
- `DiffCommand.cs` - Remove skip-pro-check flags

## Implementation Priority

### Phase 1: Core Command Improvements
1. ✅ **Generate command mode flags** - Most frequently used
2. ✅ **Lookup interactive flag** - User experience improvement
3. ✅ **AI suggest command rename** - Quick ergonomic win

### Phase 2: Universal Short Flags
1. ✅ **Generate command short flags** - High usage command
2. ✅ **Lookup command short flags** - Discovery-critical command
3. ✅ **Session/Set command short flags** - Workflow efficiency

### Phase 3: Cleanup
1. ✅ **Remove deprecated --dev flags** - Code cleanup
2. ✅ **Update help documentation** - Consistency
3. ✅ **Test all flag combinations** - Quality assurance

## Testing Strategy

### Manual Testing Checklist
- [ ] `pidgeon generate "ADT^A01" -c 5 -m model -o test.hl7`
- [ ] `pidgeon lookup PID.3 -f json --inter`
- [ ] `pidgeon ai suggest PID.5 -o suggested_name.txt`
- [ ] `pidgeon set patient.mrn "MR123" -r "test data" -s temp_session`
- [ ] Backward compatibility for existing long flags

### Automated Testing
- [ ] Update existing integration tests with new flag names
- [ ] Add short flag coverage to test suite
- [ ] Verify help text generation includes all short flags

## Documentation Updates Required

### Files to Update
- `docs/roadmap/CLI_REFERENCE.md` - Complete CLI documentation
- `README.md` - Update examples with new shorter commands
- `docs/user_stories/` - Update user story examples
- Help text generation in all affected commands

### Example Command Updates
**Before**:
```bash
pidgeon generate "ADT^A01" --count 10 --mode local-ai --output test.hl7
pidgeon lookup PID.3 --interactive --format json
pidgeon ai suggest-value PID.5 --output name.txt
```

**After**:
```bash
pidgeon generate "ADT^A01" -c 10 -m model -o test.hl7
pidgeon lookup PID.3 --inter -f json
pidgeon ai suggest PID.5 -o name.txt
```

## Success Metrics
- **Command Length Reduction**: Average command length reduced by 30-40%
- **User Experience**: More intuitive and faster to type
- **Consistency**: All major options have short flags
- **Backward Compatibility**: Existing long flags continue to work