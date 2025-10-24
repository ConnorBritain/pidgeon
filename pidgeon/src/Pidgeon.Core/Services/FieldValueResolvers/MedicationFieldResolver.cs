// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Data;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves medication-related field values using medication data source.
/// Provides realistic medication data for RDE, RDS, and other pharmacy messages.
/// Priority: 75 (medium - medication data for pharmacy fields)
/// </summary>
public class MedicationFieldResolver : IFieldValueResolver
{
    private readonly ILogger<MedicationFieldResolver> _logger;
    private readonly IMedicationDataSource _medicationDataSource;

    public int Priority => 75;

    public MedicationFieldResolver(
        ILogger<MedicationFieldResolver> logger,
        IMedicationDataSource medicationDataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _medicationDataSource = medicationDataSource ?? throw new ArgumentNullException(nameof(medicationDataSource));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        try
        {
            // Medication name fields (brand or generic)
            if (fieldName.Contains("drug") || fieldName.Contains("medication") ||
                fieldDescription.Contains("drug") || fieldDescription.Contains("medication"))
            {
                var medication = await _medicationDataSource.GetRandomMedicationAsync();

                // Prefer generic name for clinical accuracy
                if (fieldName.Contains("brand") || fieldDescription.Contains("brand"))
                    return medication.BrandName;

                return medication.GenericName;
            }

            // NDC code fields
            if (fieldName.Contains("ndc") || fieldName.Contains("national drug code") ||
                fieldDescription.Contains("ndc") || fieldDescription.Contains("national drug code"))
            {
                var medication = await _medicationDataSource.GetRandomMedicationAsync();
                return medication.Ndc;
            }

            // Strength fields
            if (fieldName.Contains("strength") || fieldName.Contains("dose") ||
                fieldDescription.Contains("strength") || fieldDescription.Contains("dose"))
            {
                var medication = await _medicationDataSource.GetRandomMedicationAsync();
                return $"{medication.Strength} {medication.Unit}";
            }

            // Dosage form fields
            if (fieldName.Contains("form") || fieldName.Contains("dosage form") ||
                fieldDescription.Contains("form") || fieldDescription.Contains("dosage form"))
            {
                var medication = await _medicationDataSource.GetRandomMedicationAsync();
                return medication.DosageForm;
            }

            // Route fields
            if (fieldName.Contains("route") || fieldDescription.Contains("route"))
            {
                var medication = await _medicationDataSource.GetRandomMedicationAsync();
                return medication.RouteName;
            }

            // Not a medication field
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving medication field {FieldName}", fieldName);
            return null;
        }
    }
}
