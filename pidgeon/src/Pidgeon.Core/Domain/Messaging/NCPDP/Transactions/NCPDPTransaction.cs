// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.NCPDP.Transactions;

/// <summary>
/// Base class for all NCPDP pharmacy transaction messages.
/// Captures NCPDP-specific concepts like transaction types and pharmacy routing.
/// </summary>
public abstract class NCPDPTransaction : HealthcareMessage
{
    /// <summary>
    /// Gets the NCPDP transaction type (e.g., "NEWRX", "REFILL", "CANCEL").
    /// </summary>
    public required NCPDPTransactionType TransactionType { get; set; }

    /// <summary>
    /// Gets all segments in this transaction, keyed by segment ID.
    /// </summary>
    public Dictionary<string, NCPDPSegment> Segments { get; set; } = new();

    /// <summary>
    /// Gets the NCPDP version being used.
    /// </summary>
    public NCPDPVersion NCPDPVersion { get; set; } = NCPDPVersion.Version2017071;

    /// <summary>
    /// Gets the transaction reference number for tracking.
    /// </summary>
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>
    /// Validates NCPDP transaction structure and required segments.
    /// </summary>
    /// <returns>A result indicating whether the NCPDP transaction is valid</returns>
    public override Result<HealthcareMessage> Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.IsSuccess)
            return baseResult;

        if (!Standard.Equals("NCPDP", StringComparison.OrdinalIgnoreCase))
            return Error.Validation($"Standard must be NCPDP, got: {Standard}", nameof(Standard));


        // NCPDP transactions must have UIB (Interchange Header) and UIH (Message Header)
        if (!Segments.ContainsKey("UIB"))
            return Error.Validation("NCPDP transactions must contain UIB (Interchange Header) segment", nameof(Segments));

        if (!Segments.ContainsKey("UIH"))
            return Error.Validation("NCPDP transactions must contain UIH (Message Header) segment", nameof(Segments));

        return Result<HealthcareMessage>.Success(this);
    }

    /// <summary>
    /// Gets the NCPDP display summary including transaction type and key segments.
    /// </summary>
    public override string GetDisplaySummary()
    {
        var segmentCount = Segments.Count;
        var segmentSummary = segmentCount == 1 ? "1 segment" : $"{segmentCount} segments";
        var refNumber = !string.IsNullOrWhiteSpace(TransactionReferenceNumber) ? $" (Ref: {TransactionReferenceNumber})" : "";
        return $"NCPDP {TransactionType} transaction {MessageControlId}{refNumber} with {segmentSummary} from {SendingSystem} to {ReceivingSystem}";
    }

    /// <summary>
    /// Gets a segment by its ID.
    /// </summary>
    /// <typeparam name="T">The expected segment type</typeparam>
    /// <param name="segmentId">The segment ID (e.g., "PVD", "DRU")</param>
    /// <returns>The segment if found and of correct type, null otherwise</returns>
    public T? GetSegment<T>(string segmentId) where T : NCPDPSegment
    {
        return Segments.TryGetValue(segmentId, out var segment) ? segment as T : null;
    }

    /// <summary>
    /// Gets all segments of a specific type.
    /// </summary>
    /// <typeparam name="T">The expected segment type</typeparam>
    /// <param name="segmentId">The base segment ID</param>
    /// <returns>All segments matching the type</returns>
    public IEnumerable<T> GetSegments<T>(string segmentId) where T : NCPDPSegment
    {
        return Segments
            .Where(kvp => kvp.Key.StartsWith(segmentId))
            .Select(kvp => kvp.Value)
            .OfType<T>();
    }
}

/// <summary>
/// Represents NCPDP transaction types.
/// </summary>
public enum NCPDPTransactionType
{
    /// <summary>
    /// New prescription transaction.
    /// </summary>
    NewRx,

    /// <summary>
    /// Refill request transaction.
    /// </summary>
    RefillRequest,

    /// <summary>
    /// Refill response transaction.
    /// </summary>
    RefillResponse,

    /// <summary>
    /// Prescription change request.
    /// </summary>
    ChangeRequest,

    /// <summary>
    /// Prescription change response.
    /// </summary>
    ChangeResponse,

    /// <summary>
    /// Cancel prescription request.
    /// </summary>
    CancelRx,

