# Pidgeon Development Setup

## 🔧 Development Environment

This document outlines the development setup for working with Pidgeon CLI, including how to bypass Professional/Enterprise tier restrictions during development.

## 🚀 Quick Start

### Option 1: Development Scripts (Recommended)

We provide platform-specific development scripts that automatically enable all Pro/Enterprise features:

**Linux/macOS:**
```bash
./dev.sh --help                              # Run with development mode enabled
./dev.sh path list "ADT^A01"                 # Test path commands
./dev.sh workflow wizard --name "Test"       # Test Pro workflow features
./dev.sh diff file1.hl7 file2.hl7           # Test Pro diff features
```

**Windows (Batch):**
```cmd
.\dev.bat --help
.\dev.bat path list "ADT^A01"
.\dev.bat workflow wizard --name "Test"
```

**Windows (PowerShell):**
```powershell
.\dev.ps1 --help
.\dev.ps1 path list "ADT^A01"
.\dev.ps1 workflow wizard --name "Test"
```

### Option 2: Environment Variable

Set the development mode environment variable manually:

**Linux/macOS/PowerShell:**
```bash
export PIDGEON_DEV_MODE=true
dotnet run --project src/Pidgeon.CLI -- workflow wizard --name "Test"
```

**Windows (Command Prompt):**
```cmd
set PIDGEON_DEV_MODE=true
dotnet run --project src/Pidgeon.CLI -- workflow wizard --name "Test"
```

### Option 3: CLI Flag Override

Use the `--skip-pro-check` flag on individual commands:

```bash
dotnet run --project src/Pidgeon.CLI -- workflow wizard --skip-pro-check --name "Test"
dotnet run --project src/Pidgeon.CLI -- diff file1.hl7 file2.hl7 --skip-pro-check
```

## 🔒 Subscription Tier System

### Tier Structure

- **🆓 Free Tier**: Core CLI functionality (generate, validate, deident, path, session)
- **🔒 Professional Tier ($29/month)**: Workflow wizard, diff analysis, local AI, enhanced datasets
- **🏢 Enterprise Tier ($199/seat)**: Team workspaces, audit trails, private AI, unlimited usage

### Development Mode

When `PIDGEON_DEV_MODE=true` is set:
- All subscription tier checks are bypassed
- Professional and Enterprise features are unlocked
- Usage limits are ignored
- Warning messages indicate development mode is active

### Feature Gating Implementation

Pro/Enterprise features use the new `ProTierValidationService`:

```csharp
// Example from WorkflowCommand
var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
    FeatureFlags.WorkflowWizard, skipProCheck, cancellationToken);

if (validationResult.IsFailure)
{
    Console.WriteLine(validationResult.Error.Message);  // Shows upgrade message
    return 1;
}
```

### User-Friendly Upgrade Messages

When users hit tier restrictions, they see professional upgrade prompts:

```
┌─ Professional Feature Required ─────────────────────────────┐
│                                                             │
│  Workflow Wizard requires Professional subscription         │
│                                                             │
│  💰 Professional: $29/month                                │
│  ✅ Workflow Wizard                                         │
│  ✅ Diff + AI Analysis                                      │
│  ✅ Local AI Models                                         │
│  ✅ Enhanced Datasets                                       │
│  ✅ HTML Reports                                            │
│                                                             │
│  🚀 Upgrade: https://pidgeon.health/upgrade                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 🧪 Testing Pro/Enterprise Features

### Test Workflow Commands
```bash
./dev.sh workflow wizard --name "Integration Test"
./dev.sh workflow list
./dev.sh workflow run --name "Integration Test"
```

### Test Diff Analysis
```bash
./dev.sh diff samples/adt1.hl7 samples/adt2.hl7
./dev.sh diff samples/adt1.hl7 samples/adt2.hl7 --ai --report diff_report.html
```

### Test Path Commands (Free Tier)
```bash
./dev.sh path list "ADT^A01"
./dev.sh path resolve patient.mrn "ADT^A01"
./dev.sh path validate medication.dosage "ADT^A01"
./dev.sh path search "phone"
```

## 🏗️ Architecture Notes

### Subscription Service Architecture

- `ISubscriptionService`: Core subscription management interface
- `SubscriptionService`: Implementation with configuration-based tier detection
- `ProTierValidationService`: CLI-specific validation with user-friendly messages
- `FeatureFlags`: Enum defining all features across tiers
- `SubscriptionTier`: Free/Professional/Enterprise tier definitions

### Development Override Hierarchy

1. **Environment Variable**: `PIDGEON_DEV_MODE=true` (highest priority)
2. **CLI Flag**: `--skip-pro-check` (command-specific override)
3. **Configuration**: AI provider configured = Professional tier
4. **Default**: Free tier

### Service Registration

All subscription services are automatically registered via dependency injection:

```csharp
// In ServiceCollectionExtensions.cs
services.AddSubscriptionManagement();

// Registers:
// - ISubscriptionService -> SubscriptionService
// - ProTierValidationService (CLI-specific)
```

## 🔄 Future Enhancements

### Planned Improvements
- **Configuration Integration**: Detect subscription tier from user config
- **Usage Tracking**: Implement actual usage recording against storage
- **License Key Support**: File-based license validation for Enterprise
- **Team Workspace**: Multi-user collaboration features
- **Audit Trails**: Compliance reporting for Enterprise

### Development TODOs
- Update remaining commands (`LookupCommand`) to use new validation system
- Add tier indicators to all CLI help text
- Implement shell completion for semantic paths
- Add Progressive Web App (PWA) support for GUI frontend

## 📝 Development Guidelines

### Adding New Pro/Enterprise Features

1. **Define Feature Flag**: Add to `FeatureFlags` enum
2. **Update Tier Definitions**: Add to appropriate tier in `SubscriptionTier.cs`
3. **Add Validation**: Use `ProTierValidationService.ValidateFeatureAccessAsync()`
4. **Development Override**: Support `--skip-pro-check` flag
5. **Help Text**: Add tier indicator (🔒 Pro, 🏢 Enterprise)

### Testing Subscription Features

1. **Test Free Path**: Ensure feature blocks without subscription
2. **Test Development Override**: Verify `PIDGEON_DEV_MODE=true` bypasses
3. **Test CLI Override**: Verify `--skip-pro-check` flag works
4. **Test Upgrade Messages**: Ensure user-friendly upgrade prompts display

## 🎯 Command Reference

### Available Development Scripts

| Platform | Script | Purpose |
|----------|--------|---------|
| Linux/macOS | `./dev.sh` | Bash development runner |
| Windows | `.\dev.bat` | Batch development runner |
| Windows | `.\dev.ps1` | PowerShell development runner |

### Pro/Enterprise Commands

| Command | Tier | Feature Flag | Description |
|---------|------|--------------|-------------|
| `workflow wizard` | 🔒 Pro | `WorkflowWizard` | Interactive workflow creation |
| `workflow run` | 🔒 Pro | `WorkflowWizard` | Execute workflow scenarios |
| `diff` | 🔒 Pro | `DiffAnalysis` | Message comparison with AI analysis |
| `lookup --interactive` | 🔒 Pro | `LocalAIModels` | TUI browsing mode |

---

**Happy Developing! 🚀**

For questions about the subscription system or development setup, see the Sprint 2 strategy document at `docs/data/sprint2/sprint2_strat.md`.