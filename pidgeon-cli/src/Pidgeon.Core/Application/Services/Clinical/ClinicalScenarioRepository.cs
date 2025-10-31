// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical;

namespace Pidgeon.Core.Application.Services.Clinical;

/// <summary>
/// Repository of predefined clinical scenarios for realistic message generation.
/// Provides clinically coherent combinations of diagnoses, medications, and lab tests.
/// </summary>
public class ClinicalScenarioRepository
{
    private readonly List<ClinicalScenario> _scenarios;
    private readonly Random _random;

    public ClinicalScenarioRepository()
    {
        _random = new Random();
        _scenarios = InitializeScenarios();
    }

    /// <summary>
    /// Gets a random clinical scenario weighted by commonality.
    /// </summary>
    public ClinicalScenario GetRandomScenario()
    {
        var totalWeight = _scenarios.Sum(s => s.Weight);
        var randomValue = _random.Next(totalWeight);

        var currentWeight = 0;
        foreach (var scenario in _scenarios)
        {
            currentWeight += scenario.Weight;
            if (randomValue < currentWeight)
                return scenario;
        }

        return _scenarios.First();
    }

    /// <summary>
    /// Gets a specific scenario by ID.
    /// </summary>
    public ClinicalScenario? GetScenarioById(string scenarioId)
    {
        return _scenarios.FirstOrDefault(s => s.ScenarioId == scenarioId);
    }

    /// <summary>
    /// Gets all available scenarios.
    /// </summary>
    public IReadOnlyList<ClinicalScenario> GetAllScenarios() => _scenarios.AsReadOnly();

    private List<ClinicalScenario> InitializeScenarios()
    {
        return new List<ClinicalScenario>
        {
            CreateCongestiveHeartFailureScenario(),
            CreateType2DiabetesScenario(),
            CreateCOPDScenario(),
            CreateHypertensionScenario(),
            CreatePneumoniaScenario(),
            CreateAcuteMyocardialInfarctionScenario()
        };
    }

