#!/usr/bin/env python3
"""
Test all other domains (Tables, DataTypes, Segments) to ensure clean data structure
"""

import sys
import json
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_tables_domain():
    """Test Tables domain with clean structure"""
    logger.info("Testing Tables domain...")

    output_dir = Path("./test_clean_tables")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scraper = create_scraper(
            domain="Tables",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Test specific table URL
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/Tables/0001"

        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            scraper.driver.get(test_url)
            scraper.driver.implicitly_wait(3)

            result = scraper._parse_domain_specific_page(test_url, "0001")

            if result:
                filename = f"v23_0001.json"
                filepath = output_dir / filename

                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ Tables test saved: {filename}")

                # Validate structure
                logger.info(f"Tables result structure:")
                logger.info(f"  Code: {result.get('code', 'Unknown')}")
                logger.info(f"  Name: '{result.get('name', 'Unknown')}'")
                logger.info(f"  Version: '{result.get('version', 'Unknown')}'")
                logger.info(f"  Chapter: {result.get('chapter', 'Unknown')}")
                logger.info(f"  Type: {result.get('type', 'Unknown')}")
                logger.info(f"  Values: {len(result.get('values', []))}")
                logger.info(f"  Has detail_url: {'detail_url' in result}")

                return 'detail_url' not in result and result.get('name') != 'Unknown'
            else:
                logger.error("❌ Tables test failed")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ Tables test error: {e}")
        return False

def test_datatypes_domain():
    """Test DataTypes domain with clean structure"""
    logger.info("Testing DataTypes domain...")

    output_dir = Path("./test_clean_datatypes")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scraper = create_scraper(
            domain="DataTypes",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Test specific datatype URL
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/DataTypes/AD"

        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            scraper.driver.get(test_url)
            scraper.driver.implicitly_wait(3)

            result = scraper._parse_domain_specific_page(test_url, "AD")

            if result:
                filename = f"v23_AD.json"
                filepath = output_dir / filename

                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ DataTypes test saved: {filename}")

                # Validate structure
                logger.info(f"DataTypes result structure:")
                logger.info(f"  Code: {result.get('code', 'Unknown')}")
                logger.info(f"  Name: '{result.get('name', 'Unknown')}'")
                logger.info(f"  Version: '{result.get('version', 'Unknown')}'")
                logger.info(f"  Category: {result.get('category', 'Unknown')}")
                logger.info(f"  Fields: {len(result.get('fields', []))}")
                logger.info(f"  Has detail_url: {'detail_url' in result}")

                return 'detail_url' not in result and result.get('name') != 'Unknown'
            else:
                logger.error("❌ DataTypes test failed")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ DataTypes test error: {e}")
        return False

def test_segments_domain():
    """Test Segments domain with clean structure"""
    logger.info("Testing Segments domain...")

    output_dir = Path("./test_clean_segments")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scraper = create_scraper(
            domain="Segments",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Test specific segment URL
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/Segments/PID"

        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            scraper.driver.get(test_url)
            scraper.driver.implicitly_wait(3)

            result = scraper._parse_domain_specific_page(test_url, "PID")

            if result:
                filename = f"v23_PID.json"
                filepath = output_dir / filename

                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ Segments test saved: {filename}")

                # Validate structure
                logger.info(f"Segments result structure:")
                logger.info(f"  Code: {result.get('code', 'Unknown')}")
                logger.info(f"  Name: '{result.get('name', 'Unknown')}'")
                logger.info(f"  Version: '{result.get('version', 'Unknown')}'")
                logger.info(f"  Chapter: {result.get('chapter', 'Unknown')}")
                logger.info(f"  Fields: {len(result.get('fields', []))}")
                logger.info(f"  Has detail_url: {'detail_url' in result}")

                return 'detail_url' not in result and result.get('name') != 'Unknown'
            else:
                logger.error("❌ Segments test failed")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ Segments test error: {e}")
        return False

def main():
    """Test all three domains"""
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    logger.info("Testing all domains for clean data structure...")

    # Test all domains
    tables_success = test_tables_domain()
    datatypes_success = test_datatypes_domain()
    segments_success = test_segments_domain()

    # Summary
    logger.info(f"\n{'='*60}")
    logger.info("DOMAIN TEST RESULTS")
    logger.info(f"{'='*60}")
    logger.info(f"  Tables: {'✅ PASS' if tables_success else '❌ FAIL'}")
    logger.info(f"  DataTypes: {'✅ PASS' if datatypes_success else '❌ FAIL'}")
    logger.info(f"  Segments: {'✅ PASS' if segments_success else '❌ FAIL'}")

    overall_success = tables_success and datatypes_success and segments_success

    if overall_success:
        logger.info(f"\n🎉 All domains PASSED - Clean data structure confirmed!")
    else:
        logger.error(f"\n💥 Some domains FAILED - Check individual results")

    return overall_success

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)