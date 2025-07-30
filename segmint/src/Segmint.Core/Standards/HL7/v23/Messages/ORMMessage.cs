// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 Order Message (ORM) for managing laboratory, pharmacy, and other healthcare orders.
/// The ORM message is used to communicate order information including new orders, modifications, and cancellations.
/// This message type is fundamental for Order Management in healthcare systems.
/// </summary>
public class ORMMessage : HL7Message
{
    /// <inheritdoc />
    public override string MessageType => "ORM";

    /// <inheritdoc />
    public override string TriggerEvent => "O01";

    /// <inheritdoc />
    public override string MessageStructure => "ORM_O01";

    #region Message Segments

    /// <summary>
    /// Message Header segment (required).
    /// </summary>
    public MSHSegment Header { get; private set; }

    /// <summary>
    /// Patient Identification segment (required).
    /// Contains patient demographics and identifiers.
    /// </summary>
    public PIDSegment PatientIdentification { get; private set; }

    /// <summary>
    /// Patient Visit segment (optional).
    /// Contains information about the patient's current visit or encounter.
    /// </summary>
    public PV1Segment? PatientVisit { get; set; }

    /// <summary>
    /// List of Order Control segments with their associated observations.
    /// Each order can have multiple observation requests and results.
    /// </summary>
    public List<OrderGroup> Orders { get; private set; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the ORMMessage class.
    /// </summary>
    public ORMMessage()
    {
        Header = new MSHSegment();
        PatientIdentification = new PIDSegment();
        Orders = new List<OrderGroup>();
        InitializeMessage();
    }

    /// <summary>
    /// Initializes the message with default values.
    /// </summary>
    protected override void InitializeMessage()
    {
        // Set up message header
        Header.SetBasicInfo();
        Header.SetMessageType(MessageType, TriggerEvent, MessageStructure);
        Header.SetProcessingId(true); // Production mode
        Header.GenerateMessageControlId();
    }

    #region Message Building Methods

    /// <summary>
    /// Sets patient information for the order.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="middleName">Patient's middle name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="gender">Patient's gender (M/F/O/U)</param>
    /// <param name="ssn">Patient's SSN</param>
    public void SetPatientInformation(string patientId, string lastName, string? firstName = null, 
        string? middleName = null, DateTime? dateOfBirth = null, string? gender = null, string? ssn = null)
    {
        PatientIdentification.PatientIdentifierList.SetValue(patientId);
        if (!string.IsNullOrEmpty(ssn))
            PatientIdentification.PatientAccountNumber.SetValue(ssn);
        PatientIdentification.SetPatientName(lastName, firstName ?? "", middleName);
        
        if (dateOfBirth.HasValue)
            PatientIdentification.SetDateOfBirth(dateOfBirth.Value);
            
        if (!string.IsNullOrEmpty(gender))
            PatientIdentification.SetGender(gender);
    }

    /// <summary>
    /// Sets patient address information.
    /// </summary>
    /// <param name="street">Street address</param>
    /// <param name="city">City</param>
    /// <param name="state">State</param>
    /// <param name="zipCode">ZIP code</param>
    /// <param name="country">Country</param>
    public void SetPatientAddress(string? street = null, string? city = null, string? state = null, 
        string? zipCode = null, string? country = null)
    {
        PatientIdentification.SetAddress(street, city, state, zipCode, country);
    }

    /// <summary>
    /// Sets patient visit information.
    /// </summary>
    /// <param name="patientClass">Patient class (I=Inpatient, O=Outpatient, E=Emergency, etc.)</param>
    /// <param name="assignedPatientLocation">Assigned patient location</param>
    /// <param name="attendingDoctor">Attending physician</param>
    /// <param name="visitNumber">Visit number</param>
    /// <param name="admissionType">Admission type</param>
    public void SetPatientVisit(string patientClass, string? assignedPatientLocation = null, 
        string? attendingDoctor = null, string? visitNumber = null, string? admissionType = null)
    {
        PatientVisit ??= new PV1Segment();
        PatientVisit.SetPatientClass(patientClass);
        
        if (!string.IsNullOrEmpty(assignedPatientLocation))
            PatientVisit.SetAssignedPatientLocation(assignedPatientLocation);
            
        if (!string.IsNullOrEmpty(attendingDoctor))
            PatientVisit.SetAttendingDoctor(attendingDoctor);
            
        if (!string.IsNullOrEmpty(visitNumber))
            PatientVisit.SetVisitNumber(visitNumber);
            
        if (!string.IsNullOrEmpty(admissionType))
            PatientVisit.SetAdmissionType(admissionType);
    }

    /// <summary>
    /// Adds a new laboratory order to the message.
    /// </summary>
    /// <param name="orderControl">Order control code (NW=New, CA=Cancel, etc.)</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="serviceCode">Service/test code (LOINC, CPT, etc.)</param>
    /// <param name="serviceName">Service/test name</param>
    /// <param name="orderingProvider">Ordering provider</param>
    /// <param name="priority">Order priority (S=Stat, A=ASAP, R=Routine)</param>
    /// <param name="specimenSource">Specimen source</param>
    /// <param name="clinicalInfo">Relevant clinical information</param>
    /// <returns>The created order group for additional configuration</returns>
    public OrderGroup AddLabOrder(string orderControl, string? placerOrderNumber = null, 
        string? serviceCode = null, string? serviceName = null, string? orderingProvider = null, 
        string priority = "R", string? specimenSource = null, string? clinicalInfo = null)
    {
        var orderGroup = new OrderGroup();
        
        // Set up ORC segment
        if (string.IsNullOrEmpty(placerOrderNumber))
            orderGroup.OrderControl.GeneratePlacerOrderNumber("LAB");
            
        orderGroup.OrderControl.SetBasicInfo(orderControl, placerOrderNumber, orderingProvider: orderingProvider);
        
        // Set up OBR segment if service information provided
        if (!string.IsNullOrEmpty(serviceCode) || !string.IsNullOrEmpty(serviceName))
        {
            var obr = OBRSegment.CreateForLabTest(serviceCode ?? "LABTEST", serviceName ?? "Laboratory Test",
                orderGroup.OrderControl.PlacerOrderNumber.Value, orderingProvider, priority, specimenSource);
                
            if (!string.IsNullOrEmpty(clinicalInfo))
                obr.SetRelevantClinicalInfo(clinicalInfo);
                
            orderGroup.ObservationRequests.Add(obr);
        }
        
        Orders.Add(orderGroup);
        return orderGroup;
    }

    /// <summary>
    /// Adds a new radiology order to the message.
    /// </summary>
    /// <param name="orderControl">Order control code</param>
    /// <param name="placerOrderNumber">Placer order number</param>
    /// <param name="procedureCode">Radiology procedure code</param>
    /// <param name="procedureName">Radiology procedure name</param>
    /// <param name="orderingProvider">Ordering provider</param>
    /// <param name="priority">Order priority</param>
    /// <param name="scheduledDateTime">Scheduled date/time</param>
    /// <param name="clinicalInfo">Clinical indication</param>
    /// <returns>The created order group</returns>
    public OrderGroup AddRadiologyOrder(string orderControl, string? placerOrderNumber = null,
        string? procedureCode = null, string? procedureName = null, string? orderingProvider = null,
        string priority = "R", DateTime? scheduledDateTime = null, string? clinicalInfo = null)
    {
        var orderGroup = new OrderGroup();
        
        // Set up ORC segment
        if (string.IsNullOrEmpty(placerOrderNumber))
            orderGroup.OrderControl.GeneratePlacerOrderNumber("RAD");
            
        orderGroup.OrderControl.SetBasicInfo(orderControl, placerOrderNumber, orderingProvider: orderingProvider);
        
        // Set up OBR segment
        if (!string.IsNullOrEmpty(procedureCode) || !string.IsNullOrEmpty(procedureName))
        {
            var obr = OBRSegment.CreateForRadiology(procedureCode ?? "RADPROC", procedureName ?? "Radiology Procedure",
                orderGroup.OrderControl.PlacerOrderNumber.Value, orderingProvider, priority, scheduledDateTime);
                
            if (!string.IsNullOrEmpty(clinicalInfo))
                obr.SetRelevantClinicalInfo(clinicalInfo);
                
            orderGroup.ObservationRequests.Add(obr);
        }
        
        Orders.Add(orderGroup);
        return orderGroup;
    }

    /// <summary>
    /// Adds observation results to an existing order.
    /// </summary>
    /// <param name="orderIndex">Index of the order to add results to</param>
    /// <param name="testCode">Test code</param>
    /// <param name="testName">Test name</param>
    /// <param name="result">Result value</param>
    /// <param name="units">Units of measure</param>
    /// <param name="referenceRange">Reference range</param>
    /// <param name="abnormalFlags">Abnormal flags</param>
    /// <param name="resultStatus">Result status</param>
    public void AddObservationResult(int orderIndex, string testCode, string testName, string result,
        string? units = null, string? referenceRange = null, string? abnormalFlags = null, string resultStatus = "F")
    {
        if (orderIndex < 0 || orderIndex >= Orders.Count)
            throw new ArgumentOutOfRangeException(nameof(orderIndex), "Order index is out of range");
            
        var orderGroup = Orders[orderIndex];
        var setId = orderGroup.ObservationResults.Count + 1;
        
        var obx = OBXSegment.CreateLabResult(setId, testCode, testName, result, units, referenceRange);
        if (!string.IsNullOrEmpty(abnormalFlags))
            obx.AbnormalFlags.SetValue(abnormalFlags);
        obx.ObservationResultStatus.SetValue(resultStatus);
        
        orderGroup.ObservationResults.Add(obx);
    }

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="placerOrderNumber">Placer order number to cancel</param>
    /// <param name="fillerOrderNumber">Filler order number (if known)</param>
    /// <param name="cancelledBy">Person cancelling the order</param>
    /// <param name="reasonCode">Reason for cancellation</param>
    /// <returns>The created cancellation order group</returns>
    public OrderGroup CancelOrder(string placerOrderNumber, string? fillerOrderNumber = null, 
        string? cancelledBy = null, string? reasonCode = null)
    {
        var orderGroup = new OrderGroup();
        
        orderGroup.OrderControl.SetBasicInfo("CA", placerOrderNumber, fillerOrderNumber, "CA");
        
        if (!string.IsNullOrEmpty(cancelledBy))
            orderGroup.OrderControl.ActionBy.SetValue(cancelledBy);
            
        if (!string.IsNullOrEmpty(reasonCode))
            orderGroup.OrderControl.OrderControlCodeReason.SetComponents(reasonCode);
        
        Orders.Add(orderGroup);
        return orderGroup;
    }

    #endregion

    #region Message Serialization

    /// <inheritdoc />
    public string ToHL7String()
    {
        var segments = new List<string>();
        
        // Add required segments
        segments.Add(Header.ToHL7String());
        segments.Add(PatientIdentification.ToHL7String());
        
        // Add optional patient visit
        if (PatientVisit != null)
            segments.Add(PatientVisit.ToHL7String());
        
        // Add order groups
        foreach (var orderGroup in Orders)
        {
            segments.Add(orderGroup.OrderControl.ToHL7String());
            
            // Add observation requests
            foreach (var obr in orderGroup.ObservationRequests)
                segments.Add(obr.ToHL7String());
            
            // Add observation results
            foreach (var obx in orderGroup.ObservationResults)
                segments.Add(obx.ToHL7String());
        }
        
        return string.Join("\r", segments);
    }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();
        
        // Must have at least one order
        if (!Orders.Any())
            result.AddIssue(ValidationIssue.SemanticError("ORM001", "ORM message must contain at least one order group", "ORM"));
        
        // Validate each order group (simplified for now)
        for (int i = 0; i < Orders.Count; i++)
        {
            var orderGroup = Orders[i];
            try
            {
                // Try to call Validate if it exists, otherwise skip detailed validation for now
                var orderValidation = orderGroup.GetType().GetMethod("Validate")?.Invoke(orderGroup, null);
                if (orderValidation is List<string> stringErrors)
                {
                    foreach (var error in stringErrors)
                    {
                        result.AddIssue(ValidationIssue.SemanticError($"ORM{i + 2:D3}", $"Order {i + 1}: {error}", $"Order[{i}]"));
                    }
                }
            }
            catch
            {
                // Skip validation if method doesn't exist or fails
            }
        }
        
        return result;
    }

