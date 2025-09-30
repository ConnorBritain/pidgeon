#!/usr/bin/env python3
"""
Debug the actual DOM structure of BAR_P06 page to understand parsing issues
"""

import time
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

def debug_bar_p06_dom():
    """Debug the DOM structure of BAR_P06 specifically"""
    print("Debugging BAR_P06 DOM structure...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/BAR_P06"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(5)

        print(f"Page title: {driver.title}")

        # Look for the table container
        table_selectors = [
            ".tree-table.table-with-margins",
            ".tree-table",
            ".table-with-margins",
            "[class*='table']",
            "table"
        ]

        table_element = None
        for selector in table_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    table_element = elements[0]
                    print(f"Found table using selector: {selector}")
                    break
            except:
                pass

        if not table_element:
            print("ERROR: No table found!")
            return

        # Analyze table structure
        print(f"\n=== Table Analysis ===")
        table_html = table_element.get_attribute("outerHTML")
        print(f"Table HTML length: {len(table_html)}")

        # Look for expandable elements BEFORE expanding
        expandables = table_element.find_elements(By.CSS_SELECTOR, "button[aria-expanded='false']")
        print(f"Expandable buttons found: {len(expandables)}")

        for i, button in enumerate(expandables[:5]):
            try:
                button_text = button.text.strip()
                parent_text = button.find_element(By.XPATH, "./..").text.strip()
                print(f"  Button {i}: '{button_text}' in '{parent_text[:50]}...'")
            except:
                print(f"  Button {i}: Could not get text")

        # Try to expand all buttons
        expanded_count = 0
        for button in expandables:
            try:
                driver.execute_script("arguments[0].scrollIntoView(true);", button)
                time.sleep(0.2)
                driver.execute_script("arguments[0].click();", button)
                expanded_count += 1
                time.sleep(0.5)
            except Exception as e:
                print(f"Failed to expand button: {e}")

        print(f"Expanded {expanded_count} buttons")

        # Re-analyze after expansion
        time.sleep(2)
        expandables_after = table_element.find_elements(By.CSS_SELECTOR, "button[aria-expanded='false']")
        print(f"Remaining expandable buttons: {len(expandables_after)}")

        # Look for all segment rows
        row_selectors = [
            ".flex-segment",
            ".tree-table-row",
            "[class*='row']",
            "tr",
            ".segment-row"
        ]

        all_rows = []
        for selector in row_selectors:
            try:
                elements = table_element.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    all_rows = elements
                    print(f"Found {len(elements)} rows using: {selector}")
                    break
            except:
                pass

        print(f"\n=== Row Analysis ===")
        for i, row in enumerate(all_rows[:10]):
            try:
                row_text = row.text.strip()
                classes = row.get_attribute("class")

                # Try to find cells within this row
                cell_selectors = [".tree-table-cell", ".cell", ".column", "td", "[class*='cell']"]
                cells = []
                for cell_sel in cell_selectors:
                    cells = row.find_elements(By.CSS_SELECTOR, cell_sel)
                    if cells:
                        break

                cell_texts = [cell.text.strip() for cell in cells]

                print(f"Row {i:2d}: '{row_text[:60]}...'")
                print(f"         Classes: {classes}")
                if cells:
                    print(f"         Cells: {cell_texts}")
                else:
                    print(f"         No cells found")
                print()

            except Exception as e:
                print(f"Row {i}: Error - {e}")

        # Look for description content
        print(f"\n=== Content Analysis ===")
        desc_selectors = [".content p", "main p", ".description", "p"]

        for selector in desc_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                print(f"{selector}: {len(elements)} elements")
                for j, elem in enumerate(elements[:3]):
                    text = elem.text.strip()
                    if text and len(text) > 20:
                        print(f"  {j}: {text[:100]}...")
            except:
                pass

        # Save page source for analysis
        with open("bar_p06_debug.html", "w", encoding="utf-8") as f:
            f.write(driver.page_source)
        print(f"\nPage source saved to bar_p06_debug.html")

    except Exception as e:
        print(f"Debug failed: {e}")
        import traceback
        traceback.print_exc()

    finally:
        driver.quit()

if __name__ == "__main__":
    debug_bar_p06_dom()