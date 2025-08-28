// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// RXR - Pharmacy/Treatment Route Segment.
/// Specifies the route of administration for the medication.
/// </summary>
public class RXRSegment : HL7Segment
{
    public override string SegmentId => "RXR";
    public override string DisplayName => "Pharmacy Route";

    // Field accessors
    public StringField Route => GetField<StringField>(1)!;
    public StringField Site => GetField<StringField>(2)!;
    public StringField AdministrationDevice => GetField<StringField>(3)!;
    public StringField AdministrationMethod => GetField<StringField>(4)!;

    public override void InitializeFields()
    {
        // RXR.1 - Route (Required)
        AddField(StringField.Required(60));

        // RXR.2 - Site
        AddField(StringField.Optional(60));

        // RXR.3 - Administration Device
        AddField(StringField.Optional(60));

        // RXR.4 - Administration Method
        AddField(StringField.Optional(60));
    }

    /// <summary>
    /// Sets the route of administration.
    /// </summary>
    /// <param name="route">The route (e.g., "PO" for oral, "IV" for intravenous)</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<RXRSegment> SetRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return Error.Validation("Route is required for RXR segment", "Route");

        // Map common route descriptions to HL7 codes
        var routeCode = route.ToUpperInvariant() switch
        {
            "ORAL" or "BY MOUTH" => "PO^Oral",
            "INTRAVENOUS" or "IV" => "IV^Intravenous",
            "INTRAMUSCULAR" or "IM" => "IM^Intramuscular",
            "SUBCUTANEOUS" or "SUBCUT" or "SC" or "SQ" => "SC^Subcutaneous",
            "TOPICAL" or "TOP" => "TOP^Topical",
            "INHALATION" or "INH" => "IH^Inhalation",
            "RECTAL" or "PR" => "PR^Rectal",
            "SUBLINGUAL" or "SL" => "SL^Sublingual",
            "INTRANASAL" or "NAS" => "NAS^Intranasal",
            _ => route.Contains('^') ? route : $"{route}^{route}"
        };

        var result = Route.SetValue(routeCode);
        if (result.IsFailure)
            return Error.Validation($"Failed to set route: {result.Error.Message}", "Route");

        return Result<RXRSegment>.Success(this);
    }

    /// <summary>
    /// Sets the administration site.
    /// </summary>
    /// <param name="site">The administration site (e.g., "LA" for left arm)</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<RXRSegment> SetSite(string site)
    {
        var result = Site.SetValue(site);
        if (result.IsFailure)
            return Error.Validation($"Failed to set site: {result.Error.Message}", "Site");

        return Result<RXRSegment>.Success(this);
    }
}