using System;
using System.IO;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Testing LLamaSharp 0.24.0 AI Inference...");
        Console.WriteLine("========================================");

        // Test Phi-3 Mini which should be faster
        var modelPath = @"C:\ProgramData\Pidgeon\models\phi3-mini-instruct.gguf";

        Console.WriteLine($"🧪 Testing Phi-3 Mini");
        Console.WriteLine($"Path: {modelPath}");

        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"❌ Model not found: {modelPath}");
            return;
        }

        Console.WriteLine($"✅ Model file exists");
        var sizeMB = new FileInfo(modelPath).Length / (1024.0 * 1024.0);
        Console.WriteLine($"📊 Model size: {sizeMB:F1} MB");

        try
        {
            Console.WriteLine($"🔄 Loading Phi-3 Mini...");

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 1024,  // Smaller context for faster loading
                GpuLayerCount = 0,
                UseMemorymap = true,
                UseMemoryLock = false,
                Threads = (int)(Environment.ProcessorCount / 2)
            };

            Console.WriteLine("🔧 Creating LLamaWeights...");
            using var weights = LLamaWeights.LoadFromFile(parameters);

            Console.WriteLine("🔧 Creating context...");
            using var context = weights.CreateContext(parameters);

            Console.WriteLine("🔧 Creating executor...");
            var executor = new InteractiveExecutor(context);

            Console.WriteLine($"✅ Phi-3 Mini loaded successfully!");

            // Test HL7 healthcare modification inference
            var prompt = @"Healthcare HL7 Message Modification Request:

User Intent: 'make patient female'

Current HL7 Message:
MSH|^~\&|EPIC|HOSPITAL|PIDGEON|TEST|20250923170900||ADT^A01|12345|P|2.3
EVN||20250923170900
PID|1||123456789^^^HOSPITAL^MR||DOE^JOHN^A||19801201|M|||123 MAIN ST^APT 1^ANYTOWN^CA^90210^USA||(555)123-4567|||||123-45-6789
PV1|1|I|ICU^101^1|U|||SMITH^JANE^M^MD^^^^^NPI|||||||||S||A|||||||||||||||||||HOSPITAL

IMPORTANT: You must respond with specific field modifications in this EXACT format:

EXAMPLE OUTPUT:
PID.5: ""CurrentName^CurrentFirst"" → ""NewName^NewFirst""
PID.8: ""F"" → ""M""

Your task: Analyze the user intent and provide field modifications in the above format.
For each field that needs changes, use this pattern:
FIELD_PATH: ""current_value"" → ""new_value""

DO NOT provide general analysis. ONLY provide field modifications.";
            Console.WriteLine($"\n💬 Testing HL7 healthcare modification");
            Console.WriteLine("🔄 Generating response (max 100 tokens)...");

            var inferenceParams = new InferenceParams()
            {
                SamplingPipeline = new DefaultSamplingPipeline()
                {
                    Temperature = 0.1f,  // Very low for deterministic output
                    RepeatPenalty = 1.1f
                },
                AntiPrompts = new[] { "[DONE]", "User:", "Human:", "\n\n---" },
                MaxTokens = 100  // More tokens for healthcare response
            };

            var response = "";
            var tokenCount = 0;
            var startTime = DateTime.Now;

            try
            {
                await foreach (var text in executor.InferAsync(prompt, inferenceParams))
                {
                    response += text;
                    tokenCount++;
                    Console.Write(text); // Show tokens as they generate

                    if (tokenCount >= 100) break; // Stop after 100 tokens max

                    // Timeout after 60 seconds for this test
                    if ((DateTime.Now - startTime).TotalSeconds > 60)
                    {
                        Console.WriteLine("\n⏰ Timeout after 60 seconds");
                        break;
                    }
                }
            }
            catch (Exception inferenceEx)
            {
                Console.WriteLine($"\n❌ Inference error: {inferenceEx.Message}");
                return;
            }

            var elapsedTime = DateTime.Now - startTime;
            Console.WriteLine($"\n📝 Complete response: '{response.Trim()}'");
            Console.WriteLine($"🎯 Generated {tokenCount} tokens in {elapsedTime.TotalSeconds:F1}s");
            if (elapsedTime.TotalSeconds > 0)
            {
                Console.WriteLine($"⚡ Speed: {tokenCount / elapsedTime.TotalSeconds:F1} tokens/sec");
            }
            Console.WriteLine($"✅ Phi-3 Mini inference test: SUCCESS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Phi-3 Mini test failed!");
            Console.WriteLine($"🚨 Error Type: {ex.GetType().Name}");
            Console.WriteLine($"📋 Message: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"🔍 Inner Exception: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine("\n📊 Test Complete");
        Console.WriteLine("If this worked, LLamaSharp 0.24.0 is fully functional!");
    }
}