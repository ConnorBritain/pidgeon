using Pidgeon.Core.Domain.Configuration;
using Pidgeon.Core.Infrastructure.Interop;

namespace Pidgeon.Core.Application.Services.Database;

/// <summary>
/// Comprehensive database service providing unified access to all Pidgeon data sources:
/// - HL7 v2.3 structure data (segments, fields, data types, code tables)
/// - Semantic path mappings (HL7-FHIR interoperability)
/// - Session management and lock functionality
/// - Generated message storage and audit
/// - Demographic datasets and value-add data
/// - Vendor pattern detection and configuration profiles
/// - User workspace and persistence
/// </summary>
public interface IHL7DatabaseService
{
    // =======================================================================
    // PHASE 1-6: HL7 STRUCTURE DATA QUERIES
    // =======================================================================

    /// <summary>
    /// Get complete segment definition by code (e.g., 'PID', 'MSH', 'OBR')
    /// </summary>
    Task<SegmentDefinition?> GetSegmentAsync(string segmentCode, string standard = "HL7v2.3");

    /// <summary>
    /// Get all field definitions for a specific segment
    /// </summary>
    Task<IList<FieldDefinition>> GetFieldsAsync(string segmentCode, string standard = "HL7v2.3");

    /// <summary>
    /// Get specific field definition (e.g., PID.3, MSH.9)
    /// </summary>
    Task<FieldDefinition?> GetFieldAsync(string segmentCode, int position, string standard = "HL7v2.3");

    /// <summary>
    /// Get data type definition by code (e.g., 'CX', 'XPN', 'TS')
    /// </summary>
    Task<DataTypeDefinition?> GetDataTypeAsync(string dataTypeCode, string standard = "HL7v2.3");

    /// <summary>
    /// Get code table definition and values by table number (e.g., '0001', '0002')
    /// </summary>
    Task<CodeTableDefinition?> GetCodeTableAsync(string tableNumber, string standard = "HL7v2.3");

    /// <summary>
    /// Get valid values for a specific code table
    /// </summary>
    Task<IList<CodeValue>> GetTableValuesAsync(string tableNumber, string standard = "HL7v2.3");

    /// <summary>
    /// Get trigger event definition (e.g., 'ADT_A01', 'ORU_R01')
    /// </summary>
    Task<TriggerEventDefinition?> GetTriggerEventAsync(string eventCode, string standard = "HL7v2.3");

    /// <summary>
    /// Search across all HL7 elements by keyword
    /// </summary>
    Task<HL7SearchResults> SearchAsync(string keyword, HL7SearchScope scope = HL7SearchScope.All);

    // =======================================================================
    // PHASE 7: SEMANTIC PATH INTEGRATION
    // =======================================================================

    /// <summary>
    /// Get essential semantic paths for progressive disclosure UI
    /// Performance target: <1ms
    /// </summary>
    Task<IList<SemanticPath>> GetEssentialPathsAsync();

    /// <summary>
    /// Get all semantic paths (essential + advanced)
    /// Performance target: <5ms
    /// </summary>
    Task<IList<SemanticPath>> GetAllSemanticPathsAsync();

    /// <summary>
    /// Resolve semantic path to HL7 and FHIR field mappings
    /// Performance target: <10ms for cross-standard resolution
    /// </summary>
    Task<PathResolution?> ResolveSemanticPathAsync(string semanticPath);

    /// <summary>
    /// Search semantic paths by keyword with fuzzy matching
    /// Performance target: <100ms
    /// </summary>
    Task<IList<SemanticPath>> SearchSemanticPathsAsync(string keyword);

    /// <summary>
    /// Translate between standards (HL7 ↔ FHIR)
    /// </summary>
    Task<CrossStandardMapping?> TranslateFieldAsync(string fieldPath, string fromStandard, string toStandard);

    // =======================================================================
    // SESSION MANAGEMENT & LOCK FUNCTIONALITY
    // =======================================================================

    /// <summary>
    /// Create a new session for maintaining patient/context consistency
    /// </summary>
    Task<string> CreateSessionAsync(string? sessionName = null, bool isTemporary = true, TimeSpan? ttl = null);

    /// <summary>
    /// Get current active session for user
    /// </summary>
    Task<string?> GetCurrentSessionAsync();

