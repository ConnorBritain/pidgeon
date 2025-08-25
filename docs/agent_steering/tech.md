# Technology Stack

## Platform & Runtime

- **.NET 8.0**: Latest LTS for performance and cross-platform support
- **C# 12**: Modern language features with nullable reference types
- **Cross-Platform**: Windows, macOS, Linux support via .NET

## Architecture Patterns

**Domain-Driven Design with Clean Architecture**:
- **Domain Layer**: Pure business logic, no dependencies
- **Application Layer**: Use cases and application services
- **Infrastructure Layer**: External integrations (databases, APIs)
- **Presentation Layer**: CLI, GUI, Web API

**Key Patterns**:
- **Plugin Architecture**: Standards as plugins
- **Dependency Injection**: Throughout entire solution
- **Result Pattern**: Explicit error handling without exceptions
- **Repository Pattern**: Data access abstraction
- **CQRS**: Separate read/write models where appropriate

## Core Libraries

### Standards Processing
- **System.Text.Json**: High-performance JSON for FHIR
- **System.Xml**: XML processing for NCPDP
- **Custom HL7 Parser**: Optimized for HL7 v2.x pipe-delimited format
- **Configuration Inference Engine**: Pattern recognition for vendor-specific implementations
- **Message Analysis Framework**: Statistical analysis of HL7 field patterns

### Dependency Injection
- **Microsoft.Extensions.DependencyInjection**: Core DI container
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.Logging**: Structured logging

### Testing
- **xUnit**: Test framework
- **FluentAssertions**: Readable test assertions
- **NSubstitute**: Mocking framework
- **BenchmarkDotNet**: Performance benchmarking

## User Interfaces

### CLI (Open Source)
- **System.CommandLine**: Modern CLI framework
- **Spectre.Console**: Rich console output with tables, progress bars

### Desktop GUI (Proprietary)
- **WPF** (Windows): Rich desktop experience
- **.NET MAUI** (Future): Cross-platform desktop
- **ModernWpfUI**: Modern Windows 11 styling
- **AvalonEdit**: Advanced text editor for messages

## Cloud & Services

### Azure Integration (Enterprise)
- **Azure Service Bus**: Message queuing
- **Azure Key Vault**: Secrets management
- **Azure Storage**: Message repository
- **Application Insights**: Monitoring and telemetry

### APIs
- **ASP.NET Core**: REST API framework
- **Minimal APIs**: Lightweight endpoints
- **OpenAPI/Swagger**: API documentation
- **API Versioning**: Version management

## AI Integration

### LLM Providers
- **OpenAI**: GPT-4 for mapping and documentation
- **Azure OpenAI**: Enterprise-grade AI services
- **Semantic Kernel**: AI orchestration framework

### AI Features
- **BYOK Support**: Bring Your Own Key for Professional tier
- **Token Management**: Usage tracking and limits
- **Prompt Engineering**: Optimized prompts for healthcare
- **Configuration Intelligence**: AI-powered analysis of vendor patterns
- **Anomaly Detection**: ML-based detection of unusual message patterns

## Data & Storage

### Local Storage
- **SQLite**: Local configuration and cache
- **File System**: Message files and configurations
- **JSON Configuration Store**: Vendor-specific configuration templates
- **Pattern Database**: Statistical patterns and confidence scores

### Enterprise Storage
- **SQL Server**: Enterprise message repository
- **Entity Framework Core**: ORM for data access
- **Redis**: Distributed caching

## Common Commands

### Development
```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run CLI
dotnet run --project src/Segmint.CLI -- generate --type ADT --format hl7

# Run benchmarks
dotnet run -c Release --project tests/Segmint.Performance.Tests

# Create release build
dotnet publish -c Release -r win-x64 --self-contained
```

### Docker (Future)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "Segmint.Cloud.dll"]
```

## Package Management

### NuGet Packages
```xml
<!-- Core dependencies -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.*" />
<PackageReference Include="System.CommandLine" Version="2.0.*" />
<PackageReference Include="Serilog" Version="3.1.*" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.6.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />

<!-- AI (Proprietary projects only) -->
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.*" />
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.*" />
```

## Performance Targets

### Message Processing
- **Parse HL7 message**: <5ms
- **Generate FHIR resource**: <10ms
- **Validate NCPDP message**: <15ms
- **Cross-standard transform**: <50ms
- **Configuration inference**: <100ms per message (batch analysis faster)
- **Vendor pattern matching**: <10ms

### Scalability
- **Concurrent users**: 1,000+ (Enterprise)
- **Messages per second**: 100+ (single instance)
- **Memory footprint**: <500MB (typical load)

## Security Considerations

### Code Security
- **No hardcoded secrets**: Use configuration
- **Input validation**: Prevent injection attacks
- **HIPAA compliance**: No PHI in logs

### Enterprise Security
- **SSO/SAML**: Enterprise authentication
- **RBAC**: Role-based access control
- **Audit logging**: Complete audit trail
- **Encryption**: Data at rest and in transit

## Development Tools

### Required
- **Visual Studio 2022** or **VS Code**
- **.NET 8 SDK**
- **Git**

### Recommended
- **Docker Desktop**: Container development
- **Azure CLI**: Cloud deployment
- **Postman**: API testing
- **SQL Server Developer**: Database development

## CI/CD Pipeline

### GitHub Actions
```yaml
name: Build and Test
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: dotnet build
    - run: dotnet test
```

### Release Process
1. Version tagging (semantic versioning)
2. Automated builds for multiple platforms
3. NuGet package publishing (Core library)
4. Installer generation (GUI application)

## Monitoring & Observability

### Logging
- **Serilog**: Structured logging
- **Console sink**: Development
- **File sink**: Production
- **Application Insights sink**: Enterprise

### Metrics
- **Performance counters**: Message throughput
- **Health checks**: Service availability
- **Custom metrics**: Business KPIs

This technology stack balances modern capabilities with enterprise requirements, ensuring Segmint can scale from individual developers to large healthcare organizations.