#!/usr/bin/env python3
"""
Analyze Caristix site structure to understand how trigger events are organized
"""

import time
import re
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By

def analyze_site_structure():
    """Analyze Caristix site structure for HL7 v2.3 organization"""
    print("Analyzing Caristix HL7 v2.3 site structure...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        # Check the main HL7 v2.3 page
        urls_to_check = [
            "https://hl7-definition.caristix.com/v2/HL7v2.3",
            "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents",
            "https://hl7-definition.caristix.com/v2/HL7v2.3/Messages",
            "https://hl7-definition.caristix.com/v2/HL7v2.3/Segments"
        ]

        for url in urls_to_check:
            print(f"\n{'='*60}")
            print(f"Analyzing: {url}")
            print('='*60)

            try:
                driver.get(url)
                time.sleep(3)

                title = driver.title
                print(f"Page title: {title}")

                # Count all links on the page
                all_links = driver.find_elements(By.TAG_NAME, "a")
                print(f"Total links on page: {len(all_links)}")

                # Look for HL7-related links
                hl7_patterns = [
                    r'ADT[_-]A\d+',
                    r'ORM[_-][A-Z]\d+',
                    r'ORU[_-][A-Z]\d+',
                    r'SIU[_-][A-Z]\d+',
                    r'MDM[_-][A-Z]\d+',
                    r'[A-Z]{3}[_-][A-Z]\d+'
                ]

                page_source = driver.page_source

                for pattern in hl7_patterns:
                    matches = re.findall(pattern, page_source, re.IGNORECASE)
                    unique_matches = set(matches)
                    if unique_matches:
                        print(f"\nPattern '{pattern}': {len(unique_matches)} unique matches")
                        for match in sorted(unique_matches)[:10]:
                            print(f"  {match}")

                # Look for navigation menus or categories
                nav_selectors = [
                    ".navigation",
                    ".menu",
                    ".sidebar",
                    ".nav",
                    "nav",
                    ".categories",
                    ".tabs",
                    ".tab-content"
                ]

                for selector in nav_selectors:
                    try:
                        elements = driver.find_elements(By.CSS_SELECTOR, selector)
                        if elements:
                            print(f"\nFound {len(elements)} '{selector}' elements")
                            for i, elem in enumerate(elements[:2]):
                                text = elem.text.strip()
                                if text and len(text) < 500:
                                    print(f"  {selector}[{i}]: {text[:200]}...")
                    except:
                        pass

                # Check for specific trigger event types
                if "TriggerEvents" in url:
                    # Look for message type categories
                    links_with_text = []
                    for link in all_links:
                        href = link.get_attribute("href")
                        text = link.text.strip()
                        if href and text:
                            # Check for message type patterns
                            if any(pattern in text.upper() for pattern in ['ADT', 'ORM', 'ORU', 'SIU', 'MDM', 'ACK', 'QRY']):
                                links_with_text.append((text, href))

                    print(f"\nMessage-type links found: {len(links_with_text)}")
                    for text, href in sorted(links_with_text)[:20]:
                        print(f"  {text}: {href}")

            except Exception as e:
                print(f"Error analyzing {url}: {e}")

        # Check if there are different versions or comprehensive lists
        print(f"\n{'='*60}")
        print("Checking for comprehensive trigger event lists...")
        print('='*60)

        comprehensive_urls = [
            "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents?all=true",
            "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/list",
            "https://hl7-definition.caristix.com/v2/HL7v2.3/Messages",
            "https://hl7-definition.caristix.com"  # Root page
        ]

        for url in comprehensive_urls:
            try:
                print(f"\nTrying: {url}")
                driver.get(url)
                time.sleep(2)

                # Quick check for event count
                trigger_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                message_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/Messages/']")

                print(f"  Trigger event links: {len(trigger_links)}")
                print(f"  Message links: {len(message_links)}")

                if len(trigger_links) > 12 or len(message_links) > 0:
                    print(f"  [FOUND MORE] This page has more content!")

                    # Get some sample links
                    for link in (trigger_links + message_links)[:10]:
                        href = link.get_attribute("href")
                        text = link.text.strip()
                        print(f"    {text}: {href}")

            except Exception as e:
                print(f"  Error: {e}")

        return True

    except Exception as e:
        print(f"Analysis failed: {e}")
        return False

    finally:
        driver.quit()

if __name__ == "__main__":
    analyze_site_structure()