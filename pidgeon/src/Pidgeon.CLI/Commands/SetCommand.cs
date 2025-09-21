// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for setting field values in lock sessions.
/// Enables granular control over specific field values for workflow consistency.
/// </summary>
public class SetCommand : CommandBuilderBase
{
    private readonly ILockSessionService _lockSessionService;
    private readonly IFieldPathResolver _fieldPathResolver;

    public SetCommand(
        ILogger<SetCommand> logger,
        ILockSessionService lockSessionService,
        IFieldPathResolver fieldPathResolver)
        : base(logger)
    {
        _lockSessionService = lockSessionService;
        _fieldPathResolver = fieldPathResolver;
    }

    public override Command CreateCommand()
    {
        var command = new Command("set", "Set field values in a lock session for consistent message generation");

        // Arguments
        var sessionArg = new Argument<string>("session")
        {
            Description = "Name of the lock session"
        };

        var fieldArg = new Argument<string>("field")
        {
            Description = "Field path to set (e.g., patient.mrn, PID.3.1, Patient.identifier[0].value)"
        };

        var valueArg = new Argument<string>("value")
        {
            Description = "Value to set for the field"
        };

        // Options
        var reasonOption = CreateNullableOption("--reason", "Reason for setting this value");
        var listOption = CreateBooleanOption("--list", "List current values in the session instead of setting");
        var removeOption = CreateBooleanOption("--remove", "Remove the specified field value from the session");

        command.Add(sessionArg);
        command.Add(fieldArg);
        command.Add(valueArg);
        command.Add(reasonOption);
        command.Add(listOption);
        command.Add(removeOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var sessionName = parseResult.GetValue(sessionArg)!;
                var fieldPath = parseResult.GetValue(fieldArg)!;
                var value = parseResult.GetValue(valueArg);
                var reason = parseResult.GetValue(reasonOption);
                var list = parseResult.GetValue(listOption);
                var remove = parseResult.GetValue(removeOption);

                // Handle list operation
                if (list)
                {
                    return await HandleListValues(sessionName, cancellationToken);
                }

                // Handle remove operation
                if (remove)
                {
                    return await HandleRemoveValue(sessionName, fieldPath, cancellationToken);
                }

                // Handle set operation
                if (string.IsNullOrWhiteSpace(value))
                {
                    Console.WriteLine("❌ Value is required when setting a field");
                    Console.WriteLine("Usage: pidgeon set <session> <field> <value>");
                    return 1;
                }

                return await HandleSetValue(sessionName, fieldPath, value, reason, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error in set command");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task<int> HandleSetValue(
        string sessionName,
        string fieldPath,
        string value,
        string? reason,
        CancellationToken cancellationToken)
    {
        // Validate that session exists
        var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ {sessionResult.Error.Message}");
            Console.WriteLine();
            Console.WriteLine("💡 Create a session first:");
            Console.WriteLine($"   pidgeon lock create {sessionName} --scope patient");
            return 1;
        }

        // Validate field path and value for common message types
        await ValidateFieldPathAndValue(fieldPath, value);

        // Set the value
        var setResult = await _lockSessionService.SetValueAsync(sessionName, fieldPath, value, reason, cancellationToken);
        if (setResult.IsFailure)
        {
            Console.WriteLine($"❌ Failed to set value: {setResult.Error.Message}");
            return 1;
        }

        // Get updated session to show current state
        var updatedSessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (updatedSessionResult.IsSuccess)
        {
            var session = updatedSessionResult.Value;
            var lockedValue = session.LockedValues.FirstOrDefault(v => v.FieldPath == fieldPath);

            Console.WriteLine($"✅ Set value in session: {sessionName}");
            Console.WriteLine($"   Field: {fieldPath}");
            Console.WriteLine($"   Value: \"{value}\"");
            if (!string.IsNullOrEmpty(reason))
            {
                Console.WriteLine($"   Reason: {reason}");
            }
            Console.WriteLine($"   Locked: {lockedValue?.LockedAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();
            Console.WriteLine($"📊 Session now has {session.LockedValues.Count} locked value(s)");
            Console.WriteLine();
            Console.WriteLine("💡 Next steps:");
            Console.WriteLine($"   pidgeon generate \"ADT^A01\" --use-lock {sessionName}");
            Console.WriteLine($"   pidgeon set {sessionName} --list  # View all values");
        }

        return 0;
    }

    private async Task<int> HandleRemoveValue(
        string sessionName,
        string fieldPath,
        CancellationToken cancellationToken)
    {
        var removeResult = await _lockSessionService.RemoveValueAsync(sessionName, fieldPath, cancellationToken);
        if (removeResult.IsFailure)
        {
            Console.WriteLine($"❌ Failed to remove value: {removeResult.Error.Message}");
            return 1;
        }

        Console.WriteLine($"✅ Removed value from session: {sessionName}");
        Console.WriteLine($"   Field: {fieldPath}");

        return 0;
    }

    private async Task<int> HandleListValues(
        string sessionName,
        CancellationToken cancellationToken)
    {
        var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ {sessionResult.Error.Message}");
            return 1;
        }

        var session = sessionResult.Value;

        Console.WriteLine($"🔒 Locked Values in Session: {sessionName}");
        Console.WriteLine($"   Scope: {session.Scope}");
        Console.WriteLine();

        if (!session.LockedValues.Any())
        {
            Console.WriteLine("No locked values found.");
            Console.WriteLine();
            Console.WriteLine("💡 Set values with:");
            Console.WriteLine($"   pidgeon set {sessionName} patient.mrn \"MR123456\"");
            Console.WriteLine($"   pidgeon set {sessionName} patient.lastName \"Smith\"");
            return 0;
        }

        foreach (var value in session.LockedValues.OrderBy(v => v.FieldPath))
        {
            Console.WriteLine($"📝 {value.FieldPath}");
            Console.WriteLine($"   Value: \"{value.Value}\"");
            Console.WriteLine($"   Locked: {value.LockedAt:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(value.Reason))
            {
                Console.WriteLine($"   Reason: {value.Reason}");
            }
            if (!string.IsNullOrEmpty(value.DataType))
            {
                Console.WriteLine($"   Type: {value.DataType}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"📊 Total: {session.LockedValues.Count} locked value(s)");
        Console.WriteLine();
        Console.WriteLine("💡 Commands:");
        Console.WriteLine($"   pidgeon set {sessionName} <field> <value>     # Set new value");
        Console.WriteLine($"   pidgeon set {sessionName} <field> --remove    # Remove value");
        Console.WriteLine($"   pidgeon generate \"ADT^A01\" --use-lock {sessionName}  # Use in generation");

        return 0;
    }

    private async Task ValidateFieldPathAndValue(string fieldPath, string value)
    {
        try
        {
            // For semantic paths (patient.mrn, encounter.location), validate against user's configured standards
            if (IsSemanticPath(fieldPath))
            {
                // Get user's standard preferences to determine which message types to validate against
                var standardMessageTypes = await GetRelevantMessageTypesForSemanticPath(fieldPath);
                var validationWarnings = new List<string>();

                foreach (var messageType in standardMessageTypes)
                {
                    var pathValidation = await _fieldPathResolver.ValidatePathAsync(fieldPath, messageType);
                    if (pathValidation.IsSuccess && pathValidation.Value)
                    {
                        // Path is valid for this message type, check value
                        var valueValidation = await _fieldPathResolver.ValidateValueAsync(fieldPath, value, messageType);
                        if (valueValidation.IsSuccess && !valueValidation.Value.IsValid)
                        {
                            validationWarnings.Add($"⚠️  For {messageType}: {valueValidation.Value.ErrorMessage}");
                            if (valueValidation.Value.Suggestions.Any())
                            {
                                foreach (var suggestion in valueValidation.Value.Suggestions)
                                {
                                    validationWarnings.Add($"   💡 {suggestion}");
                                }
                            }
                        }
                    }
                }

                if (validationWarnings.Any())
                {
                    Console.WriteLine("⚠️  Validation warnings:");
                    foreach (var warning in validationWarnings)
                    {
                        Console.WriteLine($"   {warning}");
                    }
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Field validation failed for {FieldPath}, continuing with set operation", fieldPath);
            // Don't fail the operation if validation has issues
        }
    }

    private async Task<IReadOnlyList<string>> GetRelevantMessageTypesForSemanticPath(string semanticPath)
    {
        try
        {
            var messageTypes = new List<string>();

            // Extract entity from semantic path (e.g., "patient" from "patient.mrn")
            var entity = semanticPath.Split('.')[0];

            // Map entity to relevant message types based on common healthcare workflows
            var entityMessageTypeMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["patient"] = new[] { "Patient", "ADT^A01" },        // FHIR Patient + HL7 ADT
                ["provider"] = new[] { "Practitioner", "ADT^A01" },  // FHIR Practitioner + HL7 ADT
                ["encounter"] = new[] { "Encounter", "ADT^A01", "ADT^A02" },    // FHIR Encounter + HL7 ADT
                ["observation"] = new[] { "Observation", "ORU^R01" }, // FHIR Observation + HL7 ORU
                ["medication"] = new[] { "MedicationRequest", "RDE^O11" }, // FHIR MedicationRequest + HL7 RDE
                ["allergy"] = new[] { "AllergyIntolerance", "ADT^A01" },  // FHIR AllergyIntolerance + HL7 ADT
                ["insurance"] = new[] { "Coverage", "ADT^A01" }      // FHIR Coverage + HL7 ADT (IN1 segment)
            };

            if (entityMessageTypeMap.TryGetValue(entity, out var types))
            {
                messageTypes.AddRange(types);
            }
            else
            {
                // Fallback to common message types if entity not recognized
                messageTypes.AddRange(new[] { "Patient", "ADT^A01" });
            }

            return messageTypes;
        }
        catch (Exception)
        {
            // Fallback to safe defaults
            return new[] { "Patient", "ADT^A01" };
        }
    }

    private static bool IsSemanticPath(string fieldPath)
    {
        // Semantic paths use dot notation (patient.mrn) vs HL7 notation (PID.3)
        return fieldPath.Contains('.') &&
               !char.IsUpper(fieldPath[0]) &&
               !fieldPath.Any(char.IsDigit);
    }
}