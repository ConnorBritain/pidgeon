#!/usr/bin/env python3
"""
Extract Free Tier Medications from NDC Database
Extracts the top 25 most prescribed medications for the free tier.
"""

import json
import csv
from pathlib import Path

# Top 25 most prescribed medications in the US (2024 data)
# Source: CMS prescription frequency data, FDA NDC database
TOP_MEDICATIONS = [
    "Lisinopril",
    "Atorvastatin",
    "Metformin",
    "Amlodipine",
    "Metoprolol",
    "Omeprazole",
    "Albuterol",
    "Losartan",
    "Gabapentin",
    "Levothyroxine",
    "Hydrochlorothiazide",
    "Simvastatin",
    "Furosemide",
    "Sertraline",
    "Escitalopram",
    "Ibuprofen",
    "Acetaminophen",
    "Aspirin",
    "Amoxicillin",
    "Azithromycin",
    "Montelukast",
    "Pantoprazole",
    "Prednisone",
    "Tramadol",
    "Insulin Glargine"
]

def extract_medications(product_file, output_file):
    """Extract medication data from NDC product.txt file."""
    medications = []
    found_meds = set()

    print(f"Reading NDC product file: {product_file}")

    with open(product_file, 'r', encoding='utf-8', errors='ignore') as f:
        reader = csv.DictReader(f, delimiter='\t')

        for row in reader:
            nonproprietary_name = row.get('NONPROPRIETARYNAME', '').strip()
            proprietary_name = row.get('PROPRIETARYNAME', '').strip()

            # Check if this is one of our top medications
            for target_med in TOP_MEDICATIONS:
                if target_med in found_meds:
                    continue

                # Check both proprietary and nonproprietary names
                if (target_med.lower() in nonproprietary_name.lower() or
                    target_med.lower() in proprietary_name.lower()):

                    med_data = {
                        "name": nonproprietary_name or proprietary_name,
                        "genericName": nonproprietary_name,
                        "brandName": proprietary_name,
                        "labelerName": row.get('LABELERNAME', '').strip(),
                        "ndc": row.get('PRODUCTNDC', '').strip(),
                        "strength": row.get('ACTIVE_NUMERATOR_STRENGTH', '').strip(),
                        "unit": row.get('ACTIVE_INGRED_UNIT', '').strip(),
                        "dosageForm": row.get('DOSAGEFORMNAME', '').strip(),
                        "routeName": row.get('ROUTENAME', '').strip(),
                        "packageDescription": row.get('PACKAGEDESCRIPTION', '').strip()[:100]  # Limit length
                    }

                    medications.append(med_data)
                    found_meds.add(target_med)
                    print(f"  Found: {target_med} ({len(found_meds)}/25)")
                    break

            if len(found_meds) >= 25:
                break

    # Write to JSON
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(medications, f, indent=2, ensure_ascii=False)

    print(f"\n✓ Extracted {len(medications)} medications to {output_file}")
    print(f"  File size: {output_file.stat().st_size / 1024:.1f} KB")

    if len(medications) < 25:
        print(f"\n⚠ Warning: Only found {len(medications)} out of 25 target medications")
        missing = set(TOP_MEDICATIONS) - found_meds
        print(f"  Missing: {', '.join(sorted(missing))}")

    return medications

if __name__ == "__main__":
    # Paths relative to script location
    script_dir = Path(__file__).parent
    product_file = script_dir.parent / "extracted" / "ndctext" / "product.txt"
    output_file = script_dir.parent.parent / "free" / "medications.json"

    print("=" * 60)
    print("FREE TIER MEDICATION EXTRACTION")
    print("=" * 60)

    if not product_file.exists():
        print(f"❌ Error: NDC product file not found at {product_file}")
        print("   Please extract ndctext.zip first")
        exit(1)

    medications = extract_medications(product_file, output_file)

    print("\n" + "=" * 60)
    print("EXTRACTION COMPLETE")
    print("=" * 60)
