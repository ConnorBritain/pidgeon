#!/usr/bin/env python3
"""
Extract Free Tier ICD-10 Codes
Extracts the top 100 most common diagnosis codes for the free tier.
"""

import json
from pathlib import Path

# Top 100 most common ICD-10 codes in ambulatory care
# Source: CMS ambulatory care frequency data
TOP_ICD10_CODES = [
    # Cardiovascular
    "I10", "I25.10", "I50.9", "I48.91", "I73.9",
    # Diabetes
    "E11.9", "E11.65", "E11.22", "E11.40", "E10.9",
    # Lipid disorders
    "E78.5", "E78.0", "E78.2",
    # Respiratory
    "J44.9", "J45.909", "J06.9", "J02.9", "J18.9",
    # Mental Health
    "F32.9", "F41.1", "F33.9", "F43.10", "F90.0",
    # Musculoskeletal
    "M54.5", "M19.90", "M25.50", "M79.3", "M17.11",
    # Kidney
    "N18.3", "N18.9", "N39.0",
    # Preventive/Screening
    "Z00.00", "Z00.01", "Z23", "Z12.11", "Z79.899",
    # Metabolic/Endocrine
    "E03.9", "E66.9", "E55.9",
    # GI
    "K21.9", "K58.9", "K76.0",
    # Infectious
    "B34.9", "A49.9",
    # Genitourinary
    "N40.0", "N95.1",
    # Dermatologic
    "L70.0", "L30.9",
    # Hematologic
    "D64.9", "D50.9",
    # Neurologic
    "G43.909", "G47.00", "R51",
    # Eye/ENT
    "H10.9", "H66.90",
    # Symptoms/Signs (R codes)
    "R05", "R10.9", "R50.9", "R53.83", "R06.02",
    # Additional common codes to reach 100
    "I11.9", "I73.00", "E04.9", "E87.6", "F17.210",
    "G89.29", "J30.9", "K59.00", "M62.81", "N73.9",
    "R07.9", "R63.4", "Z68.41", "Z79.4", "Z86.73",
    "E11.69", "I25.119", "J44.1", "K63.5", "M15.9",
    "M47.816", "N18.4", "R73.09", "Z00.129", "Z13.220",
    "E78.1", "G62.9", "I50.22", "K44.9", "M81.0",
    "N20.0", "R31.9", "Z12.31", "Z79.82", "Z87.891",
    "E66.01", "G44.1", "I27.20", "K64.9", "M48.06",
    "Z01.419", "Z48.02", "Z51.81", "Z79.01", "Z88.9"
]

def extract_icd10_codes(codes_file, output_file):
    """Extract ICD-10 code data from icd10cm-codes file."""
    codes = []
    found_codes = set()

    # Normalize target codes by removing dots for comparison
    # File format: I10, E119 (no dots)
    # Target list: I10, E11.9 (has dots)
    normalized_targets = {code.replace('.', ''): code for code in TOP_ICD10_CODES}

    print(f"Reading ICD-10 codes file: {codes_file}")
    print(f"Looking for {len(normalized_targets)} target codes...")

    with open(codes_file, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line:
                continue

            # ICD-10 format: "CODE DESCRIPTION" (code has no dots)
            parts = line.split(maxsplit=1)
            if len(parts) == 2:
                file_code, description = parts

                # Check if this file code (without dots) matches any of our targets
                if file_code in normalized_targets:
                    original_code = normalized_targets[file_code]
                    if original_code not in found_codes:
                        codes.append({
                            "code": original_code,  # Use the dotted version
                            "description": description.strip(),
                            "isBillable": True,  # File codes are billable
                            "category": categorize_icd10(original_code)
                        })
                        found_codes.add(original_code)
                        print(f"  Found: {original_code} ({len(found_codes)}/100)")

            if len(found_codes) >= 100:
                break

    # Write to JSON
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(codes, f, indent=2, ensure_ascii=False)

    print(f"\n✓ Extracted {len(codes)} ICD-10 codes to {output_file}")
    print(f"  File size: {output_file.stat().st_size / 1024:.1f} KB")

    if len(codes) < 100:
        print(f"\n⚠ Warning: Only found {len(codes)} out of 100 target codes")
        missing = set(TOP_ICD10_CODES) - found_codes
        print(f"  Missing count: {len(missing)}")

    return codes

def categorize_icd10(code):
    """Simple categorization based on ICD-10 code prefix."""
    prefix = code[0]

    categories = {
        'A': 'Infectious Diseases',
        'B': 'Infectious Diseases',
        'C': 'Neoplasms',
        'D': 'Blood Disorders',
        'E': 'Endocrine/Metabolic',
        'F': 'Mental Health',
        'G': 'Nervous System',
        'H': 'Eye/Ear',
        'I': 'Cardiovascular',
        'J': 'Respiratory',
        'K': 'Digestive',
        'L': 'Skin',
        'M': 'Musculoskeletal',
        'N': 'Genitourinary',
        'O': 'Pregnancy',
        'P': 'Perinatal',
        'Q': 'Congenital',
        'R': 'Symptoms/Signs',
        'S': 'Injury',
        'T': 'Injury',
        'V': 'External Causes',
        'W': 'External Causes',
        'X': 'External Causes',
        'Y': 'External Causes',
        'Z': 'Health Services'
    }

    return categories.get(prefix, 'Other')

if __name__ == "__main__":
    # Paths relative to script location
    script_dir = Path(__file__).parent
    codes_file = script_dir.parent / "extracted" / "icd10cm-2026" / "icd10cm-codes-2026.txt"
    output_file = script_dir.parent.parent / "free" / "icd10_common.json"

    print("=" * 60)
    print("FREE TIER ICD-10 CODE EXTRACTION")
    print("=" * 60)

    if not codes_file.exists():
        print(f"❌ Error: ICD-10 codes file not found at {codes_file}")
        print("   Please extract icd10cm-2026.zip first")
        exit(1)

    codes = extract_icd10_codes(codes_file, output_file)

    print("\n" + "=" * 60)
    print("EXTRACTION COMPLETE")
    print("=" * 60)
