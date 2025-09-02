// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;

namespace Pidgeon.Core.Application.DTOs;

/// <summary>
/// Conversion utilities for Clinical entities to DTOs.
/// Implements hybrid DTO strategy with shared core DTOs and standard-specific extensions.
/// </summary>
public static class DtoConversions
{
    public static PatientDto ToDto(this Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            Name = patient.Name.ToDto(),
            BirthDate = patient.BirthDate,
            Gender = patient.Gender?.ToDto(),
            Address = patient.Address?.ToDto(),
            PhoneNumber = patient.PhoneNumber,
            Race = patient.Race,
            PrimaryLanguage = patient.PrimaryLanguage,
            MaritalStatus = patient.MaritalStatus?.ToDto(),
            SocialSecurityNumber = patient.SocialSecurityNumber,
            Ethnicity = patient.Ethnicity
        };
    }
    
    public static PersonNameDto ToDto(this PersonName personName)
    {
        return new PersonNameDto
        {
            LastName = personName.Family ?? "",
            FirstName = personName.Given ?? "",
            MiddleName = personName.Middle,
            Suffix = personName.Suffix,
            Prefix = personName.Prefix
        };
    }
    
    public static AddressDto ToDto(this Address address)
    {
        return new AddressDto
        {
            Street1 = address.Street1,
            Street2 = address.Street2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country
        };
    }
    
    public static EncounterDto ToDto(this Encounter encounter)
    {
        return new EncounterDto
        {
            Id = encounter.Id,
            Type = encounter.Type.ToDto(),
            Location = encounter.Location,
            StartTime = encounter.StartTime,
            EndTime = encounter.EndTime,
            Provider = encounter.Provider?.ToDto()
        };
    }
    
    public static ProviderDto ToDto(this Provider provider)
    {
        return new ProviderDto
        {
            Id = provider.Id,
            Name = provider.Name.ToDto(),
            LicenseNumber = provider.LicenseNumber,
            DeaNumber = provider.DeaNumber,
            Specialty = provider.Specialty,
            Address = provider.Address?.ToDto(),
            PhoneNumber = provider.PhoneNumber
        };
    }
    
    public static GenderDto ToDto(this Gender gender)
    {
        return gender switch
        {
            Gender.Male => GenderDto.Male,
            Gender.Female => GenderDto.Female,
            Gender.Other => GenderDto.Other,
            Gender.Unknown => GenderDto.Unknown,
            _ => GenderDto.Unknown
        };
    }
    
    public static MaritalStatusDto ToDto(this MaritalStatus maritalStatus)
    {
        return maritalStatus switch
        {
            MaritalStatus.Single => MaritalStatusDto.Single,
            MaritalStatus.Married => MaritalStatusDto.Married,
            MaritalStatus.Divorced => MaritalStatusDto.Divorced,
            MaritalStatus.Widowed => MaritalStatusDto.Widowed,
            MaritalStatus.Separated => MaritalStatusDto.Separated,
            MaritalStatus.Unknown => MaritalStatusDto.Unknown,
            _ => MaritalStatusDto.Unknown
        };
    }
    
    public static EncounterTypeDto ToDto(this EncounterType encounterType)
    {
        return encounterType switch
        {
            EncounterType.Inpatient => EncounterTypeDto.Inpatient,
            EncounterType.Outpatient => EncounterTypeDto.Outpatient,
            EncounterType.Emergency => EncounterTypeDto.Emergency,
            EncounterType.Observation => EncounterTypeDto.Observation,
            EncounterType.DaySurgery => EncounterTypeDto.DaySurgery,
            EncounterType.Telemedicine => EncounterTypeDto.Telemedicine,
            EncounterType.HomeHealth => EncounterTypeDto.HomeHealth,
            EncounterType.PreAdmission => EncounterTypeDto.PreAdmission,
            _ => EncounterTypeDto.Outpatient
        };
    }
}