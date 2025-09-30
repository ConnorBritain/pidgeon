#!/usr/bin/env python3
"""
Test script to verify proper title and description extraction for TriggerEvents
"""

import sys
import time
from pathlib import Path
from loguru import logger
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

def test_title_description_extraction():
    """Test extracting title and description using the exact selectors provided"""

    logger.info("Testing title and description extraction for ADT_A01")

    # Setup Chrome driver
    chrome_options = Options()
    chrome_options.add_argument("--headless")
    chrome_options.add_argument("--no-sandbox")
    chrome_options.add_argument("--disable-dev-shm-usage")
    chrome_options.add_argument("--disable-gpu")
    chrome_options.add_argument("--window-size=1920,1080")

    driver = None
    try:
        driver = webdriver.Chrome(options=chrome_options)
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"

        logger.info(f"Navigating to: {test_url}")
        driver.get(test_url)

        # Wait for page to load
        WebDriverWait(driver, 15).until(
            EC.presence_of_element_located((By.TAG_NAME, "h2"))
        )
        time.sleep(2)

        # Extract title using the exact selector provided
        title_text = ""
        try:
            title_selector = "h2.cx-h2.content-header-title-centered"
            title_element = driver.find_element(By.CSS_SELECTOR, title_selector)
            title_text = title_element.text.strip()
            logger.info(f"✅ Title extracted: '{title_text}'")
        except Exception as e:
            logger.error(f"❌ Failed to extract title: {e}")

        # Parse title to extract components
        version = ""
        code = ""
        name = ""

        if " - " in title_text:
            parts = title_text.split(" - ")
            if len(parts) >= 3:
                version = parts[0].strip()  # "HL7 v2.3"
                code = parts[1].strip()     # "ADT_A01"
                name = parts[2].strip()     # "Admit/visit notification"
                logger.info(f"  Parsed version: '{version}'")
                logger.info(f"  Parsed code: '{code}'")
                logger.info(f"  Parsed name: '{name}'")
            else:
                logger.warning(f"  ❌ Title format unexpected: {len(parts)} parts")

        # Extract description using the exact selector provided
        description_text = ""
        try:
            desc_selector = "span.cx-body.text--preserve-paragraphs"
            desc_element = driver.find_element(By.CSS_SELECTOR, desc_selector)
            description_text = desc_element.text.strip()
            logger.info(f"✅ Description extracted ({len(description_text)} chars): '{description_text[:100]}...'")
        except Exception as e:
            logger.error(f"❌ Failed to extract description: {e}")

            # Try alternative selectors for description
            alternative_selectors = [
                ".detail-text-container span",
                ".cx-body",
                ".text--preserve-paragraphs",
                "span[class*='preserve-paragraphs']",
                ".detail-text-container .cx-body"
            ]

            for alt_selector in alternative_selectors:
                try:
                    alt_element = driver.find_element(By.CSS_SELECTOR, alt_selector)
                    alt_text = alt_element.text.strip()
                    if alt_text and len(alt_text) > 50:
                        description_text = alt_text
                        logger.info(f"✅ Description found with alternative selector '{alt_selector}': '{alt_text[:100]}...'")
                        break
                except:
                    continue

        # Validate extraction success
        success = True
        if not name or name == title_text:
            logger.error("❌ Name extraction failed")
            success = False
        else:
            logger.info(f"✅ Name successfully extracted: '{name}'")

        if not description_text or len(description_text) < 50:
            logger.error("❌ Description extraction failed")
            success = False
        else:
            logger.info(f"✅ Description successfully extracted: {len(description_text)} characters")

        if not version or "HL7" not in version:
            logger.error("❌ Version extraction failed")
            success = False
        else:
            logger.info(f"✅ Version successfully extracted: '{version}'")

        # Summary
        logger.info(f"\n{'='*60}")
        logger.info("EXTRACTION RESULTS SUMMARY")
        logger.info(f"{'='*60}")
        logger.info(f"  Full title: '{title_text}'")
        logger.info(f"  Version: '{version}'")
        logger.info(f"  Code: '{code}'")
        logger.info(f"  Name: '{name}'")
        logger.info(f"  Description: '{description_text}'")
        logger.info(f"  Success: {'✅' if success else '❌'}")

        return success

    except Exception as e:
        logger.error(f"❌ Test failed with error: {e}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        if driver:
            driver.quit()

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_title_description_extraction()

    if success:
        logger.info("\n✨ Title and description extraction test PASSED!")
    else:
        logger.error("\n💥 Title and description extraction test FAILED")