#!/usr/bin/env python3
"""
Test script to validate enhanced parsing with BAR_P06 specifically
"""

import json
import sys
from pathlib import Path

# Add the caristix-triggerevent-scraper to path
sys.path.append(str(Path(__file__).parent))

import importlib.util
import sys

# Load the scraper module with dashes in filename
spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def test_bar_p06():
    """Test parsing of BAR_P06 specifically to validate improvements"""

    # Create scraper instance
    dest_path = Path("./trigger-events/test_bar_p06")
    dest_path.mkdir(parents=True, exist_ok=True)

    scraper = CaristixScraper(dest_path, headless=True, delay_ms=1000)  # Headless to avoid setup issues

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("ERROR: Failed to setup Chrome driver")
            return

        scraper.driver = driver

        # Test specific URL
        bar_p06_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/BAR_P06"

        print(f"Testing BAR_P06 parsing...")
        event_data = scraper.parse_event_page(bar_p06_url)

        if event_data:
            print(f"[SUCCESS] Successfully parsed BAR_P06")
            print(f"[DATA] Event data:")
            print(f"   - Code: {event_data['code']}")
            print(f"   - Title: {event_data['title']}")
            print(f"   - Chapter: {event_data['chapter']}")
            print(f"   - Description: {event_data['description'][:100]}...")
            print(f"   - Segments: {len(event_data['segments'])}")

            print(f"\n[SEGMENTS] Segment details:")
            for i, seg in enumerate(event_data['segments'][:10]):  # First 10 segments
                print(f"   {i+1:2d}. {seg['segment_code']:<4} - {seg['segment_desc']:<40} [{seg['optionality']}/{seg['repeatability']}]")

            # Save the results
            with open(dest_path / "bar_p06_enhanced.json", "w", encoding="utf-8") as f:
                json.dump(event_data, f, ensure_ascii=False, indent=2)

            print(f"\n[SAVE] Results saved to: {dest_path / 'bar_p06_enhanced.json'}")

            # Quality check
            quality_score = 0
            if event_data['chapter']:
                quality_score += 20
                print(f"[PASS] Chapter extracted: {event_data['chapter']}")
            else:
                print(f"[FAIL] Chapter missing")

            if event_data['description']:
                quality_score += 20
                print(f"[PASS] Description extracted ({len(event_data['description'])} chars)")
            else:
                print(f"[FAIL] Description missing")

            segments_with_desc = sum(1 for seg in event_data['segments'] if seg['segment_desc'])
            if segments_with_desc > 0:
                quality_score += 30
                print(f"[PASS] Segment descriptions: {segments_with_desc}/{len(event_data['segments'])}")
            else:
                print(f"[FAIL] No segment descriptions found")

            segments_with_opt = sum(1 for seg in event_data['segments'] if seg['optionality'] in ['R', 'O', 'C'])
            if segments_with_opt > 0:
                quality_score += 15
                print(f"[PASS] Optionality values: {segments_with_opt}/{len(event_data['segments'])}")
            else:
                print(f"[FAIL] No optionality values found")

            segments_with_rep = sum(1 for seg in event_data['segments'] if seg['repeatability'] in ['-', 'infinity', '*', 'inf'])
            if segments_with_rep > 0:
                quality_score += 15
                print(f"[PASS] Repeatability values: {segments_with_rep}/{len(event_data['segments'])}")
            else:
                print(f"[FAIL] No repeatability values found")

            print(f"\n[SCORE] Overall quality score: {quality_score}/100")

        else:
            print(f"[FAIL] Failed to parse BAR_P06")

    except Exception as e:
        print(f"[ERROR] Error testing BAR_P06: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    test_bar_p06()