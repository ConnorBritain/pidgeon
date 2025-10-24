// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves identifier field values (patient IDs, account numbers, license numbers, etc.).
/// Handles complex field names with parentheses, dashes, and special characters.
/// Priority: 75 (medium - identifier data before fallback)
/// </summary>
public class IdentifierFieldResolver : IFieldValueResolver
{
    private readonly ILogger<IdentifierFieldResolver> _logger;
    private readonly Random _random;

    public int Priority => 75;

    public IdentifierFieldResolver(ILogger<IdentifierFieldResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        await Task.Yield();

        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var description = context.Field.Description?.ToLowerInvariant() ?? "";
        var combined = $"{fieldName} {description}";

        try
        {
            // Patient identifiers
            if (combined.Contains("patient") && (combined.Contains("id") || combined.Contains("identifier")))
                return GeneratePatientId();

            // Account numbers
            if (combined.Contains("account") && (combined.Contains("number") || combined.Contains("id")))
                return GenerateAccountNumber();

            // Medical record numbers
            if (combined.Contains("medical record") || combined.Contains("mrn"))
                return GenerateMedicalRecordNumber();

            // Social Security Numbers
            if (combined.Contains("ssn") || combined.Contains("social security"))
                return GenerateSocialSecurityNumber();

            // Driver's license
            if (combined.Contains("driver") || combined.Contains("license number"))
                return GenerateDriversLicense();

            // Operator/User IDs
            if (combined.Contains("operator") && (combined.Contains("id") || combined.Contains("identifier")))
                return GenerateOperatorId();

            // Producer/Provider IDs
            if ((combined.Contains("producer") || combined.Contains("provider")) &&
                (combined.Contains("id") || combined.Contains("identifier")))
                return GenerateProviderId();

            // Mother's identifier
            if (combined.Contains("mother") && (combined.Contains("id") || combined.Contains("identifier")))
                return GeneratePatientId();

            // Visit/Encounter identifiers
            if ((combined.Contains("visit") || combined.Contains("encounter")) &&
                (combined.Contains("number") || combined.Contains("id")))
                return GenerateVisitNumber();

            // Order/Placer/Filler identifiers
            if ((combined.Contains("order") || combined.Contains("placer") || combined.Contains("filler")) &&
                (combined.Contains("number") || combined.Contains("id")))
                return GenerateOrderNumber();

            // Message control IDs
            if (combined.Contains("message") && combined.Contains("control"))
                return GenerateMessageControlId();

            // Generic identifiers (catch remaining ID fields)
            if (combined.Contains("identifier") ||
                (combined.Contains("id") && !combined.Contains("void"))) // Avoid "void" as ID
                return GenerateGenericId();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving identifier field {FieldName}", fieldName);
            return null;
        }
    }

    private string GeneratePatientId() => $"PAT{_random.Next(100000, 999999)}";
    private string GenerateAccountNumber() => $"ACCT{_random.Next(1000000, 9999999)}";
    private string GenerateMedicalRecordNumber() => $"MRN{_random.Next(100000, 999999)}";
    private string GenerateSocialSecurityNumber() => $"{_random.Next(100, 999)}-{_random.Next(10, 99)}-{_random.Next(1000, 9999)}";
    private string GenerateDriversLicense() => $"DL{_random.Next(1000000, 9999999)}";
    private string GenerateOperatorId() => $"OP{_random.Next(1000, 9999)}";
    private string GenerateProviderId() => $"PRV{_random.Next(10000, 99999)}";
    private string GenerateVisitNumber() => $"V{_random.Next(1000000, 9999999)}";
    private string GenerateOrderNumber() => $"ORD{_random.Next(100000, 999999)}";
    private string GenerateMessageControlId() => $"MSG{DateTime.Now:yyyyMMddHHmmss}{_random.Next(1000, 9999)}";
    private string GenerateGenericId() => $"ID{_random.Next(100000, 999999)}";
}
