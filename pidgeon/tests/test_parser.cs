// Simple test to verify the HL7 parser works correctly
using Pidgeon.Core.Standards.HL7.v23.Parsing;

var hl7Message = @"MSH|\S\\R\\E\\T\|Pidgeon|PidgeonCore|||20250826153540||ADT\S\A01||P|2.3|||||
PID|1||29010460||Lee\S\Lisa||20200711|F|||789 Park Dr\S\\S\Washington\S\GA\S\62778\S\US||(727) 268-7520||||||988-63-3391|||
PV1||E|Primary Care Clinic||||1773182582\S\Davis\S\Nancy\S\\S\\S\\S\|||||||||||||||||||||||||||||||||||||20250817000000|";

Console.WriteLine("=== Testing HL7 Parser ===");
Console.WriteLine($"Input message:\n{hl7Message}");
Console.WriteLine();

var parser = new HL7Parser();
var result = parser.ParseMessage(hl7Message);

if (result.IsSuccess)
{
    Console.WriteLine("✅ Parser succeeded!");
    Console.WriteLine($"Message Type: {result.Value.MessageType}");
    Console.WriteLine($"Number of segments: {result.Value.Segments.Count}");
    
    foreach (var segment in result.Value.Segments)
    {
        Console.WriteLine($"- {segment.SegmentId}: {segment.GetDisplayValue()}");
    }
}
else
{
    Console.WriteLine($"❌ Parser failed: {result.Error.Message}");
}

Console.WriteLine();
Console.WriteLine("=== Testing Segment Parser ===");

var segmentParser = new HL7Parser();
var testSegment = @"PV1||E|Primary Care Clinic||||1773182582\S\Davis\S\Nancy\S\\S\\S\\S\|||||||||||||||||||||||||||||||||||||20250817000000|";
var segmentResult = segmentParser.ParseSegment(testSegment);

if (segmentResult.IsSuccess)
{
    Console.WriteLine($"✅ Segment parsing succeeded!");
    Console.WriteLine($"Segment ID: {segmentResult.Value.SegmentId}");
    Console.WriteLine($"Display: {segmentResult.Value.GetDisplayValue()}");
}
else
{
    Console.WriteLine($"❌ Segment parsing failed: {segmentResult.Error.Message}");
}