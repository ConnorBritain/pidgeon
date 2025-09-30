#!/usr/bin/env python3
"""
Test PID segment specifically - crucial segment with rich description
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_pid_segment():
    """Test PID segment scraping"""

    logger.info("Testing PID segment - Patient Identification")

    # Create output directory
    output_dir = Path("./test_pid_segment")
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        # Create segments scraper
        scraper = create_scraper(
            domain="Segments",
            dest_path=output_dir,
            headless=True,
            delay_ms=1000
        )

        # Override to scrape just PID by going directly to URL
        scraper.driver = scraper.setup_driver()
        if not scraper.driver:
            logger.error("Failed to setup driver")
            return False

        # Go directly to PID segment
        pid_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/Segments/PID"
        logger.info(f"Testing PID segment directly at: {pid_url}")

        pid_data = scraper.parse_item_page(pid_url)

        if pid_data:
            # Save the data
            scraper.save_item_data(pid_data)

            logger.info(f"\n{'='*60}")
            logger.info("PID SEGMENT SCRAPER RESULTS")
            logger.info(f"{'='*60}")
            logger.info(f"Code: {pid_data.get('code', 'Unknown')}")
            logger.info(f"Name: {pid_data.get('name', 'Unknown')}")
            logger.info(f"Chapter: {pid_data.get('chapter', 'Unknown')}")
            logger.info(f"Description length: {len(pid_data.get('description', ''))}")
            logger.info(f"Total fields: {len(pid_data.get('fields', []))}")

            # Show description
            description = pid_data.get('description', '')
            if description:
                logger.info(f"\nDescription preview:")
                logger.info(f"  {description[:200]}...")
            else:
                logger.info(f"\nNo description captured")

            # Show first few fields
            fields = pid_data.get('fields', [])[:5]
            logger.info(f"\nFirst 5 fields:")
            for field in fields:
                pos = field.get('position', '?')
                name = field.get('field_name', 'Unknown')
                desc = field.get('field_description', 'Unknown')
                data_type = field.get('data_type', 'Unknown')
                table = field.get('table', '')
                table_info = f" (Table: {table})" if table else ""

                logger.info(f"  {pos:2d}. {name} - {desc}")
                logger.info(f"      Type: {data_type}{table_info}")

            return True
        else:
            logger.error("Failed to scrape PID segment")
            return False

    except Exception as e:
        logger.error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        if hasattr(scraper, 'driver') and scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_pid_segment()

    if success:
        logger.info(f"\nPID segment test PASSED!")
    else:
        logger.error(f"\nPID segment test FAILED")