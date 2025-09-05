// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Threading;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Factory for generating HL7 v2.3 standards-compliant messages.
/// Implements HL7.org v2.3 specification with proper segment structure and field validation.
/// </summary>
public class HL7v23MessageFactory : IHL7MessageFactory
{
    private readonly ILogger<HL7v23MessageFactory> _logger;
    private readonly HL7v23Configuration _config;
    
    // HL7 v2.3 Standard Constants
    private const string FieldSeparator = "|";
    private const string EncodingCharacters = "^~\\&";
    private const string HL7Version = "2.3";
    private const string ProcessingId = "P"; // P=Production, T=Training, D=Debug
    private const char SegmentTerminator = '\r';
    
    public HL7v23MessageFactory(ILogger<HL7v23MessageFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = new HL7v23Configuration
        {
            SendingApplication = "PIDGEON",
            SendingFacility = "FACILITY",
            ReceivingApplication = "RECEIVER",
            ReceivingFacility = "FACILITY"
        };
    }

    public Result<string> GenerateADT_A01(Patient patient, Encounter encounter, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ADT^A01 message");
            
            var segments = new List<string>();
            var messageControlId = GenerateMessageControlId(options);
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            
            // Required segments for ADT^A01: MSH, EVN, PID, PV1
            segments.Add(BuildMSH("ADT", "A01", messageControlId, timestamp));
            segments.Add(BuildEVN("A01", timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildPV1(encounter ?? CreateDefaultEncounter(patient), 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 ADT^A01 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ADT^A01 message");
            return Result<string>.Failure($"ADT^A01 generation failed: {ex.Message}");
        }
    }

    public Result<string> GenerateADT_A08(Patient patient, Encounter encounter, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ADT^A08 message");
            
            var segments = new List<string>();
            var messageControlId = GenerateMessageControlId(options);
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            
            // ADT^A08 (Update) has same structure as A01
            segments.Add(BuildMSH("ADT", "A08", messageControlId, timestamp));
            segments.Add(BuildEVN("A08", timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildPV1(encounter ?? CreateDefaultEncounter(patient), 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 ADT^A08 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ADT^A08 message");
            return Result<string>.Failure($"ADT^A08 generation failed: {ex.Message}");
        }
    }

    public Result<string> GenerateADT_A03(Patient patient, Encounter encounter, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ADT^A03 message");
            
            var segments = new List<string>();
            var messageControlId = GenerateMessageControlId(options);
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            
            // ADT^A03 (Discharge) has same required segments
            segments.Add(BuildMSH("ADT", "A03", messageControlId, timestamp));
            segments.Add(BuildEVN("A03", timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildPV1(encounter ?? CreateDefaultEncounter(patient), 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 ADT^A03 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ADT^A03 message");
            return Result<string>.Failure($"ADT^A03 generation failed: {ex.Message}");
        }
    }

    public Result<string> GenerateORU_R01(Patient patient, ObservationResult observation, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ORU^R01 message");
            if (observation == null)
                return Result<string>.Failure("Observation is required for ORU^R01 message");
            
            var segments = new List<string>();
            var messageControlId = GenerateMessageControlId(options);
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            
            // Required segments for ORU^R01: MSH, PID, OBR, OBX
            segments.Add(BuildMSH("ORU", "R01", messageControlId, timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildOBR(observation, 1));
            segments.Add(BuildOBX(observation, 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 ORU^R01 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ORU^R01 message");
            return Result<string>.Failure($"ORU^R01 generation failed: {ex.Message}");
        }
    }

    public Result<string> GenerateRDE_O11(Patient patient, Prescription prescription, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for RDE^O11 message");
            if (prescription == null)
                return Result<string>.Failure("Prescription is required for RDE^O11 message");
            
            var segments = new List<string>();
            var messageControlId = GenerateMessageControlId(options);
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            
            // Required segments for RDE^O11: MSH, PID, RXE
            segments.Add(BuildMSH("RDE", "O11", messageControlId, timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildRXE(prescription, 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 RDE^O11 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate RDE^O11 message");
            return Result<string>.Failure($"RDE^O11 generation failed: {ex.Message}");
        }
    }

    public Result<string> GenerateORM_O01(Patient patient, Order order, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ORM^O01 message");
            if (order == null)
                return Result<string>.Failure("Order is required for ORM^O01 message");

            var segments = new List<string>();
            var timestamp = FormatHL7Timestamp(DateTime.Now);
            var controlId = GenerateMessageControlId(options);

            // Required segments for ORM^O01: MSH, PID, ORC, OBR
            segments.Add(BuildMSH("ORM^O01", "O01", controlId, timestamp));
            segments.Add(BuildPID(patient, 1));
            segments.Add(BuildORC(order, 1));
            segments.Add(BuildOBRForOrder(order, 1));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Generated HL7 v2.3 ORM^O01 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ORM^O01 message");
            return Result<string>.Failure($"ORM^O01 generation failed: {ex.Message}");
        }
    }

    // === Segment Builders ===

    /// <summary>
    /// Builds MSH segment with 6 required fields per HL7 v2.3 specification.
    /// </summary>
    private string BuildMSH(string messageType, string triggerEvent, string controlId, string timestamp)
    {
        var sb = new StringBuilder("MSH");
        
        // MSH-1: Field separator (always |)
        sb.Append(FieldSeparator);
        
        // MSH-2: Encoding characters (^~\&)
        sb.Append(EncodingCharacters).Append(FieldSeparator);
        
        // MSH-3: Sending application
        sb.Append(_config.SendingApplication).Append(FieldSeparator);
        
        // MSH-4: Sending facility
        sb.Append(_config.SendingFacility).Append(FieldSeparator);
        
        // MSH-5: Receiving application
        sb.Append(_config.ReceivingApplication).Append(FieldSeparator);
        
        // MSH-6: Receiving facility
        sb.Append(_config.ReceivingFacility).Append(FieldSeparator);
        
        // MSH-7: Date/time of message
        sb.Append(timestamp).Append(FieldSeparator);
        
        // MSH-8: Security (optional, leave empty)
        sb.Append(FieldSeparator);
        
        // MSH-9: Message type (REQUIRED) - format: MSG^EVENT^MSG_STRUCTURE
        sb.Append(messageType).Append("^").Append(triggerEvent).Append("^")
          .Append(messageType).Append("_").Append(triggerEvent).Append(FieldSeparator);
        
        // MSH-10: Message control ID (REQUIRED)
        sb.Append(controlId).Append(FieldSeparator);
        
        // MSH-11: Processing ID (REQUIRED)
        sb.Append(ProcessingId).Append(FieldSeparator);
        
        // MSH-12: Version ID (REQUIRED)
        sb.Append(HL7Version);
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds EVN segment with required fields per HL7 v2.3 specification.
    /// </summary>
    private string BuildEVN(string eventTypeCode, string recordedDateTime)
    {
        var sb = new StringBuilder("EVN");
        
        sb.Append(FieldSeparator);
        
        // EVN-1: Event type code (REQUIRED)
        sb.Append(eventTypeCode).Append(FieldSeparator);
        
        // EVN-2: Recorded date/time (REQUIRED)
        sb.Append(recordedDateTime);
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds PID segment with required fields per HL7 v2.3 specification.
    /// </summary>
    private string BuildPID(Patient patient, int setId)
    {
        var sb = new StringBuilder("PID");
        
        sb.Append(FieldSeparator);
        
        // PID-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // PID-2: Patient ID (external) - deprecated in v2.3, leave empty
        sb.Append(FieldSeparator);
        
        // PID-3: Patient identifier list (REQUIRED)
        sb.Append(patient.MedicalRecordNumber ?? patient.Id)
          .Append("^^^").Append(_config.SendingFacility).Append("^MR")
          .Append(FieldSeparator);
        
        // PID-4: Alternate patient ID (optional)
        sb.Append(FieldSeparator);
        
        // PID-5: Patient name (REQUIRED) - format: Family^Given^Middle^Suffix^Prefix
        sb.Append(EscapeHL7Field(patient.Name.Family ?? "UNKNOWN"))
          .Append("^").Append(EscapeHL7Field(patient.Name.Given ?? "UNKNOWN"));
        
        if (!string.IsNullOrWhiteSpace(patient.Name.Middle))
            sb.Append("^").Append(EscapeHL7Field(patient.Name.Middle));
        
        if (!string.IsNullOrWhiteSpace(patient.Name.Suffix))
            sb.Append("^^").Append(EscapeHL7Field(patient.Name.Suffix));
        
        sb.Append(FieldSeparator);
        
        // PID-6: Mother's maiden name (optional)
        sb.Append(FieldSeparator);
        
        // PID-7: Date of birth (optional but commonly used)
        if (patient.BirthDate.HasValue)
            sb.Append(patient.BirthDate.Value.ToString("yyyyMMdd"));
        sb.Append(FieldSeparator);
        
        // PID-8: Administrative sex (optional but commonly used)
        sb.Append(MapGenderToHL7(patient.Gender));
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds PV1 segment with required fields per HL7 v2.3 specification.
    /// </summary>
    private string BuildPV1(Encounter encounter, int setId)
    {
        var sb = new StringBuilder("PV1");
        
        sb.Append(FieldSeparator);
        
        // PV1-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // PV1-2: Patient class (REQUIRED) - E=Emergency, I=Inpatient, O=Outpatient, etc.
        sb.Append(MapEncounterTypeToPatientClass(encounter.Type)).Append(FieldSeparator);
        
        // PV1-3: Assigned patient location (optional but commonly used)
        if (!string.IsNullOrWhiteSpace(encounter.Location))
        {
            // Format: Building^Room^Bed
            var location = encounter.Location.Replace(" ", "^");
            sb.Append(location);
        }
        sb.Append(FieldSeparator);
        
        // PV1-4 through PV1-6: Admission type, preadmit number, prior location (optional)
        sb.Append(FieldSeparator).Append(FieldSeparator).Append(FieldSeparator);
        
        // PV1-7: Attending doctor (optional but commonly used)
        if (encounter.Provider != null)
        {
            sb.Append(encounter.Provider.Id ?? "")
              .Append("^")
              .Append(EscapeHL7Field(encounter.Provider.Name.Family ?? ""))
              .Append("^")
              .Append(EscapeHL7Field(encounter.Provider.Name.Given ?? ""));
        }
        sb.Append(FieldSeparator);
        
        // PV1-8: Referring doctor (optional)
        sb.Append(FieldSeparator);
        
        // PV1-9 through PV1-18: Various optional fields
        for (int i = 9; i <= 18; i++)
            sb.Append(FieldSeparator);
        
        // PV1-19: Visit number (optional but commonly used)
        sb.Append(encounter.Id).Append(FieldSeparator);
        
        // PV1-20 through PV1-43: Various optional fields
        for (int i = 20; i <= 43; i++)
            sb.Append(FieldSeparator);
        
        // PV1-44: Admit date/time (optional but commonly used)
        if (encounter.StartTime.HasValue)
            sb.Append(FormatHL7Timestamp(encounter.StartTime.Value));
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds OBR segment for observation requests (lab orders).
    /// </summary>
    private string BuildOBR(ObservationResult observation, int setId)
    {
        var sb = new StringBuilder("OBR");
        
        sb.Append(FieldSeparator);
        
        // OBR-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // OBR-2: Placer order number (optional)
        sb.Append(observation.Id).Append(FieldSeparator);
        
        // OBR-3: Filler order number (optional)
        sb.Append(FieldSeparator);
        
        // OBR-4: Universal service identifier (REQUIRED)
        sb.Append(observation.TestCode ?? "")
          .Append("^").Append(EscapeHL7Field(observation.TestName))
          .Append(FieldSeparator);
        
        // OBR-5 through OBR-7: Optional fields (leave empty but include separators)
        sb.Append(FieldSeparator); // OBR-5: Priority
        sb.Append(FieldSeparator); // OBR-6: Requested date/time
        sb.Append(FieldSeparator); // OBR-7: Observation date/time
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds OBX segment for observation results.
    /// </summary>
    private string BuildOBX(ObservationResult observation, int setId)
    {
        var sb = new StringBuilder("OBX");
        
        sb.Append(FieldSeparator);
        
        // OBX-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // OBX-2: Value type (REQUIRED) - NM=Numeric, TX=Text, CE=Coded Element
        var valueType = IsNumeric(observation.Value) ? "NM" : "TX";
        sb.Append(valueType).Append(FieldSeparator);
        
        // OBX-3: Observation identifier (REQUIRED)
        sb.Append(observation.TestCode ?? "")
          .Append("^").Append(EscapeHL7Field(observation.TestName))
          .Append(FieldSeparator);
        
        // OBX-4: Observation sub-ID (optional)
        sb.Append(FieldSeparator);
        
        // OBX-5: Observation value (REQUIRED)
        sb.Append(EscapeHL7Field(observation.Value)).Append(FieldSeparator);
        
        // OBX-6: Units (optional but commonly used)
        if (!string.IsNullOrWhiteSpace(observation.Units))
            sb.Append(observation.Units);
        sb.Append(FieldSeparator);
        
        // OBX-7: Reference range (optional)
        if (!string.IsNullOrWhiteSpace(observation.ReferenceRange))
            sb.Append(observation.ReferenceRange);
        sb.Append(FieldSeparator);
        
        // OBX-8: Abnormal flags (optional)
        sb.Append(FieldSeparator);
        
        // OBX-9: Probability (optional)
        sb.Append(FieldSeparator);
        
        // OBX-10: Nature of abnormal test (optional)
        sb.Append(FieldSeparator);
        
        // OBX-11: Observation result status (F=Final, C=Correction, P=Preliminary)
        sb.Append(observation.Status ?? "F");
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds RXE segment for pharmacy orders.
    /// </summary>
    private string BuildRXE(Prescription prescription, int setId)
    {
        var sb = new StringBuilder("RXE");
        
        sb.Append(FieldSeparator);
        
        // RXE-1: Quantity/Timing (optional)
        sb.Append(FieldSeparator);
        
        // RXE-2: Give code (REQUIRED) - medication identifier
        sb.Append(prescription.Medication?.Id ?? "")
          .Append("^").Append(EscapeHL7Field(prescription.Medication?.Name ?? "Unknown Medication"))
          .Append(FieldSeparator);
        
        // RXE-3: Give amount min (optional)
        if (prescription.Dosage != null && !string.IsNullOrWhiteSpace(prescription.Dosage.Dose))
            sb.Append(prescription.Dosage.Dose);
        sb.Append(FieldSeparator);
        
        // RXE-4: Give amount max (optional)
        sb.Append(FieldSeparator);
        
        // RXE-5: Give units (optional but commonly used)
        if (prescription.Dosage != null && !string.IsNullOrWhiteSpace(prescription.Dosage.DoseUnit))
            sb.Append(prescription.Dosage.DoseUnit);
        sb.Append(FieldSeparator);
        
        // RXE-6: Give dosage form (optional)
        sb.Append(FieldSeparator);
        
        // RXE-7: Provider's administration instructions (optional)
        if (!string.IsNullOrWhiteSpace(prescription.Instructions))
            sb.Append(EscapeHL7Field(prescription.Instructions));
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds ORC segment for order control (Common Order segment).
    /// Required for ORM^O01 messages.
    /// </summary>
    private string BuildORC(Order order, int setId)
    {
        var sb = new StringBuilder("ORC");
        
        sb.Append(FieldSeparator);
        
        // ORC-1: Order Control (REQUIRED) - NW=New Order
        sb.Append(order.Status ?? "NW").Append(FieldSeparator);
        
        // ORC-2: Placer Order Number (REQUIRED)
        sb.Append(order.Id).Append(FieldSeparator);
        
        // ORC-3: Filler Order Number (optional)
        sb.Append(FieldSeparator);
        
        // ORC-4: Placer Group Number (optional)
        sb.Append(FieldSeparator);
        
        // ORC-5: Order Status (optional)
        sb.Append(order.Status ?? "").Append(FieldSeparator);
        
        // ORC-6: Response Flag (optional)
        sb.Append(FieldSeparator);
        
        // ORC-7: Quantity/Timing (optional)
        sb.Append(FieldSeparator);
        
        // ORC-8: Parent (optional)
        sb.Append(FieldSeparator);
        
        // ORC-9: Date/Time of Transaction
        if (order.OrderDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(order.OrderDateTime.Value));
        sb.Append(FieldSeparator);
        
        // ORC-10: Entered By (optional)
        if (order.OrderingProvider != null)
            sb.Append(order.OrderingProvider.Id).Append("^")
              .Append(EscapeHL7Field(order.OrderingProvider.Name.Family ?? ""))
              .Append("^").Append(EscapeHL7Field(order.OrderingProvider.Name.Given ?? ""));
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds OBR segment for order requests (specialized for ORM^O01).
    /// </summary>
    private string BuildOBRForOrder(Order order, int setId)
    {
        var sb = new StringBuilder("OBR");
        
        sb.Append(FieldSeparator);
        
        // OBR-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // OBR-2: Placer order number (optional)
        sb.Append(order.Id).Append(FieldSeparator);
        
        // OBR-3: Filler order number (optional)
        sb.Append(FieldSeparator);
        
        // OBR-4: Universal service identifier (REQUIRED)
        sb.Append(order.OrderCode ?? "")
          .Append("^").Append(EscapeHL7Field(order.Description))
          .Append(FieldSeparator);
        
        // OBR-5: Priority
        sb.Append(order.Priority ?? "R").Append(FieldSeparator);
        
        // OBR-6: Requested date/time
        if (order.OrderDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(order.OrderDateTime.Value));
        sb.Append(FieldSeparator);
        
        // OBR-7: Observation date/time
        sb.Append(FieldSeparator);
        
        return sb.ToString();
    }

    // === Helper Methods ===

    private string GenerateMessageControlId(GenerationOptions? options = null)
    {
        // Generate deterministic message control ID based on seed for testing consistency
        var seed = options?.Seed ?? Environment.TickCount;
        var random = new Random(seed);
        
        // Generate 10-character hex control ID
        var controlId = random.Next(0x10000000, 0x7FFFFFFF).ToString("X10")[..10];
        return controlId;
    }

    private string FormatHL7Timestamp(DateTime dateTime)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        return dateTime.ToString("yyyyMMddHHmmss");
    }

    private string MapGenderToHL7(Gender? gender)
    {
        return gender switch
        {
            Gender.Male => "M",
            Gender.Female => "F",
            Gender.Other => "O",
            Gender.Unknown => "U",
            _ => "U"
        };
    }

    private string MapEncounterTypeToPatientClass(EncounterType? type)
    {
        return type switch
        {
            EncounterType.Emergency => "E",
            EncounterType.Inpatient => "I",
            EncounterType.Outpatient => "O",
            EncounterType.Observation => "O",
            _ => "I" // Default to inpatient
        };
    }

    private Encounter CreateDefaultEncounter(Patient patient)
    {
        var defaultProvider = new Provider
        {
            Id = "DOC001",
            Name = new PersonName { Given = "John", Family = "Smith" },
            Degree = "MD",
            Specialty = "Internal Medicine",
            NpiNumber = "1234567890"
        };

        return new Encounter
        {
            Id = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Patient = patient,
            Provider = defaultProvider,
            Type = EncounterType.Inpatient,
            Status = EncounterStatus.Finished,
            StartTime = DateTime.Now,
            Location = "WARD^101^A",
            Priority = EncounterPriority.Routine
        };
    }

    private string EscapeHL7Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        // HL7 escape sequences for special characters
        return value
            .Replace("\\", "\\E\\")  // Escape character
            .Replace("|", "\\F\\")   // Field separator
            .Replace("^", "\\S\\")   // Component separator
            .Replace("&", "\\T\\")   // Subcomponent separator
            .Replace("~", "\\R\\");  // Repetition separator
    }

    private bool IsNumeric(string value)
    {
        return double.TryParse(value, out _);
    }
}

/// <summary>
/// Configuration for HL7 v2.3 message generation.
/// </summary>
internal class HL7v23Configuration
{
    public string SendingApplication { get; set; } = "PIDGEON";
    public string SendingFacility { get; set; } = "FACILITY";
    public string ReceivingApplication { get; set; } = "RECEIVER";
    public string ReceivingFacility { get; set; } = "FACILITY";
}