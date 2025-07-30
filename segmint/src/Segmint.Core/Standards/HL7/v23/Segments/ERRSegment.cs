// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Error (ERR) segment.
/// This segment provides detailed error information for failed or rejected
/// messages, particularly important for pharmacy order processing where
/// detailed error reporting is essential for clinical safety.
/// Used in ORR (Order Response) and acknowledgment messages.
/// </summary>
public class ERRSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "ERR";

    /// <summary>
    /// Initializes a new instance of the <see cref="ERRSegment"/> class.
    /// </summary>
    public ERRSegment()
    {
    }

    /// <summary>
    /// Gets or sets the error code and location (ERR.1) - Required.
    /// Contains the specific error code and field location where the error occurred.
    /// </summary>
    public CodedElementField ErrorCodeAndLocation
    {
        get => this[1] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the error location (ERR.2).
    /// Specific location in the message where the error occurred.
    /// Format: segment^sequence^field^component^subcomponent
    /// </summary>
    public StringField ErrorLocation
    {
        get => this[2] as StringField ?? new StringField();
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the HL7 error code (ERR.3).
    /// Standard HL7 error code from Table 0357.
    /// </summary>
    public CodedElementField HL7ErrorCode
    {
        get => this[3] as CodedElementField ?? new CodedElementField();
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the severity (ERR.4) - Required.
    /// Severity level of the error (E=Error, W=Warning, I=Information, F=Fatal).
    /// </summary>
    public IdentifierField Severity
    {
        get => this[4] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the application error code (ERR.5).
    /// Application-specific error code.
    /// </summary>
    public CodedElementField ApplicationErrorCode
    {
        get => this[5] as CodedElementField ?? new CodedElementField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the application error parameter (ERR.6).
    /// Additional parameters related to the application error.
    /// </summary>
    public StringField ApplicationErrorParameter
    {
        get => this[6] as StringField ?? new StringField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the diagnostic information (ERR.7).
    /// Detailed diagnostic information about the error.
    /// </summary>
    public TextField DiagnosticInformation
    {
        get => this[7] as TextField ?? new TextField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the user message (ERR.8).
    /// Human-readable error message for end users.
    /// </summary>
    public TextField UserMessage
    {
        get => this[8] as TextField ?? new TextField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the inform person indicator (ERR.9).
    /// Who should be informed about this error.
    /// </summary>
    public IdentifierField InformPersonIndicator
    {
        get => this[9] as IdentifierField ?? new IdentifierField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the override type (ERR.10).
    /// Type of override that can be applied to this error.
    /// </summary>
    public CodedElementField OverrideType
    {
        get => this[10] as CodedElementField ?? new CodedElementField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the override reason code (ERR.11).
    /// Reason codes for why an override might be allowed.
    /// </summary>
    public CodedElementField OverrideReasonCode
    {
        get => this[11] as CodedElementField ?? new CodedElementField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the help desk contact point (ERR.12).
    /// Contact information for help desk or technical support.
    /// </summary>
    public ExtendedCompositeIdField HelpDeskContactPoint
    {
        get => this[12] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[12] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // ERR.1: Error Code and Location (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // ERR.2: Error Location
        AddField(new StringField());
        
        // ERR.3: HL7 Error Code
        AddField(new CodedElementField());
        
        // ERR.4: Severity (Required)
        AddField(new IdentifierField(isRequired: true));
        
        // ERR.5: Application Error Code
        AddField(new CodedElementField());
        
        // ERR.6: Application Error Parameter
        AddField(new StringField());
        
        // ERR.7: Diagnostic Information
        AddField(new TextField());
        
        // ERR.8: User Message
        AddField(new TextField());
        
        // ERR.9: Inform Person Indicator
        AddField(new IdentifierField());
        
        // ERR.10: Override Type
        AddField(new CodedElementField());
        
        // ERR.11: Override Reason Code
        AddField(new CodedElementField());
        
        // ERR.12: Help Desk Contact Point
        AddField(new ExtendedCompositeIdField());
    }

    /// <summary>
    /// Sets basic error information.
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="errorDescription">Error description</param>
    /// <param name="severity">Severity level (E=Error, W=Warning, I=Information, F=Fatal)</param>
    /// <param name="location">Location where error occurred</param>
    /// <param name="userMessage">User-friendly error message</param>
    public void SetBasicError(
        string errorCode,
        string errorDescription,
        string severity,
        string? location = null,
        string? userMessage = null)
    {
        ErrorCodeAndLocation.SetComponents(errorCode, errorDescription);
        Severity.SetValue(severity);
        
        if (!string.IsNullOrEmpty(location))
            ErrorLocation.SetValue(location);
            
        if (!string.IsNullOrEmpty(userMessage))
            UserMessage.SetValue(userMessage);
    }

    /// <summary>
    /// Sets HL7 standard error information.
    /// </summary>
    /// <param name="hl7ErrorCode">HL7 error code from Table 0357</param>
    /// <param name="hl7ErrorText">HL7 error description</param>
    public void SetHL7Error(string hl7ErrorCode, string hl7ErrorText)
    {
        HL7ErrorCode.SetComponents(hl7ErrorCode, hl7ErrorText, "HL70357");
    }

    /// <summary>
    /// Sets application-specific error information.
    /// </summary>
    /// <param name="applicationErrorCode">Application error code</param>
    /// <param name="applicationErrorText">Application error description</param>
    /// <param name="errorParameter">Additional error parameter</param>
    /// <param name="diagnosticInfo">Detailed diagnostic information</param>
    public void SetApplicationError(
        string applicationErrorCode,
        string applicationErrorText,
        string? errorParameter = null,
        string? diagnosticInfo = null)
    {
        ApplicationErrorCode.SetComponents(applicationErrorCode, applicationErrorText);
        
        if (!string.IsNullOrEmpty(errorParameter))
            ApplicationErrorParameter.SetValue(errorParameter);
            
        if (!string.IsNullOrEmpty(diagnosticInfo))
            DiagnosticInformation.SetValue(diagnosticInfo);
    }

    /// <summary>
    /// Sets field location error for validation failures.
    /// </summary>
    /// <param name="segmentId">Segment identifier (e.g., "RXO")</param>
    /// <param name="sequence">Segment sequence number</param>
    /// <param name="fieldNumber">Field number</param>
    /// <param name="component">Component number (optional)</param>
    /// <param name="subcomponent">Subcomponent number (optional)</param>
    public void SetFieldLocationError(
        string segmentId,
        int sequence,
        int fieldNumber,
        int? component = null,
        int? subcomponent = null)
    {
        var location = $"{segmentId}^{sequence}^{fieldNumber}";
        
        if (component.HasValue)
            location += $"^{component}";
            
        if (subcomponent.HasValue)
            location += $"^{subcomponent}";
            
        ErrorLocation.SetValue(location);
    }

    /// <summary>
    /// Sets pharmacy-specific error for medication orders.
    /// </summary>
    /// <param name="pharmacyErrorCode">Pharmacy system error code</param>
    /// <param name="errorDescription">Error description</param>
    /// <param name="severity">Severity level</param>
    /// <param name="medicationContext">Medication-related context</param>
    /// <param name="clinicalMessage">Clinical safety message</param>
    public void SetPharmacyError(
        string pharmacyErrorCode,
        string errorDescription,
        string severity = "E",
        string? medicationContext = null,
        string? clinicalMessage = null)
    {
        SetBasicError(pharmacyErrorCode, errorDescription, severity, null, clinicalMessage);
        
        if (!string.IsNullOrEmpty(medicationContext))
        {
            ApplicationErrorParameter.SetValue(medicationContext);
            DiagnosticInformation.SetValue($"Medication context: {medicationContext}");
        }
        
        // Set appropriate inform person indicator for pharmacy errors
        InformPersonIndicator.SetValue("P"); // Prescriber
    }

    /// <summary>
    /// Sets drug interaction error.
    /// </summary>
    /// <param name="interactionSeverity">Interaction severity</param>
    /// <param name="drug1">First drug</param>
    /// <param name="drug2">Second drug</param>
    /// <param name="clinicalEffect">Clinical effect description</param>
    public void SetDrugInteractionError(
        string interactionSeverity,
        string drug1,
        string drug2,
        string clinicalEffect)
    {
        SetBasicError("DRUG_INTERACTION", $"{interactionSeverity} drug interaction detected", "E");
        ApplicationErrorParameter.SetValue($"{drug1}|{drug2}");
        DiagnosticInformation.SetValue($"Interaction between {drug1} and {drug2}: {clinicalEffect}");
        UserMessage.SetValue($"WARNING: {interactionSeverity} drug interaction between {drug1} and {drug2}. {clinicalEffect}");
        InformPersonIndicator.SetValue("P"); // Prescriber must be informed
    }

    /// <summary>
    /// Sets allergy contraindication error.
    /// </summary>
    /// <param name="allergen">Allergen substance</param>
    /// <param name="medication">Contraindicated medication</param>
    /// <param name="reactionType">Type of allergic reaction</param>
    public void SetAllergyError(string allergen, string medication, string reactionType)
    {
        SetBasicError("ALLERGY_CONTRAINDICATION", "Allergy contraindication detected", "F");
        ApplicationErrorParameter.SetValue($"Allergen: {allergen}");
        DiagnosticInformation.SetValue($"Patient allergic to {allergen}. Contraindicated medication: {medication}");
        UserMessage.SetValue($"CONTRAINDICATED: Patient has documented allergy to {allergen}. Risk of {reactionType}.");
        InformPersonIndicator.SetValue("P"); // Prescriber must be informed
    }

    /// <summary>
    /// Sets dosage range error.
    /// </summary>
    /// <param name="prescribedDose">Prescribed dose</param>
    /// <param name="recommendedRange">Recommended dose range</param>
    /// <param name="patientContext">Patient-specific context</param>
    public void SetDosageRangeError(string prescribedDose, string recommendedRange, string patientContext)
    {
        SetBasicError("DOSAGE_RANGE", "Dose outside recommended range", "W");
        ApplicationErrorParameter.SetValue($"Prescribed: {prescribedDose}, Recommended: {recommendedRange}");
        DiagnosticInformation.SetValue($"Patient context: {patientContext}");
        UserMessage.SetValue($"Prescribed dose ({prescribedDose}) is outside recommended range ({recommendedRange}) for this patient.");
        InformPersonIndicator.SetValue("P"); // Prescriber should review
    }

    /// <summary>
    /// Sets override information for errors that can be overridden.
    /// </summary>
    /// <param name="overrideType">Type of override allowed</param>
    /// <param name="reasonCodes">Valid reason codes for override</param>
    /// <param name="helpDeskContact">Help desk contact info</param>
    public void SetOverrideInfo(
        string overrideType,
        string[] reasonCodes,
        string? helpDeskContact = null)
    {
        OverrideType.SetComponents(overrideType);
        
        if (reasonCodes.Any())
        {
            var reasonCodesText = string.Join(", ", reasonCodes);
            OverrideReasonCode.SetComponents(reasonCodesText);
        }
        
        if (!string.IsNullOrEmpty(helpDeskContact))
            HelpDeskContactPoint.SetValue(helpDeskContact);
    }

    /// <summary>
    /// Determines if this error is fatal (blocks processing).
    /// </summary>
    /// <returns>True if error severity is Fatal.</returns>
    public bool IsFatal()
    {
        return Severity.Value == "F";
    }

    /// <summary>
    /// Determines if this error can be overridden.
    /// </summary>
    /// <returns>True if override type is specified.</returns>
    public bool CanBeOverridden()
    {
        return !string.IsNullOrEmpty(OverrideType.Value);
    }

    /// <summary>
    /// Gets a display-friendly representation of the error.
    /// </summary>
    /// <returns>Formatted error string.</returns>
    public string GetDisplayValue()
    {
        var severity = GetSeverityDescription(Severity.Value);
        var code = ErrorCodeAndLocation.Identifier ?? "";
        var description = ErrorCodeAndLocation.Text ?? "";
        var userMsg = UserMessage.Value ?? description;
        
        return $"{severity}: {code} - {userMsg}";
    }

    /// <summary>
    /// Gets severity description from code.
    /// </summary>
    /// <param name="severityCode">Severity code</param>
    /// <returns>Severity description</returns>
    private static string GetSeverityDescription(string? severityCode)
    {
        return severityCode switch
        {
            "F" => "FATAL",
            "E" => "ERROR", 
            "W" => "WARNING",
            "I" => "INFO",
            _ => "UNKNOWN"
        };
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(ErrorCodeAndLocation.Identifier))
            errors.Add("Error Code and Location (ERR.1) is required");

        if (string.IsNullOrEmpty(Severity.Value))
            errors.Add("Severity (ERR.4) is required");

        // Validate severity
        var validSeverities = new[] { "F", "E", "W", "I" };
        if (!string.IsNullOrEmpty(Severity.Value) && !validSeverities.Contains(Severity.Value))
        {
            errors.Add($"Severity '{Severity.Value}' is not valid. Valid values: F (Fatal), E (Error), W (Warning), I (Information)");
        }

        // Validate inform person indicator
        if (!string.IsNullOrEmpty(InformPersonIndicator.Value))
        {
            var validIndicators = new[] { "P", "U", "S" }; // Prescriber, User, System
            if (!validIndicators.Contains(InformPersonIndicator.Value))
            {
                errors.Add($"Inform Person Indicator '{InformPersonIndicator.Value}' is not valid. Valid values: P (Prescriber), U (User), S (System)");
            }
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new ERRSegment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    /// <summary>
    /// Creates a validation error for required field.
    /// </summary>
    /// <param name="fieldName">Name of the required field</param>
    /// <param name="segmentLocation">Location in message</param>
    /// <returns>Configured ERR segment.</returns>
    public static ERRSegment CreateRequiredFieldError(string fieldName, string segmentLocation)
    {
        var err = new ERRSegment();
        err.SetBasicError("REQUIRED_FIELD_MISSING", $"Required field {fieldName} is missing", "E", segmentLocation);
        err.SetHL7Error("101", "Required field missing");
        return err;
    }

    /// <summary>
    /// Creates a data type validation error.
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="expectedType">Expected data type</param>
    /// <param name="actualValue">Actual value received</param>
    /// <returns>Configured ERR segment.</returns>
    public static ERRSegment CreateDataTypeError(string fieldName, string expectedType, string actualValue)
    {
        var err = new ERRSegment();
        err.SetBasicError("DATA_TYPE_ERROR", $"Invalid data type for {fieldName}", "E");
        err.ApplicationErrorParameter.SetValue($"Expected: {expectedType}, Received: {actualValue}");
        err.SetHL7Error("102", "Data type error");
        return err;
    }

    /// <summary>
    /// Creates a pharmacy business rule violation error.
    /// </summary>
    /// <param name="ruleName">Name of the violated rule</param>
    /// <param name="ruleDescription">Description of the rule</param>
    /// <param name="severity">Error severity</param>
    /// <returns>Configured ERR segment.</returns>
    public static ERRSegment CreateBusinessRuleError(string ruleName, string ruleDescription, string severity = "E")
    {
        var err = new ERRSegment();
        err.SetPharmacyError(ruleName, ruleDescription, severity);
        err.SetHL7Error("207", "Application internal error");
        return err;
    }
}
