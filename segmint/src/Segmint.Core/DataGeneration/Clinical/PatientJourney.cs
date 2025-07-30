// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Segmint.Core.DataGeneration.Demographics;

namespace Segmint.Core.DataGeneration.Clinical;

/// <summary>
/// Represents a patient's healthcare journey with clinical events.
/// </summary>
public class PatientJourney
{
    /// <summary>
    /// Patient demographic information.
    /// </summary>
    public PatientDemographics Patient { get; set; } = new();

    /// <summary>
    /// List of clinical events in chronological order.
    /// </summary>
    public List<ClinicalEvent> Events { get; set; } = new();

    /// <summary>
    /// Journey start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Journey end date (optional).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Journey type/scenario.
    /// </summary>
    public string JourneyType { get; set; } = "";

    /// <summary>
    /// Primary diagnosis or condition.
    /// </summary>
    public string? PrimaryDiagnosis { get; set; }

    /// <summary>
    /// Healthcare facility where journey takes place.
    /// </summary>
    public FacilityInfo Facility { get; set; } = new();

    /// <summary>
    /// Insurance information for this journey.
    /// </summary>
    public InsuranceInfo? Insurance { get; set; }

    /// <summary>
    /// Gets the duration of the journey.
    /// </summary>
    public TimeSpan Duration => (EndDate ?? DateTime.Now) - StartDate;

    /// <summary>
    /// Adds an event to the journey.
    /// </summary>
    /// <param name="clinicalEvent">The clinical event to add.</param>
    public void AddEvent(ClinicalEvent clinicalEvent)
    {
        Events.Add(clinicalEvent);
        Events.Sort((e1, e2) => e1.EventDateTime.CompareTo(e2.EventDateTime));
    }

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <returns>Events of the specified type.</returns>
    public IEnumerable<ClinicalEvent> GetEventsByType(string eventType)
    {
        foreach (var evt in Events)
        {
            if (evt.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase))
                yield return evt;
        }
    }
}

/// <summary>
/// Represents a clinical event in a patient's journey.
/// </summary>
public class ClinicalEvent
{
    /// <summary>
    /// Event identifier.
    /// </summary>
    public string EventId { get; set; } = "";

    /// <summary>
    /// Event type (admission, discharge, transfer, etc.).
    /// </summary>
    public string EventType { get; set; } = "";

    /// <summary>
    /// ADT trigger event code (A01, A02, A03, etc.).
    /// </summary>
    public string TriggerEvent { get; set; } = "";

    /// <summary>
    /// Date and time when the event occurred.
    /// </summary>
    public DateTime EventDateTime { get; set; }

    /// <summary>
    /// Patient class (inpatient, outpatient, emergency, etc.).
    /// </summary>
    public string PatientClass { get; set; } = "";

    /// <summary>
    /// Patient location information.
    /// </summary>
    public PatientLocation Location { get; set; } = new();

    /// <summary>
    /// Attending physician information.
    /// </summary>
    public PhysicianInfo? AttendingPhysician { get; set; }

    /// <summary>
    /// Admission information (if applicable).
    /// </summary>
    public AdmissionInfo? Admission { get; set; }

    /// <summary>
    /// Discharge information (if applicable).
    /// </summary>
    public DischargeInfo? Discharge { get; set; }

    /// <summary>
    /// Visit number for this encounter.
    /// </summary>
    public string? VisitNumber { get; set; }

    /// <summary>
    /// Account number for billing.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Event notes or comments.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets a description of the event.
    /// </summary>
    public string Description => $"{EventType} - {TriggerEvent} at {EventDateTime:MM/dd/yyyy HH:mm}";
}

/// <summary>
/// Patient location information.
/// </summary>
public class PatientLocation
{
    /// <summary>
    /// Point of care (floor, unit).
    /// </summary>
    public string PointOfCare { get; set; } = "";

    /// <summary>
    /// Room number.
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// Bed identifier.
    /// </summary>
    public string? Bed { get; set; }

    /// <summary>
    /// Facility identifier.
    /// </summary>
    public string? Facility { get; set; }

    /// <summary>
    /// Location status (active, inactive, temporarily inactive).
    /// </summary>
    public string? LocationStatus { get; set; }

    /// <summary>
    /// Person location type (nursing unit, department).
    /// </summary>
    public string? PersonLocationType { get; set; }

    /// <summary>
    /// Building identifier.
    /// </summary>
    public string? Building { get; set; }

