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
/// Represents an HL7 Observation/Result (OBX) segment.
/// This segment is used to transmit individual observations or results.
/// Used in many message types including ORU (Lab Results), ADT (Patient observations), and others.
/// </summary>
public class OBXSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "OBX";

    /// <summary>
    /// Initializes a new instance of the <see cref="OBXSegment"/> class.
    /// </summary>
    public OBXSegment()
    {
    }

    /// <summary>
    /// Gets or sets the set ID (OBX.1) - Required.
    /// Sequential number of the observation within a group.
    /// </summary>
    public SequenceIdField SetId
    {
        get => this[1] as SequenceIdField ?? new SequenceIdField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the value type (OBX.2) - Required.
    /// Defines the data type of the observation value.
    /// Common values: CE (Coded Element), NM (Numeric), ST (String Text), TX (Text), etc.
    /// </summary>
    public IdentifierField ValueType
    {
        get => this[2] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the observation identifier (OBX.3) - Required.
    /// Identifies what is being observed (e.g., lab test, vital sign).
    /// Usually contains LOINC codes for standardization.
    /// </summary>
    public CodedElementField ObservationIdentifier
    {
        get => this[3] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the observation sub-ID (OBX.4).
    /// Used when multiple observations of the same type are reported.
    /// </summary>
    public StringField ObservationSubId
    {
        get => this[4] as StringField ?? new StringField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the observation value (OBX.5).
    /// The actual result/observation data. Data type varies based on ValueType.
    /// </summary>
    public HL7Field ObservationValue
    {
        get => this[5] ?? new StringField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the units (OBX.6).
    /// Units of measure for the observation value.
    /// Should use UCUM (Unified Code for Units of Measure) when possible.
    /// </summary>
    public CodedElementField Units
    {
        get => this[6] as CodedElementField ?? new CodedElementField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the reference range (OBX.7).
    /// Normal range or reference values for the observation.
    /// </summary>
    public StringField ReferenceRange
    {
        get => this[7] as StringField ?? new StringField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the abnormal flags (OBX.8).
    /// Indicates if the result is abnormal (H=High, L=Low, A=Abnormal, etc.).
    /// </summary>
    public CodedValueField AbnormalFlags
    {
        get => this[8] as CodedValueField ?? new CodedValueField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the probability (OBX.9).
    /// Statistical probability of the observation.
    /// </summary>
    public NumericField Probability
    {
        get => this[9] as NumericField ?? new NumericField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the nature of abnormal test (OBX.10).
    /// Describes the nature of abnormal results.
    /// </summary>
    public CodedValueField NatureOfAbnormalTest
    {
        get => this[10] as CodedValueField ?? new CodedValueField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the observation result status (OBX.11) - Required.
    /// Status of the result: F=Final, P=Preliminary, C=Corrected, etc.
    /// </summary>
    public IdentifierField ObservationResultStatus
    {
        get => this[11] as IdentifierField ?? new IdentifierField(value: "F", isRequired: true);
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the effective date of reference range (OBX.12).
    /// When the reference range values were established.
    /// </summary>
    public TimestampField EffectiveDateOfReferenceRange
    {
        get => this[12] as TimestampField ?? new TimestampField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the user defined access checks (OBX.13).
    /// Security or access control information.
    /// </summary>
    public StringField UserDefinedAccessChecks
    {
        get => this[13] as StringField ?? new StringField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the date/time of observation (OBX.14).
    /// When the observation was made or specimen collected.
    /// </summary>
    public TimestampField DateTimeOfObservation
    {
        get => this[14] as TimestampField ?? new TimestampField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the producer's ID (OBX.15).
    /// Identifies who or what produced the observation.
    /// </summary>
    public CodedElementField ProducerId
    {
        get => this[15] as CodedElementField ?? new CodedElementField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the responsible observer (OBX.16).
    /// Person responsible for the observation.
    /// </summary>
    public ExtendedCompositeIdField ResponsibleObserver
    {
        get => this[16] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the observation method (OBX.17).
    /// Method used to obtain the observation.
    /// </summary>
    public CodedElementField ObservationMethod
    {
        get => this[17] as CodedElementField ?? new CodedElementField();
        set => this[17] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // Initialize required fields with default values
        SetId.SetValue("1");
        ValueType.SetValue("ST"); // Default to string text
        ObservationResultStatus.SetValue("F"); // Default to final
    }

    /// <summary>
    /// Sets basic observation information.
    /// </summary>
    /// <param name="setId">Set ID for this observation.</param>
    /// <param name="valueType">Type of the observation value.</param>
    /// <param name="observationCode">Code identifying the observation.</param>
    /// <param name="observationName">Name/description of the observation.</param>
    /// <param name="observationValue">The actual observation value.</param>
    /// <param name="units">Units of measure.</param>
    /// <param name="resultStatus">Status of the result (F=Final, P=Preliminary).</param>
    public void SetBasicObservation(
        int setId,
        string valueType,
        string observationCode,
        string observationName,
        string observationValue,
        string? units = null,
        string resultStatus = "F")
    {
        SetId.SetValue(setId.ToString());
        ValueType.SetValue(valueType);
        ObservationIdentifier.SetComponents(observationCode, observationName, "LN"); // Default to LOINC
        
        // Set value based on type
        ObservationValue = valueType.ToUpperInvariant() switch
        {
            "NM" => new NumericField(observationValue),
            "CE" => new CodedElementField(observationValue),
            "ST" => new StringField(observationValue),
            "TX" => new TextField(observationValue),
            "TS" => new TimestampField(observationValue),
            _ => new StringField(observationValue)
        };

        if (!string.IsNullOrEmpty(units))
        {
            Units.SetComponents(units, "", "UCUM"); // Default to UCUM units
        }

        ObservationResultStatus.SetValue(resultStatus);
    }

    /// <summary>
    /// Sets a numeric lab result with reference range.
    /// </summary>
    /// <param name="setId">Set ID for this observation.</param>
    /// <param name="testCode">Lab test code (usually LOINC).</param>
    /// <param name="testName">Lab test name.</param>
    /// <param name="numericValue">Numeric result value.</param>
    /// <param name="units">Units of measure.</param>
    /// <param name="referenceRange">Normal reference range.</param>
    /// <param name="abnormalFlag">Abnormal flag if applicable (H, L, A, etc.).</param>
    /// <param name="resultStatus">Result status.</param>
    /// <param name="observationDateTime">When observation was made.</param>
    public void SetNumericLabResult(
        int setId,
        string testCode,
        string testName,
        decimal numericValue,
        string units,
        string? referenceRange = null,
        string? abnormalFlag = null,
        string resultStatus = "F",
        DateTime? observationDateTime = null)
    {
        SetBasicObservation(setId, "NM", testCode, testName, numericValue.ToString(), units, resultStatus);
        
        if (!string.IsNullOrEmpty(referenceRange))
            ReferenceRange.SetValue(referenceRange);

        if (!string.IsNullOrEmpty(abnormalFlag))
            AbnormalFlags.SetValue(abnormalFlag);

        if (observationDateTime.HasValue)
            DateTimeOfObservation.SetValue(observationDateTime.Value.ToString("yyyyMMddHHmmss"));
    }

    /// <summary>
    /// Sets a coded lab result (e.g., positive/negative, present/absent).
    /// </summary>
    /// <param name="setId">Set ID for this observation.</param>
    /// <param name="testCode">Lab test code.</param>
    /// <param name="testName">Lab test name.</param>
    /// <param name="resultCode">Result code.</param>
    /// <param name="resultText">Result description.</param>
    /// <param name="codingSystem">Coding system for result.</param>
    /// <param name="resultStatus">Result status.</param>
    /// <param name="observationDateTime">When observation was made.</param>
    public void SetCodedLabResult(
        int setId,
        string testCode,
        string testName,
        string resultCode,
        string resultText,
        string codingSystem = "L",
        string resultStatus = "F",
        DateTime? observationDateTime = null)
    {
        SetId.SetValue(setId.ToString());
        ValueType.SetValue("CE");
        ObservationIdentifier.SetComponents(testCode, testName, "LN");
        
        var codedResult = new CodedElementField();
        codedResult.SetComponents(resultCode, resultText, codingSystem);
        ObservationValue = codedResult;

        ObservationResultStatus.SetValue(resultStatus);

        if (observationDateTime.HasValue)
            DateTimeOfObservation.SetValue(observationDateTime.Value.ToString("yyyyMMddHHmmss"));
    }

    /// <summary>
    /// Sets a text-based observation (e.g., clinical notes, impressions).
    /// </summary>
    /// <param name="setId">Set ID for this observation.</param>
    /// <param name="observationCode">Observation code.</param>
    /// <param name="observationName">Observation name.</param>
    /// <param name="textValue">Text observation value.</param>
    /// <param name="resultStatus">Result status.</param>
    /// <param name="observationDateTime">When observation was made.</param>
    public void SetTextObservation(
        int setId,
        string observationCode,
        string observationName,
        string textValue,
        string resultStatus = "F",
        DateTime? observationDateTime = null)
    {
        SetBasicObservation(setId, "TX", observationCode, observationName, textValue, null, resultStatus);

        if (observationDateTime.HasValue)
            DateTimeOfObservation.SetValue(observationDateTime.Value.ToString("yyyyMMddHHmmss"));
    }

    /// <summary>
    /// Sets vital signs observation.
    /// </summary>
    /// <param name="setId">Set ID for this observation.</param>
    /// <param name="vitalSignType">Type of vital sign.</param>
    /// <param name="value">Vital sign value.</param>
    /// <param name="units">Units of measure.</param>
    /// <param name="observationDateTime">When vital was taken.</param>
    public void SetVitalSign(
        int setId,
        VitalSignType vitalSignType,
        decimal value,
        string units,
        DateTime? observationDateTime = null)
    {
        var (code, name, defaultUnits, normalRange) = GetVitalSignInfo(vitalSignType);
        
        SetNumericLabResult(
            setId, 
            code, 
            name, 
            value, 
            units ?? defaultUnits, 
            normalRange,
            null, // Let abnormal flag be determined by range
            "F",
            observationDateTime
        );
    }

    /// <summary>
    /// Sets observation method information.
    /// </summary>
    /// <param name="methodCode">Method code.</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="codingSystem">Coding system.</param>
    public void SetObservationMethod(string methodCode, string methodName, string codingSystem = "L")
    {
        ObservationMethod.SetComponents(methodCode, methodName, codingSystem);
    }

    /// <summary>
    /// Sets the responsible observer/performer.
    /// </summary>
    /// <param name="observerId">Observer ID.</param>
    /// <param name="observerName">Observer name.</param>
    /// <param name="observerType">Type of observer.</param>
    public void SetResponsibleObserver(string observerId, string observerName, string? observerType = null)
    {
        var observerValue = string.IsNullOrEmpty(observerType) ? $"{observerId}^{observerName}" : $"{observerId}^{observerName}^{observerType}";
        ResponsibleObserver.SetValue(observerValue);
    }

    /// <summary>
    /// Determines if the observation value is abnormal based on reference range.
    /// </summary>
    /// <returns>True if abnormal, false if normal or cannot determine.</returns>
    public bool IsAbnormal()
    {
        return !string.IsNullOrEmpty(AbnormalFlags.Value) && 
               AbnormalFlags.Value != "N" && 
               AbnormalFlags.Value != "Normal";
    }

    /// <summary>
    /// Gets a display-friendly representation of the observation.
    /// </summary>
    /// <returns>Formatted observation string.</returns>
    public string GetDisplayValue()
    {
        var identifier = ObservationIdentifier.Text ?? ObservationIdentifier.Identifier ?? "Unknown";
        var value = ObservationValue?.ToString() ?? "";
        var units = Units.Identifier ?? "";
        var abnormal = IsAbnormal() ? $" [{AbnormalFlags.Value}]" : "";

        return string.IsNullOrEmpty(units) 
            ? $"{identifier}: {value}{abnormal}"
            : $"{identifier}: {value} {units}{abnormal}";
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(SetId.Value))
            errors.Add("Set ID (OBX.1) is required");

        if (string.IsNullOrEmpty(ValueType.Value))
            errors.Add("Value Type (OBX.2) is required");

        if (string.IsNullOrEmpty(ObservationIdentifier.Identifier))
            errors.Add("Observation Identifier (OBX.3) is required");

        if (string.IsNullOrEmpty(ObservationResultStatus.Value))
            errors.Add("Observation Result Status (OBX.11) is required");

        // Validate value type consistency
        if (!string.IsNullOrEmpty(ValueType.Value))
        {
            var expectedFieldType = ValueType.Value.ToUpperInvariant() switch
            {
                "NM" => typeof(NumericField),
                "CE" => typeof(CodedElementField),
                "ST" => typeof(StringField),
                "TX" => typeof(TextField),
                "TS" => typeof(TimestampField),
                _ => null
            };

            if (expectedFieldType != null && ObservationValue?.GetType() != expectedFieldType)
            {
                errors.Add($"Observation value type mismatch: Expected {expectedFieldType.Name} for value type {ValueType.Value}");
            }
        }

        // Validate result status
        var validStatuses = new[] { "F", "P", "C", "X", "I", "R", "S", "N" };
        if (!string.IsNullOrEmpty(ObservationResultStatus.Value) && 
            !validStatuses.Contains(ObservationResultStatus.Value.ToUpperInvariant()))
        {
            errors.Add($"Invalid observation result status: {ObservationResultStatus.Value}. Valid values: {string.Join(", ", validStatuses)}");
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new OBXSegment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    private static (string code, string name, string units, string range) GetVitalSignInfo(VitalSignType vitalSignType)
    {
        return vitalSignType switch
        {
            VitalSignType.Temperature => ("8310-5", "Body temperature", "Cel", "36.1-37.2"),
            VitalSignType.BloodPressureSystolic => ("8480-6", "Systolic blood pressure", "mm[Hg]", "90-140"),
            VitalSignType.BloodPressureDiastolic => ("8462-4", "Diastolic blood pressure", "mm[Hg]", "60-90"),
            VitalSignType.HeartRate => ("8867-4", "Heart rate", "/min", "60-100"),
            VitalSignType.RespiratoryRate => ("9279-1", "Respiratory rate", "/min", "12-20"),
            VitalSignType.OxygenSaturation => ("2708-6", "Oxygen saturation", "%", "95-100"),
            VitalSignType.Height => ("8302-2", "Body height", "cm", ""),
            VitalSignType.Weight => ("29463-7", "Body weight", "kg", ""),
            VitalSignType.BMI => ("39156-5", "Body mass index", "kg/m2", "18.5-25"),
            VitalSignType.PainScale => ("72133-2", "Pain severity", "1", "0-10"),
            _ => ("", "Unknown vital sign", "", "")
        };
    }

    /// <summary>
    /// Creates a basic lab result OBX segment.
    /// </summary>
    /// <param name="setId">Set ID.</param>
    /// <param name="testCode">Test code.</param>
    /// <param name="testName">Test name.</param>
    /// <param name="result">Result value.</param>
    /// <param name="units">Units.</param>
    /// <param name="referenceRange">Reference range.</param>
    /// <returns>Configured OBX segment.</returns>
    public static OBXSegment CreateLabResult(int setId, string testCode, string testName, string result, string? units = null, string? referenceRange = null)
    {
        var obx = new OBXSegment();
        
        // Determine if result is numeric
        if (decimal.TryParse(result, out var numericResult) && !string.IsNullOrEmpty(units))
        {
            obx.SetNumericLabResult(setId, testCode, testName, numericResult, units, referenceRange);
        }
        else
        {
            obx.SetBasicObservation(setId, "ST", testCode, testName, result, units);
            if (!string.IsNullOrEmpty(referenceRange))
                obx.ReferenceRange.SetValue(referenceRange);
        }

        return obx;
    }

    /// <summary>
    /// Creates a vital sign OBX segment.
    /// </summary>
    /// <param name="setId">Set ID.</param>
    /// <param name="vitalType">Type of vital sign.</param>
    /// <param name="value">Vital sign value.</param>
    /// <param name="observationTime">When vital was taken.</param>
    /// <returns>Configured OBX segment.</returns>
    public static OBXSegment CreateVitalSign(int setId, VitalSignType vitalType, decimal value, DateTime? observationTime = null)
    {
        var obx = new OBXSegment();
        var (_, _, units, _) = GetVitalSignInfo(vitalType);
        obx.SetVitalSign(setId, vitalType, value, units, observationTime);
        return obx;
    }
}

/// <summary>
/// Common vital sign types with standardized LOINC codes.
/// </summary>
public enum VitalSignType
{
    Temperature,
    BloodPressureSystolic,
    BloodPressureDiastolic,
    HeartRate,
    RespiratoryRate,
    OxygenSaturation,
    Height,
    Weight,
    BMI,
    PainScale
}
