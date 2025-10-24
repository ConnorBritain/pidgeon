using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Interop;

namespace Pidgeon.Core.Infrastructure.Interop;

/// <summary>
/// Generates semantic paths from imported HL7 v2-to-FHIR mapping data.
/// Implements the two-tier progressive disclosure system (Essential vs Advanced).
/// </summary>
public class SemanticPathGenerator
{
    private readonly ILogger<SemanticPathGenerator> _logger;

    public SemanticPathGenerator(ILogger<SemanticPathGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeneratedSemanticPath>> GenerateSemanticPathsAsync(ImportedMappingData mappingData)
    {
        var paths = new List<GeneratedSemanticPath>();

        // Generate paths from segment mappings (primary source)
        foreach (var (fileName, rows) in mappingData.SegmentMappings)
        {
            var segmentPaths = await GeneratePathsFromSegmentMappingAsync(fileName, rows);
            paths.AddRange(segmentPaths);
        }

        // Classify into tiers and deduplicate
        var classifiedPaths = ClassifyPathsIntoTiers(paths);

        _logger.LogInformation("Generated {Total} semantic paths: {Essential} essential, {Advanced} advanced",
            classifiedPaths.Count,
            classifiedPaths.Count(p => p.Tier == SemanticPathTier.Essential),
            classifiedPaths.Count(p => p.Tier == SemanticPathTier.Advanced));

        return classifiedPaths;
    }

    private async Task<IList<GeneratedSemanticPath>> GeneratePathsFromSegmentMappingAsync(
        string fileName,
        IReadOnlyList<SegmentMappingRow> rows)
    {
        var paths = new List<GeneratedSemanticPath>();
        var segmentInfo = ExtractSegmentInfo(fileName);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.HL7Identifier) || string.IsNullOrWhiteSpace(row.FHIRAttribute))
                continue;

