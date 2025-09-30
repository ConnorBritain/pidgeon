#!/usr/bin/env python3
"""
Test TriggerEvents using the working enhanced scraper with version field fixes applied
"""

import sys
import json
import importlib.util
from pathlib import Path
from loguru import logger

def test_trigger_event_enhanced():
    """Test trigger event with enhanced scraper and version fixes"""

    logger.info("Testing TriggerEvents with enhanced scraper + version fixes")

    # Create output directory
    output_dir = Path("./test_enhanced_triggerevents")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        # Load the enhanced scraper module
        spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
        scraper_module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(scraper_module)

        CaristixScraper = scraper_module.CaristixScraper

        # Create enhanced scraper
        scraper = CaristixScraper(
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        logger.info("Created enhanced TriggerEvents scraper...")

        # Test specific URL directly
        test_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADT_A01"

        # Initialize driver
        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to initialize driver")
            return False

        try:
            # Parse using enhanced scraper
            result = scraper.parse_event_page(test_url)

            if result:
                # The enhanced scraper now provides all fields correctly, no fixes needed
                result_final = result

                # Save the result
                filename = f"v23_{result_final['code']}.json"
                filepath = output_dir / filename

                with open(filepath, 'w', encoding='utf-8') as f:
                    json.dump(result_final, f, indent=2, ensure_ascii=False)

                logger.info(f"✅ Saved enhanced trigger event: {filename}")

                # Validate the enhanced result
                logger.info(f"\n{'='*60}")
                logger.info("ENHANCED TRIGGER EVENT VALIDATION")
                logger.info(f"{'='*60}")
                logger.info(f"  Code: {result_final.get('code', 'Unknown')}")
                logger.info(f"  Name: '{result_final.get('name', 'Unknown')}'")
                logger.info(f"  Version: '{result_final.get('version', 'Unknown')}'")
                logger.info(f"  Chapter: {result_final.get('chapter', 'Unknown')}")
                logger.info(f"  Description: {result_final.get('description', 'No description')[:100]}...")
                logger.info(f"  Segments: {len(result_final.get('segments', []))}")

                segments = result_final.get('segments', [])
                if segments:
                    groups = [seg for seg in segments if seg.get('is_group')]
                    logger.info(f"  Groups found: {len(groups)}")
                    if groups:
                        group_names = [g['segment_code'] for g in groups]
                        logger.info(f"  Group names: {', '.join(group_names)}")

                    # Show hierarchy levels
                    levels = set(seg.get('level', 0) for seg in segments)
                    logger.info(f"  Hierarchy levels: {sorted(levels)}")

                    # Show first few segments
                    logger.info(f"\n  First 5 segments:")
                    for i, seg in enumerate(segments[:5]):
                        level_indent = "  " * seg.get('level', 0)
                        group_marker = "[GROUP]" if seg.get('is_group') else ""
                        logger.info(f"    {i+1}. {level_indent}{seg.get('segment_code', 'Unknown')} - {seg.get('segment_desc', 'No description')} {group_marker}")

                # Validate fixes
                fixes_valid = validate_enhanced_fixes(result_final)

                return fixes_valid

            else:
                logger.error("❌ Enhanced scraper failed to parse trigger event")
                return False

        finally:
            if scraper.driver:
                scraper.driver.quit()

    except Exception as e:
        logger.error(f"❌ Enhanced trigger event test ERROR: {e}")
        import traceback
        traceback.print_exc()
        return False

def apply_version_field_fix(result):
    """Apply version field fix to maintain compatibility with other domain fixes"""
    result_fixed = result.copy()

    # Extract version from title if present
    if 'title' in result_fixed:
        title = result_fixed['title']

        # Parse title to extract version: "HL7 v2.3 - CODE - Descriptive Name"
        if " - " in title:
            parts = title.split(" - ")
            if len(parts) >= 1:
                version = parts[0]  # "HL7 v2.3" or similar
                if 'HL7' in version:
                    result_fixed['version'] = version
                else:
                    result_fixed['version'] = "HL7 v2.3"  # Default

                # Extract name (everything after the code)
                if len(parts) >= 3:
                    result_fixed['name'] = " - ".join(parts[2:])
                elif len(parts) == 2:
                    result_fixed['name'] = parts[1]

        # Remove the old title field to match other domains
        del result_fixed['title']

    # Ensure version field exists
    if 'version' not in result_fixed:
        result_fixed['version'] = "HL7 v2.3"

    return result_fixed

def validate_enhanced_fixes(result):
    """Validate that the enhanced result has all expected fixes"""
    fixes_valid = True

    # Validate code extraction
    if result.get('code', 'unknown') == 'unknown':
        logger.warning(f"  ❌ Code still showing as 'unknown'")
        fixes_valid = False
    else:
        logger.info(f"  ✅ Code properly extracted: {result.get('code')}")

    # Validate version field
    version = result.get('version', 'Unknown')
    if 'version' in result:
        if version == 'Unknown' or 'HL7' not in version:
            logger.warning(f"  ❌ Version not properly extracted: '{version}'")
            fixes_valid = False
        else:
            logger.info(f"  ✅ Version properly extracted: '{version}'")
    else:
        logger.warning(f"  ❌ Version field missing")
        fixes_valid = False

    # Validate name extraction
    name = result.get('name', '')
    if not name or name == 'Unknown':
        logger.warning(f"  ❌ Name not extracted: '{name}'")
        fixes_valid = False
    else:
        logger.info(f"  ✅ Name properly extracted: '{name}'")

    # Validate proper field structure (no title field, has version and name)
    if 'title' in result:
        logger.warning(f"  ❌ Still has redundant 'title' field")
        fixes_valid = False
    elif 'version' in result and 'name' in result:
        logger.info(f"  ✅ Has proper version and name fields without redundant title")
    else:
        logger.warning(f"  ❌ Missing proper version/name field structure")
        fixes_valid = False

    # Validate enhanced features (segments, hierarchy)
    segments = result.get('segments', [])
    if len(segments) > 5:  # Enhanced should have many segments
        logger.info(f"  ✅ Enhanced segment extraction: {len(segments)} segments")
    else:
        logger.warning(f"  ❌ Limited segment extraction: {len(segments)} segments")
        fixes_valid = False

    # Validate chapter extraction
    chapter = result.get('chapter', 'Unknown')
    if chapter != 'Unknown':
        logger.info(f"  ✅ Chapter extracted: {chapter}")
    else:
        logger.warning(f"  ❌ Chapter not extracted")
        fixes_valid = False

    return fixes_valid

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_trigger_event_enhanced()

    if success:
        logger.info("\n✨ Enhanced TriggerEvents fixes validation PASSED!")
    else:
        logger.error("\n💥 Enhanced TriggerEvents fixes validation FAILED")