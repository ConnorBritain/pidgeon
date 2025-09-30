#!/usr/bin/env python3
"""
Debug BAR_P06 after expanding all groups to see final DOM structure
"""

import time
import json
from pathlib import Path
import importlib.util

# Load the scraper module
spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def debug_bar_p06_after_expansion():
    """Debug BAR_P06 after full expansion"""
    print("Debugging BAR_P06 after expansion...")

    dest_path = Path("./trigger-events/test_expansion_debug")
    dest_path.mkdir(parents=True, exist_ok=True)

    scraper = CaristixScraper(dest_path, headless=True, delay_ms=500)

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("ERROR: Failed to setup Chrome driver")
            return

        scraper.driver = driver

        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/BAR_P06"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(5)

        # Find table
        table = driver.find_element(scraper_module.By.CSS_SELECTOR, ".tree-table.table-with-margins")
        print(f"Found table")

        # Expand all groups using scraper logic
        print("Expanding groups...")
        segments = scraper.expand_groups(table)
        print(f"Extracted {len(segments)} segments after expansion")

        # Now debug the current state
        print(f"\n=== POST-EXPANSION DOM ANALYSIS ===")

        # Get all rows again
        all_rows = table.find_elements(scraper_module.By.CSS_SELECTOR, ".flex-segment")
        print(f"Total rows after expansion: {len(all_rows)}")

        for i, row in enumerate(all_rows):
            try:
                row_text = row.text.strip()
                classes = row.get_attribute("class")
                print(f"\nRow {i:2d}:")
                print(f"  Text: {repr(row_text)}")
                print(f"  Classes: {classes}")

                # Show what our parsing logic would extract
                if i < len(segments):
                    seg = segments[i]
                    print(f"  Parsed as: {seg['segment_code']} - {seg['segment_desc']} [{seg['optionality']}/{seg['repeatability']}] group={seg['is_group']}")

            except Exception as e:
                print(f"Row {i}: Error - {e}")

        # Save results
        result_data = {
            "url": url,
            "total_rows_after_expansion": len(all_rows),
            "segments_extracted": segments,
            "row_details": []
        }

        for i, row in enumerate(all_rows[:10]):  # First 10 rows
            try:
                result_data["row_details"].append({
                    "index": i,
                    "text": row.text.strip(),
                    "classes": row.get_attribute("class"),
                    "html": row.get_attribute("outerHTML")[:500]  # First 500 chars
                })
            except:
                pass

        with open(dest_path / "expansion_debug.json", "w", encoding="utf-8") as f:
            json.dump(result_data, f, ensure_ascii=False, indent=2)

        print(f"\nDebug results saved to: {dest_path / 'expansion_debug.json'}")

    except Exception as e:
        print(f"Debug failed: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    debug_bar_p06_after_expansion()