    /// <summary>
    /// Floor identifier.
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// Gets the formatted location string.
    /// </summary>
    public string FormattedLocation
    {
        get
        {
            var location = PointOfCare;
            if (!string.IsNullOrEmpty(Room))
                location += $"^{Room}";
            if (!string.IsNullOrEmpty(Bed))
                location += $"^{Bed}";
            return location;
        }
    }
}

/// <summary>
/// Physician information.
/// </summary>
public class PhysicianInfo
{
    /// <summary>
    /// Physician identifier.
    /// </summary>
    public string PhysicianId { get; set; } = "";

    /// <summary>
    /// Physician's name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Medical specialty.
    /// </summary>
    public string Specialty { get; set; } = "";

    /// <summary>
    /// Department or service.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Pager number.
    /// </summary>
    public string? PagerNumber { get; set; }
}

/// <summary>
/// Admission information.
/// </summary>
public class AdmissionInfo
{
    /// <summary>
    /// Admission type (elective, emergency, urgent, etc.).
    /// </summary>
    public string AdmissionType { get; set; } = "";

    /// <summary>
    /// Admission source (physician referral, emergency room, etc.).
    /// </summary>
    public string? AdmissionSource { get; set; }

    /// <summary>
    /// Pre-admission number.
    /// </summary>
    public string? PreAdmissionNumber { get; set; }

    /// <summary>
    /// Admitting physician.
    /// </summary>
    public PhysicianInfo? AdmittingPhysician { get; set; }

    /// <summary>
    /// Referring physician.
    /// </summary>
    public PhysicianInfo? ReferringPhysician { get; set; }

    /// <summary>
    /// Hospital service.
    /// </summary>
    public string? HospitalService { get; set; }

    /// <summary>
    /// Admit reason.
    /// </summary>
    public string? AdmitReason { get; set; }
}

/// <summary>
/// Discharge information.
/// </summary>
public class DischargeInfo
{
    /// <summary>
    /// Discharge disposition (home, skilled nursing facility, etc.).
    /// </summary>
    public string DischargeDisposition { get; set; } = "";

    /// <summary>
    /// Discharge location.
    /// </summary>
    public string? DischargeLocation { get; set; }

    /// <summary>
    /// Discharge to location type.
    /// </summary>
    public string? DischargeToLocationType { get; set; }

    /// <summary>
    /// Discharging physician.
    /// </summary>
    public PhysicianInfo? DischargingPhysician { get; set; }

    /// <summary>
    /// Discharge instructions.
    /// </summary>
    public string? DischargeInstructions { get; set; }

    /// <summary>
    /// Follow-up instructions.
    /// </summary>
    public string? FollowUpInstructions { get; set; }

    /// <summary>
    /// Length of stay in days.
    /// </summary>
    public int? LengthOfStay { get; set; }
}

/// <summary>
/// Healthcare facility information.
/// </summary>
public class FacilityInfo
{
    /// <summary>
    /// Facility identifier.
    /// </summary>
    public string FacilityId { get; set; } = "";

    /// <summary>
    /// Facility name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Facility type (hospital, clinic, etc.).
    /// </summary>
    public string FacilityType { get; set; } = "";

    /// <summary>
    /// Address information.
    /// </summary>
    public AddressInfo Address { get; set; } = new();

    /// <summary>
    /// Phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>
    /// Organization identifier.
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Number of beds.
    /// </summary>
    public int? BedCount { get; set; }

    /// <summary>
    /// Available services.
    /// </summary>
    public List<string> Services { get; set; } = new();
}

/// <summary>
/// Predefined journey types for common healthcare scenarios.
/// </summary>
public static class JourneyTypes
{
    public const string EmergencyAdmission = "Emergency Admission";
    public const string ElectiveSurgery = "Elective Surgery";
    public const string OutpatientVisit = "Outpatient Visit";
    public const string EmergencyDepartmentVisit = "Emergency Department Visit";
    public const string ObservationStay = "Observation Stay";
    public const string NewbornAdmission = "Newborn Admission";
    public const string PsychiatricAdmission = "Psychiatric Admission";
    public const string Readmission = "Readmission";
    public const string Transfer = "Inter-facility Transfer";
    public const string PreAdmissionTesting = "Pre-admission Testing";

    /// <summary>
    /// Gets all available journey types.
    /// </summary>
    /// <returns>Array of journey type names.</returns>
    public static string[] GetAll()
    {
        return new[]
        {
            EmergencyAdmission, ElectiveSurgery, OutpatientVisit, EmergencyDepartmentVisit,
            ObservationStay, NewbornAdmission, PsychiatricAdmission, Readmission,
            Transfer, PreAdmissionTesting
        };
    }
}