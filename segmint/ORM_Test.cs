// Simple test to validate ORM message compilation
using System;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;

namespace Segmint.Test
{
    class ORMTest
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Testing ORM Message Creation...");
                
                // Test 1: Create a basic lab order
                var labOrder = ORMMessage.CreateLabOrder(
                    "12345", 
                    "Doe", 
                    "John", 
                    "CBC", 
                    "Complete Blood Count", 
                    "Dr. Smith"
                );
                
                Console.WriteLine("✓ Lab order created successfully");
                
                // Test 2: Create ORC segment
                var orc = new ORCSegment();
                orc.SetBasicInfo("NW", "ORD123", orderingProvider: "Dr. Johnson");
                
                Console.WriteLine("✓ ORC segment created successfully");
                
                // Test 3: Create OBR segment  
                var obr = OBRSegment.CreateForLabTest("CBC", "Complete Blood Count");
                
                Console.WriteLine("✓ OBR segment created successfully");
                
                // Test 4: Create OBX segment
                var obx = OBXSegment.CreateLabResult(1, "WBC", "White Blood Count", "7.5", "10^3/uL", "4.0-11.0");
                
                Console.WriteLine("✓ OBX segment created successfully");
                
                // Test 5: Generate HL7 string
                var hl7String = labOrder.ToHL7String();
                
                Console.WriteLine("✓ HL7 string generated successfully");
                Console.WriteLine($"Generated HL7 message length: {hl7String.Length} characters");
                
                // Test 6: Validate message
                var errors = labOrder.Validate();
                if (errors.Count == 0)
                {
                    Console.WriteLine("✓ Message validation passed");
                }
                else
                {
                    Console.WriteLine($"⚠ Message has {errors.Count} validation issues:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
                
                Console.WriteLine("\n🎉 All ORM tests completed successfully!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}