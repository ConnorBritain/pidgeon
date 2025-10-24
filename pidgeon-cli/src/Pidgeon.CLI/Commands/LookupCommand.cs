// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Reference;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for healthcare standards reference lookup functionality.
/// Provides smart inference, search, and browsing capabilities across HL7, FHIR, and NCPDP standards.
/// </summary>
public class LookupCommand : CommandBuilderBase
{
    private readonly IStandardReferenceService _referenceService;

    public LookupCommand(
        ILogger<LookupCommand> logger,
        IStandardReferenceService referenceService)
        : base(logger)
    {
        _referenceService = referenceService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("lookup", "Look up healthcare standards definitions with smart inference");

        // Positional argument for element path
        var pathArg = new Argument<string?>("path")
        {
            Description = "Element path (e.g., PID.3.5, Patient.identifier, NewRx.Patient) - leave empty to browse",
            Arity = ArgumentArity.ZeroOrOne
        };

        // Options for different lookup modes
        var searchOption = CreateNullableOption("--search", "-s", "Search across all standards for elements matching query");
        var standardOption = CreateNullableOption("--standard", "-t", "Explicit standard (hl7v23, fhir-r4, ncpdp)");
        var vendorOption = CreateNullableOption("--vendor", "-v", "Show vendor-specific variations (epic, cerner, allscripts)");
        var formatOption = CreateOptionalOption("--format", "-f", "Output format: text|json|table", "text");
        var segmentsOption = CreateBooleanOption("--segments", "List all segments/resources in specified standard");
        var childrenOption = CreateBooleanOption("--children", "List child elements of specified path");
        var examplesOption = CreateBooleanOption("--examples", "Show detailed examples and usage patterns");
        var crossRefOption = CreateBooleanOption("--cross-ref", "Show cross-references to other standards");

        // Beta features
        var interactiveOption = CreateBooleanOption("--inter", "-i", "⚠️  Beta: Interactive TUI browsing mode (in development)");

        command.Add(pathArg);
        command.Add(searchOption);
        command.Add(standardOption);
        command.Add(vendorOption);
        command.Add(formatOption);
        command.Add(segmentsOption);
        command.Add(childrenOption);
        command.Add(examplesOption);
        command.Add(crossRefOption);
        command.Add(interactiveOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var path = parseResult.GetValue(pathArg);
                var search = parseResult.GetValue(searchOption);
                var standard = parseResult.GetValue(standardOption);
                var vendor = parseResult.GetValue(vendorOption);
                var format = parseResult.GetValue(formatOption);
                var showSegments = parseResult.GetValue(segmentsOption);
                var showChildren = parseResult.GetValue(childrenOption);
                var showExamples = parseResult.GetValue(examplesOption);
                var showCrossRef = parseResult.GetValue(crossRefOption);
                var interactive = parseResult.GetValue(interactiveOption);

                // Check for interactive TUI mode availability
                if (interactive)
                {
                    Console.WriteLine("⚠️  Interactive TUI browsing mode is currently in development.");
                    Console.WriteLine("💡 This feature will provide a terminal-based interface for exploring healthcare standards.");
                    Console.WriteLine("    For now, use standard lookup commands. Check release notes for updates.");
                    Console.WriteLine();
                }

                // Handle different operation modes
                if (!string.IsNullOrEmpty(search))
                {
                    return await HandleSearchAsync(search, standard, format, cancellationToken);
                }

                if (showSegments)
                {
                    if (string.IsNullOrEmpty(standard))
                    {
                        Console.WriteLine("Error: --segments requires --standard option");
                        Console.WriteLine("Usage: pidgeon lookup --segments --standard hl7v23");
                        return 1;
                    }
                    return await HandleListSegmentsAsync(standard, format, cancellationToken);
                }

                if (!string.IsNullOrEmpty(path))
                {
                    if (showChildren)
                    {
                        return await HandleListChildrenAsync(path, format, cancellationToken);
                    }
                    else
                    {
                        return await HandleElementLookupAsync(path, standard, vendor, format, showExamples, showCrossRef, cancellationToken);
                    }
                }

                // No specific operation - show help and suggestions
                return await HandleBrowseModeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during lookup");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task<int> HandleElementLookupAsync(
        string path, 
        string? explicitStandard, 
        string? vendor, 
        string? format,
        bool showExamples,
        bool showCrossRef,
        CancellationToken cancellationToken)
    {
        var result = !string.IsNullOrEmpty(explicitStandard)
            ? await _referenceService.LookupAsync(path, explicitStandard, cancellationToken)
            : await _referenceService.LookupAsync(path, cancellationToken);

        if (result.IsFailure)
        {
            Console.WriteLine($"❌ {result.Error.Message}");
            Console.WriteLine();
            
            // Try to provide helpful suggestions
            var inferResult = _referenceService.InferStandard(path);
            if (inferResult.IsSuccess)
            {
                Console.WriteLine($"💡 Suggestions for {inferResult.Value}:");
                // TODO: Get suggestions from the service
            }
            
            return 1;
        }

        var element = result.Value;

        // Handle vendor-specific lookup
        if (!string.IsNullOrEmpty(vendor))
        {
            // TODO: Implement vendor variation lookup
            Console.WriteLine($"🏥 Vendor-specific information for {vendor} not yet implemented");
            Console.WriteLine();
        }

        // Display the element information
        DisplayElement(element, format, showExamples, showCrossRef);
        
        return 0;
    }

    private async Task<int> HandleSearchAsync(string query, string? standard, string? format, CancellationToken cancellationToken)
    {
        Console.WriteLine($"🔍 Searching for '{query}'");
        if (!string.IsNullOrEmpty(standard))
        {
            Console.WriteLine($"   Standard: {standard}");
        }
        Console.WriteLine();

        var result = !string.IsNullOrEmpty(standard)
            ? await _referenceService.SearchAsync(query, standard, cancellationToken)
            : await _referenceService.SearchAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            Console.WriteLine($"❌ {result.Error.Message}");
            return 1;
        }

        var elements = result.Value;
        if (!elements.Any())
        {
            Console.WriteLine($"No results found for '{query}'");
            Console.WriteLine();
            Console.WriteLine("💡 Try:");
            Console.WriteLine("  • Broader search terms (e.g., 'patient' instead of 'patient name')");
            Console.WriteLine("  • Different spelling or abbreviations");
            Console.WriteLine("  • Browse available elements: pidgeon lookup --segments --standard hl7v23");
            return 0;
        }

        DisplaySearchResults(elements, format);
        return 0;
    }