    /// <summary>
    /// Cancel prescription response.
    /// </summary>
    CancelResponse,

    /// <summary>
    /// Medication history request.
    /// </summary>
    MedHistoryRequest,

    /// <summary>
    /// Medication history response.
    /// </summary>
    MedHistoryResponse,

    /// <summary>
    /// Status notification.
    /// </summary>
    Status,

    /// <summary>
    /// Error notification.
    /// </summary>
    Error,

    /// <summary>
    /// Verify prescription.
    /// </summary>
    Verify
}

/// <summary>
/// Represents NCPDP SCRIPT standard versions.
/// </summary>
public enum NCPDPVersion
{
    /// <summary>
    /// NCPDP SCRIPT Version 2017071 (current production standard).
    /// </summary>
    Version2017071,

    /// <summary>
    /// NCPDP SCRIPT Version 2023 (upcoming mandated upgrade).
    /// </summary>
    Version2023,

    /// <summary>
    /// NCPDP SCRIPT Version 10.6 (legacy).
    /// </summary>
    Version10_6
}

/// <summary>
/// Base class for all NCPDP segments.
/// </summary>
public abstract class NCPDPSegment
{
    /// <summary>
    /// Gets the segment ID (e.g., "UIB", "UIH", "PVD", "DRU").
    /// </summary>
    public abstract string SegmentId { get; }

    /// <summary>
    /// Gets the segment sequence number (for segments that can repeat).
    /// </summary>
    public int SequenceNumber { get; set; } = 1;

    /// <summary>
    /// Gets additional data elements specific to this segment.
    /// NCPDP uses numbered data elements rather than named fields.
    /// </summary>
    public Dictionary<string, object> DataElements { get; set; } = new();

    /// <summary>
    /// Validates the segment structure and required data elements.
    /// Derived segments should override to add segment-specific validation.
    /// </summary>
    /// <returns>A result indicating whether the segment is valid</returns>
    public virtual Result<NCPDPSegment> Validate()
    {
        if (string.IsNullOrWhiteSpace(SegmentId))
            return Error.Validation("Segment ID is required", nameof(SegmentId));

        if (SequenceNumber < 1)
            return Error.Validation("Sequence number must be positive", nameof(SequenceNumber));

        return Result<NCPDPSegment>.Success(this);
    }

    /// <summary>
    /// Gets a data element value by its identifier.
    /// </summary>
    /// <typeparam name="T">The expected data type</typeparam>
    /// <param name="elementId">The data element identifier</param>
    /// <returns>The data element value, or default if not found</returns>
    public T? GetDataElement<T>(string elementId)
    {
        return DataElements.TryGetValue(elementId, out var value) ? (T?)value : default;
    }

    /// <summary>
    /// Sets a data element value.
    /// </summary>
    /// <param name="elementId">The data element identifier</param>
    /// <param name="value">The value to set</param>
    public void SetDataElement(string elementId, object value)
    {
        DataElements[elementId] = value;
    }
}

/// <summary>
/// Common NCPDP data element identifiers for reference.
/// </summary>
public static class NCPDPDataElements
{
    // UIB - Interchange Header
    public const string InterchangeSender = "010";
    public const string InterchangeReceiver = "020";
    public const string InterchangeDate = "030";
    public const string InterchangeTime = "040";
    public const string InterchangeControlNumber = "050";

    // UIH - Message Header  
    public const string MessageFunction = "010";
    public const string MessageControlID = "020";
    public const string MessageDate = "030";
    public const string MessageTime = "040";

    // PVD - Provider Segment
    public const string ProviderID = "010";
    public const string ProviderName = "020";
    public const string DEANumber = "030";
    public const string NPINumber = "040";

    // PTT - Patient Segment
    public const string PatientID = "010";
    public const string PatientName = "020";
    public const string PatientDateOfBirth = "030";
    public const string PatientGender = "040";
    public const string PatientAddress = "050";

    // DRU - Drug Segment
    public const string ProductID = "010";
    public const string ProductIDQualifier = "020";
    public const string DrugDescription = "030";
    public const string Quantity = "040";
    public const string DaysSupply = "050";
    public const string Substitutions = "060";

    // SIG - Directions Segment
    public const string Directions = "010";
    public const string DirectionsCode = "020";
    public const string FreeFormMessage = "030";
}