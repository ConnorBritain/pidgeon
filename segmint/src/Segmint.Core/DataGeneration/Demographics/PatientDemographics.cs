// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;

namespace Segmint.Core.DataGeneration.Demographics;

/// <summary>
/// Represents synthetic patient demographic information.
/// </summary>
public class PatientDemographics
{
    /// <summary>
    /// Patient identifier (medical record number).
    /// </summary>
    public string PatientId { get; set; } = "";

    /// <summary>
    /// Patient's first name.
    /// </summary>
    public string FirstName { get; set; } = "";

    /// <summary>
    /// Patient's middle name (optional).
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Patient's last name.
    /// </summary>
    public string LastName { get; set; } = "";

    /// <summary>
    /// Patient's date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Patient's gender (M, F, U, O).
    /// </summary>
    public string Gender { get; set; } = "";

    /// <summary>
    /// Social security number (if enabled).
    /// </summary>
    public string? SocialSecurityNumber { get; set; }

    /// <summary>
    /// Patient's address.
    /// </summary>
    public AddressInfo Address { get; set; } = new();

    /// <summary>
    /// Patient's phone numbers.
    /// </summary>
    public ContactInfo Contact { get; set; } = new();

    /// <summary>
    /// Patient's race/ethnicity.
    /// </summary>
    public string? Race { get; set; }

    /// <summary>
    /// Patient's marital status.
    /// </summary>
    public string? MaritalStatus { get; set; }

    /// <summary>
    /// Patient's primary language.
    /// </summary>
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// Patient account number.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Emergency contact information.
    /// </summary>
    public EmergencyContact? EmergencyContact { get; set; }

    /// <summary>
    /// Gets the patient's age in years.
    /// </summary>
    public int Age => DateTime.Now.Year - DateOfBirth.Year - 
        (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    /// <summary>
    /// Gets the patient's full name.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(MiddleName) 
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    /// <summary>
    /// Gets a formatted display name (Last, First Middle).
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleName}";
}

/// <summary>
/// Address information for a patient.
/// </summary>
public class AddressInfo
{
    /// <summary>
    /// Street address line 1.
    /// </summary>
    public string Street { get; set; } = "";

    /// <summary>
    /// Street address line 2 (apartment, suite, etc.).
    /// </summary>
    public string? Street2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string City { get; set; } = "";

    /// <summary>
    /// State or province.
    /// </summary>
    public string State { get; set; } = "";

    /// <summary>
    /// Postal/ZIP code.
    /// </summary>
    public string PostalCode { get; set; } = "";

    /// <summary>
    /// Country code.
    /// </summary>
    public string Country { get; set; } = "USA";

    /// <summary>
    /// County name.
    /// </summary>
    public string? County { get; set; }

    /// <summary>
    /// Gets the formatted address.
    /// </summary>
    public string FormattedAddress
    {
        get
        {
            var address = Street;
            if (!string.IsNullOrEmpty(Street2))
                address += $", {Street2}";
            address += $", {City}, {State} {PostalCode}";
            if (Country != "USA")
                address += $", {Country}";
            return address;
        }
    }
}

/// <summary>
/// Contact information for a patient.
/// </summary>
public class ContactInfo
{
    /// <summary>
    /// Home phone number.
    /// </summary>
    public string? HomePhone { get; set; }

    /// <summary>
    /// Work phone number.
    /// </summary>
    public string? WorkPhone { get; set; }

    /// <summary>
    /// Mobile phone number.
    /// </summary>
    public string? MobilePhone { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Preferred contact method.
    /// </summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>
    /// Gets the primary phone number (first non-null phone).
    /// </summary>
    public string? PrimaryPhone => HomePhone ?? MobilePhone ?? WorkPhone;
}

/// <summary>
/// Emergency contact information.
/// </summary>
public class EmergencyContact
{
    /// <summary>
    /// Emergency contact's name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Relationship to patient.
    /// </summary>
    public string Relationship { get; set; } = "";

    /// <summary>
    /// Emergency contact's phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>
    /// Emergency contact's address (optional).
    /// </summary>
    public AddressInfo? Address { get; set; }
}

/// <summary>
/// Insurance information for a patient.
/// </summary>
public class InsuranceInfo
{
    /// <summary>
    /// Insurance plan name.
    /// </summary>
    public string PlanName { get; set; } = "";

    /// <summary>
    /// Insurance member ID.
    /// </summary>
    public string MemberId { get; set; } = "";

    /// <summary>
    /// Insurance group number.
    /// </summary>
    public string? GroupNumber { get; set; }

    /// <summary>
    /// Policy holder name (if different from patient).
    /// </summary>
    public string? PolicyHolderName { get; set; }

    /// <summary>
    /// Relationship to policy holder.
    /// </summary>
    public string? RelationshipToHolder { get; set; }

    /// <summary>
    /// Insurance company information.
    /// </summary>
    public InsuranceCompany Company { get; set; } = new();

    /// <summary>
    /// Whether this is the primary insurance.
    /// </summary>
    public bool IsPrimary { get; set; } = true;
}

/// <summary>
/// Insurance company information.
/// </summary>
public class InsuranceCompany
{
    /// <summary>
    /// Company name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Company identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Company address.
    /// </summary>
    public AddressInfo Address { get; set; } = new();

    /// <summary>
    /// Company phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = "";
}