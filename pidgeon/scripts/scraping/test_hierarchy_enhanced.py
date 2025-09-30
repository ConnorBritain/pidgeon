#!/usr/bin/env python3
"""
Test the enhanced hierarchy detection with ADR_A19
"""

import json
import sys
from pathlib import Path
import importlib.util

# Load the scraper module
spec = importlib.util.spec_from_file_location("scraper", "caristix-triggerevent-scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def test_hierarchy_enhanced():
    """Test enhanced hierarchy detection with ADR_A19"""

    # Create scraper instance
    dest_path = Path("./trigger-events/test_hierarchy_enhanced")
    dest_path.mkdir(parents=True, exist_ok=True)

    scraper = CaristixScraper(dest_path, headless=True, delay_ms=1000)

    try:
        driver = scraper.setup_driver()
        if not driver:
            print("ERROR: Failed to setup Chrome driver")
            return

        scraper.driver = driver

        # Test specific URL
        adr_url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents/ADR_A19"

        print(f"Testing enhanced hierarchy detection with ADR_A19...")
        event_data = scraper.parse_event_page(adr_url)

        if event_data:
            print(f"[SUCCESS] Successfully parsed ADR_A19 with hierarchy")
            print(f"[DATA] Event data:")
            print(f"   - Code: {event_data['code']}")
            print(f"   - Title: {event_data['title']}")
            print(f"   - Chapter: {event_data['chapter']}")
            print(f"   - Segments: {len(event_data['segments'])}")

            print(f"\n[HIERARCHY] Segment hierarchy details:")
            for i, seg in enumerate(event_data['segments']):
                indent = "  " * seg['level']
                group_info = f" (in {seg['group_path']})" if seg['group_path'] else ""
                group_marker = " [GROUP]" if seg['is_group'] else ""
                print(f"   {i+1:2d}. {indent}{seg['segment_code']:<15} - Level {seg['level']}{group_info}{group_marker}")

            # Save the results
            with open(dest_path / "adr_a19_hierarchy.json", "w", encoding="utf-8") as f:
                json.dump(event_data, f, ensure_ascii=False, indent=2)

            print(f"\n[SAVE] Results saved to: {dest_path / 'adr_a19_hierarchy.json'}")

            # Analyze hierarchy quality
            levels_found = set(seg['level'] for seg in event_data['segments'])
            groups_found = [seg for seg in event_data['segments'] if seg['is_group']]
            segments_in_groups = [seg for seg in event_data['segments'] if seg['group_path']]

            print(f"\n[HIERARCHY ANALYSIS]")
            print(f"Levels found: {sorted(levels_found)}")
            print(f"Groups found: {len(groups_found)}")
            print(f"Segments with group paths: {len(segments_in_groups)}")

            print(f"\n[GROUPS]")
            for group in groups_found:
                print(f"  - {group['segment_code']} (Level {group['level']}, in {group['group_path']})")

            # Validate expected hierarchy for ADR_A19
            expected_structure = {
                'QUERY RESPONSE': {'level': 0, 'children': ['EVN', 'PID', 'PD1', 'NK1', 'PV1', 'PV2', 'DB1', 'OBX', 'AL1', 'DG1', 'DRG', 'PROCEDURE', 'GT1', 'INSURANCE', 'ACC', 'UB1', 'UB2']},
                'PROCEDURE': {'level': 1, 'parent': 'QUERY RESPONSE', 'children': ['PR1', 'ROL']},
                'INSURANCE': {'level': 1, 'parent': 'QUERY RESPONSE', 'children': ['IN1', 'IN2', 'IN3']}
            }

            print(f"\n[VALIDATION]")
            validation_passed = True

            # Check QUERY RESPONSE group
            query_response = next((seg for seg in event_data['segments'] if seg['segment_code'] == 'QUERY RESPONSE'), None)
            if query_response and query_response['level'] == 0:
                print(f"✓ QUERY RESPONSE found at level 0")
            else:
                print(f"✗ QUERY RESPONSE not found at level 0")
                validation_passed = False

            # Check segments inside QUERY RESPONSE
            query_segments = [seg for seg in event_data['segments'] if 'QUERY RESPONSE' in seg.get('group_path', [])]
            if len(query_segments) > 10:
                print(f"✓ Found {len(query_segments)} segments inside QUERY RESPONSE")
            else:
                print(f"✗ Expected more segments inside QUERY RESPONSE, found {len(query_segments)}")
                validation_passed = False

            # Check PROCEDURE group
            procedure_group = next((seg for seg in event_data['segments'] if seg['segment_code'] == 'PROCEDURE'), None)
            if procedure_group and procedure_group['level'] == 1:
                print(f"✓ PROCEDURE found at level 1")
            else:
                print(f"✗ PROCEDURE not found at level 1")
                validation_passed = False

            if validation_passed:
                print(f"\n[HIERARCHY SUCCESS] ✅ Hierarchy detection working correctly!")
            else:
                print(f"\n[HIERARCHY ISSUES] ❌ Some hierarchy validation failed")

        else:
            print(f"[FAIL] Failed to parse ADR_A19")

    except Exception as e:
        print(f"[ERROR] Error testing hierarchy: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

if __name__ == "__main__":
    test_hierarchy_enhanced()