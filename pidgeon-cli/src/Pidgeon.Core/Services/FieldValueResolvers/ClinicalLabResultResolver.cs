// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Services.Clinical;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves lab result field values using clinical scenario coordinator.
/// Provides clinically coherent lab tests and results that align with patient's diagnosis.
/// Priority: 77 (higher than LabTestFieldResolver at 75, ensuring clinical coherence)
/// </summary>
public class ClinicalLabResultResolver : IFieldValueResolver
{
    private readonly ILogger<ClinicalLabResultResolver> _logger;
    private readonly ClinicalScenarioCoordinator _scenarioCoordinator;
    private static int _labTestIndex = 0;
    private static List<LabTestResult>? _currentLabResults;

    public int Priority => 77;

    public ClinicalLabResultResolver(
        ILogger<ClinicalLabResultResolver> logger,
        ClinicalScenarioCoordinator scenarioCoordinator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scenarioCoordinator = scenarioCoordinator ?? throw new ArgumentNullException(nameof(scenarioCoordinator));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        await Task.Yield(); // Async for interface compliance

        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";
        var segmentCode = context.SegmentCode?.ToUpperInvariant() ?? "";

        try
        {
            // Only handle observation/result fields (OBX segment primarily)
            if (segmentCode != "OBX" && segmentCode != "OBR")
                return null;

            // Initialize lab results for this message if not already done
            if (_currentLabResults == null || _currentLabResults.Count == 0)
            {
                _currentLabResults = _scenarioCoordinator.GetLabTests(maxTests: 5);
                _labTestIndex = 0;
            }

            if (!_currentLabResults.Any())
                return null;

            // Get current lab result (cycle through available tests)
            var labResult = _currentLabResults[_labTestIndex % _currentLabResults.Count];

            // OBX.3 - Observation Identifier (LOINC code or test name)
            if (context.FieldPosition == 3 && segmentCode == "OBX")
            {
                // Return as LOINC code
                _logger.LogDebug("Resolved lab test from scenario: {LoincCode} - {TestName}",
                    labResult.LoincCode, labResult.TestName);

                return labResult.LoincCode;
            }

            // OBX.5 - Observation Value (numeric result)
            if (context.FieldPosition == 5 && segmentCode == "OBX")
            {
                return labResult.Value;
            }

            // OBX.6 - Units
            if (context.FieldPosition == 6 && segmentCode == "OBX")
            {
                return labResult.Units;
            }

            // OBX.7 - Reference Range
            if (context.FieldPosition == 7 && segmentCode == "OBX")
            {
                return labResult.ReferenceRange;
            }

            // OBX.8 - Abnormal Flags
            if (context.FieldPosition == 8 && segmentCode == "OBX")
            {
                return labResult.AbnormalFlag;
            }

            // For OBR.4 - Universal Service Identifier
            if (context.FieldPosition == 4 && segmentCode == "OBR")
            {
                // Increment for next OBX segment group
                _labTestIndex++;
                if (_labTestIndex > 100)
                    _labTestIndex = 0;

                return labResult.LoincCode;
            }

            // Field name/description based detection for other observation fields
            if (fieldName.Contains("observation") || fieldName.Contains("loinc") ||
                fieldName.Contains("test") || fieldName.Contains("result") ||
                fieldDescription.Contains("observation") || fieldDescription.Contains("loinc"))
            {
                // LOINC code field
                if (fieldName.Contains("code") || fieldName.Contains("identifier") ||
                    fieldDescription.Contains("code") || fieldDescription.Contains("identifier"))
                    return labResult.LoincCode;

                // Test name field
                if (fieldName.Contains("name") || fieldName.Contains("component") ||
                    fieldName.Contains("description") ||
                    fieldDescription.Contains("name") || fieldDescription.Contains("component"))
                    return labResult.TestName;

                // Value field
                if (fieldName.Contains("value") || fieldDescription.Contains("value"))
                    return labResult.Value;

                // Units field
                if (fieldName.Contains("unit") || fieldDescription.Contains("unit"))
                    return labResult.Units;

                // Reference range field
                if (fieldName.Contains("reference") || fieldName.Contains("range") ||
                    fieldDescription.Contains("reference") || fieldDescription.Contains("range"))
                    return labResult.ReferenceRange;

                // Abnormal flag field
                if (fieldName.Contains("abnormal") || fieldName.Contains("flag") ||
                    fieldDescription.Contains("abnormal") || fieldDescription.Contains("flag"))
                    return labResult.AbnormalFlag;

                // Default to LOINC code
                return labResult.LoincCode;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving clinical lab result field {FieldName}", fieldName);
            return null;
        }
    }

    /// <summary>
    /// Resets the lab result cache (call between messages).
    /// </summary>
    public static void Reset()
    {
        _currentLabResults = null;
        _labTestIndex = 0;
    }
}
