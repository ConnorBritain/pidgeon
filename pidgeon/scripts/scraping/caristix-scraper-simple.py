#!/usr/bin/env python3
"""
Simplified Caristix scraper with better ChromeDriver handling
"""

import time
import json
import os
import sys
from pathlib import Path

print("Starting simplified scraper...")

# Try to import dependencies
try:
    from selenium import webdriver
    from selenium.webdriver.chrome.service import Service
    from selenium.webdriver.chrome.options import Options
    from selenium.webdriver.common.by import By
    from selenium.webdriver.support.ui import WebDriverWait
    from selenium.webdriver.support import expected_conditions as EC
    from webdriver_manager.chrome import ChromeDriverManager
    print("[OK] Selenium imported successfully")
except ImportError as e:
    print(f"[ERROR] Failed to import Selenium: {e}")
    print("Please install: pip install selenium webdriver-manager")
    sys.exit(1)

# Constants
BASE_URL = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents"
OUTPUT_DIR = Path(r"C:\Users\Connor.England.FUSIONMGT\OneDrive - Fusion\Documents\Code\CRE Code\hl7generator\scripts\caristix\trigger-events\test_run")

def setup_driver():
    """Setup Chrome driver with multiple fallback options"""
    print("Setting up Chrome driver...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--window-size=1920,1080")

    # Try different driver setups
    driver = None

    # Method 1: Use webdriver-manager
    try:
        print("  Trying webdriver-manager...")
        driver_path = ChromeDriverManager().install()
        print(f"  ChromeDriver path: {driver_path}")

        # Check if it's a valid executable
        if not os.path.exists(driver_path) or os.path.getsize(driver_path) < 1000:
            print("  ChromeDriver seems invalid, trying alternative...")
            raise Exception("Invalid ChromeDriver")

        service = Service(driver_path)
        driver = webdriver.Chrome(service=service, options=options)
        print("  [OK] Chrome driver initialized with webdriver-manager")
        return driver
    except Exception as e:
        print(f"  [ERROR] webdriver-manager failed: {e}")

    # Method 2: Try system ChromeDriver
    try:
        print("  Trying system ChromeDriver...")
        driver = webdriver.Chrome(options=options)
        print("  [OK] Chrome driver initialized with system ChromeDriver")
        return driver
    except Exception as e:
        print(f"  [ERROR] System ChromeDriver failed: {e}")

    # Method 3: Look for ChromeDriver in common locations
    common_paths = [
        r"C:\chromedriver\chromedriver.exe",
        r"C:\WebDriver\bin\chromedriver.exe",
        r"C:\Program Files\ChromeDriver\chromedriver.exe",
        r"chromedriver.exe"  # Current directory
    ]

    for path in common_paths:
        if os.path.exists(path):
            try:
                print(f"  Trying ChromeDriver at: {path}")
                service = Service(path)
                driver = webdriver.Chrome(service=service, options=options)
                print(f"  [OK] Chrome driver initialized with {path}")
                return driver
            except Exception as e:
                print(f"  [ERROR] Failed with {path}: {e}")

    print("\n[FAILED] Could not initialize Chrome driver!")
    print("\nTroubleshooting steps:")
    print("1. Download ChromeDriver from: https://chromedriver.chromium.org/")
    print("2. Place chromedriver.exe in the current directory or C:\\chromedriver\\")
    print("3. Ensure Chrome browser is installed and up-to-date")
    print("4. Try running: webdriver-manager clean")

    return None

def scrape_test():
    """Simple test scrape to verify setup"""
    print("\n" + "="*60)
    print("CARISTIX SCRAPER TEST")
    print("="*60)

    # Setup output directory
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    print(f"\nOutput directory: {OUTPUT_DIR}")

    # Setup driver
    driver = setup_driver()
    if not driver:
        return False

    try:
        print(f"\nNavigating to: {BASE_URL}")
        driver.get(BASE_URL)

        # Wait for page to load
        print("Waiting for page to load...")
        WebDriverWait(driver, 15).until(
            EC.presence_of_element_located((By.TAG_NAME, "body"))
        )

        # Get page title
        title = driver.title
        print(f"[OK] Page loaded: {title}")

        # Look for trigger event links
        print("\nSearching for trigger event links...")
        links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
        print(f"[OK] Found {len(links)} trigger event links")

        # Get first 3 events
        event_urls = []
        for link in links[:3]:
            href = link.get_attribute("href")
            text = link.text
            if href and "/TriggerEvents/" in href and href != BASE_URL:
                event_urls.append((href, text))
                print(f"  - {text}: {href}")

        print(f"\n[OK] Successfully connected to Caristix!")
        print(f"[OK] Found {len(event_urls)} test events")

        # Save test results
        test_result = {
            "test_time": time.strftime("%Y-%m-%d %H:%M:%S"),
            "page_title": title,
            "events_found": len(links),
            "sample_events": [{"url": url, "text": text} for url, text in event_urls],
            "status": "success"
        }

        test_file = OUTPUT_DIR / "test_connection.json"
        with open(test_file, "w", encoding="utf-8") as f:
            json.dump(test_result, f, indent=2)

        print(f"\n[OK] Test results saved to: {test_file}")
        return True

    except Exception as e:
        print(f"\n[ERROR] Error during test: {e}")
        return False

    finally:
        print("\nClosing driver...")
        driver.quit()
        print("[OK] Driver closed")

def main():
    print("Caristix Scraper - Simple Test Version")
    print("="*60)

    success = scrape_test()

    if success:
        print("\n[SUCCESS] TEST SUCCESSFUL!")
        print("\nNext steps:")
        print("1. Review test results in test_connection.json")
        print("2. Run full scraper: python caristix-scraper-enhanced.py")
    else:
        print("\n[FAILED] TEST FAILED!")
        print("\nPlease resolve ChromeDriver issues before running full scraper")

if __name__ == "__main__":
    main()