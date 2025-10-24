// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Data;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves diagnosis-related field values using diagnosis data source.
/// Provides realistic ICD-10 diagnosis codes for clinical messages.
/// Priority: 75 (medium - diagnosis data for clinical fields)
/// </summary>
public class DiagnosisFieldResolver : IFieldValueResolver
{
    private readonly ILogger<DiagnosisFieldResolver> _logger;
    private readonly IDiagnosisDataSource _diagnosisDataSource;

    public int Priority => 75;

    public DiagnosisFieldResolver(
        ILogger<DiagnosisFieldResolver> logger,
        IDiagnosisDataSource diagnosisDataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosisDataSource = diagnosisDataSource ?? throw new ArgumentNullException(nameof(diagnosisDataSource));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        try
        {
            // Diagnosis code fields
            if (fieldName.Contains("diagnosis") || fieldName.Contains("icd") ||
                fieldDescription.Contains("diagnosis") || fieldDescription.Contains("icd"))
            {
                var diagnosis = await _diagnosisDataSource.GetRandomDiagnosisCodeAsync();

                // ICD-10 code field
                if (fieldName.Contains("code") || fieldDescription.Contains("code"))
                    return diagnosis.Code;

                // Diagnosis description field
                if (fieldName.Contains("description") || fieldName.Contains("text") ||
                    fieldDescription.Contains("description") || fieldDescription.Contains("text"))
                    return diagnosis.Description;

                // Default to code for diagnosis fields
                return diagnosis.Code;
            }

            // Not a diagnosis field
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving diagnosis field {FieldName}", fieldName);
            return null;
        }
    }
}
