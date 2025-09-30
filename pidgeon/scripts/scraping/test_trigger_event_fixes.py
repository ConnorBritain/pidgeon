#!/usr/bin/env python3
"""
Test trigger event scraper with the version extraction fixes
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_trigger_event_fixes():
    """Test trigger event scraper to validate the version extraction"""

    logger.info("Testing TriggerEvents scraper fixes")

    # Create output directory
    output_dir = Path("./test_fixes_triggerevents")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        # Create trigger events scraper
        scraper = create_scraper(
            domain="TriggerEvents",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        logger.info("Created TriggerEvents scraper, testing specific URL...")

        # Test specific URL directly
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"

        # Initialize driver
        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            # Navigate to specific URL and parse
            scraper.driver.get(test_url)
            scraper.driver.implicitly_wait(3)

            # Extract code from URL
            code = "ADT_A01"
            logger.info(f"Parsing trigger event: {code}")

            # Parse the trigger event
            result = scraper._parse_domain_specific_page(test_url, code)

            if result:
                # Save the result
                filename = f"v23_{code}.json"
                filepath = output_dir / filename

                import json
                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ Saved trigger event: {filename}")

        finally:
            if scraper.driver:
                scraper.driver.quit()

        # Check results
        json_files = list(output_dir.glob("v23_*.json"))

        if json_files:
            import json
            sample_file = json_files[0]
            with open(sample_file, 'r', encoding='utf-8') as f:
                data = json.load(f)

            logger.info(f"✅ TriggerEvents scraper SUCCESS: {sample_file.name}")

            logger.info(f"\n{'='*60}")
            logger.info("TRIGGER EVENT FIXES VALIDATION")
            logger.info(f"{'='*60}")
            logger.info(f"  Code: {data.get('code', 'Unknown')}")
            logger.info(f"  Name: '{data.get('name', 'Unknown')}'")
            logger.info(f"  Version: '{data.get('version', 'Unknown')}'")
            logger.info(f"  Chapter: {data.get('chapter', 'Unknown')}")
            logger.info(f"  Segments: {len(data.get('segments', []))}")
            if data.get('segments'):
                first_segment = data.get('segments', [])[0]
                logger.info(f"  Sample segment: {first_segment.get('segment_code', 'Unknown')} - {first_segment.get('segment_desc', 'Unknown')}")

            # Validate fixes
            fixes_valid = True

            # Validate code is not "unknown"
            if data.get('code', 'unknown') == 'unknown':
                logger.warning(f"  ❌ Code still showing as 'unknown'")
                fixes_valid = False
            else:
                logger.info(f"  ✅ Code properly extracted")

            # Validate version is extracted properly
            version = data.get('version', 'Unknown')
            if version == 'Unknown' or 'HL7' not in version:
                logger.warning(f"  ❌ Version not properly extracted: '{version}'")
                fixes_valid = False
            else:
                logger.info(f"  ✅ Version properly extracted: '{version}'")

            # Validate name is not generic
            name = data.get('name', '')
            if not name or name == 'Unknown':
                logger.warning(f"  ❌ Name not extracted: '{name}'")
                fixes_valid = False
            else:
                logger.info(f"  ✅ Name properly extracted: '{name}'")

            # Validate no title field (should be replaced by version)
            if 'title' in data:
                logger.warning(f"  ❌ Still has 'title' field instead of 'version'")
                fixes_valid = False
            else:
                logger.info(f"  ✅ Using 'version' instead of 'title'")

            return fixes_valid

        else:
            logger.error("❌ TriggerEvents scraper FAILED: No files created")
            return False

    except Exception as e:
        logger.error(f"❌ TriggerEvents scraper ERROR: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_trigger_event_fixes()

    if success:
        logger.info("\n✨ TriggerEvents fixes validation PASSED!")
    else:
        logger.error("\n💥 TriggerEvents fixes validation FAILED")