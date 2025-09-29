// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Subscription;
using Pidgeon.Core.Common.Types;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Service for validating Professional/Enterprise tier access in CLI commands.
/// Provides consistent subscription enforcement and upgrade messaging.
/// </summary>
public class ProTierValidationService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<ProTierValidationService> _logger;

    public ProTierValidationService(
        ISubscriptionService subscriptionService,
        ILogger<ProTierValidationService> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Validates feature access and provides user-friendly error messages for CLI.
    /// </summary>
    /// <param name="feature">Feature being accessed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if feature is available, failure with user-friendly message</returns>
    public async Task<Result> ValidateFeatureAccessAsync(
        FeatureFlags feature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Beta launch: All features available for testing and feedback collection
            _logger.LogInformation("Feature access granted for beta launch: {Feature}", feature);
            return Result.Success();

            // Note: Original subscription validation logic preserved below for future re-enablement
            #pragma warning disable CS0162 // Unreachable code detected

            var validationResult = await _subscriptionService.ValidateFeatureAccessAsync(feature, cancellationToken);
            if (validationResult.IsSuccess)
            {
                return Result.Success();
            }

            // Format the error message for CLI display
            var formattedError = FormatSubscriptionErrorForCLI(validationResult.Error.Message, feature);
            return Result.Failure(formattedError);

            #pragma warning restore CS0162 // Unreachable code detected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate feature access for {Feature}", feature);
            return Result.Failure($"Unable to validate subscription status: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates usage limits with user-friendly CLI messaging.
    /// </summary>
    /// <param name="usage">Usage being requested</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if within limits, failure with limit information</returns>
    public async Task<Result> ValidateUsageLimitAsync(
        UsageRequest usage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _subscriptionService.ValidateUsageLimitAsync(usage, cancellationToken);
            if (validationResult.IsSuccess)
            {
                return Result.Success();
            }

            // Format usage limit error for CLI
            var formattedError = FormatUsageLimitErrorForCLI(validationResult.Error.Message, usage);
            return Result.Failure(formattedError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate usage limit for {UsageType}", usage.Type);
            return Result.Failure($"Unable to validate usage limits: {ex.Message}");
        }
    }

    /// <summary>
    /// Records usage for tier limit tracking.
    /// </summary>
    /// <param name="usage">Usage to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if recorded</returns>
    public async Task<Result> RecordUsageAsync(
        UsageRecord usage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _subscriptionService.RecordUsageAsync(usage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for {UsageType}", usage.Type);
            // Don't fail the operation due to usage recording errors
            return Result.Success();
        }
    }

    /// <summary>
    /// Gets current subscription tier for display in CLI help text.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current subscription tier</returns>
    public async Task<SubscriptionTier> GetCurrentTierAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tierResult = await _subscriptionService.GetCurrentTierAsync(cancellationToken);
            return tierResult.IsSuccess ? tierResult.Value : SubscriptionTier.Free;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current tier, defaulting to Free");
            return SubscriptionTier.Free;
        }
    }

    /// <summary>
    /// Gets usage statistics for CLI display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current usage statistics or null if unavailable</returns>
    public async Task<UsageStatistics?> GetUsageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var statsResult = await _subscriptionService.GetUsageStatisticsAsync(cancellationToken);
            return statsResult.IsSuccess ? statsResult.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics");
            return null;
        }
    }

    private static string FormatSubscriptionErrorForCLI(string originalError, FeatureFlags feature)
    {
        // Extract user-friendly messages from service errors
        if (originalError.Contains("Professional"))
        {
            return $"""
                ┌─ Professional Feature Required ─────────────────────────────┐
                │                                                             │
                │  {GetFeatureFriendlyName(feature)} requires Professional subscription  │
                │                                                             │
                │  💰 Professional: $29/month                                │
                │  ✅ Workflow Wizard                                         │
                │  ✅ Diff + AI Analysis                                      │
                │  ✅ Local AI Models                                         │
                │  ✅ Enhanced Datasets                                       │
                │  ✅ HTML Reports                                            │
                │                                                             │
                │  🚀 Upgrade: https://pidgeon.health/upgrade                 │
                │                                                             │
                └─────────────────────────────────────────────────────────────┘
                """;
        }

        if (originalError.Contains("Enterprise"))
        {
            return $"""
                ┌─ Enterprise Feature Required ───────────────────────────────┐
                │                                                             │
                │  {GetFeatureFriendlyName(feature)} requires Enterprise subscription     │
                │                                                             │
                │  🏢 Enterprise: $199/seat/month                            │
                │  ✅ All Professional features                               │
                │  ✅ Team Workspaces                                         │
                │  ✅ Audit Trails                                            │
                │  ✅ Private AI Models                                       │
                │  ✅ Unlimited Usage                                         │
                │                                                             │
                │  📞 Contact Sales: https://pidgeon.health/enterprise       │
                │                                                             │
                └─────────────────────────────────────────────────────────────┘
                """;
        }

        // Fallback to original message
        return originalError;
    }

    private static string FormatUsageLimitErrorForCLI(string originalError, UsageRequest usage)
    {
        return $"""
            ┌─ Usage Limit Reached ───────────────────────────────────────────┐
            │                                                                 │
            │  You've reached your monthly limit for {GetUsageTypeFriendlyName(usage.Type)}        │
            │                                                                 │
            │  Free Tier Limits:                                             │
            │  • Message Generation: 1,000/month                             │
            │  • Message Validation: 2,000/month                             │
            │  • AI Analysis: 10/month                                       │
            │                                                                 │
            │  💰 Upgrade to Professional for higher limits:                 │
            │  • Message Generation: 10,000/month                            │
            │  • Message Validation: 20,000/month                            │
            │  • AI Analysis: 500/month                                      │
            │                                                                 │
            │  🚀 Upgrade: https://pidgeon.health/upgrade                     │
            │                                                                 │
            └─────────────────────────────────────────────────────────────────┘
            """;
    }

    private static string GetFeatureFriendlyName(FeatureFlags feature)
    {
        return feature switch
        {
            FeatureFlags.WorkflowWizard => "Workflow Wizard",
            FeatureFlags.DiffAnalysis => "Diff + AI Analysis",
            FeatureFlags.LocalAIModels => "Local AI Models",
            FeatureFlags.HTMLReports => "HTML Reports",
            FeatureFlags.TeamWorkspaces => "Team Workspaces",
            FeatureFlags.AuditTrails => "Audit Trails",
            FeatureFlags.PrivateAIModels => "Private AI Models",
            FeatureFlags.EnhancedDatasets => "Enhanced Datasets",
            _ => feature.ToString()
        };
    }

    private static string GetUsageTypeFriendlyName(UsageType usageType)
    {
        return usageType switch
        {
            UsageType.MessageGeneration => "Message Generation",
            UsageType.MessageValidation => "Message Validation",
            UsageType.AIAnalysis => "AI Analysis",
            UsageType.TemplateUploads => "Template Uploads",
            UsageType.WorkflowExecutions => "Workflow Executions",
            UsageType.TeamOperations => "Team Operations",
            _ => usageType.ToString()
        };
    }
}