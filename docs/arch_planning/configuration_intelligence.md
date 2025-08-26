# Configuration Intelligence Architecture

**Document Version**: 1.0  
**Date**: August 26, 2025  
**Status**: Architectural Planning - Pre-Implementation  
**Scope**: Multi-standard, multi-vendor configuration inference system

---

## 🎯 **Real-World Use Cases Driving Design**

### **Use Case 1: Multi-Standard Vendor (CorEMR)**
- **HL7 Interface**: CorEMR sends ADT^A01, ORM^O01 via HL7 v2.3
- **NCPDP Interface**: Same CorEMR sends NewRx, Census via SCRIPT 2017071
- **Requirement**: Two separate configs for same vendor, different standards
- **Config IDs**: `CorEMR-HL7-v2.3` and `CorEMR-NCPDP-SCRIPT`

### **Use Case 2: Message-Specific Patterns (Epic)**
- **ADT Messages**: Epic ADT^A01 has specific PID.3 patterns, MSH.3 = "EpicADT"  
- **ORM Messages**: Epic ORM^O01 has different MSH.3 = "EpicOrders", different ORC patterns
- **Requirement**: Can't infer complete Epic config from single ADT message
- **Solution**: Incremental config building as new message types are analyzed

### **Use Case 3: Interface Evolution**
- **Epic 2023.1**: Uses MSH.3 = "EpicMyChart"
- **Epic 2024.2**: Updates to MSH.3 = "EpicMyChart2024", adds Z-segments  
- **Requirement**: Detect config drift, versioned configurations
- **Solution**: Config diffing and evolution tracking

---

## 🏗️ **Hierarchical Configuration Model**

### **Three-Tier Hierarchy**
```
VENDOR -> STANDARD -> MESSAGE_TYPE
   ↓         ↓           ↓
CorEMR -> HL7v23 -> ADT^A01
       -> HL7v23 -> ORM^O01  
       -> SCRIPT -> NewRx
       -> SCRIPT -> Census
```

### **Configuration Addressing**
```csharp
public record ConfigurationAddress(
    string Vendor,      // "CorEMR", "Epic", "Cerner"
    string Standard,    // "HL7v23", "FHIRv4", "NCPDP-SCRIPT"
    string MessageType  // "ADT^A01", "Patient", "NewRx"
);

// Examples:
// CorEMR-HL7v23-ADT^A01
// Epic-HL7v23-ORM^O01  
// SureScripts-NCPDP-NewRx
// Epic-FHIRv4-Patient
```

---

## 🧩 **Multi-Standard Architecture**

### **Plugin-Based Standard Support**
Following INIT.md Plugin Architecture principles:

```csharp
// Standard-agnostic inference interface
public interface IConfigurationInferenceService
{
    Result<VendorConfiguration> AnalyzeMessages(
        IEnumerable<string> messages,
        ConfigurationAddress address);
        
    Result<VendorConfiguration> MergeConfigurations(
        VendorConfiguration existing,
        VendorConfiguration newData);
}

// Standard-specific implementations
public interface IHL7InferencePlugin : IConfigurationInferenceService { }
public interface IFHIRInferencePlugin : IConfigurationInferenceService { }  
public interface INCPDPInferencePlugin : IConfigurationInferenceService { }
```

### **Standard-Agnostic Base Types**
```csharp
public record VendorConfiguration
{
    public ConfigurationAddress Address { get; init; }
    public VendorSignature Signature { get; init; }
    public IStandardPatterns Patterns { get; init; }  // Standard-specific
    public ConfigurationMetadata Metadata { get; init; }
}

public record VendorSignature  
{
    public string DetectedVendor { get; init; }
    public double Confidence { get; init; }
    public Dictionary<string, string> SignatureFields { get; init; }  // Flexible
    public List<VendorDeviation> Deviations { get; init; }
}

// Standard-specific pattern implementations
public interface IStandardPatterns { }
public record HL7FieldPatterns : IStandardPatterns { }
public record FHIRProfilePatterns : IStandardPatterns { }
public record NCPDPElementPatterns : IStandardPatterns { }
```

---

## 🔄 **Incremental Configuration Building**

