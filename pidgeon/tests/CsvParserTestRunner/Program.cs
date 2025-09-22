using CsvParserTestRunner;

// Skip first tests to focus on PID integration
// // CSV parser tests
// await SimpleCsvParserTest.RunTestAsync();
//
// Console.WriteLine("\n" + new string('=', 60) + "\n");
//
// // Database import tests
// await DatabaseImportTest.RunTestAsync();
//
// Console.WriteLine("\n" + new string('=', 60) + "\n");
//
// // Comprehensive data source analysis
// await ComprehensiveDataTest.RunTestAsync();
//
// Console.WriteLine("\n" + new string('=', 60) + "\n");

// PID segment import and integration test
await PidSegmentImportTest.RunTestAsync();