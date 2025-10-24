#!/usr/bin/env python3
"""
ICD-10 Text Format Loader

This script processes the CMS ICD-10-CM text file and extracts the most
commonly used diagnosis codes for realistic message generation.

Usage:
    python load_icd10_text.py

Input:
    icd10cm-Code Descriptions-2026/icd10cm-order-2026.txt

Output:
    common_icd10_codes.csv (prioritized subset)
    icd10_all_codes.csv (complete dataset for Pro/Enterprise tiers)
"""

import csv
import re
from collections import Counter, defaultdict

def parse_icd10_line(line):
    """
    Parse ICD-10 text line format:
    00001 A00     0 Cholera                                    Cholera

    Returns: {
        'line_num': '00001',
        'code': 'A00',
        'hierarchy': '0',
        'short_desc': 'Cholera',
        'long_desc': 'Cholera'
    }
    """
    if not line or len(line) < 50:
        return None

    try:
        # Fixed-width parsing based on format
        line_num = line[0:5].strip()
        code = line[6:13].strip()
        hierarchy = line[14:15].strip()
        short_desc = line[16:74].strip()
        long_desc = line[75:].strip() if len(line) > 75 else short_desc

        return {
            'line_num': line_num,
            'code': code,
            'hierarchy': hierarchy,
            'short_desc': short_desc,
            'long_desc': long_desc,
            'chapter': get_icd10_chapter(code),
            'billable': hierarchy == '1',
            'priority': calculate_priority(code, long_desc)
        }
    except Exception as e:
        print(f"Warning: Could not parse line: {line[:50]}... ({e})")
        return None

def get_icd10_chapter(code):
    """Determine ICD-10 chapter based on first character"""
    if not code:
        return "Unknown"

    first_char = code[0].upper()

    chapter_map = {
        'A': 'Infectious diseases (A00-B99)',
        'B': 'Infectious diseases (A00-B99)',
        'C': 'Neoplasms (C00-D49)',
        'D': 'Neoplasms/Blood (D00-D89)',
        'E': 'Endocrine/Metabolic (E00-E89)',
        'F': 'Mental/Behavioral (F01-F99)',
        'G': 'Nervous system (G00-G99)',
        'H': 'Eye/Ear (H00-H95)',
        'I': 'Circulatory (I00-I99)',
        'J': 'Respiratory (J00-J99)',
        'K': 'Digestive (K00-K95)',
        'L': 'Skin (L00-L99)',
        'M': 'Musculoskeletal (M00-M99)',
        'N': 'Genitourinary (N00-N99)',
        'O': 'Pregnancy (O00-O9A)',
        'P': 'Perinatal (P00-P96)',
        'Q': 'Congenital (Q00-Q99)',
        'R': 'Symptoms/Signs (R00-R99)',
        'S': 'Injury (S00-T88)',
        'T': 'Injury/Poisoning (S00-T88)',
        'U': 'Special purposes (U00-U85)',
        'V': 'External causes (V00-Y99)',
        'W': 'External causes (V00-Y99)',
        'X': 'External causes (V00-Y99)',
        'Y': 'External causes (V00-Y99)',
        'Z': 'Health status (Z00-Z99)'
    }

    return chapter_map.get(first_char, f"Unknown ({first_char})")

def calculate_priority(code, description):
    """
    Calculate priority score for diagnosis codes
    Lower number = higher priority (more commonly used)
    """

    # Ultra high priority - most common ambulatory diagnoses
    ultra_high = {
        'I10': 1,      # Essential hypertension
        'E11.9': 1,    # Type 2 diabetes
        'E78.5': 1,    # Hyperlipidemia
        'Z00.00': 1,   # General exam
        'Z23': 1,      # Immunization
        'J06.9': 1,    # Upper respiratory infection
        'R50.9': 1,    # Fever
        'R05': 1,      # Cough
        'R51.9': 1,    # Headache
        'M54.5': 1,    # Low back pain
    }

    if code in ultra_high:
        return ultra_high[code]

    desc_lower = description.lower()

    # Very high priority patterns (common conditions)
    very_high_patterns = [
        ('covid', 2), ('coronavirus', 2),
        ('diabetes', 3), ('hypertension', 3),
        ('asthma', 4), ('copd', 4),
        ('pneumonia', 5), ('bronchitis', 5),
        ('urinary tract', 6), ('gastroenteritis', 6),
        ('otitis', 7), ('pharyngitis', 7), ('sinusitis', 7)
    ]

    for pattern, priority in very_high_patterns:
        if pattern in desc_lower:
            return priority

    # High priority patterns
    high_patterns = [
        'screening', 'encounter for', 'immunization', 'vaccination',
        'chronic', 'acute', 'pain', 'infection', 'disorder'
    ]

    for pattern in high_patterns:
        if pattern in desc_lower:
            return 10

    # Z codes (administrative/screening) - often used
    if code.startswith('Z'):
        return 15

    # R codes (symptoms) - very common in ED/urgent care
    if code.startswith('R'):
        return 20

    # E codes (endocrine/metabolic) - common chronic conditions
    if code.startswith('E'):
        return 25

    # J codes (respiratory) - very common
    if code.startswith('J'):
        return 25

    # I codes (cardiovascular) - common chronic
    if code.startswith('I'):
        return 30

    # M codes (musculoskeletal) - common in ortho/primary care
    if code.startswith('M'):
        return 35

    # Billable codes generally more useful than category codes
    # (hierarchy is checked in the main function)

    # Default priority
    return 50

