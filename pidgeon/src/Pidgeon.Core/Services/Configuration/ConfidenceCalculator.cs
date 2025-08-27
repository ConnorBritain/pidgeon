// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Standard-agnostic confidence calculator that delegates to standard-specific plugins.
/// Follows sacred principle: Plugin architecture with no hardcoded standard logic.
/// </summary>
internal class ConfidenceCalculator : IConfidenceCalculator
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly ILogger<ConfidenceCalculator> _logger;
    
    // Confidence calculation weights
    private const double MinimumSampleSizeForHighConfidence = 50;
    private const double OptimalSampleSize = 100;
    private const double SampleSizeWeight = 0.3;
    private const double ConsistencyWeight = 0.5;
    private const double CoverageWeight = 0.2;

    public ConfidenceCalculator(
        IStandardPluginRegistry pluginRegistry,
        ILogger<ConfidenceCalculator> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateFieldPatternConfidenceAsync(
        FieldPatterns fieldPatterns, 
        int sampleSize)
    {
        try
        {
            if (fieldPatterns == null)
                return Result<double>.Failure("Field patterns cannot be null");
                
            if (sampleSize <= 0)
                return Result<double>.Failure("Sample size must be positive");

            _logger.LogDebug("Calculating field pattern confidence for {SampleSize} samples", sampleSize);

            // Calculate sample size confidence factor (0.0 to 1.0)
            var sampleSizeConfidence = CalculateSampleSizeConfidence(sampleSize);
            
            // Calculate field consistency confidence (how consistent are the patterns)
            var consistencyConfidence = await CalculateFieldConsistencyAsync(fieldPatterns);
            
            // Calculate coverage confidence using standard-specific plugin if available
            var coverageConfidence = await CalculateFieldCoverageAsync(fieldPatterns);
            
            // Weighted average of all confidence factors
            var overallConfidence = 
                (sampleSizeConfidence * SampleSizeWeight) +
                (consistencyConfidence * ConsistencyWeight) +
                (coverageConfidence * CoverageWeight);
                
            // Ensure confidence is between 0 and 1
            overallConfidence = Math.Min(1.0, Math.Max(0.0, overallConfidence));
            
            _logger.LogInformation(
                "Field pattern confidence calculated: {Confidence:P1} " +
                "(Sample: {SampleConfidence:P1}, Consistency: {ConsistencyConfidence:P1}, Coverage: {CoverageConfidence:P1})",
                overallConfidence, sampleSizeConfidence, consistencyConfidence, coverageConfidence);

            return Result<double>.Success(overallConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating field pattern confidence");
            return Result<double>.Failure($"Confidence calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateVendorConfidenceAsync(
        VendorSignature signature,
        int matchedPatterns,
        int totalPatterns)
    {
        try
        {
            if (signature == null)
                return Result<double>.Failure("Vendor signature cannot be null");
                
            if (totalPatterns <= 0)
                return Result<double>.Failure("Total patterns must be positive");
                
            if (matchedPatterns < 0 || matchedPatterns > totalPatterns)
                return Result<double>.Failure("Invalid matched patterns count");

            _logger.LogDebug("Calculating vendor confidence: {Matched}/{Total} patterns matched", 
                matchedPatterns, totalPatterns);

            // Base confidence from signature
            var baseConfidence = signature.Confidence;
            
            // Pattern match ratio
            var matchRatio = totalPatterns > 0 ? (double)matchedPatterns / totalPatterns : 0.0;
            
            // Combine base confidence with match ratio
            var confidence = (baseConfidence * 0.6) + (matchRatio * 0.4);
            
            // Apply penalty if too few patterns matched
            if (matchedPatterns < 2 && totalPatterns >= 5)
            {
                confidence *= 0.5; // Low confidence if very few patterns matched
            }
            
            confidence = Math.Min(1.0, Math.Max(0.0, confidence));
            
            _logger.LogInformation("Vendor confidence calculated: {Confidence:P1} for {VendorName}",
                confidence, signature.Name);

            return await Task.FromResult(Result<double>.Success(confidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating vendor confidence");
            return Result<double>.Failure($"Vendor confidence calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateOverallConfidenceAsync(
        double vendorConfidence,
        double fieldConfidence,
        int sampleSize)
    {
        try
        {
            if (vendorConfidence < 0 || vendorConfidence > 1)
                return Result<double>.Failure("Vendor confidence must be between 0 and 1");
                
            if (fieldConfidence < 0 || fieldConfidence > 1)
                return Result<double>.Failure("Field confidence must be between 0 and 1");
                
            if (sampleSize <= 0)
                return Result<double>.Failure("Sample size must be positive");

            // Calculate sample size modifier
            var sampleModifier = CalculateSampleSizeConfidence(sampleSize);
            
            // Weighted combination
            var baseConfidence = (vendorConfidence * 0.4) + (fieldConfidence * 0.6);
            
            // Apply sample size modifier
            var overallConfidence = baseConfidence * (0.7 + (sampleModifier * 0.3));
            
            // Ensure within bounds
            overallConfidence = Math.Min(1.0, Math.Max(0.0, overallConfidence));
            
            _logger.LogInformation(
                "Overall confidence calculated: {Confidence:P1} " +
                "(Vendor: {VendorConfidence:P1}, Field: {FieldConfidence:P1}, Samples: {SampleSize})",
                overallConfidence, vendorConfidence, fieldConfidence, sampleSize);

            return await Task.FromResult(Result<double>.Success(overallConfidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overall confidence");
            return Result<double>.Failure($"Overall confidence calculation failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates confidence based on sample size using logarithmic scale.
    /// </summary>
    private double CalculateSampleSizeConfidence(int sampleSize)
    {
        if (sampleSize >= OptimalSampleSize)
            return 1.0;
            
        if (sampleSize <= 0)
            return 0.0;
            
        // Logarithmic scale from 0 to optimal sample size
        var logOptimal = Math.Log(OptimalSampleSize + 1);
        var logSample = Math.Log(sampleSize + 1);
        
        return Math.Min(1.0, logSample / logOptimal);
    }

    /// <summary>
    /// Calculates field consistency across patterns.
    /// Standard-agnostic implementation based on pattern counts.
    /// </summary>
    private async Task<double> CalculateFieldConsistencyAsync(FieldPatterns fieldPatterns)
    {
        await Task.CompletedTask;
        
        // Standard-agnostic consistency check based on pattern presence
        if (fieldPatterns.SegmentPatterns == null || fieldPatterns.SegmentPatterns.Count == 0)
            return 0.3; // Low confidence if no patterns
            
        if (fieldPatterns.SegmentPatterns.Count < 3)
            return 0.6; // Medium confidence with few patterns
            
        // Calculate consistency based on field frequency variance
        var frequencies = new List<double>();
        foreach (var segment in fieldPatterns.SegmentPatterns.Values)
        {
            if (segment.FieldFrequencies != null)
            {
                frequencies.AddRange(segment.FieldFrequencies.Values.Select(f => f.Frequency));
            }
        }
        
        if (frequencies.Count == 0)
            return 0.5;
            
        // Calculate coefficient of variation
        var mean = frequencies.Average();
        var variance = frequencies.Select(f => Math.Pow(f - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        var coefficientOfVariation = mean > 0 ? stdDev / mean : 1.0;
        
        // Lower variation = higher consistency
        var consistency = Math.Max(0.0, 1.0 - (coefficientOfVariation / 2.0));
        
        return consistency;
    }

    /// <summary>
    /// Calculates coverage of expected fields using standard-specific plugin.
    /// Falls back to generic calculation if no plugin available.
    /// </summary>
    private async Task<double> CalculateFieldCoverageAsync(FieldPatterns fieldPatterns)
    {
        try
        {
            // Attempt to get standard-specific plugin for coverage calculation
            if (!string.IsNullOrWhiteSpace(fieldPatterns.Standard))
            {
                var plugin = _pluginRegistry.GetFieldAnalysisPlugin(fieldPatterns.Standard);
                if (plugin != null)
                {
                    _logger.LogDebug("Using {Standard} plugin for coverage calculation", fieldPatterns.Standard);
                    var coverageResult = await plugin.CalculateFieldCoverageAsync(fieldPatterns);
                    if (coverageResult.IsSuccess)
                        return coverageResult.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to use plugin for coverage calculation, using generic approach");
        }
        
        // Generic coverage calculation based on pattern presence
        if (fieldPatterns.SegmentPatterns == null || fieldPatterns.SegmentPatterns.Count == 0)
            return 0.3;
            
        // Basic heuristic: more segments and fields = better coverage
        var segmentCount = fieldPatterns.SegmentPatterns.Count;
        var totalFields = fieldPatterns.SegmentPatterns.Values
            .SelectMany(s => s.FieldFrequencies?.Keys ?? Enumerable.Empty<string>())
            .Distinct()
            .Count();
            
        // Normalize to 0-1 range with reasonable thresholds
        var segmentScore = Math.Min(1.0, segmentCount / 10.0);  // Assume 10+ segments is good coverage
        var fieldScore = Math.Min(1.0, totalFields / 50.0);     // Assume 50+ fields is good coverage
        
        return (segmentScore * 0.4) + (fieldScore * 0.6);
    }

    #endregion
}