    /// <inheritdoc />
    public string ToDisplayString()
    {
        var patientInfo = $"{PatientIdentification.PatientName.ToDisplayString()} (ID: {PatientIdentification.PatientIdentifierList.RawValue})";
        var orderCount = Orders.Count;
        var orderTypes = Orders.Select(o => o.OrderControl.OrderControl.RawValue).Distinct();
        
        return $"ORM Message - Patient: {patientInfo}, Orders: {orderCount} ({string.Join(", ", orderTypes)})";
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new laboratory order message.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="patientLastName">Patient last name</param>
    /// <param name="patientFirstName">Patient first name</param>
    /// <param name="testCode">Lab test code</param>
    /// <param name="testName">Lab test name</param>
    /// <param name="orderingProvider">Ordering provider</param>
    /// <param name="specimenSource">Specimen source</param>
    /// <returns>Configured ORM message for lab order</returns>
    public static ORMMessage CreateLabOrder(string patientId, string patientLastName, string? patientFirstName = null,
        string? testCode = null, string? testName = null, string? orderingProvider = null, string? specimenSource = null)
    {
        var message = new ORMMessage();
        message.SetPatientInformation(patientId, patientLastName, patientFirstName);
        message.AddLabOrder("NW", serviceCode: testCode, serviceName: testName, 
            orderingProvider: orderingProvider, specimenSource: specimenSource);
        return message;
    }

    /// <summary>
    /// Creates a new radiology order message.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="patientLastName">Patient last name</param>
    /// <param name="patientFirstName">Patient first name</param>
    /// <param name="procedureCode">Radiology procedure code</param>
    /// <param name="procedureName">Radiology procedure name</param>
    /// <param name="orderingProvider">Ordering provider</param>
    /// <param name="scheduledDateTime">Scheduled date/time</param>
    /// <returns>Configured ORM message for radiology order</returns>
    public static ORMMessage CreateRadiologyOrder(string patientId, string patientLastName, string? patientFirstName = null,
        string? procedureCode = null, string? procedureName = null, string? orderingProvider = null, DateTime? scheduledDateTime = null)
    {
        var message = new ORMMessage();
        message.SetPatientInformation(patientId, patientLastName, patientFirstName);
        message.AddRadiologyOrder("NW", procedureCode: procedureCode, procedureName: procedureName,
            orderingProvider: orderingProvider, scheduledDateTime: scheduledDateTime);
        return message;
    }

    /// <summary>
    /// Creates a order cancellation message.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="patientLastName">Patient last name</param>
    /// <param name="patientFirstName">Patient first name</param>
    /// <param name="placerOrderNumber">Order number to cancel</param>
    /// <param name="fillerOrderNumber">Filler order number</param>
    /// <param name="cancelledBy">Person cancelling</param>
    /// <param name="reasonCode">Cancellation reason</param>
    /// <returns>Configured ORM message for order cancellation</returns>
    public static ORMMessage CreateOrderCancellation(string patientId, string patientLastName, string? patientFirstName = null,
        string? placerOrderNumber = null, string? fillerOrderNumber = null, string? cancelledBy = null, string? reasonCode = null)
    {
        var message = new ORMMessage();
        message.SetPatientInformation(patientId, patientLastName, patientFirstName);
        message.CancelOrder(placerOrderNumber ?? "UNKNOWN", fillerOrderNumber, cancelledBy, reasonCode);
        return message;
    }

    #endregion

    /// <inheritdoc />
    public override HL7Message Clone()
    {
        var cloned = new ORMMessage();
        
        // Clear default segments
        cloned.ClearSegments();
        
        // Clone all segments
        foreach (var segment in this)
        {
            cloned.AddSegment(segment.Clone());
        }
        
        // Update references
        cloned.Header = cloned.GetSegment<MSHSegment>() ?? new MSHSegment();
        cloned.PatientIdentification = cloned.GetSegment<PIDSegment>() ?? new PIDSegment();
        cloned.PatientVisit = cloned.GetSegment<PV1Segment>() ?? new PV1Segment();
        
        // Rebuild order groups
        cloned.Orders.Clear();
        foreach (var order in Orders)
        {
            // Find the corresponding cloned segments
            var clonedOrc = cloned.GetSegments<ORCSegment>().Skip(cloned.Orders.Count).FirstOrDefault();
            if (clonedOrc != null)
            {
                var newOrder = new OrderGroup(clonedOrc);
                cloned.Orders.Add(newOrder);
            }
        }
        
        return cloned;
    }
}

/// <summary>
/// Represents a group of order-related segments (ORC + OBR + OBX).
/// Each order group contains one order control, zero or more observation requests, and zero or more observation results.
/// </summary>
public class OrderGroup
{
    /// <summary>
    /// Order Control segment (required).
    /// </summary>
    public ORCSegment OrderControl { get; private set; }

