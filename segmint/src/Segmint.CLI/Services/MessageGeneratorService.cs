// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;

namespace Segmint.CLI.Services;

/// <summary>
/// Implementation of message generation service.
/// </summary>
public class MessageGeneratorService : IMessageGeneratorService
{
    private readonly ILogger<MessageGeneratorService> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageGeneratorService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MessageGeneratorService(ILogger<MessageGeneratorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HL7Message>> GenerateMessagesAsync(
        string messageType,
        int count = 1,
        string? configurationPath = null,
        string[]? templateNames = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {Count} {MessageType} messages", count, messageType);

        var messages = new List<HL7Message>();

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = messageType.ToUpperInvariant() switch
            {
                "RDE" => await GenerateRDEMessageAsync(i + 1, cancellationToken),
                "ADT" => await GenerateADTMessageAsync(i + 1, cancellationToken),
                "ACK" => await GenerateACKMessageAsync(i + 1, cancellationToken),
                _ => throw new ArgumentException($"Unsupported message type: {messageType}")
            };

            messages.Add(message);

            if (i % 10 == 0 && i > 0)
            {
                _logger.LogDebug("Generated {Generated} of {Total} messages", i, count);
            }
        }

        _logger.LogInformation("Successfully generated {Count} {MessageType} messages", count, messageType);
        return messages;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableMessageTypes()
    {
        return new[] { "RDE", "ADT", "ACK" };
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableTemplates(string messageType)
    {
        return messageType.ToUpperInvariant() switch
        {
            "RDE" => new[] { "standard", "controlled-substance", "compound", "unit-dose" },
            "ADT" => new[] { "admit", "discharge", "transfer", "update" },
            "ACK" => new[] { "accept", "reject", "error" },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Generates an RDE (pharmacy order) message.
    /// </summary>
    private async Task<RDEMessage> GenerateRDEMessageAsync(int sequenceNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation

        var message = new RDEMessage();

        // Generate synthetic patient data
        var patientData = GenerateSyntheticPatientData(sequenceNumber);
        var medicationData = GenerateSyntheticMedicationData();

        // Setup basic order
        message.SetupBasicOrder(
            "SEGMINT_CLI",
            "TEST_FACILITY",
            "PHARMACY_SYSTEM",
            "MAIN_PHARMACY",
            patientData.Id,
            patientData.FirstName,
            patientData.LastName,
            medicationData.Code,
            medicationData.Name,
            "DR001",
            isProduction: false);

        // Set comprehensive patient info
        message.SetPatientInfo(
            patientData.Id,
            patientData.FirstName,
            patientData.LastName,
            patientData.MiddleName,
            patientData.DateOfBirth,
            patientData.Gender,
            patientData.AccountNumber,
            patientData.Street,
            patientData.City,
            patientData.State,
            patientData.PostalCode,
            patientData.Phone);

        // Set medication details
        message.SetMedicationDetails(
            medicationData.Code,
            medicationData.Name,
            medicationData.Strength,
            medicationData.StrengthUnits,
            medicationData.DosageForm,
            medicationData.DispenseQuantity,
            medicationData.DispenseUnits,
            medicationData.Refills,
            medicationData.Sig,
            medicationData.DaysSupply);

        return message;
    }

    /// <summary>
    /// Generates an ADT (patient administration) message.
    /// </summary>
    private async Task<HL7Message> GenerateADTMessageAsync(int sequenceNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        
        // Generate random trigger event for ADT
        var triggerEvents = new[] { "A01", "A02", "A03", "A04", "A05", "A08" };
        var selectedEvent = triggerEvents[_random.Next(triggerEvents.Length)];
        
        var message = new ADTMessage(selectedEvent);
        
        // Generate synthetic patient data
        var patientData = GenerateSyntheticPatientData(sequenceNumber);
        
        message.SetPatientDemographics(
            patientData.Id, 
            patientData.LastName, 
            patientData.FirstName, 
            patientData.MiddleName,
            patientData.DateOfBirth, 
            patientData.Gender);
        
        message.SetPatientAddress(
            patientData.Street ?? "123 Main St", 
            patientData.City ?? "Anytown", 
            patientData.State ?? "CA", 
            patientData.PostalCode ?? "12345");
        
        // Generate visit information
        var patientClasses = new[] { "I", "O", "E", "N", "R" };
        var patientClass = patientClasses[_random.Next(patientClasses.Length)];
        var location = $"{_random.Next(1, 10)}N{_random.Next(100, 999)}";
        var admissionTypes = new[] { "E", "L", "N", "R", "U" };
        var admissionType = admissionTypes[_random.Next(admissionTypes.Length)];
        var visitNumber = $"V{sequenceNumber:D8}";
        
        message.SetPatientVisit(patientClass, location, null, admissionType, visitNumber);
        
        // Set admission time for admit events
        if (selectedEvent == "A01" || selectedEvent == "A04")
        {
            var admissionTime = DateTime.Now.AddHours(-_random.Next(1, 48));
            message.SetAdmissionDateTime(admissionTime);
        }
        
        // Set discharge time for discharge events  
        if (selectedEvent == "A03")
        {
            var dischargeTime = DateTime.Now.AddMinutes(-_random.Next(5, 120));
            message.SetDischargeDateTime(dischargeTime);
        }
        
        // Set applications
        message.SetSendingApplication("SEGMINT", "SEGMINT_FACILITY");
        message.SetReceivingApplication("TARGET_APP", "TARGET_FACILITY");
        
        return message;
    }

    /// <summary>
    /// Generates an ACK (acknowledgment) message.
    /// </summary>
    private async Task<HL7Message> GenerateACKMessageAsync(int sequenceNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        
        // Generate random acknowledgment scenario
        var ackCodes = new[] { "AA", "AE", "AR", "CA", "CE", "CR" };
        var selectedCode = ackCodes[_random.Next(ackCodes.Length)];
        
        string? textMessage = selectedCode switch
        {
            "AA" => "Message accepted successfully",
            "AE" => "Application error - invalid patient ID",
            "AR" => "Application reject - duplicate order",
            "CA" => "Commit accept - order processed",
            "CE" => "Commit error - unable to process order",
            "CR" => "Commit reject - order validation failed",
            _ => null
        };
        
        var message = new ACKMessage(null, selectedCode, textMessage);
        
        // Set applications
        message.SetSendingApplication("TARGET_APP", "TARGET_FACILITY");
        message.SetReceivingApplication("SEGMINT", "SEGMINT_FACILITY");
        
        // For error acknowledgments, add error condition
        if (selectedCode is "AE" or "AR" or "CE" or "CR")
        {
            var errorCodes = new[] { "100", "101", "102", "200", "201", "202" };
            var errorCode = errorCodes[_random.Next(errorCodes.Length)];
            var errorDescriptions = new[]
            {
                "Invalid patient identifier",
                "Required field missing", 
                "Invalid data format",
                "Processing error",
                "Database error",
                "System unavailable"
            };
            var errorDescription = errorDescriptions[_random.Next(errorDescriptions.Length)];
            
            message.SetErrorCondition(errorCode, errorDescription);
        }
        
        return message;
    }

    /// <summary>
    /// Generates synthetic patient data for testing.
    /// </summary>
    private PatientData GenerateSyntheticPatientData(int sequenceNumber)
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Lisa", "Robert", "Jennifer", "William", "Mary" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        var middleNames = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" };
        var streets = new[] { "Main St", "Oak Ave", "Pine Rd", "Elm Blvd", "Maple Dr", "Cedar Ln", "Park Ave", "First St", "Second St", "Third St" };
        var cities = new[] { "Springfield", "Franklin", "Georgetown", "Madison", "Arlington", "Salem", "Fairview", "Riverside", "Oakland", "Clayton" };
        var states = new[] { "CA", "TX", "FL", "NY", "PA", "IL", "OH", "GA", "NC", "MI" };

        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        var middleName = middleNames[_random.Next(middleNames.Length)];

        return new PatientData
        {
            Id = $"PAT{sequenceNumber:D6}",
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = DateTime.Today.AddYears(-_random.Next(18, 85)).AddDays(-_random.Next(0, 365)),
            Gender = _random.Next(2) == 0 ? "M" : "F",
            AccountNumber = $"ACC{sequenceNumber:D8}",
            Street = $"{_random.Next(100, 9999)} {streets[_random.Next(streets.Length)]}",
            City = cities[_random.Next(cities.Length)],
            State = states[_random.Next(states.Length)],
            PostalCode = $"{_random.Next(10000, 99999)}",
            Phone = $"({_random.Next(200, 999)}) {_random.Next(200, 999)}-{_random.Next(1000, 9999)}"
        };
    }

    /// <summary>
    /// Generates synthetic medication data for testing.
    /// </summary>
    private MedicationData GenerateSyntheticMedicationData()
    {
        var medications = new[]
        {
            new { Code = "00781-1506-01", Name = "Lisinopril", Strength = 10m, Units = "mg", Form = "tablet" },
            new { Code = "00781-1507-01", Name = "Metformin", Strength = 500m, Units = "mg", Form = "tablet" },
            new { Code = "00781-1508-01", Name = "Amlodipine", Strength = 5m, Units = "mg", Form = "tablet" },
            new { Code = "00781-1509-01", Name = "Atorvastatin", Strength = 20m, Units = "mg", Form = "tablet" },
            new { Code = "00781-1510-01", Name = "Omeprazole", Strength = 20m, Units = "mg", Form = "capsule" }
        };

        var medication = medications[_random.Next(medications.Length)];
        var dispenseQty = _random.Next(30, 90);
        var refills = _random.Next(0, 6);

        return new MedicationData
        {
            Code = medication.Code,
            Name = medication.Name,
            Strength = medication.Strength,
            StrengthUnits = medication.Units,
            DosageForm = medication.Form,
            DispenseQuantity = dispenseQty,
            DispenseUnits = medication.Form,
            Refills = refills,
            Sig = $"Take 1 {medication.Form} by mouth daily",
            DaysSupply = dispenseQty
        };
    }

    /// <summary>
    /// Patient data model for message generation.
    /// </summary>
    private record PatientData
    {
        public string Id { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? AccountNumber { get; init; }
        public string? Street { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? Phone { get; init; }
    }

    /// <summary>
    /// Medication data model for message generation.
    /// </summary>
    private record MedicationData
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal Strength { get; init; }
        public string StrengthUnits { get; init; } = string.Empty;
        public string DosageForm { get; init; } = string.Empty;
        public decimal DispenseQuantity { get; init; }
        public string DispenseUnits { get; init; } = string.Empty;
        public int Refills { get; init; }
        public string Sig { get; init; } = string.Empty;
        public int DaysSupply { get; init; }
    }
}