def load_icd10_codes(input_file):
    """Load and parse ICD-10 codes from text file"""

    print(f"📊 Loading ICD-10 codes from {input_file}...")

    codes = []
    billable_count = 0
    category_count = 0

    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                if line_num % 10000 == 0:
                    print(f"   📋 Processed {line_num:,} lines...")

                code_data = parse_icd10_line(line)

                if code_data:
                    codes.append(code_data)

                    if code_data['billable']:
                        billable_count += 1
                    else:
                        category_count += 1

        print(f"   ✅ Total codes loaded: {len(codes):,}")
        print(f"   📊 Billable codes: {billable_count:,}")
        print(f"   📁 Category codes: {category_count:,}")

        return codes

    except FileNotFoundError:
        print(f"❌ Error: Could not find {input_file}")
        return []
    except Exception as e:
        print(f"❌ Error loading file: {e}")
        return []

def create_common_subset(codes, max_codes=2000):
    """Create prioritized subset of most commonly used codes"""

    print(f"\n🎯 Creating common subset ({max_codes} codes)...")

    # Only include billable codes for the common subset
    billable_codes = [c for c in codes if c['billable']]

    # Sort by priority (lower = better)
    billable_codes.sort(key=lambda x: (x['priority'], x['code']))

    # Get chapter distribution
    chapter_counts = Counter(c['chapter'] for c in billable_codes[:max_codes])

    print(f"   📊 Chapter distribution in common subset:")
    for chapter, count in chapter_counts.most_common(10):
        print(f"      {count:3d} codes - {chapter}")

    return billable_codes[:max_codes]

def write_csv(codes, output_file, include_all_fields=False):
    """Write codes to CSV file"""

    print(f"💾 Writing {len(codes):,} codes to {output_file}...")

    try:
        with open(output_file, 'w', newline='', encoding='utf-8') as f:
            if include_all_fields:
                fieldnames = ['code', 'short_desc', 'long_desc', 'chapter',
                             'hierarchy', 'billable', 'priority']
            else:
                fieldnames = ['code', 'description', 'chapter', 'priority']

            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()

            for code_data in codes:
                if include_all_fields:
                    writer.writerow({
                        'code': code_data['code'],
                        'short_desc': code_data['short_desc'],
                        'long_desc': code_data['long_desc'],
                        'chapter': code_data['chapter'],
                        'hierarchy': code_data['hierarchy'],
                        'billable': code_data['billable'],
                        'priority': code_data['priority']
                    })
                else:
                    writer.writerow({
                        'code': code_data['code'],
                        'description': code_data['long_desc'],
                        'chapter': code_data['chapter'],
                        'priority': code_data['priority']
                    })

        print(f"   ✅ File written successfully")

    except Exception as e:
        print(f"❌ Error writing file: {e}")

def create_summary_stats(codes):
    """Create summary statistics"""

    print(f"\n📊 ICD-10 Dataset Summary:")
    print(f"   Total codes: {len(codes):,}")

    billable = [c for c in codes if c['billable']]
    categories = [c for c in codes if not c['billable']]

    print(f"   Billable codes: {len(billable):,}")
    print(f"   Category codes: {len(categories):,}")

    # Chapter breakdown
    chapter_counts = Counter(c['chapter'] for c in codes)
    print(f"\n   📚 Codes per chapter:")
    for chapter, count in sorted(chapter_counts.items(), key=lambda x: x[1], reverse=True)[:15]:
        print(f"      {count:5,} - {chapter}")

    # Priority distribution
    priority_ranges = {
        'Ultra High (1-5)': len([c for c in billable if c['priority'] <= 5]),
        'Very High (6-10)': len([c for c in billable if 6 <= c['priority'] <= 10]),
        'High (11-20)': len([c for c in billable if 11 <= c['priority'] <= 20]),
        'Medium (21-35)': len([c for c in billable if 21 <= c['priority'] <= 35]),
        'Normal (36+)': len([c for c in billable if c['priority'] > 35])
    }

    print(f"\n   🎯 Priority distribution (billable codes only):")
    for range_name, count in priority_ranges.items():
        print(f"      {count:6,} - {range_name}")

def main():
    """Main execution"""

    print("🏥 ICD-10-CM Text Format Loader")
    print("=" * 50)

    # Input file
    input_file = 'icd10cm-Code Descriptions-2026/icd10cm-order-2026.txt'

    # Load all codes
    all_codes = load_icd10_codes(input_file)

    if not all_codes:
        print("❌ No codes loaded. Exiting.")
        return

    # Create summary statistics
    create_summary_stats(all_codes)

    # Create common subset (Free tier - 2000 most common)
    common_codes = create_common_subset(all_codes, max_codes=2000)
    write_csv(common_codes, 'common_icd10_codes.csv', include_all_fields=False)

    # Write complete dataset (Professional/Enterprise tiers - all billable codes)
    all_billable = [c for c in all_codes if c['billable']]
    write_csv(all_billable, 'icd10_all_billable_codes.csv', include_all_fields=True)

    # Write absolute complete dataset (including category codes)
    write_csv(all_codes, 'icd10_complete_dataset.csv', include_all_fields=True)

    print(f"\n🎉 Processing Complete!")
    print(f"   📁 Files created:")
    print(f"      • common_icd10_codes.csv ({len(common_codes):,} codes) - Free tier")
    print(f"      • icd10_all_billable_codes.csv ({len(all_billable):,} codes) - Pro/Enterprise")
    print(f"      • icd10_complete_dataset.csv ({len(all_codes):,} codes) - Complete reference")

    print(f"\n🚀 Next steps:")
    print(f"   1. Run: python subset_nppes.py")
    print(f"   2. Run: python load_data_to_sqlite.py")

if __name__ == '__main__':
    main()
