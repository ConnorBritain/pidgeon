# Message Factory Pattern for Multi-Standard Healthcare Platforms

**Date**: August 28, 2025  
**Status**: ✅ APPROVED - Implementation Strategy  
**Context**: Cross-standard message creation architecture for Pidgeon Healthcare Platform

---

## 🎯 **Problem Statement**

Healthcare message creation requires handling fundamentally different patterns across standards:

- **HL7**: Create message → Add MSH → Add PID → Add segments in sequence
- **FHIR**: Create Bundle → Add Patient Resource → Add other resources → Set metadata  
- **NCPDP**: Create Transaction → Set XML elements → Add nested structures

Traditional approaches either:
1. **Hardcode versions/standards** in domain models (violates plugin architecture)
2. **Require standard-specific knowledge** from consumers (poor API experience)
3. **Create inconsistent interfaces** across plugins (maintenance burden)

---

## ✅ **Chosen Solution: Hybrid Abstract Factory Pattern**

### **Core Architecture**
```csharp
// Common factory interface across all healthcare standards
public interface IStandardMessageFactory
{
    // Core healthcare concepts that translate across standards
    Result<IStandardMessage> CreatePatientAdmission(Patient patient, MessageOptions? options = null);
    Result<IStandardMessage> CreatePatientDischarge(Patient patient, Encounter encounter, MessageOptions? options = null);
    Result<IStandardMessage> CreatePrescription(Prescription rx, MessageOptions? options = null);
    Result<IStandardMessage> CreateLabOrder(LabOrder order, MessageOptions? options = null);
    Result<IStandardMessage> CreateLabResult(LabResult result, MessageOptions? options = null);
    
    // Standard-specific capabilities (optional implementations)
    bool SupportsMessageType(string messageType);
    Result<IStandardMessage> CreateCustomMessage(string messageType, object data, MessageOptions? options = null);
}

// Each plugin implements the factory with standard-specific logic
public abstract class StandardPlugin : IStandardPlugin
{
    public abstract string StandardName { get; }
    public abstract Version StandardVersion { get; }
    public abstract IStandardMessageFactory MessageFactory { get; }
    // ... other plugin interface members
}
```

### **Standard-Specific Implementation**
```csharp
public class HL7v23Plugin : StandardPlugin
{
    public override string StandardName => "HL7";
    public override Version StandardVersion => new Version(2, 3);
    public override IStandardMessageFactory MessageFactory => new HL7v23MessageFactory(this);
}

public class HL7v23MessageFactory : IStandardMessageFactory
{
    private readonly IStandardPlugin _plugin;
    
    public HL7v23MessageFactory(IStandardPlugin plugin)
    {
        _plugin = plugin;
    }
    
    public Result<IStandardMessage> CreatePatientAdmission(Patient patient, MessageOptions? options = null)
    {
        var message = new ADTMessage()
        {
            // Plugin provides all standard-specific configuration from metadata
            Standard = _plugin.StandardName,
            Version = _plugin.StandardVersion.ToString(),
            MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString(),
            Timestamp = options?.Timestamp ?? DateTime.UtcNow,
            SendingSystem = options?.SendingApplication ?? GetDefaultSendingApp(),
            ReceivingSystem = options?.ReceivingApplication ?? "UNKNOWN",
            MessageType = HL7MessageType.Common.ADT_A01
        };

        // HL7-specific message construction
        message.InitializeMessage();
        
        // Configure MSH segment with plugin knowledge
        var msh = message.MSH!;
        msh.SetMessageType("ADT", "A01");
        msh.SendingApplication.SetValue(message.SendingSystem);
        msh.ReceivingApplication.SetValue(message.ReceivingSystem);
        
        // Add patient information
        var pid = new PIDSegment();
        pid.SetPatientData(patient);
        message.AddSegment(pid);
        
        return Result<IStandardMessage>.Success(message);
    }
    
    private string GetDefaultSendingApp() => $"PIDGEON-{_plugin.StandardName}v{_plugin.StandardVersion}";
}

public class FHIRv4MessageFactory : IStandardMessageFactory
{
    public Result<IStandardMessage> CreatePatientAdmission(Patient patient, MessageOptions? options = null)
    {
        var bundle = new FHIRBundle()
        {
            Standard = "FHIR",
            Version = "4.0.1",
            MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString(),
            // ... other FHIR-specific setup
        };

        // FHIR-specific bundle construction
        bundle.AddPatientResource(patient);
        bundle.AddEncounterResource(/* admission data */);
        
        return Result<IStandardMessage>.Success(bundle);
    }
}
```

### **Consumer Usage Pattern**
```csharp
// Service layer - standard-agnostic
public class MessageGenerationService
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    
    public Result<IStandardMessage> GenerateAdmissionMessage(
        Patient patient, 
        string standard, 
        string? version = null,
        MessageOptions? options = null)
    {
        var plugin = _pluginRegistry.GetPlugin(standard, version);
        if (plugin == null)
            return Result<IStandardMessage>.Failure($"No plugin found for {standard} {version}");
            
        // Same interface works for HL7, FHIR, NCPDP, etc.
        return plugin.MessageFactory.CreatePatientAdmission(patient, options);
    }
}

// CLI usage - simple and discoverable
var hl7Plugin = pluginRegistry.GetPlugin("HL7", "2.3");
var admissionMsg = hl7Plugin.MessageFactory.CreatePatientAdmission(patient);

var fhirPlugin = pluginRegistry.GetPlugin("FHIR", "4.0");
var fhirBundle = fhirPlugin.MessageFactory.CreatePatientAdmission(patient); // Same method!
```

