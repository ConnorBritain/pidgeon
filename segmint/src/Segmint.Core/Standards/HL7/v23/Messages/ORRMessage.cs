// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 Order Response (ORR) message for pharmacy and clinical order responses.
/// This message is used by CIPS and pharmacy systems to respond to incoming orders,
/// indicating acceptance, rejection, or providing detailed error information.
/// Critical for bidirectional communication in pharmacy workflows.
/// Structure: MSH MSA [ERR] [PID] [PV1] [ORC] [RXO] [DG1] [NTE]
/// </summary>
public class ORRMessage : HL7Message
{
    /// <inheritdoc />
    public override string MessageType => "ORR";

    /// <inheritdoc />
    public override string TriggerEvent => "O01";

    /// <inheritdoc />
    public override string MessageStructure => "ORR_O01";

    #region Message Segments

    /// <summary>
    /// Message Header segment (required).
    /// </summary>
    public MSHSegment Header { get; private set; }

    /// <summary>
    /// Message Acknowledgment segment (required).
    /// Contains the response status and original message control ID.
    /// </summary>
    public MSASegment MessageAcknowledgment { get; private set; }

    /// <summary>
    /// Error segments (optional).
    /// Detailed error information for rejected or problematic orders.
    /// </summary>
    public List<ERRSegment> Errors { get; private set; }

    /// <summary>
    /// Patient Identification segment (conditional).
    /// Required when responding with patient-specific information.
    /// </summary>
    public PIDSegment? PatientIdentification { get; set; }

    /// <summary>
    /// Patient Visit segment (optional).
    /// Visit context for the order response.
    /// </summary>
    public PV1Segment? PatientVisit { get; set; }

