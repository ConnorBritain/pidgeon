#!/usr/bin/env python3
"""
Test Segments domain using base scraper to ensure clean data structure
"""

import sys
import json
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_segments_domain():
    """Test Segments domain with base scraper"""
    logger.info("Testing Segments domain with base scraper...")

    output_dir = Path("./test_segments_base")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scraper = create_scraper(
            domain="Segments",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Test specific segment URL - PID segment
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

                # Validation criteria: clean structure, no detail_url, proper field count
                is_valid = (
                    'detail_url' not in result and
                    result.get('name') != 'Unknown' and
                    len(result.get('fields', [])) > 10  # PID should have many fields
                )

                if is_valid:
                    logger.info("✅ Segments test PASSED - Clean data structure confirmed!")
                else:
                    logger.error("❌ Segments test FAILED - Structure issues detected")

                return is_valid
            else:
                logger.error("❌ Segments test failed - No result returned")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ Segments test error: {e}")
        return False

def main():
    """Test Segments domain"""
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    logger.info("Testing Segments domain with base scraper...")

    success = test_segments_domain()

    # Summary
    logger.info(f"\n{'='*60}")
    logger.info("SEGMENTS TEST RESULTS")
    logger.info(f"{'='*60}")
    logger.info(f"  Segments: {'✅ PASS' if success else '❌ FAIL'}")

    if success:
        logger.info(f"\n🎉 Segments domain PASSED - Clean data structure confirmed!")
    else:
        logger.error(f"\n💥 Segments domain FAILED - Check output for details")

    return success

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)