    /// <summary>
    /// Set current active session
    /// </summary>
    Task SetCurrentSessionAsync(string sessionName);

    /// <summary>
    /// Get session details and locked field values
    /// </summary>
    Task<SessionData?> GetSessionAsync(string sessionName);

    /// <summary>
    /// List all user sessions (temporary and permanent)
    /// </summary>
    Task<IList<SessionSummary>> ListSessionsAsync();

    /// <summary>
    /// Set field value in session (lock functionality)
    /// </summary>
    Task<Result> SetSessionValueAsync(string sessionName, string fieldPath, string value, string? reason = null);

    /// <summary>
    /// Save temporary session as permanent
    /// </summary>
    Task<Result> SaveSessionAsync(string currentName, string newName);

    /// <summary>
    /// Delete session and all associated data
    /// </summary>
    Task<Result> DeleteSessionAsync(string sessionName);

    /// <summary>
    /// Clean up expired temporary sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();

    /// <summary>
    /// Import session from template file (YAML/JSON)
    /// </summary>
    Task<Result<string>> ImportSessionTemplateAsync(string filePath);

    /// <summary>
    /// Export session to template file
    /// </summary>
    Task<Result> ExportSessionTemplateAsync(string sessionName, string filePath);

    // =======================================================================
    // MESSAGE GENERATION & AUDIT
    // =======================================================================

    /// <summary>
    /// Store generated message for audit and recall
    /// </summary>
    Task<string> StoreGeneratedMessageAsync(GeneratedMessage message);

    /// <summary>
    /// Get generated message by ID
    /// </summary>
    Task<GeneratedMessage?> GetGeneratedMessageAsync(string messageId);

    /// <summary>
    /// Get generation history for session or user
    /// </summary>
    Task<IList<GeneratedMessage>> GetGenerationHistoryAsync(string? sessionName = null, DateTime? since = null, int limit = 100);

    /// <summary>
    /// Store batch generation results
    /// </summary>
    Task<string> StoreBatchGenerationAsync(BatchGenerationResult batch);

    /// <summary>
    /// Get generation statistics and performance metrics
    /// </summary>
    Task<GenerationMetrics> GetGenerationMetricsAsync(DateTime? since = null);

    // =======================================================================
    // VALIDATION & ERROR TRACKING
    // =======================================================================

    /// <summary>
    /// Store validation results for audit and pattern analysis
    /// </summary>
    Task StoreValidationResultAsync(ValidationResult validationResult);

    /// <summary>
    /// Get validation history for message or field
    /// </summary>
    Task<IList<ValidationResult>> GetValidationHistoryAsync(string? messageId = null, string? fieldPath = null);

    /// <summary>
    /// Get common validation errors for learning and improvement
    /// </summary>
    Task<IList<ValidationErrorPattern>> GetCommonValidationErrorsAsync(string? eventType = null);

    /// <summary>
    /// Get validation performance metrics
    /// </summary>
    Task<ValidationMetrics> GetValidationMetricsAsync(DateTime? since = null);

    // =======================================================================
    // DEMOGRAPHIC & VALUE-ADD DATASETS
    // =======================================================================

    /// <summary>
    /// Get available demographic datasets (FirstName, LastName, ZipCode, etc.)
    /// </summary>
    Task<IList<DatasetInfo>> GetAvailableDatasetsAsync(string? category = null, DatasetTier? tier = null);

    /// <summary>
    /// Get random values from demographic dataset
    /// </summary>
    Task<IList<string>> GetDatasetValuesAsync(string datasetName, int count = 1, DatasetConstraints? constraints = null);

    /// <summary>
    /// Get weighted/realistic values based on frequency distribution
    /// </summary>
    Task<IList<string>> GetRealisticValuesAsync(string datasetName, int count = 1, RealisticConstraints? constraints = null);

    /// <summary>
    /// Add custom dataset values (Enterprise tier)
    /// </summary>
    Task<Result> AddCustomDatasetAsync(string datasetName, IList<DatasetValue> values, DatasetTier tier = DatasetTier.Enterprise);

    // =======================================================================
    // VENDOR PATTERN DETECTION
    // =======================================================================

