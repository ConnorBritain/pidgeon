// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents the HL7 RXR (Pharmacy/Treatment Route) segment.
/// Contains information about the route of administration for medications.
/// </summary>
public class RXRSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "RXR";

    /// <summary>
    /// Initializes a new instance of the <see cref="RXRSegment"/> class.
    /// </summary>
    public RXRSegment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields according to HL7 v2.3 specification.
    /// </summary>
    protected override void InitializeFields()
    {
        // RXR.1 - Route
        AddField(new CodedElementField(value: null, isRequired: true));
        
        // RXR.2 - Administration Site
        AddField(new CodedElementField(value: null, isRequired: false));
        
        // RXR.3 - Administration Device
        AddField(new CodedElementField(value: null, isRequired: false));
        
        // RXR.4 - Administration Method
        AddField(new CodedElementField(value: null, isRequired: false));
        
        // RXR.5 - Routing Instruction
        AddField(new CodedElementField(value: null, isRequired: false));
    }

    /// <summary>
    /// Gets or sets the route of administration.
    /// </summary>
    public CodedElementField Route
    {
        get => (CodedElementField)this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the administration site.
    /// </summary>
    public CodedElementField AdministrationSite
    {
        get => (CodedElementField)this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the administration device.
    /// </summary>
    public CodedElementField AdministrationDevice
    {
        get => (CodedElementField)this[3];
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the administration method.
    /// </summary>
    public CodedElementField AdministrationMethod
    {
        get => (CodedElementField)this[4];
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the routing instruction.
    /// </summary>
    public CodedElementField RoutingInstruction
    {
        get => (CodedElementField)this[5];
        set => this[5] = value;
    }

    /// <summary>
    /// Creates a new RXR segment for oral administration.
    /// </summary>
    /// <param name="site">The administration site (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateOralRoute(string? site = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("PO", "Oral", "HL70162");
        
        if (!string.IsNullOrEmpty(site))
        {
            segment.AdministrationSite = CodedElementField.CreateSite(site, "", "HL70163");
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXR segment for intravenous administration.
    /// </summary>
    /// <param name="site">The administration site (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateIntravenousRoute(string? site = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("IV", "Intravenous", "HL70162");
        
        if (!string.IsNullOrEmpty(site))
        {
            segment.AdministrationSite = CodedElementField.CreateSite(site, "", "HL70163");
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXR segment for intramuscular administration.
    /// </summary>
    /// <param name="site">The administration site (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateIntramuscularRoute(string? site = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("IM", "Intramuscular", "HL70162");
        
        if (!string.IsNullOrEmpty(site))
        {
            segment.AdministrationSite = CodedElementField.CreateSite(site, "", "HL70163");
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXR segment for subcutaneous administration.
    /// </summary>
    /// <param name="site">The administration site (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateSubcutaneousRoute(string? site = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("SC", "Subcutaneous", "HL70162");
        
        if (!string.IsNullOrEmpty(site))
        {
            segment.AdministrationSite = CodedElementField.CreateSite(site, "", "HL70163");
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXR segment for topical administration.
    /// </summary>
    /// <param name="site">The administration site (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateTopicalRoute(string? site = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("TOP", "Topical", "HL70162");
        
        if (!string.IsNullOrEmpty(site))
        {
            segment.AdministrationSite = CodedElementField.CreateSite(site, "", "HL70163");
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXR segment for inhalation administration.
    /// </summary>
    /// <param name="device">The administration device (optional).</param>
    /// <returns>A new <see cref="RXRSegment"/> instance.</returns>
    public static RXRSegment CreateInhalationRoute(string? device = null)
    {
        var segment = new RXRSegment();
        segment.Route = CodedElementField.CreateRoute("INH", "Inhalation", "HL70162");
        
        if (!string.IsNullOrEmpty(device))
        {
            segment.AdministrationDevice = CodedElementField.CreateDevice(device, "", "HL70164");
        }
        
        return segment;
    }

    /// <summary>
    /// Sets the route of administration.
    /// </summary>
    /// <param name="routeCode">The route code (e.g., "PO", "IV", "IM", "SC").</param>
    /// <param name="routeText">The route description (optional).</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetRoute(string routeCode, string? routeText = null, string? codingSystem = null)
    {
        Route = CodedElementField.CreateRoute(routeCode, routeText, codingSystem);
    }

    /// <summary>
    /// Sets the administration site.
    /// </summary>
    /// <param name="siteCode">The site code.</param>
    /// <param name="siteText">The site description (optional).</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetAdministrationSite(string siteCode, string? siteText = null, string? codingSystem = null)
    {
        AdministrationSite = CodedElementField.CreateSite(siteCode, siteText, codingSystem);
    }

    /// <summary>
    /// Sets the administration device.
    /// </summary>
    /// <param name="deviceCode">The device code.</param>
    /// <param name="deviceText">The device description (optional).</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetAdministrationDevice(string deviceCode, string? deviceText = null, string? codingSystem = null)
    {
        AdministrationDevice = CodedElementField.CreateDevice(deviceCode, deviceText, codingSystem);
    }

    /// <summary>
    /// Sets the administration method.
    /// </summary>
    /// <param name="methodCode">The method code.</param>
    /// <param name="methodText">The method description (optional).</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetAdministrationMethod(string methodCode, string? methodText = null, string? codingSystem = null)
    {
        AdministrationMethod = CodedElementField.CreateMethod(methodCode, methodText, codingSystem);
    }

    /// <summary>
    /// Gets a formatted display string for this route.
    /// </summary>
    /// <returns>A human-readable representation of the route.</returns>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Route.RawValue))
        {
            parts.Add($"Route: {Route.ToDisplayString()}");
        }
        
        if (!string.IsNullOrEmpty(AdministrationSite.RawValue))
        {
            parts.Add($"Site: {AdministrationSite.ToDisplayString()}");
        }
        
        if (!string.IsNullOrEmpty(AdministrationDevice.RawValue))
        {
            parts.Add($"Device: {AdministrationDevice.ToDisplayString()}");
        }
        
        if (!string.IsNullOrEmpty(AdministrationMethod.RawValue))
        {
            parts.Add($"Method: {AdministrationMethod.ToDisplayString()}");
        }
        
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new instance with the same field values.</returns>
    public override HL7Segment Clone()
    {
        var cloned = new RXRSegment();
        for (var i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i].Clone();
        }
        return cloned;
    }
}
