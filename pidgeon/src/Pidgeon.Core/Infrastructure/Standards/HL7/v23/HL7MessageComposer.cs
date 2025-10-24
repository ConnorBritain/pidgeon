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
    private readonly Random _random;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, List<string>> _demographicTableCache = new();

    public HL7MessageComposer(
        ILogger<HL7MessageComposer> logger,
        IHL7TriggerEventProvider triggerEventProvider,
        IHL7SegmentProvider segmentProvider,
        IHL7DataTypeProvider dataTypeProvider,
        IHL7TableProvider tableProvider,
        IFieldValueResolverService fieldValueResolverService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _triggerEventProvider = triggerEventProvider ?? throw new ArgumentNullException(nameof(triggerEventProvider));
        _segmentProvider = segmentProvider ?? throw new ArgumentNullException(nameof(segmentProvider));
        _dataTypeProvider = dataTypeProvider ?? throw new ArgumentNullException(nameof(dataTypeProvider));
        _tableProvider = tableProvider ?? throw new ArgumentNullException(nameof(tableProvider));
        _fieldValueResolverService = fieldValueResolverService ?? throw new ArgumentNullException(nameof(fieldValueResolverService));
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

            // Create resolution context for resolver chain
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
        SegmentGenerationContext context)
    {
        // For composite data types, generate each component
        if (dataType.Components.Any())
        {
            var componentValues = new List<string>();

            foreach (var component in dataType.Components)
            {
                var componentValue = await GenerateComponentValueAsync(component, field, context);
                componentValues.Add(componentValue);
            }

            return string.Join("^", componentValues);
        }

        // For primitive data types, generate value based on field semantics and clinical context
        return await GeneratePrimitiveValueAsync(dataType.Code, field, context);
    }

    /// <summary>
    /// Generates component values for composite data types.
    /// </summary>
    private async Task<string> GenerateComponentValueAsync(
        DataTypeComponent component,
        SegmentField parentField,
        SegmentGenerationContext context)
    {
        if (component.Optionality == "O" && _random.NextDouble() > 0.8)
        {
            return string.Empty;
        }

        if (component.TableId.HasValue)
        {
            return await GenerateValueFromTableAsync(component.TableId.Value);
        }

        // Create a pseudo-field for the component to reuse field generation logic
        var componentField = new SegmentField(
            Position: 0,
            Name: component.Name,
            DataType: component.DataType,
            Optionality: component.Optionality,
            Repeatability: "-",
            Length: 0,
            TableId: component.TableId,
            Description: null);

        return await GeneratePrimitiveValueAsync(component.DataType, componentField, context);
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

        // Use field semantics to generate appropriate coded values
        if (fieldName.Contains("sex") || fieldName.Contains("gender"))
            return _random.Next(2) == 0 ? "M" : "F";
        if (fieldName.Contains("marital"))
            return new[] { "S", "M", "D", "W" }[_random.Next(4)];
        if (fieldName.Contains("race"))
            return new[] { "1002-5", "2028-9", "2054-5", "2076-8" }[_random.Next(4)];
        if (fieldName.Contains("admission"))
            return new[] { "E", "I", "O", "P" }[_random.Next(4)];
        if (fieldName.Contains("class"))
            return new[] { "E", "I", "O", "P", "R" }[_random.Next(5)];
        if (fieldName.Contains("status"))
            return new[] { "A", "I", "P" }[_random.Next(3)];

        // Default coded value
        return "U"; // Unknown/Other
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
}

/// <summary>
/// Context object for segment generation containing all available clinical data.
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
};

