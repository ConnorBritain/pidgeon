using Pidgeon.Core.Infrastructure.Standards.Common.HL7;

var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
                "PID|1||29010460||Lee^Lisa||20200711|F|||789 Park Dr^^Washington^GA^62778^US||(727) 268-7520||||||988-63-3391|||\r";

try 
{
    var parser = new HL7Parser();
    Console.WriteLine("Parser created successfully");
    
    var result = parser.ParseMessage(hl7Message);
    Console.WriteLine($"Parse result: Success={result.IsSuccess}");
    
    if (result.IsFailure)
        Console.WriteLine($"Error: {result.Error.Message}");
    else
        Console.WriteLine($"Parsed message with {result.Value.Segments.Count} segments");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}