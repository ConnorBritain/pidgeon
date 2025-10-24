// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Data;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves field values using demographic data source (names and addresses).
/// Provides realistic demographic data for patient-related fields.
/// Priority: 80 (medium-high - demographic data preferred over random)
/// </summary>
public class DemographicFieldResolver : IFieldValueResolver
{
    private readonly ILogger<DemographicFieldResolver> _logger;
    private readonly IDemographicDataSource _demographicDataSource;

    public int Priority => 80;

    public DemographicFieldResolver(
        ILogger<DemographicFieldResolver> logger,
        IDemographicDataSource demographicDataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _demographicDataSource = demographicDataSource ?? throw new ArgumentNullException(nameof(demographicDataSource));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        try
        {
            // Patient name fields
            if (fieldName.Contains("family name") || fieldName.Contains("last name") ||
                fieldDescription.Contains("family name") || fieldDescription.Contains("last name"))
            {
                return await _demographicDataSource.GetRandomLastNameAsync();
            }

            if (fieldName.Contains("given name") || fieldName.Contains("first name") ||
                fieldDescription.Contains("given name") || fieldDescription.Contains("first name"))
            {
                // Use random gender for first name
                var random = new Random();
                return random.Next(2) == 0
                    ? await _demographicDataSource.GetRandomMaleFirstNameAsync()
                    : await _demographicDataSource.GetRandomFemaleFirstNameAsync();
            }

            if (fieldName.Contains("patient name") || fieldName.Contains("person name"))
            {
                // Generate full name with random gender
                var random = new Random();
                var gender = random.Next(2) == 0 ? "M" : "F";
                return await _demographicDataSource.GetRandomFullNameAsync(gender);
            }

            // Address fields
            if (fieldName.Contains("street") || fieldDescription.Contains("street address"))
            {
                var address = await _demographicDataSource.GetRandomAddressAsync();
                return address.Street;
            }

            if (fieldName.Contains("city") || fieldDescription.Contains("city"))
            {
                var address = await _demographicDataSource.GetRandomAddressAsync();
                return address.City;
            }

            if (fieldName.Contains("state") || fieldDescription.Contains("state"))
            {
                var address = await _demographicDataSource.GetRandomAddressAsync();
                return address.State;
            }

            if (fieldName.Contains("zip") || fieldName.Contains("postal") ||
                fieldDescription.Contains("zip") || fieldDescription.Contains("postal"))
            {
                var address = await _demographicDataSource.GetRandomAddressAsync();
                return address.ZipCode;
            }

            if (fieldName.Contains("country") || fieldDescription.Contains("country"))
            {
                // All free tier addresses are US
                return "USA";
            }

            // Not a demographic field
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving demographic field {FieldName}", fieldName);
            return null;
        }
    }
}