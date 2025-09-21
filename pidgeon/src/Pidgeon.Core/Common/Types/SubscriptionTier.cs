// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Common.Types;

/// <summary>
/// Subscription tiers for Pidgeon platform feature access.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free tier - Core CLI functionality with basic limitations.
    /// Includes: Message generation, validation, de-identification, vendor detection.
    /// Limits: 25 medications, 50 names, basic datasets only.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Professional tier - Enhanced features and datasets.
    /// Includes: Workflow wizard, diff + AI triage, local AI models, enhanced datasets.
    /// Pricing: $29/month individual subscription.
    /// </summary>
    Professional = 1,

    /// <summary>
    /// Enterprise tier - Team collaboration and advanced governance.
    /// Includes: Team workspaces, audit trails, private AI models, unlimited usage.
    /// Pricing: $199/seat/month with team features.
    /// </summary>
    Enterprise = 2
}

/// <summary>
/// Feature flags for subscription tier enforcement.
/// </summary>
[Flags]
public enum FeatureFlags : long
{
    // ===== FREE TIER FEATURES =====
    /// <summary>Core message generation (always available)</summary>
    MessageGeneration = 1L << 0,

    /// <summary>Message validation (always available)</summary>
    MessageValidation = 1L << 1,

    /// <summary>On-premises de-identification (always available)</summary>
    DeIdentification = 1L << 2,

    /// <summary>Vendor pattern detection (always available)</summary>
    VendorDetection = 1L << 3,

    /// <summary>Basic semantic paths (always available)</summary>
    SemanticPaths = 1L << 4,

    /// <summary>Session import/export (always available)</summary>
    SessionManagement = 1L << 5,

    // ===== PROFESSIONAL TIER FEATURES =====
    /// <summary>Interactive workflow wizard</summary>
    WorkflowWizard = 1L << 10,

    /// <summary>Diff + AI triage functionality</summary>
    DiffAnalysis = 1L << 11,

    /// <summary>Local AI model integration</summary>
    LocalAIModels = 1L << 12,

    /// <summary>Enhanced datasets (more medications, edge cases)</summary>
    EnhancedDatasets = 1L << 13,

    /// <summary>HTML report generation</summary>
    HTMLReports = 1L << 14,

    /// <summary>Advanced configuration profiles</summary>
    AdvancedProfiles = 1L << 15,

    /// <summary>Cross-standard transforms (HL7↔FHIR↔NCPDP)</summary>
    CrossStandardTransforms = 1L << 16,

    /// <summary>FHIR search test harness</summary>
    FHIRSearchHarness = 1L << 17,

    /// <summary>Message studio (natural language generation)</summary>
    MessageStudio = 1L << 18,

    /// <summary>Vendor specification guide and trust hub</summary>
    VendorSpecGuide = 1L << 19,

    // ===== ENTERPRISE TIER FEATURES =====
    /// <summary>Team workspaces and collaboration</summary>
    TeamWorkspaces = 1L << 20,

    /// <summary>Audit trails and compliance reporting</summary>
    AuditTrails = 1L << 21,

    /// <summary>Private AI model hosting</summary>
    PrivateAIModels = 1L << 22,

    /// <summary>Role-based access controls</summary>
    RoleBasedAccess = 1L << 23,

    /// <summary>Advanced integration guideline validation</summary>
    AdvancedIGValidation = 1L << 24,

    /// <summary>Unlimited usage (no rate limits)</summary>
    UnlimitedUsage = 1L << 25,

    // ===== TIER COMBINATIONS =====
    /// <summary>All features available in Free tier</summary>
    FreeTierFeatures = MessageGeneration | MessageValidation | DeIdentification |
                      VendorDetection | SemanticPaths | SessionManagement,

    /// <summary>All features available in Professional tier</summary>
    ProfessionalTierFeatures = FreeTierFeatures | WorkflowWizard | DiffAnalysis |
                              LocalAIModels | EnhancedDatasets | HTMLReports | AdvancedProfiles |
                              CrossStandardTransforms | FHIRSearchHarness | MessageStudio | VendorSpecGuide,

    /// <summary>All features available in Enterprise tier</summary>
    EnterpriseTierFeatures = ProfessionalTierFeatures | TeamWorkspaces | AuditTrails |
                            PrivateAIModels | RoleBasedAccess | AdvancedIGValidation | UnlimitedUsage
}

/// <summary>
/// Usage types for tier limit enforcement.
/// </summary>
public enum UsageType
{
    /// <summary>Message generation requests</summary>
    MessageGeneration,

    /// <summary>Message validation requests</summary>
    MessageValidation,

    /// <summary>AI analysis requests (diff, triage)</summary>
    AIAnalysis,

    /// <summary>Template marketplace uploads</summary>
    TemplateUploads,

    /// <summary>Workflow executions</summary>
    WorkflowExecutions,

    /// <summary>Team collaboration operations</summary>
    TeamOperations
}

/// <summary>
/// Usage request for validation against tier limits.
/// </summary>
/// <param name="Type">Type of usage being requested</param>
/// <param name="Amount">Amount of usage (e.g., number of messages, API calls)</param>
/// <param name="Context">Additional context for usage calculation</param>
public record UsageRequest(
    UsageType Type,
    int Amount,
    Dictionary<string, object>? Context = null);

/// <summary>
/// Usage record for tracking consumption.
/// </summary>
/// <param name="Type">Type of usage consumed</param>
/// <param name="Amount">Amount consumed</param>
/// <param name="Timestamp">When the usage occurred</param>
/// <param name="Context">Additional context for usage tracking</param>
public record UsageRecord(
    UsageType Type,
    int Amount,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Context = null);

/// <summary>
/// Usage statistics for current billing period.
/// </summary>
/// <param name="BillingPeriodStart">Start of current billing period</param>
/// <param name="BillingPeriodEnd">End of current billing period</param>
/// <param name="UsageByType">Usage amounts by type for current period</param>
/// <param name="Limits">Usage limits for current tier</param>
/// <param name="PercentageUsed">Percentage of limits used by type</param>
public record UsageStatistics(
    DateTimeOffset BillingPeriodStart,
    DateTimeOffset BillingPeriodEnd,
    Dictionary<UsageType, int> UsageByType,
    Dictionary<UsageType, int> Limits,
    Dictionary<UsageType, double> PercentageUsed);

/// <summary>
/// Information about upgrading to a higher subscription tier.
/// </summary>
/// <param name="CurrentTier">Current subscription tier</param>
/// <param name="TargetTier">Tier being upgraded to</param>
/// <param name="MonthlyPrice">Monthly price for target tier</param>
/// <param name="Currency">Currency for pricing</param>
/// <param name="NewFeatures">Features that will be unlocked</param>
/// <param name="UsageLimitChanges">Changes to usage limits</param>
/// <param name="UpgradeUrl">URL for completing upgrade</param>
public record UpgradeInformation(
    SubscriptionTier CurrentTier,
    SubscriptionTier TargetTier,
    decimal MonthlyPrice,
    string Currency,
    FeatureFlags NewFeatures,
    Dictionary<UsageType, int> UsageLimitChanges,
    string UpgradeUrl);