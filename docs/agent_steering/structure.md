# Project Structure

## Solution Organization

Segmint follows **Domain-Driven Design** with **Clean Architecture** principles, ensuring the core business logic remains independent of infrastructure concerns:

```
segmint/
├── src/
│   ├── Segmint.Core/              # Domain logic & standards engines (MPL 2.0)
│   ├── Segmint.CLI/               # Command-line interface (MPL 2.0)
│   ├── Segmint.GUI/               # Desktop application (Proprietary)
│   ├── Segmint.Cloud/             # Cloud services & API (Proprietary)
│   └── Segmint.AI/                # AI integration services (Proprietary)
├── tests/
│   ├── Segmint.Core.Tests/        # Behavior-driven domain tests
│   ├── Segmint.Integration.Tests/ # Cross-standard healthcare scenarios
│   └── Segmint.Performance.Tests/ # <50ms processing benchmarks
├── docs/
│   ├── agent_steering/            # AI agent guidance
│   └── founding_plan/             # Strategic documents
└── examples/                      # Sample messages & configs
```

## Project Responsibilities

### Segmint.Core (Open Source)
- **Purpose**: Universal healthcare standards engine
- **Contains**: Domain models, parsers, generators, validators
- **Architecture**: Plugin-based with standard adapters
- **Key Namespaces**:
  ```
  Domain/           # Patient, Medication, Provider models
  Standards/
    Common/        # IStandardAdapter, IValidator interfaces
    HL7/          # HL7 v2.x implementation
    FHIR/         # FHIR R4 implementation  
    NCPDP/        # NCPDP SCRIPT implementation
  Generation/      # Synthetic data generation
  Validation/      # Multi-level validation pipeline
  ```

### Segmint.CLI (Open Source)
- **Purpose**: Command-line interface for all operations
- **Contains**: Commands, console output, configuration
- **Pattern**: Command pattern with System.CommandLine
- **Key Commands**:
  ```bash
  segmint generate --type ADT --format hl7
  segmint validate --file message.hl7 --standard hl7v23
  segmint transform --from hl7 --to fhir --input file.hl7
  segmint config infer --samples ./messages/
  ```

### Segmint.GUI (Proprietary)
- **Purpose**: Professional desktop application
- **Contains**: WPF/MAUI views, visual designers, workflows
- **Architecture**: MVVM with dependency injection
- **Key Features**:
  - Message flow designer
  - Visual field mapping
  - Validation studio
  - Batch processing

### Segmint.Cloud (Proprietary)
- **Purpose**: Enterprise cloud services
- **Contains**: REST APIs, multi-tenancy, collaboration
- **Architecture**: ASP.NET Core with Azure integration
- **Services**:
  - Validation API
  - Message repository
  - Team workspaces
  - Audit logging

### Segmint.AI (Proprietary)
- **Purpose**: AI augmentation services
- **Contains**: LLM integration, mapping engine, chatbot
- **Pattern**: Strategy pattern for multiple AI providers
- **Features**:
  - Mapping suggestions
  - Documentation generation
  - Natural language queries
  - Standards chatbot

## Key Architectural Patterns

### Domain-Driven Design
```csharp
// Core domain models (standard-agnostic)
namespace Segmint.Core.Domain
{
    public record Patient(
        string Id,
        PersonName Name,
        DateTime BirthDate,
        Gender Gender
    );
    
    public record Prescription(
        Patient Patient,
        Medication Medication,
        Provider Prescriber,
        Instructions Dosage
    );
}
```

### Plugin Architecture
```csharp
// Standards are plugins
namespace Segmint.Core.Standards.Common
{
    public interface IStandardAdapter<TDomain>
    {
        string Generate(TDomain model, GenerationOptions options);
        TDomain Parse(string message);
        ValidationResult Validate(string message);
    }
}

// Each standard implements the adapter
namespace Segmint.Core.Standards.HL7
{
    public class HL7Adapter : IStandardAdapter<Prescription>
    {
        public string Generate(Prescription rx, GenerationOptions opt)
            => // Generate RDE message
    }
}
```

### Dependency Injection Throughout
```csharp
// No static classes, everything injectable
public class MessageService
{
    private readonly IValidationService _validator;
    private readonly IGenerationService _generator;
    private readonly ILogger<MessageService> _logger;
    
    public MessageService(
        IValidationService validator,
        IGenerationService generator,
        ILogger<MessageService> logger)
    {
        _validator = validator;
        _generator = generator;
        _logger = logger;
    }
}
```