            var semanticPath = GenerateSemanticPathFromMapping(segmentInfo, row);
            if (!string.IsNullOrWhiteSpace(semanticPath))
            {
                paths.Add(new GeneratedSemanticPath
                {
                    SemanticPath = semanticPath,
                    Description = row.HL7Name ?? "",
                    HL7Field = row.HL7Identifier,
                    FHIRPath = row.FHIRAttribute,
                    HL7DataType = row.HL7DataType ?? "",
                    FHIRDataType = row.FHIRDataType ?? "",
                    Category = DetermineCategory(segmentInfo.SegmentType),
                    Condition = row.Condition,
                    Comments = row.Comments,
                    SourceFile = fileName,
                    Tier = SemanticPathTier.Essential // Will be reclassified later
                });
            }
        }

        await Task.Yield(); // Make this async for future database operations
        return paths;
    }

    private string GenerateSemanticPathFromMapping(SegmentInfo segmentInfo, SegmentMappingRow row)
    {
        // Generate semantic paths based on segment type and FHIR mapping
        var basePath = segmentInfo.SegmentType.ToLowerInvariant() switch
        {
            "pid" => "patient",
            "pv1" => "encounter",
            "msh" => "message",
            "obx" => "observation",
            "orc" => "order",
            "dg1" => "diagnosis",
            "al1" => "allergy",
            "in1" => "insurance",
            _ => segmentInfo.SegmentType.ToLowerInvariant()
        };

        // Map common HL7 fields to semantic paths
        var semanticField = row.HL7Identifier switch
        {
            "PID-3" => "mrn",
            "PID-5" => "name",
            "PID-7" => "dateOfBirth",
            "PID-8" => "sex",
            "PID-11" => "address",
            "PID-13" => "phone",
            "PV1-2" => "class",
            "PV1-3" => "location",
            "PV1-4" => "type",
            "PV1-44" => "date",
            "MSH-7" => "timestamp",
            "MSH-4" => "facility",
            "MSH-3" => "application",
            _ => DeriveSemanticFieldFromFHIR(row.FHIRAttribute, row.HL7Name)
        };

        if (string.IsNullOrWhiteSpace(semanticField))
            return "";

        return $"{basePath}.{semanticField}";
    }

    private string DeriveSemanticFieldFromFHIR(string? fhirAttribute, string? hl7Name)
    {
        if (string.IsNullOrWhiteSpace(fhirAttribute))
            return "";

        // Extract meaningful semantic names from FHIR paths
        var field = fhirAttribute.ToLowerInvariant();

        if (field.Contains("identifier")) return "identifier";
        if (field.Contains("name")) return "name";
        if (field.Contains("address")) return "address";
        if (field.Contains("telecom")) return "contact";
        if (field.Contains("birthdate")) return "dateOfBirth";
        if (field.Contains("gender")) return "sex";
        if (field.Contains("extension") && hl7Name?.Contains("Maiden") == true) return "mothersMaidenName";

        // Fallback to simplified field name
        var parts = field.Split('[', '.', ']');
        return parts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && !char.IsDigit(p[0])) ?? "";
    }

    private IReadOnlyList<GeneratedSemanticPath> ClassifyPathsIntoTiers(IList<GeneratedSemanticPath> paths)
    {
        var classified = new List<GeneratedSemanticPath>();

        foreach (var path in paths)
        {
            var tier = ClassifyPathTier(path);
            classified.Add(path with { Tier = tier });
        }

        // Deduplicate by semantic path, preferring essential tier
        var deduplicated = classified
            .GroupBy(p => p.SemanticPath)
            .Select(g => g.OrderBy(p => p.Tier).First())
            .ToList();

        return deduplicated;
    }

    private SemanticPathTier ClassifyPathTier(GeneratedSemanticPath path)
    {
        // Essential paths: Core fields used in 80% of testing scenarios
        var essentialPaths = new HashSet<string>
        {
            "patient.mrn", "patient.name", "patient.dateOfBirth", "patient.sex",
            "patient.address", "patient.phone",
            "encounter.location", "encounter.type", "encounter.class", "encounter.date",
            "message.timestamp", "message.facility", "message.application",
            "order.identifier", "observation.value", "diagnosis.code"
        };

        if (essentialPaths.Contains(path.SemanticPath))
            return SemanticPathTier.Essential;

        // Advanced: Extensions, complex hierarchies, conditional logic
        if (!string.IsNullOrWhiteSpace(path.Condition) ||
            path.FHIRPath?.Contains("extension") == true ||
            path.SemanticPath.Split('.').Length > 2)
        {
            return SemanticPathTier.Advanced;
        }

        // Default to advanced for now - we'll refine this
        return SemanticPathTier.Advanced;
    }

    private string DetermineCategory(string segmentType)
    {
        return segmentType.ToUpperInvariant() switch
        {
            "PID" => "patient",
            "PV1" => "encounter",
            "MSH" => "message",
            "OBX" => "observation",
            "ORC" => "order",
            "DG1" => "diagnosis",
            "AL1" => "allergy",
            "IN1" => "insurance",
            _ => "general"
        };
    }

    private SegmentInfo ExtractSegmentInfo(string fileName)
    {
        // Extract from "HL7 Segment - FHIR R4_ PID[Patient] - PID.csv"
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"([A-Z0-9]{2,3})\[([^\]]+)\]");
        if (match.Success)
        {
            return new SegmentInfo(match.Groups[1].Value, match.Groups[2].Value);
        }
        return new SegmentInfo("UNKNOWN", "Unknown");
    }
}

/// <summary>
/// Generated semantic path definition with tier classification and metadata
/// </summary>
public record GeneratedSemanticPath
{
    public string SemanticPath { get; init; } = "";
    public string Description { get; init; } = "";
    public string HL7Field { get; init; } = "";
    public string FHIRPath { get; init; } = "";
    public string HL7DataType { get; init; } = "";
    public string FHIRDataType { get; init; } = "";
    public string Category { get; init; } = "";
    public SemanticPathTier Tier { get; init; }
    public string? Condition { get; init; }
    public string? Comments { get; init; }
    public string SourceFile { get; init; } = "";
}

public enum SemanticPathTier
{
    Essential,  // 25-30 paths for 80% of use cases
    Advanced    // 200+ paths for complete interoperability
}

public record SegmentInfo(string SegmentType, string FHIRResource);