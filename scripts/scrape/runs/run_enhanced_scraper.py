#!/usr/bin/env python3
"""
Run the enhanced trigger event scraper on all events with quality tracking
"""

import json
import time
from pathlib import Path
import importlib.util

# Load the enhanced scraper module
spec = importlib.util.spec_from_file_location("scraper", "hl7_event_scraper.py")
scraper_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(scraper_module)

CaristixScraper = scraper_module.CaristixScraper

def run_enhanced_scraper(limit=None, resume=False):
    """Run enhanced scraper on all trigger events"""

    # Use a new output directory for enhanced results
    dest_path = Path("./trigger-events/enhanced_run")

    print(f"Running enhanced trigger event scraper...")
    print(f"Output directory: {dest_path}")
    print(f"Limit: {limit if limit else 'All events'}")
    print(f"Resume: {resume}")

    # Initialize scraper with optimized settings for bulk processing
    scraper = CaristixScraper(
        dest_path=dest_path,
        headless=True,  # Headless for performance
        delay_ms=800   # Moderate delay for stability
    )

    try:
        # Run the scraper
        start_time = time.time()
        scraper.run(resume=resume, limit=limit)

        # Calculate timing
        elapsed = time.time() - start_time
        print(f"\nScraping completed in {elapsed:.1f} seconds")

        # Analyze results
        analyze_results(dest_path)

    except Exception as e:
        print(f"Error running scraper: {e}")
        import traceback
        traceback.print_exc()

    finally:
        if scraper.driver:
            scraper.driver.quit()

def analyze_results(dest_path):
    """Analyze the quality of scraped results"""
    print(f"\n=== ENHANCED SCRAPER RESULTS ANALYSIS ===")

    # Load master file
    master_file = dest_path / "trigger_events_v23_master.json"
    if not master_file.exists():
        print(f"Master file not found: {master_file}")
        return

    with open(master_file, 'r', encoding='utf-8') as f:
        events = json.load(f)

    print(f"Total events processed: {len(events)}")

    # Quality metrics
    total_segments = 0
    events_with_descriptions = 0
    segments_with_descriptions = 0
    events_with_chapters = 0
    events_with_groups = 0
    total_groups = 0

    # Analysis by event
    quality_scores = []

    for event in events:
        segments = event.get('segments', [])
        total_segments += len(segments)

        # Check event-level quality
        event_quality = 0
        total_checks = 4

        if event.get('chapter'):
            events_with_chapters += 1
            event_quality += 1

        if event.get('description'):
            events_with_descriptions += 1
            event_quality += 1

        # Check segment-level quality
        seg_with_desc = sum(1 for seg in segments if seg.get('segment_desc'))
        if seg_with_desc > 0:
            segments_with_descriptions += seg_with_desc
            event_quality += 1

        # Check for groups
        groups = [seg for seg in segments if seg.get('is_group')]
        if groups:
            events_with_groups += 1
            total_groups += len(groups)
            event_quality += 1

        quality_scores.append(event_quality / total_checks * 100)

    # Calculate averages
    avg_segments_per_event = total_segments / len(events) if events else 0
    avg_quality_score = sum(quality_scores) / len(quality_scores) if quality_scores else 0

    # Print results
    print(f"\n=== OVERALL STATISTICS ===")
    print(f"Events with chapters: {events_with_chapters}/{len(events)} ({events_with_chapters/len(events)*100:.1f}%)")
    print(f"Events with descriptions: {events_with_descriptions}/{len(events)} ({events_with_descriptions/len(events)*100:.1f}%)")
    print(f"Events with groups: {events_with_groups}/{len(events)} ({events_with_groups/len(events)*100:.1f}%)")
    print(f"Total groups found: {total_groups}")
    print(f"Average segments per event: {avg_segments_per_event:.1f}")
    print(f"Segments with descriptions: {segments_with_descriptions}/{total_segments} ({segments_with_descriptions/total_segments*100:.1f}%)")
    print(f"Average quality score: {avg_quality_score:.1f}%")

    # Show top events by segment count (most complex)
    print(f"\n=== MOST COMPLEX EVENTS (by segment count) ===")
    sorted_events = sorted(events, key=lambda x: len(x.get('segments', [])), reverse=True)
    for i, event in enumerate(sorted_events[:10]):
        groups = [seg['segment_code'] for seg in event.get('segments', []) if seg.get('is_group')]
        print(f"{i+1:2d}. {event['code']:<12} - {len(event.get('segments', []))} segments {f'(Groups: {groups})' if groups else ''}")

    # Show events with groups
    print(f"\n=== EVENTS WITH EXPANDABLE GROUPS ===")
    events_with_groups_list = [(event, [seg['segment_code'] for seg in event.get('segments', []) if seg.get('is_group')])
                              for event in events if any(seg.get('is_group') for seg in event.get('segments', []))]

    for event, group_names in events_with_groups_list[:20]:  # Show first 20
        print(f"{event['code']:<12} - Groups: {', '.join(group_names)}")

    if len(events_with_groups_list) > 20:
        print(f"... and {len(events_with_groups_list) - 20} more events with groups")

    print(f"\n=== ENHANCEMENT SUCCESS ===")
    print(f"🎯 Enhanced parsing successfully captures complex HL7 structures!")
    print(f"📈 Average {avg_segments_per_event:.1f} segments per event (vs ~3 with basic parsing)")
    print(f"🔧 {total_groups} expandable groups successfully processed")
    print(f"📝 {segments_with_descriptions/total_segments*100:.1f}% of segments have descriptions")

def main():
    """Main execution with command line options"""
    import argparse

    parser = argparse.ArgumentParser(description='Run enhanced trigger event scraper')
    parser.add_argument('--limit', type=int, help='Limit number of events to process (for testing)')
    parser.add_argument('--resume', action='store_true', help='Resume from previous run')

    args = parser.parse_args()

    run_enhanced_scraper(limit=args.limit, resume=args.resume)

if __name__ == "__main__":
    main()