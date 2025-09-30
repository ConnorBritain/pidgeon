#!/usr/bin/env python3
"""
Test segments scraper with position and field name fixes
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_segments_fixed():
    """Test segments scraper with fixes for position and field name parsing"""

    logger.info("Testing FIXED Segments scraper with ACC segment")

    # Create output directory
    output_dir = Path("./test_segments_fixed")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        # Create segments scraper
        scraper = create_scraper(
            domain="Segments",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        logger.info("Created SegmentsScraper with fixes, starting test...")

        # Run with limit of 1 for just ACC to test fixes
        scraper.run(limit=1)

        # Check results
        json_files = list(output_dir.glob("v23_*.json"))

        if json_files:
            acc_file = json_files[0]
            logger.info(f"Reading {acc_file.name} to verify fixes...")

            import json
            with open(acc_file, 'r', encoding='utf-8') as f:
                data = json.load(f)

            logger.info(f"\n{'='*60}")
            logger.info("FIXED SEGMENTS SCRAPER RESULTS")
            logger.info(f"{'='*60}")
            logger.info(f"Segment: {data.get('code', 'Unknown')}")
            logger.info(f"Total fields: {len(data.get('fields', []))}")

            # Show fields with fixed structure
            fields = data.get('fields', [])
            for field in fields:
                pos = field.get('position', 'Unknown')
                name = field.get('field_name', 'Unknown')
                desc = field.get('field_description', 'Unknown')
                data_type = field.get('data_type', 'Unknown')
                table = field.get('table', '')
                table_info = f" (Table: {table})" if table else ""

                logger.info(f"  {pos:2d}. {name}")
                logger.info(f"      Description: {desc}")
                logger.info(f"      Type: {data_type}{table_info}")

            # Verify fixes
            logger.info(f"\n{'='*60}")
            logger.info("VERIFICATION OF FIXES")
            logger.info(f"{'='*60}")

            # Check position sequence
            positions = [field.get('position', 0) for field in fields]
            expected_positions = list(range(1, len(fields) + 1))
            positions_correct = positions == expected_positions

            logger.info(f"Position sequence: {positions}")
            logger.info(f"Expected sequence: {expected_positions}")
            logger.info(f"Positions FIXED: {'YES' if positions_correct else 'NO'}")

            # Check field name/description separation
            has_separate_fields = all(
                'field_name' in field and 'field_description' in field
                for field in fields
            )
            logger.info(f"Field name/description separation: {'YES' if has_separate_fields else 'NO'}")

            # Check that field names don't contain descriptions
            clean_field_names = all(
                ' - ' not in field.get('field_name', '')
                for field in fields
            )
            logger.info(f"Clean field names (no descriptions): {'YES' if clean_field_names else 'NO'}")

            if positions_correct and has_separate_fields and clean_field_names:
                logger.info(f"\n✅ ALL FIXES SUCCESSFUL!")
                return True
            else:
                logger.warning(f"\n❌ SOME FIXES STILL NEEDED")
                return False

        else:
            logger.error("No files were created")
            return False

    except Exception as e:
        logger.error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_segments_fixed()

    if success:
        logger.info(f"\n🎉 FIXED SEGMENTS SCRAPER TEST PASSED!")
    else:
        logger.error(f"\n❌ FIXED SEGMENTS SCRAPER TEST FAILED")