#!/usr/bin/env python3
"""
SQLite Data Loading Script

This script loads all the extracted healthcare datasets into a SQLite database
for use with Pidgeon's realistic message generation.

Usage:
    python load_data_to_sqlite.py

Requirements:
    - Run after subset_nppes.py and process_icd_codes.py
    - Creates/extends pidgeon.db database

Files processed:
    - providers_subset.csv (from subset_nppes.py)
    - common_icd10_codes.csv (from process_icd_codes.py)
    - NDC product data
    - LOINC lab tests
    - ZIP codes
    - Name tables from JSON
"""

import sqlite3
import csv
import json
import os
import sys
from pathlib import Path
from collections import defaultdict

def create_database_schema(db_path):
    """Create or extend the database schema for healthcare data"""

    print(f"🗄️  Setting up database schema in {db_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Enable foreign keys
    cur.execute("PRAGMA foreign_keys = ON")

    # Names tables
    cur.execute("""
        CREATE TABLE IF NOT EXISTS names_first (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            gender TEXT, -- 'M', 'F', or NULL for unisex
            frequency_rank INTEGER,
            UNIQUE(name, gender)
        )
    """)

    cur.execute("""
        CREATE TABLE IF NOT EXISTS names_last (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            frequency_rank INTEGER
        )
    """)

    # Medications table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS medications (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            ndc_code TEXT UNIQUE,
            proprietary_name TEXT,
            nonproprietary_name TEXT,
            dosage_form TEXT,
            strength TEXT,
            route TEXT,
            labeler_name TEXT,
            active_ingredient TEXT,
            frequency_rank INTEGER,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # Lab tests table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS lab_tests (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            loinc_code TEXT UNIQUE NOT NULL,
            long_common_name TEXT,
            component TEXT,
            property TEXT,
            system TEXT,
            scale_type TEXT,
            units TEXT,
            normal_low REAL,
            normal_high REAL,
            frequency_rank INTEGER,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # Providers table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS providers (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            npi TEXT UNIQUE NOT NULL,
            last_name TEXT,
            first_name TEXT,
            middle_name TEXT,
            credential TEXT,
            specialty TEXT,
            taxonomy_code TEXT,
            practice_address TEXT,
            practice_city TEXT,
            practice_state TEXT,
            practice_zip TEXT,
            gender TEXT,
            phone TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # Diagnoses table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS diagnoses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            icd10_code TEXT UNIQUE NOT NULL,
            description TEXT,
            chapter TEXT,
            priority INTEGER,
            frequency_rank INTEGER,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # ZIP codes table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS zip_codes (
            zip TEXT PRIMARY KEY,
            city TEXT,
            state_id TEXT,
            state_name TEXT,
            county_name TEXT,
            latitude REAL,
            longitude REAL,
            population INTEGER,
            timezone TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # Vaccines table
    cur.execute("""
        CREATE TABLE IF NOT EXISTS vaccines (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            cvx_code TEXT UNIQUE NOT NULL,
            short_description TEXT,
            full_name TEXT,
            vaccine_status TEXT,
            notes TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    # Create performance indexes
    indexes = [
        "CREATE INDEX IF NOT EXISTS idx_names_first_gender ON names_first(gender)",
        "CREATE INDEX IF NOT EXISTS idx_names_first_rank ON names_first(frequency_rank)",
        "CREATE INDEX IF NOT EXISTS idx_names_last_rank ON names_last(frequency_rank)",
        "CREATE INDEX IF NOT EXISTS idx_medications_rank ON medications(frequency_rank)",
        "CREATE INDEX IF NOT EXISTS idx_medications_generic ON medications(nonproprietary_name)",
        "CREATE INDEX IF NOT EXISTS idx_lab_tests_rank ON lab_tests(frequency_rank)",
        "CREATE INDEX IF NOT EXISTS idx_lab_tests_component ON lab_tests(component)",
        "CREATE INDEX IF NOT EXISTS idx_providers_specialty ON providers(specialty)",
        "CREATE INDEX IF NOT EXISTS idx_providers_state ON providers(practice_state)",
        "CREATE INDEX IF NOT EXISTS idx_diagnoses_chapter ON diagnoses(chapter)",
        "CREATE INDEX IF NOT EXISTS idx_diagnoses_priority ON diagnoses(priority)",
        "CREATE INDEX IF NOT EXISTS idx_zip_codes_state ON zip_codes(state_id)",
    ]

    for index_sql in indexes:
        cur.execute(index_sql)

    conn.commit()
    conn.close()
    print("   ✅ Database schema created successfully")

def load_names_from_json(db_path):
    """Load first and last names from the existing JSON files"""

    print("👥 Loading names from JSON files...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM names_first")
    cur.execute("DELETE FROM names_last")

    # Load male names
    male_json_path = '../standards/hl7v23/tables/FirstNameMale.json'
    if os.path.exists(male_json_path):
        with open(male_json_path, 'r', encoding='utf-8') as f:
            male_data = json.load(f)
            for i, item in enumerate(male_data.get('values', [])[:1000]):  # Top 1000
                cur.execute("""
                    INSERT OR IGNORE INTO names_first (name, gender, frequency_rank)
                    VALUES (?, 'M', ?)
                """, (item.get('value', ''), i + 1))
        print(f"   📋 Loaded {len(male_data.get('values', [])[:1000])} male names")

    # Load female names
    female_json_path = '../standards/hl7v23/tables/FirstNameFemale.json'
    if os.path.exists(female_json_path):
        with open(female_json_path, 'r', encoding='utf-8') as f:
            female_data = json.load(f)
            for i, item in enumerate(female_data.get('values', [])[:1000]):  # Top 1000
                cur.execute("""
                    INSERT OR IGNORE INTO names_first (name, gender, frequency_rank)
                    VALUES (?, 'F', ?)
                """, (item.get('value', ''), i + 1))
        print(f"   📋 Loaded {len(female_data.get('values', [])[:1000])} female names")

    # Load last names
    last_json_path = '../standards/hl7v23/tables/LastName.json'
    if os.path.exists(last_json_path):
        with open(last_json_path, 'r', encoding='utf-8') as f:
            last_data = json.load(f)
            for i, item in enumerate(last_data.get('values', [])[:1500]):  # Top 1500
                cur.execute("""
                    INSERT OR IGNORE INTO names_last (name, frequency_rank)
                    VALUES (?, ?)
                """, (item.get('value', ''), i + 1))
        print(f"   📋 Loaded {len(last_data.get('values', [])[:1500])} last names")

    conn.commit()
    conn.close()

def load_providers_from_csv(db_path, csv_path):
    """Load providers from the NPPES subset CSV"""

    if not os.path.exists(csv_path):
        print(f"⚠️  Warning: {csv_path} not found - run subset_nppes.py first")
        return

    print(f"👨‍⚕️ Loading providers from {csv_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM providers")

    count = 0
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        for row in reader:
            cur.execute("""
                INSERT OR IGNORE INTO providers (
                    npi, last_name, first_name, middle_name, credential,
                    specialty, taxonomy_code, practice_address, practice_city,
                    practice_state, practice_zip, gender, phone
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                row.get('npi', ''),
                row.get('last_name', ''),
                row.get('first_name', ''),
                row.get('middle_name', ''),
                row.get('credential', ''),
                row.get('specialty', ''),
                row.get('taxonomy_code', ''),
                row.get('practice_address', ''),
                row.get('practice_city', ''),
                row.get('practice_state', ''),
                row.get('practice_zip', ''),
                row.get('gender', ''),
                row.get('phone', '')
            ))
            count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} providers")