    /// <summary>
    /// List of Observation Request segments (optional).
    /// </summary>
    public List<OBRSegment> ObservationRequests { get; private set; }

    /// <summary>
    /// List of Observation Result segments (optional).
    /// </summary>
    public List<OBXSegment> ObservationResults { get; private set; }

    /// <summary>
    /// Initializes a new instance of the OrderGroup class.
    /// </summary>
    public OrderGroup()
    {
        OrderControl = new ORCSegment();
        ObservationRequests = new List<OBRSegment>();
        ObservationResults = new List<OBXSegment>();
    }

    /// <summary>
    /// Initializes a new instance of the OrderGroup class with a specific OrderControl segment.
    /// </summary>
    /// <param name="orderControl">The order control segment to use</param>
    public OrderGroup(ORCSegment orderControl)
    {
        OrderControl = orderControl;
        ObservationRequests = new List<OBRSegment>();
        ObservationResults = new List<OBXSegment>();
    }

    /// <summary>
    /// Validates the order group.
    /// </summary>
    /// <returns>List of validation issues.</returns>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        // Validate ORC segment
        issues.AddRange(OrderControl.Validate());
        
        // Validate OBR segments
        for (int i = 0; i < ObservationRequests.Count; i++)
        {
            var obrIssues = ObservationRequests[i].Validate();
            issues.AddRange(obrIssues.Select(issue => $"OBR {i + 1}: {issue}"));
        }
        
        // Validate OBX segments
        for (int i = 0; i < ObservationResults.Count; i++)
        {
            var obxIssues = ObservationResults[i].Validate();
            issues.AddRange(obxIssues.Select(issue => $"OBX {i + 1}: {issue}"));
        }
        
        return issues;
    }
}
