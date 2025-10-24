// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Common.Types;

namespace Pidgeon.Core.Application.Interfaces.Subscription;

/// <summary>
/// Service for managing subscription tiers and feature access validation.
/// Enforces Free/Professional/Enterprise tier boundaries and usage limits.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets the current user's subscription tier.
    /// </summary>
    /// <returns>Current subscription tier</returns>
    Task<Result<SubscriptionTier>> GetCurrentTierAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a feature is available in the current subscription tier.
    /// </summary>
    /// <param name="feature">Feature to check access for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if feature is available, failure with upgrade message if not</returns>
    Task<Result> ValidateFeatureAccessAsync(
        FeatureFlags feature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a usage operation is within tier limits.
    /// </summary>
    /// <param name="usage">Usage type and amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if within limits, failure with limit information if exceeded</returns>
    Task<Result> ValidateUsageLimitAsync(
        UsageRequest usage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records usage for tier limit tracking.
    /// </summary>
    /// <param name="usage">Usage to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success if recorded, failure on error</returns>
    Task<Result> RecordUsageAsync(
        UsageRecord usage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current usage statistics for the subscription period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics for current billing period</returns>
    Task<Result<UsageStatistics>> GetUsageStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upgrade information for moving to a higher tier.
    /// </summary>
    /// <param name="targetTier">Desired subscription tier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upgrade information and pricing</returns>
    Task<Result<UpgradeInformation>> GetUpgradeInformationAsync(
        SubscriptionTier targetTier,
        CancellationToken cancellationToken = default);
}