### Result Pattern (No Exceptions for Control Flow)
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Error Error { get; }
    
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}

// Usage
public Result<HL7Message> GenerateMessage(Patient patient)
{
    if (patient == null)
        return Result<HL7Message>.Failure(Error.NullPatient);
        
    var message = BuildMessage(patient);
    return Result<HL7Message>.Success(message);
}
```

## Naming Conventions

### Projects
- **Segmint.{Component}**: Core components
- **Segmint.{Component}.Tests**: Test projects

### Namespaces
- **Domain**: Business entities
- **Standards**: Standard-specific implementations
- **Services**: Application services
- **Infrastructure**: External integrations

### Classes
- **Entities**: Patient, Medication, Provider
- **Services**: End with "Service" (ValidationService)
- **Adapters**: End with "Adapter" (HL7Adapter)
- **Factories**: End with "Factory" (MessageFactory)

### Interfaces
- **Prefix with "I"**: IValidator, IGenerator
- **Match implementation**: IValidator → Validator

## File Organization

### Core Library
```
Segmint.Core/
├── Domain/              # Business entities
│   ├── Patient.cs
│   ├── Medication.cs
│   └── Provider.cs
├── Standards/
│   ├── Common/         # Shared interfaces
│   ├── HL7/           # HL7 implementation
│   │   ├── v23/
│   │   ├── v251/
│   │   └── v27/
│   ├── FHIR/          # FHIR implementation
│   │   └── R4/
│   └── NCPDP/         # NCPDP implementation
│       └── Script2017071/
└── Services/          # Core services
```

### Testing Structure
*Following principles in `test-philosophy.md`*
```
tests/
├── Unit/              # Behavior-driven domain tests (95% coverage critical paths)
├── Integration/       # Healthcare workflow scenarios (25% of test suite)  
├── Performance/       # <50ms processing benchmarks (5% of test suite)
└── TestData/          # Anonymized real-world messages
    ├── Epic/          # Epic implementation samples
    ├── Cerner/        # Cerner implementation samples
    └── Scenarios/     # Healthcare workflow test data
```

## Documentation Structure

```
docs/
├── roadmap/                 # Development roadmap
│   └── INIT.md             # 90-day foundation with sacred principles
├── LEDGER.md               # Complete decision log and rollback procedures
├── agent_steering/         # AI agent guidance
│   ├── product.md         # Product vision
│   ├── structure.md       # This file
│   ├── tech.md           # Technology stack
│   ├── test-philosophy.md # Behavior-driven testing approach
│   └── agent-reflection.md# Decision framework
├── founding_plan/         # Strategic documents
│   ├── core_plus_strategy.md
│   ├── 1_founder.md
│   ├── 2_consultant.md
│   └── 3_investor.md
└── api/                  # API documentation
```

## Critical Design Principles

### 1. Domain First
Business logic NEVER depends on infrastructure. HL7/FHIR/NCPDP are just serialization formats for our domain models.

### 2. Plugin Architecture
New standards or message types are plugins, not core changes. Adding HL7 v2.7 should not touch v2.3 code.

### 3. Dependency Injection
No static classes. Everything testable. Configuration through DI container.

### 4. Result Pattern
No exceptions for control flow. Explicit error handling with Result<T>.

### 5. Open Core
Clear separation between open source (Core, CLI) and proprietary (GUI, Cloud, AI).

## Development Process Integration

### **Every Development Session Should:**
1. **Reference `docs/roadmap/INIT.md`** - Verify alignment with sacred principles
2. **Check `docs/LEDGER.md`** - Understand current architectural state and decisions
3. **Follow `test-philosophy.md`** - Write behavior-driven tests that describe healthcare scenarios
4. **Use `agent-reflection.md`** - Validate consistency after significant changes

### **Required Documentation Updates:**
- **LEDGER.md**: For any architectural decisions or breaking changes
- **test-philosophy.md**: If testing approaches evolve
- **INIT.md**: Only for exceptions to sacred principles (requires consensus)

This integrated structure ensures Segmint can scale from a single developer using the CLI to enterprise deployments with hundreds of interfaces, without architectural refactoring while maintaining complete traceability of all decisions.