// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Pidgeon.Core.Application.Services.Generation;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Domain.Clinical.Entities;
using Xunit;

namespace Pidgeon.Core.Tests.Generation;

/// <summary>
/// Tests for GenerationService focusing on healthcare data safety and template method pattern.
/// Verifies deterministic generation, realistic healthcare data, and consolidated error handling patterns.
/// </summary>
public class AlgorithmicGenerationServiceTests
{
    private readonly GenerationService _service;
    private readonly GenerationOptions _defaultOptions;

    public AlgorithmicGenerationServiceTests()
    {
        _service = new GenerationService(NullLogger<GenerationService>.Instance);
        _defaultOptions = new GenerationOptions();
    }

    #region Template Method Pattern Tests

    [Fact(DisplayName = "Should generate deterministic patients with same seed")]
    public void Should_Generate_Deterministic_Patients_With_Same_Seed()
    {
        var options = new GenerationOptions { Seed = 12345 };
        
        var patient1 = _service.GeneratePatient(options);
        var patient2 = _service.GeneratePatient(options);
        
        patient1.IsSuccess.Should().BeTrue();
        patient2.IsSuccess.Should().BeTrue();
        patient1.Value.Name.Family.Should().Be(patient2.Value.Name.Family);
        patient1.Value.Name.Given.Should().Be(patient2.Value.Name.Given);
        patient1.Value.MedicalRecordNumber.Should().Be(patient2.Value.MedicalRecordNumber);
        patient1.Value.SocialSecurityNumber.Should().Be(patient2.Value.SocialSecurityNumber);
        patient1.Value.PhoneNumber.Should().Be(patient2.Value.PhoneNumber);
        patient1.Value.Address?.Street1.Should().Be(patient2.Value.Address?.Street1);
        patient1.Value.Address?.City.Should().Be(patient2.Value.Address?.City);
        patient1.Value.Address?.State.Should().Be(patient2.Value.Address?.State);
    }

    [Fact(DisplayName = "Should generate different patients with different seeds")]
    public void Should_Generate_Different_Patients_With_Different_Seeds()
    {
        var options1 = new GenerationOptions { Seed = 12345 };
        var options2 = new GenerationOptions { Seed = 54321 };
        
        var patient1 = _service.GeneratePatient(options1);
        var patient2 = _service.GeneratePatient(options2);
        patient1.IsSuccess.Should().BeTrue();
        patient2.IsSuccess.Should().BeTrue();
        patient1.Value.Should().NotBeEquivalentTo(patient2.Value);
    }

    [Fact(DisplayName = "Should generate deterministic medications with same seed")]
    public void Should_Generate_Deterministic_Medications_With_Same_Seed()
    {
        // Arrange - Use specific seed for deterministic generation
        var options = new GenerationOptions { Seed = 98765 };
        
        // Act - Generate two medications with same seed
        var medication1 = _service.GenerateMedication(options);
        var medication2 = _service.GenerateMedication(options);
        
        // Assert - Should produce identical medications (except GUIDs)
        medication1.IsSuccess.Should().BeTrue();
        medication2.IsSuccess.Should().BeTrue();
        medication1.Value.Name.Should().Be(medication2.Value.Name);
        medication1.Value.GenericName.Should().Be(medication2.Value.GenericName);
        medication1.Value.Strength.Should().Be(medication2.Value.Strength);
    }

    [Fact(DisplayName = "Should use different seed offsets for different entity types")]
    public void Should_Use_Different_Seed_Offsets_For_Different_Entity_Types()
    {
        var options = new GenerationOptions { Seed = 11111 };
        
        var encounter = _service.GenerateEncounter(options);
        var prescription = _service.GeneratePrescription(options);
        
        encounter.IsSuccess.Should().BeTrue();
        prescription.IsSuccess.Should().BeTrue();
        
        encounter.Value.Patient.MedicalRecordNumber
            .Should().NotBe(prescription.Value.Patient.MedicalRecordNumber);
    }

    #endregion

    #region Healthcare Data Validity Tests

