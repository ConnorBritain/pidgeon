#!/usr/bin/env python3
"""
Test all domain scrapers with the enhanced base framework
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_all_domains():
    """Test each domain scraper with a small sample"""

    logger.info("Testing all domain scrapers with enhanced base framework")

    domains = ['Segments', 'DataTypes', 'Tables']
    results = {}

    for domain in domains:
        logger.info(f"\n{'='*60}")
        logger.info(f"TESTING {domain.upper()} SCRAPER")
        logger.info(f"{'='*60}")

        # Create output directory
        output_dir = Path(f"./test_{domain.lower()}")
        output_dir.mkdir(parents=True, exist_ok=True)

        try:
            # Create domain scraper
            scraper = create_scraper(
                domain=domain,
                dest_path=output_dir,
                headless=True,
                delay_ms=1000
            )

            logger.info(f"Created {domain}Scraper, starting test...")

            # Run with limit of 2 for quick testing
            scraper.run(limit=2)

            # Check results
            json_files = list(output_dir.glob("v23_*.json"))

            if json_files:
                logger.info(f"✅ {domain} scraper SUCCESS: {len(json_files)} files created")

                # Show sample data
                import json
                sample_file = json_files[0]
                with open(sample_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)

                logger.info(f"Sample {domain.lower()[:-1]}: {data.get('code', 'Unknown')}")

                if domain == 'Segments':
                    fields = data.get('fields', [])
                    logger.info(f"  Fields: {len(fields)}")
                    if fields:
                        logger.info(f"  Sample field: {fields[0].get('field_name', 'Unknown')}")
                elif domain == 'DataTypes':
                    components = data.get('components', [])
                    logger.info(f"  Components: {len(components)}")
                    logger.info(f"  Category: {data.get('category', 'Unknown')}")
                elif domain == 'Tables':
                    values = data.get('values', [])
                    logger.info(f"  Values: {len(values)}")
                    if values:
                        logger.info(f"  Sample value: {values[0].get('value', 'Unknown')} = {values[0].get('description', 'Unknown')}")

                results[domain] = 'SUCCESS'

            else:
                logger.error(f"❌ {domain} scraper FAILED: No files created")
                results[domain] = 'FAILED'

        except Exception as e:
            logger.error(f"❌ {domain} scraper ERROR: {e}")
            import traceback
            traceback.print_exc()
            results[domain] = 'ERROR'

    # Summary
    logger.info(f"\n{'='*60}")
    logger.info("DOMAIN SCRAPER TEST RESULTS SUMMARY")
    logger.info(f"{'='*60}")

    for domain, result in results.items():
        status_icon = "✅" if result == 'SUCCESS' else "❌"
        logger.info(f"{status_icon} {domain}: {result}")

    success_count = sum(1 for result in results.values() if result == 'SUCCESS')
    total_count = len(results)

    if success_count == total_count:
        logger.info(f"\n🎉 ALL {total_count} DOMAIN SCRAPERS PASSED!")
        return True
    else:
        logger.info(f"\n⚠️  {success_count}/{total_count} domain scrapers passed")
        return False

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_all_domains()

    if success:
        logger.info("\n✨ Domain scraper framework test PASSED!")
    else:
        logger.error("\n💥 Domain scraper framework test FAILED")