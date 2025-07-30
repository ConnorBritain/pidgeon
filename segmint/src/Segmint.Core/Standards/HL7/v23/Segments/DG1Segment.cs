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
/// Represents an HL7 Diagnosis (DG1) segment.
/// This segment contains diagnosis information including ICD codes, descriptions,
/// and clinical context for the patient's condition. Essential for pharmacy
/// orders to provide clinical justification and decision support.
/// Used in ADT, ORM, ORR, and RDE messages.
/// </summary>
public class DG1Segment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "DG1";

    /// <summary>
    /// Initializes a new instance of the <see cref="DG1Segment"/> class.
    /// </summary>
    public DG1Segment()
    {
    }

    /// <summary>
    /// Gets or sets the set ID (DG1.1) - Required.
    /// Sequential number for multiple diagnosis segments.
    /// </summary>
    public SequenceIdField SetId
    {
        get => this[1] as SequenceIdField ?? new SequenceIdField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis coding method (DG1.2) - Required.
    /// Specifies the coding system used (ICD9, ICD10, SNOMED, etc.).
    /// </summary>
    public IdentifierField DiagnosisCodingMethod
    {
        get => this[2] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis code (DG1.3) - Required.
    /// The actual diagnosis code from the specified coding system.
    /// </summary>
    public CodedElementField DiagnosisCode
    {
        get => this[3] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis description (DG1.4).
    /// Text description of the diagnosis.
    /// </summary>
    public StringField DiagnosisDescription
    {
        get => this[4] as StringField ?? new StringField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis date/time (DG1.5).
    /// When the diagnosis was made or recorded.
    /// </summary>
    public TimestampField DiagnosisDateTime
    {
        get => this[5] as TimestampField ?? new TimestampField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis type (DG1.6) - Required.
    /// Type of diagnosis (A=Admission, W=Working, F=Final, etc.).
    /// </summary>
    public IdentifierField DiagnosisType
    {
        get => this[6] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the major diagnostic category (DG1.7).
    /// Major diagnostic category for DRG grouping.
    /// </summary>
    public CodedElementField MajorDiagnosticCategory
    {
        get => this[7] as CodedElementField ?? new CodedElementField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the diagnostic related group (DG1.8).
    /// DRG code for reimbursement purposes.
    /// </summary>
    public CodedElementField DiagnosticRelatedGroup
    {
        get => this[8] as CodedElementField ?? new CodedElementField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the DRG approval indicator (DG1.9).
    /// Whether the DRG has been approved (Y/N).
    /// </summary>
    public IdentifierField DRGApprovalIndicator
    {
        get => this[9] as IdentifierField ?? new IdentifierField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the DRG grouper review code (DG1.10).
    /// Review code from the DRG grouper.
    /// </summary>
    public IdentifierField DRGGrouperReviewCode
    {
        get => this[10] as IdentifierField ?? new IdentifierField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the outlier type (DG1.11).
    /// Type of outlier (cost or day outlier).
    /// </summary>
    public CodedElementField OutlierType
    {
        get => this[11] as CodedElementField ?? new CodedElementField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the outlier days (DG1.12).
    /// Number of outlier days.
    /// </summary>
    public NumericField OutlierDays
    {
        get => this[12] as NumericField ?? new NumericField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the outlier cost (DG1.13).
    /// Outlier cost amount.
    /// </summary>
    public CompositeQuantityField OutlierCost
    {
        get => this[13] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the grouper version and type (DG1.14).
    /// Version and type of the DRG grouper software.
    /// </summary>
    public StringField GrouperVersionAndType
    {
        get => this[14] as StringField ?? new StringField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis priority (DG1.15).
    /// Priority ranking of this diagnosis.
    /// </summary>
    public NumericField DiagnosisPriority
    {
        get => this[15] as NumericField ?? new NumericField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosing clinician (DG1.16).
    /// Clinician who made the diagnosis.
    /// </summary>
    public ExtendedCompositeIdField DiagnosingClinician
    {
        get => this[16] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the diagnosis classification (DG1.17).
    /// Classification of the diagnosis (M=Medical, S=Surgical, etc.).
    /// </summary>
    public IdentifierField DiagnosisClassification
    {
        get => this[17] as IdentifierField ?? new IdentifierField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the confidential indicator (DG1.18).
    /// Whether this diagnosis is confidential (Y/N).
    /// </summary>
    public IdentifierField ConfidentialIndicator
    {
        get => this[18] as IdentifierField ?? new IdentifierField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the attestation date/time (DG1.19).
    /// When the diagnosis was attested or confirmed.
    /// </summary>
    public TimestampField AttestationDateTime
    {
        get => this[19] as TimestampField ?? new TimestampField();
        set => this[19] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // DG1.1: Set ID - Diagnosis (Required)
        AddField(new SequenceIdField(isRequired: true));
        
        // DG1.2: Diagnosis Coding Method (Required)
        AddField(new IdentifierField(isRequired: true));
        
        // DG1.3: Diagnosis Code (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // DG1.4: Diagnosis Description
        AddField(new StringField());
        
        // DG1.5: Diagnosis Date/Time
        AddField(new TimestampField());
        
        // DG1.6: Diagnosis Type (Required)
        AddField(new IdentifierField(isRequired: true));
        
        // DG1.7: Major Diagnostic Category
        AddField(new CodedElementField());
        
        // DG1.8: Diagnostic Related Group
        AddField(new CodedElementField());
        
        // DG1.9: DRG Approval Indicator
        AddField(new IdentifierField());
        
        // DG1.10: DRG Grouper Review Code
        AddField(new IdentifierField());
        
        // DG1.11: Outlier Type
        AddField(new CodedElementField());
        
        // DG1.12: Outlier Days
        AddField(new NumericField());
        
        // DG1.13: Outlier Cost
        AddField(new CompositeQuantityField());
        
        // DG1.14: Grouper Version and Type
        AddField(new StringField());
        
        // DG1.15: Diagnosis Priority
        AddField(new NumericField());
        
        // DG1.16: Diagnosing Clinician
        AddField(new ExtendedCompositeIdField());
        
        // DG1.17: Diagnosis Classification
        AddField(new IdentifierField());
        
        // DG1.18: Confidential Indicator
        AddField(new IdentifierField());
        
        // DG1.19: Attestation Date/Time
        AddField(new TimestampField());
    }

    /// <summary>
    /// Sets basic diagnosis information.
    /// </summary>
    /// <param name="setId">Set ID for this diagnosis</param>
    /// <param name="codingMethod">Coding method (ICD9, ICD10, SNOMED)</param>
    /// <param name="diagnosisCode">Diagnosis code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="diagnosisType">Type of diagnosis (A=Admission, W=Working, F=Final)</param>
    /// <param name="diagnosisDateTime">When diagnosis was made</param>
    public void SetBasicDiagnosis(
        int setId,
        string codingMethod,
        string diagnosisCode,
        string description,
        string diagnosisType,
        DateTime? diagnosisDateTime = null)
    {
        SetId.SetValue(setId.ToString());
        DiagnosisCodingMethod.SetValue(codingMethod);
        DiagnosisCode.SetComponents(diagnosisCode, description, codingMethod);
        DiagnosisDescription.SetValue(description);
        DiagnosisType.SetValue(diagnosisType);
        
        if (diagnosisDateTime.HasValue)
            DiagnosisDateTime.SetValue(diagnosisDateTime.Value);
    }

    /// <summary>
    /// Sets ICD-10 diagnosis information.
    /// </summary>
    /// <param name="setId">Set ID</param>
    /// <param name="icd10Code">ICD-10 diagnosis code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="diagnosisType">Type of diagnosis</param>
    /// <param name="priority">Diagnosis priority (1=Primary)</param>
    public void SetICD10Diagnosis(
        int setId,
        string icd10Code,
        string description,
        string diagnosisType = "F",
        int? priority = null)
    {
        SetBasicDiagnosis(setId, "ICD10", icd10Code, description, diagnosisType, DateTime.Now);
        
        if (priority.HasValue)
            DiagnosisPriority.SetValue(priority.Value.ToString());
    }

    /// <summary>
    /// Sets ICD-9 diagnosis information.
    /// </summary>
    /// <param name="setId">Set ID</param>
    /// <param name="icd9Code">ICD-9 diagnosis code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="diagnosisType">Type of diagnosis</param>
    /// <param name="priority">Diagnosis priority</param>
    public void SetICD9Diagnosis(
        int setId,
        string icd9Code,
        string description,
        string diagnosisType = "F",
        int? priority = null)
    {
        SetBasicDiagnosis(setId, "ICD9", icd9Code, description, diagnosisType, DateTime.Now);
        
        if (priority.HasValue)
            DiagnosisPriority.SetValue(priority.Value.ToString());
    }

    /// <summary>
    /// Sets SNOMED CT diagnosis information.
    /// </summary>
    /// <param name="setId">Set ID</param>
    /// <param name="snomedCode">SNOMED CT concept ID</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="diagnosisType">Type of diagnosis</param>
    /// <param name="priority">Diagnosis priority</param>
    public void SetSNOMEDDiagnosis(
        int setId,
        string snomedCode,
        string description,
        string diagnosisType = "F",
        int? priority = null)
    {
        SetBasicDiagnosis(setId, "SNM3", snomedCode, description, diagnosisType, DateTime.Now);
        
        if (priority.HasValue)
            DiagnosisPriority.SetValue(priority.Value.ToString());
    }

    /// <summary>
    /// Sets clinical information.
    /// </summary>
    /// <param name="clinician">Diagnosing clinician</param>
    /// <param name="classification">Diagnosis classification (M=Medical, S=Surgical)</param>
    /// <param name="confidential">Whether diagnosis is confidential</param>
    /// <param name="attestationDate">Attestation date</param>
    public void SetClinicalInfo(
        string? clinician = null,
        string? classification = null,
        bool? confidential = null,
        DateTime? attestationDate = null)
    {
        if (!string.IsNullOrEmpty(clinician))
            DiagnosingClinician.SetValue(clinician);
            
        if (!string.IsNullOrEmpty(classification))
            DiagnosisClassification.SetValue(classification);
            
        if (confidential.HasValue)
            ConfidentialIndicator.SetValue(confidential.Value ? "Y" : "N");
            
        if (attestationDate.HasValue)
            AttestationDateTime.SetValue(attestationDate.Value);
    }

    /// <summary>
    /// Sets DRG information for billing.
    /// </summary>
    /// <param name="majorDiagnosticCategory">Major diagnostic category</param>
    /// <param name="diagnosticRelatedGroup">DRG code</param>
    /// <param name="approved">Whether DRG is approved</param>
    /// <param name="grouperVersion">Grouper software version</param>
    public void SetDRGInfo(
        string? majorDiagnosticCategory = null,
        string? diagnosticRelatedGroup = null,
        bool? approved = null,
        string? grouperVersion = null)
    {
        if (!string.IsNullOrEmpty(majorDiagnosticCategory))
            MajorDiagnosticCategory.SetComponents(majorDiagnosticCategory);
            
        if (!string.IsNullOrEmpty(diagnosticRelatedGroup))
            DiagnosticRelatedGroup.SetComponents(diagnosticRelatedGroup);
            
        if (approved.HasValue)
            DRGApprovalIndicator.SetValue(approved.Value ? "Y" : "N");
            
        if (!string.IsNullOrEmpty(grouperVersion))
            GrouperVersionAndType.SetValue(grouperVersion);
    }

    /// <summary>
    /// Determines if this is a primary diagnosis.
    /// </summary>
    /// <returns>True if this is the primary diagnosis (priority 1 or set ID 1).</returns>
    public bool IsPrimaryDiagnosis()
    {
        // Check priority first, then fall back to set ID
        if (!string.IsNullOrEmpty(DiagnosisPriority.RawValue))
            return DiagnosisPriority.RawValue == "1";
            
        return SetId.RawValue == "1";
    }

    /// <summary>
    /// Gets a display-friendly representation of the diagnosis.
    /// </summary>
    /// <returns>Formatted diagnosis string.</returns>
    public string GetDisplayValue()
    {
        var code = DiagnosisCode.Identifier ?? "";
        var description = DiagnosisDescription.Value ?? DiagnosisCode.Text ?? "";
        var method = DiagnosisCodingMethod.Value ?? "";
        var type = DiagnosisType.Value ?? "";
        
        var priority = IsPrimaryDiagnosis() ? " (Primary)" : "";
        
        return string.IsNullOrEmpty(description) 
            ? $"{method} {code}{priority}"
            : $"{method} {code}: {description}{priority}";
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(SetId.Value))
            errors.Add("Set ID (DG1.1) is required");

        if (string.IsNullOrEmpty(DiagnosisCodingMethod.Value))
            errors.Add("Diagnosis Coding Method (DG1.2) is required");

        if (string.IsNullOrEmpty(DiagnosisCode.Identifier))
            errors.Add("Diagnosis Code (DG1.3) is required");

        if (string.IsNullOrEmpty(DiagnosisType.Value))
            errors.Add("Diagnosis Type (DG1.6) is required");

        // Validate coding method
        var validCodingMethods = new[] { "ICD9", "ICD10", "ICD10CM", "ICD10PCS", "SNM3", "CPT4", "HCPCS" };
        if (!string.IsNullOrEmpty(DiagnosisCodingMethod.Value) && 
            !validCodingMethods.Contains(DiagnosisCodingMethod.Value))
        {
            errors.Add($"Diagnosis Coding Method '{DiagnosisCodingMethod.Value}' is not recognized. Valid values: {string.Join(", ", validCodingMethods)}");
        }

        // Validate diagnosis type
        var validDiagnosisTypes = new[] { "A", "W", "F" };
        if (!string.IsNullOrEmpty(DiagnosisType.Value) && 
            !validDiagnosisTypes.Contains(DiagnosisType.Value))
        {
            errors.Add($"Diagnosis Type '{DiagnosisType.Value}' is not valid. Valid values: A (Admission), W (Working), F (Final)");
        }

        // Validate Y/N fields
        if (!string.IsNullOrEmpty(DRGApprovalIndicator.Value) && 
            DRGApprovalIndicator.Value != "Y" && DRGApprovalIndicator.Value != "N")
        {
            errors.Add("DRG Approval Indicator (DG1.9) must be Y or N");
        }

        if (!string.IsNullOrEmpty(ConfidentialIndicator.Value) && 
            ConfidentialIndicator.Value != "Y" && ConfidentialIndicator.Value != "N")
        {
            errors.Add("Confidential Indicator (DG1.18) must be Y or N");
        }

        // Validate diagnosis classification
        if (!string.IsNullOrEmpty(DiagnosisClassification.Value))
        {
            var validClassifications = new[] { "M", "S", "P" };
            if (!validClassifications.Contains(DiagnosisClassification.Value))
            {
                errors.Add($"Diagnosis Classification '{DiagnosisClassification.Value}' is not valid. Valid values: M (Medical), S (Surgical), P (Principal)");
            }
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new DG1Segment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    /// <summary>
    /// Creates a primary ICD-10 diagnosis.
    /// </summary>
    /// <param name="icd10Code">ICD-10 code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="clinician">Diagnosing clinician</param>
    /// <returns>Configured DG1 segment.</returns>
    public static DG1Segment CreatePrimaryICD10(string icd10Code, string description, string? clinician = null)
    {
        var dg1 = new DG1Segment();
        dg1.SetICD10Diagnosis(1, icd10Code, description, "F", 1);
        if (!string.IsNullOrEmpty(clinician))
            dg1.SetClinicalInfo(clinician);
        return dg1;
    }

    /// <summary>
    /// Creates a secondary diagnosis.
    /// </summary>
    /// <param name="setId">Set ID</param>
    /// <param name="codingMethod">Coding method</param>
    /// <param name="code">Diagnosis code</param>
    /// <param name="description">Description</param>
    /// <param name="priority">Priority ranking</param>
    /// <returns>Configured DG1 segment.</returns>
    public static DG1Segment CreateSecondaryDiagnosis(int setId, string codingMethod, string code, string description, int priority)
    {
        var dg1 = new DG1Segment();
        dg1.SetBasicDiagnosis(setId, codingMethod, code, description, "F");
        dg1.DiagnosisPriority.SetValue(priority.ToString());
        return dg1;
    }
}
