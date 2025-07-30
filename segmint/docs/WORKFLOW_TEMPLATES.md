# Workflow Templates Architecture - Hybrid Approach

*Decision Date: July 17, 2025*

## 📋 **Decision Summary**

After analyzing three workflow template options, we have chosen a **Hybrid Approach** combining **Code Templates (Option A)** with **Builder Pattern API (Option C)** for our pharmacy workflow implementation.

## 🎯 **Chosen Architecture**

### **Primary: Code Templates (Option A)**
Static factory methods providing pre-configured workflows with compile-time safety.

```csharp
public static class PharmacyWorkflows
{
    // Core workflow templates
    public static RDEMessage CreateNewPrescription(string patientId, string lastName, 
        string firstName, string medicationCode, string medicationName, 
        decimal quantity, string units, string sig, string providerId)
    
    public static ORRMessage CreateAcceptanceResponse(string originalMessageControlId,
        string placerOrderNumber, string fillerOrderNumber, string acceptanceMessage = null)
    
    public static RDSMessage CreateDispenseRecord(string patientId, string lastName,
        string firstName, string prescriptionNumber, string medicationCode, 
        string medicationName, decimal dispensedAmount, string units, string pharmacist)
    
    public static ORRMessage CreateDrugInteractionResponse(string originalMessageControlId,
        string placerOrderNumber, List<DrugInteraction> interactions, bool allowOverride = true)
    
    public static RDSMessage CreateControlledSubstanceDispense(string patientId, 
        string lastName, string firstName, string prescriptionNumber, 
        string medicationCode, string medicationName, decimal amount, string units,
        string pharmacist, string lotNumber, DateTime expirationDate, 
        string manufacturer, string deaNumber, string schedule)
}
```

### **Secondary: Builder Pattern API (Option C)**
Fluent interface for customization and complex scenarios.

```csharp
public static class PharmacyWorkflowBuilder
{
    public static PrescriptionBuilder NewPrescription()
    {
        return new PrescriptionBuilder();
    }
    
    public static OrderResponseBuilder NewOrderResponse()
    {
        return new OrderResponseBuilder();
    }
    
    public static DispenseBuilder NewDispense()
    {
        return new DispenseBuilder();
    }
}

public class PrescriptionBuilder
{
    public PrescriptionBuilder WithPatient(string id, string lastName, string firstName)
    public PrescriptionBuilder WithMedication(string code, string name, decimal strength, string units)
    public PrescriptionBuilder WithProvider(string id, string lastName, string firstName)
    public PrescriptionBuilder WithInstructions(string sig, int refills, int daysSupply)
    public PrescriptionBuilder WithPriority(string priority)
    public PrescriptionBuilder WithDiagnosis(string code, string description)
    public RDEMessage Build()
}
```

## 🎨 **Usage Examples**

### **Code Templates (Primary Usage)**
```csharp
// Simple prescription creation
var prescription = PharmacyWorkflows.CreateNewPrescription(
    patientId: "12345",
    lastName: "Smith",
    firstName: "John",
    medicationCode: "0069-2587-68",
    medicationName: "Lisinopril 10mg Tablet",
    quantity: 30,
    units: "TAB",
    sig: "Take 1 tablet by mouth daily",
    providerId: "DEA123456"
);

// Acceptance response
var response = PharmacyWorkflows.CreateAcceptanceResponse(
    originalMessageControlId: "MSG001",
    placerOrderNumber: "ORD001",
    fillerOrderNumber: "RX001",
    acceptanceMessage: "Order accepted for processing"
);
```

### **Builder Pattern (Advanced Usage)**
```csharp
// Complex prescription with multiple customizations
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("12345", "Smith", "John")
    .WithMedication("0069-2587-68", "Lisinopril 10mg Tablet", 10, "mg")
    .WithProvider("DEA123456", "Johnson", "Robert")
    .WithInstructions("Take 1 tablet by mouth daily", 3, 30)
    .WithPriority("R")
    .WithDiagnosis("I10", "Essential hypertension")
    .Build();

// Custom order response with multiple errors
var response = PharmacyWorkflowBuilder.NewOrderResponse()
    .ForOriginalMessage("MSG001")
    .WithOrderNumber("ORD001")
    .RejectWithErrors(new[]
    {
        new PharmacyError("DRUG001", "Drug interaction detected"),
        new PharmacyError("ALLERGY01", "Patient allergy to penicillin")
    })
    .Build();
```

## 🏗️ **Implementation Plan**

