# Constraint Resolution Architecture

## Overview

The Constraint Resolution system ensures that all generated healthcare data complies with standard-specific constraints (HL7, FHIR, NCPDP) while maintaining Pidgeon's sacred architectural principles. This system enables field-level constraint validation and generation without embedding standard-specific logic in core services.

## Architectural Principles

### 1. Plugin-Based Architecture
- **All standard-specific logic lives in plugins**
- Core services remain standard-agnostic
- New standards added without modifying existing code

### 2. Four-Domain Separation
- **Clinical Domain**: Healthcare business concepts (Patient, Medication)
- **Messaging Domain**: Wire format structures (HL7 segments, FHIR resources)
- **Configuration Domain**: Constraint definitions and vendor patterns
- **Transformation Domain**: Mapping between domains

### 3. Performance First
- Multi-level caching strategy
- Lazy loading of constraint definitions
- Batch constraint resolution
- 30-minute TTL for cached constraints

## Core Components

### IConstraintResolver (Core Interface)

```csharp
namespace Pidgeon.Core.Application.Interfaces.Generation;

/// <summary>
/// Standard-agnostic constraint resolution service.
/// Generates values that comply with field-level constraints.
/// </summary>
public interface IConstraintResolver
{
    /// <summary>
    /// Resolves constraints for a field and generates a compliant value.
    /// </summary>
    /// <param name="context">Field context (e.g., "Patient.Gender" or "PID.8")</param>
    /// <param name="constraints">Field constraint definitions</param>
    /// <param name="random">Random generator for consistent generation</param>
    /// <returns>A constraint-compliant value</returns>
    Task<Result<object>> GenerateConstrainedValueAsync(
        string context,
        FieldConstraints constraints,
        Random random);

    /// <summary>
    /// Validates a value against field constraints.
    /// </summary>
    Task<Result<bool>> ValidateValueAsync(
        string context,
        object value,
        FieldConstraints constraints);

    /// <summary>
    /// Gets constraint definitions for a field.
    /// </summary>
    Task<Result<FieldConstraints>> GetConstraintsAsync(string context);
}
```

### FieldConstraints (Domain Model)

```csharp
namespace Pidgeon.Core.Domain.Configuration;

/// <summary>
/// Represents field-level constraints for healthcare standards.
/// Standard-agnostic model used across all plugins.
/// </summary>
public record FieldConstraints
{
    /// <summary>
    /// Data type (e.g., ST, NM, DT for HL7; string, integer for FHIR)
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Reference to a coded value table (e.g., "0001" for HL7 Admin Sex)
    /// </summary>
    public string? TableReference { get; init; }

    /// <summary>
    /// Maximum field length
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Minimum field length
    /// </summary>
    public int? MinLength { get; init; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// Whether the field can have multiple values
    /// </summary>
    public bool Repeating { get; init; }

    /// <summary>
    /// Regular expression pattern for validation
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Explicit list of allowed values
    /// </summary>
    public List<string>? AllowedValues { get; init; }

    /// <summary>
    /// Numeric constraints
    /// </summary>
    public NumericConstraints? Numeric { get; init; }

    /// <summary>
    /// Date/time constraints
    /// </summary>
    public DateTimeConstraints? DateTime { get; init; }

    /// <summary>
    /// Cross-field dependencies
    /// </summary>
    public List<FieldDependency>? Dependencies { get; init; }
}

public record NumericConstraints(
    decimal? MinValue,
    decimal? MaxValue,
    int? Precision,
    int? Scale);

public record DateTimeConstraints(
    DateTime? MinDate,
    DateTime? MaxDate,
    string? Format);

public record FieldDependency(
    string DependentField,
    string Condition,
    object RequiredValue);
```

## Plugin Infrastructure

### IConstraintResolverPlugin

```csharp
namespace Pidgeon.Core.Infrastructure.Generation.Constraints;

/// <summary>
/// Standard-specific constraint resolution plugin.
/// Each healthcare standard implements its own plugin.
/// </summary>
public interface IConstraintResolverPlugin
{
    /// <summary>
    /// Standard identifier (e.g., "hl7v23", "fhir-r4")
    /// </summary>
    string StandardId { get; }

    /// <summary>
    /// Priority for plugin selection (higher = preferred)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this plugin can handle the context.
    /// </summary>
    bool CanResolve(string context);

    /// <summary>
    /// Gets constraint definitions for a field.
    /// </summary>
    Task<Result<FieldConstraints>> GetConstraintsAsync(string context);

    /// <summary>
    /// Generates a constraint-compliant value.
    /// </summary>
    Task<Result<object>> GenerateValueAsync(
        FieldConstraints constraints,
        Random random);

    /// <summary>
    /// Validates a value against constraints.
    /// </summary>
    Task<Result<bool>> ValidateValueAsync(
        object value,
        FieldConstraints constraints);
}
```

