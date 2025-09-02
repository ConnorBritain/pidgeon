// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// PV1 - Patient Visit Segment.
/// Contains information about the patient's visit/encounter including location, attending doctor, and visit details.
/// Essential for ADT messages to be complete.
/// </summary>
public class PV1Segment : HL7Segment
{
    public override string SegmentId => "PV1";
    public override string DisplayName => "Patient Visit";

    // Field accessors (1-based indexing to match HL7 standard)
    public StringField SetId => GetField<StringField>(1)!;
    public StringField PatientClass => GetField<StringField>(2)!;
    public StringField AssignedPatientLocation => GetField<StringField>(3)!;
    public StringField AdmissionType => GetField<StringField>(4)!;
    public StringField PreadmitNumber => GetField<StringField>(5)!;
    public StringField PriorPatientLocation => GetField<StringField>(6)!;
    public StringField AttendingDoctor => GetField<StringField>(7)!;
    public StringField ReferringDoctor => GetField<StringField>(8)!;
    public StringField ConsultingDoctor => GetField<StringField>(9)!;
    public StringField HospitalService => GetField<StringField>(10)!;
    public StringField TemporaryLocation => GetField<StringField>(11)!;
    public StringField PreadmitTestIndicator => GetField<StringField>(12)!;
    public StringField ReadmissionIndicator => GetField<StringField>(13)!;
    public StringField AdmitSource => GetField<StringField>(14)!;
    public StringField AmbulatoryStatus => GetField<StringField>(15)!;
    public StringField VipIndicator => GetField<StringField>(16)!;
    public StringField AdmittingDoctor => GetField<StringField>(17)!;
    public StringField PatientType => GetField<StringField>(18)!;
    public StringField VisitNumber => GetField<StringField>(19)!;
    public StringField FinancialClass => GetField<StringField>(20)!;
    public TimestampField AdmitDateTime => GetField<TimestampField>(44)!;
    public TimestampField DischargeDateTime => GetField<TimestampField>(45)!;

    public override void InitializeFields()
    {
        // PV1.1 - Set ID (Optional)
        AddField(StringField.Optional(4));

        // PV1.2 - Patient Class (Required) - I=Inpatient, O=Outpatient, E=Emergency, etc.
        AddField(StringField.Required(1));

        // PV1.3 - Assigned Patient Location (Optional) - Point of care^Room^Bed^Facility
        AddField(StringField.Optional(80));

        // PV1.4 - Admission Type (Optional) - A=Accident, E=Emergency, L=Labor, etc.
        AddField(StringField.Optional(2));

        // PV1.5 - Preadmit Number (Optional)
        AddField(StringField.Optional(250));

        // PV1.6 - Prior Patient Location (Optional)
        AddField(StringField.Optional(80));

        // PV1.7 - Attending Doctor (Optional) - ID^Last^First^Middle^Suffix^Prefix^Degree
        AddField(StringField.Optional(250));

        // PV1.8 - Referring Doctor (Optional)
        AddField(StringField.Optional(250));

        // PV1.9 - Consulting Doctor (Optional)
        AddField(StringField.Optional(250));

        // PV1.10 - Hospital Service (Optional)
        AddField(StringField.Optional(3));

        // PV1.11 - Temporary Location (Optional)
        AddField(StringField.Optional(80));

        // PV1.12 - Preadmit Test Indicator (Optional)
        AddField(StringField.Optional(2));

        // PV1.13 - Re-admission Indicator (Optional)
        AddField(StringField.Optional(2));

        // PV1.14 - Admit Source (Optional)
        AddField(StringField.Optional(6));

        // PV1.15 - Ambulatory Status (Optional)
        AddField(StringField.Optional(2));

        // PV1.16 - VIP Indicator (Optional)
        AddField(StringField.Optional(2));

        // PV1.17 - Admitting Doctor (Optional)
        AddField(StringField.Optional(250));

        // PV1.18 - Patient Type (Optional)
        AddField(StringField.Optional(2));

        // PV1.19 - Visit Number (Optional)
        AddField(StringField.Optional(250));

        // PV1.20 - Financial Class (Optional)
        AddField(StringField.Optional(50));

        // Add empty fields 21-43 to maintain proper field positioning
        for (int i = 21; i <= 43; i++)
        {
            AddField(StringField.Optional(250));
        }

        // PV1.44 - Admit Date/Time (Optional)
        AddField(new TimestampField());

        // PV1.45 - Discharge Date/Time (Optional)  
        AddField(new TimestampField());
    }

