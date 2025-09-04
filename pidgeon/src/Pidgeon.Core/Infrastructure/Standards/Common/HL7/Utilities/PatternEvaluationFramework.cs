// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using System.Text.RegularExpressions;
using ConfigMatchType = Pidgeon.Core.Domain.Configuration.Entities.MatchType;

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7.Utilities;

/// <summary>
/// Provides pattern evaluation services for vendor detection rule processing.
/// Supports multiple matching strategies including exact, contains, regex, and string operations.
/// </summary>
public static class PatternEvaluationFramework
{
    /// <summary>
    /// Evaluates multiple pattern rule sets against message headers and calculates total confidence boost.
    /// </summary>
    /// <param name="patternRuleSets">Collection of rule sets to evaluate</param>
    /// <param name="headers">Message headers containing values to match against</param>
    /// <param name="logger">Logger for trace-level rule matching details</param>
    /// <returns>Total confidence boost from all matching rules</returns>
    public static double EvaluatePatternRuleSets(
        IEnumerable<PatternRuleSet> patternRuleSets,
        MessageHeaders headers,
        ILogger logger)
    {
        double totalConfidenceBoost = 0.0;

        foreach (var ruleSet in patternRuleSets)
        {
            if (string.IsNullOrWhiteSpace(ruleSet.HeaderValue))
                continue;

            foreach (var rule in ruleSet.Rules)
            {
                if (EvaluateRule(rule, ruleSet.HeaderValue, logger))
                {
                    totalConfidenceBoost += rule.ConfidenceBoost;
                    logger.LogTrace("HL7 {RuleType} rule matched: {Pattern} -> +{Boost}",
                        ruleSet.RuleType, rule.Pattern, rule.ConfidenceBoost);
                }
            }
        }

        return totalConfidenceBoost;
    }

    /// <summary>
    /// Evaluates a single detection rule against a header value using the rule's match type.
    /// </summary>
    /// <param name="rule">Detection rule to evaluate</param>
    /// <param name="headerValue">Header value to match against</param>
    /// <param name="logger">Logger for regex error reporting</param>
    /// <returns>True if rule matches the header value</returns>
    public static bool EvaluateRule(DetectionRule rule, string headerValue, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
            return false;

        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return rule.MatchType switch
        {
            ConfigMatchType.Exact => headerValue.Equals(rule.Pattern, comparison),
            ConfigMatchType.Contains => headerValue.Contains(rule.Pattern, comparison),
            ConfigMatchType.StartsWith => headerValue.StartsWith(rule.Pattern, comparison),
            ConfigMatchType.EndsWith => headerValue.EndsWith(rule.Pattern, comparison),
            ConfigMatchType.Regex => EvaluateRegexRule(rule, headerValue, logger),
            _ => false
        };
    }

    /// <summary>
    /// Evaluates a regex-based detection rule with error handling and logging.
    /// </summary>
    /// <param name="rule">Regex detection rule</param>
    /// <param name="headerValue">Value to match against</param>
    /// <param name="logger">Logger for regex compilation errors</param>
    /// <returns>True if regex matches the header value</returns>
    private static bool EvaluateRegexRule(DetectionRule rule, string headerValue, ILogger logger)
    {
        try
        {
            var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(rule.Pattern, options | RegexOptions.Compiled);
            return regex.IsMatch(headerValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid regex pattern in HL7 vendor detection: {Pattern}", rule.Pattern);
            return false;
        }
    }
}

/// <summary>
/// Represents a set of pattern rules to evaluate against a specific header field.
/// Groups rules by header field type for organized evaluation processing.
/// </summary>
public class PatternRuleSet
{
    /// <summary>
    /// Type of rule for logging purposes (e.g., "application", "facility", "message type").
    /// </summary>
    public required string RuleType { get; init; }

    /// <summary>
    /// Header value to evaluate rules against.
    /// </summary>
    public required string HeaderValue { get; init; }

    /// <summary>
    /// Collection of detection rules to evaluate against the header value.
    /// </summary>
    public required IEnumerable<DetectionRule> Rules { get; init; }
}