    private async Task<int> HandleListSegmentsAsync(string standard, string? format, CancellationToken cancellationToken)
    {
        Console.WriteLine($"📋 Segments/Resources in {standard.ToUpperInvariant()}");
        Console.WriteLine();

        var result = await _referenceService.ListTopLevelElementsAsync(standard, cancellationToken);
        
        if (result.IsFailure)
        {
            Console.WriteLine($"❌ {result.Error.Message}");
            return 1;
        }

        var elements = result.Value;
        if (!elements.Any())
        {
            Console.WriteLine($"No segments found for standard '{standard}'");
            return 0;
        }

        DisplayElementList(elements, format);
        return 0;
    }

    private async Task<int> HandleListChildrenAsync(string parentPath, string? format, CancellationToken cancellationToken)
    {
        Console.WriteLine($"📂 Child elements of {parentPath}");
        Console.WriteLine();

        var result = await _referenceService.ListChildrenAsync(parentPath, cancellationToken);
        
        if (result.IsFailure)
        {
            Console.WriteLine($"❌ {result.Error.Message}");
            return 1;
        }

        var children = result.Value;
        if (!children.Any())
        {
            Console.WriteLine($"No child elements found for '{parentPath}'");
            return 0;
        }

        DisplayElementList(children, format);
        return 0;
    }

    private async Task<int> HandleBrowseModeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("🔍 Pidgeon Standards Reference");
        Console.WriteLine();
        