    /// <summary>
    /// Creates a PV1 segment for a patient encounter.
    /// </summary>
    /// <param name="encounter">Patient encounter/visit information</param>
    /// <param name="attendingProvider">Attending physician information</param>
    /// <returns>Configured PV1 segment</returns>
    public static PV1Segment Create(
        EncounterDto? encounter = null,
        ProviderDto? attendingProvider = null)
    {
        var pv1 = new PV1Segment();

        if (encounter != null)
        {
            // Set basic encounter information
            pv1.PatientClass.SetValue(MapEncounterTypeToPatientClass(encounter.Type));
            
            if (encounter.Location != null)
            {
                pv1.AssignedPatientLocation.SetValue(encounter.Location);
            }
            
            if (encounter.StartTime.HasValue)
            {
                pv1.AdmitDateTime.SetTypedValue(encounter.StartTime.Value);
            }
            
            if (encounter.EndTime.HasValue)
            {
                pv1.DischargeDateTime.SetTypedValue(encounter.EndTime.Value);
            }

            // Use the encounter's Provider (primary provider for the encounter)
            if (attendingProvider == null)
            {
                attendingProvider = encounter.Provider;
            }
        }

        if (attendingProvider != null)
        {
            // Format as ID^Last^First^Middle^Suffix^Prefix^Degree
            var doctorName = FormatProviderForHL7(attendingProvider);
            pv1.AttendingDoctor.SetValue(doctorName);
        }

        return pv1;
    }

    /// <summary>
    /// Maps encounter type to HL7 patient class codes.
    /// </summary>
    private static string MapEncounterTypeToPatientClass(EncounterTypeDto encounterType)
    {
        return encounterType switch
        {
            EncounterTypeDto.Inpatient => "I",
            EncounterTypeDto.Outpatient => "O", 
            EncounterTypeDto.Emergency => "E",
            EncounterTypeDto.Observation => "O",
            EncounterTypeDto.DaySurgery => "O",
            EncounterTypeDto.Telemedicine => "O",
            EncounterTypeDto.HomeHealth => "H",
            EncounterTypeDto.PreAdmission => "P",
            _ => "O" // Default to outpatient
        };
    }

    /// <summary>
    /// Formats provider information for HL7 XCN data type.
    /// Format: ID^Last^First^Middle^Suffix^Prefix^Degree
    /// </summary>
    private static string FormatProviderForHL7(ProviderDto provider)
    {
        var parts = new[]
        {
            provider.Id ?? "",
            provider.Name.LastName ?? "",
            provider.Name.FirstName ?? "",
            provider.Name.MiddleName ?? "",
            provider.Name.Suffix ?? "",
            provider.Name.Prefix ?? "",
            "" // Degree not in DTO
        };

        return string.Join("^", parts);
    }

    /// <summary>
    /// Sets the patient class with validation.
    /// </summary>
    /// <param name="patientClass">Patient class code (I, O, E, etc.)</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<PV1Segment> SetPatientClass(string patientClass)
    {
        var validClasses = new[] { "I", "O", "E", "P", "R", "B", "N", "U" };
        if (!validClasses.Contains(patientClass.ToUpper()))
        {
            return Result<PV1Segment>.Failure($"Invalid patient class: {patientClass}. Valid values are I, O, E, P, R, B, N, U");
        }

        var result = PatientClass.SetValue(patientClass.ToUpper());
        if (result.IsFailure)
            return Result<PV1Segment>.Failure("Failed to set patient class");

        return Result<PV1Segment>.Success(this);
    }

    /// <summary>
    /// Sets the assigned patient location.
    /// </summary>
    /// <param name="pointOfCare">Point of care (unit/ward)</param>
    /// <param name="room">Room number</param>
    /// <param name="bed">Bed number</param>
    /// <param name="facility">Facility identifier</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<PV1Segment> SetPatientLocation(
        string? pointOfCare = null,
        string? room = null,
        string? bed = null,
        string? facility = null)
    {
        var locationParts = new[]
        {
            pointOfCare ?? "",
            room ?? "",
            bed ?? "",
            facility ?? ""
        };

        var location = string.Join(ComponentSeparator, locationParts);
        var result = AssignedPatientLocation.SetValue(location);
        
        if (result.IsFailure)
            return Result<PV1Segment>.Failure("Failed to set patient location");

        return Result<PV1Segment>.Success(this);
    }

    public override string GetDisplayValue()
    {
        var patientClass = PatientClass.GetDisplayValue();
        var location = AssignedPatientLocation.GetDisplayValue();
        var attendingDoc = AttendingDoctor.GetDisplayValue();
        
        var displayParts = new List<string>();
        
        if (!string.IsNullOrEmpty(patientClass))
            displayParts.Add($"Class: {patientClass}");
            
        if (!string.IsNullOrEmpty(location))
            displayParts.Add($"Location: {location}");
            
        if (!string.IsNullOrEmpty(attendingDoc))
        {
            var docParts = attendingDoc.Split(ComponentSeparator);
            if (docParts.Length >= 3 && !string.IsNullOrEmpty(docParts[2]))
                displayParts.Add($"Attending: Dr. {docParts[2]} {docParts[1]}");
        }
        
        return $"PV1: {string.Join(", ", displayParts)}";
    }
    
    /// <summary>
    /// Validates the PV1 segment according to HL7 requirements.
    /// </summary>
    public override Result<HL7Segment> Validate()
    {
        // PV1 has no strictly required fields in HL7 v2.3
        return Result<HL7Segment>.Success(this);
    }
}