# Segmint HL7 Generator - .NET Core Implementation

**Cross-Platform HL7 v2.3 Message Generation and Validation**

This is the modern .NET 8 implementation of Segmint HL7, providing superior performance, reliability, and cross-platform compatibility compared to the legacy Python version.

## 🏗️ Architecture Overview

### Project Structure

```
Segmint.sln
├── src/
│   ├── Segmint.Core/           # Core HL7 engine (MPL 2.0)
│   ├── Segmint.CLI/            # Command-line interface (MPL 2.0)
│   └── Segmint.GUI/            # Professional desktop GUI (Proprietary)
├── tests/
│   └── Segmint.Tests/          # Comprehensive test suite
└── docs/                       # .NET-specific documentation
```

### Technology Stack

- **.NET 8** - Latest LTS version for maximum performance and compatibility
- **C# 12** - Modern language features with nullable reference types
- **System.CommandLine** - Modern CLI framework for the command-line interface
- **WPF + ModernWpfUI** - Professional desktop GUI with modern styling
- **xUnit + FluentAssertions** - Comprehensive testing framework
- **System.Text.Json** - High-performance JSON serialization

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (latest version)
- Visual Studio 2022 17.8+ or VS Code with C# extension
- Git for version control

### Building the Solution

```bash
# Clone and navigate to the .NET solution
cd segmint

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run --project src/Segmint.CLI -- --help
```

### Development Environment Setup

**Visual Studio 2022:**
1. Open `Segmint.sln`
2. Set `Segmint.CLI` as startup project for command-line development
3. Set `Segmint.GUI` as startup project for GUI development

**VS Code:**
1. Install the C# extension
2. Open the `segmint` folder
3. Use integrated terminal for `dotnet` commands

## 📦 Project Components

### Segmint.Core (Open Source - MPL 2.0)

**Core HL7 Processing Engine**

```csharp
// Type-safe HL7 field handling
var patientName = new StringField("Doe^John^M", maxLength: 250, isRequired: true);
var gender = IdentifierField.Gender("M", isRequired: true);

// Segment creation and validation
var pidSegment = new PIDSegment();
pidSegment.SetPatientName(patientName);
pidSegment.SetGender(gender);

// Message assembly
var message = new RDEMessage();
message.AddSegment(pidSegment);
string hl7Output = message.ToHL7String();
```

**Key Features:**
- Type-safe HL7 field system with validation
- Comprehensive segment and message classes
- Configuration-driven message generation
- Multi-level validation (syntax, semantic, clinical)
- JSON-based configuration management
- Synthetic data generation capabilities

### Segmint.CLI (Open Source - MPL 2.0)

**Professional Command-Line Interface**

```bash
# Generate HL7 messages
segmint generate --type RDE --output ./messages/

# Validate existing messages
segmint validate input.hl7 --levels syntax semantic clinical

# Analyze and infer configurations
segmint analyze samples.hl7 --output config.json

# Configuration management
segmint config compare old.json new.json
segmint config validate myconfig.json
```

**Features:**
- Intuitive command structure with rich help
- Batch processing capabilities
- Multiple output formats (HL7, JSON, XML)
- Configuration validation and management
- Cross-platform single-file deployment

### Segmint.GUI (Proprietary - Commercial License)

**Professional Desktop Application**

> **Note:** The GUI component is proprietary software requiring a separate commercial license. See [STRATEGY.md](../docs/STRATEGY.md) for licensing details.

**Features:**
- Modern WPF interface with healthcare-optimized themes
- Visual message construction and validation
- Configuration management with diff visualization
- Real-time validation feedback
- Batch processing with progress tracking
- Cloud integration for enhanced AI features

## 🔧 Development Guidelines

### Code Standards

- **Nullable Reference Types**: Enabled project-wide for better null safety
- **Treat Warnings as Errors**: All warnings must be resolved
- **XML Documentation**: All public APIs must be documented
- **Modern C# Features**: Use latest language features appropriately
- **Performance**: Optimize for healthcare interface processing volumes

### Testing Requirements

- **Minimum 90% code coverage** for Segmint.Core
- **Unit tests** for all public APIs and business logic
- **Integration tests** for HL7 message processing workflows
- **Performance tests** for high-volume scenarios
- **Cross-platform validation** on Windows, macOS, and Linux

### Healthcare Compliance

- **No PHI processing**: All data must be synthetic
- **HIPAA awareness**: Follow healthcare data handling best practices
- **Audit logging**: Track all configuration and generation activities
- **Validation integrity**: Ensure clinical accuracy in synthetic data

## 🎯 Performance Targets

The .NET implementation targets significant performance improvements over the Python version:

- **10x faster** HL7 message processing
- **5x lower** memory usage
- **Sub-100ms** startup time for CLI operations
- **Native performance** on all supported platforms

## 🔄 Migration from Python

### Compatibility

The .NET version maintains **100% compatibility** with Python-generated configurations and HL7 messages while providing enhanced capabilities:

- **Same configuration format**: JSON configs are fully compatible
- **Identical output**: Generated HL7 messages match Python version exactly
- **Enhanced validation**: Additional validation layers not possible in Python
- **Better performance**: Significantly faster processing for large volumes

### Migration Path

1. **Phase 1**: Use .NET CLI alongside existing Python GUI
2. **Phase 2**: Migrate to .NET GUI for enhanced features
3. **Phase 3**: Deprecate Python version (post-migration validation)

## 📚 Documentation

- **[API Reference](docs/API_REFERENCE.md)** - Complete .NET API documentation
- **[Configuration Guide](docs/CONFIGURATION.md)** - HL7 interface configuration
- **[Performance Guide](docs/PERFORMANCE.md)** - Optimization best practices
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment strategies

## 🛠️ Build and Deployment

### Development Builds

```bash
# Debug build with full symbols
dotnet build --configuration Debug

# Release build optimized for performance
dotnet build --configuration Release
```

### Production Deployment

```bash
# Self-contained CLI executable
dotnet publish src/Segmint.CLI --configuration Release --runtime win-x64 --self-contained

# Cross-platform deployment
dotnet publish src/Segmint.CLI --configuration Release --runtime linux-x64 --self-contained
dotnet publish src/Segmint.CLI --configuration Release --runtime osx-x64 --self-contained
```

### NuGet Packages

The core library will be available as NuGet packages for integration into other healthcare applications:

```xml
<PackageReference Include="Segmint.Core" Version="2.0.0" />
```

## 🤝 Contributing

Contributions to the open source components (Core and CLI) are welcome! Please see:

- **[CONTRIBUTING.md](../docs/CONTRIBUTING.md)** - Contribution guidelines
- **[MPL 2.0 License](../docs/LICENSE.md)** - Licensing requirements
- **[Code of Conduct](docs/CODE_OF_CONDUCT.md)** - Community standards

## 📄 Licensing

- **Segmint.Core & Segmint.CLI**: Mozilla Public License 2.0 (Open Source)
- **Segmint.GUI**: Proprietary commercial license required
- **Third-party dependencies**: Various permissive licenses (MIT, Apache 2.0, BSD)

---

**Built for the Healthcare Community** 🏥

*Empowering healthcare developers with robust, performant HL7 testing tools.*