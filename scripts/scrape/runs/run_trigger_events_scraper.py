#!/usr/bin/env python3
"""
Run the HL7 Trigger Events scraper on all trigger events with quality tracking
"""

import json
import time
from pathlib import Path
import importlib.util
from datetime import datetime

# Load the enhanced event scraper module
spec = importlib.util.spec_from_file_location("scraper", "../scrapers/hl7_event_scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def run_trigger_events_scraper(limit=None, resume=False, test_mode=True):
    """Run enhanced scraper on all trigger events"""

    # Generate timestamped directory
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    mode = "test" if test_mode else "prod"
    run_dir = f"{timestamp}_{mode}"

    dest_path = Path(f"../outputs/trigger_events/{run_dir}")
    dest_path.mkdir(parents=True, exist_ok=True)

    print(f"Running HL7 Trigger Events scraper...")
    print(f"Mode: {'TEST' if test_mode else 'PRODUCTION'}")
    print(f"Output directory: {dest_path}")
    print(f"Limit: {limit if limit else 'All trigger events'}")
    print(f"Resume: {resume}")

    # Initialize scraper with optimized settings for bulk processing
    scraper = CaristixScraper(
        dest_path=dest_path,
        headless=True,
        delay_ms=1000
    )

    try:
        # Start timer
        start_time = time.time()

        # Run the scraper
        scraper.run(resume=resume, limit=limit)

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
    print(f"\n=== TRIGGER EVENTS SCRAPER RESULTS ANALYSIS ===")

    try:
        # Count individual files
        events_dir = dest_path / "events"
        if events_dir.exists():
            individual_files = list(events_dir.glob("v23_*.json"))
            print(f"Total trigger events processed: {len(individual_files)}")

        # Check for master file
        master_file = dest_path / "trigger_events_v23_master.json"
        if master_file.exists():
            with open(master_file, 'r', encoding='utf-8') as f:
                master_data = json.load(f)
            print(f"Master file contains: {len(master_data)} trigger events")

            # Sample analysis
            if master_data:
                sample = master_data[0]
                print(f"Sample trigger event: {sample.get('code', 'Unknown')}")
                print(f"  Name: {sample.get('name', 'Unknown')}")
                print(f"  Version: {sample.get('version', 'Unknown')}")
                print(f"  Chapter: {sample.get('chapter', 'Unknown')}")
                print(f"  Description length: {len(sample.get('description', ''))}")
                print(f"  Segments: {len(sample.get('segments', []))}")

            # Chapter breakdown
            chapters = {}
            for item in master_data:
                chapter = item.get('chapter', 'Unknown')
                chapters[chapter] = chapters.get(chapter, 0) + 1

            print(f"Chapter breakdown:")
            for chapter, count in sorted(chapters.items()):
                print(f"  {chapter}: {count}")

            # Segment count statistics
            segment_counts = [len(item.get('segments', [])) for item in master_data]
            if segment_counts:
                print(f"Segment count statistics:")
                print(f"  Min segments: {min(segment_counts)}")
                print(f"  Max segments: {max(segment_counts)}")
                print(f"  Avg segments: {sum(segment_counts) / len(segment_counts):.1f}")

    except Exception as e:
        print(f"Error analyzing results: {e}")

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description='Run HL7 Trigger Events Scraper')
    parser.add_argument('--limit', type=int, help='Limit number of trigger events to scrape')
    parser.add_argument('--resume', action='store_true', help='Resume from previous run')
    parser.add_argument('--prod', action='store_true', help='Production mode (default: test mode)')

    args = parser.parse_args()

    success = run_trigger_events_scraper(limit=args.limit, resume=args.resume, test_mode=not args.prod)
    exit(0 if success else 1)