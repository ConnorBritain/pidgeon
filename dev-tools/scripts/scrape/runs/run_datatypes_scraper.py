#!/usr/bin/env python3
"""
Run the HL7 DataTypes scraper on all data types with quality tracking
"""

import json
import time
from pathlib import Path
import importlib.util
from datetime import datetime

# Load the component scraper module
spec = importlib.util.spec_from_file_location("scraper", "../scrapers/hl7_component_scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

create_scraper = scraper_module.create_scraper

def run_datatypes_scraper(limit=None, test_mode=True):
    """Run component scraper on all data types"""

    # Generate timestamped directory
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    mode = "test" if test_mode else "prod"
    run_dir = f"{timestamp}_{mode}"

    dest_path = Path(f"../outputs/data_types/{run_dir}")
    dest_path.mkdir(parents=True, exist_ok=True)

    print(f"Running HL7 DataTypes scraper...")
    print(f"Mode: {'TEST' if test_mode else 'PRODUCTION'}")
    print(f"Output directory: {dest_path}")
    print(f"Limit: {limit if limit else 'All data types'}")

    # Initialize scraper with optimized settings for bulk processing
    scraper = create_scraper(
        domain="DataTypes",
        dest_path=dest_path,
        headless=True,
        delay_ms=1000
    )

    try:
        # Start timer
        start_time = time.time()

        # Run the scraper
        scraper.run(limit=limit)

        # Calculate time
        elapsed = time.time() - start_time
        print(f"\nScraping completed in {elapsed:.1f} seconds")

        # Analyze results
        analyze_results(dest_path)

        return True

    except Exception as e:
        print(f"Error running scraper: {e}")
        return False

def analyze_results(dest_path):
    """Analyze and summarize scraping results"""
    print(f"\n=== DATATYPES SCRAPER RESULTS ANALYSIS ===")

    try:
        # Count individual files
        individual_files = list(dest_path.glob("v23_*.json"))
        print(f"Total data types processed: {len(individual_files)}")

        # Check for master file
        master_file = dest_path / "datatypes_v23_master.json"
        if master_file.exists():
            with open(master_file, 'r', encoding='utf-8') as f:
                master_data = json.load(f)
            print(f"Master file contains: {len(master_data)} data types")

            # Sample analysis
            if master_data:
                sample = master_data[0]
                print(f"Sample data type: {sample.get('code', 'Unknown')}")
                print(f"  Name: {sample.get('name', 'Unknown')}")
                print(f"  Category: {sample.get('category', 'Unknown')}")
                print(f"  Fields: {len(sample.get('fields', []))}")

            # Category breakdown
            categories = {}
            for item in master_data:
                cat = item.get('category', 'Unknown')
                categories[cat] = categories.get(cat, 0) + 1

            print(f"Category breakdown:")
            for cat, count in categories.items():
                print(f"  {cat}: {count}")

    except Exception as e:
        print(f"Error analyzing results: {e}")

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description='Run HL7 DataTypes Scraper')
    parser.add_argument('--limit', type=int, help='Limit number of data types to scrape')
    parser.add_argument('--prod', action='store_true', help='Production mode (default: test mode)')

    args = parser.parse_args()

    success = run_datatypes_scraper(limit=args.limit, test_mode=not args.prod)
    exit(0 if success else 1)