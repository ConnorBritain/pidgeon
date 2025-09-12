# Shell Completion for Pidgeon CLI

## Overview
Pidgeon CLI uses System.CommandLine which provides built-in support for tab completion in bash, zsh, PowerShell, and fish shells. This enables users to tab-complete commands, options, and file paths for a smoother CLI experience.

## What Gets Auto-Completed

### Built-in Completions (No Configuration Needed)
- **Commands**: `pidgeon gen[TAB]` → `pidgeon generate`
- **Subcommands**: `pidgeon workflow w[TAB]` → `pidgeon workflow wizard`
- **Options**: `pidgeon diff --ig[TAB]` → `pidgeon diff --ignore`
- **File paths**: `pidgeon diff msg[TAB]` → Lists all files starting with "msg"
- **Directories**: `pidgeon diff ./dev/[TAB]` → Lists subdirectories
- **Enum values**: `pidgeon diff --severity w[TAB]` → `warn`

### Examples in Action
```bash
# Command completion
pidgeon d[TAB]              # → deident, diff

# File path completion  
pidgeon diff old[TAB]        # → old.hl7, old_backup.hl7
pidgeon diff msg.hl7 new[TAB] # → new.hl7, newer.hl7

# Option completion
pidgeon generate --fo[TAB]   # → --format
pidgeon diff --sev[TAB]      # → --severity

# Enum value completion
pidgeon diff --severity h[TAB]  # → hint
pidgeon generate --format j[TAB] # → json
```

## Enabling Shell Completion

### Bash
```bash
# One-time setup: Add to ~/.bashrc
echo 'source <(pidgeon completions bash)' >> ~/.bashrc
source ~/.bashrc

# Or for current session only
source <(pidgeon completions bash)
```

### Zsh
```bash
# One-time setup: Create completion file
pidgeon completions zsh > ~/.zsh/completions/_pidgeon

# Ensure completions directory is in fpath (add to ~/.zshrc if needed)
fpath=(~/.zsh/completions $fpath)
autoload -U compinit && compinit
```

### PowerShell
```powershell
# For current session
pidgeon completions powershell | Out-String | Invoke-Expression

# For permanent setup: Add to $PROFILE
Add-Content $PROFILE "`npidgeon completions powershell | Out-String | Invoke-Expression"
```

### Fish
```fish
# One-time setup
pidgeon completions fish > ~/.config/fish/completions/pidgeon.fish
```

## How It Works Technically

System.CommandLine automatically provides completions based on:

1. **Argument/Option Types**:
   - `Argument<string>` with file system paths → file/directory completion
   - `Option<T>` where T is an enum → enum value completion
   - `Option<bool>` → true/false completion

2. **Arity Settings**:
   - `ArgumentArity.ZeroOrMore` → allows multiple file selections
   - `ArgumentArity.ExactlyOne` → stops after one selection

3. **Custom Completions** (when implemented):
   ```csharp
   argument.AddCompletions(context => new[] {
       "ADT^A01", "ADT^A04", "ORU^R01"
   });
   ```

## Current Pidgeon Commands with Completion

### `pidgeon diff`
```bash
pidgeon diff [TAB]                    # Lists files in current directory
pidgeon diff old.hl7 [TAB]            # Lists files for second argument
pidgeon diff --report [TAB]           # Suggests .html/.json files
pidgeon diff --ignore MSH-7,P[TAB]    # No completion (string input)
```

### `pidgeon generate`
```bash
pidgeon generate ADT[TAB]             # No completion yet (could add message types)
pidgeon generate --output [TAB]       # File path completion
pidgeon generate --format [TAB]       # auto, hl7, json, ndjson
```

### `pidgeon validate`
```bash
pidgeon validate --file [TAB]         # Lists .hl7/.json files
pidgeon validate --mode [TAB]         # strict, compatibility
```

## Future Enhancements

### Healthcare-Specific Completions
We could add custom completion providers for:

1. **Message Types**: 
   - HL7: ADT^A01, ADT^A04, ORM^O01, ORU^R01
   - FHIR: Patient, Encounter, Observation
   - NCPDP: NewRx, Refill, Cancel

2. **Common Field Paths** (for --ignore):
   - MSH-7 (timestamp)
   - PID-5 (patient name)
   - PV1-44 (admit date)

3. **Vendor Patterns**:
   - epic_er, cerner_lab, meditech_pharm

4. **Workflow Names**:
   - List saved workflows from ~/.pidgeon/workflows/

### Implementation Example
```csharp
// In DiffCommand.cs
var ignoreOption = new Option<string[]>("--ignore")
    .AddCompletions(ctx => new[] {
        "MSH-7",    // Timestamp
        "MSH-10",   // Message Control ID  
        "PID-3",    // Patient ID
        "PID-5",    // Patient Name
        "PV1-2",    // Patient Class
        "PV1-44"    // Admit Date/Time
    });
```

## Testing Completion

1. Enable completion for your shell (see above)
2. Navigate to a directory with test files:
   ```bash
   cd ~/pidgeon-test-data/
   ls
   # patient1.hl7  patient2.hl7  lab_results.hl7  
   ```
3. Test completion:
   ```bash
   pidgeon diff pat[TAB]     # Should list patient1.hl7, patient2.hl7
   pidgeon diff patient1.hl7 lab[TAB]  # Should complete to lab_results.hl7
   ```

## Troubleshooting

### Completion Not Working
1. **Check shell version**: Modern bash (4.4+), zsh (5.0+), PowerShell (5.0+)
2. **Verify installation**: Run `pidgeon completions [shell]` manually
3. **Reload shell**: `source ~/.bashrc` or start new terminal
4. **Check PATH**: Ensure pidgeon is in PATH

### Slow Completion
- File system completion can be slow on network drives
- Consider limiting search depth with more specific paths

## Developer Notes

- System.CommandLine handles completion infrastructure
- No additional packages needed
- Completions work with both `dotnet run` and compiled executable
- Custom completions are evaluated at runtime (can be dynamic)

## References
- [System.CommandLine Tab Completion](https://github.com/dotnet/command-line-api/blob/main/docs/Features/tab-completion.md)
- [Shell Completion Best Practices](https://github.com/dotnet/command-line-api/wiki/Shell-Completion-Best-Practices)