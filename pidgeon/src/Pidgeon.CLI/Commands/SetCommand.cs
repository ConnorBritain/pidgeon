// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.CLI.Services;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for setting field values with smart session management.
/// Auto-creates sessions when needed and provides progressive disclosure of session complexity.
/// </summary>
public class SetCommand : CommandBuilderBase
{
    private readonly ILockSessionService _lockSessionService;
    private readonly IFieldPathResolver _fieldPathResolver;
    private readonly SessionHelper _sessionHelper;

    public SetCommand(
        ILogger<SetCommand> logger,
        ILockSessionService lockSessionService,
        IFieldPathResolver fieldPathResolver,
        SessionHelper sessionHelper)
        : base(logger)
    {
        _lockSessionService = lockSessionService;
        _fieldPathResolver = fieldPathResolver;
        _sessionHelper = sessionHelper;
    }

    public override Command CreateCommand()
    {
        var command = new Command("set", "Set field values with smart session management");

        // Arguments (field and value are now the primary arguments)
        var fieldArg = new Argument<string?>("field")
        {
            Description = "Field path to set (e.g., patient.mrn, PID.3.1, Patient.identifier[0].value)",
            Arity = ArgumentArity.ZeroOrOne
        };

        var valueArg = new Argument<string?>("value")
        {
            Description = "Value to set for the field (optional for --list and --remove operations)",
            Arity = ArgumentArity.ZeroOrOne
        };

        // Options
        var reasonOption = CreateNullableOption("--reason", "Reason for setting this value");
        var listOption = CreateBooleanOption("--list", "List current values in the current session");
        var removeOption = CreateBooleanOption("--remove", "Remove the specified field value from the current session");
        var sessionOption = CreateNullableOption("--session", "Override session (advanced: use specific session instead of current)");

        command.Add(fieldArg);
        command.Add(valueArg);
        command.Add(reasonOption);
        command.Add(listOption);
        command.Add(removeOption);
        command.Add(sessionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var fieldPath = parseResult.GetValue(fieldArg);
                var value = parseResult.GetValue(valueArg);
                var reason = parseResult.GetValue(reasonOption);
                var list = parseResult.GetValue(listOption);
                var remove = parseResult.GetValue(removeOption);
                var sessionOverride = parseResult.GetValue(sessionOption);

                // Handle list operation
                if (list)
                {
                    return await HandleListValues(sessionOverride, cancellationToken);
                }

                // Handle remove operation
                if (remove)
                {
                    if (string.IsNullOrWhiteSpace(fieldPath))
                    {
                        Console.WriteLine("❌ Field path is required when removing a value");
                        Console.WriteLine("Usage: pidgeon set <field> --remove");
                        return 1;
                    }
                    return await HandleRemoveValue(sessionOverride, fieldPath, cancellationToken);
                }

                // Handle set operation
                if (string.IsNullOrWhiteSpace(fieldPath))
                {
                    Console.WriteLine("❌ Field path is required when setting a value");
                    Console.WriteLine("Usage: pidgeon set <field> <value>");
                    Console.WriteLine("       pidgeon set --list                    # Show current values");
                    Console.WriteLine("       pidgeon set <field> --remove         # Remove a field");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    Console.WriteLine("❌ Value is required when setting a field");
                    Console.WriteLine("Usage: pidgeon set <field> <value>");
                    Console.WriteLine("       pidgeon set --list                    # Show current values");
                    Console.WriteLine("       pidgeon set <field> --remove         # Remove a field");
                    return 1;
                }

                return await HandleSetValue(sessionOverride, fieldPath, value, reason, cancellationToken);
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
        string? sessionOverride,
        string fieldPath,
        string value,
        string? reason,
        CancellationToken cancellationToken)
    {
        // Smart session management: get or create session
        string sessionName;
        bool sessionCreated = false;

        if (!string.IsNullOrEmpty(sessionOverride))
        {
            // Use explicit session override
            sessionName = sessionOverride;
            var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
            if (sessionResult.IsFailure)
            {
                Console.WriteLine($"❌ Session '{sessionName}' not found");
                Console.WriteLine("💡 Create the session first:");
                Console.WriteLine($"   pidgeon session create {sessionName}");
                return 1;
            }
        }
        else
        {
            // Smart session management: get current or create new
            var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);

            if (currentSession == null)
            {
                // Auto-create temporary session with friendly name
                sessionName = await _sessionHelper.CreateTemporarySessionAsync(cancellationToken);
                sessionCreated = true;

                Console.WriteLine($"✅ Created session: {sessionName} (expires in 24h unless saved)");
            }
            else
            {
                sessionName = currentSession;
            }
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

        // Show success message
        Console.WriteLine($"✅ Set {fieldPath} = \"{value}\" in session: {sessionName}");

        // Show helpful hints for new users
        if (sessionCreated || await _sessionHelper.IsFirstValueInSessionAsync(sessionName, cancellationToken))
        {
            Console.WriteLine("💡 Continue setting values or generate messages with these locked fields");
        }

        // Show session summary
        var updatedSessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (updatedSessionResult.IsSuccess)
        {
            var session = updatedSessionResult.Value;
            Console.WriteLine($"📊 Session now has {session.LockedValues.Count} value(s) set");

            // Show save reminder for temporary sessions
            if (await _sessionHelper.IsTemporarySessionAsync(sessionName, cancellationToken))
            {
                if (session.LockedValues.Count >= 2)  // Show after a few values are set
                {
                    Console.WriteLine($"💡 Save this session: pidgeon session save <name>");
                }
            }
        }

        return 0;
    }

    private async Task<int> HandleRemoveValue(
        string? sessionOverride,
        string fieldPath,
        CancellationToken cancellationToken)
    {
        // Determine which session to use
        string? sessionName = sessionOverride ?? await _sessionHelper.GetCurrentSessionAsync(cancellationToken);

        if (sessionName == null)
        {
            Console.WriteLine("❌ No active session found");
            Console.WriteLine("💡 Create a session first:");
            Console.WriteLine("   pidgeon set patient.mrn \"TEST123\"  # Creates session automatically");
            return 1;
        }

        var removeResult = await _lockSessionService.RemoveValueAsync(sessionName, fieldPath, cancellationToken);
        if (removeResult.IsFailure)
        {
            Console.WriteLine($"❌ Failed to remove value: {removeResult.Error.Message}");
            return 1;
        }

        Console.WriteLine($"✅ Removed {fieldPath} from session: {sessionName}");

        return 0;
    }

    private async Task<int> HandleListValues(
        string? sessionOverride,
        CancellationToken cancellationToken)
    {
        // Determine which session to use
        string? sessionName = sessionOverride ?? await _sessionHelper.GetCurrentSessionAsync(cancellationToken);

        if (sessionName == null)
        {
            Console.WriteLine("❌ No active session found");
            Console.WriteLine("💡 Create a session first:");
            Console.WriteLine("   pidgeon set patient.mrn \"TEST123\"  # Creates session automatically");
            return 1;
        }

        var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ {sessionResult.Error.Message}");
            return 1;
        }

        var session = sessionResult.Value;

        Console.WriteLine($"🔒 Current Session: {sessionName}");
        if (await _sessionHelper.IsTemporarySessionAsync(sessionName, cancellationToken))
        {
            Console.WriteLine("   Type: Temporary (expires in 24h unless saved)");
        }
        else
        {
            Console.WriteLine("   Type: Permanent");
        }
        Console.WriteLine();

        if (!session.LockedValues.Any())
        {
            Console.WriteLine("No values set yet.");
            Console.WriteLine();
            Console.WriteLine("💡 Set values with:");
            Console.WriteLine("   pidgeon set patient.mrn \"MR123456\"");
            Console.WriteLine("   pidgeon set patient.lastName \"Smith\"");
            return 0;
        }

        foreach (var value in session.LockedValues.OrderBy(v => v.FieldPath))
        {
            Console.WriteLine($"📝 {value.FieldPath} = \"{value.Value}\"");
            if (!string.IsNullOrEmpty(value.Reason))
            {
                Console.WriteLine($"   Reason: {value.Reason}");
            }
            Console.WriteLine($"   Set: {value.LockedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        Console.WriteLine($"📊 Total: {session.LockedValues.Count} value(s) set");
        Console.WriteLine();
        Console.WriteLine("💡 Commands:");
        Console.WriteLine("   pidgeon set <field> <value>         # Set new value");
        Console.WriteLine("   pidgeon set <field> --remove        # Remove value");
        Console.WriteLine("   pidgeon generate \"ADT^A01\"           # Use in generation");

        // Show save reminder for temporary sessions
        if (await _sessionHelper.IsTemporarySessionAsync(sessionName, cancellationToken))
        {
            Console.WriteLine("   pidgeon session save <name>        # Save permanently");
        }

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

    private Task<IReadOnlyList<string>> GetRelevantMessageTypesForSemanticPath(string semanticPath)
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

            return Task.FromResult<IReadOnlyList<string>>(messageTypes);
        }
        catch (Exception)
        {
            // Fallback to safe defaults
            return Task.FromResult<IReadOnlyList<string>>(new[] { "Patient", "ADT^A01" });
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