#!/usr/bin/env python3
"""
Analyze the actual DOM structure to understand how trigger events are organized
"""

import time
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By

def analyze_dom_structure():
    """Analyze DOM structure in detail"""
    print("Analyzing DOM structure for trigger events...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(5)

        # Get complete page source for analysis
        page_source = driver.page_source
        print(f"Page source length: {len(page_source)} characters")

        # Save page source for examination
        with open("page_source.html", "w", encoding="utf-8") as f:
            f.write(page_source)
        print("Saved page source to page_source.html")

        # Look for all unique href patterns
        import re
        all_hrefs = re.findall(r'href="([^"]*)"', page_source)
        trigger_hrefs = [href for href in all_hrefs if 'TriggerEvents' in href]

        print(f"\nAll href attributes with 'TriggerEvents': {len(trigger_hrefs)}")
        unique_trigger_hrefs = sorted(set(trigger_hrefs))
        for href in unique_trigger_hrefs:
            print(f"  {href}")

        # Extract event names from URLs
        event_names = []
        for href in unique_trigger_hrefs:
            if '/TriggerEvents/' in href:
                event_name = href.split('/TriggerEvents/')[-1].split('?')[0].split('#')[0]
                if event_name and event_name not in ['', '/']:
                    event_names.append(event_name)

        print(f"\nUnique event names from URLs: {len(set(event_names))}")
        for name in sorted(set(event_names)):
            print(f"  {name}")

        # Look for text patterns that might indicate more events
        print(f"\n=== Text Pattern Analysis ===")

        # ADT patterns
        adt_matches = re.findall(r'ADT[_-]?A(\d+)', page_source, re.IGNORECASE)
        adt_numbers = sorted(set(int(num) for num in adt_matches if num.isdigit()))
        print(f"ADT event numbers found: {adt_numbers}")
        print(f"ADT range: A{min(adt_numbers) if adt_numbers else 0} to A{max(adt_numbers) if adt_numbers else 0}")

        # Other message types
        other_patterns = [
            r'ORM[_-]?[A-Z](\d+)',
            r'ORU[_-]?[A-Z](\d+)',
            r'SIU[_-]?[A-Z](\d+)',
            r'MDM[_-]?[A-Z](\d+)',
            r'QRY[_-]?[A-Z](\d+)',
            r'BAR[_-]?[A-Z](\d+)',
            r'DFT[_-]?[A-Z](\d+)'
        ]

        for pattern in other_patterns:
            matches = re.findall(pattern, page_source, re.IGNORECASE)
            if matches:
                numbers = sorted(set(int(num) for num in matches if num.isdigit()))
                msg_type = pattern.split('[')[0]
                print(f"{msg_type} event numbers: {numbers}")

        # Check for list/table structures
        print(f"\n=== DOM Structure Analysis ===")

        # Look for list containers
        list_selectors = [
            "ul", "ol", "div[role='list']", ".list", "[class*='list']",
            "table", "tbody", "[role='table']", ".table", "[class*='table']"
        ]

        for selector in list_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    print(f"\n{selector}: {len(elements)} elements")
                    for i, elem in enumerate(elements[:3]):
                        text = elem.text.strip()
                        if 'ADT' in text or 'ACK' in text or 'TriggerEvents' in text:
                            print(f"  Element {i} contains trigger events:")
                            lines = text.split('\n')[:10]  # First 10 lines
                            for line in lines:
                                if any(pattern in line for pattern in ['ADT', 'ACK', 'ORM', 'ORU']):
                                    print(f"    {line.strip()}")
            except:
                pass

        # Look for Angular Material components
        print(f"\n=== Angular Material Components ===")
        angular_selectors = [
            "mat-list", "mat-list-item", ".mat-list", ".mat-list-item",
            "cdk-virtual-scroll-viewport", ".cdk-virtual-scroll-viewport"
        ]

        for selector in angular_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    print(f"{selector}: {len(elements)} elements")

                    # For mat-list-item, check each item
                    if 'mat-list-item' in selector:
                        for i, elem in enumerate(elements[:20]):  # Check first 20 items
                            try:
                                text = elem.text.strip()
                                link = elem.find_element(By.TAG_NAME, "a")
                                href = link.get_attribute("href")
                                if href and 'TriggerEvents' in href:
                                    event_name = href.split('/TriggerEvents/')[-1]
                                    print(f"  Item {i}: {event_name} - {text[:100]}")
                            except:
                                pass
            except:
                pass

        # Check for infinite scroll or pagination
        print(f"\n=== Scroll/Pagination Analysis ===")

        # Check current viewport vs total content
        viewport_height = driver.execute_script("return window.innerHeight")
        scroll_height = driver.execute_script("return document.documentElement.scrollHeight")
        current_scroll = driver.execute_script("return window.pageYOffset")

        print(f"Viewport height: {viewport_height}")
        print(f"Document scroll height: {scroll_height}")
        print(f"Current scroll position: {current_scroll}")
        print(f"Scrollable: {'Yes' if scroll_height > viewport_height else 'No'}")

        return len(set(event_names))

    except Exception as e:
        print(f"Analysis failed: {e}")
        return 0

    finally:
        driver.quit()

if __name__ == "__main__":
    count = analyze_dom_structure()
    print(f"\nTotal unique events found: {count}")