def load_medications_from_ndc(db_path, ndc_file_path, limit=2000):
    """Load medications from NDC product file"""

    if not os.path.exists(ndc_file_path):
        print(f"⚠️  Warning: {ndc_file_path} not found")
        return

    print(f"💊 Loading medications from {ndc_file_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM medications")

    # Common medications for prioritization
    common_generics = [
        'lisinopril', 'metformin', 'atorvastatin', 'amlodipine', 'omeprazole',
        'simvastatin', 'levothyroxine', 'metoprolol', 'losartan', 'gabapentin',
        'hydrochlorothiazide', 'acetaminophen', 'ibuprofen', 'aspirin',
        'albuterol', 'prednisone', 'furosemide', 'sertraline', 'fluoxetine'
    ]

    count = 0
    with open(ndc_file_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f, delimiter='\t')

        for row in reader:
            if count >= limit:
                break

            # Skip discontinued products
            if row.get('ENDMARKETINGDATE'):
                continue

            # Calculate priority rank
            generic = (row.get('NONPROPRIETARYNAME') or '').lower()
            rank = 999
            for i, common in enumerate(common_generics):
                if common in generic:
                    rank = i + 1
                    break

            cur.execute("""
                INSERT OR IGNORE INTO medications (
                    ndc_code, proprietary_name, nonproprietary_name,
                    dosage_form, strength, route, labeler_name,
                    active_ingredient, frequency_rank
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                row.get('PRODUCTNDC', ''),
                row.get('PROPRIETARYNAME', ''),
                row.get('NONPROPRIETARYNAME', ''),
                row.get('DOSAGEFORMNAME', ''),
                row.get('ACTIVE_NUMERATOR_STRENGTH', ''),
                row.get('ROUTENAME', ''),
                row.get('LABELERNAME', ''),
                row.get('SUBSTANCENAME', ''),
                rank + count
            ))
            count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} medications")

def load_lab_tests_from_loinc(db_path, loinc_file_path, limit=1000):
    """Load lab tests from LOINC CSV"""

    if not os.path.exists(loinc_file_path):
        print(f"⚠️  Warning: {loinc_file_path} not found")
        return

    print(f"🧪 Loading lab tests from {loinc_file_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM lab_tests")

    # Common lab test keywords for prioritization
    common_labs = [
        'glucose', 'cholesterol', 'hemoglobin', 'hematocrit', 'sodium',
        'potassium', 'chloride', 'carbon dioxide', 'urea nitrogen', 'creatinine',
        'protein', 'albumin', 'bilirubin', 'ast', 'alt', 'alkaline phosphatase'
    ]

    count = 0
    with open(loinc_file_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        for row in reader:
            if count >= limit:
                break

            loinc_num = row.get('LOINC_NUM', '')
            component = row.get('COMPONENT', '')
            long_name = row.get('LONG_COMMON_NAME', '')

            if not loinc_num or not component:
                continue

            # Calculate priority
            component_lower = component.lower()
            rank = 999
            for i, common in enumerate(common_labs):
                if common in component_lower:
                    rank = i + 1
                    break

            cur.execute("""
                INSERT OR IGNORE INTO lab_tests (
                    loinc_code, long_common_name, component, property,
                    system, scale_type, units, frequency_rank
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                loinc_num,
                long_name,
                component,
                row.get('PROPERTY', ''),
                row.get('SYSTEM', ''),
                row.get('SCALE_TYP', ''),
                row.get('EXAMPLE_UNITS', ''),
                rank + count
            ))
            count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} lab tests")