### HL7 Constraint Plugin Implementation

```csharp
public class HL7ConstraintResolverPlugin : IConstraintResolverPlugin
{
    private readonly IStandardReferenceService _referenceService;
    private readonly IDemographicsDataService _demographicsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HL7ConstraintResolverPlugin> _logger;

    public string StandardId => "hl7v23";
    public int Priority => 100;

    public bool CanResolve(string context)
    {
        // Handle HL7 contexts: PID.8, MSH.9.1, etc.
        return Regex.IsMatch(context, @"^[A-Z]{2,3}(\.\d+){1,2}$");
    }

    public async Task<Result<FieldConstraints>> GetConstraintsAsync(string context)
    {
        var cacheKey = $"constraints:{StandardId}:{context}";
        if (_cache.TryGetValue<FieldConstraints>(cacheKey, out var cached))
            return Result<FieldConstraints>.Success(cached!);

        // Parse context (e.g., "PID.8")
        var parts = context.Split('.');
        var segmentName = parts[0];
        var fieldPosition = int.Parse(parts[1]);

        // Load segment definition from JSON
        var segmentResult = await _referenceService.LookupAsync(StandardId, segmentName);
        if (!segmentResult.IsSuccess)
            return Result<FieldConstraints>.Failure($"Segment {segmentName} not found");

        // Extract field constraints
        var constraints = ExtractFieldConstraints(segmentResult.Value, fieldPosition);

        // Cache for 30 minutes
        _cache.Set(cacheKey, constraints, TimeSpan.FromMinutes(30));

        return Result<FieldConstraints>.Success(constraints);
    }

    public async Task<Result<object>> GenerateValueAsync(
        FieldConstraints constraints,
        Random random)
    {
        // Generate based on constraint type
        if (constraints.TableReference != null)
        {
            return await GenerateFromTableAsync(constraints.TableReference, random);
        }

        if (constraints.DataType != null)
        {
            return GenerateByDataType(constraints.DataType, constraints, random);
        }

        if (constraints.AllowedValues?.Any() == true)
        {
            var value = constraints.AllowedValues[random.Next(constraints.AllowedValues.Count)];
            return Result<object>.Success(value);
        }

        // Default generation
        return GenerateDefaultValue(constraints, random);
    }

    private async Task<Result<object>> GenerateFromTableAsync(string tableId, Random random)
    {
        // Look up table values from JSON
        var tableResult = await _referenceService.LookupAsync(StandardId, tableId);
        if (!tableResult.IsSuccess)
            return Result<object>.Failure($"Table {tableId} not found");

        // Extract values and select randomly
        var values = ExtractTableValues(tableResult.Value);
        if (!values.Any())
            return Result<object>.Failure($"Table {tableId} has no values");

        var selectedValue = values[random.Next(values.Count)];
        return Result<object>.Success(selectedValue);
    }

    private Result<object> GenerateByDataType(
        string dataType,
        FieldConstraints constraints,
        Random random)
    {
        return dataType.ToUpperInvariant() switch
        {
            "ST" => GenerateString(constraints, random),
            "NM" => GenerateNumeric(constraints, random),
            "DT" => GenerateDate(constraints, random),
            "TM" => GenerateTime(constraints, random),
            "TS" => GenerateTimestamp(constraints, random),
            "ID" => GenerateIdentifier(constraints, random),
            "IS" => GenerateCodedValue(constraints, random),
            "CE" => GenerateCodedElement(constraints, random),
            "XPN" => GeneratePersonName(constraints, random),
            "XAD" => GenerateAddress(constraints, random),
            "XTN" => GenerateTelephone(constraints, random),
            _ => GenerateDefaultValue(constraints, random)
        };
    }
}
```

## Orchestration Layer

### ConstraintResolver Service

