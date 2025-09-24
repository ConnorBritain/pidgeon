// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Application.Interfaces.Subscription;
using Pidgeon.Core.Common.Types;

namespace Pidgeon.Core.Application.Services.Subscription;

/// <summary>
/// Implementation of subscription tier management and feature access validation.
/// Handles Free/Professional/Enterprise tier enforcement and usage tracking.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<SubscriptionService> _logger;

    // Development override for bypassing subscription checks
    private bool _developmentMode = false;

    public SubscriptionService(
        IConfigurationService configurationService,
        ILogger<SubscriptionService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;

        // TODO: Read development mode from configuration
        _developmentMode = Environment.GetEnvironmentVariable("PIDGEON_DEV_MODE") == "true";
    }

    public async Task<Result<SubscriptionTier>> GetCurrentTierAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Development mode override
            if (_developmentMode)
            {
                _logger.LogDebug("Development mode active - returning Enterprise tier");
                return Result<SubscriptionTier>.Success(SubscriptionTier.Enterprise);
            }

            var configResult = await _configurationService.GetCurrentConfigurationAsync();
            if (configResult.IsFailure)
            {
                _logger.LogWarning("Failed to get configuration, defaulting to Free tier: {Error}",
                    configResult.Error.Message);
                return Result<SubscriptionTier>.Success(SubscriptionTier.Free);
            }

            // TODO: Implement actual subscription tier retrieval from user account
            // For now, check if user has configured any Professional features
            var config = configResult.Value;

            // TODO: Check for subscription key or license file
            // Beta launch: Default to Free tier (all features enabled for testing)
            // Future: Will check subscription configuration here
            // if (!string.IsNullOrEmpty(config.DefaultAIProvider))
            // {
            //     _logger.LogDebug("AI provider configured - assuming Professional tier");
            //     return Result<SubscriptionTier>.Success(SubscriptionTier.Professional);
            // }

            // Default to Free tier
            _logger.LogDebug("No subscription indicators found - defaulting to Free tier");
            return Result<SubscriptionTier>.Success(SubscriptionTier.Free);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine subscription tier");
            return Result<SubscriptionTier>.Failure($"Failed to determine subscription tier: {ex.Message}");
        }
    }

    public async Task<Result> ValidateFeatureAccessAsync(
        FeatureFlags feature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tierResult = await GetCurrentTierAsync(cancellationToken);
            if (tierResult.IsFailure)
            {
                return Result.Failure(tierResult.Error);
            }

            var currentTier = tierResult.Value;
            var availableFeatures = GetFeaturesForTier(currentTier);

            if (availableFeatures.HasFlag(feature))
            {
                _logger.LogDebug("Feature {Feature} is available in {Tier} tier", feature, currentTier);
                return Result.Success();
            }

            // Feature not available - provide upgrade guidance
            var requiredTier = GetMinimumTierForFeature(feature);
            var upgradeMessage = GenerateUpgradeMessage(feature, currentTier, requiredTier);

            _logger.LogInformation("Feature {Feature} requires {RequiredTier} tier, user has {CurrentTier}",
                feature, requiredTier, currentTier);

            return Result.Failure(upgradeMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate feature access for {Feature}", feature);
            return Result.Failure($"Failed to validate feature access: {ex.Message}");
        }
    }

    public async Task<Result> ValidateUsageLimitAsync(
        UsageRequest usage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tierResult = await GetCurrentTierAsync(cancellationToken);
            if (tierResult.IsFailure)
            {
                return Result.Failure(tierResult.Error);
            }

            var currentTier = tierResult.Value;

            // Enterprise tier has unlimited usage
            if (currentTier == SubscriptionTier.Enterprise)
            {
                return Result.Success();
            }

            var limits = GetUsageLimitsForTier(currentTier);
            if (!limits.ContainsKey(usage.Type))
            {
                // No limit defined for this usage type
                return Result.Success();
            }

            var limit = limits[usage.Type];

            // TODO: Implement actual usage tracking against storage
            // For now, allow usage but log for future implementation
            _logger.LogDebug("Usage validation: {Type}={Amount}, Limit={Limit}, Tier={Tier}",
                usage.Type, usage.Amount, limit, currentTier);

            // Placeholder: always allow for initial implementation
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate usage limit for {UsageType}", usage.Type);
            return Result.Failure($"Failed to validate usage limit: {ex.Message}");
        }
    }

    public async Task<Result> RecordUsageAsync(
        UsageRecord usage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement actual usage recording to storage
            _logger.LogDebug("Recording usage: {Type}={Amount} at {Timestamp}",
                usage.Type, usage.Amount, usage.Timestamp);

            await Task.Yield(); // Placeholder for async operation
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for {UsageType}", usage.Type);
            return Result.Failure($"Failed to record usage: {ex.Message}");
        }
    }

    public async Task<Result<UsageStatistics>> GetUsageStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tierResult = await GetCurrentTierAsync(cancellationToken);
            if (tierResult.IsFailure)
            {
                return Result<UsageStatistics>.Failure(tierResult.Error);
            }

            var currentTier = tierResult.Value;
            var limits = GetUsageLimitsForTier(currentTier);

            // TODO: Implement actual usage statistics from storage
            var now = DateTimeOffset.UtcNow;
            var billingPeriodStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

            // Placeholder statistics
            var usageByType = new Dictionary<UsageType, int>
            {
                [UsageType.MessageGeneration] = 0,
                [UsageType.MessageValidation] = 0,
                [UsageType.AIAnalysis] = 0,
                [UsageType.WorkflowExecutions] = 0
            };

            var percentageUsed = limits.ToDictionary(
                kvp => kvp.Key,
                kvp => usageByType.GetValueOrDefault(kvp.Key, 0) / (double)kvp.Value * 100.0);

            var statistics = new UsageStatistics(
                billingPeriodStart,
                billingPeriodEnd,
                usageByType,
                limits,
                percentageUsed);

            return Result<UsageStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics");
            return Result<UsageStatistics>.Failure($"Failed to get usage statistics: {ex.Message}");
        }
    }

    public async Task<Result<UpgradeInformation>> GetUpgradeInformationAsync(
        SubscriptionTier targetTier,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tierResult = await GetCurrentTierAsync(cancellationToken);
            if (tierResult.IsFailure)
            {
                return Result<UpgradeInformation>.Failure(tierResult.Error);
            }

            var currentTier = tierResult.Value;

            if (currentTier >= targetTier)
            {
                return Result<UpgradeInformation>.Failure(
                    $"Already subscribed to {currentTier} tier or higher");
            }

            var pricing = GetPricingForTier(targetTier);
            var newFeatures = GetFeaturesForTier(targetTier) & ~GetFeaturesForTier(currentTier);
            var usageLimitChanges = GetUsageLimitChanges(currentTier, targetTier);

            var upgradeInfo = new UpgradeInformation(
                currentTier,
                targetTier,
                pricing.MonthlyPrice,
                pricing.Currency,
                newFeatures,
                usageLimitChanges,
                pricing.UpgradeUrl);

            return Result<UpgradeInformation>.Success(upgradeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get upgrade information for {TargetTier}", targetTier);
            return Result<UpgradeInformation>.Failure($"Failed to get upgrade information: {ex.Message}");
        }
    }

    private static FeatureFlags GetFeaturesForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => FeatureFlags.FreeTierFeatures,
            SubscriptionTier.Professional => FeatureFlags.ProfessionalTierFeatures,
            SubscriptionTier.Enterprise => FeatureFlags.EnterpriseTierFeatures,
            _ => FeatureFlags.FreeTierFeatures
        };
    }

    private static SubscriptionTier GetMinimumTierForFeature(FeatureFlags feature)
    {
        if (FeatureFlags.FreeTierFeatures.HasFlag(feature))
            return SubscriptionTier.Free;
        if (FeatureFlags.ProfessionalTierFeatures.HasFlag(feature))
            return SubscriptionTier.Professional;
        if (FeatureFlags.EnterpriseTierFeatures.HasFlag(feature))
            return SubscriptionTier.Enterprise;

        return SubscriptionTier.Enterprise; // Default to highest tier for unknown features
    }

    private static string GenerateUpgradeMessage(
        FeatureFlags feature,
        SubscriptionTier currentTier,
        SubscriptionTier requiredTier)
    {
        var featureName = GetFeatureFriendlyName(feature);
        var tierName = requiredTier.ToString();

        return requiredTier switch
        {
            SubscriptionTier.Professional =>
                $"🔒 {featureName} requires Professional subscription ($29/month). " +
                $"Upgrade at https://pidgeon.health/upgrade",

            SubscriptionTier.Enterprise =>
                $"🏢 {featureName} requires Enterprise subscription ($199/seat/month). " +
                $"Contact sales at https://pidgeon.health/enterprise",

            _ => $"🔒 {featureName} requires {tierName} subscription. Visit https://pidgeon.health/pricing"
        };
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
            _ => feature.ToString()
        };
    }

    private static Dictionary<UsageType, int> GetUsageLimitsForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Free => new Dictionary<UsageType, int>
            {
                [UsageType.MessageGeneration] = 1000,  // 1k messages per month
                [UsageType.MessageValidation] = 2000,  // 2k validations per month
                [UsageType.AIAnalysis] = 10,           // 10 AI analyses per month
                [UsageType.TemplateUploads] = 5,       // 5 template uploads per month
                [UsageType.WorkflowExecutions] = 0     // No workflows in free tier
            },

            SubscriptionTier.Professional => new Dictionary<UsageType, int>
            {
                [UsageType.MessageGeneration] = 10000,  // 10k messages per month
                [UsageType.MessageValidation] = 20000,  // 20k validations per month
                [UsageType.AIAnalysis] = 500,           // 500 AI analyses per month
                [UsageType.TemplateUploads] = 100,      // 100 template uploads per month
                [UsageType.WorkflowExecutions] = 1000   // 1k workflow executions per month
            },

            SubscriptionTier.Enterprise => new Dictionary<UsageType, int>
            {
                // Unlimited usage for Enterprise tier
            },

            _ => new Dictionary<UsageType, int>()
        };
    }

    private static (decimal MonthlyPrice, string Currency, string UpgradeUrl) GetPricingForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Professional => (29m, "USD", "https://pidgeon.health/upgrade/professional"),
            SubscriptionTier.Enterprise => (199m, "USD", "https://pidgeon.health/upgrade/enterprise"),
            _ => (0m, "USD", "https://pidgeon.health/pricing")
        };
    }

    private static Dictionary<UsageType, int> GetUsageLimitChanges(
        SubscriptionTier currentTier,
        SubscriptionTier targetTier)
    {
        var currentLimits = GetUsageLimitsForTier(currentTier);
        var targetLimits = GetUsageLimitsForTier(targetTier);

        var changes = new Dictionary<UsageType, int>();

        foreach (var usageType in Enum.GetValues<UsageType>())
        {
            var currentLimit = currentLimits.GetValueOrDefault(usageType, 0);
            var targetLimit = targetLimits.GetValueOrDefault(usageType, int.MaxValue);

            if (targetLimit != currentLimit)
            {
                changes[usageType] = targetLimit;
            }
        }

        return changes;
    }
}