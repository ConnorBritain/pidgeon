// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Application.Services.Configuration;

namespace Pidgeon.Core.Application.Interfaces.Standards;

/// <summary>
/// Universal Configuration Intelligence plugin interface.
/// STANDARD-AGNOSTIC: Works with domain types as input, produces universal analysis results.
/// Plugins handle standard-specific logic internally but conform to universal interface.
/// 
/// This follows the Domain-Driven Standard Detection pattern:
/// - Core services remain standard-agnostic
/// - Plugin selection is driven by domain context (FieldPatterns.Standard)
/// - Plugins work with pure domain types and return universal results
/// </summary>
public interface IConfigurationAnalysisPlugin
{
    /// <summary>
    /// Standard name this plugin handles (e.g., "HL7v23", "FHIR", "NCPDP").
    /// Used by plugin registry for selection.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Standard version this plugin supports.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Determines if this plugin can handle the specified healthcare standard.
    /// </summary>
    /// <param name="standard">Healthcare standard identifier</param>
    /// <returns>True if this plugin can analyze the standard</returns>
    bool CanHandle(string standard);

    /// <summary>
    /// Analyzes field patterns and produces comprehensive configuration intelligence.
    /// 
    /// DOMAIN INPUT: Pure domain types with no plugin-specific properties
    /// UNIVERSAL OUTPUT: Standard-agnostic analysis results populated by plugin logic
    /// </summary>
    /// <param name="patterns">Domain field patterns to analyze</param>
    /// <returns>Universal analysis results with standard-specific insights</returns>
    Task<Result<AnalysisResult>> AnalyzeFieldPatternsAsync(FieldPatterns patterns);

    /// <summary>
    /// Analyzes individual segment patterns for detailed field frequency analysis.
    /// 
    /// DOMAIN INPUT: Clean domain SegmentPattern collection
    /// UNIVERSAL OUTPUT: Standard-agnostic segment analysis results
    /// </summary>
    /// <param name="segments">Domain segment patterns to analyze</param>
    /// <param name="standard">Healthcare standard context</param>
    /// <returns>Universal segment analysis results</returns>
    Task<Result<List<SegmentAnalysisResult>>> AnalyzeSegmentPatternsAsync(
        IEnumerable<SegmentPattern> segments, 
        string standard);

    /// <summary>
    /// Calculates field coverage based on expected patterns for the healthcare standard.
    /// Each plugin implements standard-specific coverage logic (e.g., HL7 ADT expectations vs FHIR Patient expectations).
    /// </summary>
    /// <param name="patterns">Domain field patterns to evaluate</param>
    /// <returns>Coverage score (0.0 to 1.0)</returns>
    Task<Result<double>> CalculateFieldCoverageAsync(FieldPatterns patterns);

    /// <summary>
    /// Generates field statistics from analyzed patterns.
    /// Universal output format populated with standard-specific insights.
    /// </summary>
    /// <param name="patterns">Domain field patterns to analyze</param>
    /// <returns>Universal field statistics</returns>
    Task<Result<Domain.Configuration.Entities.FieldStatistics>> CalculateFieldStatisticsAsync(FieldPatterns patterns);

    /// <summary>
    /// Detects vendor/system signatures from message patterns.
    /// Each plugin implements standard-specific vendor detection logic.
    /// </summary>
    /// <param name="patterns">Domain field patterns containing vendor signatures</param>
    /// <returns>Universal vendor analysis results</returns>
    Task<Result<VendorAnalysisResult>> DetectVendorSignaturesAsync(FieldPatterns patterns);
}