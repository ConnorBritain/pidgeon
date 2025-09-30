#!/usr/bin/env python3
"""
Analyze the actual DOM structure of Caristix trigger event pages
"""

import time
import re
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By

def analyze_structure():
    """Analyze the actual page structure"""
    print("Analyzing Caristix page structure...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        # Test with ADT_A01
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(5)  # Allow time for dynamic content

        # Find all elements that might contain segment data
        potential_containers = [
            "div",
            "section",
            "ul",
            "ol",
            "li",
            ".segment",
            ".segments",
            "[class*='segment']",
            "[class*='table']",
            "[data-*]"
        ]

        print("\n=== Container Analysis ===")
        for selector in potential_containers:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    print(f"{selector}: {len(elements)} elements")

                    # Check first few elements for segment-related content
                    for i, elem in enumerate(elements[:3]):
                        text = elem.text.strip()
                        if any(word in text for word in ["MSH", "PID", "PV1", "segment", "optionality"]):
                            print(f"  Element {i}: Contains segment data")
                            print(f"    Text preview: {text[:100]}...")
                            print(f"    Tag: {elem.tag_name}")
                            print(f"    Classes: {elem.get_attribute('class')}")
                            print(f"    ID: {elem.get_attribute('id')}")
                            break
            except Exception as e:
                pass

        # Look for specific patterns in page source
        page_source = driver.page_source

        print("\n=== Pattern Analysis ===")

        # Look for segment patterns
        segment_patterns = [
            r'MSH.*?(?:Required|Optional|Conditional)',
            r'PID.*?(?:Required|Optional|Conditional)',
            r'segment.*?(?:Required|Optional|Conditional)',
            r'(?:Required|Optional|Conditional).*?segment',
            r'class="[^"]*segment[^"]*"',
            r'id="[^"]*segment[^"]*"',
            r'data-[^=]*="[^"]*segment[^"]*"'
        ]

        for pattern in segment_patterns:
            matches = re.findall(pattern, page_source, re.IGNORECASE)
            if matches:
                print(f"Pattern '{pattern}': {len(matches)} matches")
                if matches:
                    print(f"  Example: {matches[0][:150]}...")

        # Look for specific class/id patterns
        class_patterns = re.findall(r'class="([^"]*)"', page_source)
        relevant_classes = [cls for cls in class_patterns if any(word in cls.lower() for word in ['segment', 'table', 'row', 'column', 'grid'])]

        print(f"\nRelevant classes found: {len(set(relevant_classes))}")
        for cls in sorted(set(relevant_classes))[:10]:
            print(f"  {cls}")

        # Check for JavaScript-rendered content
        print(f"\n=== JavaScript Content Check ===")

        # Wait a bit more and check again
        time.sleep(3)

        # Try to trigger any lazy loading
        driver.execute_script("window.scrollTo(0, document.body.scrollHeight);")
        time.sleep(2)

        # Re-check for tables after scrolling
        tables_after = driver.find_elements(By.TAG_NAME, "table")
        print(f"Tables after scroll: {len(tables_after)}")

        # Check for common lazy-loading indicators
        lazy_indicators = ["loading", "spinner", "skeleton", "placeholder"]
        for indicator in lazy_indicators:
            elements = driver.find_elements(By.CSS_SELECTOR, f"[class*='{indicator}']")
            if elements:
                print(f"Found {len(elements)} elements with '{indicator}' in class")

        # Try to find the actual segment data container
        print(f"\n=== Segment Data Hunt ===")

        # Look for text content that looks like segment definitions
        all_text = driver.find_element(By.TAG_NAME, "body").text
        lines = all_text.split('\n')

        segment_lines = []
        for line in lines:
            line = line.strip()
            if any(seg in line for seg in ['MSH', 'PID', 'PV1', 'EVN', 'NK1']) and \
               any(opt in line for opt in ['Required', 'Optional', 'Conditional']):
                segment_lines.append(line)

        print(f"Found {len(segment_lines)} lines with segment + optionality:")
        for line in segment_lines[:5]:
            print(f"  {line}")

        return True

    except Exception as e:
        print(f"Analysis failed: {e}")
        return False

    finally:
        driver.quit()

if __name__ == "__main__":
    analyze_structure()