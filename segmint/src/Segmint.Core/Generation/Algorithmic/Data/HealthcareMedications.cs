// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Segmint.Core.Generation.Algorithmic.Data;

/// <summary>
/// Free tier healthcare medications dataset - 50 medications covering ~80% of common prescriptions.
/// Curated for clinical diversity, age-based selection, and realistic healthcare scenarios.
/// Includes SUDS, psychiatric, specialty medications for comprehensive coverage.
/// Based on top prescribed medications in US healthcare with proper dosing and indications.
/// </summary>
public static class HealthcareMedications
{
    /// <summary>
    /// Core medication data for the top 50 prescribed drugs in US healthcare.
    /// Includes generic name, common brand name, therapeutic class, and typical indications.
    /// Comprehensive coverage including SUDS, psychiatric, and specialty medications.
    /// </summary>
    public static readonly MedicationData[] Medications = new[]
    {
        // Cardiovascular medications (most prescribed category)
        new MedicationData("Lisinopril", "Prinivil", "ACE Inhibitor", "Hypertension, Heart Failure", 
            new[] { "5mg", "10mg", "20mg", "40mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Amlodipine", "Norvasc", "Calcium Channel Blocker", "Hypertension, Angina", 
            new[] { "2.5mg", "5mg", "10mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Atorvastatin", "Lipitor", "Statin", "Hyperlipidemia, Cardiovascular Prevention", 
            new[] { "10mg", "20mg", "40mg", "80mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Metoprolol", "Lopressor", "Beta Blocker", "Hypertension, Heart Failure, Angina", 
            new[] { "25mg", "50mg", "100mg", "200mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Diabetes medications
        new MedicationData("Metformin", "Glucophage", "Antidiabetic", "Type 2 Diabetes Mellitus", 
            new[] { "500mg", "850mg", "1000mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Glipizide", "Glucotrol", "Sulfonylurea", "Type 2 Diabetes Mellitus", 
            new[] { "5mg", "10mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Antibiotics (common infections)
        new MedicationData("Amoxicillin", "Amoxil", "Penicillin Antibiotic", "Bacterial Infections", 
            new[] { "250mg", "500mg", "875mg" }, AgeGroup.All, false),
        
        new MedicationData("Azithromycin", "Z-Pack", "Macrolide Antibiotic", "Respiratory Tract Infections", 
            new[] { "250mg", "500mg" }, AgeGroup.All, false),
        
        new MedicationData("Cephalexin", "Keflex", "Cephalosporin Antibiotic", "Skin and Soft Tissue Infections", 
            new[] { "250mg", "500mg", "750mg" }, AgeGroup.All, false),

        // Pain management
        new MedicationData("Ibuprofen", "Advil", "NSAID", "Pain, Inflammation, Fever", 
            new[] { "200mg", "400mg", "600mg", "800mg" }, AgeGroup.All, false),
        
        new MedicationData("Acetaminophen", "Tylenol", "Analgesic", "Pain, Fever", 
            new[] { "325mg", "500mg", "650mg" }, AgeGroup.All, false),
        
        new MedicationData("Tramadol", "Ultram", "Opioid Analgesic", "Moderate to Severe Pain", 
            new[] { "50mg", "100mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),

        // Respiratory medications
        new MedicationData("Albuterol", "ProAir", "Beta2 Agonist", "Asthma, COPD", 
            new[] { "90mcg/inhaler", "2.5mg/3mL nebulizer" }, AgeGroup.All, false),
        
        new MedicationData("Prednisone", "Deltasone", "Corticosteroid", "Inflammation, Autoimmune Conditions", 
            new[] { "5mg", "10mg", "20mg", "50mg" }, AgeGroup.All, false),

        // Mental health medications  
        new MedicationData("Sertraline", "Zoloft", "SSRI Antidepressant", "Depression, Anxiety Disorders", 
            new[] { "25mg", "50mg", "100mg", "150mg", "200mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Fluoxetine", "Prozac", "SSRI Antidepressant", "Depression, OCD, Panic Disorder", 
            new[] { "10mg", "20mg", "40mg", "60mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Escitalopram", "Lexapro", "SSRI Antidepressant", "Depression, Generalized Anxiety", 
            new[] { "5mg", "10mg", "20mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Bupropion", "Wellbutrin", "NDRI Antidepressant", "Depression, Smoking Cessation", 
            new[] { "75mg", "100mg", "150mg", "300mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // SUDS (Substance Use Disorder) medications
        new MedicationData("Buprenorphine", "Suboxone", "Opioid Partial Agonist", "Opioid Use Disorder", 
            new[] { "2mg", "4mg", "8mg", "12mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),
        
        new MedicationData("Methadone", "Dolophine", "Opioid Agonist", "Opioid Use Disorder", 
            new[] { "5mg", "10mg", "40mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),
        
        new MedicationData("Naltrexone", "Vivitrol", "Opioid Antagonist", "Alcohol/Opioid Use Disorder", 
            new[] { "50mg tablet", "380mg injection" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Acamprosate", "Campral", "GABA Modulator", "Alcohol Use Disorder", 
            new[] { "333mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Psychiatric medications (extended coverage)
        new MedicationData("Aripiprazole", "Abilify", "Atypical Antipsychotic", "Schizophrenia, Bipolar Disorder", 
            new[] { "5mg", "10mg", "15mg", "20mg", "30mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Quetiapine", "Seroquel", "Atypical Antipsychotic", "Schizophrenia, Bipolar Disorder", 
            new[] { "25mg", "50mg", "100mg", "200mg", "300mg", "400mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Lithium", "Lithobid", "Mood Stabilizer", "Bipolar Disorder", 
            new[] { "150mg", "300mg", "450mg", "600mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Alprazolam", "Xanax", "Benzodiazepine", "Anxiety, Panic Disorder", 
            new[] { "0.25mg", "0.5mg", "1mg", "2mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),
        
        new MedicationData("Lorazepam", "Ativan", "Benzodiazepine", "Anxiety, Seizures", 
            new[] { "0.5mg", "1mg", "2mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),

        // ADHD medications
        new MedicationData("Amphetamine", "Adderall", "CNS Stimulant", "ADHD, Narcolepsy", 
            new[] { "5mg", "7.5mg", "10mg", "12.5mg", "15mg", "20mg", "30mg" }, AgeGroup.Pediatric | AgeGroup.Adult, true),
        
        new MedicationData("Methylphenidate", "Ritalin", "CNS Stimulant", "ADHD", 
            new[] { "5mg", "10mg", "20mg" }, AgeGroup.Pediatric | AgeGroup.Adult, true),

        // Proton pump inhibitors
        new MedicationData("Omeprazole", "Prilosec", "Proton Pump Inhibitor", "GERD, Peptic Ulcer Disease", 
            new[] { "10mg", "20mg", "40mg" }, AgeGroup.All, false),
        
        new MedicationData("Pantoprazole", "Protonix", "Proton Pump Inhibitor", "GERD, Erosive Esophagitis", 
            new[] { "20mg", "40mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Seizure medications
        new MedicationData("Phenytoin", "Dilantin", "Anticonvulsant", "Seizure Disorders", 
            new[] { "30mg", "100mg" }, AgeGroup.All, false),
        
        new MedicationData("Carbamazepine", "Tegretol", "Anticonvulsant", "Seizures, Trigeminal Neuralgia", 
            new[] { "100mg", "200mg", "400mg" }, AgeGroup.All, false),

        // Sleep medications
        new MedicationData("Zolpidem", "Ambien", "Hypnotic", "Insomnia", 
            new[] { "5mg", "10mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),
        
        new MedicationData("Trazodone", "Desyrel", "Atypical Antidepressant", "Depression, Insomnia", 
            new[] { "50mg", "100mg", "150mg", "300mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Thyroid medications
        new MedicationData("Levothyroxine", "Synthroid", "Thyroid Hormone", "Hypothyroidism", 
            new[] { "25mcg", "50mcg", "75mcg", "100mcg", "125mcg", "150mcg" }, AgeGroup.All, false),

        // Blood thinners
        new MedicationData("Warfarin", "Coumadin", "Anticoagulant", "Atrial Fibrillation, DVT/PE", 
            new[] { "1mg", "2mg", "2.5mg", "5mg", "7.5mg", "10mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Rivaroxaban", "Xarelto", "Factor Xa Inhibitor", "Atrial Fibrillation, DVT/PE", 
            new[] { "10mg", "15mg", "20mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Dermatology
        new MedicationData("Hydrocortisone", "Cortisone", "Topical Corticosteroid", "Skin Inflammation, Eczema", 
            new[] { "0.5%", "1%", "2.5%" }, AgeGroup.All, false),
        
        new MedicationData("Tretinoin", "Retin-A", "Retinoid", "Acne, Photoaging", 
            new[] { "0.025%", "0.05%", "0.1%" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Specialty medications
        new MedicationData("Furosemide", "Lasix", "Loop Diuretic", "Heart Failure, Edema", 
            new[] { "20mg", "40mg", "80mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Spironolactone", "Aldactone", "Potassium-Sparing Diuretic", "Heart Failure, Hypertension", 
            new[] { "25mg", "50mg", "100mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // HIV medications (common regimen components)
        new MedicationData("Emtricitabine", "Emtriva", "NRTI", "HIV Treatment", 
            new[] { "200mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),
        
        new MedicationData("Tenofovir", "Viread", "NRTI", "HIV Treatment, Hepatitis B", 
            new[] { "150mg", "200mg", "250mg", "300mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Pediatric-specific additions
        new MedicationData("Amoxicillin", "Amoxil Suspension", "Penicillin Antibiotic", "Pediatric Infections", 
            new[] { "125mg/5mL", "200mg/5mL", "250mg/5mL", "400mg/5mL" }, AgeGroup.Pediatric, false),
        
        new MedicationData("Acetaminophen", "Tylenol Suspension", "Analgesic", "Pediatric Fever, Pain", 
            new[] { "80mg/0.8mL", "80mg/2.5mL", "160mg/5mL" }, AgeGroup.Pediatric, false),

        // Additional common prescriptions
        new MedicationData("Clonazepam", "Klonopin", "Benzodiazepine", "Seizures, Panic Disorder", 
            new[] { "0.5mg", "1mg", "2mg" }, AgeGroup.Adult | AgeGroup.Geriatric, true),
        
        new MedicationData("Gabapentin", "Neurontin", "Anticonvulsant", "Neuropathic Pain, Seizures", 
            new[] { "100mg", "300mg", "400mg", "600mg", "800mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false),

        // Muscle relaxants
        new MedicationData("Cyclobenzaprine", "Flexeril", "Muscle Relaxant", "Muscle Spasms", 
            new[] { "5mg", "7.5mg", "10mg" }, AgeGroup.Adult | AgeGroup.Geriatric, false)
    };

    /// <summary>
    /// Gets medications appropriate for a specific age group.
    /// </summary>
    public static IEnumerable<MedicationData> GetMedicationsForAgeGroup(AgeGroup ageGroup)
    {
        return Medications.Where(med => med.AppropriateAgeGroups.HasFlag(ageGroup));
    }

    /// <summary>
    /// Gets controlled substance medications for realistic prescription tracking.
    /// </summary>
    public static IEnumerable<MedicationData> GetControlledSubstances()
    {
        return Medications.Where(med => med.IsControlledSubstance);
    }

    /// <summary>
    /// Gets medications by therapeutic class for clinical correlation.
    /// </summary>
    public static IEnumerable<MedicationData> GetMedicationsByClass(string therapeuticClass)
    {
        return Medications.Where(med => 
            med.TherapeuticClass.Contains(therapeuticClass, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets random medication from the dataset with optional filtering.
    /// </summary>
    public static MedicationData GetRandomMedication(Random random, AgeGroup? ageGroup = null, bool excludeControlled = false)
    {
        var availableMeds = Medications.AsEnumerable();
        
        if (ageGroup.HasValue)
            availableMeds = availableMeds.Where(med => med.AppropriateAgeGroups.HasFlag(ageGroup.Value));
        
        if (excludeControlled)
            availableMeds = availableMeds.Where(med => !med.IsControlledSubstance);

        var medicationArray = availableMeds.ToArray();
        return medicationArray[random.Next(medicationArray.Length)];
    }

    /// <summary>
    /// Common drug allergies for realistic patient profiles.
    /// </summary>
    public static readonly string[] CommonAllergies = new[]
    {
        "Penicillin", "Sulfa", "Codeine", "Aspirin", "Iodine",
        "Latex", "Morphine", "Shellfish", "Nuts", "Eggs"
    };

    /// <summary>
    /// Gets random allergies for patient generation.
    /// </summary>
    public static IEnumerable<string> GetRandomAllergies(Random random, int maxCount = 2)
    {
        var count = random.Next(0, Math.Min(maxCount + 1, CommonAllergies.Length));
        return CommonAllergies.OrderBy(x => random.Next()).Take(count);
    }
}

/// <summary>
/// Represents a medication with clinical data for realistic generation.
/// </summary>
public record MedicationData
{
    /// <summary>
    /// Generic name of the medication.
    /// </summary>
    public string GenericName { get; }

    /// <summary>
    /// Common brand name of the medication.
    /// </summary>
    public string BrandName { get; }

    /// <summary>
    /// Therapeutic class (e.g., "ACE Inhibitor", "SSRI Antidepressant").
    /// </summary>
    public string TherapeuticClass { get; }

    /// <summary>
    /// Common indications for prescribing.
    /// </summary>
    public string Indications { get; }

    /// <summary>
    /// Available strengths/dosages.
    /// </summary>
    public string[] AvailableStrengths { get; }

    /// <summary>
    /// Age groups this medication is appropriate for.
    /// </summary>
    public AgeGroup AppropriateAgeGroups { get; }

    /// <summary>
    /// Whether this is a controlled substance requiring special handling.
    /// </summary>
    public bool IsControlledSubstance { get; }

    public MedicationData(string genericName, string brandName, string therapeuticClass, 
        string indications, string[] availableStrengths, AgeGroup appropriateAgeGroups, 
        bool isControlledSubstance)
    {
        GenericName = genericName;
        BrandName = brandName;
        TherapeuticClass = therapeuticClass;
        Indications = indications;
        AvailableStrengths = availableStrengths;
        AppropriateAgeGroups = appropriateAgeGroups;
        IsControlledSubstance = isControlledSubstance;
    }

    /// <summary>
    /// Gets a random strength for this medication.
    /// </summary>
    public string GetRandomStrength(Random random)
    {
        return AvailableStrengths[random.Next(AvailableStrengths.Length)];
    }
}

/// <summary>
/// Age groups for medication appropriateness.
/// </summary>
[Flags]
public enum AgeGroup
{
    None = 0,
    Pediatric = 1,
    Adult = 2,
    Geriatric = 4,
    All = Pediatric | Adult | Geriatric
}