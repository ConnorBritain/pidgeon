// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Segmint.Core.Standards.HL7.v23.Workflows.Common;

/// <summary>
/// Extension methods for workflow operations across all HL7 message types.
/// Provides common functionality for workflow templates and builders.
/// </summary>
public static class WorkflowExtensions
{
    #region Message Extensions

    /// <summary>
    /// Sets standard application information for workflow messages.
    /// </summary>
    /// <param name="message">HL7 message</param>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <returns>The message for method chaining</returns>
    public static T WithApplicationInfo<T>(this T message, 
        string sendingApplication,
        string sendingFacility,
        string receivingApplication,
        string receivingFacility) where T : HL7Message
    {
        var msh = message.GetSegment<MSHSegment>();
        if (msh != null)
        {
            msh.SetBasicInfo(sendingApplication, sendingFacility, receivingApplication, receivingFacility);
        }
        return message;
    }

    /// <summary>
    /// Sets processing environment information.
    /// </summary>
    /// <param name="message">HL7 message</param>
    /// <param name="isProduction">True for production, false for test</param>
    /// <param name="version">HL7 version (default: 2.3)</param>
    /// <returns>The message for method chaining</returns>
    public static T WithProcessingInfo<T>(this T message, 
        bool isProduction = false,
        string version = "2.3") where T : HL7Message
    {
        var msh = message.GetSegment<MSHSegment>();
        if (msh != null)
        {
            msh.SetProcessingId(isProduction);
        }
        return message;
    }

    /// <summary>
    /// Adds a workflow note to the message.
    /// </summary>
    /// <param name="message">HL7 message</param>
    /// <param name="noteText">Note text</param>
    /// <param name="noteType">Note type/source</param>
    /// <returns>The message for method chaining</returns>
    public static T WithWorkflowNote<T>(this T message, 
        string noteText,
        string? noteType = null) where T : HL7Message
    {
        var note = new NTESegment();
        note.Comment.SetValue(noteText);
        if (!string.IsNullOrWhiteSpace(noteType))
            note.SourceOfComment.SetValue(noteType);
        message.InsertSegment(message.SegmentCount, note);
        return message;
    }

    /// <summary>
    /// Gets a summary of the workflow message.
    /// </summary>
    /// <param name="message">HL7 message</param>
    /// <returns>Workflow summary information</returns>
    public static WorkflowSummary GetWorkflowSummary(this HL7Message message)
    {
        var msh = message.GetSegment<MSHSegment>();
        var pid = message.GetSegment<PIDSegment>();
        
        return new WorkflowSummary
        {
            MessageType = msh?.MessageType.GetPrimaryValue() ?? "Unknown",
            TriggerEvent = msh?.TriggerEvent.Value ?? "Unknown",
            MessageControlId = msh?.MessageControlId.Value ?? "Unknown",
            MessageDateTime = msh?.DateTimeOfMessage.ToDateTime(),
            PatientId = pid?.PatientIdentifierList.IdNumber,
            PatientName = pid?.PatientName.DisplayName,
            WorkflowType = GetWorkflowType(message),
            ValidationStatus = GetValidationStatus(message)
        };
    }

    #endregion

    #region Validation Extensions

    /// <summary>
    /// Validates a workflow message with enhanced error reporting.
    /// </summary>
    /// <param name="message">HL7 message to validate</param>
    /// <returns>Enhanced validation result</returns>
    public static WorkflowValidationResult ValidateWorkflow(this HL7Message message)
    {
        var result = new WorkflowValidationResult();
        
        // Get basic validation issues
        var basicIssues = message.Validate();
        result.ValidationIssues.AddRange(basicIssues.Issues.Select(issue => issue.ToString()));
        
        // Add workflow-specific validations
        AddWorkflowSpecificValidations(message, result);
        
        // Set overall status
        result.IsValid = result.ValidationIssues.Count == 0;
        result.HasWarnings = result.ValidationIssues.Any(i => i.Contains("Warning"));
        
        return result;
    }

    /// <summary>
    /// Checks if a message is ready for transmission.
    /// </summary>
    /// <param name="message">HL7 message</param>
    /// <returns>True if ready for transmission</returns>
    public static bool IsReadyForTransmission(this HL7Message message)
    {
        var validation = message.ValidateWorkflow();
        return validation.IsValid && !validation.HasCriticalErrors;
    }

