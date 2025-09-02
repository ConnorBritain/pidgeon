// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// PID - Patient Identification Segment
/// Contains patient demographic and identification information.
/// This is one of the most critical segments in HL7 messaging.
/// </summary>
public class PIDSegment : HL7Segment
{
    /// <summary>
    /// Gets the segment ID for PID segments.
    /// </summary>
    public override string SegmentId => "PID";

    public PIDSegment() : base()
    {
    }

    /// <summary>
    /// PID.1 - Set ID - Patient ID (SI)
    /// Sequence number for multiple PID segments.
    /// </summary>
    public NumericField SetId { get; set; } = new();

    /// <summary>
    /// PID.2 - Patient ID (External ID) (CK)
    /// External patient identifier (often not used in modern implementations).
    /// </summary>
    public StringField ExternalPatientId { get; set; } = new();

    /// <summary>
    /// PID.3 - Patient Identifier List (CX)
    /// Primary patient identifier (MRN, etc.). Most important field.
    /// </summary>
    public StringField PatientId { get; set; } = new();

    /// <summary>
    /// PID.4 - Alternate Patient ID (CX)
    /// Alternative patient identifiers.
    /// </summary>
    public StringField AlternatePatientId { get; set; } = new();

    /// <summary>
    /// PID.5 - Patient Name (XPN)
    /// Patient's legal name.
    /// </summary>
    public PersonNameField PatientName { get; set; } = new();

    /// <summary>
    /// PID.6 - Mother's Maiden Name (XPN)
    /// Mother's maiden name for identification purposes.
    /// </summary>
    public PersonNameField MothersMaidenName { get; set; } = new();

    /// <summary>
    /// PID.7 - Date/Time of Birth (TS)
    /// Patient's birth date and time.
    /// </summary>
    public DateField DateOfBirth { get; set; } = new();

    /// <summary>
    /// PID.8 - Sex (IS)
    /// Patient's gender/sex (M, F, O, U).
    /// </summary>
    public StringField Sex { get; set; } = new();

    /// <summary>
    /// PID.9 - Patient Alias (XPN)
    /// Known aliases or previous names.
    /// </summary>
    public PersonNameField PatientAlias { get; set; } = new();

    /// <summary>
    /// PID.10 - Race (CE)
    /// Patient's race information.
    /// </summary>
    public StringField Race { get; set; } = new();

    /// <summary>
    /// PID.11 - Patient Address (XAD)
    /// Patient's home address.
    /// </summary>
    public AddressField PatientAddress { get; set; } = new();

    /// <summary>
    /// PID.12 - County Code (IS)
    /// County code for patient's address.
    /// </summary>
    public StringField CountyCode { get; set; } = new();

    /// <summary>
    /// PID.13 - Phone Number - Home (XTN)
    /// Patient's home phone number.
    /// </summary>
    public TelephoneField HomePhone { get; set; } = new();

    /// <summary>
    /// PID.14 - Phone Number - Business (XTN)
    /// Patient's business/work phone number.
    /// </summary>
    public TelephoneField BusinessPhone { get; set; } = new();

    /// <summary>
    /// PID.15 - Primary Language (CE)
    /// Patient's primary spoken language.
    /// </summary>
    public StringField PrimaryLanguage { get; set; } = new();

    /// <summary>
    /// PID.16 - Marital Status (CE)
    /// Patient's marital status (S, M, D, W, etc.).
    /// </summary>
    public StringField MaritalStatus { get; set; } = new();

    /// <summary>
    /// PID.17 - Religion (CE)
    /// Patient's religious affiliation.
    /// </summary>
    public StringField Religion { get; set; } = new();

    /// <summary>
    /// PID.18 - Patient Account Number (CX)
    /// Financial account number for billing.
    /// </summary>
    public StringField AccountNumber { get; set; } = new();

    /// <summary>
    /// PID.19 - SSN Number - Patient (ST)
    /// Social Security Number (handle with extreme care for HIPAA compliance).
    /// </summary>
    public StringField SocialSecurityNumber { get; set; } = new();

    /// <summary>
    /// PID.20 - Driver's License Number (DLN)
    /// Patient's driver's license information.
    /// </summary>
    public StringField DriversLicenseNumber { get; set; } = new();

    /// <summary>
    /// PID.21 - Mother's Identifier (CX)
    /// Identifier for patient's mother.
    /// </summary>
    public StringField MothersIdentifier { get; set; } = new();

    /// <summary>
    /// PID.22 - Ethnic Group (CE)
    /// Patient's ethnic group information.
    /// </summary>
    public StringField EthnicGroup { get; set; } = new();

    public override void InitializeFields()
    {
        AddField(SetId);                      // PID.1
        AddField(ExternalPatientId);          // PID.2
        AddField(PatientId);                  // PID.3
        AddField(AlternatePatientId);         // PID.4
        AddField(PatientName);                // PID.5
        AddField(MothersMaidenName);          // PID.6
        AddField(DateOfBirth);                // PID.7
        AddField(Sex);                        // PID.8
        AddField(PatientAlias);               // PID.9
        AddField(Race);                       // PID.10
        AddField(PatientAddress);             // PID.11
        AddField(CountyCode);                 // PID.12
        AddField(HomePhone);                  // PID.13
        AddField(BusinessPhone);              // PID.14
        AddField(PrimaryLanguage);            // PID.15
        AddField(MaritalStatus);              // PID.16
        AddField(Religion);                   // PID.17
        AddField(AccountNumber);              // PID.18
        AddField(SocialSecurityNumber);       // PID.19
        AddField(DriversLicenseNumber);       // PID.20
        AddField(MothersIdentifier);          // PID.21
        AddField(EthnicGroup);                // PID.22
    }

