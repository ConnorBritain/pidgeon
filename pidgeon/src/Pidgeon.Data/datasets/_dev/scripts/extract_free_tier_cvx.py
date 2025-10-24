#!/usr/bin/env python3
"""
Extract CVX Vaccine Codes
Converts CVX text file to JSON for free tier.
"""

import json
from pathlib import Path

def extract_cvx_codes(cvx_file, output_file):
    """Convert CVX text file to JSON."""
    vaccines = []

    print(f"Reading CVX file: {cvx_file}")

    with open(cvx_file, 'r', encoding='utf-8-sig') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue

            # CVX format: CVX_CODE|SHORT_DESC|FULL_NAME|NOTES|STATUS|NON_VACCINE|LAST_UPDATED
            parts = line.split('|')
            if len(parts) >= 5:
                cvx_code = parts[0].strip()
                short_desc = parts[1].strip()
                full_name = parts[2].strip()
                notes = parts[3].strip() if len(parts) > 3 else ""
                status = parts[4].strip() if len(parts) > 4 else "Unknown"
                non_vaccine = parts[5].strip().lower() == 'true' if len(parts) > 5 else False
                last_updated = parts[6].strip() if len(parts) > 6 else ""

                # Only include active vaccines
                if status == "Active" and not non_vaccine:
                    vaccine_data = {
                        "cvxCode": cvx_code,
                        "shortDescription": short_desc,
                        "fullName": full_name,
                        "notes": notes,
                        "status": status,
                        "lastUpdated": last_updated
                    }
                    vaccines.append(vaccine_data)

    print(f"  Found {len(vaccines)} active vaccines")

    # Write to JSON
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(vaccines, f, indent=2, ensure_ascii=False)

    print(f"\n✓ Extracted {len(vaccines)} vaccine codes to {output_file}")
    print(f"  File size: {output_file.stat().st_size / 1024:.1f} KB")

    return vaccines

if __name__ == "__main__":
    # Paths relative to script location
    script_dir = Path(__file__).parent
    cvx_file = script_dir.parent / "extracted" / "cvx.txt"
    output_file = script_dir.parent.parent / "free" / "vaccines_cvx.json"

    print("=" * 60)
    print("CVX VACCINE CODE EXTRACTION")
    print("=" * 60)

    if not cvx_file.exists():
        print(f"❌ Error: CVX file not found at {cvx_file}")
        exit(1)

    vaccines = extract_cvx_codes(cvx_file, output_file)

    print("\n" + "=" * 60)
    print("EXTRACTION COMPLETE")
    print("=" * 60)
