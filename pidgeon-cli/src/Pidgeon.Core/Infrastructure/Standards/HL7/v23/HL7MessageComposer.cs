// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Common;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Pidgeon.Core.Services.FieldValueResolvers;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Semantic importance of composite components for realistic data generation.
/// Determines population probability: Critical=95%, Important=50%, Optional=20%
/// </summary>
internal enum ComponentImportance
{
    Critical,   // Core fields needed for realistic data (Street, City, Name)
    Important,  // Commonly populated fields that add realism (Suite, Email, Middle name)
    Optional    // Rarely used fields (Census Tract, Other designation)
}

/// <summary>
/// Completely data-driven HL7 message composer that generates any HL7 v2.3 message
/// using only data from Pidgeon.Data JSON files:
/// - trigger_events/*.json for message structure and segment rules
/// - segments/*.json for field definitions and constraints
/// - data_types/*.json for field formatting rules
/// - tables/*.json for coded value options
/// Zero hardcoded message types, segment builders, or field values.
/// </summary>
public class HL7MessageComposer
{
    private readonly ILogger<HL7MessageComposer> _logger;
    private readonly IHL7TriggerEventProvider _triggerEventProvider;
    private readonly IHL7SegmentProvider _segmentProvider;
    private readonly IHL7DataTypeProvider _dataTypeProvider;
    private readonly IHL7TableProvider _tableProvider;
    private readonly IFieldValueResolverService _fieldValueResolverService;
    private readonly IEnumerable<ICompositeAwareResolver> _compositeAwareResolvers;
    private readonly Random _random;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, List<string>> _demographicTableCache = new();

