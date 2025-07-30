// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using Segmint.Core.DataGeneration.Demographics;

namespace Segmint.Core.DataGeneration.Clinical;

/// <summary>
/// Generates realistic patient healthcare journeys for ADT message testing.
/// </summary>
public class PatientJourneyGenerator : IDataGenerator<PatientJourney>
{
    private readonly Random _random;
    private readonly DemographicsGenerator _demographicsGenerator;
    private readonly ClinicalDataSets _dataSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatientJourneyGenerator"/> class.
    /// </summary>
    /// <param name="seed">Random seed for reproducible generation.</param>
    public PatientJourneyGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _demographicsGenerator = new DemographicsGenerator(seed);
        _dataSets = new ClinicalDataSets();
    }

    /// <inheritdoc />
    public PatientJourney Generate()
    {
        return Generate(new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public IEnumerable<PatientJourney> Generate(int count)
    {
        return Generate(count, new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public PatientJourney Generate(DataGenerationConstraints constraints)
    {
        var journeyType = SelectJourneyType(constraints);
        return GenerateJourney(journeyType, constraints);
    }

    /// <inheritdoc />
    public IEnumerable<PatientJourney> Generate(int count, DataGenerationConstraints constraints)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Generate(constraints);
        }
    }

    /// <summary>
    /// Generates a specific type of patient journey.
    /// </summary>
    /// <param name="journeyType">The type of journey to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Generated patient journey.</returns>
    public PatientJourney GenerateJourney(string journeyType, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();
        
        var patient = _demographicsGenerator.Generate(constraints);
        var facility = GenerateFacility();
        var startDate = GenerateStartDate(constraints.DateRange);

        var journey = new PatientJourney
        {
            Patient = patient,
            StartDate = startDate,
            JourneyType = journeyType,
            Facility = facility,
            Insurance = GenerateInsurance(patient)
        };

        GenerateEventsForJourneyType(journey, journeyType);

        return journey;
    }

    private string SelectJourneyType(DataGenerationConstraints constraints)
    {
        // Check if specific journey type is requested
        if (constraints.CustomConstraints.TryGetValue("JourneyType", out var journeyTypeObj) 
            && journeyTypeObj is string journeyType)
        {
            return journeyType;
        }

        // Weight journey types by frequency
        var weightedTypes = new[]
        {
            (JourneyTypes.OutpatientVisit, 40),
            (JourneyTypes.EmergencyDepartmentVisit, 20),
            (JourneyTypes.EmergencyAdmission, 15),
            (JourneyTypes.ElectiveSurgery, 10),
            (JourneyTypes.ObservationStay, 8),
            (JourneyTypes.Readmission, 3),
            (JourneyTypes.Transfer, 2),
            (JourneyTypes.NewbornAdmission, 1),
            (JourneyTypes.PsychiatricAdmission, 1)
        };

        var totalWeight = weightedTypes.Sum(w => w.Item2);
        var randomValue = _random.Next(totalWeight);
        var currentWeight = 0;

        foreach (var (type, weight) in weightedTypes)
        {
            currentWeight += weight;
            if (randomValue < currentWeight)
                return type;
        }

        return JourneyTypes.OutpatientVisit;
    }

    private void GenerateEventsForJourneyType(PatientJourney journey, string journeyType)
    {
        switch (journeyType)
        {
            case JourneyTypes.EmergencyAdmission:
                GenerateEmergencyAdmissionJourney(journey);
                break;
            case JourneyTypes.ElectiveSurgery:
                GenerateElectiveSurgeryJourney(journey);
                break;
            case JourneyTypes.OutpatientVisit:
                GenerateOutpatientVisitJourney(journey);
                break;
            case JourneyTypes.EmergencyDepartmentVisit:
                GenerateEmergencyDepartmentJourney(journey);
                break;
            case JourneyTypes.ObservationStay:
                GenerateObservationStayJourney(journey);
                break;
            case JourneyTypes.NewbornAdmission:
                GenerateNewbornAdmissionJourney(journey);
                break;
            case JourneyTypes.Readmission:
                GenerateReadmissionJourney(journey);
                break;
            default:
                GenerateBasicJourney(journey);
                break;
        }
    }

    private void GenerateEmergencyAdmissionJourney(PatientJourney journey)
    {
        var admissionTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Registration/Pre-admission
        var registration = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Registration",
            TriggerEvent = "A04",
            EventDateTime = admissionTime.AddMinutes(-30),
            PatientClass = "E", // Emergency
            Location = GenerateEmergencyLocation(),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber,
            Notes = "Emergency registration"
        };
        journey.AddEvent(registration);

        // Emergency admission
        var admission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Admit Patient",
            TriggerEvent = "A01",
            EventDateTime = admissionTime,
            PatientClass = "I", // Inpatient
            Location = GenerateInpatientLocation(),
            AttendingPhysician = GeneratePhysician("Emergency Medicine"),
            Admission = GenerateAdmissionInfo("emergency"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(admission);

        // Possible transfer
        if (_random.NextDouble() < 0.3)
        {
            var transferTime = admissionTime.AddHours(_random.Next(2, 24));
            var transfer = new ClinicalEvent
            {
                EventId = GenerateEventId(),
                EventType = "Transfer Patient",
                TriggerEvent = "A02",
                EventDateTime = transferTime,
                PatientClass = "I",
                Location = GenerateInpatientLocation(),
                AttendingPhysician = GeneratePhysician(),
                VisitNumber = visitNumber,
                AccountNumber = accountNumber,
                Notes = "Transfer to appropriate unit"
            };
            journey.AddEvent(transfer);
        }

        // Discharge
        var lengthOfStay = _random.Next(1, 14);
        var dischargeTime = admissionTime.AddDays(lengthOfStay);
        var discharge = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Discharge Patient",
            TriggerEvent = "A03",
            EventDateTime = dischargeTime,
            PatientClass = "I",
            Location = GenerateInpatientLocation(),
            Discharge = GenerateDischargeInfo(lengthOfStay),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(discharge);

        journey.EndDate = dischargeTime;
        journey.PrimaryDiagnosis = GenerateDiagnosis("emergency");
    }

    private void GenerateElectiveSurgeryJourney(PatientJourney journey)
    {
        var surgeryDate = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Pre-admission
        var preAdmission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Pre-admit Patient",
            TriggerEvent = "A05",
            EventDateTime = surgeryDate.AddDays(-7),
            PatientClass = "P", // Pre-admit
            Location = GenerateOutpatientLocation(),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber,
            Notes = "Pre-operative testing and clearance"
        };
        journey.AddEvent(preAdmission);

        // Admission for surgery
        var admission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Admit Patient",
            TriggerEvent = "A01",
            EventDateTime = surgeryDate.AddHours(6), // Early morning admission
            PatientClass = "I",
            Location = GenerateInpatientLocation(),
            AttendingPhysician = GeneratePhysician("Surgery"),
            Admission = GenerateAdmissionInfo("elective"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(admission);

        // Post-operative discharge
        var dischargeTime = surgeryDate.AddDays(_random.Next(1, 4));
        var discharge = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Discharge Patient",
            TriggerEvent = "A03",
            EventDateTime = dischargeTime,
            PatientClass = "I",
            Discharge = GenerateDischargeInfo((int)(dischargeTime - surgeryDate).TotalDays),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(discharge);

        journey.EndDate = dischargeTime;
        journey.PrimaryDiagnosis = GenerateDiagnosis("surgical");
    }

    private void GenerateOutpatientVisitJourney(PatientJourney journey)
    {
        var visitTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Registration
        var registration = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Register Patient",
            TriggerEvent = "A04",
            EventDateTime = visitTime.AddMinutes(-15),
            PatientClass = "O", // Outpatient
            Location = GenerateOutpatientLocation(),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(registration);

        // Update patient info (if needed)
        if (_random.NextDouble() < 0.2)
        {
            var update = new ClinicalEvent
            {
                EventId = GenerateEventId(),
                EventType = "Update Patient Information",
                TriggerEvent = "A08",
                EventDateTime = visitTime,
                PatientClass = "O",
                Location = GenerateOutpatientLocation(),
                VisitNumber = visitNumber,
                AccountNumber = accountNumber,
                Notes = "Updated demographics or insurance"
            };
            journey.AddEvent(update);
        }

        journey.EndDate = visitTime.AddHours(2);
        journey.PrimaryDiagnosis = GenerateDiagnosis("outpatient");
    }

    private void GenerateEmergencyDepartmentJourney(PatientJourney journey)
    {
        var arrivalTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // ED arrival
        var arrival = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Register Patient",
            TriggerEvent = "A04",
            EventDateTime = arrivalTime,
            PatientClass = "E",
            Location = GenerateEmergencyLocation(),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(arrival);

        // Treatment and observation
        var treatmentTime = arrivalTime.AddHours(_random.Next(2, 8));
        
        // Discharge home (80% chance) or admit (20% chance)
        if (_random.NextDouble() < 0.8)
        {
            var discharge = new ClinicalEvent
            {
                EventId = GenerateEventId(),
                EventType = "Discharge Patient",
                TriggerEvent = "A03",
                EventDateTime = treatmentTime,
                PatientClass = "E",
                Location = GenerateEmergencyLocation(),
                Discharge = GenerateDischargeInfo(0, "home"),
                VisitNumber = visitNumber,
                AccountNumber = accountNumber
            };
            journey.AddEvent(discharge);
            journey.EndDate = treatmentTime;
        }
        else
        {
            // Convert to inpatient
            var admission = new ClinicalEvent
            {
                EventId = GenerateEventId(),
                EventType = "Change Outpatient to Inpatient",
                TriggerEvent = "A06",
                EventDateTime = treatmentTime,
                PatientClass = "I",
                Location = GenerateInpatientLocation(),
                VisitNumber = visitNumber,
                AccountNumber = accountNumber
            };
            journey.AddEvent(admission);
        }

        journey.PrimaryDiagnosis = GenerateDiagnosis("emergency");
    }

    private void GenerateObservationStayJourney(PatientJourney journey)
    {
        var admissionTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Observation admission
        var admission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Admit Patient",
            TriggerEvent = "A01",
            EventDateTime = admissionTime,
            PatientClass = "O", // Observation
            Location = GenerateObservationLocation(),
            AttendingPhysician = GeneratePhysician(),
            Admission = GenerateAdmissionInfo("observation"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(admission);

        // Discharge after 1-2 days
        var dischargeTime = admissionTime.AddDays(_random.Next(1, 3));
        var discharge = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Discharge Patient",
            TriggerEvent = "A03",
            EventDateTime = dischargeTime,
            PatientClass = "O",
            Discharge = GenerateDischargeInfo((int)(dischargeTime - admissionTime).TotalDays, "home"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(discharge);

        journey.EndDate = dischargeTime;
        journey.PrimaryDiagnosis = GenerateDiagnosis("observation");
    }

    private void GenerateNewbornAdmissionJourney(PatientJourney journey)
    {
        // Adjust patient age for newborn
        journey.Patient.DateOfBirth = journey.StartDate.Date;
        
        var birthTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Birth/admission
        var admission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Admit Patient",
            TriggerEvent = "A01",
            EventDateTime = birthTime,
            PatientClass = "I",
            Location = GenerateNewbornLocation(),
            AttendingPhysician = GeneratePhysician("Pediatrics"),
            Admission = GenerateAdmissionInfo("newborn"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(admission);

        // Discharge with mother
        var dischargeTime = birthTime.AddDays(_random.Next(1, 4));
        var discharge = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Discharge Patient",
            TriggerEvent = "A03",
            EventDateTime = dischargeTime,
            PatientClass = "I",
            Discharge = GenerateDischargeInfo((int)(dischargeTime - birthTime).TotalDays, "home"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(discharge);

        journey.EndDate = dischargeTime;
        journey.PrimaryDiagnosis = "Normal newborn";
    }

    private void GenerateReadmissionJourney(PatientJourney journey)
    {
        // This would typically follow a previous admission
        var admissionTime = journey.StartDate;
        var visitNumber = GenerateVisitNumber();
        var accountNumber = GenerateAccountNumber();

        // Readmission
        var admission = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Admit Patient",
            TriggerEvent = "A01",
            EventDateTime = admissionTime,
            PatientClass = "I",
            Location = GenerateInpatientLocation(),
            AttendingPhysician = GeneratePhysician(),
            Admission = GenerateAdmissionInfo("emergency"),
            VisitNumber = visitNumber,
            AccountNumber = accountNumber,
            Notes = "30-day readmission"
        };
        journey.AddEvent(admission);

        // Extended stay due to complications
        var lengthOfStay = _random.Next(3, 21);
        var dischargeTime = admissionTime.AddDays(lengthOfStay);
        var discharge = new ClinicalEvent
        {
            EventId = GenerateEventId(),
            EventType = "Discharge Patient",
            TriggerEvent = "A03",
            EventDateTime = dischargeTime,
            PatientClass = "I",
            Discharge = GenerateDischargeInfo(lengthOfStay, "snf"), // Skilled nursing facility
            VisitNumber = visitNumber,
            AccountNumber = accountNumber
        };
        journey.AddEvent(discharge);

        journey.EndDate = dischargeTime;
        journey.PrimaryDiagnosis = GenerateDiagnosis("readmission");
    }

    private void GenerateBasicJourney(PatientJourney journey)
    {
        GenerateOutpatientVisitJourney(journey);
    }

    // Helper methods for generating various components

    private DateTime GenerateStartDate(DateRange? range)
    {
        var defaultRange = new DateRange(DateTime.Now.AddDays(-90), DateTime.Now.AddDays(30));
        var dateRange = range ?? defaultRange;
        
        var days = (dateRange.End - dateRange.Start).Days;
        var randomDays = _random.Next(days + 1);
        var baseDate = dateRange.Start.AddDays(randomDays);
        
        // Add random time
        var hour = _random.Next(0, 24);
        var minute = _random.Next(0, 60);
        
        return baseDate.Date.AddHours(hour).AddMinutes(minute);
    }

    private FacilityInfo GenerateFacility()
    {
        var facilityNames = new[]
        {
            "General Hospital", "Medical Center", "Regional Medical Center", "University Hospital",
            "Community Hospital", "Memorial Hospital", "Saint Mary's Hospital", "City Hospital"
        };

        var facilityName = facilityNames[_random.Next(facilityNames.Length)];
        
        return new FacilityInfo
        {
            FacilityId = $"FAC{_random.Next(1000, 9999)}",
            Name = facilityName,
            FacilityType = "Hospital",
            Address = new AddressInfo
            {
                Street = $"{_random.Next(100, 9999)} Medical Center Dr",
                City = "Healthcare City",
                State = "HS",
                PostalCode = $"{_random.Next(10000, 99999):D5}",
                Country = "USA"
            },
            PhoneNumber = $"({_random.Next(200, 999):D3}) {_random.Next(200, 999):D3}-{_random.Next(1000, 9999):D4}",
            BedCount = _random.Next(100, 500),
            Services = new List<string> { "Emergency", "Surgery", "ICU", "Cardiology", "Orthopedics" }
        };
    }

    private InsuranceInfo GenerateInsurance(PatientDemographics patient)
    {
        var insuranceCompanies = new[]
        {
            "Blue Cross Blue Shield", "Aetna", "Cigna", "UnitedHealthcare", "Humana",
            "Anthem", "Kaiser Permanente", "Medicare", "Medicaid"
        };

        var company = insuranceCompanies[_random.Next(insuranceCompanies.Length)];
        
        return new InsuranceInfo
        {
            PlanName = $"{company} Health Plan",
            MemberId = $"{patient.PatientId.Replace("MRN", "INS")}",
            GroupNumber = $"GRP{_random.Next(100000, 999999)}",
            PolicyHolderName = patient.FullName,
            RelationshipToHolder = "Self",
            Company = new InsuranceCompany
            {
                Name = company,
                Id = $"IC{_random.Next(1000, 9999)}",
                PhoneNumber = $"1-800-{_random.Next(100, 999):D3}-{_random.Next(1000, 9999):D4}"
            }
        };
    }

    private string GenerateEventId()
    {
        return $"EVT{_random.Next(100000, 999999)}";
    }

    private string GenerateVisitNumber()
    {
        return $"V{_random.Next(10000000, 99999999)}";
    }

    private string GenerateAccountNumber()
    {
        return $"A{_random.Next(10000000, 99999999)}";
    }

    private PatientLocation GenerateInpatientLocation()
    {
        var floors = new[] { "2", "3", "4", "5", "6", "7", "ICU", "CCU" };
        var floor = floors[_random.Next(floors.Length)];
        var room = _random.Next(101, 999).ToString();
        var bed = new[] { "A", "B", "C", "D" }[_random.Next(4)];

        return new PatientLocation
        {
            PointOfCare = floor,
            Room = room,
            Bed = bed,
            Facility = "MAIN",
            LocationStatus = "A",
            PersonLocationType = "N"
        };
    }

    private PatientLocation GenerateEmergencyLocation()
    {
        return new PatientLocation
        {
            PointOfCare = "ED",
            Room = $"TRAUMA{_random.Next(1, 6)}",
            Facility = "MAIN",
            LocationStatus = "A",
            PersonLocationType = "E"
        };
    }

    private PatientLocation GenerateOutpatientLocation()
    {
        var clinics = new[] { "CLINIC1", "CLINIC2", "SPECIALTY", "SURGERY" };
        return new PatientLocation
        {
            PointOfCare = clinics[_random.Next(clinics.Length)],
            Room = $"ROOM{_random.Next(1, 20)}",
            Facility = "OUTPATIENT",
            LocationStatus = "A",
            PersonLocationType = "C"
        };
    }

    private PatientLocation GenerateObservationLocation()
    {
        return new PatientLocation
        {
            PointOfCare = "OBS",
            Room = $"OBS{_random.Next(1, 20)}",
            Bed = new[] { "A", "B" }[_random.Next(2)],
            Facility = "MAIN",
            LocationStatus = "A",
            PersonLocationType = "N"
        };
    }

    private PatientLocation GenerateNewbornLocation()
    {
        return new PatientLocation
        {
            PointOfCare = "NURSERY",
            Room = "NEWBORN",
            Bed = $"NB{_random.Next(1, 20)}",
            Facility = "MAIN",
            LocationStatus = "A",
            PersonLocationType = "N"
        };
    }

    private PhysicianInfo GeneratePhysician(string? specialty = null)
    {
        var specialties = new[]
        {
            "Internal Medicine", "Family Medicine", "Emergency Medicine", "Surgery",
            "Cardiology", "Neurology", "Orthopedics", "Pediatrics", "Psychiatry", "Radiology"
        };

        var firstNames = new[] { "John", "Sarah", "Michael", "Jennifer", "David", "Lisa", "Robert", "Karen" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };

        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        var selectedSpecialty = specialty ?? specialties[_random.Next(specialties.Length)];

        return new PhysicianInfo
        {
            PhysicianId = $"PHY{_random.Next(10000, 99999)}",
            Name = $"Dr. {firstName} {lastName}",
            Specialty = selectedSpecialty,
            Department = selectedSpecialty,
            PhoneNumber = $"({_random.Next(200, 999):D3}) {_random.Next(200, 999):D3}-{_random.Next(1000, 9999):D4}"
        };
    }

    private AdmissionInfo GenerateAdmissionInfo(string admissionTypeHint)
    {
        var admissionTypes = admissionTypeHint switch
        {
            "emergency" => new[] { "E", "U" }, // Emergency, Urgent
            "elective" => new[] { "E", "L" }, // Elective, Labor
            "observation" => new[] { "O" }, // Observation
            "newborn" => new[] { "N" }, // Newborn
            _ => new[] { "E", "U", "E", "L" }
        };

        return new AdmissionInfo
        {
            AdmissionType = admissionTypes[_random.Next(admissionTypes.Length)],
            AdmissionSource = GenerateAdmissionSource(),
            AdmittingPhysician = GeneratePhysician(),
            HospitalService = GenerateHospitalService(),
            AdmitReason = GenerateAdmitReason(admissionTypeHint)
        };
    }

    private DischargeInfo GenerateDischargeInfo(int lengthOfStay, string? dispositionHint = null)
    {
        var dispositions = dispositionHint switch
        {
            "home" => new[] { "01", "06", "07" }, // Home, Home health, Left AMA
            "snf" => new[] { "03", "04" }, // SNF, ICF
            _ => new[] { "01", "03", "04", "06", "20" } // Various
        };

        return new DischargeInfo
        {
            DischargeDisposition = dispositions[_random.Next(dispositions.Length)],
            DischargeLocation = "Home",
            LengthOfStay = lengthOfStay,
            DischargingPhysician = GeneratePhysician(),
            DischargeInstructions = "Follow up with primary care physician in 1-2 weeks",
            FollowUpInstructions = "Return if symptoms worsen"
        };
    }

    private string GenerateAdmissionSource()
    {
        var sources = new[] { "1", "2", "4", "7", "8" }; // Physician referral, Clinic, Transfer, Emergency, Court/Law
        return sources[_random.Next(sources.Length)];
    }

    private string GenerateHospitalService()
    {
        var services = new[] { "MED", "SUR", "CAR", "NEU", "ORT", "PED", "PSY", "OBS" };
        return services[_random.Next(services.Length)];
    }

    private string GenerateAdmitReason(string admissionTypeHint)
    {
        return admissionTypeHint switch
        {
            "emergency" => "Acute illness requiring immediate treatment",
            "elective" => "Scheduled procedure",
            "observation" => "Observation and monitoring",
            "newborn" => "Normal delivery",
            _ => "Medical treatment"
        };
    }

    private string GenerateDiagnosis(string categoryHint)
    {
        var diagnoses = categoryHint switch
        {
            "emergency" => new[] { "Chest pain", "Abdominal pain", "Shortness of breath", "Trauma", "Stroke" },
            "surgical" => new[] { "Appendectomy", "Cholecystectomy", "Hernia repair", "Joint replacement" },
            "outpatient" => new[] { "Hypertension", "Diabetes", "Annual physical", "Follow-up visit" },
            "observation" => new[] { "Chest pain observation", "Rule out MI", "Syncope workup" },
            "readmission" => new[] { "CHF exacerbation", "Pneumonia", "Surgical site infection", "Medication reaction" },
            _ => new[] { "Medical condition", "Follow-up care", "Routine visit" }
        };

        return diagnoses[_random.Next(diagnoses.Length)];
    }
}

/// <summary>
/// Contains clinical data sets for journey generation.
/// </summary>
internal class ClinicalDataSets
{
    // Additional clinical data sets can be added here for more realistic generation
}