    /// <summary>
    /// Populates the PID segment from patient data.
    /// </summary>
    /// <param name="patient">The patient data</param>
    /// <returns>A result indicating success or validation errors</returns>
    public Result<PIDSegment> PopulateFromPatient(PatientDto patient)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(patient.Id) && string.IsNullOrEmpty(patient.MedicalRecordNumber))
                return Result<PIDSegment>.Failure("Patient must have ID or MRN");

            // Set sequence number (typically 1 for single patient)
            SetId.SetTypedValue(1);

            // PID.3 - Primary patient identifier (most important)
            if (!string.IsNullOrEmpty(patient.MedicalRecordNumber))
            {
                PatientId.SetValue(patient.MedicalRecordNumber);
            }
            else
            {
                PatientId.SetValue(patient.Id);
            }

            // PID.5 - Patient name (required)
            PatientName.SetTypedValue(patient.Name);

            // PID.7 - Date of birth
            if (patient.BirthDate.HasValue)
            {
                DateOfBirth.SetTypedValue(patient.BirthDate.Value);
            }

            // PID.8 - Sex/Gender
            if (patient.Gender.HasValue)
            {
                var genderCode = patient.Gender.Value switch
                {
                    GenderDto.Male => "M",
                    GenderDto.Female => "F",
                    GenderDto.Other => "O",
                    GenderDto.Unknown => "U",
                    _ => "U"
                };
                Sex.SetValue(genderCode);
            }

            // PID.10 - Race
            if (!string.IsNullOrEmpty(patient.Race))
            {
                Race.SetValue(patient.Race);
            }

            // PID.11 - Address
            if (patient.Address != null)
            {
                PatientAddress.SetTypedValue(patient.Address);
            }

            // PID.13 - Home phone
            if (!string.IsNullOrEmpty(patient.PhoneNumber))
            {
                HomePhone.SetValue(patient.PhoneNumber);
            }

            // PID.15 - Primary language
            if (!string.IsNullOrEmpty(patient.PrimaryLanguage))
            {
                PrimaryLanguage.SetValue(patient.PrimaryLanguage);
            }

            // PID.16 - Marital status
            if (patient.MaritalStatus.HasValue)
            {
                var maritalCode = patient.MaritalStatus.Value switch
                {
                    MaritalStatusDto.Single => "S",
                    MaritalStatusDto.Married => "M",
                    MaritalStatusDto.Divorced => "D",
                    MaritalStatusDto.Widowed => "W",
                    MaritalStatusDto.Separated => "A",
                    MaritalStatusDto.Unknown => "U",
                    _ => "U"
                };
                MaritalStatus.SetValue(maritalCode);
            }

            // PID.19 - SSN (handle with care for HIPAA)
            if (!string.IsNullOrEmpty(patient.SocialSecurityNumber))
            {
                SocialSecurityNumber.SetValue(patient.SocialSecurityNumber);
            }

            // PID.22 - Ethnic group
            if (!string.IsNullOrEmpty(patient.Ethnicity))
            {
                EthnicGroup.SetValue(patient.Ethnicity);
            }

            return Result<PIDSegment>.Success(this);
        }
        catch (Exception ex)
        {
            return Result<PIDSegment>.Failure($"Failed to populate PID segment from patient: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a PID segment from patient data.
    /// </summary>
    /// <param name="patient">The patient data</param>
    /// <returns>A result containing the populated PID segment or an error</returns>
    public static Result<PIDSegment> FromPatient(PatientDto patient)
    {
        var pidSegment = new PIDSegment();
        return pidSegment.PopulateFromPatient(patient);
    }

    /// <summary>
    /// Validates the PID segment according to HL7 requirements.
    /// </summary>
    /// <returns>A result indicating whether the segment is valid</returns>
    public override Result<HL7Segment> Validate()
    {
        var errors = new List<string>();

        // PID.3 (Patient ID) is required
        if (PatientId.IsEmpty)
            errors.Add("PID.3 Patient ID is required");

        // PID.5 (Patient Name) is required
        if (PatientName.TypedValue?.IsEmpty() != false)
            errors.Add("PID.5 Patient Name is required");

        // Additional validation rules can be added here

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return Result<HL7Segment>.Failure($"PID segment validation failed: {errorMessage}");
        }

        return Result<HL7Segment>.Success(this);
    }

    /// <summary>
    /// Gets a human-readable display of key patient information.
    /// </summary>
    /// <returns>Patient display information</returns>
    public string GetPatientDisplay()
    {
        var parts = new List<string>();

        if (!PatientName.IsEmpty && PatientName.TypedValue != null)
            parts.Add($"Name: {PatientName.TypedValue.DisplayName}");

        if (!PatientId.IsEmpty)
            parts.Add($"ID: {PatientId.Value}");

        if (!DateOfBirth.IsEmpty)
            parts.Add($"DOB: {DateOfBirth.Value}");

        return parts.Any() ? string.Join(" | ", parts) : "Unknown Patient";
    }
}