using System;
using Pidgeon.Core.Infrastructure.Standards.Common.HL7;

class Program 
{
    static void Main()
    {
        try 
        {
            Console.WriteLine("Creating parser...");
            var parser = new HL7Parser();
            Console.WriteLine("Parser created successfully");
            
            var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||";
            Console.WriteLine($"Parsing message: {hl7Message}");
            
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
    }
}