def load_diagnoses_from_csv(db_path, csv_path):
    """Load diagnoses from processed ICD-10 CSV"""

    if not os.path.exists(csv_path):
        print(f"⚠️  Warning: {csv_path} not found - run process_icd_codes.py first")
        return

    print(f"🩺 Loading diagnoses from {csv_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM diagnoses")

    count = 0
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        for row in reader:
            cur.execute("""
                INSERT OR IGNORE INTO diagnoses (
                    icd10_code, description, chapter, priority, frequency_rank
                ) VALUES (?, ?, ?, ?, ?)
            """, (
                row.get('code', ''),
                row.get('description', ''),
                row.get('chapter', ''),
                int(row.get('priority', 999)),
                count + 1
            ))
            count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} diagnoses")

def load_zip_codes_from_csv(db_path, csv_path):
    """Load ZIP codes from simplemaps CSV"""

    if not os.path.exists(csv_path):
        print(f"⚠️  Warning: {csv_path} not found")
        return

    print(f"📮 Loading ZIP codes from {csv_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM zip_codes")

    count = 0
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)

        for row in reader:
            cur.execute("""
                INSERT OR IGNORE INTO zip_codes (
                    zip, city, state_id, state_name, county_name,
                    latitude, longitude, population, timezone
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                row.get('zip', ''),
                row.get('city', ''),
                row.get('state_id', ''),
                row.get('state_name', ''),
                row.get('county_name', ''),
                float(row.get('lat', 0)) if row.get('lat') else None,
                float(row.get('lng', 0)) if row.get('lng') else None,
                int(row.get('population', 0)) if row.get('population') else None,
                row.get('timezone', '')
            ))
            count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} ZIP codes")

def load_vaccines_from_txt(db_path, txt_path):
    """Load vaccines from CVX text file"""

    if not os.path.exists(txt_path):
        print(f"⚠️  Warning: {txt_path} not found")
        return

    print(f"💉 Loading vaccines from {txt_path}...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Clear existing data
    cur.execute("DELETE FROM vaccines")

    count = 0
    with open(txt_path, 'r', encoding='utf-8') as f:
        for line in f:
            # CVX format: code|short_desc|full_name|notes|status|...
            parts = line.strip().split('|')
            if len(parts) >= 5:
                cur.execute("""
                    INSERT OR IGNORE INTO vaccines (
                        cvx_code, short_description, full_name, vaccine_status, notes
                    ) VALUES (?, ?, ?, ?, ?)
                """, (
                    parts[0].strip(),
                    parts[1].strip(),
                    parts[2].strip(),
                    parts[4].strip(),
                    parts[3].strip() if len(parts) > 3 else ''
                ))
                count += 1

    conn.commit()
    conn.close()
    print(f"   ✅ Loaded {count} vaccines")

def create_summary_report(db_path):
    """Create a summary report of loaded data"""

    print("\n📊 Creating summary report...")

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # Get counts for each table
    tables = [
        'names_first', 'names_last', 'medications', 'lab_tests',
        'providers', 'diagnoses', 'zip_codes', 'vaccines'
    ]

    report = {
        'database_file': db_path,
        'table_counts': {},
        'samples': {}
    }

    for table in tables:
        try:
            cur.execute(f"SELECT COUNT(*) FROM {table}")
            count = cur.fetchone()[0]
            report['table_counts'][table] = count

            # Get a few sample records
            cur.execute(f"SELECT * FROM {table} LIMIT 3")
            samples = cur.fetchall()
            report['samples'][table] = samples

        except sqlite3.Error as e:
            report['table_counts'][table] = f"Error: {e}"

    conn.close()

    # Print summary
    print(f"\n🎉 Data Loading Complete!")
    print(f"   📁 Database: {db_path}")
    print(f"   📊 Table counts:")

    for table, count in report['table_counts'].items():
        print(f"      {table:15}: {count:,}")

    # Calculate total size
    if os.path.exists(db_path):
        size_mb = os.path.getsize(db_path) / 1024 / 1024
        print(f"   💾 Database size: {size_mb:.1f} MB")

    return report

def main():
    """Main execution function"""

    print("🏥 SQLite Data Loading")
    print("=" * 50)

    # Determine database path
    db_path = 'pidgeon.db'

    # Create schema
    create_database_schema(db_path)

    # Load all datasets
    load_names_from_json(db_path)

    load_providers_from_csv(db_path, 'providers_subset.csv')

    load_medications_from_ndc(db_path, 'ndctext/product.txt')

    load_lab_tests_from_loinc(db_path, 'Loinc_2.81/LoincTable/Loinc.csv')

    load_diagnoses_from_csv(db_path, 'common_icd10_codes.csv')

    load_zip_codes_from_csv(db_path, 'simplemaps_uszips_basicv1.911/uszips.csv')

    load_vaccines_from_txt(db_path, 'cvx.txt')

    # Create summary
    report = create_summary_report(db_path)

    print(f"\n🚀 Next steps:")
    print(f"   1. Test database: sqlite3 {db_path} '.tables'")
    print(f"   2. Sample query: SELECT * FROM medications LIMIT 5;")
    print(f"   3. Integrate with Pidgeon field resolvers")

if __name__ == '__main__':
    main()