    /// <summary>
    /// Learn field usage patterns from analyzed messages
    /// </summary>
    Task StoreVendorPatternAsync(VendorPattern pattern);

    /// <summary>
    /// Get vendor configuration profile (Epic, Cerner, AllScripts)
    /// </summary>
    Task<VendorProfile?> GetVendorProfileAsync(string vendorName, string? systemName = null);

    /// <summary>
    /// Get field patterns for specific vendor and event type
    /// </summary>
    Task<IList<FieldPattern>> GetFieldPatternsAsync(string vendorName, string eventType);

    /// <summary>
    /// Analyze messages to detect vendor patterns
    /// </summary>
    Task<VendorAnalysisResult> AnalyzeVendorPatternsAsync(IList<string> messages, string? knownVendor = null);

    /// <summary>
    /// Get pattern confidence scores for field usage
    /// </summary>
    Task<PatternConfidence> GetPatternConfidenceAsync(string vendorName, string fieldPath);

    // =======================================================================
    // USER WORKSPACE & PERSISTENCE
    // =======================================================================

    /// <summary>
    /// Get user preferences and settings
    /// </summary>
    Task<UserSettings> GetUserSettingsAsync();

    /// <summary>
    /// Update user preferences
    /// </summary>
    Task SetUserSettingAsync(string key, string value);

    /// <summary>
    /// Get user's template library
    /// </summary>
    Task<IList<UserTemplate>> GetUserTemplatesAsync();

    /// <summary>
    /// Save user template for reuse
    /// </summary>
    Task<Result> SaveUserTemplateAsync(UserTemplate template);

    /// <summary>
    /// Get usage analytics for feature adoption
    /// </summary>
    Task<UsageAnalytics> GetUsageAnalyticsAsync(DateTime? since = null);

    /// <summary>
    /// Track command usage for analytics
    /// </summary>
    Task TrackCommandUsageAsync(string command, string? subcommand = null, Dictionary<string, object>? parameters = null);

    // =======================================================================
    // DATABASE MANAGEMENT
    // =======================================================================

    /// <summary>
    /// Initialize database with schema and seed data
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Get database health and performance status
    /// </summary>
    Task<DatabaseHealth> GetDatabaseHealthAsync();

    /// <summary>
    /// Optimize database performance (indexes, cleanup)
    /// </summary>
    Task OptimizeDatabaseAsync();

    /// <summary>
    /// Backup user data for portability
    /// </summary>
    Task<Result> BackupUserDataAsync(string backupPath);

    /// <summary>
    /// Restore user data from backup
    /// </summary>
    Task<Result> RestoreUserDataAsync(string backupPath);
}

// =======================================================================
// SUPPORTING TYPES AND ENUMS
// =======================================================================

public enum HL7SearchScope
{
    All,
    Segments,
    Fields,
    DataTypes,
    Tables,
    TriggerEvents
}

public enum DatasetTier
{
    Free,
    Professional,
    Enterprise
}

public class HL7SearchResults
{
    public IList<SegmentDefinition> Segments { get; set; } = new List<SegmentDefinition>();
    public IList<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
    public IList<DataTypeDefinition> DataTypes { get; set; } = new List<DataTypeDefinition>();
    public IList<CodeTableDefinition> Tables { get; set; } = new List<CodeTableDefinition>();
    public IList<TriggerEventDefinition> TriggerEvents { get; set; } = new List<TriggerEventDefinition>();
}

public class SemanticPath
{
    public string Path { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty; // 'essential' or 'advanced'
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HL7Field { get; set; }
    public string? FHIRPath { get; set; }
}

public class PathResolution
{
    public string SemanticPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HL7Field { get; set; }
    public string? FHIRPath { get; set; }
    public FieldDefinition? HL7FieldDefinition { get; set; }
    public IList<ConditionalMapping> ConditionalMappings { get; set; } = new List<ConditionalMapping>();
}

public class CrossStandardMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string SourceStandard { get; set; } = string.Empty;
    public string TargetStandard { get; set; } = string.Empty;
    public IList<MappingCondition> Conditions { get; set; } = new List<MappingCondition>();
}

public class SessionData
{
    public string SessionName { get; set; } = string.Empty;
    public bool IsTemporary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, SessionValue> LockedValues { get; set; } = new();
}