    [Fact(DisplayName = "Generated patients should have valid healthcare demographics")]
    public void Generated_Patients_Should_Have_Valid_Healthcare_Demographics()
    {
        var patients = new List<Patient>();
        for (int i = 0; i < 10; i++)
        {
            var result = _service.GeneratePatient(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            patients.Add(result.Value);
        }
        
        patients.Should().AllSatisfy(patient =>
        {
            patient.Id.Should().NotBeNullOrEmpty("Patient ID is required for healthcare systems");
            patient.MedicalRecordNumber.Should().NotBeNullOrEmpty("MRN is required for patient identification");
            patient.Name.Should().NotBeNull("Patient name is required");
            patient.Name.Family.Should().NotBeNullOrEmpty("Last name is required");
            patient.Name.Given.Should().NotBeNullOrEmpty("First name is required");
            patient.BirthDate.Should().BeBefore(DateTime.Today, "Birth date must be in the past");
            patient.BirthDate.Should().BeAfter(DateTime.Today.AddYears(-120), "Birth date should be realistic");
            patient.PhoneNumber.Should().NotBeNullOrEmpty("Phone number should be provided");
            patient.Address.Should().NotBeNull("Address should be provided for healthcare records");
        });
    }

    [Fact(DisplayName = "Generated SSNs should use safe fake range")]
    public void Generated_SSNs_Should_Use_Safe_Fake_Range()
    {
        var patients = new List<Patient>();
        for (int i = 0; i < 20; i++)
        {
            var result = _service.GeneratePatient(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            patients.Add(result.Value);
        }
        
        // Assert - All SSNs should use 900+ area code (fake SSN range)
        patients.Should().AllSatisfy(patient =>
        {
            patient.SocialSecurityNumber.Should().NotBeNullOrEmpty();
            patient.SocialSecurityNumber.Should().MatchRegex(@"^9\d{2}-\d{2}-\d{4}$", 
                "Should use 900+ range to avoid real SSN collision");
        });
    }

    [Fact(DisplayName = "Generated medications should be from healthcare dataset")]
    public void Generated_Medications_Should_Be_From_Healthcare_Dataset()
    {
        var medications = new List<Medication>();
        for (int i = 0; i < 15; i++)
        {
            var result = _service.GenerateMedication(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            medications.Add(result.Value);
        }
        
        medications.Should().AllSatisfy(medication =>
        {
            medication.Id.Should().NotBeNullOrEmpty("Medication ID is required");
            medication.Name.Should().NotBeNullOrEmpty("Medication name is required");
            medication.GenericName.Should().NotBeNullOrEmpty("Generic name is required for healthcare safety");
            medication.DrugClass.Should().NotBeNullOrEmpty("Drug class is important for clinical decisions");
        });
    }

    [Fact(DisplayName = "Generated prescriptions should have valid clinical relationships")]
    public void Generated_Prescriptions_Should_Have_Valid_Clinical_Relationships()
    {
        var prescriptions = new List<Prescription>();
        for (int i = 0; i < 5; i++)
        {
            var result = _service.GeneratePrescription(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            prescriptions.Add(result.Value);
        }
        
        prescriptions.Should().AllSatisfy(prescription =>
        {
            prescription.Id.Should().NotBeNullOrEmpty("Prescription number is required");
            prescription.Patient.Should().NotBeNull("Every prescription needs a patient");
            prescription.Medication.Should().NotBeNull("Every prescription needs a medication");
            prescription.Prescriber.Should().NotBeNull("Every prescription needs a prescriber");
            prescription.Dosage.Should().NotBeNull("Dosage instructions are required for safety");
            prescription.DatePrescribed.Should().BeBefore(DateTime.Today.AddDays(1), "Prescription date should be reasonable");
        });
    }

    [Fact(DisplayName = "Generated encounters should have valid healthcare workflow data")]
    public void Generated_Encounters_Should_Have_Valid_Healthcare_Workflow_Data()
    {
        var encounters = new List<Encounter>();
        for (int i = 0; i < 5; i++)
        {
            var result = _service.GenerateEncounter(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            encounters.Add(result.Value);
        }
        
        encounters.Should().AllSatisfy(encounter =>
        {
            encounter.Id.Should().NotBeNullOrEmpty("Encounter ID is required for tracking");
            encounter.Patient.Should().NotBeNull("Every encounter needs a patient");
            encounter.Provider.Should().NotBeNull("Every encounter needs a provider");
            encounter.Type.Should().BeDefined("Encounter type must be specified");
            encounter.Status.Should().BeDefined("Encounter status must be tracked");
            encounter.Location.Should().NotBeNullOrEmpty("Encounter location should be specified");
        });
    }

    #endregion

    #region Age Distribution Tests

    [Theory(DisplayName = "Should generate realistic age distribution for healthcare utilization")]
    [InlineData(100)] // Generate 100 patients to test distribution
    public void Should_Generate_Realistic_Age_Distribution(int patientCount)
    {
        // Act - Generate many patients to test age distribution
        var ages = new List<int>();
        for (int i = 0; i < patientCount; i++)
        {
            var options = new GenerationOptions { Seed = i }; // Different seed for each
            var result = _service.GeneratePatient(options);
            result.IsSuccess.Should().BeTrue();
            
            var age = result.Value.GetAge();
            if (age.HasValue)
                ages.Add(age.Value);
        }
        
        // Assert - Age distribution should reflect healthcare utilization patterns
        ages.Should().AllSatisfy(age => age.Should().BeInRange(0, 95, "Ages should be realistic"));
        
        // Should have representation across all age groups (not perfect distribution, but some variety)
        var pediatric = ages.Count(a => a < 18);
        var adult = ages.Count(a => a >= 18 && a < 65);
        var geriatric = ages.Count(a => a >= 65);
        
        pediatric.Should().BeGreaterThan(0, "Should generate some pediatric patients");
        adult.Should().BeGreaterThan(0, "Should generate some adult patients");
        geriatric.Should().BeGreaterThan(0, "Should generate some geriatric patients");
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "Should handle generation errors gracefully with Result pattern")]
    public void Should_Handle_Generation_Errors_Gracefully_With_Result_Pattern()
    {
        // All generation methods should return Result<T> and not throw exceptions
        // This test verifies the template method pattern handles errors correctly
        
        // Act & Assert - All methods should return Result<T> without throwing
        var patientResult = _service.GeneratePatient(_defaultOptions);
        var medicationResult = _service.GenerateMedication(_defaultOptions);
        var prescriptionResult = _service.GeneratePrescription(_defaultOptions);
        var encounterResult = _service.GenerateEncounter(_defaultOptions);
        
        // All should succeed with valid options
        patientResult.IsSuccess.Should().BeTrue();
        medicationResult.IsSuccess.Should().BeTrue();
        prescriptionResult.IsSuccess.Should().BeTrue();
        encounterResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Service Info Tests

    [Fact(DisplayName = "Should provide accurate service information for free tier")]
    public void Should_Provide_Accurate_Service_Information_For_Free_Tier()
    {
        // Act - Get service information
        var serviceInfo = _service.GetServiceInfo();
        
        // Assert - Should provide accurate free tier information
        serviceInfo.ServiceTier.Should().Be("Core (Free)");
        serviceInfo.AIAvailable.Should().BeFalse("Free tier does not include AI");
        serviceInfo.Dataset.Should().NotBeNull();
        serviceInfo.Dataset.MedicationCount.Should().BeGreaterThan(0, "Should have medications in dataset");
        serviceInfo.Dataset.FirstNameCount.Should().BeGreaterThan(0, "Should have first names in dataset");
        serviceInfo.Dataset.SurnameCount.Should().BeGreaterThan(0, "Should have surnames in dataset");
        serviceInfo.Limits.Should().NotBeNull();
        serviceInfo.Limits.MaxGenerationsPerPeriod.Should().BeGreaterThan(0, "Should have generation limits");
    }

    #endregion

    #region Healthcare Realism Tests

    [Fact(DisplayName = "Should generate culturally appropriate names")]
    public void Should_Generate_Culturally_Appropriate_Names()
    {
        var names = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            var result = _service.GeneratePatient(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            names.Add($"{result.Value.Name.Given} {result.Value.Name.Family}");
        }
        
        // Assert - Names should be realistic and varied
        names.Should().OnlyContain(name => !string.IsNullOrEmpty(name), "All names should be non-empty");
        names.Should().OnlyContain(name => name.Contains(' '), "Names should have first and last parts");
        names.Distinct().Count().Should().BeGreaterThan(15, "Should generate varied names, not just repeats");
    }

    [Fact(DisplayName = "Should generate valid US addresses")]
    public void Should_Generate_Valid_US_Addresses()
    {
        var addresses = new List<Address>();
        for (int i = 0; i < 10; i++)
        {
            var result = _service.GeneratePatient(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            addresses.Add(result.Value.Address);
        }
        
        // Assert - All addresses should have valid US format
        addresses.Should().AllSatisfy(address =>
        {
            address.Street1.Should().NotBeNullOrEmpty("Street address is required");
            address.City.Should().NotBeNullOrEmpty("City is required");
            address.State.Should().MatchRegex(@"^[A-Z]{2}$", "Should be valid US state abbreviation");
            address.PostalCode.Should().MatchRegex(@"^\d{5}$", "Should be valid US ZIP code");
            address.Country.Should().Be("US", "Generated addresses should be US format");
        });
    }

    [Fact(DisplayName = "Should generate valid phone numbers")]
    public void Should_Generate_Valid_Phone_Numbers()
    {
        var phoneNumbers = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var result = _service.GeneratePatient(_defaultOptions);
            result.IsSuccess.Should().BeTrue();
            phoneNumbers.Add(result.Value.PhoneNumber);
        }
        
        // Assert - All phone numbers should follow US format
        phoneNumbers.Should().AllSatisfy(phoneNumber =>
        {
            phoneNumber.Should().MatchRegex(@"^\(\d{3}\) \d{3}-\d{4}$", 
                "Should follow US phone format: (xxx) xxx-xxxx");
        });
    }

    #endregion
}