    #endregion

    #region Message Type Specific Extensions

    /// <summary>
    /// Gets pharmacy-specific information from RDE messages.
    /// </summary>
    /// <param name="rdeMessage">RDE message</param>
    /// <returns>Pharmacy prescription information</returns>
    public static PharmacyPrescriptionInfo GetPrescriptionInfo(this RDEMessage rdeMessage)
    {
        var rxe = rdeMessage.GetSegment<RXESegment>();
        var pid = rdeMessage.GetSegment<PIDSegment>();
        var orc = rdeMessage.GetSegment<ORCSegment>();
        
        return new PharmacyPrescriptionInfo
        {
            PrescriptionNumber = rxe?.PrescriptionNumber.Value,
            MedicationCode = rxe?.GiveCode.Identifier,
            MedicationName = rxe?.GiveCode.Text,
            Instructions = rxe?.ProviderPharmacyTreatmentInstructions.Value,
            PatientId = pid?.PatientIdentifierList.IdNumber,
            PatientName = pid?.PatientName.DisplayName,
            OrderingProvider = orc?.OrderingProvider.Value ?? "Unknown",
            OrderDateTime = orc?.DateTimeOfTransaction.ToDateTime()
        };
    }

    /// <summary>
    /// Gets dispense information from RDS messages.
    /// </summary>
    /// <param name="rdsMessage">RDS message</param>
    /// <returns>Dispense information</returns>
    public static List<DispenseInfo> GetDispenseInfo(this RDSMessage rdsMessage)
    {
        var dispenseInfos = new List<DispenseInfo>();
        
        foreach (var dispense in rdsMessage.Dispenses)
        {
            dispenseInfos.Add(new DispenseInfo
            {
                MedicationCode = dispense.DispenseRecord?.DispenseGiveCode.Identifier,
                MedicationName = dispense.DispenseRecord?.DispenseGiveCode.Text,
                DispensedAmount = dispense.DispenseRecord?.ActualDispenseAmount.ToDecimal()?.ToString(),
                DispenseUnits = dispense.DispenseRecord?.ActualDispenseUnits.Identifier,
                DateDispensed = dispense.DispenseRecord?.DateTimeDispensed.ToDateTime(),
                PharmacistName = "Unknown" // DispensingPharmacist field mapping needs review
            });
        }
        
        return dispenseInfos;
    }