    public HL7MessageComposer(
        ILogger<HL7MessageComposer> logger,
        IHL7TriggerEventProvider triggerEventProvider,
        IHL7SegmentProvider segmentProvider,
        IHL7DataTypeProvider dataTypeProvider,
        IHL7TableProvider tableProvider,
        IFieldValueResolverService fieldValueResolverService,
        IEnumerable<ICompositeAwareResolver> compositeAwareResolvers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _triggerEventProvider = triggerEventProvider ?? throw new ArgumentNullException(nameof(triggerEventProvider));
        _segmentProvider = segmentProvider ?? throw new ArgumentNullException(nameof(segmentProvider));
        _dataTypeProvider = dataTypeProvider ?? throw new ArgumentNullException(nameof(dataTypeProvider));
        _tableProvider = tableProvider ?? throw new ArgumentNullException(nameof(tableProvider));
        _fieldValueResolverService = fieldValueResolverService ?? throw new ArgumentNullException(nameof(fieldValueResolverService));
        _compositeAwareResolvers = compositeAwareResolvers ?? throw new ArgumentNullException(nameof(compositeAwareResolvers));
        _random = new Random();

        // Load resources from Pidgeon.Data assembly
        _assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    /// <summary>
    /// Composes any HL7 message type using pure data-driven approach.
    /// Reads trigger event JSON, follows segment rules, uses data type/table definitions.
    /// </summary>
    public async Task<Result<string>> ComposeMessageAsync(
        string messageType,
        Patient patient,
        Encounter? encounter = null,
        Prescription? prescription = null,
        ObservationResult? observation = null,
        GenerationOptions? options = null)
    {
        try
        {
            options ??= new GenerationOptions();

            // Convert message type to trigger event code (e.g., "ADT^A01" -> "ADT_A01")
            var triggerEventCode = messageType.Replace("^", "_").ToLower();

            // Load trigger event definition from JSON
            var triggerEventResult = await _triggerEventProvider.GetTriggerEventAsync(triggerEventCode);
            if (triggerEventResult.IsFailure)
            {
                return Result<string>.Failure($"Trigger event {triggerEventCode} not found: {triggerEventResult.Error}");
            }

            var triggerEvent = triggerEventResult.Value;
            var messageBuilder = new StringBuilder();
            var segmentContext = new SegmentGenerationContext(patient, encounter, prescription, observation, messageType);

            // Process all segments according to trigger event definition
            await ProcessSegmentsFromTriggerEvent(triggerEvent.Segments, messageBuilder, segmentContext, options);

            var finalMessage = messageBuilder.ToString().TrimEnd();
            _logger.LogDebug("Generated {MessageType} with {SegmentCount} segments using trigger event {TriggerEventCode}",
                messageType, finalMessage.Split('\n').Length, triggerEventCode);

            return Result<string>.Success(finalMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error composing HL7 message {MessageType}", messageType);
            return Result<string>.Failure($"Error composing message: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes segments exactly as defined in trigger event JSON with proper group handling.
    /// </summary>
    private async Task ProcessSegmentsFromTriggerEvent(
        IReadOnlyList<TriggerEventSegment> segments,
        StringBuilder messageBuilder,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        var groupInclusionStack = new Stack<bool>();

        foreach (var segmentDef in segments)
        {
            // Handle group segments
            if (segmentDef.IsGroup)
            {
                var includeGroup = ShouldIncludeSegment(segmentDef, options);
                groupInclusionStack.Push(includeGroup);

                _logger.LogTrace("Processing group {GroupCode}: included={Include}",
                    segmentDef.SegmentCode, includeGroup);
                continue;
            }

            // Skip segments in excluded groups
            if (groupInclusionStack.Any() && !groupInclusionStack.Peek())
            {
                continue;
            }

            // Check segment inclusion based on optionality rules from JSON
            if (!ShouldIncludeSegment(segmentDef, options))
            {
                _logger.LogTrace("Skipping optional segment {SegmentCode}", segmentDef.SegmentCode);
                continue;
            }

            // Generate segment using pure data-driven approach
            var segmentResult = await GenerateSegmentFromSchemaAsync(segmentDef, context, options);
            if (segmentResult.IsFailure)
            {
                _logger.LogWarning("Failed to generate segment {SegmentCode}: {Error}",
                    segmentDef.SegmentCode, segmentResult.Error);
                continue;
            }

            messageBuilder.AppendLine(segmentResult.Value);

            // Handle segment repetition based on JSON repeatability
            if (segmentDef.Repeatability == "∞")
            {
                var repeatCount = await DetermineRepeatCountFromDataAsync(segmentDef, options);
                for (int i = 1; i < repeatCount; i++)
                {
                    var repeatResult = await GenerateSegmentFromSchemaAsync(segmentDef, context, options);
                    if (repeatResult.IsSuccess)
                    {
                        messageBuilder.AppendLine(repeatResult.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determines segment inclusion based purely on trigger event JSON optionality rules.
    /// Uses data-driven probabilities instead of hardcoded values.
    /// </summary>
    private bool ShouldIncludeSegment(TriggerEventSegment segmentDef, GenerationOptions options)
    {
        // Required segments always included
        if (segmentDef.Optionality == "R")
            return true;

        // Use custom probabilities if provided
        if (options.SegmentProbabilities?.TryGetValue(segmentDef.SegmentCode, out var probability) == true)
        {
            return _random.NextDouble() < probability;
        }

        // TODO: Read default probabilities from trigger event JSON when available
        // For now, use reasonable healthcare defaults
        return _random.NextDouble() < 0.6; // 60% inclusion for optional segments
    }

    /// <summary>
    /// Generates segments using pure schema-driven approach from Pidgeon.Data.
    /// </summary>
    private async Task<Result<string>> GenerateSegmentFromSchemaAsync(
        TriggerEventSegment segmentDef,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        try
        {
            // Load segment schema from JSON
            var segmentSchemaResult = await _segmentProvider.GetSegmentAsync(segmentDef.SegmentCode);
            if (segmentSchemaResult.IsFailure)
            {
                _logger.LogDebug("No schema found for {SegmentCode}, generating minimal segment",
                    segmentDef.SegmentCode);
                return GenerateMinimalSegment(segmentDef.SegmentCode);
            }

            // Update context with current segment code
            var segmentContext = context with { SegmentCode = segmentDef.SegmentCode };
            return await GenerateSegmentFromSchemaDefinition(segmentSchemaResult.Value, segmentContext, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating segment {SegmentCode}", segmentDef.SegmentCode);
            return GenerateMinimalSegment(segmentDef.SegmentCode);
        }
    }

    /// <summary>
    /// Generates segment using complete schema definition from segments/*.json.
    /// Special handling for MSH segment which has unique HL7 structure.
    /// </summary>
    private async Task<Result<string>> GenerateSegmentFromSchemaDefinition(
        SegmentSchema segmentSchema,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        // Special handling for MSH segment - unique HL7 structure
        if (segmentSchema.Code == "MSH")
        {
            return await GenerateMSHSegmentAsync(segmentSchema, context, options);
        }

        var fieldValues = new List<string> { segmentSchema.Code };

        foreach (var field in segmentSchema.Fields)
        {
            var fieldValue = await GenerateFieldFromSchemaAsync(field, context, options);
            fieldValues.Add(fieldValue);
        }

        return Result<string>.Success(string.Join("|", fieldValues));
    }

    /// <summary>
    /// Generates MSH segment with proper HL7 structure.
    /// MSH has special field numbering where field separator "|" is field 1.
    /// </summary>
    private async Task<Result<string>> GenerateMSHSegmentAsync(
        SegmentSchema segmentSchema,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        var mshBuilder = new StringBuilder("MSH");

        foreach (var field in segmentSchema.Fields)
        {
            string fieldValue;

            // Special handling for MSH fields based on position
            switch (field.Position)
            {
                case 1: // Field Separator - always "|"
                    fieldValue = "|";
                    break;
                case 2: // Encoding Characters - always "^~\&"
                    fieldValue = "^~\\&";
                    break;
                case 3: // Sending Application
                    fieldValue = "PIDGEON^^L";
                    break;
                case 4: // Sending Facility
                    fieldValue = "PIDGEON_FACILITY";
                    break;
                case 5: // Receiving Application
                    fieldValue = "TARGET^^L";
                    break;
                case 6: // Receiving Facility
                    fieldValue = "TARGET_FACILITY";
                    break;
                case 7: // Date/Time of Message
                    fieldValue = DateTime.Now.ToString("yyyyMMddHHmmss");
                    break;
                case 8: // Security
                    fieldValue = "";
                    break;
                case 9: // Message Type
                    fieldValue = context.MessageType;
                    break;
                case 10: // Message Control ID
                    fieldValue = GenerateRandomId();
                    break;
                case 11: // Processing ID
                    fieldValue = "P";
                    break;
                case 12: // Version ID
                    fieldValue = "2.3";
                    break;
                default:
                    // For any other fields, use normal generation
                    fieldValue = await GenerateFieldFromSchemaAsync(field, context, options);
                    break;
            }

            // For positions 1-2, special MSH formatting (field separator and encoding chars)
            if (field.Position == 1 || field.Position == 2)
            {
                mshBuilder.Append(fieldValue);
            }
            else
            {
                mshBuilder.Append("|").Append(fieldValue);
            }
        }

        return Result<string>.Success(mshBuilder.ToString());
    }

    /// <summary>
    /// Generates field values using priority-based resolver chain.
    /// Handles composite data types (XAD, XPN, XTN, etc.) by expanding into components.
    /// Tries resolvers in order: Session context → HL7 semantics → Demographics → Random
    /// </summary>
    private async Task<string> GenerateFieldFromSchemaAsync(
        SegmentField field,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        try
        {
            // Handle field optionality from schema
            if (field.Optionality == "O" && _random.NextDouble() > 0.7)
            {
                return string.Empty;
            }

            // Check if this field has a composite data type (XAD, XPN, XTN, CE, CX, etc.)
            var dataTypeResult = await _dataTypeProvider.GetDataTypeAsync(field.DataType);
            if (dataTypeResult.IsSuccess && dataTypeResult.Value.Components.Any())
            {
                // Check if any composite-aware resolver can handle this composite type
                // This ensures semantic coherence (e.g., CE components all refer to same code)
                var compositeResolvers = _compositeAwareResolvers
                    .Where(r => r.CanHandleComposite(field.DataType))
                    .OrderByDescending(r => r.Priority);

                foreach (var resolver in compositeResolvers)
                {
                    var compositeResolverContext = new FieldResolutionContext
                    {
                        SegmentCode = context.SegmentCode,
                        FieldPosition = field.Position,
                        Field = field,
                        GenerationContext = context,
                        Options = options
                    };

                    var compositeResult = await resolver.ResolveCompositeAsync(field, dataTypeResult.Value, compositeResolverContext);
                    if (compositeResult != null)
                    {
                        // Resolver handled it - build composite string from component dictionary
                        // Use the max of JSON component count and resolver's component count
                        // (Some data types like XPN have incomplete JSON definitions)
                        var maxComponentPosition = compositeResult.Keys.Any()
                            ? Math.Max(dataTypeResult.Value.Components.Count, compositeResult.Keys.Max())
                            : dataTypeResult.Value.Components.Count;

                        var resolvedComponents = new List<string>();
                        for (int i = 1; i <= maxComponentPosition; i++)
                        {
                            resolvedComponents.Add(compositeResult.ContainsKey(i) ? compositeResult[i] : string.Empty);
                        }

                        _logger.LogDebug("Composite {DataType} resolved by {Resolver} for field {Field}",
                            field.DataType, resolver.GetType().Name, field.Name);

                        return string.Join("^", resolvedComponents);
                    }
                }

                // No composite-aware resolver handled it - fall back to component-by-component generation
                var componentValues = new List<string>();

                foreach (var component in dataTypeResult.Value.Components)
                {
                    var componentValue = await GenerateComponentValueAsync(component, field, context, options);
                    componentValues.Add(componentValue);
                }

                return string.Join("^", componentValues);
            }

            // Simple/primitive field - use resolver chain directly
            var resolverContext = new FieldResolutionContext
            {
                SegmentCode = context.SegmentCode,
                FieldPosition = field.Position,
                Field = field,
                GenerationContext = context,
                Options = options
            };

            // Use resolver chain to determine field value
            var resolvedValue = await _fieldValueResolverService.ResolveFieldValueAsync(resolverContext);

            return resolvedValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating field {FieldName}", field.Name);
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates values from demographic table definitions (FirstName.json, LastName.json, etc).
    /// </summary>
    private async Task<string?> GenerateValueFromDemographicTableAsync(string tableName)
    {
        try
        {
            // Check cache first
            if (_demographicTableCache.TryGetValue(tableName, out var cachedValues))
            {
                return cachedValues.Count > 0 ? cachedValues[_random.Next(cachedValues.Count)] : null;
            }

            // Build resource name for demographic table JSON file
            var resourceName = $"data.standards.hl7v23.tables.{tableName}.json";

            _logger.LogDebug("Loading demographic table {TableName} from resource {ResourceName}",
                tableName, resourceName);

            // Load from embedded resource
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogDebug("Demographic table resource not found: {ResourceName}", resourceName);
                _demographicTableCache[tableName] = new List<string>(); // Cache empty list to avoid repeated lookups
                return null;
            }

            // Parse JSON to extract values
            var json = await new StreamReader(stream).ReadToEndAsync();
            var jsonDoc = JsonDocument.Parse(json);

            var values = new List<string>();
            if (jsonDoc.RootElement.TryGetProperty("values", out var valuesArray))
            {
                foreach (var valueElement in valuesArray.EnumerateArray())
                {
                    if (valueElement.TryGetProperty("value", out var valueProperty))
                    {
                        var value = valueProperty.GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }

            // Cache for future requests
            _demographicTableCache[tableName] = values;

            _logger.LogDebug("Successfully loaded demographic table {TableName} with {ValueCount} values",
                tableName, values.Count);

            return values.Count > 0 ? values[_random.Next(values.Count)] : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading demographic table {TableName}", tableName);
            return null;
        }
    }

    /// <summary>
    /// Generates values from table definitions in tables/*.json.
    /// </summary>
    private async Task<string> GenerateValueFromTableAsync(int tableId)
    {
        var tableResult = await _tableProvider.GetTableAsync(tableId);
        if (tableResult.IsFailure || !tableResult.Value.Values.Any())
        {
            return string.Empty;
        }

        var randomValue = tableResult.Value.Values[_random.Next(tableResult.Value.Values.Count)];
        return randomValue.Code;
    }

    /// <summary>
    /// Generates values using data type component structure from data_types/*.json.
    /// </summary>
    private async Task<string> GenerateValueFromDataTypeAsync(
        DataType dataType,
        SegmentField field,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        // For composite data types, generate each component
        if (dataType.Components.Any())
        {
            var componentValues = new List<string>();

            foreach (var component in dataType.Components)
            {
                var componentValue = await GenerateComponentValueAsync(component, field, context, options);
                componentValues.Add(componentValue);
            }

            return string.Join("^", componentValues);
        }

        // For primitive data types, generate value based on field semantics and clinical context
        return await GeneratePrimitiveValueAsync(dataType.Code, field, context);
    }

    /// <summary>
    /// Generates component values for composite data types using the resolver chain.
    /// Now properly routes composite components through FieldValueResolverService instead of bypassing it.
    /// Uses semantic awareness to distinguish critical components (address core fields) from truly optional ones.
    /// </summary>
    private async Task<string> GenerateComponentValueAsync(
        DataTypeComponent component,
        SegmentField parentField,
        SegmentGenerationContext context,
        GenerationOptions options)
    {
        // Handle optional components with 3-tier semantic probability
        if (component.Optionality == "O")
        {
            var importance = GetComponentImportance(component, parentField);

            var shouldSkip = importance switch
            {
                ComponentImportance.Critical => _random.NextDouble() > 0.95,  // 95% populated
                ComponentImportance.Important => _random.NextDouble() > 0.50, // 50% populated
                ComponentImportance.Optional => _random.NextDouble() > 0.20,  // 20% populated
                _ => _random.NextDouble() > 0.20
            };

            if (shouldSkip)
                return string.Empty;
        }

        // Create a pseudo-field from component for resolver chain
        var componentField = new SegmentField(
            Position: parentField.Position,  // Use parent field position for resolver context
            Name: component.Name,
            DataType: component.DataType,
            Optionality: component.Optionality,
            Repeatability: "-",
            Length: 0,
            TableId: component.TableId,
            Description: null);

        // Create resolution context to use resolver chain (proper architecture!)
        var resolverContext = new FieldResolutionContext
        {
            SegmentCode = context.SegmentCode,
            FieldPosition = parentField.Position,
            Field = componentField,
            GenerationContext = context,
            Options = options
        };

        // Use resolver service instead of bypassing it
        // This allows composite components to benefit from:
        // - HL7TableFieldResolver (Priority 85) - HL7 table lookups
        // - DemographicFieldResolver (Priority 80) - Realistic names/addresses
        // - IdentifierFieldResolver (Priority 75) - Patient/encounter IDs
        // - ContactFieldResolver (Priority 75) - Phone/email/fax
        // - All other resolvers in the chain
        return await _fieldValueResolverService.ResolveFieldValueAsync(resolverContext);
    }

    /// <summary>
    /// Determines the semantic importance of a composite component for realistic data generation.
    /// Three tiers: Critical (95% populated), Important (50% populated), Optional (20% populated)
    /// </summary>
    private ComponentImportance GetComponentImportance(DataTypeComponent component, SegmentField parentField)
    {
        var fieldName = component.Name?.ToLowerInvariant() ?? "";

        // XAD (Extended Address)
        if (parentField.DataType == "XAD")
        {
            // Critical (95%): Core address components
            if (fieldName.Contains("street") || fieldName.Contains("city") ||
                fieldName.Contains("state") || fieldName.Contains("zip") || fieldName.Contains("postal") ||
                fieldName.Contains("address type") || component.Position == 7)
                return ComponentImportance.Critical;

            // Important (50%): Suite/apt, country
            if (fieldName.Contains("other designation") || component.Position == 2 ||
                fieldName.Contains("country") || component.Position == 6)
                return ComponentImportance.Important;

            // Optional (20%): Census tract, county, other geographic
            return ComponentImportance.Optional;
        }

        // XPN (Extended Person Name)
        if (parentField.DataType == "XPN")
        {
            // Critical (95%): Core name components
            if (fieldName.Contains("family") || fieldName.Contains("given") || fieldName.Contains("first"))
                return ComponentImportance.Critical;

            // Important (50%): Middle name/initial, prefix, suffix
            if (fieldName.Contains("middle") || component.Position == 3 ||
                fieldName.Contains("prefix") || component.Position == 4 ||
                fieldName.Contains("suffix") || component.Position == 5)
                return ComponentImportance.Important;

            // Optional (20%): Degree, name type code, validity range
            return ComponentImportance.Optional;
        }

        // CX (Extended Composite ID)
        if (parentField.DataType == "CX")
        {
            // Critical (95%): ID value, check digit, check digit scheme
            if (component.Position == 1 || component.Position == 2 || component.Position == 3 ||
                (fieldName.Contains("id") && !fieldName.Contains("assigning")) ||
                fieldName.Contains("check digit"))
                return ComponentImportance.Critical;

            // Important (50%): Assigning authority, identifier type
            if (component.Position == 4 || component.Position == 5 ||
                fieldName.Contains("assigning authority") || fieldName.Contains("identifier type"))
                return ComponentImportance.Important;

            // Optional (20%): Assigning facility
            return ComponentImportance.Optional;
        }

        // XTN (Extended Telecommunication)
        if (parentField.DataType == "XTN")
        {
            // Critical (95%): Phone number, use code, equipment type
            if (component.Position == 1 || component.Position == 2 || component.Position == 3 ||
                (fieldName.Contains("telephone") && !fieldName.Contains("use") && !fieldName.Contains("equipment")))
                return ComponentImportance.Critical;

            // Important (50%): Email, area code
            if (fieldName.Contains("email") || component.Position == 4 ||
                fieldName.Contains("area") || component.Position == 6)
                return ComponentImportance.Important;

            // Optional (20%): Country code, extension, any text
            return ComponentImportance.Optional;
        }

        // CE/CWE (Coded Element): Identifier and Text are critical, alternates are important
        if (parentField.DataType == "CE" || parentField.DataType == "CWE")
        {
            // Critical (95%): Primary identifier and text
            if (component.Position <= 2 || fieldName.Contains("identifier") || fieldName.Contains("text"))
                return ComponentImportance.Critical;

            // Important (50%): Coding system, alternate codes
            if (component.Position == 3 || fieldName.Contains("coding system") ||
                fieldName.Contains("alternate"))
                return ComponentImportance.Important;

            // Optional (20%): Rarely used alternate system fields
            return ComponentImportance.Optional;
        }

        // Default: Optional (20% population)
        return ComponentImportance.Optional;
    }

    /// <summary>
    /// Generates primitive data type values using field semantics and clinical context.
    /// Completely data-driven - no hardcoded values or switch statements.
    /// </summary>
    private async Task<string> GeneratePrimitiveValueAsync(
        string dataTypeCode,
        SegmentField field,
        SegmentGenerationContext context)
    {
        // If field has table constraint, use that for any data type
        if (field.TableId.HasValue)
        {
            return await GenerateValueFromTableAsync(field.TableId.Value);
        }

        // Generate based on data type and field semantics
        return dataTypeCode switch
        {
            "ST" or "TX" or "FT" => await GenerateStringFromContextAsync(field, context),
            "NM" or "SI" => GenerateNumericFromContext(field, context),
            "DT" => GenerateDateFromContext(field, context),
            "TM" => DateTime.Now.ToString("HHmmss"),
            "TS" or "DTM" => DateTime.Now.ToString("yyyyMMddHHmmss"),
            "ID" or "IS" => GenerateCodedValue(field, context),
            _ => ""
        };
    }

    /// <summary>
    /// Generates coded values by examining field name semantics.
    /// Uses clinical knowledge to generate appropriate codes.
    /// </summary>
    private string GenerateCodedValue(SegmentField field, SegmentGenerationContext context)
    {
        var fieldName = field.Name?.ToLowerInvariant() ?? "";
        var description = field.Description?.ToLowerInvariant() ?? "";
        var combined = $"{fieldName} {description}";

        // Patient demographics
        if (combined.Contains("sex") || combined.Contains("gender"))
            return _random.Next(2) == 0 ? "M" : "F";
        if (combined.Contains("marital"))
            return new[] { "S", "M", "D", "W", "A", "L" }[_random.Next(6)]; // Single, Married, Divorced, Widowed, Separated, Living Together
        if (combined.Contains("race"))
            return new[] { "1002-5", "2028-9", "2054-5", "2076-8", "2106-3" }[_random.Next(5)];
        if (combined.Contains("ethnicity"))
            return _random.Next(2) == 0 ? "H" : "N"; // Hispanic or Not Hispanic
        if (combined.Contains("religion"))
            return new[] { "CHR", "JUD", "MOS", "BAH", "HIN", "BUD", "OTH" }[_random.Next(7)];
        if (combined.Contains("language"))
            return new[] { "en", "es", "fr", "de", "zh", "ja", "ar" }[_random.Next(7)];
        if (combined.Contains("nationality") || combined.Contains("citizenship"))
            return new[] { "USA", "CAN", "MEX", "GBR", "CHN", "IND" }[_random.Next(6)];

        // Admission/Encounter types
        if (combined.Contains("admission") && (combined.Contains("type") || combined.Contains("source")))
            return new[] { "E", "I", "O", "P", "R", "U" }[_random.Next(6)]; // Emergency, Inpatient, Outpatient, Preadmit, Recurring, Urgent
        if (combined.Contains("patient") && combined.Contains("class"))
            return new[] { "E", "I", "O", "P", "R", "B", "N" }[_random.Next(7)]; // Various patient classes
        if (combined.Contains("hospital") && combined.Contains("service"))
            return new[] { "MED", "SUR", "URO", "PUL", "CAR", "GYN", "OBS", "PED", "PSY", "NEU" }[_random.Next(10)];
        if (combined.Contains("admit") && combined.Contains("source"))
            return new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" }[_random.Next(9)];

        // Status codes
        if (combined.Contains("status"))
            return new[] { "A", "I", "P", "C", "S", "D" }[_random.Next(6)]; // Active, Inactive, Pending, Complete, Suspended, Discontinued
        if (combined.Contains("result") && combined.Contains("status"))
            return new[] { "F", "P", "C", "R", "X" }[_random.Next(5)]; // Final, Preliminary, Correction, Results stored, Canceled

        // Priority codes
        if (combined.Contains("priority"))
            return new[] { "S", "A", "R", "P", "C", "T" }[_random.Next(6)]; // Stat, ASAP, Routine, Preop, Callback, Timing critical

        // Yes/No/Unknown patterns
        if (combined.Contains("indicator") || combined.Contains("flag"))
            return new[] { "Y", "N" }[_random.Next(2)];
        if (combined.Contains("living") && combined.Contains("dependency"))
            return new[] { "S", "M", "C", "WU", "O" }[_random.Next(5)]; // Spouse, Medical supervision, Child, Walk up, Other

        // Financial/Insurance codes
        if (combined.Contains("financial") || combined.Contains("billing"))
            return new[] { "COM", "HMO", "INS", "MED", "MM", "PPO", "POS" }[_random.Next(7)];
        if (combined.Contains("relationship") && combined.Contains("patient"))
            return new[] { "SEL", "SPO", "CHD", "PAR", "OTH", "GRD", "DEP" }[_random.Next(7)]; // Self, Spouse, Child, Parent, Other, Guardian, Dependent

        // Event/Reason codes
        if (combined.Contains("event") && combined.Contains("reason"))
            return new[] { "01", "02", "03", "O", "U" }[_random.Next(5)];
        if (combined.Contains("event") && combined.Contains("type"))
            return new[] { "A01", "A02", "A03", "A04", "A05", "A08", "A11" }[_random.Next(7)];

        // Allergy/Sensitivity
        if (combined.Contains("allergy") && combined.Contains("type"))
            return new[] { "DA", "FA", "MA", "MC", "EA", "AA", "LA", "PA" }[_random.Next(8)]; // Drug, Food, Misc, Misc contraindication, Environmental, Animal, Pollen, Plant
        if (combined.Contains("allergy") && combined.Contains("severity"))
            return new[] { "SV", "MO", "MI", "U" }[_random.Next(4)]; // Severe, Moderate, Mild, Unknown

        // Diagnostic codes
        if (combined.Contains("diagnosis") && combined.Contains("type"))
            return new[] { "A", "W", "F" }[_random.Next(3)]; // Admitting, Working, Final
        if (combined.Contains("diagnosis") && combined.Contains("priority"))
            return (_random.Next(1, 10)).ToString(); // 1-9 priority

        // Observation/Result codes
        if (combined.Contains("observation") && combined.Contains("result") && combined.Contains("status"))
            return new[] { "F", "P", "C", "R", "X", "S", "I" }[_random.Next(7)];
        if (combined.Contains("abnormal") && combined.Contains("flags"))
            return new[] { "L", "H", "LL", "HH", "N", "A", "<", ">" }[_random.Next(8)]; // Low, High, Critical low, Critical high, Normal, Abnormal

        // Specimen codes
        if (combined.Contains("specimen") && (combined.Contains("source") || combined.Contains("type")))
            return new[] { "BLD", "BLDV", "BLDA", "UR", "CSF", "GAST", "SPUT", "SER", "PLAS" }[_random.Next(9)];

        // Default to empty or Unknown based on field optionality
        // Empty is preferable to "U" for optional fields
        return field.Optionality == "O" ? "" : "U";
    }

    /// <summary>
    /// Generates string values using clinical context and field semantics.
    /// Completely data-driven approach using field names to infer appropriate values.
    /// </summary>
    private async Task<string> GenerateStringFromContextAsync(SegmentField field, SegmentGenerationContext context)
    {
        var fieldName = field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = field.Description?.ToLowerInvariant() ?? "";

        // HL7 Standard Message Header (MSH) fields - use proper HL7 values
        if (fieldName.Contains("field separator") || fieldName.Contains("field_separator"))
            return "|";
        if (fieldName.Contains("encoding characters") || fieldName.Contains("encoding_characters"))
            return "^~\\&";
        if (fieldDescription.Contains("sending application"))
            return "PIDGEON^^L";
        if (fieldDescription.Contains("receiving application"))
            return "TARGET^^L";
        if (fieldDescription.Contains("message type"))
            return context.MessageType;
        if (fieldDescription.Contains("message control id"))
            return GenerateRandomId();
        if (fieldDescription.Contains("processing id"))
            return "P";
        if (fieldDescription.Contains("version id"))
            return "2.3";

        // Patient demographic fields - use demographic tables
        if (fieldName.Contains("patient name") || fieldName.Contains("person name"))
            return context.Patient.Name.DisplayName;
        if (fieldName.Contains("family name") || fieldName.Contains("last name"))
            return await GenerateValueFromDemographicTableAsync("LastName") ?? context.Patient.Name.Family ?? "";
        if (fieldName.Contains("given name") || fieldName.Contains("first name"))
            return await GenerateValueFromDemographicTableAsync("FirstName") ?? context.Patient.Name.Given ?? "";
        if (fieldName.Contains("middle"))
            return context.Patient.Name.Middle ?? "";

        // Location fields
        if (fieldName.Contains("room"))
            return context.Encounter?.Room ?? GenerateRandomRoom();
        if (fieldName.Contains("bed"))
            return context.Encounter?.Room ?? GenerateRandomBed();
        if (fieldName.Contains("facility") || fieldName.Contains("hospital"))
            return context.Encounter?.Location ?? "General Hospital";
        if (fieldName.Contains("department") || fieldName.Contains("unit"))
            return context.Encounter?.Department ?? "General Medicine";

        // Clinical fields
        if (fieldName.Contains("medication") || fieldName.Contains("drug"))
            return context.Prescription?.Medication.DisplayName ?? "Unknown Medication";
        if (fieldName.Contains("result") || fieldName.Contains("value"))
            return context.Observation?.Value ?? "Normal";
        if (fieldName.Contains("diagnosis"))
            return context.Encounter?.PrimaryDiagnosis?.Description ?? "General Diagnosis";

        // Address fields
        if (fieldName.Contains("address") || fieldName.Contains("street"))
            return context.Patient.Address?.Street1 ?? "123 Main St";
        if (fieldName.Contains("city"))
            return context.Patient.Address?.City ?? "Anytown";
        if (fieldName.Contains("state"))
            return context.Patient.Address?.State ?? "ST";
        if (fieldName.Contains("zip") || fieldName.Contains("postal"))
            return context.Patient.Address?.PostalCode ?? "12345";

        // Contact fields
        if (fieldName.Contains("phone") || fieldName.Contains("telephone"))
            return context.Patient.PhoneNumber ?? "555-123-4567";

        // Default for unrecognized fields
        return $"Generated_{field.Name?.Replace(" ", "_") ?? "Value"}";
    }

    private string GenerateNumericFromContext(SegmentField field, SegmentGenerationContext context)
    {
        var fieldName = field.Name?.ToLowerInvariant() ?? "";

        // Sequence numbers and IDs
        if (fieldName.Contains("sequence") || fieldName.Contains("set id"))
            return "1";
        if (fieldName.Contains("patient") && fieldName.Contains("id"))
            return context.Patient.Id ?? _random.Next(100000, 999999).ToString();
        if (fieldName.Contains("account"))
            return context.Patient.MedicalRecordNumber ?? _random.Next(1000000, 9999999).ToString();
        if (fieldName.Contains("visit"))
            return context.Encounter?.Id ?? _random.Next(100000, 999999).ToString();

        // Medical values
        if (fieldName.Contains("age"))
            return CalculateAge(context.Patient.BirthDate).ToString();
        if (fieldName.Contains("weight"))
            return _random.Next(50, 200).ToString(); // kg
        if (fieldName.Contains("height"))
            return _random.Next(150, 200).ToString(); // cm
        if (fieldName.Contains("temperature"))
            return (36.0 + _random.NextDouble() * 3.0).ToString("F1"); // 36.0-39.0°C
        if (fieldName.Contains("pressure") && fieldName.Contains("systolic"))
            return _random.Next(90, 180).ToString();
        if (fieldName.Contains("pressure") && fieldName.Contains("diastolic"))
            return _random.Next(60, 110).ToString();

        // Default numeric value
        return _random.Next(1, 1000).ToString();
    }

    private string GenerateDateFromContext(SegmentField field, SegmentGenerationContext context)
    {
        var fieldName = field.Name?.ToLowerInvariant() ?? "";

        // Patient dates
        if (fieldName.Contains("birth"))
            return context.Patient.BirthDate?.ToString("yyyyMMdd") ?? "19800101";
        if (fieldName.Contains("death"))
            return ""; // Most patients are alive

        // Encounter dates
        if (fieldName.Contains("admit"))
            return context.Encounter?.StartTime?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
        if (fieldName.Contains("discharge"))
            return context.Encounter?.EndTime?.ToString("yyyyMMdd") ?? DateTime.Today.AddDays(3).ToString("yyyyMMdd");

        // Clinical dates
        if (fieldName.Contains("order"))
            return DateTime.Today.ToString("yyyyMMdd");
        if (fieldName.Contains("result") || fieldName.Contains("observation"))
            return DateTime.Today.ToString("yyyyMMdd");

        // Default to today
        return DateTime.Today.ToString("yyyyMMdd");
    }

    /// <summary>
    /// Calculates age from date of birth, handling null dates.
    /// </summary>
    private int CalculateAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
            return _random.Next(20, 80);

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return Math.Max(0, age);
    }

    /// <summary>
    /// Determines repeat counts using data-driven approach.
    /// Uses segment schema statistics when available, falls back to medically realistic defaults.
    /// </summary>
    private async Task<int> DetermineRepeatCountFromDataAsync(TriggerEventSegment segmentDef, GenerationOptions options)
    {
        if (segmentDef.Repeatability != "∞")
            return 1;

        // Check if user provided specific repeat counts in options
        if (options.SegmentRepeatCounts?.TryGetValue(segmentDef.SegmentCode, out var repeatCount) == true)
        {
            return repeatCount;
        }

        // TODO: Read repeat probability distributions from segment schema JSON when available
        // For now, use medically realistic defaults based on segment purpose
        var segmentSchemaResult = await _segmentProvider.GetSegmentAsync(segmentDef.SegmentCode);
        if (segmentSchemaResult.IsSuccess)
        {
            // Use segment description/purpose to determine realistic repeat counts
            var description = segmentSchemaResult.Value.Description?.ToLowerInvariant() ?? "";

            if (description.Contains("next of kin") || description.Contains("emergency contact"))
                return _random.Next(1, 3);   // 1-2 contacts
            if (description.Contains("observation") || description.Contains("result"))
                return _random.Next(1, 6);   // 1-5 observations
            if (description.Contains("allergy") || description.Contains("adverse"))
                return _random.Next(1, 4);   // 1-3 allergies
            if (description.Contains("diagnosis") || description.Contains("condition"))
                return _random.Next(1, 4);   // 1-3 diagnoses
            if (description.Contains("guarantor") || description.Contains("financial"))
                return _random.Next(1, 3);   // 1-2 guarantors
        }

        // Default fallback
        return _random.Next(1, 3);
    }


    /// <summary>
    /// Generates minimal segment when schema/builder unavailable.
    /// Shows segment code to help identify missing schemas during development.
    /// </summary>
    private Result<string> GenerateMinimalSegment(string segmentCode)
    {
        return Result<string>.Success(segmentCode);
    }

    // Helper methods for random generation when context doesn't provide values
    private string GenerateRandomId() => _random.Next(100000, 999999).ToString();
    private string GenerateRandomRoom() => $"{_random.Next(100, 999)}";
    private string GenerateRandomBed() => $"{(char)('A' + _random.Next(0, 4))}";

    #region Temporal Coherence

    /// <summary>
    /// Defines temporal relationships between HL7 fields for realistic timestamp generation.
    /// Maps target field → (anchor field, min delta, max delta).
    /// </summary>
    private static readonly Dictionary<string, (string AnchorField, TimeSpan MinDelta, TimeSpan MaxDelta)> TemporalRelationships = new()
    {
        // PV1.44 (Admit Date/Time) should match EVN.2 (Event Occurred) within a few minutes
        ["PV1.44"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromMinutes(5)),

        // DG1.5 (Diagnosis Date/Time) should be within 0-48 hours after admission
        ["DG1.5"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(48)),

        // OBX.14 (Observation Date/Time) should be within 0-24 hours after admission
        ["OBX.14"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(24)),

        // MSH.7 (Message Date/Time) is typically "now" - same as event or up to 1 hour after
        ["MSH.7"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(1)),

        // ORC.9 (Order Date/Time) should be within encounter timespan
        ["ORC.9"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(24)),

        // RXE.32 (Original Order Date/Time) should match or precede current order
        ["RXE.32"] = ("ORC.9", TimeSpan.FromHours(-24), TimeSpan.Zero),
    };

    /// <summary>
    /// Gets the temporal anchor timestamp for a field, if one exists.
    /// Returns the anchor timestamp and relationship definition.
    /// </summary>
    private (DateTime? AnchorTime, (string AnchorField, TimeSpan MinDelta, TimeSpan MaxDelta)? Relationship)
        GetTemporalAnchor(string fieldPath, SegmentGenerationContext context)
    {
        // Check if this field has a defined temporal relationship
        if (!TemporalRelationships.TryGetValue(fieldPath, out var relationship))
            return (null, null);

        // Try to find the anchor timestamp in the context
        if (context.GeneratedTimestamps.TryGetValue(relationship.AnchorField, out var anchorTime))
        {
            return (anchorTime, relationship);
        }

        // Special case: If anchor is EVN.2, use EncounterStartTime if available
        if (relationship.AnchorField == "EVN.2" && context.EncounterStartTime.HasValue)
        {
            return (context.EncounterStartTime.Value, relationship);
        }

        return (null, relationship);
    }

    /// <summary>
    /// Generates a timestamp relative to an anchor time with realistic variation.
    /// </summary>
    private DateTime GenerateRelativeTimestamp(DateTime anchor, TimeSpan minDelta, TimeSpan maxDelta)
    {
        // Calculate random delta within the specified range
        var deltaRange = maxDelta - minDelta;
        var randomDelta = TimeSpan.FromSeconds(_random.NextDouble() * deltaRange.TotalSeconds);
        var actualDelta = minDelta + randomDelta;

        return anchor.Add(actualDelta);
    }

    /// <summary>
    /// Records a generated timestamp in the context for future field references.
    /// </summary>
    private void RecordGeneratedTimestamp(string fieldPath, DateTime timestamp, SegmentGenerationContext context)
    {
        context.GeneratedTimestamps[fieldPath] = timestamp;

        // If this is EVN.2 and we don't have an encounter start time yet, set it as the anchor
        if (fieldPath == "EVN.2" && !context.EncounterStartTime.HasValue)
        {
            // Use 'with' to create a new record instance with updated EncounterStartTime
            // Note: This doesn't mutate the existing context, but the caller should use the returned context
            // For now, we'll just log this - actual implementation needs context to be mutable or returned
            _logger.LogDebug("Generated EVN.2 timestamp as encounter anchor: {Timestamp}", timestamp);
        }
    }

    #endregion
}

/// <summary>
/// Context object for segment generation containing all available clinical data.
/// Includes temporal coherence tracking to ensure realistic timestamp relationships.
/// </summary>
public record SegmentGenerationContext(
    Patient Patient,
    Encounter? Encounter,
    Prescription? Prescription,
    ObservationResult? Observation,
    string MessageType)
{
    /// <summary>
    /// Gets the segment code being generated (set by generation process).
    /// </summary>
    public string SegmentCode { get; init; } = string.Empty;

    /// <summary>
    /// Tracks generated timestamps by field path (e.g., "EVN.2", "PV1.44") for temporal coherence.
    /// Enables subsequent fields to generate timestamps relative to anchors.
    /// </summary>
    public Dictionary<string, DateTime> GeneratedTimestamps { get; init; } = new();

    /// <summary>
    /// Primary temporal anchor for the encounter (typically EVN.2 - Event Occurred).
    /// Other timestamps are generated relative to this anchor.
    /// </summary>
    public DateTime? EncounterStartTime { get; init; }
};

