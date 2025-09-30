#!/usr/bin/env python3
"""
Targeted test to diagnose and fix optionality/repeatability extraction
Uses the existing event scraper as base but adds detailed DOM analysis
"""

import sys
import json
from pathlib import Path

# Import the existing event scraper
sys.path.append(str(Path(__file__).parent))
from hl7_event_scraper import CaristixScraper

class OptionalityTestScraper(CaristixScraper):
    """Extended scraper with detailed DOM analysis for optionality/repeatability"""

    def debug_extract_segment_from_row(self, row_element, order_index: int, group_stack: list):
        """Debug version that logs detailed cell analysis"""
        try:
            print(f"\n--- Debugging Row {order_index} ---")

            # Try to find individual cells/columns in the row
            cell_selectors = [
                ".tree-table-cell",
                ".cell",
                ".column",
                "td",
                "[class*='cell']"
            ]

            cells = []
            for selector in cell_selectors:
                cells = row_element.find_elements(By.CSS_SELECTOR, selector)
                if cells:
                    print(f"Found {len(cells)} cells using selector: {selector}")
                    break

            if not cells:
                print("No cells found, trying row text parsing")
                return self._parse_row_text(row_element, order_index, group_stack)

            print(f"Analyzing {len(cells)} cells:")

            # Analyze each cell in detail
            cell_analysis = []
            for i, cell in enumerate(cells):
                cell_text = cell.text.strip()
                cell_html = cell.get_attribute('outerHTML')[:200]  # First 200 chars
                cell_classes = cell.get_attribute('class')

                cell_info = {
                    "index": i,
                    "text": cell_text,
                    "classes": cell_classes,
                    "html_snippet": cell_html
                }
                cell_analysis.append(cell_info)

                print(f"  Cell {i}: '{cell_text}' (classes: {cell_classes})")

            # Extract data from cells with logging
            seg_code = ""
            seg_desc = ""
            optionality = "UNKNOWN"  # Change default to track what we actually find
            repeatability = "UNKNOWN"
            is_group = False
            level = len(group_stack)

            for i, cell in enumerate(cells):
                cell_text = cell.text.strip()

                if i == 0:  # First cell: segment code and description
                    if ' - ' in cell_text:
                        parts = cell_text.split(' - ', 1)
                        seg_code = parts[0].strip()
                        seg_desc = parts[1].strip()
                    else:
                        seg_code = cell_text
                        seg_desc = ""

                    # Filter out UI elements
                    if seg_code in ['chevron_right', 'expand_more', 'expand_less', '']:
                        print(f"  Skipping UI element: {seg_code}")
                        return None

                    # Check if this is a group
                    is_group = seg_code.isupper() and len(seg_code) > 3 and not seg_code.startswith(('MSH', 'PID', 'EVN'))
                    print(f"  Segment: {seg_code} (group: {is_group})")

                elif i == 1:  # Second cell: optionality
                    print(f"  Cell 1 content for optionality: '{cell_text}'")
                    if cell_text in ['R', 'O', 'C']:
                        optionality = cell_text
                        print(f"  ✓ Found optionality: {optionality}")
                    else:
                        print(f"  ✗ Unrecognized optionality: '{cell_text}'")

                elif i == 2:  # Third cell: repeatability
                    print(f"  Cell 2 content for repeatability: '{cell_text}'")
                    if cell_text in ['-', '∞', '*']:
                        repeatability = cell_text
                        print(f"  ✓ Found repeatability: {repeatability}")
                    else:
                        print(f"  ✗ Unrecognized repeatability: '{cell_text}'")

                elif i >= 3:  # Additional cells
                    print(f"  Additional cell {i}: '{cell_text}'")

            # Skip if no valid segment code found
            if not seg_code or len(seg_code) < 2:
                print(f"  Skipping - invalid segment code: '{seg_code}'")
                return None

            result = {
                "segment_code": seg_code,
                "segment_desc": seg_desc,
                "optionality": optionality,
                "repeatability": repeatability,
                "is_group": is_group,
                "group_path": group_stack.copy(),
                "level": level,
                "order_index": order_index,
                "debug_info": {
                    "cell_count": len(cells),
                    "cell_analysis": cell_analysis
                }
            }

            print(f"  Result: {seg_code} [{optionality}/{repeatability}]")
            return result

        except Exception as e:
            print(f"  Error extracting segment from row: {e}")
            import traceback
            traceback.print_exc()
            return None

    def debug_parse_event_page(self, url: str):
        """Debug version of parse_event_page with detailed logging"""
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC

        code = url.rstrip('/').split('/')[-1]
        print(f"\n🔍 DEBUG PARSING: {code}")
        print(f"URL: {url}")

        self.driver.get(url)
        self.random_delay()

        # Wait for tree-table to load
        print("Waiting for table to load...")
        WebDriverWait(self.driver, 15).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, ".tree-table.table-with-margins"))
        )
        print("✓ Table loaded")

        # Get table element
        table = self.driver.find_element(By.CSS_SELECTOR, ".tree-table.table-with-margins")

        # Debug the table structure first
        print("\n📊 TABLE STRUCTURE ANALYSIS")
        print("Looking for table headers...")

        # Check for header row
        header_selectors = ["th", ".header", "[class*='header']", "tr:first-child"]
        for selector in header_selectors:
            try:
                headers = table.find_elements(By.CSS_SELECTOR, selector)
                if headers:
                    print(f"Found {len(headers)} headers with selector '{selector}':")
                    for i, header in enumerate(headers):
                        print(f"  Header {i}: '{header.text.strip()}'")
                    break
            except:
                continue

        # Analyze first few rows to understand structure
        print("\nAnalyzing first few rows...")
        rows = table.find_elements(By.CSS_SELECTOR, ".flex-segment")
        print(f"Found {len(rows)} total rows")

        for i, row in enumerate(rows[:3]):  # Just first 3 rows for analysis
            print(f"\nRow {i} raw text: '{row.text[:100]}...'")

        # Now expand groups as usual
        print("\n🔧 EXPANDING GROUPS")
        segments = self.expand_groups_debug(table)

        return {
            "code": code,
            "segments": segments,
            "debug_table_info": {
                "total_rows": len(rows),
                "first_row_text": rows[0].text if rows else "No rows found"
            }
        }

    def expand_groups_debug(self, table_element):
        """Debug version of expand_groups with detailed logging"""
        from selenium.webdriver.common.by import By

        print("Starting group expansion...")

        # First expand groups (reuse existing logic)
        segments = []

        # Look for expandable elements
        expandables = table_element.find_elements(By.XPATH, ".//*[contains(text(), 'chevron_right')]")
        print(f"Found {len(expandables)} potentially expandable elements")

        # Try to expand a few
        expanded_count = 0
        for expandable in expandables[:3]:  # Limit to first 3 for testing
            try:
                self.driver.execute_script("arguments[0].click();", expandable)
                expanded_count += 1
                print(f"Expanded element {expanded_count}")
                import time
                time.sleep(1)
            except Exception as e:
                print(f"Failed to expand element: {e}")

        print(f"Expanded {expanded_count} groups")

        # Now extract segments with debug info
        print("\n📝 EXTRACTING SEGMENTS")
        segment_elements = table_element.find_elements(By.CSS_SELECTOR, ".flex-segment")
        print(f"Found {len(segment_elements)} segment elements after expansion")

        order_index = 0
        group_stack = []

        for i, segment_elem in enumerate(segment_elements[:10]):  # Limit to first 10 for testing
            try:
                print(f"\n--- Processing segment element {i} ---")
                segment_data = self.debug_extract_segment_from_row(segment_elem, order_index, group_stack)
                if segment_data:
                    segments.append(segment_data)
                    order_index += 1

                    # Update group stack if this is a group
                    if segment_data.get('is_group'):
                        current_level = segment_data.get('level', 0)
                        if current_level < len(group_stack):
                            group_stack = group_stack[:current_level]
                        group_stack.append(segment_data['segment_code'])
                        print(f"Updated group stack: {group_stack}")
                else:
                    print(f"No segment data extracted from element {i}")

            except Exception as e:
                print(f"Error processing segment {i}: {e}")
                continue

        print(f"\n✓ Extracted {len(segments)} segments total")
        return segments

