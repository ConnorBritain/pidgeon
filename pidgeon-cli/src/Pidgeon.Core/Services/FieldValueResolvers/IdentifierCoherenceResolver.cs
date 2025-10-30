// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Data;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves identifier composite types (CX, EI, XCN, XON) with semantic coherence:
/// - Check digits derived from base ID using specified algorithm (Mod10, Mod11, ISO)
/// - ID and name components refer to same person/organization
/// - Consistent assigning authority and facility
///
/// Priority: 78 (after CodedElementResolver at 82, before Demographic at 80)
/// </summary>
public class IdentifierCoherenceResolver : ICompositeAwareResolver
{
    private readonly ILogger<IdentifierCoherenceResolver> _logger;
    private readonly IHL7TableProvider _tableProvider;
    private readonly IDemographicDataSource _demographicDataSource;
    private readonly Random _random;

    public int Priority => 78;

    // Common healthcare organization names for XON
    private static readonly string[] OrganizationNames = new[]
    {
        "General Healthcare Center",
        "Community Medical Center",
        "Regional Hospital",
        "University Medical Center",
        "Memorial Hospital",
        "St. Mary's Hospital",
        "Children's Hospital",
        "Veterans Administration Hospital"
    };

    public IdentifierCoherenceResolver(
        ILogger<IdentifierCoherenceResolver> logger,
        IHL7TableProvider tableProvider,
        IDemographicDataSource demographicDataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tableProvider = tableProvider ?? throw new ArgumentNullException(nameof(tableProvider));
        _demographicDataSource = demographicDataSource ?? throw new ArgumentNullException(nameof(demographicDataSource));
        _random = new Random();
    }

    public bool CanHandleComposite(string dataTypeCode)
        => dataTypeCode is "CX" or "EI" or "XCN" or "XON";

    public async Task<Dictionary<int, string>?> ResolveCompositeAsync(
        SegmentField parentField,
        DataType dataType,
        FieldResolutionContext context)
    {
        try
        {
            return dataType.Code switch
            {
                "CX" => await ResolveCXAsync(parentField, context),
                "EI" => await ResolveEIAsync(parentField, context),
                "XCN" => await ResolveXCNAsync(parentField, context),
                "XON" => await ResolveXONAsync(parentField, context),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving identifier composite {DataType} for field {Field}",
                dataType.Code, parentField.Name);
            return null;
        }
    }

    /// <summary>
    /// Resolves CX (Extended Composite ID With Check Digit).
    /// Structure: ID^CheckDigit^CheckDigitScheme^AssigningAuthority^IdentifierTypeCode^AssigningFacility
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveCXAsync(SegmentField parentField, FieldResolutionContext context)
    {
        // Generate base ID (6-8 digits)
        var baseId = $"ID{_random.Next(100000, 999999)}";

        // Choose check digit scheme
        var checkDigitScheme = ChooseCheckDigitScheme();
        var checkDigit = CalculateCheckDigit(baseId, checkDigitScheme);

        // Get identifier type code from table 0203 if available
        var identifierTypeCode = await GetTableValueAsync(203, "MR"); // Default to "MR" (Medical Record)

        return new Dictionary<int, string>
        {
            { 1, baseId },                  // CX.1: ID
            { 2, checkDigit },              // CX.2: Check Digit
            { 3, checkDigitScheme },        // CX.3: Check Digit Scheme
            { 4, string.Empty },            // CX.4: Assigning Authority (HD - leave empty for now)
            { 5, identifierTypeCode },      // CX.5: Identifier Type Code
            { 6, string.Empty }             // CX.6: Assigning Facility (HD - leave empty for now)
        };
    }

    /// <summary>
    /// Resolves EI (Entity Identifier).
    /// Structure: EntityID^NamespaceID^UniversalID^UniversalIDType
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveEIAsync(SegmentField parentField, FieldResolutionContext context)
    {
        // Generate entity identifier
        var entityId = $"ID{_random.Next(100000, 999999)}";

        // Get namespace from table 0300 if available
        var namespaceId = await GetTableValueAsync(300, "DefaultName");

        // Choose universal ID type from table 0301 (DNS, GUID, ISO, etc.)
        var universalIdType = await GetTableValueAsync(301, "ISO");

        return new Dictionary<int, string>
        {
            { 1, entityId },            // EI.1: Entity Identifier
            { 2, string.Empty },        // EI.2: Namespace ID (leave empty for simplicity)
            { 3, string.Empty },        // EI.3: Universal ID
            { 4, string.Empty }         // EI.4: Universal ID Type
        };
    }

    /// <summary>
    /// Resolves XCN (Extended Composite ID Number And Name).
    /// Structure: IDNumber^FamilyName^GivenName^MiddleInitial^Suffix^Prefix^Degree^SourceTable^AssigningAuthority^NameType^CheckDigit^CheckDigitScheme^IdentifierTypeCode^AssigningFacilityID
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveXCNAsync(SegmentField parentField, FieldResolutionContext context)
    {
        // Generate ID
        var idNumber = $"ID{_random.Next(100000, 999999)}";

        // Get person name from demographic data
        var familyName = await _demographicDataSource.GetRandomLastNameAsync();
        var givenName = _random.Next(2) == 0
            ? await _demographicDataSource.GetRandomMaleFirstNameAsync()
            : await _demographicDataSource.GetRandomFemaleFirstNameAsync();

        // Calculate check digit
        var checkDigitScheme = ChooseCheckDigitScheme();
        var checkDigit = CalculateCheckDigit(idNumber, checkDigitScheme);

        // Get name type from table 0200
        var nameType = await GetTableValueAsync(200, "L"); // "L" = Legal

        // Get identifier type from table 0203
        var identifierType = await GetTableValueAsync(203, "U"); // "U" = Unique Identifier

        return new Dictionary<int, string>
        {
            { 1, idNumber },                // XCN.1: ID Number
            { 2, familyName },              // XCN.2: Family Name
            { 3, givenName },               // XCN.3: Given Name
            { 4, string.Empty },            // XCN.4: Middle Initial
            { 5, string.Empty },            // XCN.5: Suffix
            { 6, string.Empty },            // XCN.6: Prefix
            { 7, string.Empty },            // XCN.7: Degree
            { 8, string.Empty },            // XCN.8: Source Table
            { 9, string.Empty },            // XCN.9: Assigning Authority
            { 10, nameType },               // XCN.10: Name Type
            { 11, checkDigit },             // XCN.11: Identifier Check Digit
            { 12, checkDigitScheme },       // XCN.12: Check Digit Scheme
            { 13, identifierType },         // XCN.13: Identifier Type Code
            { 14, string.Empty }            // XCN.14: Assigning Facility ID
        };
    }

