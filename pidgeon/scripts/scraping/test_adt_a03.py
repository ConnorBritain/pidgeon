#!/usr/bin/env python3
"""
Test script to validate enhanced parsing with ADT_A03 specifically (PROCEDURE group)
"""

import json
import sys
from pathlib import Path
import importlib.util

# Load the scraper module
spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def test_adt_a03():
    """Test parsing of ADT_A03 specifically to validate PROCEDURE group expansion"""

    # Create scraper instance
    dest_path = Path("./trigger-events/test_adt_a03")
    dest_path.mkdir(parents=True, exist_ok=True)

    scraper = CaristixScraper(dest_path, headless=True, delay_ms=1000)

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("ERROR: Failed to setup Chrome driver")
            return

        scraper.driver = driver

        # Test specific URL
        adt_a03_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A03"

        print(f"Testing ADT_A03 parsing...")
        event_data = scraper.parse_event_page(adt_a03_url)

        if event_data:
            print(f"[SUCCESS] Successfully parsed ADT_A03")
            print(f"[DATA] Event data:")
            print(f"   - Code: {event_data['code']}")
            print(f"   - Title: {event_data['title']}")
            print(f"   - Chapter: {event_data['chapter']}")
            print(f"   - Description: {event_data['description'][:100] if event_data['description'] else 'None'}...")
            print(f"   - Segments: {len(event_data['segments'])}")

            print(f"\n[SEGMENTS] Segment details:")
            for i, seg in enumerate(event_data['segments']):
                print(f"   {i+1:2d}. {seg['segment_code']:<12} - {seg['segment_desc']:<40} [{seg['optionality']}/{seg['repeatability']}] {'(GROUP)' if seg['is_group'] else ''}")

            # Save the results
            with open(dest_path / "adt_a03_enhanced.json", "w", encoding="utf-8") as f:
                json.dump(event_data, f, ensure_ascii=False, indent=2)

            print(f"\n[SAVE] Results saved to: {dest_path / 'adt_a03_enhanced.json'}")

            # Quality check with focus on groups
            quality_score = 0
            total_checks = 6

            if event_data['chapter']:
                quality_score += 1
                print(f"[PASS] Chapter extracted: {event_data['chapter']}")
            else:
                print(f"[FAIL] Chapter missing")

            if event_data['description']:
                quality_score += 1
                print(f"[PASS] Description extracted ({len(event_data['description'])} chars)")
            else:
                print(f"[FAIL] Description missing")

            segments_with_desc = sum(1 for seg in event_data['segments'] if seg['segment_desc'])
            if segments_with_desc > 0:
                quality_score += 1
                print(f"[PASS] Segment descriptions: {segments_with_desc}/{len(event_data['segments'])}")
            else:
                print(f"[FAIL] No segment descriptions found")

            segments_with_opt = sum(1 for seg in event_data['segments'] if seg['optionality'] in ['R', 'O', 'C'])
            if segments_with_opt > 0:
                quality_score += 1
                print(f"[PASS] Optionality values: {segments_with_opt}/{len(event_data['segments'])}")
            else:
                print(f"[FAIL] No optionality values found")

            # Check for groups
            groups_found = [seg for seg in event_data['segments'] if seg['is_group']]
            if groups_found:
                quality_score += 1
                print(f"[PASS] Groups found: {[g['segment_code'] for g in groups_found]}")
            else:
                print(f"[FAIL] No groups found")

            # Check for expanded content (should have more than just basic segments)
            if len(event_data['segments']) > 5:  # Expect more than just MSH, EVN, PID, etc.
                quality_score += 1
                print(f"[PASS] Complex structure captured: {len(event_data['segments'])} segments")
            else:
                print(f"[FAIL] Structure seems incomplete: only {len(event_data['segments'])} segments")

            print(f"\n[SCORE] Quality score: {quality_score}/{total_checks} ({quality_score/total_checks*100:.0f}%)")

            # Look for specific PROCEDURE-related content
            procedure_segments = [seg for seg in event_data['segments'] if 'PROCEDURE' in seg['segment_code'].upper() or 'PROC' in seg['segment_code'].upper()]
            if procedure_segments:
                print(f"[PROCEDURE] Found procedure-related segments: {[p['segment_code'] for p in procedure_segments]}")
            else:
                print(f"[PROCEDURE] No procedure-related segments found")

        else:
            print(f"[FAIL] Failed to parse ADT_A03")

    except Exception as e:
        print(f"[ERROR] Error testing ADT_A03: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    test_adt_a03()