public class SessionSummary
{
    public string SessionName { get; set; } = string.Empty;
    public bool IsTemporary { get; set; }
    public int FieldCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public TimeSpan? TimeRemaining => ExpiresAt - DateTime.UtcNow;
}

public class SessionValue
{
    public string Value { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime SetAt { get; set; }
}

public class GeneratedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SessionName { get; set; }
    public Dictionary<string, object> GenerationParameters { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public TimeSpan GenerationTime { get; set; }
}

public class BatchGenerationResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public IList<GeneratedMessage> Messages { get; set; } = new List<GeneratedMessage>();
    public Dictionary<string, object> BatchParameters { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public TimeSpan TotalTime { get; set; }
}

public class GenerationMetrics
{
    public int TotalMessages { get; set; }
    public int TotalBatches { get; set; }
    public double AverageGenerationTime { get; set; }
    public Dictionary<string, int> MessageTypeBreakdown { get; set; } = new();
    public int SuccessRate { get; set; }
}

public class ValidationResult
{
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public IList<ValidationError> Errors { get; set; } = new List<ValidationError>();
    public DateTime ValidatedAt { get; set; }
    public TimeSpan ValidationTime { get; set; }
}

public class ValidationError
{
    public string FieldPath { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ExpectedValue { get; set; }
    public string? ActualValue { get; set; }
}

public class ValidationErrorPattern
{
    public string FieldPath { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public string CommonMessage { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class ValidationMetrics
{
    public int TotalValidations { get; set; }
    public int SuccessfulValidations { get; set; }
    public double AverageValidationTime { get; set; }
    public IList<ValidationErrorPattern> TopErrors { get; set; } = new List<ValidationErrorPattern>();
}

public class DatasetInfo
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DatasetTier Tier { get; set; }
    public int ValueCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class DatasetValue
{
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public double Frequency { get; set; } = 1.0;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DatasetConstraints
{
    public string? Gender { get; set; }
    public string? Ethnicity { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public string? Region { get; set; }
}

public class RealisticConstraints : DatasetConstraints
{
    public double? FrequencyThreshold { get; set; }
    public bool PreferCommon { get; set; } = true;
}

public class VendorPattern
{
    public string VendorName { get; set; } = string.Empty;
    public string? SystemName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string PatternType { get; set; } = string.Empty; // 'always_populated', 'never_used', 'format'
    public object PatternValue { get; set; } = new();
    public double Confidence { get; set; }
    public int SampleCount { get; set; }
}

public class VendorProfile
{
    public string VendorName { get; set; } = string.Empty;
    public string? SystemName { get; set; }
    public IList<FieldPattern> FieldPatterns { get; set; } = new List<FieldPattern>();
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class FieldPattern
{
    public string FieldPath { get; set; } = string.Empty;
    public double PopulationRate { get; set; }
    public string? CommonFormat { get; set; }
    public IList<string> CommonValues { get; set; } = new List<string>();
    public bool IsRequired { get; set; }
}

public class VendorAnalysisResult
{
    public string? DetectedVendor { get; set; }
    public double Confidence { get; set; }
    public IList<VendorPattern> DetectedPatterns { get; set; } = new List<VendorPattern>();
    public Dictionary<string, double> VendorScores { get; set; } = new();
}

public class PatternConfidence
{
    public string FieldPath { get; set; } = string.Empty;
    public double PopulationConfidence { get; set; }
    public double FormatConfidence { get; set; }
    public int SampleSize { get; set; }
}

public class UserSettings
{
    public Dictionary<string, string> Settings { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class UserTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty; // 'session', 'generation', 'validation'
    public Dictionary<string, object> Content { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int UsageCount { get; set; }
}

public class UsageAnalytics
{
    public Dictionary<string, int> CommandFrequency { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public int TotalCommands { get; set; }
    public DateTime FirstUse { get; set; }
    public DateTime LastUse { get; set; }
}

public class DatabaseHealth
{
    public bool IsHealthy { get; set; }
    public long DatabaseSize { get; set; }
    public int TableCount { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public TimeSpan LastOptimization { get; set; }
    public IList<string> Issues { get; set; } = new List<string>();
}

public class ConditionalMapping
{
    public string Condition { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string? TargetValue { get; set; }
}

public class MappingCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}