```csharp
namespace Pidgeon.Core.Application.Services.Generation;

public class ConstraintResolver : IConstraintResolver
{
    private readonly IEnumerable<IConstraintResolverPlugin> _plugins;
    private readonly ILogger<ConstraintResolver> _logger;
    private readonly IMemoryCache _cache;

    public ConstraintResolver(
        IEnumerable<IConstraintResolverPlugin> plugins,
        ILogger<ConstraintResolver> logger,
        IMemoryCache cache)
    {
        _plugins = plugins.OrderByDescending(p => p.Priority).ToList();
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<object>> GenerateConstrainedValueAsync(
        string context,
        FieldConstraints? constraints,
        Random random)
    {
        // If no constraints provided, fetch them
        if (constraints == null)
        {
            var constraintsResult = await GetConstraintsAsync(context);
            if (!constraintsResult.IsSuccess)
            {
                _logger.LogDebug("No constraints found for {Context}, using default generation", context);
                return GenerateDefaultValue(context, random);
            }
            constraints = constraintsResult.Value;
        }

        // Check cache for previously generated patterns
        var cacheKey = $"generated:{context}:{constraints.GetHashCode()}:{random.Next()}";
        if (ShouldUseCache(constraints) && _cache.TryGetValue<object>(cacheKey, out var cached))
        {
            return Result<object>.Success(cached!);
        }

        // Find appropriate plugin
        var plugin = _plugins.FirstOrDefault(p => p.CanResolve(context));
        if (plugin == null)
        {
            _logger.LogDebug("No plugin found for {Context}, using basic generation", context);
            return GenerateBasicValue(constraints, random);
        }

        // Generate value using plugin
        var result = await plugin.GenerateValueAsync(constraints, random);

        // Cache successful generation for reusable patterns
        if (result.IsSuccess && ShouldUseCache(constraints))
        {
            _cache.Set(cacheKey, result.Value, TimeSpan.FromMinutes(5));
        }

        return result;
    }

    public async Task<Result<FieldConstraints>> GetConstraintsAsync(string context)
    {
        var plugin = _plugins.FirstOrDefault(p => p.CanResolve(context));
        if (plugin == null)
        {
            return Result<FieldConstraints>.Failure($"No plugin can resolve context: {context}");
        }

        return await plugin.GetConstraintsAsync(context);
    }

    public async Task<Result<bool>> ValidateValueAsync(
        string context,
        object value,
        FieldConstraints constraints)
    {
        var plugin = _plugins.FirstOrDefault(p => p.CanResolve(context));
        if (plugin == null)
        {
            // Basic validation without plugin
            return ValidateBasic(value, constraints);
        }

        return await plugin.ValidateValueAsync(value, constraints);
    }

    private bool ShouldUseCache(FieldConstraints constraints)
    {
        // Cache patterns, table references, but not random strings
        return constraints.Pattern != null ||
               constraints.TableReference != null ||
               constraints.AllowedValues != null;
    }
}
```

## Integration with GenerationService

### Modified GenerationService

```csharp
public class GenerationService : IGenerationService
{
    private readonly IConstraintResolver _constraintResolver;
    private readonly IDemographicsDataService _demographicsService;
    private readonly ILogger<GenerationService> _logger;

    private async Task<Patient> GeneratePatientLogicAsync(Random random, GenerationOptions options)
    {
        // Generate administrative sex using HL7 table 0001
        var genderResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.8",  // HL7 field context
            new FieldConstraints
            {
                TableReference = "0001",
                DataType = "IS",
                MaxLength = 1,
                Required = false
            },
            random);

        var gender = genderResult.IsSuccess
            ? ParseGender(genderResult.Value?.ToString())
            : Gender.Unknown;

        // Generate patient identifier with proper constraints
        var identifierResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.3",
            new FieldConstraints
            {
                DataType = "CX",
                Required = true,
                Pattern = @"^\d{6,10}$"
            },
            random);

        // Generate birth date with realistic constraints
        var dobResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.7",
            new FieldConstraints
            {
                DataType = "TS",
                DateTime = new DateTimeConstraints(
                    MinDate = DateTime.Today.AddYears(-120),
                    MaxDate = DateTime.Today,
                    Format = "yyyyMMdd")
            },
            random);

        // Use demographics service for non-constrained fields
        var (firstName, lastName, _) = await _demographicsService.GenerateRandomNameAsync(random);
        var address = await _demographicsService.GenerateRandomAddressAsync(random);

        return new Patient
        {
            Id = identifierResult.Value?.ToString() ?? GenerateMRN(random),
            Name = PersonName.Create(lastName, firstName),
            Gender = gender,
            BirthDate = dobResult.IsSuccess
                ? DateTime.Parse(dobResult.Value!.ToString()!)
                : CalculateDateOfBirth(GenerateAgeForPatientType(random), random),
            Address = address,
            // ... other fields
        };
    }
}
```

## Caching Strategy

### Multi-Level Cache