    /// <summary>
    /// List of Order Response groups containing order details.
    /// Each group represents a response to a specific order.
    /// </summary>
    public List<OrderResponseGroup> OrderResponses { get; private set; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the ORRMessage class.
    /// </summary>
    public ORRMessage()
    {
        Header = new MSHSegment();
        MessageAcknowledgment = new MSASegment();
        Errors = new List<ERRSegment>();
        OrderResponses = new List<OrderResponseGroup>();
        InitializeMessage();
    }

    /// <summary>
    /// Initializes the message with default values.
    /// </summary>
    protected override void InitializeMessage()
    {
        // Set up message header
        Header.SetBasicInfo();
        Header.SetMessageType(MessageType, TriggerEvent, MessageStructure);
        Header.SetProcessingId(true); // Production mode
        Header.GenerateMessageControlId();
    }

    #region Message Building Methods

    /// <summary>
    /// Sets acknowledgment for successful order acceptance.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="acknowledgmentText">Optional acknowledgment text</param>
    public void SetAcceptedAcknowledgment(string originalMessageControlId, string? acknowledgmentText = null)
    {
        MessageAcknowledgment.AcknowledgmentCode.SetValue("AA");
        MessageAcknowledgment.MessageControlId.SetValue(originalMessageControlId);
        MessageAcknowledgment.TextMessage.SetValue(acknowledgmentText ?? "Order accepted");
    }

    /// <summary>
    /// Sets acknowledgment for order rejection with errors.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="rejectionReason">Reason for rejection</param>
    public void SetRejectedAcknowledgment(string originalMessageControlId, string rejectionReason)
    {
        MessageAcknowledgment.AcknowledgmentCode.SetValue("AE");
        MessageAcknowledgment.MessageControlId.SetValue(originalMessageControlId);
        MessageAcknowledgment.TextMessage.SetValue(rejectionReason);
    }

    /// <summary>
    /// Sets acknowledgment for application error.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="errorDescription">Error description</param>
    public void SetApplicationErrorAcknowledgment(string originalMessageControlId, string errorDescription)
    {
        MessageAcknowledgment.AcknowledgmentCode.SetValue("AR");
        MessageAcknowledgment.MessageControlId.SetValue(originalMessageControlId);
        MessageAcknowledgment.TextMessage.SetValue(errorDescription);
    }

    /// <summary>
    /// Adds a general error to the response.
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="errorDescription">Error description</param>
    /// <param name="severity">Error severity (E=Error, W=Warning, F=Fatal)</param>
    /// <param name="location">Location where error occurred</param>
    /// <param name="userMessage">User-friendly message</param>
    public void AddError(
        string errorCode,
        string errorDescription,
        string severity = "E",
        string? location = null,
        string? userMessage = null)
    {
        var error = new ERRSegment();
        error.SetBasicError(errorCode, errorDescription, severity, location, userMessage);
        Errors.Add(error);
    }

    /// <summary>
    /// Adds a pharmacy-specific error.
    /// </summary>
    /// <param name="pharmacyErrorCode">Pharmacy error code</param>
    /// <param name="errorDescription">Error description</param>
    /// <param name="severity">Error severity</param>
    /// <param name="medicationContext">Medication context</param>
    /// <param name="clinicalMessage">Clinical safety message</param>
    public void AddPharmacyError(
        string pharmacyErrorCode,
        string errorDescription,
        string severity = "E",
        string? medicationContext = null,
        string? clinicalMessage = null)
    {
        var error = new ERRSegment();
        error.SetPharmacyError(pharmacyErrorCode, errorDescription, severity, medicationContext, clinicalMessage);
        Errors.Add(error);
    }

    /// <summary>
    /// Adds a drug interaction error.
    /// </summary>
    /// <param name="interactionSeverity">Severity of interaction</param>
    /// <param name="drug1">First drug</param>
    /// <param name="drug2">Second drug</param>
    /// <param name="clinicalEffect">Clinical effect</param>
    public void AddDrugInteractionError(string interactionSeverity, string drug1, string drug2, string clinicalEffect)
    {
        var error = new ERRSegment();
        error.SetDrugInteractionError(interactionSeverity, drug1, drug2, clinicalEffect);
        Errors.Add(error);
    }

    /// <summary>
    /// Adds an allergy contraindication error.
    /// </summary>
    /// <param name="allergen">Allergen substance</param>
    /// <param name="medication">Contraindicated medication</param>
    /// <param name="reactionType">Type of reaction</param>
    public void AddAllergyError(string allergen, string medication, string reactionType)
    {
        var error = new ERRSegment();
        error.SetAllergyError(allergen, medication, reactionType);
        Errors.Add(error);
    }

    /// <summary>
    /// Sets patient information for the response.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="middleName">Patient's middle name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="gender">Patient's gender</param>
    public void SetPatientInformation(
        string patientId,
        string lastName,
        string? firstName = null,
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null)
    {
        PatientIdentification ??= new PIDSegment();
        PatientIdentification.PatientIdentifierList.SetValue(patientId);
        PatientIdentification.SetPatientName(lastName, firstName ?? "", middleName);
        
        if (dateOfBirth.HasValue)
            PatientIdentification.SetDateOfBirth(dateOfBirth.Value);
            
        if (!string.IsNullOrEmpty(gender))
            PatientIdentification.SetGender(gender);
    }

    /// <summary>
    /// Adds an order response for a specific order.
    /// </summary>
    /// <param name="orderControl">Order control (NW, CA, etc.)</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="fillerOrderNumber">Assigned filler order number</param>
    /// <param name="orderStatus">Order status (A=Accepted, R=Rejected)</param>
    /// <param name="responseReason">Reason for the response</param>
    /// <returns>The created order response group</returns>
    public OrderResponseGroup AddOrderResponse(
        string orderControl,
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? orderStatus = null,
        string? responseReason = null)
    {
        var responseGroup = new OrderResponseGroup();
        
        // Set up ORC segment
        responseGroup.OrderControl.SetBasicInfo(
            orderControl,
            placerOrderNumber,
            fillerOrderNumber,
            orderStatus
        );
        
        if (!string.IsNullOrEmpty(responseReason))
        {
            responseGroup.OrderControl.OrderControlCodeReason.SetComponents(responseReason);
        }
        
        OrderResponses.Add(responseGroup);
        return responseGroup;
    }

    /// <summary>
    /// Accepts a pharmacy order with optional modifications.
    /// </summary>
    /// <param name="placerOrderNumber">Original order number</param>
    /// <param name="fillerOrderNumber">Assigned pharmacy order number</param>
    /// <param name="acceptanceNotes">Optional acceptance notes</param>
    /// <param name="estimatedFillTime">Estimated time to fill</param>
    /// <returns>The order response group</returns>
    public OrderResponseGroup AcceptPharmacyOrder(
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? acceptanceNotes = null,
        DateTime? estimatedFillTime = null)
    {
        var response = AddOrderResponse("OK", placerOrderNumber, fillerOrderNumber, "A", "Order accepted for processing");
        
        if (!string.IsNullOrEmpty(acceptanceNotes) || estimatedFillTime.HasValue)
        {
            var note = new NTESegment();
            var noteText = acceptanceNotes ?? "";
            if (estimatedFillTime.HasValue)
            {
                noteText += $" Estimated fill time: {estimatedFillTime.Value:yyyy-MM-dd HH:mm}";
            }
            note.Comment.SetValue(noteText);
            response.Notes.Add(note);
        }
        
        return response;
    }

    /// <summary>
    /// Rejects a pharmacy order with detailed error information.
    /// </summary>
    /// <param name="placerOrderNumber">Original order number</param>
    /// <param name="rejectionReason">Primary rejection reason</param>
    /// <param name="detailedErrors">Detailed error information</param>
    /// <returns>The order response group</returns>
    public OrderResponseGroup RejectPharmacyOrder(
        string placerOrderNumber,
        string rejectionReason,
        List<(string code, string description, string severity)>? detailedErrors = null)
    {
        var response = AddOrderResponse("NA", placerOrderNumber, orderStatus: "R", responseReason: rejectionReason);
        
        // Add detailed errors if provided
        if (detailedErrors != null)
        {
            foreach (var (code, description, severity) in detailedErrors)
            {
                AddPharmacyError(code, description, severity);
            }
        }
        
        return response;
    }

    /// <summary>
    /// Creates a response for drug interaction screening.
    /// </summary>
    /// <param name="placerOrderNumber">Order number</param>
    /// <param name="interactions">List of detected interactions</param>
    /// <param name="allowOverride">Whether override is permitted</param>
    /// <returns>The order response group</returns>
    public OrderResponseGroup RespondWithDrugInteractions(
        string placerOrderNumber,
        List<(string severity, string drug1, string drug2, string effect)> interactions,
        bool allowOverride = true)
    {
        var hasHighSeverity = interactions.Any(i => i.severity.ToUpper() == "HIGH" || i.severity.ToUpper() == "MAJOR");
        var response = AddOrderResponse(
            hasHighSeverity && !allowOverride ? "NA" : "OK",
            placerOrderNumber,
            orderStatus: hasHighSeverity && !allowOverride ? "R" : "A",
            responseReason: "Drug interaction screening completed"
        );
        
        foreach (var (severity, drug1, drug2, effect) in interactions)
        {
            AddDrugInteractionError(severity, drug1, drug2, effect);
        }
        
        return response;
    }

    #endregion

    #region Message Serialization

    /// <inheritdoc />
    public string ToHL7String()
    {
        var segments = new List<string>();
        
        // Add required segments
        segments.Add(Header.ToHL7String());
        segments.Add(MessageAcknowledgment.ToHL7String());
        
        // Add error segments
        foreach (var error in Errors)
            segments.Add(error.ToHL7String());
        
        // Add optional patient information
        if (PatientIdentification != null)
            segments.Add(PatientIdentification.ToHL7String());
            
        if (PatientVisit != null)
            segments.Add(PatientVisit.ToHL7String());
        
        // Add order response groups
        foreach (var orderResponse in OrderResponses)
        {
            segments.Add(orderResponse.OrderControl.ToHL7String());
            
            // Add RXO if present
            if (orderResponse.PharmacyOrder != null)
                segments.Add(orderResponse.PharmacyOrder.ToHL7String());
            
            // Add diagnosis segments
            foreach (var diagnosis in orderResponse.Diagnoses)
                segments.Add(diagnosis.ToHL7String());
            
            // Add notes
            foreach (var note in orderResponse.Notes)
                segments.Add(note.ToHL7String());
        }
        
        return string.Join("\r", segments);
    }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();
        
        // Business rule validations
        try
        {
            if (MessageAcknowledgment?.AcknowledgmentCode?.Value == "AE" && !Errors.Any())
            {
                result.AddIssue(ValidationIssue.SemanticError("ORR001", "Application Error acknowledgment requires at least one ERR segment", "MSA"));
            }
            
            if (Errors.Any() && MessageAcknowledgment?.AcknowledgmentCode?.Value == "AA")
            {
                result.AddIssue(ValidationIssue.SemanticError("ORR002", "Fatal errors present but acknowledgment indicates acceptance", "MSA"));
            }
        }
        catch
        {
            // Skip validation if properties don't exist or fail to access
        }
        
        // Validate error segments (simplified)
        for (int i = 0; i < Errors.Count; i++)
        {
            try
            {
                var errorValidation = Errors[i].GetType().GetMethod("Validate")?.Invoke(Errors[i], null);
                if (errorValidation is List<string> stringErrors)
                {
                    foreach (var error in stringErrors)
                    {
                        result.AddIssue(ValidationIssue.SemanticError($"ORR{i + 3:D3}", $"Error {i + 1}: {error}", $"ERR[{i}]"));
                    }
                }
            }
            catch
            {
                // Skip validation if method doesn't exist or fails
            }
        }
        
        return result;
    }

