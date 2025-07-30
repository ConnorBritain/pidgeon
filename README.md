# Segmint Universal Healthcare Standards Platform

> **Production-Ready HL7 v2.3 Engine** + **Future Multi-Standard Platform** for FHIR, NCPDP XML, and Integrated Healthcare Documentation

[![.NET 8](https://img.shields.io/badge/.NET-8-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://opensource.org/licenses/MPL-2.0)
[![Cross Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/dotnet/core)

---

## 🎯 **Vision: Universal Healthcare Standards Platform**

Segmint is evolving into the **Universal Healthcare Standards Platform** - a comprehensive solution for HL7, FHIR, NCPDP XML message generation, validation, and integrated healthcare documentation. We're taking a **phased approach**: complete HL7 commercialization first, then systematically expand to other healthcare standards.

### **Current Status: HL7 v2.3 Production Ready ✅**
- **Complete HL7 v2.3 Implementation**: Production-ready .NET 8 engine
- **182+ Tests Passing**: Comprehensive validation and message generation
- **Modern CLI Interface**: Full command set (generate, validate, analyze, config)
- **Cross-Platform**: Windows, macOS, Linux support
- **Healthcare-Grade Validation**: Multi-level compliance checking

### **Future Vision: Multi-Standard Platform 🔮**
- **FHIR R4+ Support**: Complete FHIR resource handling and validation
- **NCPDP XML Integration**: E-prescribing and pharmacy workflow support
- **Healthcare Documentation Hub**: AI-powered standards knowledge base
- **Cross-Standard Transformation**: Universal message mapping and conversion

---

## 🚀 **Quick Start**

### **Installation**
```bash
# Clone repository
git clone https://github.com/yourusername/segmint.git
cd segmint/segmint

# Build and run
dotnet build
dotnet run --project src/Segmint.CLI -- --help
```

### **Generate Your First HL7 Message**
```bash
# Generate a pharmacy order (RDE) message
dotnet run --project src/Segmint.CLI -- generate --type RDE --output my-message.hl7

# Validate an existing HL7 message  
dotnet run --project src/Segmint.CLI -- validate input-message.hl7

# Analyze messages to infer configuration
dotnet run --project src/Segmint.CLI -- analyze sample-messages/ --output vendor-config.json
```

### **Example Output**
```
MSH|^~\&|SEGMINT|FACILITY|TARGET|DESTINATION|20250716143022||RDE^O11^RDE_O11|MSG123456|P|2.3|||NE|NE|US
EVN||20250716143022|||PROVIDER123
PID|1||PATIENT123^^^FACILITY^MR||DOE^JOHN^MICHAEL||19800515|M|||123 MAIN ST^^ANYTOWN^ST^12345^USA
ORC|NW|ORDER123|||||^^^20250716143022||20250716143022|PROVIDER123
RXE|1^20250716143022~20250716143022|RXCUI123^MEDICATION NAME^RXN|100||MG||||||||||||||||20250716
```

---

## 📋 **Current Capabilities (HL7 v2.3)**

### **✅ Complete Message Generation**
- **RDE Messages**: Pharmacy orders with comprehensive field support
- **ADT Messages**: Patient admit/discharge/transfer workflows  
- **ACK Messages**: Acknowledgments with detailed error handling
- **Custom Z-Segments**: Pharmacy-specific extensions (ZPM, ZMA, ZDG)

### **✅ Advanced Validation System**  
- **Syntax Validation**: HL7 format compliance and structure
- **Semantic Validation**: Field requirements, data types, cardinality
- **Clinical Validation**: Medical appropriateness and code validity
- **Configuration Validation**: Vendor-specific rules and constraints

### **✅ Configuration Management**
- **Automatic Inference**: Analyze HL7 messages to generate configurations
- **Version Control**: Track configuration changes over time
- **Diff Analysis**: Compare configurations to detect vendor changes
- **Lock/Unlock System**: Prevent accidental production changes

### **✅ Modern CLI Interface**
```bash
segmint generate    # Generate HL7 messages from configurations
segmint validate    # Validate messages with multiple validation levels  
segmint analyze     # Infer configurations from sample messages
segmint config      # Manage configuration files and templates
```

---

## 🏗️ **Architecture: Built for Multi-Standard Future**

### **Universal Abstractions**
```csharp
// Common interfaces for all healthcare standards
IStandardMessage    // HL7, FHIR, NCPDP message interface
IStandardField      // Universal field behaviors  
IStandardValidator  // Pluggable validation system
IStandardConfig     // Unified configuration management
```

### **Current HL7 Implementation**
```
Segmint.Core/
├── Standards/
│   ├── Common/              # Universal healthcare abstractions ✅
│   └── HL7/v23/            # Complete HL7 v2.3 implementation ✅
│       ├── Types/          # All HL7 field types (ST, ID, TS, CE, XPN, etc.)
│       ├── Segments/       # Standard segments (MSH, PID, EVN, ORC, RXE, etc.)
│       ├── Messages/       # Message implementations (RDE, ADT, ACK)
│       └── Validation/     # Multi-level HL7 validation
├── Configuration/          # Unified configuration system ✅
├── Validation/            # Cross-standard validation pipeline ✅
└── Export/                # Universal export capabilities ✅
```

### **Future Multi-Standard Architecture**
```
Standards/
├── FHIR/R4/               # FHIR R4 resources and validation 📅
├── NCPDP/XML/             # NCPDP XML e-prescribing support 📅
Documentation/
├── Repository/            # Healthcare standards knowledge base 📅
├── Search/               # AI-powered documentation search 📅
└── API/                  # Documentation access API 📅
```

---

## 📊 **Roadmap: Phased Multi-Standard Expansion**

### **Phase 4: HL7 Commercialization (Current - Q3 2025)**
- ✅ **Production HL7 Engine**: Complete v2.3 implementation
- 🔄 **JSON Serialization Fix**: Resolve CLI output format issues
- 📋 **Advanced Field Types**: CompositeQuantityField, Insurance segments
- 📋 **Commercial GUI**: Professional desktop application
- 📋 **Performance Optimization**: Enterprise-scale benchmarking

### **Phase 5: FHIR Integration (Q1 2026)**  
- 📅 **FHIR R4 Support**: Complete resource implementation
- 📅 **HL7 ↔ FHIR Transformation**: Cross-standard message mapping
- 📅 **Terminology Services**: ValueSet and CodeSystem integration
- 📅 **JSON/XML Support**: Dual format FHIR processing

### **Phase 6: NCPDP Platform (Q2 2026)**
- 📅 **NCPDP XML Support**: SCRIPT and e-prescribing workflows
- 📅 **Pharmacy Integration**: Drug databases, formulary validation
- 📅 **Unified Workflows**: HL7 + FHIR + NCPDP testing scenarios
- 📅 **Regulatory Compliance**: DEA, FDA, pharmacy regulations

### **Phase 7: Documentation Hub (Q3 2026)**
- 📅 **Healthcare Documentation Repository**: Complete standards knowledge base
- 📅 **AI-Powered Chatbot**: Natural language healthcare standards queries
- 📅 **CLI Documentation Access**: `segmint docs search "patient identifier"`
- 📅 **Community Platform**: User contributions and vendor patterns

---

## 💼 **Business Model: Open Core + Multi-Standard Premium**

### **Open Source Foundation (MPL 2.0)**
- **Complete HL7 v2.3 Engine**: Production-ready message generation and validation
- **CLI Tools**: Full command-line interface for all core operations  
- **Configuration Management**: Advanced configuration inference and management
- **Cross-Platform Support**: Windows, macOS, Linux compatibility

### **Commercial Premium Features**
- **Professional GUI**: Desktop application for visual message building
- **Universal Standards**: FHIR and NCPDP plugins with cross-standard features
- **Documentation Hub**: AI-powered healthcare standards documentation access
- **Enterprise Support**: Priority support, training, and custom implementations

### **Pricing Strategy**
- **Professional License**: $299 one-time (HL7 GUI application)
- **Universal License**: $499 one-time (All standards + documentation)  
- **Enterprise Subscription**: $199/month/seat (Full platform + cloud features)
- **Documentation Hub**: $149/month/organization (AI chatbot + API access)

---

## 🤝 **Contributing**

We welcome contributions to the **Universal Healthcare Standards Platform**! 

### **Current Focus Areas**
- **HL7 Enhancement**: Additional field types, segments, and message types
- **Performance Optimization**: Benchmarking and enterprise-scale improvements  
- **Documentation**: API documentation, usage guides, implementation examples
- **Testing**: Additional test coverage for edge cases and real-world scenarios

### **Future Contribution Opportunities**
- **FHIR Implementation**: Help design and implement FHIR R4 support
- **NCPDP Integration**: Pharmacy domain expertise for NCPDP XML implementation
- **Documentation Platform**: Healthcare standards knowledge base development
- **Cross-Standard Features**: Message transformation and validation capabilities

### **Getting Started**
1. **Fork the repository** and create a feature branch
2. **Run tests**: `dotnet test` to ensure everything works
3. **Make your changes** following our coding standards
4. **Add tests** for new functionality
5. **Submit a pull request** with a clear description

---

## 📄 **License**

**Open Core Model**: [Mozilla Public License 2.0](LICENSE)

- **Open Source**: Core HL7 engine, CLI tools, configuration management
- **Commercial**: GUI applications, multi-standard features, documentation platform
- **Enterprise**: Custom licensing available for large organizations

---

## 🔗 **Links**

- **Documentation**: [Full documentation and guides](docs/)
- **Roadmap**: [Detailed development roadmap](docs/ROADMAP.md)  
- **Architecture**: [Technical architecture overview](docs/ARCHITECTURE.md)
- **Strategy**: [Business strategy and vision](docs/STRATEGY.md)
- **Contributing**: [Contribution guidelines](CONTRIBUTING.md)
- **Changelog**: [Version history and changes](CHANGELOG.md)

---

## 📞 **Support**

- **Community Support**: [GitHub Issues](https://github.com/yourusername/segmint/issues)
- **Documentation**: [User guides and API docs](docs/)
- **Professional Support**: Available with commercial licenses
- **Training**: Healthcare standards training and implementation consulting

---

**Build the Future of Healthcare Data Exchange** 🚀

*Segmint: From HL7 Excellence to Universal Healthcare Standards Platform*