### **Additive Analysis Process**
```csharp
public class ConfigurationCatalog
{
    private readonly Dictionary<ConfigurationAddress, VendorConfiguration> _configs = new();
    
    public Result<VendorConfiguration> AddAnalysis(
        IEnumerable<string> messages,
        ConfigurationAddress address)
    {
        // 1. Analyze new messages
        var newConfig = _inferenceService.AnalyzeMessages(messages, address);
        
        // 2. Merge with existing configuration (if any)
        if (_configs.TryGetValue(address, out var existing))
        {
            return _inferenceService.MergeConfigurations(existing, newConfig.Value);
        }
        
        // 3. Store new configuration
        _configs[address] = newConfig.Value;
        return newConfig;
    }
}
```

### **Configuration Evolution Tracking**
```csharp
public record ConfigurationMetadata
{
    public DateTime FirstSeen { get; init; }
    public DateTime LastUpdated { get; init; }
    public int MessagesSampled { get; init; }
    public string Version { get; init; }  // "1.0", "1.1", etc.
    public List<ConfigurationChange> Changes { get; init; } = new();
}

public record ConfigurationChange
{
    public DateTime ChangeDate { get; init; }
    public string ChangeType { get; init; }  // "FieldAdded", "PatternChanged", "DeviationDetected"
    public string Description { get; init; }
    public double ImpactScore { get; init; }  // 0.0 to 1.0
}
```

---

## 📁 **Directory Structure**

### **Proposed Organization**
```
src/Pidgeon.Core/
├── Configuration/
│   ├── Intelligence/                           # Renamed from "Inference"  
│   │   ├── Common/                            # Standard-agnostic
│   │   │   ├── IConfigurationInferenceService.cs
│   │   │   ├── ConfigurationCatalog.cs
│   │   │   ├── VendorConfiguration.cs
│   │   │   ├── VendorSignature.cs
│   │   │   ├── VendorDeviation.cs            # Renamed from VendorQuirk
│   │   │   ├── ConfigurationAddress.cs
│   │   │   └── ConfigurationMetadata.cs
│   │   ├── Standards/                         # Standard-specific plugins
│   │   │   ├── HL7/
│   │   │   │   ├── HL7InferencePlugin.cs
│   │   │   │   ├── HL7FieldPatterns.cs
│   │   │   │   ├── HL7VendorSignature.cs
│   │   │   │   └── HL7VendorDetectors.cs     # Epic, Cerner, etc.
│   │   │   ├── FHIR/
│   │   │   │   ├── FHIRInferencePlugin.cs
│   │   │   │   ├── FHIRProfilePatterns.cs
│   │   │   │   └── FHIRVendorDetectors.cs
│   │   │   └── NCPDP/
│   │   │       ├── NCPDPInferencePlugin.cs
│   │   │       ├── NCPDPElementPatterns.cs
│   │   │       └── NCPDPVendorDetectors.cs   # SureScripts, etc.
│   │   └── Storage/                          # Configuration persistence
│   │       ├── IConfigurationStore.cs
│   │       ├── FileConfigurationStore.cs
│   │       └── ConfigurationRepository.cs
```

---

## 🔍 **Vendor Detection Strategy**

### **Standard-Specific Detection Patterns**

#### **HL7 Vendor Detection**
```csharp
public static class HL7VendorDetectors
{
    public static VendorSignature DetectEpic(string mshSegment)
    {
        var fields = mshSegment.Split('|');
        var sendingApp = fields[3];  // MSH.3
        
        if (sendingApp.Contains("Epic", StringComparison.OrdinalIgnoreCase))
        {
            return new VendorSignature
            {
                DetectedVendor = "Epic",
                Confidence = 0.95,
                SignatureFields = new Dictionary<string, string>
                {
                    ["MSH.3"] = sendingApp,
                    ["MSH.4"] = fields[4],
                    ["EncodingChars"] = fields[1]
                }
            };
        }
        
        return VendorSignature.Unknown;
    }
}
```

#### **NCPDP Vendor Detection**  
```csharp
public static class NCPDPVendorDetectors
{
    public static VendorSignature DetectSureScripts(string xmlMessage)
    {
        var doc = XDocument.Parse(xmlMessage);
        var fromElement = doc.Descendants("From").FirstOrDefault();
        
        if (fromElement?.Value.Contains("SureScripts") == true)
        {
            return new VendorSignature
            {
                DetectedVendor = "SureScripts",
                Confidence = 0.98,
                SignatureFields = new Dictionary<string, string>
                {
                    ["From"] = fromElement.Value,
                    ["SchemaVersion"] = doc.Root?.Attribute("version")?.Value ?? "Unknown"
                }
            };
        }
        
        return VendorSignature.Unknown;
    }
}
```

---

## 🎛️ **Configuration Querying & Management**

