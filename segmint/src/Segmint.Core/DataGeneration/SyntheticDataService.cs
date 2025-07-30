// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Segmint.Core.DataGeneration.Demographics;
using Segmint.Core.DataGeneration.Pharmacy;
using Segmint.Core.DataGeneration.Clinical;
using Segmint.Core.Standards.HL7.v23.Messages;

namespace Segmint.Core.DataGeneration;

/// <summary>
/// Coordinates synthetic data generation for HL7 message testing.
/// </summary>
public class SyntheticDataService
{
    private readonly DemographicsGenerator _demographicsGenerator;
    private readonly MedicationGenerator _medicationGenerator;
    private readonly PatientJourneyGenerator _journeyGenerator;
    private readonly ILogger<SyntheticDataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticDataService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="seed">Random seed for reproducible generation.</param>
    public SyntheticDataService(ILogger<SyntheticDataService> logger, int? seed = null)
    {
        _logger = logger;
        _demographicsGenerator = new DemographicsGenerator(seed);
        _medicationGenerator = new MedicationGenerator(seed);
        _journeyGenerator = new PatientJourneyGenerator(seed);
    }

    /// <summary>
    /// Generates synthetic RDE (pharmacy order) messages.
    /// </summary>
    /// <param name="count">Number of messages to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Generated RDE messages with synthetic data.</returns>
    public IEnumerable<RDEMessage> GenerateRDEMessages(int count, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();
        
        _logger.LogInformation("Generating {Count} synthetic RDE messages", count);

        for (int i = 0; i < count; i++)
        {
            var patient = _demographicsGenerator.Generate(constraints);
            var prescription = _medicationGenerator.Generate(constraints);
            
            var message = CreateRDEMessage(patient, prescription);
            yield return message;
        }
    }

    /// <summary>
    /// Generates synthetic ADT (patient administration) messages.
    /// </summary>
    /// <param name="count">Number of messages to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Generated ADT messages with synthetic data.</returns>
    public IEnumerable<ADTMessage> GenerateADTMessages(int count, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();
        
        _logger.LogInformation("Generating {Count} synthetic ADT messages", count);

        for (int i = 0; i < count; i++)
        {
            var journey = _journeyGenerator.Generate(constraints);
            
            foreach (var clinicalEvent in journey.Events)
            {
                var message = CreateADTMessage(journey.Patient, clinicalEvent, journey.Facility);
                yield return message;
            }
        }
    }

