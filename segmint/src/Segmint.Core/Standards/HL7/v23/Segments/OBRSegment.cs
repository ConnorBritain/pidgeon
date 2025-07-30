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
/// Represents an HL7 Observation Request (OBR) segment.
/// This segment is used to transmit information specific to an order for a diagnostic study or observation.
/// It contains information about the requested service, when and where it should be performed, and by whom.
/// </summary>
public class OBRSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "OBR";

    #region Fields

    /// <summary>
    /// OBR.1 - Set ID - Observation Request (SI) - Optional
    /// Sequence number for multiple OBR segments within a message.
    /// </summary>
    public SequenceIdField SetId { get; private set; } = null!;

    /// <summary>
    /// OBR.2 - Placer Order Number (EI) - Conditional
    /// Unique identifier assigned to the order by the placer application.
    /// </summary>
    public ExtendedCompositeIdField PlacerOrderNumber { get; private set; } = null!;

    /// <summary>
    /// OBR.3 - Filler Order Number (EI) - Conditional
    /// Unique identifier assigned to the order by the filler application.
    /// </summary>
    public ExtendedCompositeIdField FillerOrderNumber { get; private set; } = null!;

    /// <summary>
    /// OBR.4 - Universal Service ID (CE) - Required
    /// Identifier for the service being requested (LOINC code, CPT code, etc.).
    /// </summary>
    public CodedElementField UniversalServiceId { get; private set; } = null!;

    /// <summary>
    /// OBR.5 - Priority (ID) - Optional
    /// Priority of the request (S=Stat, A=ASAP, R=Routine, P=Preoperative, T=Timing critical).
    /// </summary>
    public StringField Priority { get; private set; } = null!;

    /// <summary>
    /// OBR.6 - Requested Date/Time (TS) - Optional
    /// Date and time the service was requested.
    /// </summary>
    public TimestampField RequestedDateTime { get; private set; } = null!;

    /// <summary>
    /// OBR.7 - Observation Date/Time (TS) - Conditional
    /// Date and time the observation/specimen collection occurred.
    /// </summary>
    public TimestampField ObservationDateTime { get; private set; } = null!;

    /// <summary>
    /// OBR.8 - Observation End Date/Time (TS) - Optional
    /// End date and time of the observation period.
    /// </summary>
    public TimestampField ObservationEndDateTime { get; private set; } = null!;

    /// <summary>
    /// OBR.9 - Collection Volume (CQ) - Optional
    /// Volume of specimen collected.
    /// </summary>
    public CompositeQuantityField CollectionVolume { get; private set; } = null!;

    /// <summary>
    /// OBR.10 - Collector Identifier (XCN) - Optional
    /// Person who collected the specimen.
    /// </summary>
    public PersonNameField CollectorIdentifier { get; private set; } = null!;

    /// <summary>
    /// OBR.11 - Specimen Action Code (ID) - Optional
    /// Action to be taken with respect to the specimen (A=Add, G=Generated order, L=Lab to obtain specimen, etc.).
    /// </summary>
    public StringField SpecimenActionCode { get; private set; } = null!;

    /// <summary>
    /// OBR.12 - Danger Code (CE) - Optional
    /// Code indicating potential dangers associated with specimen collection.
    /// </summary>
    public CodedElementField DangerCode { get; private set; } = null!;

    /// <summary>
    /// OBR.13 - Relevant Clinical Info (ST) - Optional
    /// Clinical information relevant to the interpretation of the test results.
    /// </summary>
    public StringField RelevantClinicalInfo { get; private set; } = null!;

    /// <summary>
    /// OBR.14 - Specimen Received Date/Time (TS) - Optional
    /// Date and time the specimen was received by the diagnostic service.
    /// </summary>
    public TimestampField SpecimenReceivedDateTime { get; private set; } = null!;

    /// <summary>
    /// OBR.15 - Specimen Source (CM) - Optional
    /// Source of the specimen (blood, urine, tissue, etc.).
    /// </summary>
    public StringField SpecimenSource { get; private set; } = null!;

    /// <summary>
    /// OBR.16 - Ordering Provider (XCN) - Optional
    /// Provider who ordered the service.
    /// </summary>
    public PersonNameField OrderingProvider { get; private set; } = null!;

    /// <summary>
    /// OBR.17 - Order Callback Phone Number (XTN) - Optional
    /// Phone number to call with questions about the order.
    /// </summary>
    public TelephoneField OrderCallbackPhoneNumber { get; private set; } = null!;

    /// <summary>
    /// OBR.18 - Placer Field 1 (ST) - Optional
    /// User-defined field for placer use.
    /// </summary>
    public StringField PlacerField1 { get; private set; } = null!;

    /// <summary>
    /// OBR.19 - Placer Field 2 (ST) - Optional
    /// User-defined field for placer use.
    /// </summary>
    public StringField PlacerField2 { get; private set; } = null!;

    /// <summary>
    /// OBR.20 - Filler Field 1 (ST) - Optional
    /// User-defined field for filler use.
    /// </summary>
    public StringField FillerField1 { get; private set; } = null!;

    /// <summary>
    /// OBR.21 - Filler Field 2 (ST) - Optional
    /// User-defined field for filler use.
    /// </summary>
    public StringField FillerField2 { get; private set; } = null!;

    /// <summary>
    /// OBR.22 - Results Rpt/Status Chng - Date/Time (TS) - Optional
    /// Date and time results were reported or status changed.
    /// </summary>
    public TimestampField ResultsReportStatusChangeDateTime { get; private set; } = null!;

    /// <summary>
    /// OBR.23 - Charge to Practice (CM) - Optional
    /// Charge information for the service.
    /// </summary>
    public StringField ChargeToPractice { get; private set; } = null!;

    /// <summary>
    /// OBR.24 - Diagnostic Serv Sect ID (ID) - Optional
    /// Section of diagnostic service performing the observation.
    /// </summary>
    public StringField DiagnosticServiceSectionId { get; private set; } = null!;

    /// <summary>
    /// OBR.25 - Result Status (ID) - Optional
    /// Status of the results (O=Order received, I=No results available, S=No results available; procedure scheduled, etc.).
    /// </summary>
    public StringField ResultStatus { get; private set; } = null!;

    /// <summary>
    /// OBR.26 - Parent Result (CM) - Optional
    /// Link to parent result if this is a child observation.
    /// </summary>
    public StringField ParentResult { get; private set; } = null!;

    /// <summary>
    /// OBR.27 - Quantity/Timing (TQ) - Optional
    /// Timing instructions for the observation.
    /// </summary>
    public TimingQuantityField QuantityTiming { get; private set; } = null!;

    /// <summary>
    /// OBR.28 - Result Copies To (XCN) - Optional
    /// Additional recipients for result copies.
    /// </summary>
    public PersonNameField ResultCopiesTo { get; private set; } = null!;

    /// <summary>
    /// OBR.29 - Parent Number (CM) - Optional
    /// Parent observation identifier.
    /// </summary>
    public StringField ParentNumber { get; private set; } = null!;

    /// <summary>
    /// OBR.30 - Transportation Mode (ID) - Optional
    /// Mode of transportation for specimen.
    /// </summary>
    public StringField TransportationMode { get; private set; } = null!;

    /// <summary>
    /// OBR.31 - Reason for Study (CE) - Optional
    /// Clinical reason for the study.
    /// </summary>
    public CodedElementField ReasonForStudy { get; private set; } = null!;

    /// <summary>
    /// OBR.32 - Principal Result Interpreter (CM) - Optional
    /// Principal person interpreting the results.
    /// </summary>
    public PersonNameField PrincipalResultInterpreter { get; private set; } = null!;

    /// <summary>
    /// OBR.33 - Assistant Result Interpreter (CM) - Optional
    /// Assistant person interpreting the results.
    /// </summary>
    public PersonNameField AssistantResultInterpreter { get; private set; } = null!;

    /// <summary>
    /// OBR.34 - Technician (CM) - Optional
    /// Technician performing the observation.
    /// </summary>
    public PersonNameField Technician { get; private set; } = null!;

    /// <summary>
    /// OBR.35 - Transcriptionist (CM) - Optional
    /// Person who transcribed the results.
    /// </summary>
    public PersonNameField Transcriptionist { get; private set; } = null!;

    /// <summary>
    /// OBR.36 - Scheduled Date/Time (TS) - Optional
    /// Scheduled date and time for the observation.
    /// </summary>
    public TimestampField ScheduledDateTime { get; private set; } = null!;

    #endregion

    /// <summary>
    /// Initializes a new instance of the OBRSegment class.
    /// </summary>
    public OBRSegment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes all fields for the OBR segment.
    /// </summary>
    protected override void InitializeFields()
    {
        SetId = new SequenceIdField();
        PlacerOrderNumber = new ExtendedCompositeIdField();
        FillerOrderNumber = new ExtendedCompositeIdField();
        UniversalServiceId = new CodedElementField();
        Priority = new StringField();
        RequestedDateTime = new TimestampField();
        ObservationDateTime = new TimestampField();
        ObservationEndDateTime = new TimestampField();
        CollectionVolume = new CompositeQuantityField();
        CollectorIdentifier = new PersonNameField();
        SpecimenActionCode = new StringField();
        DangerCode = new CodedElementField();
        RelevantClinicalInfo = new StringField();
        SpecimenReceivedDateTime = new TimestampField();
        SpecimenSource = new StringField();
        OrderingProvider = new PersonNameField();
        OrderCallbackPhoneNumber = new TelephoneField();
        PlacerField1 = new StringField();
        PlacerField2 = new StringField();
        FillerField1 = new StringField();
        FillerField2 = new StringField();
        ResultsReportStatusChangeDateTime = new TimestampField();
        ChargeToPractice = new StringField();
        DiagnosticServiceSectionId = new StringField();
        ResultStatus = new StringField();
        ParentResult = new StringField();
        QuantityTiming = new TimingQuantityField();
        ResultCopiesTo = new PersonNameField();
        ParentNumber = new StringField();
        TransportationMode = new StringField();
        ReasonForStudy = new CodedElementField();
        PrincipalResultInterpreter = new PersonNameField();
        AssistantResultInterpreter = new PersonNameField();
        Technician = new PersonNameField();
        Transcriptionist = new PersonNameField();
        ScheduledDateTime = new TimestampField();

        // Add fields to the fields collection in order
        AddField(SetId);                                  // OBR.1
        AddField(PlacerOrderNumber);                      // OBR.2
        AddField(FillerOrderNumber);                      // OBR.3
        AddField(UniversalServiceId);                     // OBR.4
        AddField(Priority);                               // OBR.5
        AddField(RequestedDateTime);                      // OBR.6
        AddField(ObservationDateTime);                    // OBR.7
        AddField(ObservationEndDateTime);                 // OBR.8
        AddField(CollectionVolume);                       // OBR.9
        AddField(CollectorIdentifier);                    // OBR.10
        AddField(SpecimenActionCode);                     // OBR.11
        AddField(DangerCode);                             // OBR.12
        AddField(RelevantClinicalInfo);                   // OBR.13
        AddField(SpecimenReceivedDateTime);               // OBR.14
        AddField(SpecimenSource);                         // OBR.15
        AddField(OrderingProvider);                       // OBR.16
        AddField(OrderCallbackPhoneNumber);               // OBR.17
        AddField(PlacerField1);                           // OBR.18
        AddField(PlacerField2);                           // OBR.19
        AddField(FillerField1);                           // OBR.20
        AddField(FillerField2);                           // OBR.21
        AddField(ResultsReportStatusChangeDateTime);      // OBR.22
        AddField(ChargeToPractice);                       // OBR.23
        AddField(DiagnosticServiceSectionId);             // OBR.24
        AddField(ResultStatus);                           // OBR.25
        AddField(ParentResult);                           // OBR.26
        AddField(QuantityTiming);                         // OBR.27
        AddField(ResultCopiesTo);                         // OBR.28
        AddField(ParentNumber);                           // OBR.29
        AddField(TransportationMode);                     // OBR.30
        AddField(ReasonForStudy);                         // OBR.31
        AddField(PrincipalResultInterpreter);             // OBR.32
        AddField(AssistantResultInterpreter);             // OBR.33
        AddField(Technician);                             // OBR.34
        AddField(Transcriptionist);                       // OBR.35
        AddField(ScheduledDateTime);                      // OBR.36
    }

    #region Convenience Methods

    /// <summary>
    /// Sets the universal service identifier for the observation request.
    /// </summary>
    /// <param name="serviceCode">Service code (LOINC, CPT, etc.)</param>
    /// <param name="serviceText">Service description</param>
    /// <param name="codingSystem">Coding system (e.g., "LN" for LOINC, "CPT4")</param>
    public void SetUniversalServiceId(string serviceCode, string? serviceText = null, string? codingSystem = null)
    {
        UniversalServiceId.SetComponents(serviceCode, serviceText, codingSystem);
    }

    /// <summary>
    /// Sets the ordering information for the observation request.
    /// </summary>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="fillerOrderNumber">Filler order number</param>
    /// <param name="setId">Set ID for this OBR</param>
    public void SetOrderInformation(string? placerOrderNumber = null, string? fillerOrderNumber = null, int setId = 1)
    {
        SetId.SetValue(setId.ToString());
        
        if (!string.IsNullOrEmpty(placerOrderNumber))
            PlacerOrderNumber.SetValue(placerOrderNumber);
            
        if (!string.IsNullOrEmpty(fillerOrderNumber))
            FillerOrderNumber.SetValue(fillerOrderNumber);
    }

    /// <summary>
    /// Sets the ordering provider information.
    /// </summary>
    /// <param name="lastName">Provider's last name</param>
    /// <param name="firstName">Provider's first name</param>
    /// <param name="middleName">Provider's middle name</param>
    /// <param name="providerId">Provider's ID</param>
    /// <param name="phoneNumber">Provider's phone number</param>
    public void SetOrderingProvider(string lastName, string? firstName = null, string? middleName = null, string? providerId = null, string? phoneNumber = null)
    {
        OrderingProvider.SetComponents(lastName, firstName, middleName);
        if (!string.IsNullOrEmpty(providerId))
        {
            // Set provider ID in the appropriate component
            OrderingProvider.SetValue($"{lastName}^{firstName}^{middleName}^^{providerId}");
        }
        
        if (!string.IsNullOrEmpty(phoneNumber))
            OrderCallbackPhoneNumber.SetValue(phoneNumber);
    }

    /// <summary>
    /// Sets the timing information for the observation.
    /// </summary>
    /// <param name="requestedDateTime">When the service was requested</param>
    /// <param name="observationDateTime">When the observation occurred</param>
    /// <param name="scheduledDateTime">When the observation is scheduled</param>
    public void SetTiming(DateTime? requestedDateTime = null, DateTime? observationDateTime = null, DateTime? scheduledDateTime = null)
    {
        if (requestedDateTime.HasValue)
            RequestedDateTime.SetValue(requestedDateTime.Value);
            
        if (observationDateTime.HasValue)
            ObservationDateTime.SetValue(observationDateTime.Value);
            
        if (scheduledDateTime.HasValue)
            ScheduledDateTime.SetValue(scheduledDateTime.Value);
    }

    /// <summary>
    /// Sets the specimen information.
    /// </summary>
    /// <param name="specimenSource">Source of the specimen</param>
    /// <param name="collectionVolume">Volume collected</param>
    /// <param name="volumeUnits">Units for the volume</param>
    /// <param name="specimenActionCode">Action code for specimen</param>
    /// <param name="collectorName">Name of the collector</param>
    public void SetSpecimenInformation(string? specimenSource = null, string? collectionVolume = null, 
        string? volumeUnits = null, string? specimenActionCode = null, string? collectorName = null)
    {
        if (!string.IsNullOrEmpty(specimenSource))
            SpecimenSource.SetValue(specimenSource);
            
        if (!string.IsNullOrEmpty(collectionVolume) && !string.IsNullOrEmpty(volumeUnits))
            CollectionVolume.SetComponents(collectionVolume, volumeUnits);
            
        if (!string.IsNullOrEmpty(specimenActionCode))
            SpecimenActionCode.SetValue(specimenActionCode);
            
        if (!string.IsNullOrEmpty(collectorName))
            CollectorIdentifier.SetValue(collectorName);
    }

    /// <summary>
    /// Sets the priority of the observation request.
    /// </summary>
    /// <param name="priority">Priority code (S=Stat, A=ASAP, R=Routine, etc.)</param>
    public void SetPriority(string priority)
    {
        Priority.SetValue(priority);
    }

    /// <summary>
    /// Sets the result status.
    /// </summary>
    /// <param name="resultStatus">Result status code</param>
    public void SetResultStatus(string resultStatus)
    {
        ResultStatus.SetValue(resultStatus);
    }

    /// <summary>
    /// Sets the diagnostic service section.
    /// </summary>
    /// <param name="sectionId">Section identifier (LAB, RAD, etc.)</param>
    public void SetDiagnosticServiceSection(string sectionId)
    {
        DiagnosticServiceSectionId.SetValue(sectionId);
    }

    /// <summary>
    /// Sets relevant clinical information.
    /// </summary>
    /// <param name="clinicalInfo">Clinical information relevant to the test</param>
    public void SetRelevantClinicalInfo(string clinicalInfo)
    {
        RelevantClinicalInfo.SetValue(clinicalInfo);
    }

    /// <summary>
    /// Sets the reason for the study.
    /// </summary>
    /// <param name="reasonCode">Reason code</param>
    /// <param name="reasonText">Reason description</param>
    /// <param name="codingSystem">Coding system</param>
    public void SetReasonForStudy(string reasonCode, string? reasonText = null, string? codingSystem = null)
    {
        ReasonForStudy.SetComponents(reasonCode, reasonText, codingSystem);
    }

    #endregion

    /// <summary>
    /// Validates the OBR segment according to HL7 v2.3 specifications.
    /// </summary>
    /// <returns>List of validation issues found.</returns>
    public override List<string> Validate()
    {
        var issues = new List<string>();

        // OBR.4 Universal Service ID is required
        if (string.IsNullOrEmpty(UniversalServiceId.Value))
        {
            issues.Add("OBR.4 Universal Service ID is required");
        }

        // Either Placer Order Number or Filler Order Number should be present
        if (string.IsNullOrEmpty(PlacerOrderNumber.RawValue) && string.IsNullOrEmpty(FillerOrderNumber.RawValue))
        {
            issues.Add("Either OBR.2 Placer Order Number or OBR.3 Filler Order Number should be present");
        }

        // Validate priority if present
        if (!string.IsNullOrEmpty(Priority.Value))
        {
            var validPriorities = new[] { "S", "A", "R", "P", "C", "T" };
            if (!validPriorities.Contains(Priority.Value))
            {
                issues.Add($"OBR.5 Priority '{Priority.Value}' is not a valid priority code");
            }
        }

        // Validate result status if present
        if (!string.IsNullOrEmpty(ResultStatus.Value))
        {
            var validStatuses = new[] { "O", "I", "S", "A", "P", "C", "R", "F", "X", "Y", "Z" };
            if (!validStatuses.Contains(ResultStatus.Value))
            {
                issues.Add($"OBR.25 Result Status '{ResultStatus.Value}' is not a valid status code");
            }
        }

        // Validate diagnostic service section if present
        if (!string.IsNullOrEmpty(DiagnosticServiceSectionId.Value))
        {
            var validSections = new[] { "AU", "BG", "BLB", "CUS", "CTH", "CT", "CH", "CP", "EC", "EN", "GE", "HM", "ICU", "LAB", "MB", "MCB", "MYC", "NMR", "NMS", "NRS", "OUS", "OT", "OTH", "PATH", "PF", "PHR", "PHY", "PT", "RAD", "RX", "SP", "SR", "TX", "VUS", "VR", "XRC" };
            if (!validSections.Contains(DiagnosticServiceSectionId.Value))
            {
                issues.Add($"OBR.24 Diagnostic Service Section ID '{DiagnosticServiceSectionId.Value}' is not a valid section code");
            }
        }

        return issues;
    }

    /// <summary>
    /// Returns a human-readable display string for the OBR segment.
    /// </summary>
    /// <returns>Display string containing key observation request information.</returns>
    public string ToDisplayString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(UniversalServiceId.Value))
            parts.Add($"Service: {UniversalServiceId.ToDisplayString()}");

        if (!string.IsNullOrEmpty(PlacerOrderNumber.RawValue))
            parts.Add($"Placer: {PlacerOrderNumber.RawValue}");

        if (!string.IsNullOrEmpty(Priority.Value))
            parts.Add($"Priority: {Priority.Value}");

        if (!string.IsNullOrEmpty(ResultStatus.Value))
            parts.Add($"Status: {ResultStatus.Value}");

        if (!string.IsNullOrEmpty(OrderingProvider.RawValue))
            parts.Add($"Provider: {OrderingProvider.ToDisplayString()}");

        return $"OBR - Observation Request: {string.Join(", ", parts)}";
    }

    /// <summary>
    /// Creates an OBR segment for a laboratory test order.
    /// </summary>
    /// <param name="serviceCode">Test code (LOINC, CPT, etc.)</param>
    /// <param name="serviceDescription">Test description</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="orderingProvider">Ordering provider name</param>
    /// <param name="priority">Test priority</param>
    /// <param name="specimenSource">Source of specimen</param>
    /// <returns>Configured OBR segment for lab test</returns>
    public static OBRSegment CreateForLabTest(string serviceCode, string serviceDescription, 
        string? placerOrderNumber = null, string? orderingProvider = null, string priority = "R", string? specimenSource = null)
    {
        var obr = new OBRSegment();
        obr.SetOrderInformation(placerOrderNumber, setId: 1);
        obr.SetUniversalServiceId(serviceCode, serviceDescription, "LN");
        obr.SetPriority(priority);
        obr.SetTiming(requestedDateTime: DateTime.Now);
        obr.SetDiagnosticServiceSection("LAB");
        obr.SetResultStatus("O"); // Order received
        
        if (!string.IsNullOrEmpty(orderingProvider))
            obr.OrderingProvider.SetValue(orderingProvider);
            
        if (!string.IsNullOrEmpty(specimenSource))
            obr.SetSpecimenInformation(specimenSource: specimenSource, specimenActionCode: "L");
            
        return obr;
    }

    /// <summary>
    /// Creates an OBR segment for a radiology order.
    /// </summary>
    /// <param name="serviceCode">Radiology procedure code</param>
    /// <param name="serviceDescription">Procedure description</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="orderingProvider">Ordering provider name</param>
    /// <param name="priority">Study priority</param>
    /// <param name="scheduledDateTime">Scheduled date/time</param>
    /// <returns>Configured OBR segment for radiology</returns>
    public static OBRSegment CreateForRadiology(string serviceCode, string serviceDescription,
        string? placerOrderNumber = null, string? orderingProvider = null, string priority = "R", DateTime? scheduledDateTime = null)
    {
        var obr = new OBRSegment();
        obr.SetOrderInformation(placerOrderNumber, setId: 1);
        obr.SetUniversalServiceId(serviceCode, serviceDescription, "CPT4");
        obr.SetPriority(priority);
        obr.SetTiming(requestedDateTime: DateTime.Now, scheduledDateTime: scheduledDateTime);
        obr.SetDiagnosticServiceSection("RAD");
        obr.SetResultStatus("S"); // Procedure scheduled
        
        if (!string.IsNullOrEmpty(orderingProvider))
            obr.OrderingProvider.SetValue(orderingProvider);
            
        return obr;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var cloned = new OBRSegment();
        
        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i]?.Clone()!;
        }
        
        return cloned;
    }
}
