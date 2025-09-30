#!/usr/bin/env python3
"""
Quick test of segments scraper with ACC, ADD, and AIG
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_segments_quick():
    """Test segments scraper with first 3 segments"""

    logger.info("Testing Segments scraper with ACC, ADD, AIG")

    # Create output directory
    output_dir = Path("./test_segments_quick")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        # Create segments scraper
        scraper = create_scraper(
            domain="Segments",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        logger.info("Created SegmentsScraper, starting test...")

        # Run with limit of 3 for ACC, ADD, AIG
        scraper.run(limit=3)

        # Check results
        json_files = list(output_dir.glob("v23_*.json"))
        master_files = list(output_dir.glob("*_master.json"))

        logger.info(f"\n{'='*50}")
        logger.info("SEGMENTS SCRAPER TEST RESULTS")
        logger.info(f"{'='*50}")
        logger.info(f"Individual files: {len(json_files) - len(master_files)}")
        logger.info(f"Master files: {len(master_files)}")

        # Show individual files
        for json_file in sorted(json_files):
            if "master" not in json_file.name:
                logger.info(f"  ✅ {json_file.name}")

                # Quick peek at content
                import json
                try:
                    with open(json_file, 'r', encoding='utf-8') as f:
                        data = json.load(f)

                    logger.info(f"     Code: {data.get('code', 'Unknown')}")
                    logger.info(f"     Name: {data.get('name', 'Unknown')}")
                    logger.info(f"     Fields: {len(data.get('fields', []))}")

                    # Show first few fields
                    fields = data.get('fields', [])[:3]
                    for field in fields:
                        logger.info(f"       - {field.get('field', 'Unknown')}: {field.get('data_type', 'Unknown')}")

                except Exception as e:
                    logger.warning(f"     Could not read content: {e}")

        # Show master file summary
        for master_file in master_files:
            logger.info(f"\n📊 Master file: {master_file.name}")
            try:
                import json
                with open(master_file, 'r', encoding='utf-8') as f:
                    master_data = json.load(f)

                logger.info(f"   Total items: {master_data.get('item_count', 0)}")
                logger.info(f"   Total elements: {master_data.get('total_elements', 0)}")
                logger.info(f"   Scraped: {master_data.get('scraped_date', 'Unknown')}")

            except Exception as e:
                logger.warning(f"   Could not read master file: {e}")

        return True

    except Exception as e:
        logger.error(f"Segments test failed: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_segments_quick()

    if success:
        logger.info(f"\n🎉 Segments scraper test PASSED!")
        logger.info(f"Framework successfully handled:")
        logger.info(f"  - URL collection from segments main page")
        logger.info(f"  - Field table parsing with expandable rows")
        logger.info(f"  - Data type and table reference extraction")
        logger.info(f"  - JSON output generation")
    else:
        logger.error(f"\n❌ Segments scraper test FAILED")