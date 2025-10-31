// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical;

namespace Pidgeon.Core.Application.Services.Clinical;

/// <summary>
/// Coordinates clinical scenario selection and data generation.
/// Ensures diagnoses, medications, and lab tests are clinically coherent.
/// </summary>
public class ClinicalScenarioCoordinator
{
    private readonly ILogger<ClinicalScenarioCoordinator> _logger;
    private readonly ClinicalScenarioRepository _scenarioRepository;
    private readonly Random _random;

    // Thread-local storage for current scenario context
    [ThreadStatic]
    private static ClinicalScenario? _currentScenario;

    public ClinicalScenarioCoordinator(
        ILogger<ClinicalScenarioCoordinator> logger,
        ClinicalScenarioRepository scenarioRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scenarioRepository = scenarioRepository ?? throw new ArgumentNullException(nameof(scenarioRepository));
        _random = new Random();
    }

    /// <summary>
    /// Initializes a new clinical scenario for the current message generation context.
    /// Should be called once per message to establish clinical coherence.
    /// </summary>
    public void InitializeScenario(string? specificScenarioId = null)
    {
        _currentScenario = string.IsNullOrEmpty(specificScenarioId)
            ? _scenarioRepository.GetRandomScenario()
            : _scenarioRepository.GetScenarioById(specificScenarioId) ?? _scenarioRepository.GetRandomScenario();

        _logger.LogDebug("Initialized clinical scenario: {ScenarioName} ({ScenarioId})",
            _currentScenario.Name, _currentScenario.ScenarioId);
    }

    /// <summary>
    /// Gets the current active scenario (or initializes one if none exists).
    /// </summary>
    public ClinicalScenario GetCurrentScenario()
    {
        if (_currentScenario == null)
        {
            InitializeScenario();
        }

        return _currentScenario!;
    }

    /// <summary>
    /// Clears the current scenario context.
    /// </summary>
    public void ClearScenario()
    {
        _currentScenario = null;
    }

    /// <summary>
    /// Gets appropriate diagnosis codes for the current scenario.
    /// Returns 1-3 diagnoses with primary diagnosis always included.
    /// </summary>
    public List<DiagnosisCode> GetDiagnoses(int maxDiagnoses = 3)
    {
        var scenario = GetCurrentScenario();
        var diagnoses = new List<DiagnosisCode>();

        // Always include primary diagnosis
        if (scenario.PrimaryDiagnoses.Any())
        {
            var primaryDx = scenario.PrimaryDiagnoses[_random.Next(scenario.PrimaryDiagnoses.Count)];
            diagnoses.Add(primaryDx);
        }

        // Add secondary diagnoses with probability
        var remainingSlots = maxDiagnoses - diagnoses.Count;
        var shuffledSecondary = scenario.SecondaryDiagnoses.OrderBy(_ => _random.Next()).ToList();

        foreach (var secondaryDx in shuffledSecondary.Take(remainingSlots))
        {
            // 70% chance to include each secondary diagnosis
            if (_random.NextDouble() < 0.7)
            {
                diagnoses.Add(secondaryDx);
            }
        }

        _logger.LogDebug("Generated {Count} diagnoses for scenario {Scenario}",
            diagnoses.Count, scenario.Name);

        return diagnoses;
    }

    /// <summary>
    /// Gets appropriate medications for the current scenario.
    /// Returns medications based on prescription probability.
    /// </summary>
    public List<MedicationOption> GetMedications(int maxMedications = 5)
    {
        var scenario = GetCurrentScenario();
        var medications = new List<MedicationOption>();

        foreach (var medication in scenario.TypicalMedications)
        {
            if (medications.Count >= maxMedications)
                break;

            // Use prescription probability to determine if this med is included
            if (_random.NextDouble() < medication.PrescriptionProbability)
            {
                medications.Add(medication);
            }
        }

        _logger.LogDebug("Generated {Count} medications for scenario {Scenario}",
            medications.Count, scenario.Name);

        return medications;
    }

    /// <summary>
    /// Gets appropriate lab tests for the current scenario.
    /// Returns tests based on order probability with realistic result values.
    /// </summary>
    public List<LabTestResult> GetLabTests(int maxTests = 5)
    {
        var scenario = GetCurrentScenario();
        var labResults = new List<LabTestResult>();

        foreach (var test in scenario.RelevantLabTests)
        {
            if (labResults.Count >= maxTests)
                break;

            // Use order probability to determine if this test is included
            if (_random.NextDouble() < test.OrderProbability)
            {
                var result = GenerateLabResult(test);
                labResults.Add(result);
            }
        }

        _logger.LogDebug("Generated {Count} lab tests for scenario {Scenario}",
            labResults.Count, scenario.Name);

        return labResults;
    }

    /// <summary>
    /// Generates a realistic lab result value within the expected range for the scenario.
    /// </summary>
    private LabTestResult GenerateLabResult(LabTestOption test)
    {
        var range = test.ExpectedRange;
        var value = _random.NextDouble() * (range.MaxValue - range.MinValue) + range.MinValue;

        // Round to appropriate precision
        var roundedValue = Math.Round(value, 1);

        return new LabTestResult
        {
            LoincCode = test.LoincCode,
            TestName = test.TestName,
            Value = roundedValue.ToString("F1"),
            Units = range.Units,
            ReferenceRange = $"{range.MinValue:F1}-{range.MaxValue:F1}",
            AbnormalFlag = DetermineAbnormalFlag(roundedValue, range)
        };
    }

    /// <summary>
    /// Determines abnormal flag based on value and range.
    /// </summary>
    private string DetermineAbnormalFlag(double value, ResultRange range)
    {
        // For clinical scenarios, values are within expected abnormal range for the condition
        // So most will be flagged as abnormal (H or L)

        var midpoint = (range.MinValue + range.MaxValue) / 2;

        if (value > midpoint)
            return "H"; // High
        else if (value < midpoint)
            return "L"; // Low
        else
            return "N"; // Normal
    }
}

/// <summary>
/// Lab test result with value and metadata
/// </summary>
public class LabTestResult
{
    public string LoincCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public string AbnormalFlag { get; set; } = "N";
}
