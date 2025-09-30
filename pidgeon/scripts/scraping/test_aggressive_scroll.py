#!/usr/bin/env python3
"""
Test aggressive scrolling to load ALL trigger events from Caristix
"""

import time
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys

def test_aggressive_scroll():
    """Test different scrolling strategies to load all trigger events"""
    print("Testing aggressive scrolling for Caristix trigger events...")

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

        # Initial count
        initial_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
        print(f"Initial trigger event links: {len(initial_links)}")

        # Try different scrolling strategies
        strategies = [
            ("page_down_keys", "Send PAGE_DOWN keys"),
            ("end_key", "Send END key"),
            ("body_scroll", "Scroll document body"),
            ("window_scroll", "Window scroll commands"),
            ("element_scroll", "Scroll specific elements")
        ]

        for strategy_name, description in strategies:
            print(f"\n=== Strategy: {description} ===")

            if strategy_name == "page_down_keys":
                # Send PAGE_DOWN keys to body
                body = driver.find_element(By.TAG_NAME, "body")
                for i in range(20):
                    body.send_keys(Keys.PAGE_DOWN)
                    time.sleep(0.3)
                    if i % 5 == 0:
                        current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                        print(f"  After {i+1} PAGE_DOWN: {len(current_links)} links")

            elif strategy_name == "end_key":
                # Send END key
                body = driver.find_element(By.TAG_NAME, "body")
                body.send_keys(Keys.END)
                time.sleep(2)
                current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                print(f"  After END key: {len(current_links)} links")

            elif strategy_name == "body_scroll":
                # Scroll document body incrementally
                last_height = 0
                attempts = 0
                while attempts < 50:
                    driver.execute_script("document.body.scrollTop = document.body.scrollTop + 500")
                    time.sleep(0.2)

                    new_height = driver.execute_script("return document.body.scrollHeight")
                    if new_height == last_height:
                        attempts += 10  # Speed up if no change
                    last_height = new_height
                    attempts += 1

                    if attempts % 10 == 0:
                        current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                        print(f"  Scroll attempt {attempts}: {len(current_links)} links")

            elif strategy_name == "window_scroll":
                # Window scroll commands
                last_height = 0
                for i in range(100):
                    driver.execute_script("window.scrollTo(0, document.body.scrollHeight)")
                    time.sleep(0.1)

                    new_height = driver.execute_script("return document.body.scrollHeight")
                    if new_height == last_height:
                        # Try more aggressive scroll
                        driver.execute_script(f"window.scrollTo(0, {new_height + 1000})")
                        time.sleep(0.2)
                        final_height = driver.execute_script("return document.body.scrollHeight")
                        if final_height == new_height:
                            break

                    last_height = new_height

                    if i % 20 == 0:
                        current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                        print(f"  Scroll iteration {i}: {len(current_links)} links")

            elif strategy_name == "element_scroll":
                # Try scrolling specific container elements
                containers = [
                    "main",
                    ".content",
                    ".container",
                    ".list",
                    "[class*='list']",
                    "body"
                ]

                for container_sel in containers:
                    try:
                        containers_found = driver.find_elements(By.CSS_SELECTOR, container_sel)
                        if containers_found:
                            container = containers_found[0]
                            print(f"  Scrolling {container_sel}...")

                            for scroll in range(20):
                                driver.execute_script("arguments[0].scrollTop = arguments[0].scrollTop + 500", container)
                                time.sleep(0.1)

                            current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                            print(f"    After scrolling {container_sel}: {len(current_links)} links")
                    except:
                        pass

            # Check final count for this strategy
            final_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
            print(f"  Final count after {description}: {len(final_links)} links")

        # Get all unique trigger events found
        print(f"\n=== Final Analysis ===")
        all_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")

        unique_events = set()
        for link in all_links:
            href = link.get_attribute("href")
            if href and "/TriggerEvents/" in href:
                event_name = href.split("/TriggerEvents/")[-1]
                if event_name and event_name != "":
                    unique_events.add(event_name)

        print(f"Total unique trigger events found: {len(unique_events)}")
        print("All unique events:")
        for event in sorted(unique_events):
            print(f"  {event}")

        # Also check the page source for patterns
        page_source = driver.page_source
        import re

        # Look for event patterns in source
        event_patterns = re.findall(r'TriggerEvents/([A-Z0-9_]+)', page_source)
        source_events = set(event_patterns)

        print(f"\nEvents found in page source: {len(source_events)}")
        for event in sorted(source_events):
            print(f"  {event}")

        return len(unique_events)

    except Exception as e:
        print(f"Test failed: {e}")
        return 0

    finally:
        driver.quit()

if __name__ == "__main__":
    count = test_aggressive_scroll()
    print(f"\nResult: {count} trigger events discovered")