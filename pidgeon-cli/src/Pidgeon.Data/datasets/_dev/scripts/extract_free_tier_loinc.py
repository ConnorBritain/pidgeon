#!/usr/bin/env python3
"""
Extract Free Tier LOINC Codes
Extracts the top 50 most common laboratory test codes for the free tier.
"""

import json
import csv
from pathlib import Path

# Top 50 most common LOINC codes (lab tests and vitals)
# Source: Common laboratory test panels and vitals
TOP_LOINC_CODES = {
    # Complete Blood Count (CBC)
    "718-7": "Hemoglobin",
    "787-2": "MCV",
    "6690-2": "WBC count",
    "777-3": "Platelets",
    "713-8": "Eosinophils",

    # Comprehensive Metabolic Panel (CMP)
    "2345-7": "Glucose",
    "2160-0": "Creatinine",
    "3094-0": "BUN",
    "2951-2": "Sodium",
    "2823-3": "Potassium",
    "2075-0": "Chloride",
    "2028-9": "CO2",
    "1975-2": "Bilirubin",
    "6768-6": "Alkaline phosphatase",
    "1920-8": "AST",
    "1742-6": "ALT",
    "2885-2": "Protein total",
    "1751-7": "Albumin",
    "17861-6": "Calcium",

    # Lipid Panel
    "2093-3": "Cholesterol total",
    "2085-9": "HDL cholesterol",
    "13457-7": "LDL cholesterol",
    "2571-8": "Triglycerides",

    # Diabetes Monitoring
    "4548-4": "HbA1c",

    # Thyroid
    "3016-3": "TSH",
    "3026-2": "T4 Free",

    # Coagulation
    "5902-2": "PT",
    "6301-6": "INR",
    "3173-2": "aPTT",

    # Urinalysis
    "5767-9": "Appearance of Urine",
    "5778-6": "Color of Urine",
    "5792-7": "Glucose in Urine",
    "5794-3": "Hemoglobin in Urine",
    "5797-6": "Ketones in Urine",
    "5803-2": "pH of Urine",
    "5804-0": "Protein in Urine",

    # Vitals
    "8867-4": "Heart rate",
    "9279-1": "Respiratory rate",
    "8310-5": "Body temperature",
    "2710-2": "Oxygen saturation",
    "8480-6": "Systolic BP",
    "8462-4": "Diastolic BP",
    "29463-7": "Body weight",
    "8302-2": "Body height",
    "39156-5": "BMI",

    # Additional common tests
    "2532-0": "LDH",
    "1988-5": "CRP",
    "3255-7": "Fibrinogen",
    "14804-9": "LDL/HDL ratio"
}

def extract_loinc_codes(loinc_file, output_file):
    """Extract LOINC code data from LoincTableCore.csv file."""
    codes = []
    found_codes = set()

    print(f"Reading LOINC file: {loinc_file}")

    with open(loinc_file, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        for row in reader:
            loinc_num = row.get('LOINC_NUM', '').strip()

            if loinc_num in TOP_LOINC_CODES and loinc_num not in found_codes:
                code_data = {
                    "loincCode": loinc_num,
                    "component": row.get('COMPONENT', '').strip(),
                    "property": row.get('PROPERTY', '').strip(),
                    "timeAspect": row.get('TIME_ASPCT', '').strip(),
                    "system": row.get('SYSTEM', '').strip(),
                    "scale": row.get('SCALE_TYP', '').strip(),
                    "method": row.get('METHOD_TYP', '').strip(),
                    "commonName": TOP_LOINC_CODES[loinc_num],
                    "longCommonName": row.get('LONG_COMMON_NAME', '').strip(),
                    "shortName": row.get('SHORTNAME', '').strip()
                }

                codes.append(code_data)
                found_codes.add(loinc_num)
                print(f"  Found: {loinc_num} - {TOP_LOINC_CODES[loinc_num]} ({len(found_codes)}/50)")

            if len(found_codes) >= 50:
                break

    # Write to JSON
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(codes, f, indent=2, ensure_ascii=False)

    print(f"\n✓ Extracted {len(codes)} LOINC codes to {output_file}")
    print(f"  File size: {output_file.stat().st_size / 1024:.1f} KB")

    if len(codes) < 50:
        print(f"\n⚠ Warning: Only found {len(codes)} out of 50 target codes")
        missing = set(TOP_LOINC_CODES.keys()) - found_codes
        print(f"  Missing count: {len(missing)}")
        if missing:
            print(f"  Missing codes: {', '.join(sorted(missing)[:10])}")

    return codes

if __name__ == "__main__":
    # Paths relative to script location
    script_dir = Path(__file__).parent
    loinc_file = script_dir.parent / "extracted" / "Loinc_2.81" / "LoincTable" / "Loinc.csv"
    output_file = script_dir.parent.parent / "free" / "loinc_common.json"

    print("=" * 60)
    print("FREE TIER LOINC CODE EXTRACTION")
    print("=" * 60)

    if not loinc_file.exists():
        print(f"❌ Error: LOINC file not found at {loinc_file}")
        print("   Please extract Loinc_2.81.zip first")
        exit(1)

    codes = extract_loinc_codes(loinc_file, output_file)

    print("\n" + "=" * 60)
    print("EXTRACTION COMPLETE")
    print("=" * 60)
