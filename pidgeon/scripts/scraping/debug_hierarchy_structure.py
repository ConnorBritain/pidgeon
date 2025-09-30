#!/usr/bin/env python3
"""
Debug the DOM structure to understand how hierarchy/indentation is encoded
"""

import json
import time
from pathlib import Path
import importlib.util

# Load the enhanced scraper module
spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def debug_hierarchy_dom():
    """Debug ADR_A19 DOM structure to understand hierarchy encoding"""

    dest_path = Path("./trigger-events/debug_hierarchy")
    dest_path.mkdir(parents=True, exist_ok=True)

    scraper = CaristixScraper(dest_path, headless=True, delay_ms=1000)

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("ERROR: Failed to setup Chrome driver")
            return

        scraper.driver = driver

        # Test ADR_A19 specifically
        adr_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADR_A19"

        print(f"Debugging hierarchy structure for ADR_A19...")
        driver.get(adr_url)
        time.sleep(3)

        # Find segments table using the same selector as the scraper
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.wait import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC

        # Wait for tree-table to load
        WebDriverWait(driver, 15).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, ".tree-table.table-with-margins"))
        )

        table = driver.find_element(By.CSS_SELECTOR, ".tree-table.table-with-margins")
        print(f"Found table element")

        # Expand all groups first
        segments = scraper.expand_groups(table)
        print(f"Expanded groups and got {len(segments)} segments")

        # Wait for DOM to settle
        time.sleep(2)

        # Find all rows again after expansion
        rows = table.find_elements(By.CSS_SELECTOR, ".flex-segment")
        print(f"Found {len(rows)} segment rows")

        hierarchy_data = []

        for i, row in enumerate(rows):
            try:
                # Get row text
                row_text = row.text.strip()

                # Get all CSS classes
                css_classes = row.get_attribute("class")

                # Get style attribute for padding/indentation
                style = row.get_attribute("style")

                # Check for padding-left in style or child elements
                padding_left = "0px"
                if "padding-left:" in style:
                    import re
                    match = re.search(r'padding-left:\s*(\d+px)', style)
                    if match:
                        padding_left = match.group(1)

                # Check child elements for indentation
                child_divs = row.find_elements(By.TAG_NAME, "div")
                indentation_info = []
                for child in child_divs:
                    child_style = child.get_attribute("style")
                    if "padding-left:" in child_style:
                        match = re.search(r'padding-left:\s*(\d+px)', child_style)
                        if match:
                            indentation_info.append(match.group(1))

                # Get the HTML for analysis
                html_snippet = row.get_attribute("outerHTML")[:200] + "..."

                row_info = {
                    "index": i,
                    "text": row_text,
                    "css_classes": css_classes,
                    "style": style,
                    "padding_left": padding_left,
                    "child_indentation": indentation_info,
                    "html_snippet": html_snippet
                }

                hierarchy_data.append(row_info)

                print(f"Row {i:2d}: {row_text[:50]:<50} | Classes: {css_classes[:30]:<30} | Padding: {padding_left}")

            except Exception as e:
                print(f"Error processing row {i}: {e}")

        # Save the hierarchy debugging data
        with open(dest_path / "hierarchy_debug.json", "w", encoding="utf-8") as f:
            json.dump(hierarchy_data, f, ensure_ascii=False, indent=2)

        print(f"\nHierarchy debug data saved to: {dest_path / 'hierarchy_debug.json'}")

        # Look for patterns in indentation
        print(f"\n=== INDENTATION PATTERNS ===")
        for row in hierarchy_data:
            if row["padding_left"] != "0px" or row["child_indentation"]:
                print(f"Row {row['index']:2d}: {row['text'][:30]:<30} | Padding: {row['padding_left']} | Children: {row['child_indentation']}")

    except Exception as e:
        print(f"Error debugging hierarchy: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    debug_hierarchy_dom()