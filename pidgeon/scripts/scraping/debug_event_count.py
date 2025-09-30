#!/usr/bin/env python3
"""
Debug script to investigate trigger event count discrepancy
Expected: 80+ events, but only finding 19
"""

import time
import re
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

def debug_event_count():
    """Debug why we're only finding 19 trigger events instead of 80+"""
    print("Debugging Caristix trigger event count...")

    # Setup Chrome options
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(3)

        print(f"Page title: {driver.title}")

        # Check various selectors for trigger event links
        selectors_to_test = [
            "a[href*='/TriggerEvents/']",
            "a[href*='/v2/HL7v2.3/TriggerEvents/']",
            "a[href*='TriggerEvents']",
            "a",  # All links
            "[href*='TriggerEvents']",
            ".nav-link",
            ".menu-item",
            "li > a",
            "ul a"
        ]

        print("\n=== Initial Link Count Analysis ===")
        for selector in selectors_to_test:
            try:
                links = driver.find_elements(By.CSS_SELECTOR, selector)
                print(f"Selector '{selector}': {len(links)} links")

                # Check first few for trigger event patterns
                if selector in ["a[href*='/TriggerEvents/']", "a[href*='/v2/HL7v2.3/TriggerEvents/']"]:
                    trigger_links = []
                    for link in links[:10]:
                        href = link.get_attribute("href")
                        text = link.text.strip()
                        if href and "/TriggerEvents/" in href:
                            trigger_links.append((href, text))
                    print(f"  Sample trigger events found: {len(trigger_links)}")
                    for href, text in trigger_links[:5]:
                        print(f"    {text}: {href}")

            except Exception as e:
                print(f"Selector '{selector}': ERROR - {e}")

        # Scroll and check if more events load
        print(f"\n=== Scroll Analysis ===")
        initial_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/v2/HL7v2.3/TriggerEvents/']")
        print(f"Initial trigger event links: {len(initial_links)}")

        # Perform aggressive scrolling
        for scroll_round in range(5):
            print(f"Scroll round {scroll_round + 1}...")

            # Scroll down multiple times
            for i in range(5):
                driver.execute_script("window.scrollTo(0, document.body.scrollHeight)")
                time.sleep(0.5)

            # Check for pagination or load-more buttons
            load_more_selectors = [
                "button[class*='load']",
                "button[class*='more']",
                "[class*='pagination']",
                ".pagination",
                ".next",
                ".load-more",
                "button:contains('Load')",
                "a:contains('Next')",
                "a:contains('More')"
            ]

            for selector in load_more_selectors:
                try:
                    buttons = driver.find_elements(By.CSS_SELECTOR, selector)
                    if buttons:
                        print(f"  Found {len(buttons)} '{selector}' buttons")
                        for button in buttons:
                            try:
                                driver.execute_script("arguments[0].click();", button)
                                time.sleep(1)
                                print(f"    Clicked button: {button.text}")
                            except:
                                pass
                except:
                    pass

            # Check if more links appeared
            current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/v2/HL7v2.3/TriggerEvents/']")
            print(f"  After scroll: {len(current_links)} trigger event links")

            if len(current_links) > len(initial_links):
                print(f"  [SUCCESS] Found {len(current_links) - len(initial_links)} additional links!")
                initial_links = current_links

        # Final analysis
        print(f"\n=== Final Analysis ===")
        final_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/v2/HL7v2.3/TriggerEvents/']")

        # Extract all unique trigger events
        unique_events = set()
        event_details = []

        for link in final_links:
            href = link.get_attribute("href")
            text = link.text.strip()

            if href and "/TriggerEvents/" in href:
                # Extract event name from URL
                match = re.search(r'/TriggerEvents/([^/?]+)', href)
                if match:
                    event_name = match.group(1)
                    if event_name not in unique_events:
                        unique_events.add(event_name)
                        event_details.append((event_name, text, href))

        print(f"Total unique trigger events found: {len(unique_events)}")
        print(f"Expected trigger events: 80+")

        # Show all found events
        print(f"\nAll found trigger events:")
        for event_name, text, href in sorted(event_details):
            print(f"  {event_name}: {text}")

        # Check page source for hidden events
        print(f"\n=== Page Source Analysis ===")
        page_source = driver.page_source

        # Look for ADT patterns that might not be linked yet
        adt_patterns = [
            r'ADT[_-]A\d+',
            r'ADT.*?A\d+',
            r'A\d+\s*-\s*[A-Z]'
        ]

        for pattern in adt_patterns:
            matches = re.findall(pattern, page_source, re.IGNORECASE)
            unique_matches = set(matches)
            if unique_matches:
                print(f"Pattern '{pattern}': {len(unique_matches)} unique matches")
                for match in sorted(unique_matches)[:10]:
                    print(f"  {match}")

        # Check if this is a paginated or filtered view
        print(f"\n=== Navigation Analysis ===")

        # Look for category filters or tabs
        filter_selectors = [
            ".nav-tabs",
            ".filter",
            ".category",
            ".tab",
            "[role='tab']",
            "select",
            ".dropdown"
        ]

        for selector in filter_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    print(f"Found {len(elements)} '{selector}' elements - possible filters/categories")
                    for elem in elements[:3]:
                        print(f"  Text: {elem.text[:100]}")
            except:
                pass

        return len(unique_events), event_details

    except Exception as e:
        print(f"Debug failed: {e}")
        return 0, []

    finally:
        driver.quit()

if __name__ == "__main__":
    count, events = debug_event_count()
    print(f"\nFinal Result: {count} trigger events found")