#!/usr/bin/env python3
"""
Run all HL7 domain scrapers to collect complete dataset
"""

import subprocess
import sys
import time
from pathlib import Path

def run_scraper(script_name, limit=None):
    """Run a specific scraper script"""
    cmd = [sys.executable, script_name]
    if limit:
        cmd.extend(['--limit', str(limit)])

    print(f"\n{'='*60}")
    print(f"RUNNING: {script_name}")
    print(f"{'='*60}")

    start_time = time.time()

    try:
        result = subprocess.run(cmd, capture_output=True, text=True)
        elapsed = time.time() - start_time

        print(result.stdout)
        if result.stderr:
            print("STDERR:", result.stderr)

        print(f"\nCompleted in {elapsed:.1f} seconds")
        print(f"Exit code: {result.returncode}")

        return result.returncode == 0

    except Exception as e:
        print(f"Error running {script_name}: {e}")
        return False

def main():
    """Run all scrapers in sequence"""
    import argparse

    parser = argparse.ArgumentParser(description='Run All HL7 Scrapers')
    parser.add_argument('--limit', type=int, help='Limit items per domain (for testing)')
    parser.add_argument('--skip-trigger-events', action='store_true',
                       help='Skip trigger events (takes longest)')

    args = parser.parse_args()

    print("HL7 v2.3 COMPLETE DATA COLLECTION")
    print("=" * 60)
    print(f"Limit per domain: {args.limit if args.limit else 'No limit'}")
    print(f"Skip trigger events: {args.skip_trigger_events}")

    # Define scraper scripts in order of execution
    scrapers = [
        ("run_tables_scraper.py", "Tables"),
        ("run_datatypes_scraper.py", "DataTypes"),
        ("run_segments_scraper.py", "Segments"),
    ]

    if not args.skip_trigger_events:
        scrapers.append(("run_trigger_events_scraper.py", "Trigger Events"))

    total_start = time.time()
    results = {}

    # Run each scraper
    for script, domain in scrapers:
        success = run_scraper(script, args.limit)
        results[domain] = success

        if not success:
            print(f"❌ {domain} scraper FAILED")
        else:
            print(f"✅ {domain} scraper SUCCESS")

    total_elapsed = time.time() - total_start

    # Summary
    print(f"\n{'='*60}")
    print("FINAL RESULTS SUMMARY")
    print(f"{'='*60}")
    print(f"Total time: {total_elapsed:.1f} seconds ({total_elapsed/60:.1f} minutes)")
    print()

    success_count = 0
    for domain, success in results.items():
        status = "✅ SUCCESS" if success else "❌ FAILED"
        print(f"  {domain}: {status}")
        if success:
            success_count += 1

    print(f"\nOverall: {success_count}/{len(results)} scrapers successful")

    if success_count == len(results):
        print("\n🎉 ALL SCRAPERS COMPLETED SUCCESSFULLY!")
        print("Complete HL7 v2.3 dataset collected in ../outputs/")
    else:
        print(f"\n⚠️  {len(results) - success_count} scrapers failed")
        return 1

    return 0

if __name__ == "__main__":
    exit_code = main()
    sys.exit(exit_code)