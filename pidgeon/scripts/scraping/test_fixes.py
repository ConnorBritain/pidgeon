#!/usr/bin/env python3
"""
Test all domain scrapers with the fixes for name extraction and field consistency
"""

import sys
from pathlib import Path
from loguru import logger
from base_caristix_scraper import create_scraper

def test_domain_fixes():
    """Test domain scrapers to validate the fixes"""

    logger.info("Testing domain scraper fixes")

    domains = ['Segments', 'DataTypes', 'Tables']
    results = {}

    for domain in domains:
        logger.info(f"\n{'='*60}")
        logger.info(f"TESTING {domain.upper()} FIXES")
        logger.info(f"{'='*60}")

        # Create output directory
        output_dir = Path(f"./test_fixes_{domain.lower()}")
        output_dir.mkdir(parents=True, exist_ok=True)

        try:
            # Create domain scraper
            scraper = create_scraper(
                domain=domain,
                dest_path=output_dir,
                headless=True,
                delay_ms=1000
            )

            logger.info(f"Created {domain}Scraper, testing fixes...")

            # Run with limit of 1 for focused testing
            scraper.run(limit=1)

            # Check results
            json_files = list(output_dir.glob("v23_*.json"))

            if json_files:
                import json
                sample_file = json_files[0]
                with open(sample_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)

                logger.info(f"✅ {domain} scraper SUCCESS: {sample_file.name}")

                # Validate fixes based on domain
                if domain == 'Segments':
                    logger.info(f"  Code: {data.get('code', 'Unknown')}")
                    logger.info(f"  Name: '{data.get('name', 'Unknown')}'")
                    logger.info(f"  Chapter: {data.get('chapter', 'Unknown')}")
                    fields = data.get('fields', [])
                    logger.info(f"  Fields: {len(fields)}")
                    if fields:
                        first_field = fields[0]
                        logger.info(f"  Sample field: {first_field.get('field_name', 'Unknown')} - {first_field.get('field_description', 'Unknown')}")

                elif domain == 'DataTypes':
                    logger.info(f"  Code: {data.get('code', 'Unknown')}")
                    logger.info(f"  Name: '{data.get('name', 'Unknown')}'")
                    logger.info(f"  Category: {data.get('category', 'Unknown')}")
                    fields = data.get('fields', [])  # Changed from components to fields
                    logger.info(f"  Fields: {len(fields)}")
                    if fields:
                        first_field = fields[0]
                        logger.info(f"  Sample field: {first_field.get('field_name', 'Unknown')} - {first_field.get('field_description', 'Unknown')}")

                elif domain == 'Tables':
                    logger.info(f"  Code: {data.get('code', 'Unknown')}")
                    logger.info(f"  Name: '{data.get('name', 'Unknown')}'")
                    logger.info(f"  Chapter: {data.get('chapter', 'Unknown')}")
                    values = data.get('values', [])
                    logger.info(f"  Values: {len(values)}")
                    if values:
                        first_value = values[0]
                        logger.info(f"  Sample value: {first_value.get('value', 'Unknown')} = {first_value.get('description', 'Unknown')}")

                # Check for fixes
                fixes_valid = True

                # Validate code is not "unknown"
                if data.get('code', 'unknown') == 'unknown':
                    logger.warning(f"  ❌ Code still showing as 'unknown'")
                    fixes_valid = False
                else:
                    logger.info(f"  ✅ Code properly extracted")

                # Validate name is not generic page title
                name = data.get('name', '')
                if 'HL7-Definition V2' in name or name == 'Unknown':
                    logger.warning(f"  ❌ Name still generic: '{name}'")
                    fixes_valid = False
                else:
                    logger.info(f"  ✅ Name properly extracted: '{name}'")

                # Domain-specific validations
                if domain == 'DataTypes':
                    if 'components' in data:
                        logger.warning(f"  ❌ Still using 'components' instead of 'fields'")
                        fixes_valid = False
                    else:
                        logger.info(f"  ✅ Using 'fields' consistently")

                if domain == 'Tables':
                    if data.get('chapter', 'Unknown') == 'Unknown':
                        logger.warning(f"  ❌ Chapter still 'Unknown'")
                        fixes_valid = False
                    else:
                        logger.info(f"  ✅ Chapter extracted: {data.get('chapter')}")

                results[domain] = 'SUCCESS' if fixes_valid else 'PARTIAL'

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
    logger.info("DOMAIN FIXES TEST RESULTS SUMMARY")
    logger.info(f"{'='*60}")

    for domain, result in results.items():
        if result == 'SUCCESS':
            status_icon = "✅"
        elif result == 'PARTIAL':
            status_icon = "⚠️ "
        else:
            status_icon = "❌"
        logger.info(f"{status_icon} {domain}: {result}")

    success_count = sum(1 for result in results.values() if result == 'SUCCESS')
    total_count = len(results)

    if success_count == total_count:
        logger.info(f"\n🎉 ALL {total_count} DOMAIN FIXES VALIDATED!")
        return True
    else:
        logger.info(f"\n⚠️  {success_count}/{total_count} domain fixes validated")
        return False

if __name__ == "__main__":
    # Configure logging
    logger.remove()
    logger.add(sys.stdout, format="<green>{time:HH:mm:ss}</green> | <level>{level: <8}</level> | {message}")

    success = test_domain_fixes()

    if success:
        logger.info("\n✨ Domain fixes validation PASSED!")
    else:
        logger.error("\n💥 Domain fixes validation NEEDS WORK")