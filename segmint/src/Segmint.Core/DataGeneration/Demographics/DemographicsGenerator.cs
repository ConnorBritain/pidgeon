// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Segmint.Core.DataGeneration.Demographics;

/// <summary>
/// Generates realistic patient demographic data for testing.
/// </summary>
public class DemographicsGenerator : IDataGenerator<PatientDemographics>
{
    private readonly Random _random;
    private readonly DemographicsDataSets _dataSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemographicsGenerator"/> class.
    /// </summary>
    /// <param name="seed">Random seed for reproducible generation.</param>
    public DemographicsGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _dataSets = new DemographicsDataSets();
    }

    /// <inheritdoc />
    public PatientDemographics Generate()
    {
        return Generate(new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public IEnumerable<PatientDemographics> Generate(int count)
    {
        return Generate(count, new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public PatientDemographics Generate(DataGenerationConstraints constraints)
    {
        var gender = GenerateGender();
        var firstName = GenerateFirstName(gender);
        var lastName = GenerateLastName();
        var dateOfBirth = GenerateDateOfBirth(constraints.DateRange);

        var demographics = new PatientDemographics
        {
            PatientId = GeneratePatientId(),
            FirstName = firstName,
            MiddleName = GenerateMiddleName(),
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            SocialSecurityNumber = constraints.IncludeSensitiveData ? GenerateSSN() : null,
            Address = GenerateAddress(),
            Contact = GenerateContact(firstName, lastName),
            Race = GenerateRace(),
            MaritalStatus = GenerateMaritalStatus(),
            PrimaryLanguage = GeneratePrimaryLanguage(),
            AccountNumber = GenerateAccountNumber(),
            EmergencyContact = GenerateEmergencyContact()
        };

        return demographics;
    }

    /// <inheritdoc />
    public IEnumerable<PatientDemographics> Generate(int count, DataGenerationConstraints constraints)
    {
        // Ensure reproducible generation if seed is provided
        var localRandom = constraints.Seed.HasValue 
            ? new Random(constraints.Seed.Value) 
            : _random;

        for (int i = 0; i < count; i++)
        {
            yield return Generate(constraints);
        }
    }

    private string GenerateGender()
    {
        var genders = new[] { "M", "F", "M", "F", "M", "F", "U", "O" }; // Weighted toward M/F
        return genders[_random.Next(genders.Length)];
    }

    private string GenerateFirstName(string gender)
    {
        var names = gender switch
        {
            "M" => _dataSets.MaleFirstNames,
            "F" => _dataSets.FemaleFirstNames,
            _ => _dataSets.MaleFirstNames.Concat(_dataSets.FemaleFirstNames).ToArray()
        };
        return names[_random.Next(names.Length)];
    }

    private string GenerateLastName()
    {
        return _dataSets.LastNames[_random.Next(_dataSets.LastNames.Length)];
    }

    private string? GenerateMiddleName()
    {
        // 70% chance of having a middle name
        if (_random.NextDouble() < 0.7)
        {
            var allFirstNames = _dataSets.MaleFirstNames.Concat(_dataSets.FemaleFirstNames).ToArray();
            return allFirstNames[_random.Next(allFirstNames.Length)];
        }
        return null;
    }

    private DateTime GenerateDateOfBirth(DateRange? range = null)
    {
        var defaultRange = DateRange.AdultBirthDates();
        var dateRange = range ?? defaultRange;
        
        var days = (dateRange.End - dateRange.Start).Days;
        var randomDays = _random.Next(days + 1);
        return dateRange.Start.AddDays(randomDays);
    }

    private string GeneratePatientId()
    {
        // Format: MRN followed by 7-8 digits
        var formats = new[] { "MRN{0:D7}", "PAT{0:D8}", "{0:D9}", "P{0:D7}" };
        var format = formats[_random.Next(formats.Length)];
        var number = _random.Next(1000000, 99999999);
        return string.Format(format, number);
    }

    private string GenerateSSN()
    {
        // Generate realistic but fake SSN (avoid real ranges)
        var area = _random.Next(900, 999); // Use high numbers to avoid real SSNs
        var group = _random.Next(10, 99);
        var serial = _random.Next(1000, 9999);
        return $"{area:D3}-{group:D2}-{serial:D4}";
    }

    private AddressInfo GenerateAddress()
    {
        var streetNumber = _random.Next(1, 9999);
        var streetName = _dataSets.StreetNames[_random.Next(_dataSets.StreetNames.Length)];
        var streetType = _dataSets.StreetTypes[_random.Next(_dataSets.StreetTypes.Length)];
        var cityState = _dataSets.CitiesAndStates[_random.Next(_dataSets.CitiesAndStates.Length)];

        var address = new AddressInfo
        {
            Street = $"{streetNumber} {streetName} {streetType}",
            City = cityState.City,
            State = cityState.State,
            PostalCode = GenerateZipCode(cityState.State),
            Country = "USA"
        };

        // 20% chance of apartment/unit number
        if (_random.NextDouble() < 0.2)
        {
            var unitTypes = new[] { "Apt", "Unit", "Suite", "#" };
            var unitType = unitTypes[_random.Next(unitTypes.Length)];
            var unitNumber = _random.Next(1, 999);
            address.Street2 = $"{unitType} {unitNumber}";
        }

        return address;
    }

    private string GenerateZipCode(string state)
    {
        // Generate realistic ZIP codes based on state
        var stateZipRanges = new Dictionary<string, (int min, int max)>
        {
            ["CA"] = (90000, 96199),
            ["NY"] = (10000, 14999),
            ["TX"] = (75000, 79999),
            ["FL"] = (32000, 34999),
            ["IL"] = (60000, 62999),
            ["PA"] = (15000, 19699),
            ["OH"] = (43000, 45999),
            ["MI"] = (48000, 49999),
            ["NC"] = (27000, 28999),
            ["GA"] = (30000, 31999)
        };

        if (stateZipRanges.TryGetValue(state, out var range))
        {
            var zip = _random.Next(range.min, range.max + 1);
            return $"{zip:D5}";
        }

        // Default ZIP code for other states
        return $"{_random.Next(10000, 99999):D5}";
    }

    private ContactInfo GenerateContact(string firstName, string lastName)
    {
        var contact = new ContactInfo();

        // 90% chance of home phone
        if (_random.NextDouble() < 0.9)
            contact.HomePhone = GeneratePhoneNumber();

        // 30% chance of work phone
        if (_random.NextDouble() < 0.3)
            contact.WorkPhone = GeneratePhoneNumber();

        // 80% chance of mobile phone
        if (_random.NextDouble() < 0.8)
            contact.MobilePhone = GeneratePhoneNumber();

        // 60% chance of email
        if (_random.NextDouble() < 0.6)
            contact.Email = GenerateEmail(firstName, lastName);

        return contact;
    }

    private string GeneratePhoneNumber()
    {
        var areaCode = _random.Next(200, 999);
        var exchange = _random.Next(200, 999);
        var number = _random.Next(1000, 9999);
        return $"({areaCode:D3}) {exchange:D3}-{number:D4}";
    }

    private string GenerateEmail(string firstName, string lastName)
    {
        var domains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "aol.com", "icloud.com" };
        var domain = domains[_random.Next(domains.Length)];
        
        var emailFormats = new[]
        {
            $"{firstName.ToLower()}.{lastName.ToLower()}@{domain}",
            $"{firstName.ToLower()}{lastName.ToLower()}@{domain}",
            $"{firstName.ToLower()}{_random.Next(10, 99)}@{domain}",
            $"{firstName.ToLower().Substring(0, 1)}{lastName.ToLower()}@{domain}"
        };

        return emailFormats[_random.Next(emailFormats.Length)];
    }

    private string GenerateRace()
    {
        var races = new[]
        {
            "White", "Black or African American", "Asian", "American Indian or Alaska Native",
            "Native Hawaiian or Other Pacific Islander", "Other", "Unknown", "Hispanic or Latino"
        };
        return races[_random.Next(races.Length)];
    }

    private string GenerateMaritalStatus()
    {
        var statuses = new[] { "Single", "Married", "Divorced", "Widowed", "Separated", "Other" };
        return statuses[_random.Next(statuses.Length)];
    }

    private string GeneratePrimaryLanguage()
    {
        var languages = new[] { "English", "Spanish", "Chinese", "French", "German", "Italian", "Portuguese", "Russian" };
        return languages[_random.Next(languages.Length)];
    }

    private string GenerateAccountNumber()
    {
        var formats = new[] { "ACC{0:D8}", "A{0:D9}", "{0:D10}" };
        var format = formats[_random.Next(formats.Length)];
        var number = _random.Next(10000000, 999999999);
        return string.Format(format, number);
    }

    private EmergencyContact GenerateEmergencyContact()
    {
        var relationships = new[] { "Spouse", "Parent", "Child", "Sibling", "Friend", "Other" };
        var relationship = relationships[_random.Next(relationships.Length)];
        
        var gender = GenerateGender();
        var firstName = GenerateFirstName(gender);
        var lastName = GenerateLastName();

        return new EmergencyContact
        {
            Name = $"{firstName} {lastName}",
            Relationship = relationship,
            PhoneNumber = GeneratePhoneNumber()
        };
    }
}

/// <summary>
/// Contains realistic data sets for demographic generation.
/// </summary>
internal class DemographicsDataSets
{
    public string[] MaleFirstNames { get; } = new[]
    {
        "James", "Robert", "John", "Michael", "David", "William", "Richard", "Thomas", "Christopher", "Daniel",
        "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua", "Kenneth", "Kevin",
        "Brian", "George", "Timothy", "Ronald", "Jason", "Edward", "Jeffrey", "Ryan", "Jacob", "Gary",
        "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Samuel"
    };

    public string[] FemaleFirstNames { get; } = new[]
    {
        "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen",
        "Lisa", "Nancy", "Betty", "Helen", "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle",
        "Laura", "Sarah", "Kimberly", "Deborah", "Dorothy", "Lisa", "Nancy", "Karen", "Betty", "Helen",
        "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle", "Laura", "Sarah", "Kimberly", "Amy"
    };

    public string[] LastNames { get; } = new[]
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores"
    };

    public string[] StreetNames { get; } = new[]
    {
        "Main", "Oak", "Pine", "Maple", "Cedar", "Elm", "Washington", "Lake", "Hill", "Park",
        "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth",
        "Lincoln", "Madison", "Jefferson", "Franklin", "Church", "Mill", "School", "High", "Spring", "Center"
    };

    public string[] StreetTypes { get; } = new[]
    {
        "St", "Ave", "Dr", "Ln", "Rd", "Blvd", "Ct", "Pl", "Way", "Cir", "Pkwy", "Ter"
    };

    public (string City, string State)[] CitiesAndStates { get; } = new[]
    {
        ("New York", "NY"), ("Los Angeles", "CA"), ("Chicago", "IL"), ("Houston", "TX"), ("Phoenix", "AZ"),
        ("Philadelphia", "PA"), ("San Antonio", "TX"), ("San Diego", "CA"), ("Dallas", "TX"), ("San Jose", "CA"),
        ("Austin", "TX"), ("Jacksonville", "FL"), ("Fort Worth", "TX"), ("Columbus", "OH"), ("Charlotte", "NC"),
        ("San Francisco", "CA"), ("Indianapolis", "IN"), ("Seattle", "WA"), ("Denver", "CO"), ("Washington", "DC"),
        ("Boston", "MA"), ("El Paso", "TX"), ("Nashville", "TN"), ("Detroit", "MI"), ("Oklahoma City", "OK"),
        ("Portland", "OR"), ("Las Vegas", "NV"), ("Memphis", "TN"), ("Louisville", "KY"), ("Baltimore", "MD"),
        ("Milwaukee", "WI"), ("Albuquerque", "NM"), ("Tucson", "AZ"), ("Fresno", "CA"), ("Sacramento", "CA"),
        ("Mesa", "AZ"), ("Kansas City", "MO"), ("Atlanta", "GA"), ("Omaha", "NE"), ("Colorado Springs", "CO")
    };
}