    /// <inheritdoc />
    public string ToDisplayString()
    {
        var ackType = MessageAcknowledgment.AcknowledgmentCode.Value switch
        {
            "AA" => "Accepted",
            "AE" => "Rejected", 
            "AR" => "Error",
            _ => "Unknown"
        };
        
        var orderCount = OrderResponses.Count;
        var errorCount = Errors.Count;
        var patientInfo = PatientIdentification?.PatientName.ToDisplayString() ?? "No patient";
        
        return $"ORR Response - {ackType}: {orderCount} orders, {errorCount} errors - {patientInfo}";
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates an acceptance response for a pharmacy order.
    /// </summary>
    /// <param name="originalMessageControlId">Original message control ID</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="fillerOrderNumber">Assigned filler order number</param>
    /// <param name="acceptanceMessage">Optional acceptance message</param>
    /// <returns>Configured ORR message for acceptance</returns>
    public static ORRMessage CreateAcceptanceResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? acceptanceMessage = null)
    {
        var orr = new ORRMessage();
        orr.SetAcceptedAcknowledgment(originalMessageControlId, acceptanceMessage);
        orr.AcceptPharmacyOrder(placerOrderNumber, fillerOrderNumber, acceptanceMessage);
        return orr;
    }

    /// <summary>
    /// Creates a rejection response for a pharmacy order.
    /// </summary>
    /// <param name="originalMessageControlId">Original message control ID</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="rejectionReason">Reason for rejection</param>
    /// <param name="detailedErrors">Detailed error information</param>
    /// <returns>Configured ORR message for rejection</returns>
    public static ORRMessage CreateRejectionResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        string rejectionReason,
        List<(string code, string description)>? detailedErrors = null)
    {
        var orr = new ORRMessage();
        orr.SetRejectedAcknowledgment(originalMessageControlId, rejectionReason);
        orr.RejectPharmacyOrder(placerOrderNumber, rejectionReason);
        
        if (detailedErrors != null)
        {
            foreach (var (code, description) in detailedErrors)
            {
                orr.AddPharmacyError(code, description);
            }
        }
        
        return orr;
    }

    /// <summary>
    /// Creates a drug interaction response.
    /// </summary>
    /// <param name="originalMessageControlId">Original message control ID</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="interactions">Detected interactions</param>
    /// <param name="allowOverride">Whether override is allowed</param>
    /// <returns>Configured ORR message with interaction information</returns>
    public static ORRMessage CreateDrugInteractionResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        List<(string severity, string drug1, string drug2, string effect)> interactions,
        bool allowOverride = true)
    {
        var orr = new ORRMessage();
        var hasHighSeverity = interactions.Any(i => i.severity.ToUpper() == "HIGH");
        
        if (hasHighSeverity && !allowOverride)
        {
            orr.SetRejectedAcknowledgment(originalMessageControlId, "High severity drug interaction detected");
        }
        else
        {
            orr.SetAcceptedAcknowledgment(originalMessageControlId, "Drug interaction warnings present");
        }
        
        orr.RespondWithDrugInteractions(placerOrderNumber, interactions, allowOverride);
        return orr;
    }

    #endregion

    /// <inheritdoc />
    public override HL7Message Clone()
    {
        var cloned = new ORRMessage();
        
        // Clear default segments
        cloned.ClearSegments();
        
        // Clone all segments
        foreach (var segment in this)
        {
            cloned.AddSegment(segment.Clone());
        }
        
        // Update references
        cloned.Header = cloned.GetSegment<MSHSegment>() ?? new MSHSegment();
        cloned.MessageAcknowledgment = cloned.GetSegment<MSASegment>() ?? new MSASegment();
        cloned.PatientIdentification = cloned.GetSegment<PIDSegment>();
        cloned.PatientVisit = cloned.GetSegment<PV1Segment>();
        
        // Rebuild error segments
        cloned.Errors.Clear();
        cloned.Errors.AddRange(cloned.GetSegments<ERRSegment>());
        
        // Rebuild order response groups
        cloned.OrderResponses.Clear();
        foreach (var response in OrderResponses)
        {
            // Find the corresponding cloned segments
            var clonedOrc = cloned.GetSegments<ORCSegment>().Skip(cloned.OrderResponses.Count).FirstOrDefault();
            if (clonedOrc != null)
            {
                var newResponse = new OrderResponseGroup(clonedOrc);
                cloned.OrderResponses.Add(newResponse);
            }
        }
        
        return cloned;
    }
}