    /// <summary>
    /// CHF (Congestive Heart Failure) - Common inpatient condition
    /// </summary>
    private ClinicalScenario CreateCongestiveHeartFailureScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "chf",
            Name = "Congestive Heart Failure",
            Weight = 20, // Common condition
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I50.9", Description = "Heart failure, unspecified", CodingSystem = "ICD10" },
                new() { Code = "I50.23", Description = "Acute on chronic systolic heart failure", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I10", Description = "Essential hypertension", CodingSystem = "ICD10" },
                new() { Code = "E11.9", Description = "Type 2 diabetes mellitus", CodingSystem = "ICD10" },
                new() { Code = "N18.3", Description = "Chronic kidney disease, stage 3", CodingSystem = "ICD10" }
            },
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Furosemide", DrugClass = "Loop Diuretic", TypicalDosage = "40-80mg daily", PrescriptionProbability = 0.9 },
                new() { GenericName = "Lisinopril", DrugClass = "ACE Inhibitor", TypicalDosage = "10-40mg daily", PrescriptionProbability = 0.8 },
                new() { GenericName = "Carvedilol", DrugClass = "Beta Blocker", TypicalDosage = "6.25-25mg BID", PrescriptionProbability = 0.7 },
                new() { GenericName = "Spironolactone", DrugClass = "Potassium-Sparing Diuretic", TypicalDosage = "25-50mg daily", PrescriptionProbability = 0.6 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "33762-6", TestName = "BNP (Brain Natriuretic Peptide)", ExpectedRange = new ResultRange { MinValue = 200, MaxValue = 2000, Units = "pg/mL" }, OrderProbability = 0.9 },
                new() { LoincCode = "2160-0", TestName = "Creatinine", ExpectedRange = new ResultRange { MinValue = 1.2, MaxValue = 2.5, Units = "mg/dL" }, OrderProbability = 0.8 },
                new() { LoincCode = "6299-2", TestName = "BUN", ExpectedRange = new ResultRange { MinValue = 20, MaxValue = 60, Units = "mg/dL" }, OrderProbability = 0.7 },
                new() { LoincCode = "2823-3", TestName = "Potassium", ExpectedRange = new ResultRange { MinValue = 3.5, MaxValue = 5.5, Units = "mmol/L" }, OrderProbability = 0.8 }
            }
        };
    }

    /// <summary>
    /// Type 2 Diabetes - Very common chronic condition
    /// </summary>
    private ClinicalScenario CreateType2DiabetesScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "diabetes_type2",
            Name = "Type 2 Diabetes Mellitus",
            Weight = 25, // Very common
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "E11.9", Description = "Type 2 diabetes mellitus without complications", CodingSystem = "ICD10" },
                new() { Code = "E11.65", Description = "Type 2 diabetes with hyperglycemia", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I10", Description = "Essential hypertension", CodingSystem = "ICD10" },
                new() { Code = "E78.5", Description = "Hyperlipidemia", CodingSystem = "ICD10" }
            },
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Metformin", DrugClass = "Biguanide", TypicalDosage = "500-2000mg daily", PrescriptionProbability = 0.9 },
                new() { GenericName = "Insulin Glargine", DrugClass = "Long-Acting Insulin", TypicalDosage = "10-100 units daily", PrescriptionProbability = 0.6 },
                new() { GenericName = "Glipizide", DrugClass = "Sulfonylurea", TypicalDosage = "5-20mg daily", PrescriptionProbability = 0.5 },
                new() { GenericName = "Atorvastatin", DrugClass = "Statin", TypicalDosage = "10-80mg daily", PrescriptionProbability = 0.7 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "4548-4", TestName = "Hemoglobin A1c", ExpectedRange = new ResultRange { MinValue = 7.0, MaxValue = 12.0, Units = "%" }, OrderProbability = 0.95 },
                new() { LoincCode = "2345-7", TestName = "Glucose", ExpectedRange = new ResultRange { MinValue = 140, MaxValue = 350, Units = "mg/dL" }, OrderProbability = 0.9 },
                new() { LoincCode = "2160-0", TestName = "Creatinine", ExpectedRange = new ResultRange { MinValue = 0.8, MaxValue = 1.5, Units = "mg/dL" }, OrderProbability = 0.7 },
                new() { LoincCode = "13457-7", TestName = "LDL Cholesterol", ExpectedRange = new ResultRange { MinValue = 100, MaxValue = 190, Units = "mg/dL" }, OrderProbability = 0.7 }
            }
        };
    }

    /// <summary>
    /// COPD (Chronic Obstructive Pulmonary Disease)
    /// </summary>
    private ClinicalScenario CreateCOPDScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "copd",
            Name = "Chronic Obstructive Pulmonary Disease",
            Weight = 15,
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "J44.1", Description = "COPD with acute exacerbation", CodingSystem = "ICD10" },
                new() { Code = "J44.0", Description = "COPD with acute lower respiratory infection", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "J96.01", Description = "Acute respiratory failure with hypoxia", CodingSystem = "ICD10" }
            },
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Albuterol", DrugClass = "Short-Acting Beta Agonist", TypicalDosage = "2.5mg nebulized q4-6h", PrescriptionProbability = 0.95 },
                new() { GenericName = "Ipratropium", DrugClass = "Anticholinergic", TypicalDosage = "0.5mg nebulized q6h", PrescriptionProbability = 0.8 },
                new() { GenericName = "Prednisone", DrugClass = "Corticosteroid", TypicalDosage = "40-60mg daily", PrescriptionProbability = 0.85 },
                new() { GenericName = "Azithromycin", DrugClass = "Macrolide Antibiotic", TypicalDosage = "500mg daily x5 days", PrescriptionProbability = 0.6 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "2703-7", TestName = "Oxygen Saturation", ExpectedRange = new ResultRange { MinValue = 85, MaxValue = 94, Units = "%" }, OrderProbability = 0.95 },
                new() { LoincCode = "2019-8", TestName = "pCO2 (Arterial)", ExpectedRange = new ResultRange { MinValue = 45, MaxValue = 65, Units = "mmHg" }, OrderProbability = 0.7 },
                new() { LoincCode = "6690-2", TestName = "WBC", ExpectedRange = new ResultRange { MinValue = 10, MaxValue = 18, Units = "K/uL" }, OrderProbability = 0.8 }
            }
        };
    }

    /// <summary>
    /// Hypertension - Most common chronic condition
    /// </summary>
    private ClinicalScenario CreateHypertensionScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "hypertension",
            Name = "Essential Hypertension",
            Weight = 30, // Most common
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I10", Description = "Essential (primary) hypertension", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>(),
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Lisinopril", DrugClass = "ACE Inhibitor", TypicalDosage = "10-40mg daily", PrescriptionProbability = 0.7 },
                new() { GenericName = "Amlodipine", DrugClass = "Calcium Channel Blocker", TypicalDosage = "5-10mg daily", PrescriptionProbability = 0.6 },
                new() { GenericName = "Hydrochlorothiazide", DrugClass = "Thiazide Diuretic", TypicalDosage = "12.5-25mg daily", PrescriptionProbability = 0.5 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "2160-0", TestName = "Creatinine", ExpectedRange = new ResultRange { MinValue = 0.7, MaxValue = 1.3, Units = "mg/dL" }, OrderProbability = 0.6 },
                new() { LoincCode = "2823-3", TestName = "Potassium", ExpectedRange = new ResultRange { MinValue = 3.5, MaxValue = 5.0, Units = "mmol/L" }, OrderProbability = 0.5 }
            }
        };
    }

    /// <summary>
    /// Community-Acquired Pneumonia
    /// </summary>
    private ClinicalScenario CreatePneumoniaScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "pneumonia",
            Name = "Community-Acquired Pneumonia",
            Weight = 15,
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "J18.9", Description = "Pneumonia, unspecified organism", CodingSystem = "ICD10" },
                new() { Code = "J15.9", Description = "Bacterial pneumonia, unspecified", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "J96.01", Description = "Acute respiratory failure with hypoxia", CodingSystem = "ICD10" }
            },
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Ceftriaxone", DrugClass = "Cephalosporin", TypicalDosage = "1-2g IV daily", PrescriptionProbability = 0.9 },
                new() { GenericName = "Azithromycin", DrugClass = "Macrolide", TypicalDosage = "500mg IV/PO daily", PrescriptionProbability = 0.85 },
                new() { GenericName = "Acetaminophen", DrugClass = "Antipyretic", TypicalDosage = "650mg q6h PRN", PrescriptionProbability = 0.7 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "6690-2", TestName = "WBC", ExpectedRange = new ResultRange { MinValue = 12, MaxValue = 25, Units = "K/uL" }, OrderProbability = 0.95 },
                new() { LoincCode = "2703-7", TestName = "Oxygen Saturation", ExpectedRange = new ResultRange { MinValue = 88, MaxValue = 96, Units = "%" }, OrderProbability = 0.9 },
                new() { LoincCode = "1988-5", TestName = "CRP (C-Reactive Protein)", ExpectedRange = new ResultRange { MinValue = 50, MaxValue = 200, Units = "mg/L" }, OrderProbability = 0.7 }
            }
        };
    }

    /// <summary>
    /// Acute Myocardial Infarction (Heart Attack)
    /// </summary>
    private ClinicalScenario CreateAcuteMyocardialInfarctionScenario()
    {
        return new ClinicalScenario
        {
            ScenarioId = "ami",
            Name = "Acute Myocardial Infarction",
            Weight = 10, // Less common but high acuity
            PrimaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I21.9", Description = "Acute myocardial infarction, unspecified", CodingSystem = "ICD10" },
                new() { Code = "I21.3", Description = "ST elevation myocardial infarction", CodingSystem = "ICD10" }
            },
            SecondaryDiagnoses = new List<DiagnosisCode>
            {
                new() { Code = "I50.9", Description = "Heart failure, unspecified", CodingSystem = "ICD10" },
                new() { Code = "I10", Description = "Essential hypertension", CodingSystem = "ICD10" }
            },
            TypicalMedications = new List<MedicationOption>
            {
                new() { GenericName = "Aspirin", DrugClass = "Antiplatelet", TypicalDosage = "325mg loading, then 81mg daily", PrescriptionProbability = 0.98 },
                new() { GenericName = "Atorvastatin", DrugClass = "Statin", TypicalDosage = "80mg daily", PrescriptionProbability = 0.95 },
                new() { GenericName = "Metoprolol", DrugClass = "Beta Blocker", TypicalDosage = "25-100mg BID", PrescriptionProbability = 0.9 },
                new() { GenericName = "Lisinopril", DrugClass = "ACE Inhibitor", TypicalDosage = "5-40mg daily", PrescriptionProbability = 0.85 },
                new() { GenericName = "Clopidogrel", DrugClass = "Antiplatelet", TypicalDosage = "75mg daily", PrescriptionProbability = 0.8 }
            },
            RelevantLabTests = new List<LabTestOption>
            {
                new() { LoincCode = "10839-9", TestName = "Troponin I", ExpectedRange = new ResultRange { MinValue = 0.5, MaxValue = 50.0, Units = "ng/mL" }, OrderProbability = 0.99 },
                new() { LoincCode = "2157-6", TestName = "CK-MB", ExpectedRange = new ResultRange { MinValue = 10, MaxValue = 200, Units = "ng/mL" }, OrderProbability = 0.8 },
                new() { LoincCode = "33762-6", TestName = "BNP", ExpectedRange = new ResultRange { MinValue = 100, MaxValue = 1000, Units = "pg/mL" }, OrderProbability = 0.7 }
            }
        };
    }
}