        // Show available standards
        var standardsResult = await _referenceService.GetSupportedStandardsAsync(cancellationToken);
        if (standardsResult.IsSuccess)
        {
            Console.WriteLine("📚 Supported Standards:");
            foreach (var kvp in standardsResult.Value)
            {
                Console.WriteLine($"  {kvp.Key.PadRight(10)} - {kvp.Value}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("💡 Usage Examples:");
        Console.WriteLine("  pidgeon lookup PID.3.5                    # Look up specific field");
        Console.WriteLine("  pidgeon lookup Patient.identifier          # FHIR resource element");
        Console.WriteLine("  pidgeon lookup --search \"medical record\"   # Search across all standards");
        Console.WriteLine("  pidgeon lookup PID --children              # List all PID fields");
        Console.WriteLine("  pidgeon lookup --segments --standard hl7v23 # Browse HL7 segments");
        Console.WriteLine();
        
        Console.WriteLine("🏥 Vendor Intelligence:");
        Console.WriteLine("  pidgeon lookup PID.3.5 --vendor epic      # Epic-specific patterns");
        Console.WriteLine("  pidgeon lookup MSH.3 --vendor cerner      # Cerner implementation notes");
        Console.WriteLine();
        
        Console.WriteLine("⚠️  Beta Features:");
        Console.WriteLine("  pidgeon lookup --inter                     # Visual terminal browser (in development)");
        Console.WriteLine("  pidgeon lookup PID.3 --cross-ref          # Cross-standard mappings");
        
        return 0;
    }

    private static void DisplayElement(
        global::Pidgeon.Core.Domain.Reference.Entities.StandardElement element, 
        string? format, 
        bool showExamples, 
        bool showCrossRef)
    {
        Console.WriteLine($"🔍 {element.Path}");
        Console.WriteLine($"   {element.Name}");
        Console.WriteLine();

        Console.WriteLine($"📋 Definition:");
        Console.WriteLine($"   Standard: {element.Standard} ({element.Version})");
        Console.WriteLine($"   Data Type: {element.DataType}");
        Console.WriteLine($"   Usage: {GetUsageDescription(element.Usage)}");
        if (element.MaxLength.HasValue)
        {
            Console.WriteLine($"   Max Length: {element.MaxLength}");
        }
        if (element.Position.HasValue)
        {
            Console.WriteLine($"   Position: {element.Position}");
        }
        if (!string.IsNullOrEmpty(element.Repeatability))
        {
            Console.WriteLine($"   Repeatability: {element.Repeatability}");
        }
        if (!string.IsNullOrEmpty(element.TableReference))
        {
            Console.WriteLine($"   Table: {element.TableReference}");
            Console.WriteLine($"   📋 Valid Values: → pidgeon lookup {element.TableReference}");
        }
        Console.WriteLine();

        Console.WriteLine($"📝 Description:");
        Console.WriteLine($"   {element.Description}");
        Console.WriteLine();

        if (element.Examples.Any())
        {
            Console.WriteLine($"💡 Examples:");
            foreach (var example in element.Examples.Take(3))
            {
                Console.WriteLine($"   • {example}");
            }
            Console.WriteLine();
        }

        if (element.ValidValues.Any())
        {
            Console.WriteLine($"✅ Valid Values:");
            foreach (var validValue in element.ValidValues.Take(5))
            {
                var deprecated = validValue.IsDeprecated ? " (deprecated)" : "";
                Console.WriteLine($"   {validValue.Code.PadRight(5)} - {validValue.Description}{deprecated}");
            }
            Console.WriteLine();
        }

        if (showExamples && element.VendorVariations.Any())
        {
            Console.WriteLine($"🏥 Vendor Variations:");
            foreach (var vendor in element.VendorVariations.Take(2))
            {
                Console.WriteLine($"   {vendor.Vendor}:");
                Console.WriteLine($"     {vendor.Note}");
                if (vendor.Examples.Any())
                {
                    Console.WriteLine($"     Examples: {string.Join(", ", vendor.Examples.Take(2))}");
                }
            }
            Console.WriteLine();
        }

        if (showCrossRef && element.CrossReferences.Any())
        {
            Console.WriteLine($"🔗 Cross-References:");
            foreach (var crossRef in element.CrossReferences.Take(3))
            {
                Console.WriteLine($"   {crossRef.Standard}: {crossRef.Path} ({crossRef.MappingType})");
                if (!string.IsNullOrEmpty(crossRef.Notes))
                {
                    Console.WriteLine($"     Note: {crossRef.Notes}");
                }
            }
            Console.WriteLine();
        }
    }

    private static void DisplaySearchResults(IReadOnlyList<global::Pidgeon.Core.Domain.Reference.Entities.StandardElement> elements, string? format)
    {
        Console.WriteLine($"Found {elements.Count} result(s):");
        Console.WriteLine();

        foreach (var element in elements.Take(10))
        {
            Console.WriteLine($"📍 {element.Path.PadRight(15)} {element.Name}");
            Console.WriteLine($"   {element.Standard} | {element.DataType} | {GetUsageDescription(element.Usage)}");
            Console.WriteLine($"   {TruncateDescription(element.Description, 80)}");
            Console.WriteLine();
        }

        if (elements.Count > 10)
        {
            Console.WriteLine($"... and {elements.Count - 10} more results");
            Console.WriteLine("💡 Use more specific search terms to narrow results");
            Console.WriteLine();
        }
    }

    private static void DisplayElementList(IReadOnlyList<global::Pidgeon.Core.Domain.Reference.Entities.StandardElement> elements, string? format)
    {
        foreach (var element in elements)
        {
            Console.WriteLine($"📍 {element.Path.PadRight(15)} {element.Name}");
            Console.WriteLine($"   {GetUsageDescription(element.Usage)} | {element.DataType}");
            if (!string.IsNullOrEmpty(element.Description) && element.Description.Length > 0)
            {
                Console.WriteLine($"   {TruncateDescription(element.Description, 80)}");
            }
            Console.WriteLine();
        }
    }

    private static string GetUsageDescription(string usage)
    {
        return usage switch
        {
            "R" => "Required",
            "O" => "Optional",
            "C" => "Conditional",
            "X" => "Not used",
            _ => usage
        };
    }

    private static string TruncateDescription(string description, int maxLength)
    {
        if (description.Length <= maxLength)
            return description;
        
        return description.Substring(0, maxLength - 3) + "...";
    }

}