def test_specific_event(event_code: str):
    """Test a specific event with debug output"""
    # Create output directory
    output_dir = Path(__file__).parent / "debug_outputs"
    output_dir.mkdir(exist_ok=True)

    scraper = OptionalityTestScraper(output_dir, headless=False, delay_ms=1000)

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("❌ Failed to setup Chrome driver")
            return

        scraper.driver = driver

        # Test the specific URL
        url = f"https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/{event_code}"
        result = scraper.debug_parse_event_page(url)

        if result:
            print(f"\n✅ Successfully parsed {event_code}")
            print(f"Extracted {len(result['segments'])} segments")

            # Analyze optionality/repeatability accuracy
            print(f"\n📈 OPTIONALITY/REPEATABILITY ANALYSIS")

            segments = result['segments']
            unknown_optionality = [s for s in segments if s['optionality'] == 'UNKNOWN']
            unknown_repeatability = [s for s in segments if s['repeatability'] == 'UNKNOWN']

            print(f"Segments with unknown optionality: {len(unknown_optionality)}/{len(segments)}")
            print(f"Segments with unknown repeatability: {len(unknown_repeatability)}/{len(segments)}")

            if unknown_optionality:
                print("Segments with unknown optionality:")
                for seg in unknown_optionality[:5]:  # First 5
                    print(f"  - {seg['segment_code']}: {seg.get('debug_info', {})}")

            if unknown_repeatability:
                print("Segments with unknown repeatability:")
                for seg in unknown_repeatability[:5]:  # First 5
                    print(f"  - {seg['segment_code']}: {seg.get('debug_info', {})}")

            # Save debug results
            debug_file = output_dir / f"{event_code.lower()}_debug.json"
            with open(debug_file, "w", encoding="utf-8") as f:
                json.dump(result, f, ensure_ascii=False, indent=2)
            print(f"\n💾 Debug results saved to: {debug_file}")

        else:
            print(f"❌ Failed to parse {event_code}")

    except Exception as e:
        print(f"❌ Error testing {event_code}: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

def main():
    """Test optionality extraction on the events we have example images for"""

    print("🧪 OPTIONALITY/REPEATABILITY EXTRACTION TEST")
    print("=" * 50)

    # Test the events we have example images for
    test_events = ["ADT_A01"]  # Start with one we can compare to image

    for event_code in test_events:
        test_specific_event(event_code)
        print("\n" + "="*50)

if __name__ == "__main__":
    main()