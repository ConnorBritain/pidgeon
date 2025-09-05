// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Moq;
using Pidgeon.Core.Application.Services.Generation.Plugins;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Generation.Types;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Tests.Generation;

/// <summary>
/// Test helpers for creating properly configured test objects for HL7 standards compliance tests
/// </summary>
internal static class TestHelpers
{
    internal static HL7MessageGenerationPlugin CreateHL7Plugin()
    {
        var domainService = CreateMockDomainGenerationService();
        var pluginRegistry = CreateMockPluginRegistry();
        var hl7MessageFactory = CreateMockHL7MessageFactory();
        
        return new HL7MessageGenerationPlugin(domainService.Object, pluginRegistry.Object, hl7MessageFactory.Object);
    }

    private static Mock<IGenerationService> CreateMockDomainGenerationService()
    {
        var mock = new Mock<IGenerationService>();
        
        // Setup mock patient generation
        var mockPatient = new Patient
        {
            Id = "PAT123",
            MedicalRecordNumber = "MRN123456",
            Name = PersonName.Create("Doe", "John"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1980, 5, 15),
            SocialSecurityNumber = "123-45-6789",
            PhoneNumber = "(555) 123-4567",
            Address = new Address
            {
                Street1 = "123 Main St",
                City = "Testville",
                State = "TS",
                PostalCode = "12345",
                Country = "US"
            }
        };
        
        mock.Setup(x => x.GeneratePatient(It.IsAny<GenerationOptions?>()))
            .Returns(Result<Patient>.Success(mockPatient));
            
        // Setup mock encounter generation
        var mockEncounter = new Encounter
        {
            Id = "ENC123",
            Patient = mockPatient,
            Provider = new Provider 
            { 
                Id = "PROV123",
                Name = PersonName.Create("Smith", "Dr. Jane"),
                NpiNumber = "1234567890",
                Specialty = "Internal Medicine"
            },
            Type = EncounterType.Inpatient,
            Status = EncounterStatus.Finished,
            StartTime = DateTime.Now.AddHours(-2),
            Location = "Room 101",
            Priority = EncounterPriority.Routine
        };
        
        mock.Setup(x => x.GenerateEncounter(It.IsAny<GenerationOptions?>()))
            .Returns(Result<Encounter>.Success(mockEncounter));
            
        // Setup mock prescription generation
        var mockPrescription = new Prescription
        {
            Id = "RX123",
            Patient = mockPatient,
            Medication = new Medication
            {
                Id = "MED123", 
                Name = "Lisinopril",
                GenericName = "Lisinopril",
                Strength = "10mg",
                DrugClass = "ACE Inhibitor"
            },
            Prescriber = mockEncounter.Provider,
            DatePrescribed = DateTime.Now,
            Dosage = new DosageInstructions
            {
                Dose = "1",
                DoseUnit = "tablet",
                Frequency = "QD",
                Route = RouteOfAdministration.Oral,
                Quantity = 30,
                DaysSupply = 30,
                Refills = 3
            }
        };
        
        mock.Setup(x => x.GeneratePrescription(It.IsAny<GenerationOptions?>()))
            .Returns(Result<Prescription>.Success(mockPrescription));
        
        return mock;
    }

    private static Mock<IStandardPluginRegistry> CreateMockPluginRegistry()
    {
        var mock = new Mock<IStandardPluginRegistry>();
        
        // For now, return null since the HL7 plugin has fallback logic
        mock.Setup(x => x.GetPlugin(It.IsAny<string>(), It.IsAny<Version?>()))
            .Returns((IStandardPlugin?)null);
            
        return mock;
    }

    private static Mock<IHL7MessageFactory> CreateMockHL7MessageFactory()
    {
        var mock = new Mock<IHL7MessageFactory>();
        
        // Use the real factory implementation for proper testing
        var loggerMock = new Mock<ILogger<HL7v23MessageFactory>>();
        var realFactory = new HL7v23MessageFactory(loggerMock.Object);
        
        mock.Setup(x => x.GenerateADT_A01(It.IsAny<Patient>(), It.IsAny<Encounter>()))
            .Returns((Patient p, Encounter e) => realFactory.GenerateADT_A01(p, e));
            
        mock.Setup(x => x.GenerateADT_A08(It.IsAny<Patient>(), It.IsAny<Encounter>()))
            .Returns((Patient p, Encounter e) => realFactory.GenerateADT_A08(p, e));
            
        mock.Setup(x => x.GenerateADT_A03(It.IsAny<Patient>(), It.IsAny<Encounter>()))
            .Returns((Patient p, Encounter e) => realFactory.GenerateADT_A03(p, e));
            
        mock.Setup(x => x.GenerateORU_R01(It.IsAny<Patient>(), It.IsAny<ObservationResult>()))
            .Returns((Patient p, ObservationResult o) => realFactory.GenerateORU_R01(p, o));
            
        mock.Setup(x => x.GenerateRDE_O11(It.IsAny<Patient>(), It.IsAny<Prescription>()))
            .Returns((Patient p, Prescription pr) => realFactory.GenerateRDE_O11(p, pr));
        
        return mock;
    }
}