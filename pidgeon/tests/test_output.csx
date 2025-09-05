using Pidgeon.Core.Tests.Generation; var plugin = TestHelpers.CreateHL7Plugin(); var result = await plugin.GenerateMessagesAsync("ADT^A01", 1, new Pidgeon.Core.Generation.GenerationOptions { Seed = 12345 }); Console.WriteLine("=== GENERATED MESSAGE ==="); Console.WriteLine(result.Value.First().Replace("
", "\r\n
"));
