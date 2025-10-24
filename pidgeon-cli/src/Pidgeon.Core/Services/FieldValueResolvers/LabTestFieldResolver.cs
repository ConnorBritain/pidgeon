// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Data;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves lab test-related field values using lab test data source.
/// Provides realistic LOINC codes for observation and result messages.
/// Priority: 75 (medium - lab test data for clinical fields)
/// </summary>
public class LabTestFieldResolver : IFieldValueResolver
{
    private readonly ILogger<LabTestFieldResolver> _logger;
    private readonly ILabTestDataSource _labTestDataSource;

    public int Priority => 75;

    public LabTestFieldResolver(
        ILogger<LabTestFieldResolver> logger,
        ILabTestDataSource labTestDataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _labTestDataSource = labTestDataSource ?? throw new ArgumentNullException(nameof(labTestDataSource));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        try
        {
            // Lab test / observation identifier fields
            if (fieldName.Contains("observation") || fieldName.Contains("loinc") ||
                fieldName.Contains("test") || fieldName.Contains("result") ||
                fieldDescription.Contains("observation") || fieldDescription.Contains("loinc") ||
                fieldDescription.Contains("test") || fieldDescription.Contains("result"))
            {
                var labTest = await _labTestDataSource.GetRandomLabTestAsync();

                // LOINC code field
                if (fieldName.Contains("code") || fieldName.Contains("identifier") ||
                    fieldDescription.Contains("code") || fieldDescription.Contains("identifier"))
                    return labTest.LoincCode;

                // Test name / component field
                if (fieldName.Contains("name") || fieldName.Contains("component") ||
                    fieldName.Contains("description") ||
                    fieldDescription.Contains("name") || fieldDescription.Contains("component") ||
                    fieldDescription.Contains("description"))
                    return labTest.CommonName;

                // Default to LOINC code for observation fields
                return labTest.LoincCode;
            }

            // Not a lab test field
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving lab test field {FieldName}", fieldName);
            return null;
        }
    }
}
