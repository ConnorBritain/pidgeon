// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Builder for creating message type specifications.
/// Single responsibility: Construct complex message specifications with proper segment/field definitions.
/// </summary>
internal static class SpecificationBuilder
{
    public static MessageTypeSpec CreateBasicADTSpec()
    {
        return new MessageTypeSpec
        {
            Description = "Patient Administration Messages",
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MSH"] = CreateMSHSegment("ADT"),
                ["EVN"] = new SegmentSpec
                {
                    Description = "Event Type",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Event Type Code", Usage = FieldUsage.Required },
                        new() { Position = 2, Name = "Recorded Date/Time", Usage = FieldUsage.Required }
                    }
                },
                ["PID"] = CreatePIDSegment(),
                ["PV1"] = CreatePV1Segment()
            },
            SampleMessages = new List<string>
            {
                "MSH|^~\\&|EHR|HOSPITAL|||20240101120000||ADT^A01|12345|P|2.3",
                "EVN|A01|20240101120000",
                "PID|1||123456^^^HOSPITAL^MR||DOE^JOHN^MIDDLE||19800101|M",
                "PV1|1|I|ICU^101^1|||DOC123^SMITH^JANE|||MED"
            }
        };
    }

    public static MessageTypeSpec CreateBasicORMSpec()
    {
        return new MessageTypeSpec
        {
            Description = "Order Messages (Pharmacy Orders)",
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MSH"] = CreateMSHSegment("ORM"),
                ["PID"] = CreatePIDSegment(),
                ["PV1"] = CreatePV1Segment(),
                ["ORC"] = new SegmentSpec
                {
                    Description = "Common Order",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Order Control", Usage = FieldUsage.Required,
                            AllowedValues = new Dictionary<string, string>
                            {
                                ["NW"] = "New Order",
                                ["RF"] = "Refill",
                                ["CA"] = "Cancel",
                                ["DC"] = "Discontinue"
                            }
                        },
                        new() { Position = 2, Name = "Placer Order Number", Usage = FieldUsage.Required },
                        new() { Position = 12, Name = "Ordering Provider", Usage = FieldUsage.Required }
                    }
                },
                ["RXO"] = new SegmentSpec
                {
                    Description = "Pharmacy/Treatment Order",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Requested Give Code", Usage = FieldUsage.Required },
                        new() { Position = 7, Name = "Provider's Administration Instructions", Usage = FieldUsage.Required },
                        new() { Position = 11, Name = "Requested Dispense Amount", Usage = FieldUsage.Optional }
                    }
                }
            }
        };
    }

    public static MessageTypeSpec CreateBasicORUSpec()
    {
        return new MessageTypeSpec
        {
            Description = "Lab Results Messages",
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MSH"] = CreateMSHSegment("ORU"),
                ["PID"] = CreatePIDSegment(),
                ["OBR"] = new SegmentSpec
                {
                    Description = "Observation Request",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Set ID", Usage = FieldUsage.Required },
                        new() { Position = 4, Name = "Universal Service ID", Usage = FieldUsage.Required },
                        new() { Position = 7, Name = "Observation Date/Time", Usage = FieldUsage.Required },
                        new() { Position = 16, Name = "Ordering Provider", Usage = FieldUsage.Required }
                    }
                },
                ["OBX"] = new SegmentSpec
                {
                    Description = "Observation Result",
                    Repeating = true,
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Set ID", Usage = FieldUsage.Required },
                        new() { Position = 2, Name = "Value Type", Usage = FieldUsage.Required },
                        new() { Position = 3, Name = "Observation Identifier", Usage = FieldUsage.Required },
                        new() { Position = 5, Name = "Observation Value", Usage = FieldUsage.Required },
                        new() { Position = 6, Name = "Units", Usage = FieldUsage.Optional },
                        new() { Position = 7, Name = "References Range", Usage = FieldUsage.Optional }
                    }
                }
            }
        };
    }

    public static MessageTypeSpec CreateImagingORUSpec()
    {
        return new MessageTypeSpec
        {
            Description = "Imaging Results Messages",
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MSH"] = CreateMSHSegment("ORU"),
                ["PID"] = CreatePIDSegment(),
                ["OBR"] = new SegmentSpec
                {
                    Description = "Imaging Order",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 4, Name = "Universal Service ID", Usage = FieldUsage.Required,
                            Description = "Imaging procedure code (CPT/LOINC)" },
                        new() { Position = 24, Name = "Diagnostic Service Section ID", Usage = FieldUsage.Required,
                            FixedValue = "RAD" }
                    }
                },
                ["OBX"] = new SegmentSpec
                {
                    Description = "Imaging Result/Report",
                    Repeating = true,
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 2, Name = "Value Type", Usage = FieldUsage.Required,
                            AllowedValues = new Dictionary<string, string>
                            {
                                ["TX"] = "Text Report",
                                ["RP"] = "Reference Pointer (Image URL)",
                                ["ED"] = "Encapsulated Data"
                            }
                        },
                        new() { Position = 5, Name = "Observation Value", Usage = FieldUsage.Required,
                            Description = "Radiology report text or image reference" }
                    }
                }
            }
        };
    }

    public static MessageTypeSpec CreateBasicSIUSpec()
    {
        return new MessageTypeSpec
        {
            Description = "Scheduling Information Messages",
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MSH"] = CreateMSHSegment("SIU"),
                ["SCH"] = new SegmentSpec
                {
                    Description = "Scheduling Activity Information",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Placer Appointment ID", Usage = FieldUsage.Required },
                        new() { Position = 7, Name = "Appointment Type", Usage = FieldUsage.Required },
                        new() { Position = 11, Name = "Appointment Timing Quantity", Usage = FieldUsage.Required }
                    }
                },
                ["PID"] = CreatePIDSegment(),
                ["AIS"] = new SegmentSpec
                {
                    Description = "Appointment Information - Service",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 3, Name = "Universal Service ID", Usage = FieldUsage.Required },
                        new() { Position = 4, Name = "Start Date/Time", Usage = FieldUsage.Required }
                    }
                }
            }
        };
    }

    public static MessageTypeSpec CreateBasicSCRIPTSpec()
    {
        return new MessageTypeSpec
        {
            Description = "NCPDP SCRIPT Prescription Messages (2017071 Standard)",
            Notes = { 
                "NCPDP SCRIPT format for electronic prescriptions",
                "Production standard until January 1, 2028",
                "Upgrade to 2023011 required by 2028"
            },
            Segments = new Dictionary<string, SegmentSpec>
            {
                ["MESSAGE"] = new SegmentSpec
                {
                    Description = "SCRIPT Message Container",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Message Type", Usage = FieldUsage.Required, 
                            AllowedValues = new Dictionary<string, string> 
                            { 
                                ["NEWRX"] = "New Prescription",
                                ["REFILL"] = "Refill Request", 
                                ["CANCEL"] = "Cancel Request",
                                ["RXFILL"] = "Fill Status Notification",
                                ["VERIFY"] = "Verification Request"
                            }
                        },
                        new() { Position = 2, Name = "Version", Usage = FieldUsage.Required, FixedValue = "017071" }
                    }
                },
                ["PATIENT"] = new SegmentSpec
                {
                    Description = "Patient Information",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Patient Name", Usage = FieldUsage.Required },
                        new() { Position = 2, Name = "Date of Birth", Usage = FieldUsage.Required },
                        new() { Position = 3, Name = "Gender", Usage = FieldUsage.Required },
                        new() { Position = 4, Name = "Patient Address", Usage = FieldUsage.Required }
                    }
                },
                ["PRESCRIBER"] = new SegmentSpec
                {
                    Description = "Prescriber Information",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Prescriber Name", Usage = FieldUsage.Required },
                        new() { Position = 2, Name = "DEA Number", Usage = FieldUsage.Optional },
                        new() { Position = 3, Name = "NPI", Usage = FieldUsage.Required }
                    }
                },
                ["MEDICATION"] = new SegmentSpec
                {
                    Description = "Medication Information",
                    Fields = new List<FieldSpec>
                    {
                        new() { Position = 1, Name = "Drug Description", Usage = FieldUsage.Required },
                        new() { Position = 2, Name = "Drug Code", Usage = FieldUsage.Required },
                        new() { Position = 3, Name = "Quantity", Usage = FieldUsage.Required },
                        new() { Position = 4, Name = "Days Supply", Usage = FieldUsage.Required },
                        new() { Position = 5, Name = "Directions for Use", Usage = FieldUsage.Required }
                    }
                }
            }
        };
    }

    private static SegmentSpec CreateMSHSegment(string messageType)
    {
        return new SegmentSpec
        {
            Description = "Message Header",
            Fields = new List<FieldSpec>
            {
                new() { Position = 1, Name = "Field Separator", Usage = FieldUsage.Required, FixedValue = "|" },
                new() { Position = 2, Name = "Encoding Characters", Usage = FieldUsage.Required, FixedValue = "^~\\&" },
                new() { Position = 3, Name = "Sending Application", Usage = FieldUsage.Required },
                new() { Position = 4, Name = "Sending Facility", Usage = FieldUsage.Optional },
                new() { Position = 5, Name = "Receiving Application", Usage = FieldUsage.Optional },
                new() { Position = 6, Name = "Receiving Facility", Usage = FieldUsage.Optional },
                new() { Position = 7, Name = "Date/Time of Message", Usage = FieldUsage.Required },
                new() { Position = 8, Name = "Security", Usage = FieldUsage.Ignored },
                new() { Position = 9, Name = "Message Type", Usage = FieldUsage.Required, 
                    Components = new Dictionary<int, ComponentSpec>
                    {
                        [1] = new ComponentSpec { Name = "Message Code", Usage = FieldUsage.Required, FixedValue = messageType },
                        [2] = new ComponentSpec { Name = "Trigger Event", Usage = FieldUsage.Required },
                        [3] = new ComponentSpec { Name = "Message Structure", Usage = FieldUsage.Optional }
                    }
                },
                new() { Position = 10, Name = "Message Control ID", Usage = FieldUsage.Required },
                new() { Position = 11, Name = "Processing ID", Usage = FieldUsage.Required,
                    AllowedValues = new Dictionary<string, string>
                    {
                        ["P"] = "Production",
                        ["T"] = "Training",
                        ["D"] = "Debugging"
                    }
                },
                new() { Position = 12, Name = "Version ID", Usage = FieldUsage.Required }
            }
        };
    }

    private static SegmentSpec CreatePIDSegment()
    {
        return new SegmentSpec
        {
            Description = "Patient Identification",
            Fields = new List<FieldSpec>
            {
                new() { Position = 1, Name = "Set ID", Usage = FieldUsage.Optional },
                new() { Position = 2, Name = "Patient ID (External)", Usage = FieldUsage.Optional },
                new() { Position = 3, Name = "Patient Identifier List", Usage = FieldUsage.Required },
                new() { Position = 4, Name = "Alternate Patient ID", Usage = FieldUsage.Optional },
                new() { Position = 5, Name = "Patient Name", Usage = FieldUsage.Required,
                    Components = new Dictionary<int, ComponentSpec>
                    {
                        [1] = new ComponentSpec { Name = "Family Name", Usage = FieldUsage.Required },
                        [2] = new ComponentSpec { Name = "Given Name", Usage = FieldUsage.Required },
                        [3] = new ComponentSpec { Name = "Second/Middle Names", Usage = FieldUsage.Optional }
                    }
                },
                new() { Position = 7, Name = "Date/Time of Birth", Usage = FieldUsage.Required },
                new() { Position = 8, Name = "Administrative Sex", Usage = FieldUsage.Required,
                    AllowedValues = new Dictionary<string, string>
                    {
                        ["M"] = "Male",
                        ["F"] = "Female", 
                        ["O"] = "Other",
                        ["U"] = "Unknown"
                    }
                }
            }
        };
    }

    private static SegmentSpec CreatePV1Segment()
    {
        return new SegmentSpec
        {
            Description = "Patient Visit",
            Fields = new List<FieldSpec>
            {
                new() { Position = 1, Name = "Set ID", Usage = FieldUsage.Optional },
                new() { Position = 2, Name = "Patient Class", Usage = FieldUsage.Required,
                    AllowedValues = new Dictionary<string, string>
                    {
                        ["E"] = "Emergency",
                        ["I"] = "Inpatient",
                        ["O"] = "Outpatient",
                        ["P"] = "Preadmit",
                        ["R"] = "Recurring patient"
                    }
                },
                new() { Position = 3, Name = "Assigned Patient Location", Usage = FieldUsage.Optional },
                new() { Position = 7, Name = "Attending Doctor", Usage = FieldUsage.Optional },
                new() { Position = 8, Name = "Referring Doctor", Usage = FieldUsage.Optional }
            }
        };
    }
}