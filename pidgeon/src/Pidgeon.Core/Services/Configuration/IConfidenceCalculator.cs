// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Service responsible for calculating confidence scores for configuration analysis.
/// Single responsibility: "How confident are we in this analysis?"
/// </summary>
public interface IConfidenceCalculator
{
    /// <summary>
    /// Calculates confidence score for field pattern analysis.
    /// Target accuracy: 85%+ for valid vendor detection.
    /// </summary>
    /// <param name="fieldPatterns">The analyzed field patterns</param>
    /// <param name="sampleSize">Number of messages analyzed</param>
    /// <returns>Confidence score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateFieldPatternConfidenceAsync(
        FieldPatterns fieldPatterns, 
        int sampleSize);

    /// <summary>
    /// Calculates confidence for vendor signature detection.
    /// </summary>
    /// <param name="signature">The detected vendor signature</param>
    /// <param name="matchedPatterns">Number of patterns that matched</param>
    /// <param name="totalPatterns">Total patterns evaluated</param>
    /// <returns>Confidence score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateVendorConfidenceAsync(
        VendorSignature signature,
        int matchedPatterns,
        int totalPatterns);

    /// <summary>
    /// Calculates overall configuration confidence combining multiple factors.
    /// </summary>
    /// <param name="vendorConfidence">Vendor detection confidence</param>
    /// <param name="fieldConfidence">Field pattern confidence</param>
    /// <param name="sampleSize">Number of messages analyzed</param>
    /// <returns>Overall confidence score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateOverallConfidenceAsync(
        double vendorConfidence,
        double fieldConfidence,
        int sampleSize);
}