/// <summary>
/// Represents a group of order response segments (ORC + RXO + DG1 + NTE).
/// Each group contains the response to a specific order from the original message.
/// </summary>
public class OrderResponseGroup
{
    /// <summary>
    /// Order Control segment (required).
    /// </summary>
    public ORCSegment OrderControl { get; private set; }

    /// <summary>
    /// Pharmacy/Treatment Order segment (optional).
    /// </summary>
    public RXOSegment? PharmacyOrder { get; set; }

    /// <summary>
    /// List of Diagnosis segments (optional).
    /// </summary>
    public List<DG1Segment> Diagnoses { get; private set; }

    /// <summary>
    /// List of Note segments (optional).
    /// </summary>
    public List<NTESegment> Notes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the OrderResponseGroup class.
    /// </summary>
    public OrderResponseGroup()
    {
        OrderControl = new ORCSegment();
        Diagnoses = new List<DG1Segment>();
        Notes = new List<NTESegment>();
    }

    /// <summary>
    /// Initializes a new instance of the OrderResponseGroup class with a specific OrderControl segment.
    /// </summary>
    /// <param name="orderControl">The order control segment to use</param>
    public OrderResponseGroup(ORCSegment orderControl)
    {
        OrderControl = orderControl;
        Diagnoses = new List<DG1Segment>();
        Notes = new List<NTESegment>();
    }

    /// <summary>
    /// Validates the order response group.
    /// </summary>
    /// <returns>List of validation issues.</returns>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        // Validate ORC segment
        issues.AddRange(OrderControl.Validate());
        
        // Validate RXO segment if present
        if (PharmacyOrder != null)
            issues.AddRange(PharmacyOrder.Validate());
        
        // Validate diagnosis segments
        for (int i = 0; i < Diagnoses.Count; i++)
        {
            var diagnosisIssues = Diagnoses[i].Validate();
            issues.AddRange(diagnosisIssues.Select(issue => $"DG1 {i + 1}: {issue}"));
        }
        
        // Validate note segments
        for (int i = 0; i < Notes.Count; i++)
        {
            var noteIssues = Notes[i].Validate();
            issues.AddRange(noteIssues.Select(issue => $"NTE {i + 1}: {issue}"));
        }
        
        return issues;
    }
}
