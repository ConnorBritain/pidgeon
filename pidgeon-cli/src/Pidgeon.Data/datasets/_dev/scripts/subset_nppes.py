#!/usr/bin/env python3
"""
NPPES Provider Subset Extraction Script

This script processes the massive 11GB NPPES CSV file and extracts a manageable
subset of ~10,000 providers across key healthcare specialties.

Usage:
    python subset_nppes.py

Requirements:
    - Run from the datasets directory
    - NPPES_Data_Dissemination_September_2025 folder must exist
    - Will create providers_subset.csv (~50MB output)
"""

import csv
import os
import sys
from collections import defaultdict, Counter
from datetime import datetime

def subset_nppes_providers(input_file, output_file, max_providers=10000):
    """
    Extract a manageable subset of providers from the massive NPPES file

    Strategy:
    1. Stream process to avoid memory issues
    2. Focus on most common specialties
    3. Balance by specialty (max 1000 per specialty)
    4. Prefer individual providers over organizations
    5. Include geographic diversity
    """

    # Comprehensive specialties for realistic healthcare coverage
    # These are Healthcare Provider Taxonomy codes
    priority_taxonomies = {
        # Primary Care (highest priority)
        '208D00000X': 'General Practice',
        '207R00000X': 'Internal Medicine',
        '207Q00000X': 'Family Medicine',
        '261QP2300X': 'Primary Care Clinic',
        '208000000X': 'Pediatrics',
        '207QG0300X': 'Geriatric Medicine',

        # Specialists (high priority)
        '207RC0000X': 'Cardiovascular Disease',
        '207X00000X': 'Orthopedic Surgery',
        '207VX0201X': 'Obstetrics & Gynecology',
        '207RX0202X': 'Diagnostic Radiology',
        '207RE0101X': 'Endocrinology',
        '207RG0100X': 'Gastroenterology',
        '207RH0003X': 'Hematology & Oncology',
        '207RI0200X': 'Infectious Disease',
        '207RN0300X': 'Nephrology',
        '207RP1001X': 'Pulmonary Disease',
        '207RR0500X': 'Rheumatology',

        # Surgical Specialties
        '207T00000X': 'Neurological Surgery',
        '208600000X': 'Surgery',
        '2086S0120X': 'Plastic Surgery',
        '2086S0129X': 'Hand Surgery',
        '2086X0206X': 'Otolaryngology',
        '207W00000X': 'Ophthalmology',
        '2080P0216X': 'Podiatric Surgery',

        # Emergency/Acute Care
        '207P00000X': 'Emergency Medicine',
        '207RH0000X': 'Hospitalist',
        '207L00000X': 'Anesthesiology',
        '207RI0011X': 'Interventional Cardiology',

        # Mental Health & Behavioral
        '2084P0800X': 'Psychiatry',
        '2084P0802X': 'Addiction Psychiatry',
        '2084P0804X': 'Child & Adolescent Psychiatry',
        '1041C0700X': 'Clinical Psychology',
        '251S00000X': 'Community/Behavioral Health',
        '103T00000X': 'Psychologist',

        # Diagnostics & Laboratory
        '207ZP0102X': 'Anatomic Pathology',
        '207ZP0104X': 'Clinical Pathology',
        '207RX0202X': 'Diagnostic Radiology',
        '207RR0500X': 'Radiation Oncology',
        '207ZN0500X': 'Nuclear Medicine',

        # Dental Specialties (Important addition!)
        '122300000X': 'Dentist',
        '1223G0001X': 'General Dentist',
        '1223E0200X': 'Endodontist',
        '1223X0008X': 'Oral Surgeon',
        '1223P0221X': 'Periodontist',
        '1223P0300X': 'Pediatric Dentist',
        '1223D0001X': 'Dental Hygienist',
        '1223D0008X': 'Dental Therapist',

        # Allied Health & Therapy
        '225100000X': 'Physical Therapist',
        '2251C2600X': 'Sports Physical Therapist',
        '2251G0304X': 'Geriatric Physical Therapist',
        '2251N0400X': 'Neurologic Physical Therapist',
        '2251P0200X': 'Pediatric Physical Therapist',
        '225200000X': 'Occupational Therapist',
        '2252S0300X': 'Speech-Language Pathologist',
        '231H00000X': 'Audiologist',

        # Nursing Specialties
        '363L00000X': 'Nurse Practitioner',
        '363LA2100X': 'Acute Care Nurse Practitioner',
        '363LA2200X': 'Adult Health Nurse Practitioner',
        '363LC1500X': 'Critical Care Medicine Nurse Practitioner',
        '363LF0000X': 'Family Nurse Practitioner',
        '363LG0600X': 'Gerontology Nurse Practitioner',
        '363LP0200X': 'Pediatric Nurse Practitioner',
        '363LP2300X': 'Psychiatric Nurse Practitioner',
        '364S00000X': 'Clinical Nurse Specialist',

        # Other Important Specialties
        '207K00000X': 'Allergy & Immunology',
        '207N00000X': 'Dermatology',
        '207Y00000X': 'Otolaryngology',
        '208U00000X': 'Clinical Pharmacology',
        '208VP0000X': 'Pain Medicine',
        '208100000X': 'Physical Medicine & Rehabilitation',
        '2080A0000X': 'Addiction Medicine',

        # Reproductive Health
        '207VF0040X': 'Maternal & Fetal Medicine',
        '207VG0400X': 'Gynecologic Oncology',
        '207VC0200X': 'Reproductive Endocrinology',

        # Pediatric Subspecialties
        '208000000X': 'Pediatrics',
        '2080P0006X': 'Pediatric Allergy/Immunology',
        '2080P0008X': 'Pediatric Cardiology',
        '2080P0201X': 'Pediatric Gastroenterology',
        '2080P0202X': 'Pediatric Hematology-Oncology',
        '2080P0203X': 'Pediatric Infectious Diseases',
        '2080P0204X': 'Pediatric Nephrology',
        '2080P0205X': 'Pediatric Pulmonology',
        '2080P0206X': 'Pediatric Rheumatology',

        # Optometry & Vision
        '152W00000X': 'Optometrist',
        '152WC0802X': 'Contact Lens Optometrist',
        '152WL0500X': 'Low Vision Rehabilitation',
        '152WP0200X': 'Pediatric Optometry',
        '152WS0006X': 'Sports Vision',

        # Podiatry
        '213E00000X': 'Podiatrist',
        '213EP0504X': 'Podiatric Public Medicine',
        '213EP1101X': 'Primary Podiatric Medicine',
        '213ER0200X': 'Podiatric Radiology',
        '213ES0000X': 'Podiatric Sports Medicine',

        # Chiropractic
        '111N00000X': 'Chiropractor',
        '111NI0013X': 'Independent Medical Examiner',
        '111NI0900X': 'Neurology Chiropractor',
        '111NN0400X': 'Nutrition Chiropractor',
        '111NP0017X': 'Pediatric Chiropractor',
        '111NS0005X': 'Sports Physician Chiropractor',

        # Advanced Practice & Specialists
        '367500000X': 'Nurse Anesthetist',
        '367A00000X': 'Advanced Practice Midwife',
        '372600000X': 'Perfusionist',
        '376J00000X': 'Respiratory Therapist',
        '376K00000X': 'Nurse\'s Aide',

        # Nutrition & Wellness
        '133V00000X': 'Dietitian/Nutritionist',
        '133VN1004X': 'Nutrition Education Dietitian',
        '133VN1005X': 'Nutrition Support Dietitian',
        '133VN1006X': 'Nutrition Therapy Dietitian',

        # Laboratory & Diagnostic
        '246Q00000X': 'Specialist/Technologist, Pathology',
        '246QB0000X': 'Blood Banking',
        '246QC1000X': 'Chemistry',
        '246QC2700X': 'Cytotechnology',
        '246QH0000X': 'Hematology',
        '246QH0401X': 'Hemapheresis Practitioner',
        '246QH0600X': 'Histology',
        '246QI0000X': 'Immunology',
        '246QL0900X': 'Laboratory Management',
        '246QL0901X': 'Laboratory Information Management',
        '246QM0706X': 'Medical Technologist',
        '246QM0900X': 'Microbiology',

        # Emergency Medical Services
        '146D00000X': 'Personal Emergency Response Attendant',
        '146L00000X': 'Emergency Medical Technician, Paramedic',
        '146M00000X': 'Emergency Medical Technician, Intermediate',
        '146N00000X': 'Emergency Medical Technician, Basic'
    }

    providers_by_specialty = defaultdict(list)
    providers_per_specialty = max(50, max_providers // len(priority_taxonomies))  # At least 50 per specialty

    print(f"🔍 Processing {input_file}...")
    print(f"📊 Target: {max_providers} providers across {len(priority_taxonomies)} specialties")
    print(f"📈 Max per specialty: {providers_per_specialty}")
    print()

    # Statistics tracking
    row_count = 0
    individual_count = 0
    matched_count = 0

    try:
        with open(input_file, 'r', encoding='utf-8', errors='ignore') as infile:
            reader = csv.DictReader(infile)

            print("📋 Available fields:")
            for i, field in enumerate(reader.fieldnames[:10]):
                print(f"    {field}")
            if len(reader.fieldnames) > 10:
                print(f"    ... and {len(reader.fieldnames) - 10} more")
            print()

            for row in reader:
                row_count += 1

                # Progress indicator
                if row_count % 50000 == 0:
                    total = sum(len(v) for v in providers_by_specialty.values())
                    print(f"📊 Processed {row_count:,} rows | Found {total:,} providers | "
                          f"{matched_count:,} matched")

                # Only individual providers (Entity Type Code = 1)
                # Skip organizations (Entity Type Code = 2)
                if row.get('Entity Type Code') != '1':
                    continue

                individual_count += 1

                # Check if provider has one of our target specialties
                taxonomy = row.get('Healthcare Provider Taxonomy Code_1', '')

                if taxonomy in priority_taxonomies:
                    specialty = priority_taxonomies[taxonomy]
                    matched_count += 1

                    # Skip if we have enough of this specialty
                    if len(providers_by_specialty[specialty]) >= providers_per_specialty:
                        continue

                    # Extract key provider information
                    provider = {
                        'npi': row.get('NPI', ''),
                        'last_name': clean_text(row.get('Provider Last Name (Legal Name)', '')),
                        'first_name': clean_text(row.get('Provider First Name', '')),
                        'middle_name': clean_text(row.get('Provider Middle Name', '')),
                        'credential': clean_text(row.get('Provider Credential Text', '')),
                        'specialty': specialty,
                        'taxonomy_code': taxonomy,

                        # Practice location
                        'practice_address': clean_text(row.get('Provider First Line Business Practice Location Address', '')),
                        'practice_city': clean_text(row.get('Provider Business Practice Location Address City Name', '')),
                        'practice_state': clean_text(row.get('Provider Business Practice Location Address State Name', '')),
                        'practice_zip': clean_zip(row.get('Provider Business Practice Location Address Postal Code', '')),

                        # Additional useful fields
                        'gender': row.get('Provider Gender Code', ''),
                        'enumeration_date': row.get('Provider Enumeration Date', ''),
                        'phone': clean_phone(row.get('Provider Business Practice Location Address Telephone Number', ''))
                    }

                    # Only include if we have basic required fields
                    if provider['npi'] and provider['last_name'] and provider['specialty']:
                        providers_by_specialty[specialty].append(provider)

                    # Check if we have enough total providers
                    total = sum(len(v) for v in providers_by_specialty.values())
                    if total >= max_providers:
                        print(f"✅ Reached target of {max_providers} providers!")
                        break

    except FileNotFoundError:
        print(f"❌ Error: Could not find {input_file}")
        print("   Make sure you've extracted the NPPES zip file first")
        return False
    except Exception as e:
        print(f"❌ Error processing file: {e}")
        return False

    # Write subset to new CSV
    print(f"\n💾 Writing subset to {output_file}...")

    try:
        with open(output_file, 'w', newline='', encoding='utf-8') as outfile:
            fieldnames = [
                'npi', 'last_name', 'first_name', 'middle_name', 'credential',
                'specialty', 'taxonomy_code', 'practice_address', 'practice_city',
                'practice_state', 'practice_zip', 'gender', 'enumeration_date', 'phone'
            ]

            writer = csv.DictWriter(outfile, fieldnames=fieldnames)
            writer.writeheader()

            total_written = 0
            for specialty, providers in providers_by_specialty.items():
                print(f"  📋 {specialty}: {len(providers)} providers")
                for provider in providers:
                    writer.writerow(provider)
                    total_written += 1

        # Final statistics
        file_size_mb = os.path.getsize(output_file) / 1024 / 1024
        print(f"\n🎉 Extraction Complete!")
        print(f"   📊 Total rows processed: {row_count:,}")
        print(f"   👥 Individual providers: {individual_count:,}")
        print(f"   ✅ Specialty matches: {matched_count:,}")
        print(f"   💾 Providers written: {total_written:,}")
        print(f"   📁 Output file size: {file_size_mb:.1f} MB")
        print(f"   🎯 Memory efficiency: {(file_size_mb / 1000):.1f}% of original")

        return True

    except Exception as e:
        print(f"❌ Error writing output file: {e}")
        return False

def clean_text(text):
    """Clean and normalize text fields"""
    if not text:
        return ''
    return text.strip().title()

def clean_zip(zip_code):
    """Clean and normalize ZIP codes"""
    if not zip_code:
        return ''
    # Take first 5 digits only
    zip_clean = ''.join(filter(str.isdigit, zip_code))
    return zip_clean[:5] if len(zip_clean) >= 5 else ''

def clean_phone(phone):
    """Clean and normalize phone numbers"""
    if not phone:
        return ''
    # Remove all non-digits
    digits = ''.join(filter(str.isdigit, phone))
    # Format as (XXX) XXX-XXXX if 10 digits
    if len(digits) == 10:
        return f"({digits[:3]}) {digits[3:6]}-{digits[6:]}"
    return digits

def main():
    """Main execution function"""
    print("🏥 NPPES Provider Subset Extraction")
    print("=" * 50)

    # Check if we're in the right directory
    if not os.path.exists('NPPES_Data_Dissemination_September_2025'):
        print("❌ Error: NPPES_Data_Dissemination_September_2025 directory not found")
        print("   Make sure you're running this script from the datasets directory")
        print("   and that you've extracted the NPPES zip file")
        return

    input_file = 'NPPES_Data_Dissemination_September_2025/npidata_pfile_20050523-20250907.csv'
    output_file = 'providers_subset.csv'

    # Check if input file exists
    if not os.path.exists(input_file):
        print(f"❌ Error: {input_file} not found")
        print("   Make sure the NPPES zip file has been extracted")
        return

    # Show file size
    input_size_gb = os.path.getsize(input_file) / 1024 / 1024 / 1024
    print(f"📁 Input file size: {input_size_gb:.1f} GB")
    print(f"⚡ Starting extraction... (this may take 10-30 minutes)")
    print()

    start_time = datetime.now()
    success = subset_nppes_providers(input_file, output_file, max_providers=10000)
    end_time = datetime.now()

    duration = end_time - start_time
    print(f"\n⏱️  Total time: {duration}")

    if success:
        print("\n🚀 Next steps:")
        print("   1. Run: python process_icd_codes.py")
        print("   2. Run: python load_data_to_sqlite.py")
        print("   3. Test with: SELECT COUNT(*) FROM providers;")
    else:
        print("\n❌ Extraction failed. Check the error messages above.")

if __name__ == '__main__':
    main()