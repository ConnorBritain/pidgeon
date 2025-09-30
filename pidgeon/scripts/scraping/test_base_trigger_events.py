#!/usr/bin/env python3
"""
Test TriggerEvents integration in base scraper with enhanced hierarchy and description extraction
"""

import sys
import json
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_base_trigger_events():
    """Test TriggerEvents domain with integrated base scraper"""
    logger.info("Testing TriggerEvents domain with integrated base scraper...")

    output_dir = Path("./test_base_trigger_events")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        scraper = create_scraper(
            domain="TriggerEvents",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Test specific trigger event URL - ADT_A01
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"

        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            scraper.driver.get(test_url)
            scraper.driver.implicitly_wait(3)

            result = scraper._parse_domain_specific_page(test_url, "ADT_A01")

            if result:
                filename = f"v23_ADT_A01.json"
                filepath = output_dir / filename

                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ TriggerEvents test saved: {filename}")

                # Validate enhanced structure
                logger.info(f"TriggerEvents result structure:")
                logger.info(f"  Code: {result.get('code', 'Unknown')}")
                logger.info(f"  Name: '{result.get('name', 'Unknown')}'")
                logger.info(f"  Version: '{result.get('version', 'Unknown')}'")
                logger.info(f"  Chapter: {result.get('chapter', 'Unknown')}")
                logger.info(f"  Description: {len(result.get('description', ''))} characters")
                logger.info(f"  Segments: {len(result.get('segments', []))}")
                logger.info(f"  Has detail_url: {'detail_url' in result}")

                # Enhanced validation criteria
                is_valid = (
                    'detail_url' not in result and
                    result.get('name') == 'Admit/visit notification' and
                    result.get('version') == 'HL7 v2.3' and
                    result.get('chapter') == 'Patient Administration' and
                    len(result.get('description', '')) > 100 and  # Should have detailed description
                    len(result.get('segments', [])) > 15  # Should have many segments with hierarchy
                )

                # Check for hierarchy preservation (PROCEDURE, INSURANCE groups)
                segments = result.get('segments', [])
                has_procedure_group = any(s.get('segment_code') == 'PROCEDURE' for s in segments)
                has_insurance_group = any(s.get('segment_code') == 'INSURANCE' for s in segments)
                has_nested_segments = any(s.get('level', 0) > 0 for s in segments)

                hierarchy_valid = has_procedure_group and has_insurance_group and has_nested_segments

                logger.info(f"  Hierarchy Check - PROCEDURE: {has_procedure_group}, INSURANCE: {has_insurance_group}, Nested: {has_nested_segments}")

                overall_valid = is_valid and hierarchy_valid

                if overall_valid:
                    logger.info("✅ TriggerEvents integration test PASSED!")
                else:
                    logger.error("❌ TriggerEvents integration test FAILED - Missing enhanced features")

                return overall_valid
            else:
                logger.error("❌ TriggerEvents test failed - No result returned")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ TriggerEvents test error: {e}")
        return False

def main():
    """Test TriggerEvents integration in base scraper"""
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    logger.info("Testing TriggerEvents integration in base scraper...")

    success = test_base_trigger_events()

    # Summary
    logger.info(f"\n{'='*60}")
    logger.info("TRIGGEREVENT INTEGRATION TEST RESULTS")
    logger.info(f"{'='*60}")
    logger.info(f"  TriggerEvents Integration: {'✅ PASS' if success else '❌ FAIL'}")

    if success:
        logger.info(f"\n🎉 TriggerEvents integration PASSED - Enhanced features working in base scraper!")
    else:
        logger.error(f"\n💥 TriggerEvents integration FAILED - Check enhanced features integration")

    return success

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)