---

## ✅ **Key Benefits**

### **1. Plugin Architecture Compliance**
- ✅ **No hardcoding**: Each plugin owns its version/standard configuration
- ✅ **Standard isolation**: HL7 logic doesn't leak into FHIR implementations
- ✅ **Version flexibility**: HL7v23Plugin vs HL7v25Plugin can have completely different logic

### **2. API Consistency & Discoverability**  
- ✅ **Uniform interface**: Same method names across all standards
- ✅ **Intellisense friendly**: `.MessageFactory.Create...` always available
- ✅ **No casting required**: All plugins expose same factory interface

### **3. Cross-Standard Business Logic**
- ✅ **Multi-standard generation**: Easily generate same concept in multiple standards
- ✅ **Standard comparison**: Test message equivalence across standards
- ✅ **Migration scenarios**: Convert HL7 → FHIR using same domain objects

### **4. Maintenance & Testing**
- ✅ **Contract enforcement**: Every plugin must support core healthcare concepts
- ✅ **Mock simplicity**: One interface to mock for all standards
- ✅ **Plugin validation**: Can test that all plugins implement core scenarios

---

## ⚠️ **Potential Downsides & Mitigations**

### **Risk 1: Lowest Common Denominator Interface**
**Problem**: Interface might miss standard-specific capabilities

**Mitigation**: 
- Include `CreateCustomMessage(string messageType, object data)` for standard-specific scenarios
- `SupportsMessageType(string messageType)` discovery method
- Keep interface focused on common healthcare workflows (80% use case coverage)

### **Risk 2: Interface Bloat**
**Problem**: Interface could grow large trying to cover all healthcare scenarios

**Mitigation**:
- Start with core 5-7 message types based on validated user stories
- Use composition: `IAdmissionMessageFactory`, `IPharmacyMessageFactory` etc.
- Version the interface: `IStandardMessageFactoryV2` for major additions

### **Risk 3: Implementation Burden**
**Problem**: Every plugin must implement full interface (even if some operations don't make sense)

**Mitigation**:
- Provide `NotSupportedException` base implementations for unused operations
- Use interface segregation: Split into `IPatientMessageFactory`, `IPharmacyMessageFactory`
- Document which standards typically implement which message types

### **Risk 4: MessageOptions Parameter Complexity**
**Problem**: Options object might become kitchen sink of all possible standard configurations

**Mitigation**:
- Keep base `MessageOptions` minimal (SendingApp, MessageId, Timestamp)
- Allow standard-specific options through inheritance: `HL7MessageOptions : MessageOptions`
- Plugin can cast and use extended options when available

---

## 📋 **Implementation Plan**

### **Phase 1: Core Interface & Base Implementation**
1. Define `IStandardMessageFactory` with 5 core methods
2. Define `MessageOptions` base class
3. Update `IStandardPlugin` to include `.MessageFactory` property
4. Create abstract base `StandardMessageFactory` with helpful utilities

### **Phase 2: HL7v23 Reference Implementation**
1. Implement `HL7v23MessageFactory` with all 5 core methods
2. Update existing `ADTMessage.CreateAdmission()` to use factory pattern
3. Move hardcoded values to plugin configuration
4. Ensure all required members are set from plugin context

### **Phase 3: Plugin Registry Integration**
1. Update service registration to use factory pattern
2. Modify `GenerationService` to use plugin factories instead of hardcoded logic
3. Update CLI commands to discover and use factories
4. Add factory validation to plugin registration

### **Phase 4: Additional Standards**
1. Implement `FHIRv4MessageFactory` reference implementation
2. Implement `NCPDPMessageFactory` reference implementation
3. Validate cross-standard consistency
4. Performance optimization and caching

---

## 🎯 **Success Criteria**

### **Developer Experience**
- [ ] New developers can generate messages without knowing standard-specific APIs
- [ ] Same domain objects work across all standards
- [ ] Plugin authors have clear contract to implement

### **Architectural Compliance** 
- [ ] Zero hardcoded standards/versions in domain models
- [ ] Each plugin fully owns its creation logic
- [ ] Easy to add new standards without touching existing code

### **Business Value**
- [ ] Cross-standard message generation supports testing workflows
- [ ] Configuration intelligence works consistently across standards
- [ ] API simplicity drives adoption of platform

---

## 📚 **References**

- **Gang of Four Abstract Factory**: For multi-family object creation patterns
- **Domain-Driven Design**: Factory pattern for aggregate creation
- **Plugin Architecture Patterns**: For maintaining standard isolation
- **Healthcare Standards**: HL7 v2.x, FHIR R4, NCPDP SCRIPT specifications

---

**Decision**: Approved for implementation. This pattern balances plugin architecture principles with API usability for our multi-standard healthcare platform.

*Implementation begins immediately following ARCH-025 completion.*