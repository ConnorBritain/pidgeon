// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Patient Allergy Information (AL1) segment.
/// This segment contains patient allergy information including allergens, severity, and reactions.
/// Used in ADT messages and other patient-related message types.
/// </summary>
public class AL1Segment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "AL1";

    /// <summary>
    /// Initializes a new instance of the <see cref="AL1Segment"/> class.
    /// </summary>
    public AL1Segment()
    {
    }

    /// <summary>
    /// Gets or sets the set ID (AL1.1) - Required.
    /// Sequential number identifying the allergy record.
    /// </summary>
    public SequenceIdField SetId
    {
        get => this[1] as SequenceIdField ?? new SequenceIdField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the allergen type code (AL1.2).
    /// Type of allergen: DA=Drug allergy, FA=Food allergy, MA=Miscellaneous allergy, MC=Miscellaneous contraindication.
    /// </summary>
    public CodedElementField AllergenTypeCode
    {
        get => this[2] as CodedElementField ?? new CodedElementField();
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the allergen code/mnemonic/description (AL1.3) - Required.
    /// Specific allergen identifier and description.
    /// </summary>
    public CodedElementField AllergenCodeMnemonicDescription
    {
        get => this[3] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the allergy severity code (AL1.4).
    /// Severity of the allergic reaction: SV=Severe, MO=Moderate, MI=Mild, U=Unknown.
    /// </summary>
    public CodedElementField AllergySeverityCode
    {
        get => this[4] as CodedElementField ?? new CodedElementField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the allergy reaction code (AL1.5).
    /// Description of the allergic reaction or symptoms.
    /// </summary>
    public StringField AllergyReactionCode
    {
        get => this[5] as StringField ?? new StringField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the identification date (AL1.6).
    /// Date when the allergy was first identified or documented.
    /// </summary>
    public DateField IdentificationDate
    {
        get => this[6] as DateField ?? new DateField();
        set => this[6] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // Initialize with default set ID
        SetId.SetValue("1");
    }

    /// <summary>
    /// Sets basic allergy information.
    /// </summary>
    /// <param name="setId">Set ID for this allergy record.</param>
    /// <param name="allergenType">Type of allergen (DA, FA, MA, MC).</param>
    /// <param name="allergenCode">Allergen code.</param>
    /// <param name="allergenDescription">Allergen description.</param>
    /// <param name="severity">Severity code (SV, MO, MI, U).</param>
    /// <param name="reaction">Reaction description.</param>
    /// <param name="identificationDate">Date allergy was identified.</param>
    public void SetBasicAllergy(
        int setId,
        string allergenType,
        string allergenCode,
        string allergenDescription,
        string? severity = null,
        string? reaction = null,
        DateTime? identificationDate = null)
    {
        SetId.SetValue(setId.ToString());
        AllergenTypeCode.SetComponents(allergenType, GetAllergenTypeDescription(allergenType), "HL70127");
        AllergenCodeMnemonicDescription.SetComponents(allergenCode, allergenDescription);
        
        if (!string.IsNullOrEmpty(severity))
        {
            AllergySeverityCode.SetComponents(severity, GetSeverityDescription(severity), "HL70128");
        }

        if (!string.IsNullOrEmpty(reaction))
        {
            AllergyReactionCode.SetValue(reaction);
        }

        if (identificationDate.HasValue)
        {
            IdentificationDate.SetValue(identificationDate.Value.ToString("yyyyMMdd"));
        }
    }

    /// <summary>
    /// Sets a drug allergy.
    /// </summary>
    /// <param name="setId">Set ID for this allergy record.</param>
    /// <param name="drugCode">Drug code (e.g., NDC, RxNorm).</param>
    /// <param name="drugName">Drug name.</param>
    /// <param name="severity">Severity of the allergy.</param>
    /// <param name="reaction">Description of the reaction.</param>
    /// <param name="identificationDate">Date the allergy was identified.</param>
    public void SetDrugAllergy(
        int setId,
        string drugCode,
        string drugName,
        AllergySeverity severity = AllergySeverity.Unknown,
        string? reaction = null,
        DateTime? identificationDate = null)
    {
        var severityCode = GetSeverityCode(severity);
        SetBasicAllergy(setId, "DA", drugCode, drugName, severityCode, reaction, identificationDate);
    }

    /// <summary>
    /// Sets a food allergy.
    /// </summary>
    /// <param name="setId">Set ID for this allergy record.</param>
    /// <param name="foodCode">Food allergen code.</param>
    /// <param name="foodName">Food allergen name.</param>
    /// <param name="severity">Severity of the allergy.</param>
    /// <param name="reaction">Description of the reaction.</param>
    /// <param name="identificationDate">Date the allergy was identified.</param>
    public void SetFoodAllergy(
        int setId,
        string foodCode,
        string foodName,
        AllergySeverity severity = AllergySeverity.Unknown,
        string? reaction = null,
        DateTime? identificationDate = null)
    {
        var severityCode = GetSeverityCode(severity);
        SetBasicAllergy(setId, "FA", foodCode, foodName, severityCode, reaction, identificationDate);
    }

    /// <summary>
    /// Sets an environmental allergy.
    /// </summary>
    /// <param name="setId">Set ID for this allergy record.</param>
    /// <param name="allergenCode">Environmental allergen code.</param>
    /// <param name="allergenName">Environmental allergen name.</param>
    /// <param name="severity">Severity of the allergy.</param>
    /// <param name="reaction">Description of the reaction.</param>
    /// <param name="identificationDate">Date the allergy was identified.</param>
    public void SetEnvironmentalAllergy(
        int setId,
        string allergenCode,
        string allergenName,
        AllergySeverity severity = AllergySeverity.Unknown,
        string? reaction = null,
        DateTime? identificationDate = null)
    {
        var severityCode = GetSeverityCode(severity);
        SetBasicAllergy(setId, "MA", allergenCode, allergenName, severityCode, reaction, identificationDate);
    }

    /// <summary>
    /// Sets a contraindication (non-allergy adverse reaction).
    /// </summary>
    /// <param name="setId">Set ID for this contraindication record.</param>
    /// <param name="substanceCode">Substance code.</param>
    /// <param name="substanceName">Substance name.</param>
    /// <param name="severity">Severity of the contraindication.</param>
    /// <param name="reaction">Description of the adverse reaction.</param>
    /// <param name="identificationDate">Date the contraindication was identified.</param>
    public void SetContraindication(
        int setId,
        string substanceCode,
        string substanceName,
        AllergySeverity severity = AllergySeverity.Unknown,
        string? reaction = null,
        DateTime? identificationDate = null)
    {
        var severityCode = GetSeverityCode(severity);
        SetBasicAllergy(setId, "MC", substanceCode, substanceName, severityCode, reaction, identificationDate);
    }

    /// <summary>
    /// Gets a display-friendly representation of the allergy.
    /// </summary>
    /// <returns>Formatted allergy string.</returns>
    public string GetDisplayValue()
    {
        var allergenType = GetAllergenTypeDescription(AllergenTypeCode.Identifier ?? "");
        var allergen = AllergenCodeMnemonicDescription.Text ?? AllergenCodeMnemonicDescription.Identifier ?? "Unknown";
        var severity = AllergySeverityCode.Text ?? AllergySeverityCode.Identifier ?? "";
        var reaction = AllergyReactionCode.Value ?? "";

        var result = $"{allergenType}: {allergen}";
        
        if (!string.IsNullOrEmpty(severity))
            result += $" (Severity: {severity})";
            
        if (!string.IsNullOrEmpty(reaction))
            result += $" - {reaction}";

        return result;
    }

    /// <summary>
    /// Determines if this is a severe allergy.
    /// </summary>
    /// <returns>True if the allergy is marked as severe.</returns>
    public bool IsSevere()
    {
        return AllergySeverityCode.Identifier?.ToUpperInvariant() == "SV";
    }

    /// <summary>
    /// Gets the allergy type.
    /// </summary>
    /// <returns>Allergy type enumeration.</returns>
    public AllergyType GetAllergyType()
    {
        return AllergenTypeCode.Identifier?.ToUpperInvariant() switch
        {
            "DA" => AllergyType.Drug,
            "FA" => AllergyType.Food,
            "MA" => AllergyType.Environmental,
            "MC" => AllergyType.Contraindication,
            _ => AllergyType.Unknown
        };
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(SetId.Value))
            errors.Add("Set ID (AL1.1) is required");

        if (string.IsNullOrEmpty(AllergenCodeMnemonicDescription.Identifier) && 
            string.IsNullOrEmpty(AllergenCodeMnemonicDescription.Text))
            errors.Add("Allergen Code/Mnemonic/Description (AL1.3) is required");

        // Validate allergen type code
        var validAllergenTypes = new[] { "DA", "FA", "MA", "MC" };
        if (!string.IsNullOrEmpty(AllergenTypeCode.Identifier) && 
            !validAllergenTypes.Contains(AllergenTypeCode.Identifier.ToUpperInvariant()))
        {
            errors.Add($"Invalid allergen type code: {AllergenTypeCode.Identifier}. Valid values: {string.Join(", ", validAllergenTypes)}");
        }

        // Validate severity code
        var validSeverityCodes = new[] { "SV", "MO", "MI", "U" };
        if (!string.IsNullOrEmpty(AllergySeverityCode.Identifier) && 
            !validSeverityCodes.Contains(AllergySeverityCode.Identifier.ToUpperInvariant()))
        {
            errors.Add($"Invalid allergy severity code: {AllergySeverityCode.Identifier}. Valid values: {string.Join(", ", validSeverityCodes)}");
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new AL1Segment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    private static string GetAllergenTypeDescription(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "DA" => "Drug allergy",
            "FA" => "Food allergy",
            "MA" => "Miscellaneous allergy",
            "MC" => "Miscellaneous contraindication",
            _ => ""
        };
    }

    private static string GetSeverityDescription(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "SV" => "Severe",
            "MO" => "Moderate",
            "MI" => "Mild",
            "U" => "Unknown",
            _ => ""
        };
    }

    private static string GetSeverityCode(AllergySeverity severity)
    {
        return severity switch
        {
            AllergySeverity.Severe => "SV",
            AllergySeverity.Moderate => "MO",
            AllergySeverity.Mild => "MI",
            AllergySeverity.Unknown => "U",
            _ => "U"
        };
    }

    /// <summary>
    /// Creates a drug allergy AL1 segment.
    /// </summary>
    /// <param name="setId">Set ID.</param>
    /// <param name="drugName">Drug name.</param>
    /// <param name="severity">Severity level.</param>
    /// <param name="reaction">Reaction description.</param>
    /// <returns>Configured AL1 segment.</returns>
    public static AL1Segment CreateDrugAllergy(int setId, string drugName, AllergySeverity severity = AllergySeverity.Unknown, string? reaction = null)
    {
        var al1 = new AL1Segment();
        al1.SetDrugAllergy(setId, drugName.ToUpperInvariant().Replace(" ", ""), drugName, severity, reaction);
        return al1;
    }

    /// <summary>
    /// Creates a food allergy AL1 segment.
    /// </summary>
    /// <param name="setId">Set ID.</param>
    /// <param name="foodName">Food allergen name.</param>
    /// <param name="severity">Severity level.</param>
    /// <param name="reaction">Reaction description.</param>
    /// <returns>Configured AL1 segment.</returns>
    public static AL1Segment CreateFoodAllergy(int setId, string foodName, AllergySeverity severity = AllergySeverity.Unknown, string? reaction = null)
    {
        var al1 = new AL1Segment();
        al1.SetFoodAllergy(setId, foodName.ToUpperInvariant().Replace(" ", ""), foodName, severity, reaction);
        return al1;
    }

    /// <summary>
    /// Creates an environmental allergy AL1 segment.
    /// </summary>
    /// <param name="setId">Set ID.</param>
    /// <param name="allergenName">Environmental allergen name.</param>
    /// <param name="severity">Severity level.</param>
    /// <param name="reaction">Reaction description.</param>
    /// <returns>Configured AL1 segment.</returns>
    public static AL1Segment CreateEnvironmentalAllergy(int setId, string allergenName, AllergySeverity severity = AllergySeverity.Unknown, string? reaction = null)
    {
        var al1 = new AL1Segment();
        al1.SetEnvironmentalAllergy(setId, allergenName.ToUpperInvariant().Replace(" ", ""), allergenName, severity, reaction);
        return al1;
    }
}

/// <summary>
/// Types of allergies and contraindications.
/// </summary>
public enum AllergyType
{
    Unknown,
    Drug,
    Food,
    Environmental,
    Contraindication
}

/// <summary>
/// Severity levels for allergies and adverse reactions.
/// </summary>
public enum AllergySeverity
{
    Unknown,
    Mild,
    Moderate,
    Severe
}
