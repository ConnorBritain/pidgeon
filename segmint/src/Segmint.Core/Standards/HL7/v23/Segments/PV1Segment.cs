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
/// Represents the HL7 PV1 (Patient Visit) segment.
/// Contains patient visit information including location, attending physician, and visit details.
/// </summary>
public class PV1Segment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "PV1";

    /// <summary>
    /// Initializes a new instance of the <see cref="PV1Segment"/> class.
    /// </summary>
    public PV1Segment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields according to HL7 v2.3 specification.
    /// </summary>
    protected override void InitializeFields()
    {
        // PV1.1 - Set ID - PV1
        AddField(new SequenceIdField(isRequired: false));
        
        // PV1.2 - Patient Class
        AddField(CodedValueField.CreatePatientClass(isRequired: true));
        
        // PV1.3 - Assigned Patient Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.4 - Admission Type
        AddField(CodedValueField.CreateAdmissionType(isRequired: false));
        
        // PV1.5 - Preadmit Number
        AddField(new ExtendedCompositeIdField(isRequired: false));
        
        // PV1.6 - Prior Patient Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.7 - Attending Doctor
        AddField(new PersonNameField(isRequired: false));
        
        // PV1.8 - Referring Doctor
        AddField(new StringField(isRequired: false));
        
        // PV1.9 - Consulting Doctor
        AddField(new StringField(isRequired: false));
        
        // PV1.10 - Hospital Service
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.11 - Temporary Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.12 - Preadmit Test Indicator
        AddField(CodedValueField.CreateYesNoIndicator(isRequired: false));
        
        // PV1.13 - Re-admission Indicator
        AddField(CodedValueField.CreateYesNoIndicator(isRequired: false));
        
        // PV1.14 - Admit Source
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.15 - Ambulatory Status
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.16 - VIP Indicator
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.17 - Admitting Doctor
        AddField(new StringField(isRequired: false));
        
        // PV1.18 - Patient Type
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.19 - Visit Number
        AddField(new ExtendedCompositeIdField(isRequired: false));
        
        // PV1.20 - Financial Class
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.21 - Charge Price Indicator
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.22 - Courtesy Code
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.23 - Credit Rating
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.24 - Contract Code
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.25 - Contract Effective Date
        AddField(new DateField(isRequired: false));
        
        // PV1.26 - Contract Amount
        AddField(new NumericField(isRequired: false));
        
        // PV1.27 - Contract Period
        AddField(new NumericField(isRequired: false));
        
        // PV1.28 - Interest Code
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.29 - Transfer to Bad Debt Code
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.30 - Transfer to Bad Debt Date
        AddField(new DateField(isRequired: false));
        
        // PV1.31 - Bad Debt Agency Code
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.32 - Bad Debt Transfer Amount
        AddField(new NumericField(isRequired: false));
        
        // PV1.33 - Bad Debt Recovery Amount
        AddField(new NumericField(isRequired: false));
        
        // PV1.34 - Delete Account Indicator
        AddField(CodedValueField.CreateYesNoIndicator(isRequired: false));
        
        // PV1.35 - Delete Account Date
        AddField(new DateField(isRequired: false));
        
        // PV1.36 - Discharge Disposition
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.37 - Discharged to Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.38 - Diet Type
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.39 - Servicing Facility
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.40 - Bed Status
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.41 - Account Status
        AddField(new CodedValueField(isRequired: false));
        
        // PV1.42 - Pending Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.43 - Prior Temporary Location
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // PV1.44 - Admit Date/Time
        AddField(new TimestampField(isRequired: false));
        
        // PV1.45 - Discharge Date/Time
        AddField(new TimestampField(isRequired: false));
        
        // PV1.46 - Current Patient Balance
        AddField(new NumericField(isRequired: false));
        
        // PV1.47 - Total Charges
        AddField(new NumericField(isRequired: false));
        
        // PV1.48 - Total Adjustments
        AddField(new NumericField(isRequired: false));
        
        // PV1.49 - Total Payments
        AddField(new NumericField(isRequired: false));
        
        // PV1.50 - Alternate Visit ID
        AddField(new ExtendedCompositeIdField(isRequired: false));
    }

    /// <summary>
    /// Gets or sets the set ID.
    /// </summary>
    public SequenceIdField SetId
    {
        get => (SequenceIdField)this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the patient class.
    /// </summary>
    public CodedValueField PatientClass
    {
        get => (CodedValueField)this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the assigned patient location.
    /// </summary>
    public StringField AssignedPatientLocation
    {
        get => (StringField)this[3];
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the admission type.
    /// </summary>
    public CodedValueField AdmissionType
    {
        get => (CodedValueField)this[4];
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the preadmit number.
    /// </summary>
    public ExtendedCompositeIdField PreadmitNumber
    {
        get => (ExtendedCompositeIdField)this[5];
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the prior patient location.
    /// </summary>
    public StringField PriorPatientLocation
    {
        get => (StringField)this[6];
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the attending doctor.
    /// </summary>
    public PersonNameField AttendingDoctor
    {
        get => (PersonNameField)this[7];
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the referring doctor.
    /// </summary>
    public StringField ReferringDoctor
    {
        get => (StringField)this[8];
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the consulting doctor.
    /// </summary>
    public StringField ConsultingDoctor
    {
        get => (StringField)this[9];
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the hospital service.
    /// </summary>
    public CodedValueField HospitalService
    {
        get => (CodedValueField)this[10];
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the visit number.
    /// </summary>
    public ExtendedCompositeIdField VisitNumber
    {
        get => (ExtendedCompositeIdField)this[19];
        set => this[19] = value;
    }

    /// <summary>
    /// Gets or sets the financial class.
    /// </summary>
    public CodedValueField FinancialClass
    {
        get => (CodedValueField)this[20];
        set => this[20] = value;
    }

    /// <summary>
    /// Gets or sets the admitting doctor.
    /// </summary>
    public StringField AdmittingDoctor
    {
        get => (StringField)this[17];
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the admit date/time.
    /// </summary>
    public TimestampField AdmitDateTime
    {
        get => (TimestampField)this[44];
        set => this[44] = value;
    }

    /// <summary>
    /// Gets or sets the discharge date/time.
    /// </summary>
    public TimestampField DischargeDateTime
    {
        get => (TimestampField)this[45];
        set => this[45] = value;
    }

    /// <summary>
    /// Sets the patient class for common visit types.
    /// </summary>
    /// <param name="patientClass">The patient class (E=Emergency, I=Inpatient, O=Outpatient, P=Preadmit, R=Recurring, B=Obstetrics, N=Not Applicable).</param>
    public void SetPatientClass(string patientClass)
    {
        PatientClass.SetValue(patientClass);
    }

    /// <summary>
    /// Sets the assigned patient location.
    /// </summary>
    /// <param name="location">The location string (e.g., "2W^201^A").</param>
    public void SetAssignedPatientLocation(string location)
    {
        AssignedPatientLocation.SetValue(location);
    }

    /// <summary>
    /// Sets the attending doctor information.
    /// </summary>
    /// <param name="lastName">The doctor's last name.</param>
    /// <param name="firstName">The doctor's first name.</param>
    /// <param name="middleName">The doctor's middle name (optional).</param>
    /// <param name="suffix">The doctor's suffix (optional).</param>
    /// <param name="prefix">The doctor's prefix (optional).</param>
    public void SetAttendingDoctor(string lastName, string? firstName = null, string? middleName = null, 
        string? suffix = null, string? prefix = null)
    {
        AttendingDoctor.SetComponents(lastName, firstName, middleName, suffix, prefix);
    }

    /// <summary>
    /// Sets the visit number.
    /// </summary>
    /// <param name="visitNumber">The visit number.</param>
    /// <param name="assigningAuthority">The assigning authority (optional).</param>
    public void SetVisitNumber(string visitNumber, string? assigningAuthority = null)
    {
        VisitNumber = ExtendedCompositeIdField.CreateVisitNumber(visitNumber, assigningAuthority);
    }

    /// <summary>
    /// Sets the admission type.
    /// </summary>
    /// <param name="admissionType">The admission type (A=Accident, C=Elective, E=Emergency, L=Labor and Delivery, N=Newborn, R=Routine, T=Transfer, U=Unknown).</param>
    public void SetAdmissionType(string admissionType)
    {
        AdmissionType.SetValue(admissionType);
    }

    /// <summary>
    /// Sets the admit date and time.
    /// </summary>
    /// <param name="admitDateTime">The admit date and time.</param>
    public void SetAdmitDateTime(DateTime admitDateTime)
    {
        AdmitDateTime.SetValue(admitDateTime.ToString("yyyyMMddHHmmss"));
    }

    /// <summary>
    /// Sets the discharge date and time.
    /// </summary>
    /// <param name="dischargeDateTime">The discharge date and time.</param>
    public void SetDischargeDateTime(DateTime dischargeDateTime)
    {
        DischargeDateTime.SetValue(dischargeDateTime.ToString("yyyyMMddHHmmss"));
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new instance with the same field values.</returns>
    public override HL7Segment Clone()
    {
        var cloned = new PV1Segment();
        for (var i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i].Clone();
        }
        return cloned;
    }
}
