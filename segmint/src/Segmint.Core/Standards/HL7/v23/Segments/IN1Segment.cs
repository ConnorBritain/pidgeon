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
/// Represents an HL7 Insurance (IN1) segment.
/// This segment contains insurance information for a patient including plan details, coverage, and policy holder information.
/// Used in ADT, DFT, and other financially-related message types.
/// </summary>
public class IN1Segment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "IN1";

    /// <summary>
    /// Initializes a new instance of the <see cref="IN1Segment"/> class.
    /// </summary>
    public IN1Segment()
    {
    }

    /// <summary>
    /// Gets or sets the set ID (IN1.1) - Required.
    /// Sequential number identifying the insurance plan when multiple plans exist.
    /// </summary>
    public SequenceIdField SetId
    {
        get => this[1] as SequenceIdField ?? new SequenceIdField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the insurance plan ID (IN1.2) - Required.
    /// Unique identifier for the insurance plan.
    /// </summary>
    public CodedElementField InsurancePlanId
    {
        get => this[2] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the insurance company ID (IN1.3) - Required.
    /// Identifier for the insurance company.
    /// </summary>
    public ExtendedCompositeIdField InsuranceCompanyId
    {
        get => this[3] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField(isRequired: true);
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the insurance company name (IN1.4).
    /// Name of the insurance company.
    /// </summary>
    public ExtendedCompositeIdField InsuranceCompanyName
    {
        get => this[4] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the insurance company address (IN1.5).
    /// Address of the insurance company.
    /// </summary>
    public AddressField InsuranceCompanyAddress
    {
        get => this[5] as AddressField ?? new AddressField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the insurance co contact person (IN1.6).
    /// Contact person at the insurance company.
    /// </summary>
    public PersonNameField InsuranceCoContactPerson
    {
        get => this[6] as PersonNameField ?? new PersonNameField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the insurance co phone number (IN1.7).
    /// Phone number for the insurance company.
    /// </summary>
    public TelephoneField InsuranceCoPhoneNumber
    {
        get => this[7] as TelephoneField ?? new TelephoneField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the group number (IN1.8).
    /// Group number for the insurance plan.
    /// </summary>
    public StringField GroupNumber
    {
        get => this[8] as StringField ?? new StringField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the group name (IN1.9).
    /// Group name for the insurance plan.
    /// </summary>
    public ExtendedCompositeIdField GroupName
    {
        get => this[9] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the insured's group employee ID (IN1.10).
    /// Employee ID of the insured person within the group.
    /// </summary>
    public ExtendedCompositeIdField InsuredsGroupEmployeeId
    {
        get => this[10] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the insured's group employee name (IN1.11).
    /// Name of the group or employer for the insured.
    /// </summary>
    public ExtendedCompositeIdField InsuredsGroupEmployeeName
    {
        get => this[11] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the plan effective date (IN1.12).
    /// Date when the insurance plan becomes effective.
    /// </summary>
    public DateField PlanEffectiveDate
    {
        get => this[12] as DateField ?? new DateField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the plan expiration date (IN1.13).
    /// Date when the insurance plan expires.
    /// </summary>
    public DateField PlanExpirationDate
    {
        get => this[13] as DateField ?? new DateField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the authorization information (IN1.14).
    /// Authorization or approval information for coverage.
    /// </summary>
    public StringField AuthorizationInformation
    {
        get => this[14] as StringField ?? new StringField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the plan type (IN1.15).
    /// Type of insurance plan (HMO, PPO, etc.).
    /// </summary>
    public CodedElementField PlanType
    {
        get => this[15] as CodedElementField ?? new CodedElementField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the name of insured (IN1.16).
    /// Name of the person who holds the insurance policy.
    /// </summary>
    public PersonNameField NameOfInsured
    {
        get => this[16] as PersonNameField ?? new PersonNameField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the insured's relationship to patient (IN1.17).
    /// Relationship of the insured person to the patient.
    /// </summary>
    public CodedElementField InsuredsRelationshipToPatient
    {
        get => this[17] as CodedElementField ?? new CodedElementField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the insured's date of birth (IN1.18).
    /// Date of birth of the insured person.
    /// </summary>
    public TimestampField InsuredsDateOfBirth
    {
        get => this[18] as TimestampField ?? new TimestampField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the insured's address (IN1.19).
    /// Address of the insured person.
    /// </summary>
    public AddressField InsuredsAddress
    {
        get => this[19] as AddressField ?? new AddressField();
        set => this[19] = value;
    }

    /// <summary>
    /// Gets or sets the assignment of benefits (IN1.20).
    /// Whether benefits are assigned to the provider.
    /// </summary>
    public CodedElementField AssignmentOfBenefits
    {
        get => this[20] as CodedElementField ?? new CodedElementField();
        set => this[20] = value;
    }

    /// <summary>
    /// Gets or sets the coordination of benefits (IN1.21).
    /// Coordination of benefits information.
    /// </summary>
    public CodedElementField CoordinationOfBenefits
    {
        get => this[21] as CodedElementField ?? new CodedElementField();
        set => this[21] = value;
    }

    /// <summary>
    /// Gets or sets the coordination of benefits priority (IN1.22).
    /// Priority of this insurance when multiple plans exist.
    /// </summary>
    public StringField CoordOfBenPriority
    {
        get => this[22] as StringField ?? new StringField();
        set => this[22] = value;
    }

    /// <summary>
    /// Gets or sets the notice of admission flag (IN1.23).
    /// Whether advance notice is required for admission.
    /// </summary>
    public IdentifierField NoticeOfAdmissionFlag
    {
        get => this[23] as IdentifierField ?? new IdentifierField();
        set => this[23] = value;
    }

    /// <summary>
    /// Gets or sets the notice of admission date (IN1.24).
    /// Date advance notice was given.
    /// </summary>
    public DateField NoticeOfAdmissionDate
    {
        get => this[24] as DateField ?? new DateField();
        set => this[24] = value;
    }

    /// <summary>
    /// Gets or sets the report of eligibility flag (IN1.25).
    /// Whether eligibility was verified.
    /// </summary>
    public IdentifierField ReportOfEligibilityFlag
    {
        get => this[25] as IdentifierField ?? new IdentifierField();
        set => this[25] = value;
    }

    /// <summary>
    /// Gets or sets the report of eligibility date (IN1.26).
    /// Date eligibility was verified.
    /// </summary>
    public DateField ReportOfEligibilityDate
    {
        get => this[26] as DateField ?? new DateField();
        set => this[26] = value;
    }

    /// <summary>
    /// Gets or sets the release information code (IN1.27).
    /// Authorization to release information.
    /// </summary>
    public CodedElementField ReleaseInformationCode
    {
        get => this[27] as CodedElementField ?? new CodedElementField();
        set => this[27] = value;
    }

    /// <summary>
    /// Gets or sets the pre-admit cert (PAC) (IN1.28).
    /// Pre-admission certification number.
    /// </summary>
    public StringField PreAdmitCert
    {
        get => this[28] as StringField ?? new StringField();
        set => this[28] = value;
    }

    /// <summary>
    /// Gets or sets the verification date/time (IN1.29).
    /// Date and time when insurance was verified.
    /// </summary>
    public TimestampField VerificationDateTime
    {
        get => this[29] as TimestampField ?? new TimestampField();
        set => this[29] = value;
    }

    /// <summary>
    /// Gets or sets the verification by (IN1.30).
    /// Person who verified the insurance.
    /// </summary>
    public ExtendedCompositeIdField VerificationBy
    {
        get => this[30] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[30] = value;
    }

    /// <summary>
    /// Gets or sets the type of agreement code (IN1.31).
    /// Type of agreement with the insurance company.
    /// </summary>
    public CodedElementField TypeOfAgreementCode
    {
        get => this[31] as CodedElementField ?? new CodedElementField();
        set => this[31] = value;
    }

    /// <summary>
    /// Gets or sets the billing status (IN1.32).
    /// Current billing status for this insurance.
    /// </summary>
    public CodedElementField BillingStatus
    {
        get => this[32] as CodedElementField ?? new CodedElementField();
        set => this[32] = value;
    }

    /// <summary>
    /// Gets or sets the lifetime reserve days (IN1.33).
    /// Number of lifetime reserve days remaining.
    /// </summary>
    public NumericField LifetimeReserveDays
    {
        get => this[33] as NumericField ?? new NumericField();
        set => this[33] = value;
    }

    /// <summary>
    /// Gets or sets the delay before lifetime reserve days (IN1.34).
    /// Delay period before lifetime reserve days begin.
    /// </summary>
    public NumericField DelayBeforeLifetimeReserveDays
    {
        get => this[34] as NumericField ?? new NumericField();
        set => this[34] = value;
    }

    /// <summary>
    /// Gets or sets the company plan code (IN1.35).
    /// Plan code used by the insurance company.
    /// </summary>
    public CodedElementField CompanyPlanCode
    {
        get => this[35] as CodedElementField ?? new CodedElementField();
        set => this[35] = value;
    }

    /// <summary>
    /// Gets or sets the policy number (IN1.36).
    /// Policy number for this insurance.
    /// </summary>
    public StringField PolicyNumber
    {
        get => this[36] as StringField ?? new StringField();
        set => this[36] = value;
    }

    /// <summary>
    /// Gets or sets the policy deductible (IN1.37).
    /// Deductible amount for the policy.
    /// </summary>
    public CompositeQuantityField PolicyDeductible
    {
        get => this[37] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[37] = value;
    }

    /// <summary>
    /// Gets or sets the policy limit amount (IN1.38).
    /// Maximum benefit amount for the policy.
    /// </summary>
    public CompositeQuantityField PolicyLimitAmount
    {
        get => this[38] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[38] = value;
    }

    /// <summary>
    /// Gets or sets the policy limit days (IN1.39).
    /// Maximum number of days covered by the policy.
    /// </summary>
    public NumericField PolicyLimitDays
    {
        get => this[39] as NumericField ?? new NumericField();
        set => this[39] = value;
    }

    /// <summary>
    /// Gets or sets the room rate semi-private (IN1.40).
    /// Semi-private room rate limit.
    /// </summary>
    public CompositeQuantityField RoomRateSemiPrivate
    {
        get => this[40] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[40] = value;
    }

    /// <summary>
    /// Gets or sets the room rate private (IN1.41).
    /// Private room rate limit.
    /// </summary>
    public CompositeQuantityField RoomRatePrivate
    {
        get => this[41] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[41] = value;
    }

    /// <summary>
    /// Gets or sets the insured's employment status (IN1.42).
    /// Employment status of the insured person.
    /// </summary>
    public CodedElementField InsuredsEmploymentStatus
    {
        get => this[42] as CodedElementField ?? new CodedElementField();
        set => this[42] = value;
    }

    /// <summary>
    /// Gets or sets the insured's administrative sex (IN1.43).
    /// Gender of the insured person.
    /// </summary>
    public CodedElementField InsuredsAdministrativeSex
    {
        get => this[43] as CodedElementField ?? new CodedElementField();
        set => this[43] = value;
    }

    /// <summary>
    /// Gets or sets the insured's employer's address (IN1.44).
    /// Address of the insured person's employer.
    /// </summary>
    public AddressField InsuredsEmployersAddress
    {
        get => this[44] as AddressField ?? new AddressField();
        set => this[44] = value;
    }

    /// <summary>
    /// Gets or sets the verification status (IN1.45).
    /// Status of insurance verification.
    /// </summary>
    public StringField VerificationStatus
    {
        get => this[45] as StringField ?? new StringField();
        set => this[45] = value;
    }

    /// <summary>
    /// Gets or sets the prior insurance plan ID (IN1.46).
    /// ID of previous insurance plan.
    /// </summary>
    public CodedElementField PriorInsurancePlanId
    {
        get => this[46] as CodedElementField ?? new CodedElementField();
        set => this[46] = value;
    }

    /// <summary>
    /// Gets or sets the coverage type (IN1.47).
    /// Type of coverage provided by this insurance.
    /// </summary>
    public CodedElementField CoverageType
    {
        get => this[47] as CodedElementField ?? new CodedElementField();
        set => this[47] = value;
    }

    /// <summary>
    /// Gets or sets the handicap (IN1.48).
    /// Handicap information for the insured.
    /// </summary>
    public CodedElementField Handicap
    {
        get => this[48] as CodedElementField ?? new CodedElementField();
        set => this[48] = value;
    }

    /// <summary>
    /// Gets or sets the insured's ID number (IN1.49).
    /// Member ID number for the insured.
    /// </summary>
    public ExtendedCompositeIdField InsuredsIdNumber
    {
        get => this[49] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[49] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // Initialize required fields with default values
        SetId.SetValue("1");
    }

    /// <summary>
    /// Sets basic insurance information.
    /// </summary>
    /// <param name="setId">Set ID for this insurance record.</param>
    /// <param name="planId">Insurance plan ID.</param>
    /// <param name="planName">Insurance plan name.</param>
    /// <param name="companyId">Insurance company ID.</param>
    /// <param name="companyName">Insurance company name.</param>
    /// <param name="memberId">Member/subscriber ID.</param>
    /// <param name="groupNumber">Group number.</param>
    /// <param name="effectiveDate">Plan effective date.</param>
    /// <param name="expirationDate">Plan expiration date.</param>
    public void SetBasicInsurance(
        int setId,
        string planId,
        string planName,
        string companyId,
        string companyName,
        string memberId,
        string? groupNumber = null,
        DateTime? effectiveDate = null,
        DateTime? expirationDate = null)
    {
        SetId.SetValue(setId.ToString());
        InsurancePlanId.SetComponents(planId, planName);
        InsuranceCompanyId.SetValue(companyId);
        InsuranceCompanyName.SetValue(companyName);
        InsuredsIdNumber.SetValue(memberId);

        if (!string.IsNullOrEmpty(groupNumber))
        {
            GroupNumber.SetValue(groupNumber);
        }

        if (effectiveDate.HasValue)
        {
            PlanEffectiveDate.SetValue(effectiveDate.Value.ToString("yyyyMMdd"));
        }

        if (expirationDate.HasValue)
        {
            PlanExpirationDate.SetValue(expirationDate.Value.ToString("yyyyMMdd"));
        }
    }

    /// <summary>
    /// Sets the insured person information.
    /// </summary>
    /// <param name="insuredName">Name of the insured person.</param>
    /// <param name="relationshipToPatient">Relationship to the patient.</param>
    /// <param name="insuredDateOfBirth">Date of birth of insured.</param>
    /// <param name="insuredGender">Gender of insured.</param>
    /// <param name="insuredAddress">Address of insured.</param>
    public void SetInsuredPersonInfo(
        PersonNameField insuredName,
        string relationshipToPatient,
        DateTime? insuredDateOfBirth = null,
        string? insuredGender = null,
        AddressField? insuredAddress = null)
    {
        NameOfInsured = insuredName;
        InsuredsRelationshipToPatient.SetComponents(relationshipToPatient, GetRelationshipDescription(relationshipToPatient), "HL70063");

        if (insuredDateOfBirth.HasValue)
        {
            InsuredsDateOfBirth.SetValue(insuredDateOfBirth.Value);
        }

        if (!string.IsNullOrEmpty(insuredGender))
        {
            InsuredsAdministrativeSex.SetComponents(insuredGender, GetGenderDescription(insuredGender), "HL70001");
        }

        if (insuredAddress != null)
        {
            InsuredsAddress = insuredAddress;
        }
    }

    /// <summary>
    /// Sets insurance company contact information.
    /// </summary>
    /// <param name="companyAddress">Company address.</param>
    /// <param name="companyPhone">Company phone number.</param>
    /// <param name="contactPerson">Contact person name.</param>
    public void SetInsuranceCompanyContact(
        AddressField? companyAddress = null,
        TelephoneField? companyPhone = null,
        PersonNameField? contactPerson = null)
    {
        if (companyAddress != null)
        {
            InsuranceCompanyAddress = companyAddress;
        }

        if (companyPhone != null)
        {
            InsuranceCoPhoneNumber = companyPhone;
        }

        if (contactPerson != null)
        {
            InsuranceCoContactPerson = contactPerson;
        }
    }

    /// <summary>
    /// Sets coverage details and limitations.
    /// </summary>
    /// <param name="planType">Type of plan (HMO, PPO, etc.).</param>
    /// <param name="deductibleAmount">Deductible amount.</param>
    /// <param name="policyLimitAmount">Policy maximum amount.</param>
    /// <param name="policyLimitDays">Policy maximum days.</param>
    /// <param name="copayAmount">Copayment amount.</param>
    public void SetCoverageDetails(
        string? planType = null,
        decimal? deductibleAmount = null,
        decimal? policyLimitAmount = null,
        int? policyLimitDays = null,
        decimal? copayAmount = null)
    {
        if (!string.IsNullOrEmpty(planType))
        {
            PlanType.SetComponents(planType, GetPlanTypeDescription(planType));
        }

        if (deductibleAmount.HasValue)
        {
            PolicyDeductible.SetComponents(deductibleAmount.Value, "USD");
        }

        if (policyLimitAmount.HasValue)
        {
            PolicyLimitAmount.SetComponents(policyLimitAmount.Value, "USD");
        }

        if (policyLimitDays.HasValue)
        {
            PolicyLimitDays.SetValue(policyLimitDays.Value.ToString());
        }
    }

    /// <summary>
    /// Sets verification information.
    /// </summary>
    /// <param name="verificationDate">Date insurance was verified.</param>
    /// <param name="verifiedBy">Person who verified insurance.</param>
    /// <param name="verificationStatus">Status of verification.</param>
    public void SetVerificationInfo(
        DateTime? verificationDate = null,
        string? verifiedBy = null,
        string? verificationStatus = null)
    {
        if (verificationDate.HasValue)
        {
            VerificationDateTime.SetValue(verificationDate.Value);
        }

        if (!string.IsNullOrEmpty(verifiedBy))
        {
            VerificationBy.SetValue(verifiedBy);
        }

        if (!string.IsNullOrEmpty(verificationStatus))
        {
            VerificationStatus.SetValue(verificationStatus);
        }
    }

    /// <summary>
    /// Gets a display-friendly representation of the insurance.
    /// </summary>
    /// <returns>Formatted insurance string.</returns>
    public string GetDisplayValue()
    {
        var planName = InsurancePlanId.Text ?? InsurancePlanId.Identifier ?? "Unknown Plan";
        var companyName = InsuranceCompanyName.RawValue ?? InsuranceCompanyId.RawValue ?? "Unknown Company";
        var memberId = InsuredsIdNumber.RawValue ?? "Unknown ID";

        return $"{companyName} - {planName} (Member: {memberId})";
    }

    /// <summary>
    /// Determines if this is the primary insurance.
    /// </summary>
    /// <returns>True if this is primary insurance (set ID = 1).</returns>
    public bool IsPrimary()
    {
        return SetId.Value == "1";
    }

    /// <summary>
    /// Gets the insurance priority level.
    /// </summary>
    /// <returns>Priority level based on set ID.</returns>
    public InsurancePriority GetPriority()
    {
        return SetId.Value switch
        {
            "1" => InsurancePriority.Primary,
            "2" => InsurancePriority.Secondary,
            "3" => InsurancePriority.Tertiary,
            _ => InsurancePriority.Other
        };
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(SetId.Value))
            errors.Add("Set ID (IN1.1) is required");

        if (string.IsNullOrEmpty(InsurancePlanId.Identifier) && string.IsNullOrEmpty(InsurancePlanId.Text))
            errors.Add("Insurance Plan ID (IN1.2) is required");

        if (string.IsNullOrEmpty(InsuranceCompanyId.RawValue))
            errors.Add("Insurance Company ID (IN1.3) is required");

        // Validate date ranges
        if (PlanEffectiveDate.ToDateTime().HasValue && PlanExpirationDate.ToDateTime().HasValue)
        {
            if (PlanExpirationDate.ToDateTime() < PlanEffectiveDate.ToDateTime())
            {
                errors.Add("Plan expiration date cannot be before effective date");
            }
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new IN1Segment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    private static string GetRelationshipDescription(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "SEL" => "Self",
            "SPS" => "Spouse",
            "CHD" => "Child",
            "PAR" => "Parent",
            "OTH" => "Other",
            _ => ""
        };
    }

    private static string GetGenderDescription(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "M" => "Male",
            "F" => "Female",
            "O" => "Other",
            "U" => "Unknown",
            _ => ""
        };
    }

    private static string GetPlanTypeDescription(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "HMO" => "Health Maintenance Organization",
            "PPO" => "Preferred Provider Organization",
            "POS" => "Point of Service",
            "EPO" => "Exclusive Provider Organization",
            "IND" => "Indemnity",
            _ => ""
        };
    }

    /// <summary>
    /// Creates a basic insurance IN1 segment.
    /// </summary>
    /// <param name="setId">Set ID (priority).</param>
    /// <param name="planName">Insurance plan name.</param>
    /// <param name="companyName">Insurance company name.</param>
    /// <param name="memberId">Member ID.</param>
    /// <param name="groupNumber">Group number.</param>
    /// <returns>Configured IN1 segment.</returns>
    public static IN1Segment CreateBasicInsurance(int setId, string planName, string companyName, string memberId, string? groupNumber = null)
    {
        var in1 = new IN1Segment();
        in1.SetBasicInsurance(setId, planName, planName, companyName, companyName, memberId, groupNumber);
        return in1;
    }
}

/// <summary>
/// Insurance priority levels.
/// </summary>
public enum InsurancePriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3,
    Other = 4
}