    /// <summary>
    /// Resolves XON (Extended Composite Name And ID For Organizations).
    /// Structure: OrganizationName^NameTypeCode^IDNumber^CheckDigit^CheckDigitScheme^AssigningAuthority^IdentifierTypeCode^AssigningFacilityID
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveXONAsync(SegmentField parentField, FieldResolutionContext context)
    {
        // Choose organization name
        var organizationName = OrganizationNames[_random.Next(OrganizationNames.Length)];

        // Generate organization ID
        var idNumber = _random.Next(100000, 999999).ToString();

        // Calculate check digit
        var checkDigitScheme = ChooseCheckDigitScheme();
        var checkDigit = CalculateCheckDigit(idNumber, checkDigitScheme);

        // Get name type code from table 0204
        var nameTypeCode = await GetTableValueAsync(204, "L"); // "L" = Legal Name

        // Get identifier type from table 0203
        var identifierType = await GetTableValueAsync(203, "FI"); // "FI" = Facility ID

        return new Dictionary<int, string>
        {
            { 1, organizationName },        // XON.1: Organization Name
            { 2, nameTypeCode },            // XON.2: Name Type Code
            { 3, idNumber },                // XON.3: ID Number
            { 4, checkDigit },              // XON.4: Check Digit
            { 5, checkDigitScheme },        // XON.5: Check Digit Scheme
            { 6, string.Empty },            // XON.6: Assigning Authority
            { 7, identifierType },          // XON.7: Identifier Type Code
            { 8, string.Empty }             // XON.8: Assigning Facility ID
        };
    }

    /// <summary>
    /// Chooses a check digit scheme from HL7 table 0061.
    /// Common schemes: M10 (Mod10), M11 (Mod11), ISO (ISO 7064)
    /// </summary>
    private string ChooseCheckDigitScheme()
    {
        var schemes = new[] { "M10", "M11", "ISO" };
        return schemes[_random.Next(schemes.Length)];
    }

    /// <summary>
    /// Calculates check digit for a given ID using specified algorithm.
    /// </summary>
    private string CalculateCheckDigit(string id, string scheme)
    {
        // Extract numeric portion only
        var numericId = new string(id.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(numericId))
            return "0";

        return scheme switch
        {
            "M10" => CalculateMod10CheckDigit(numericId),
            "M11" => CalculateMod11CheckDigit(numericId),
            "ISO" => CalculateISOCheckDigit(numericId),
            _ => "0"
        };
    }

    /// <summary>
    /// Calculates Mod10 (Luhn algorithm) check digit.
    /// Used by credit cards and many healthcare identifiers.
    /// </summary>
    private string CalculateMod10CheckDigit(string id)
    {
        var sum = 0;
        var alternate = false;

        // Process digits from right to left
        for (int i = id.Length - 1; i >= 0; i--)
        {
            var digit = id[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    /// <summary>
    /// Calculates Mod11 check digit.
    /// Common in ISBN and some healthcare identifiers.
    /// </summary>
    private string CalculateMod11CheckDigit(string id)
    {
        var sum = 0;
        var weight = 2;

        // Process digits from right to left
        for (int i = id.Length - 1; i >= 0; i--)
        {
            sum += (id[i] - '0') * weight;
            weight++;
            if (weight > 7)
                weight = 2;
        }

        var remainder = sum % 11;
        var checkDigit = (11 - remainder) % 11;

        // Mod11 can produce 10 as check digit, often represented as 'X'
        return checkDigit == 10 ? "X" : checkDigit.ToString();
    }

    /// <summary>
    /// Calculates ISO 7064 Mod 11,10 check digit.
    /// </summary>
    private string CalculateISOCheckDigit(string id)
    {
        var check = 10;

        foreach (var c in id)
        {
            if (!char.IsDigit(c))
                continue;

            var digit = c - '0';
            check = ((check + digit) % 10 == 0 ? 10 : (check + digit) % 10) * 2 % 11;
        }

        var checkDigit = (11 - check) % 10;
        return checkDigit.ToString();
    }

    /// <summary>
    /// Gets a random value from an HL7 table, or returns default if table not found.
    /// </summary>
    private async Task<string> GetTableValueAsync(int tableNumber, string defaultValue)
    {
        try
        {
            var tableResult = await _tableProvider.GetTableAsync(tableNumber);
            if (tableResult.IsSuccess && tableResult.Value.Values.Count > 0)
            {
                var value = tableResult.Value.Values[_random.Next(tableResult.Value.Values.Count)];
                return value.Code;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load table {TableNumber}, using default", tableNumber);
        }

        return defaultValue;
    }
}