### **Flexible Configuration Retrieval**
```csharp
public interface IConfigurationCatalog
{
    // Exact match
    VendorConfiguration? GetConfiguration(ConfigurationAddress address);
    
    // Hierarchical queries
    IEnumerable<VendorConfiguration> GetByVendor(string vendor);
    IEnumerable<VendorConfiguration> GetByStandard(string standard);  
    IEnumerable<VendorConfiguration> GetByMessageType(string messageType);
    
    // Cross-cutting queries
    IEnumerable<VendorConfiguration> FindSimilar(VendorConfiguration reference, double threshold);
    IEnumerable<ConfigurationChange> GetRecentChanges(TimeSpan window);
    
    // Configuration operations
    Result<VendorConfiguration> MergeConfiguration(ConfigurationAddress address, IEnumerable<string> newMessages);
    Result<ConfigurationComparison> CompareConfigurations(ConfigurationAddress a, ConfigurationAddress b);
}
```

### **CLI Interface Examples**
```bash
# Analyze messages and build/update configuration
pidgeon config analyze --vendor CorEMR --standard HL7v23 --type "ADT^A01" --samples ./coremr_adt_messages/

# Query existing configurations
pidgeon config list --vendor Epic
pidgeon config show --address "Epic-HL7v23-ORM^O01"
pidgeon config diff --from "Epic-HL7v23-2023.1" --to "Epic-HL7v23-2024.2"

# Validate new messages against existing config
pidgeon validate --message test.hl7 --config "CorEMR-HL7v23-ADT^A01" --mode compatibility
```

---

## 🔄 **Integration with Existing Architecture**

### **Plugin Registration in DI Container**
```csharp
public static IServiceCollection AddConfigurationIntelligence(this IServiceCollection services)
{
    // Core services
    services.AddScoped<IConfigurationCatalog, ConfigurationCatalog>();
    services.AddScoped<IConfigurationStore, FileConfigurationStore>();
    
    // Standard-specific inference plugins
    services.AddStandardInferencePlugin<HL7InferencePlugin>();
    services.AddStandardInferencePlugin<FHIRInferencePlugin>();  
    services.AddStandardInferencePlugin<NCPDPInferencePlugin>();
    
    return services;
}
```

### **Alignment with Core+ Business Model**
- **Free Core**: Basic vendor detection, generic patterns
- **Professional ($29/mo)**: Advanced pattern analysis, config evolution tracking
- **Pro + Templates ($49/mo)**: Vendor-specific templates, detailed deviation analysis
- **Enterprise**: Custom vendor patterns, advanced analytics, configuration management

---

## 🎯 **Success Criteria & Validation**

### **Functional Requirements**
- [ ] Detect CorEMR vs Epic vs Cerner with >90% accuracy from MSH segment alone
- [ ] Build incrementally accurate configs as new message types analyzed  
- [ ] Handle same vendor across multiple standards (HL7, FHIR, NCPDP)
- [ ] Detect configuration drift when vendor implementations change
- [ ] Query configurations by vendor, standard, or message type

### **Non-Functional Requirements**  
- [ ] Analysis completes in <30 seconds for 100 message sample
- [ ] Configuration merge operations complete in <1 second
- [ ] Storage scales to 1000+ vendor configurations
- [ ] Plugin architecture allows adding new standards without core changes

### **Business Validation**
- [ ] Configuration intelligence demonstrably better than manual interface specs
- [ ] Reduces interface onboarding time from weeks to days
- [ ] Provides clear upgrade value for Professional tier features

---

## ⚠️ **Implementation Considerations**

### **Challenges to Address**
1. **Message Type Ambiguity**: Some vendors use non-standard MSH.9 values
2. **Configuration Conflicts**: What if Epic ADT analysis conflicts with Epic ORM analysis?
3. **Storage Scalability**: How to handle thousands of configurations efficiently?
4. **Version Management**: How to track and rollback configuration changes?

### **Risk Mitigation**
- **Start with HL7**: Prove architecture with well-defined standard before expanding
- **Confidence Thresholds**: Don't auto-merge configs below confidence threshold
- **Manual Override**: Always allow manual config editing and validation
- **Rollback Capability**: Version all configurations for easy rollback

---

**Next Steps**: Review this architecture, then implement the multi-standard directory structure and base interfaces before proceeding with HL7-specific implementation.

**Key Decision**: Does this hierarchical, additive approach align with your real-world testing scenarios with CorEMR and other vendors?