    /// <summary>
    /// Generates a complete patient journey as a sequence of ADT messages.
    /// </summary>
    /// <param name="journeyType">Type of journey to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Sequence of ADT messages representing a patient journey.</returns>
    public PatientJourneySequence GeneratePatientJourneySequence(string journeyType, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();
        constraints.CustomConstraints["JourneyType"] = journeyType;

        var journey = _journeyGenerator.Generate(constraints);
        var messages = new List<ADTMessage>();

        foreach (var clinicalEvent in journey.Events)
        {
            var message = CreateADTMessage(journey.Patient, clinicalEvent, journey.Facility);
            messages.Add(message);
        }

        return new PatientJourneySequence
        {
            Journey = journey,
            Messages = messages,
            SequenceId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Generates synthetic data for a specific scenario.
    /// </summary>
    /// <param name="scenario">The scenario to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Generated scenario data.</returns>
    public ScenarioData GenerateScenario(TestScenario scenario, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();

        return scenario.Type switch
        {
            ScenarioType.PharmacyOrder => GeneratePharmacyOrderScenario(scenario, constraints),
            ScenarioType.EmergencyAdmission => GenerateEmergencyAdmissionScenario(scenario, constraints),
            ScenarioType.ElectiveSurgery => GenerateElectiveSurgeryScenario(scenario, constraints),
            ScenarioType.OutpatientVisit => GenerateOutpatientVisitScenario(scenario, constraints),
            ScenarioType.CompletePatientStay => GenerateCompletePatientStayScenario(scenario, constraints),
            _ => throw new ArgumentException($"Unknown scenario type: {scenario.Type}")
        };
    }

    /// <summary>
    /// Generates multiple scenarios for comprehensive testing.
    /// </summary>
    /// <param name="scenarioMix">Mix of scenarios to generate.</param>
    /// <param name="constraints">Generation constraints.</param>
    /// <returns>Collection of generated scenario data.</returns>
    public IEnumerable<ScenarioData> GenerateTestSuite(ScenarioMix scenarioMix, DataGenerationConstraints? constraints = null)
    {
        constraints ??= new DataGenerationConstraints();

        _logger.LogInformation("Generating test suite with {ScenarioCount} scenarios", scenarioMix.TotalScenarios);

        foreach (var scenarioConfig in scenarioMix.Scenarios)
        {
            for (int i = 0; i < scenarioConfig.Count; i++)
            {
                var scenario = new TestScenario
                {
                    Type = scenarioConfig.Type,
                    Name = $"{scenarioConfig.Type}_{i + 1:D3}",
                    Description = scenarioConfig.Description
                };

                yield return GenerateScenario(scenario, constraints);
            }
        }
    }

    private RDEMessage CreateRDEMessage(PatientDemographics patient, PrescriptionOrder prescription)
    {
        var message = new RDEMessage();

        // Set up basic pharmacy order
        message.SetupBasicOrder(
            sendingApplication: "Segmint",
            sendingFacility: prescription.Pharmacy.Name,
            receivingApplication: "PharmacySystem",
            receivingFacility: "HIS",
            patientId: patient.PatientId,
            patientFirstName: patient.FirstName,
            patientLastName: patient.LastName,
            drugCode: prescription.Medication.NDC,
            drugName: prescription.Medication.GenericName,
            orderingProvider: prescription.Prescriber.ProviderId,
            isProduction: false
        );

        // Set detailed medication information
        message.SetMedicationDetails(
            drugCode: prescription.Medication.NDC,
            drugName: prescription.Medication.FullName,
            strength: decimal.Parse(prescription.Medication.Strength),
            strengthUnits: prescription.Medication.StrengthUnits,
            dosageForm: prescription.Medication.DosageForm,
            dispenseQuantity: prescription.Quantity,
            dispenseUnits: prescription.QuantityUnits,
            refills: prescription.Refills,
            sig: prescription.Directions,
            daysSupply: prescription.DaysSupply
        );

        // Set comprehensive patient information
        message.SetPatientInfo(
            patientId: patient.PatientId,
            firstName: patient.FirstName,
            lastName: patient.LastName,
            middleName: patient.MiddleName,
            dateOfBirth: patient.DateOfBirth,
            gender: patient.Gender,
            accountNumber: patient.AccountNumber,
            street: patient.Address.Street,
            city: patient.Address.City,
            state: patient.Address.State,
            postalCode: patient.Address.PostalCode,
            homePhone: patient.Contact.HomePhone
        );

        // Set order information
        message.SetOrderInfo(
            orderControl: "NW",
            orderingProvider: prescription.Prescriber.ProviderId,
            orderingProviderDEA: prescription.Prescriber.DEANumber,
            enteredBy: "SYSTEM",
            orderEffectiveDate: prescription.OrderDateTime
        );

        return message;
    }

    private ADTMessage CreateADTMessage(PatientDemographics patient, ClinicalEvent clinicalEvent, FacilityInfo facility)
    {
        var message = new ADTMessage(clinicalEvent.TriggerEvent);

        // Set patient demographics
        message.SetPatientDemographics(
            patientId: patient.PatientId,
            lastName: patient.LastName,
            firstName: patient.FirstName,
            middleName: patient.MiddleName,
            dateOfBirth: patient.DateOfBirth,
            gender: patient.Gender,
            ssn: patient.SocialSecurityNumber
        );

        // Set patient address
        if (!string.IsNullOrEmpty(patient.Address.Street))
        {
            message.SetPatientAddress(
                streetAddress: patient.Address.Street,
                city: patient.Address.City,
                state: patient.Address.State,
                zipCode: patient.Address.PostalCode,
                country: patient.Address.Country
            );
        }

        // Set patient visit information
        message.SetPatientVisit(
            patientClass: clinicalEvent.PatientClass,
            assignedPatientLocation: clinicalEvent.Location.FormattedLocation,
            attendingDoctor: clinicalEvent.AttendingPhysician?.Name,
            admissionType: clinicalEvent.Admission?.AdmissionType,
            visitNumber: clinicalEvent.VisitNumber
        );

        // Set facility information
        message.SetSendingApplication("Segmint", facility.Name);
        message.SetReceivingApplication("HIS", "Central");

        // Set event-specific timing
        if (clinicalEvent.EventType.Contains("Admit") && clinicalEvent.Admission != null)
        {
            message.SetAdmissionDateTime(clinicalEvent.EventDateTime);
        }
        else if (clinicalEvent.EventType.Contains("Discharge") && clinicalEvent.Discharge != null)
        {
            message.SetDischargeDateTime(clinicalEvent.EventDateTime);
        }

        // Add notes if available
        if (!string.IsNullOrEmpty(clinicalEvent.Notes))
        {
            message.AddNote(clinicalEvent.Notes, "P");
        }

        return message;
    }

    private ScenarioData GeneratePharmacyOrderScenario(TestScenario scenario, DataGenerationConstraints constraints)
    {
        var patient = _demographicsGenerator.Generate(constraints);
        var prescription = _medicationGenerator.Generate(constraints);
        var rdeMessage = CreateRDEMessage(patient, prescription);

        return new ScenarioData
        {
            Scenario = scenario,
            Patient = patient,
            Messages = new List<HL7Message> { rdeMessage },
            Prescription = prescription,
            GeneratedAt = DateTime.Now
        };
    }

    private ScenarioData GenerateEmergencyAdmissionScenario(TestScenario scenario, DataGenerationConstraints constraints)
    {
        constraints.CustomConstraints["JourneyType"] = JourneyTypes.EmergencyAdmission;
        var journey = _journeyGenerator.Generate(constraints);
        var messages = journey.Events.Select(e => CreateADTMessage(journey.Patient, e, journey.Facility)).Cast<HL7Message>().ToList();

        return new ScenarioData
        {
            Scenario = scenario,
            Patient = journey.Patient,
            Messages = messages,
            Journey = journey,
            GeneratedAt = DateTime.Now
        };
    }

    private ScenarioData GenerateElectiveSurgeryScenario(TestScenario scenario, DataGenerationConstraints constraints)
    {
        constraints.CustomConstraints["JourneyType"] = JourneyTypes.ElectiveSurgery;
        var journey = _journeyGenerator.Generate(constraints);
        var messages = journey.Events.Select(e => CreateADTMessage(journey.Patient, e, journey.Facility)).Cast<HL7Message>().ToList();

        return new ScenarioData
        {
            Scenario = scenario,
            Patient = journey.Patient,
            Messages = messages,
            Journey = journey,
            GeneratedAt = DateTime.Now
        };
    }

    private ScenarioData GenerateOutpatientVisitScenario(TestScenario scenario, DataGenerationConstraints constraints)
    {
        constraints.CustomConstraints["JourneyType"] = JourneyTypes.OutpatientVisit;
        var journey = _journeyGenerator.Generate(constraints);
        var messages = journey.Events.Select(e => CreateADTMessage(journey.Patient, e, journey.Facility)).Cast<HL7Message>().ToList();

        return new ScenarioData
        {
            Scenario = scenario,
            Patient = journey.Patient,
            Messages = messages,
            Journey = journey,
            GeneratedAt = DateTime.Now
        };
    }

    private ScenarioData GenerateCompletePatientStayScenario(TestScenario scenario, DataGenerationConstraints constraints)
    {
        // Generate a complete patient stay with pharmacy orders
        constraints.CustomConstraints["JourneyType"] = JourneyTypes.EmergencyAdmission;
        var journey = _journeyGenerator.Generate(constraints);
        var adtMessages = journey.Events.Select(e => CreateADTMessage(journey.Patient, e, journey.Facility)).Cast<HL7Message>().ToList();

        // Add some medication orders during the stay
        var medicationCount = new Random().Next(1, 4);
        for (int i = 0; i < medicationCount; i++)
        {
            var prescription = _medicationGenerator.Generate(constraints);
            var rdeMessage = CreateRDEMessage(journey.Patient, prescription);
            adtMessages.Add(rdeMessage);
        }

        return new ScenarioData
        {
            Scenario = scenario,
            Patient = journey.Patient,
            Messages = adtMessages,
            Journey = journey,
            GeneratedAt = DateTime.Now
        };
    }
}

/// <summary>
/// Represents a sequence of messages for a patient journey.
/// </summary>
public class PatientJourneySequence
{
    /// <summary>
    /// The patient journey data.
    /// </summary>
    public PatientJourney Journey { get; set; } = new();

    /// <summary>
    /// Generated ADT messages for the journey.
    /// </summary>
    public List<ADTMessage> Messages { get; set; } = new();

    /// <summary>
    /// Unique identifier for this sequence.
    /// </summary>
    public string SequenceId { get; set; } = "";

    /// <summary>
    /// When this sequence was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets the total duration of the journey.
    /// </summary>
    public TimeSpan Duration => Journey.Duration;

    /// <summary>
    /// Gets the number of messages in the sequence.
    /// </summary>
    public int MessageCount => Messages.Count;
}

/// <summary>
/// Represents a test scenario configuration.
/// </summary>
public class TestScenario
{
    /// <summary>
    /// Scenario type.
    /// </summary>
    public ScenarioType Type { get; set; }

    /// <summary>
    /// Scenario name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Scenario description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Custom parameters for the scenario.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Types of test scenarios.
/// </summary>
public enum ScenarioType
{
    PharmacyOrder,
    EmergencyAdmission,
    ElectiveSurgery,
    OutpatientVisit,
    CompletePatientStay
}

/// <summary>
/// Generated scenario data.
/// </summary>
public class ScenarioData
{
    /// <summary>
    /// The scenario that was generated.
    /// </summary>
    public TestScenario Scenario { get; set; } = new();

    /// <summary>
    /// The patient data.
    /// </summary>
    public PatientDemographics Patient { get; set; } = new();

    /// <summary>
    /// Generated HL7 messages.
    /// </summary>
    public List<HL7Message> Messages { get; set; } = new();

    /// <summary>
    /// Patient journey (if applicable).
    /// </summary>
    public PatientJourney? Journey { get; set; }

    /// <summary>
    /// Prescription data (if applicable).
    /// </summary>
    public PrescriptionOrder? Prescription { get; set; }

    /// <summary>
    /// When this scenario was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Configuration for generating a mix of scenarios.
/// </summary>
public class ScenarioMix
{
    /// <summary>
    /// List of scenario configurations.
    /// </summary>
    public List<ScenarioConfiguration> Scenarios { get; set; } = new();

    /// <summary>
    /// Gets the total number of scenarios to generate.
    /// </summary>
    public int TotalScenarios => Scenarios.Sum(s => s.Count);

    /// <summary>
    /// Creates a typical test mix for healthcare scenarios.
    /// </summary>
    /// <returns>A balanced mix of healthcare scenarios.</returns>
    public static ScenarioMix CreateTypicalMix()
    {
        return new ScenarioMix
        {
            Scenarios = new List<ScenarioConfiguration>
            {
                new() { Type = ScenarioType.PharmacyOrder, Count = 10, Description = "Basic pharmacy orders" },
                new() { Type = ScenarioType.OutpatientVisit, Count = 8, Description = "Routine outpatient visits" },
                new() { Type = ScenarioType.EmergencyAdmission, Count = 5, Description = "Emergency admissions" },
                new() { Type = ScenarioType.ElectiveSurgery, Count = 3, Description = "Scheduled surgeries" },
                new() { Type = ScenarioType.CompletePatientStay, Count = 2, Description = "Complete patient episodes" }
            }
        };
    }
}

/// <summary>
/// Configuration for a single scenario type.
/// </summary>
public class ScenarioConfiguration
{
    /// <summary>
    /// Scenario type.
    /// </summary>
    public ScenarioType Type { get; set; }

    /// <summary>
    /// Number of scenarios to generate.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Description of the scenario.
    /// </summary>
    public string Description { get; set; } = "";
}