```csharp
public class ConstraintCacheConfiguration
{
    /// <summary>
    /// Cache constraint definitions (expensive JSON parsing)
    /// TTL: 30 minutes
    /// </summary>
    public static readonly CacheProfile ConstraintDefinitions = new()
    {
        KeyPrefix = "constraints:",
        TimeToLive = TimeSpan.FromMinutes(30),
        Priority = CacheItemPriority.High
    };

    /// <summary>
    /// Cache table values (frequently reused)
    /// TTL: 1 hour
    /// </summary>
    public static readonly CacheProfile TableValues = new()
    {
        KeyPrefix = "table:",
        TimeToLive = TimeSpan.FromHours(1),
        Priority = CacheItemPriority.Normal
    };

    /// <summary>
    /// Cache generated patterns (complex constraints)
    /// TTL: 5 minutes
    /// </summary>
    public static readonly CacheProfile GeneratedPatterns = new()
    {
        KeyPrefix = "pattern:",
        TimeToLive = TimeSpan.FromMinutes(5),
        Priority = CacheItemPriority.Low
    };
}
```

## Dependency Registration

### ServiceCollection Extensions

```csharp
public static class ConstraintServiceExtensions
{
    public static IServiceCollection AddConstraintResolution(this IServiceCollection services)
    {
        // Register core service
        services.AddScoped<IConstraintResolver, ConstraintResolver>();

        // Register plugins
        services.AddScoped<IConstraintResolverPlugin, HL7ConstraintResolverPlugin>();
        services.AddScoped<IConstraintResolverPlugin, FHIRConstraintResolverPlugin>();
        services.AddScoped<IConstraintResolverPlugin, NCPDPConstraintResolverPlugin>();

        // Configure caching
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.25;
        });

        return services;
    }
}
```

## Performance Considerations

### Optimization Strategies

1. **Lazy Loading**
   - Constraint definitions loaded only when needed
   - Plugins initialized on first use
   - JSON files parsed once and cached

2. **Batch Processing**
   - Multiple constraints resolved in single operation
   - Shared random seed for consistent generation
   - Bulk table lookups

3. **Smart Caching**
   - Hash-based cache keys for efficiency
   - Different TTLs for different data types
   - Memory pressure-aware eviction

4. **Fallback Mechanisms**
   - Graceful degradation when constraints unavailable
   - Basic generation as last resort
   - Default values for critical fields

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class ConstraintResolverTests
{
    [TestMethod]
    public async Task GenerateConstrainedValue_WithTableReference_ReturnsValidValue()
    {
        // Arrange
        var constraints = new FieldConstraints
        {
            TableReference = "0001",
            DataType = "IS"
        };

        // Act
        var result = await _resolver.GenerateConstrainedValueAsync(
            "PID.8", constraints, new Random(42));

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(new[] { "M", "F", "O", "U" }.Contains(result.Value.ToString()));
    }

    [TestMethod]
    public async Task ValidateValue_WithMaxLength_ValidatesCorrectly()
    {
        // Arrange
        var constraints = new FieldConstraints { MaxLength = 10 };

        // Act
        var validResult = await _resolver.ValidateValueAsync(
            "PID.5", "Smith", constraints);
        var invalidResult = await _resolver.ValidateValueAsync(
            "PID.5", "VeryLongLastNameThatExceedsLimit", constraints);

        // Assert
        Assert.IsTrue(validResult.IsSuccess);
        Assert.IsFalse(invalidResult.IsSuccess);
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class HL7GenerationIntegrationTests
{
    [TestMethod]
    public async Task GeneratePatient_WithConstraints_ProducesValidHL7Message()
    {
        // Arrange
        var generationService = GetConfiguredService();

        // Act
        var patientResult = generationService.GeneratePatient(new GenerationOptions());
        var hl7Message = ConvertToHL7(patientResult.Value);

        // Assert
        Assert.IsTrue(ValidateHL7Message(hl7Message));
        Assert.AreEqual("M", ExtractField(hl7Message, "PID.8")); // Valid table value
    }
}
```

## Benefits

1. **Standards Compliance** - Generated data always respects constraints
2. **Plugin Architecture** - Easy to add new standards
3. **Performance** - Efficient caching and lazy loading
4. **Maintainability** - Clear separation of concerns
5. **Testability** - Each component independently testable
6. **Scalability** - Handles hundreds of constraints efficiently

## Future Enhancements

1. **Constraint Learning** - ML-based pattern detection from real messages
2. **Cross-Field Dependencies** - Complex constraint relationships
3. **Custom Constraint Plugins** - User-defined constraint rules
4. **Constraint Profiles** - Vendor-specific constraint sets
5. **Real-Time Validation** - Live constraint checking during generation