    /// <summary>
    /// Gets order response information from ORR messages.
    /// </summary>
    /// <param name="orrMessage">ORR message</param>
    /// <returns>Order response information</returns>
    public static OrderResponseInfo GetOrderResponseInfo(this ORRMessage orrMessage)
    {
        var msa = orrMessage.GetSegment<MSASegment>();
        var errors = orrMessage.Errors;
        
        return new OrderResponseInfo
        {
            AcknowledgmentCode = msa?.AcknowledgmentCode.Value,
            OriginalMessageControlId = msa?.MessageControlId.Value,
            ResponseText = msa?.TextMessage.Value,
            ResponseDateTime = orrMessage.Header.DateTimeOfMessage.ToDateTime(),
            HasErrors = errors.Count > 0,
            ErrorCount = errors.Count,
            Errors = errors.Select(e => new ErrorInfo
            {
                ErrorCode = e.ErrorCodeAndLocation.Identifier,
                ErrorText = e.ErrorCodeAndLocation.Text,
                Severity = e.Severity.Value
            }).ToList()
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the workflow type based on message type.
    /// </summary>
    private static string GetWorkflowType(HL7Message message)
    {
        return message switch
        {
            RDEMessage => "Prescription Order",
            ORRMessage => "Order Response",
            RDSMessage => "Medication Dispense",
            ORMMessage => "Order Management",
            ADTMessage => "Patient Administration",
            _ => "Unknown Workflow"
        };
    }

    /// <summary>
    /// Gets the validation status for a message.
    /// </summary>
    private static string GetValidationStatus(HL7Message message)
    {
        var issues = message.Validate();
        return issues.Issues.Count == 0 ? "Valid" : $"{issues.Issues.Count} issues found";
    }

    /// <summary>
    /// Adds workflow-specific validations to the result.
    /// </summary>
    private static void AddWorkflowSpecificValidations(HL7Message message, WorkflowValidationResult result)
    {
        switch (message)
        {
            case RDEMessage rde:
                ValidateRDEWorkflow(rde, result);
                break;
            case ORRMessage orr:
                ValidateORRWorkflow(orr, result);
                break;
            case RDSMessage rds:
                ValidateRDSWorkflow(rds, result);
                break;
        }
    }

    /// <summary>
    /// Validates RDE workflow-specific rules.
    /// </summary>
    private static void ValidateRDEWorkflow(RDEMessage rde, WorkflowValidationResult result)
    {
        var rxe = rde.GetSegment<RXESegment>();
        var pid = rde.GetSegment<PIDSegment>();
        
        // Check for required prescription information
        if (rxe?.GiveCode.Identifier == null)
            result.ValidationIssues.Add("Prescription workflow requires medication code");
        
        if (rxe?.ProviderPharmacyTreatmentInstructions.Value == null)
            result.ValidationIssues.Add("Prescription workflow requires dosage instructions");
        
        // Check for patient information
        if (pid?.PatientIdentifierList.IdNumber == null)
            result.ValidationIssues.Add("Prescription workflow requires patient ID");
    }

    /// <summary>
    /// Validates ORR workflow-specific rules.
    /// </summary>
    private static void ValidateORRWorkflow(ORRMessage orr, WorkflowValidationResult result)
    {
        var msa = orr.GetSegment<MSASegment>();
        
        // Check for required acknowledgment
        if (msa?.AcknowledgmentCode.Value == null)
            result.ValidationIssues.Add("Order response workflow requires acknowledgment code");
        
        // Check for original message control ID
        if (msa?.MessageControlId.Value == null)
            result.ValidationIssues.Add("Order response workflow requires original message control ID");
    }

    /// <summary>
    /// Validates RDS workflow-specific rules.
    /// </summary>
    private static void ValidateRDSWorkflow(RDSMessage rds, WorkflowValidationResult result)
    {
        if (rds.Dispenses.Count == 0)
            result.ValidationIssues.Add("Dispense workflow requires at least one dispense record");
        
        foreach (var dispense in rds.Dispenses)
        {
            if (dispense.DispenseRecord?.ActualDispenseAmount.ToDecimal() == null)
                result.ValidationIssues.Add("Dispense workflow requires dispense amount");
        }
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Summary information for a workflow message.
/// </summary>
public class WorkflowSummary
{
    public string MessageType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string MessageControlId { get; set; } = string.Empty;
    public DateTime? MessageDateTime { get; set; }
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
}

/// <summary>
/// Enhanced validation result for workflow messages.
/// </summary>
public class WorkflowValidationResult
{
    public bool IsValid { get; set; }
    public bool HasWarnings { get; set; }
    public bool HasCriticalErrors { get; set; }
    public List<string> ValidationIssues { get; set; } = new();
    public DateTime ValidationDateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// Pharmacy prescription information.
/// </summary>
public class PharmacyPrescriptionInfo
{
    public string? PrescriptionNumber { get; set; }
    public string? MedicationCode { get; set; }
    public string? MedicationName { get; set; }
    public string? Instructions { get; set; }
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? OrderingProvider { get; set; }
    public DateTime? OrderDateTime { get; set; }
}

/// <summary>
/// Dispense information.
/// </summary>
public class DispenseInfo
{
    public string? MedicationCode { get; set; }
    public string? MedicationName { get; set; }
    public string? DispensedAmount { get; set; }
    public string? DispenseUnits { get; set; }
    public DateTime? DateDispensed { get; set; }
    public string? PharmacistName { get; set; }
}

/// <summary>
/// Order response information.
/// </summary>
public class OrderResponseInfo
{
    public string? AcknowledgmentCode { get; set; }
    public string? OriginalMessageControlId { get; set; }
    public string? ResponseText { get; set; }
    public DateTime? ResponseDateTime { get; set; }
    public bool HasErrors { get; set; }
    public int ErrorCount { get; set; }
    public List<ErrorInfo> Errors { get; set; } = new();
}

/// <summary>
/// Error information.
/// </summary>
public class ErrorInfo
{
    public string? ErrorCode { get; set; }
    public string? ErrorText { get; set; }
    public string? Severity { get; set; }
}

#endregion