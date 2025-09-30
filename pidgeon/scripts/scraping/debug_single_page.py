#!/usr/bin/env python3
"""
Debug script to examine a single trigger event page
"""

import time
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

def debug_page():
    """Debug a single page to understand structure"""
    print("Debugging single Caristix page...")

    # Setup Chrome options
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        # Test with ADT_A01 page
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(3)  # Basic wait

        # Get page title
        title = driver.title
        print(f"Page title: {title}")

        # Try to find any tables
        tables = driver.find_elements(By.TAG_NAME, "table")
        print(f"Found {len(tables)} table elements")

        # Try specific selectors from our scraper
        selectors_to_test = [
            "table",
            "table.table",
            ".table",
            "tbody",
            "tr",
            "main",
            "article",
            ".content"
        ]

        for selector in selectors_to_test:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                print(f"Selector '{selector}': {len(elements)} elements found")
            except Exception as e:
                print(f"Selector '{selector}': ERROR - {e}")

        # Get page source snippet to see what's actually there
        page_source = driver.page_source
        print(f"\nPage source length: {len(page_source)} characters")

        # Look for common table indicators
        indicators = ["<table", "<tbody", "<tr", "segments", "segment", "optionality"]
        for indicator in indicators:
            count = page_source.lower().count(indicator.lower())
            print(f"'{indicator}' appears {count} times")

        # Check if page requires JavaScript loading
        if "loading" in page_source.lower() or "spinner" in page_source.lower():
            print("Page may require additional loading time")

        # Try waiting for different elements
        wait_tests = [
            ("table", 15),
            ("tbody", 15),
            (".table", 15),
            ("body", 5)
        ]

        for selector, timeout in wait_tests:
            try:
                WebDriverWait(driver, timeout).until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, selector))
                )
                print(f"Wait test '{selector}': SUCCESS")
            except Exception as e:
                print(f"Wait test '{selector}': TIMEOUT after {timeout}s")

        # Save a screenshot for debugging
        try:
            screenshot_path = "debug_page.png"
            driver.save_screenshot(screenshot_path)
            print(f"Screenshot saved: {screenshot_path}")
        except Exception as e:
            print(f"Screenshot failed: {e}")

        print(f"\nPage ready state: {driver.execute_script('return document.readyState')}")

        return True

    except Exception as e:
        print(f"Debug failed: {e}")
        return False

    finally:
        driver.quit()

if __name__ == "__main__":
    debug_page()