### **Phase 1: Core Templates**
1. **PharmacyWorkflows static class** with essential factory methods
2. **Basic workflow patterns** for common CIPS scenarios
3. **Integration with existing message types** (RDE, ORR, RDS)

### **Phase 2: Builder Pattern**
1. **PrescriptionBuilder** for complex prescription scenarios
2. **OrderResponseBuilder** for detailed response handling
3. **DispenseBuilder** for advanced dispensing workflows

### **Phase 3: GUI Integration**
1. **Code templates exposed in CLI** for scripting scenarios
2. **Builder pattern integrated in GUI** for interactive workflows
3. **Template customization interface** for power users

## 🎯 **Benefits of Hybrid Approach**

### **Consistency**
- Aligns with existing `ORMMessage.CreateLabOrder()` pattern
- Maintains architectural consistency across codebase
- Follows established conventions for factory methods

### **CLI/GUI Compatibility**
- **CLI**: Code templates provide efficient scripting interface
- **GUI**: Builder pattern enables interactive form-based workflows
- **Both**: Progressive enhancement from simple to complex scenarios

### **Performance**
- **Code templates**: Zero runtime overhead, compile-time optimization
- **Builder pattern**: Minimal overhead, optimized for flexibility
- **No configuration parsing**: Eliminates JSON/YAML interpretation costs

### **Flexibility**
- **Templates**: Cover 80% of common use cases efficiently
- **Builders**: Handle 20% of complex/custom scenarios
- **Extensibility**: Easy to add new templates and builders

## 🔧 **Technical Architecture**

### **Code Templates**
```csharp
// Static factory methods with validation
public static RDEMessage CreateNewPrescription(...)
{
    var message = new RDEMessage();
    
    // Set required fields with validation
    message.SetPatientInformation(patientId, lastName, firstName);
    message.SetMedicationDetails(medicationCode, medicationName, ...);
    message.SetOrderInfo(orderingProvider: providerId);
    
    // Apply standard configurations
    message.Header.SetProcessingInfo("P", "2.3");
    message.Header.SetMessageType("RDE", "O01");
    
    return message;
}
```

### **Builder Pattern**
```csharp
// Fluent interface with progressive disclosure
public class PrescriptionBuilder
{
    private readonly RDEMessage _message;
    
    public PrescriptionBuilder()
    {
        _message = new RDEMessage();
    }
    
    public PrescriptionBuilder WithPatient(string id, string lastName, string firstName)
    {
        _message.SetPatientInformation(id, lastName, firstName);
        return this;
    }
    
    public RDEMessage Build()
    {
        // Final validation and message preparation
        _message.Header.GenerateMessageControlId();
        return _message;
    }
}
```

## 📊 **Comparison Analysis**

| Aspect | Code Templates | Builder Pattern | Configuration |
|--------|----------------|-----------------|---------------|
| **Performance** | ✅ Excellent | ✅ Good | ❌ Poor |
| **Type Safety** | ✅ Compile-time | ✅ Compile-time | ❌ Runtime |
| **Flexibility** | ❌ Limited | ✅ High | ✅ Excellent |
| **CLI Support** | ✅ Excellent | ✅ Good | ✅ Good |
| **GUI Support** | ❌ Limited | ✅ Excellent | ✅ Good |
| **Debugging** | ✅ Easy | ✅ Easy | ❌ Difficult |
| **Maintenance** | ✅ Easy | ✅ Moderate | ❌ Complex |

## 🚀 **Implementation Status**

### **Current**
- ✅ **Architecture decision** approved
- ✅ **Documentation** updated (PHARMACY_ROADMAP.md, DEVNOTES.md)
- 🔄 **Implementation** starting

### **Next Steps**
1. **Create PharmacyWorkflows class** with core template methods
2. **Implement PrescriptionBuilder** for complex scenarios
3. **Add GUI integration** for builder pattern
4. **Create unit tests** for both approaches
5. **Update CLI commands** to use workflow templates

## 📝 **Decision Rationale**

This hybrid approach was chosen because it:

1. **Maintains consistency** with our existing codebase patterns
2. **Provides optimal performance** for common use cases
3. **Enables flexibility** for complex scenarios
4. **Supports both CLI and GUI** interfaces effectively
5. **Follows progressive enhancement** principles
6. **Minimizes learning curve** for developers familiar with our codebase

The decision aligns with our architectural goals of creating a robust, performant, and user-friendly pharmacy workflow system that can grow with our users' needs.