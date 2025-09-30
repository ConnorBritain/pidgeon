# Enhanced Caristix HL7 v2.3 Trigger Events Scraper

## Overview
This enhanced scraper extracts all HL7 v2.3 trigger events from the Caristix HL7-definition site, including complete segment layouts with nested groups. It provides the ground truth data needed for trigger event templates in the Pidgeon platform.

## Key Improvements
- ✅ **Group Expansion**: Automatically expands collapsible groups (PROCEDURE, INSURANCE, etc.)
- ✅ **Retry Logic**: Tenacity-based retry with exponential backoff for resilience
- ✅ **Better Parsing**: Improved chapter and metadata extraction
- ✅ **Hierarchical Capture**: Preserves group nesting with level and path tracking
- ✅ **Resume Support**: Can resume interrupted scraping sessions
- ✅ **Robust Logging**: Detailed logging with loguru for debugging
- ✅ **Test Mode**: Limit option for testing with subset of events

## Installation

```bash
# Install dependencies
pip install -r requirements.txt

# Or install individually
pip install selenium webdriver-manager pandas tenacity loguru
```

## Usage

### Basic Run (Test Mode)
```bash
# Run with test output directory and limit to 5 events
python caristix-scraper-enhanced.py --limit 5
```

### Full Production Run
```bash
# Scrape all events to default directory
python caristix-scraper-enhanced.py

# Or specify custom destination
python caristix-scraper-enhanced.py --dest "C:\path\to\output"
```

### Resume Interrupted Session
```bash
# Skip already processed events
python caristix-scraper-enhanced.py --resume
```

### Debug Mode (Visible Browser)
```bash
# Run with visible browser for debugging
python caristix-scraper-enhanced.py --headful --debug --limit 3
```

### Command Line Options
- `--dest PATH`: Output directory (default: `scripts\caristix\trigger-events\test_run`)
- `--resume`: Skip already scraped events
- `--headful`: Show browser window (debugging)
- `--limit N`: Process only first N events (testing)
- `--delay-ms MS`: Base delay between actions in milliseconds (default: 500)
- `--debug`: Enable debug logging

## Output Structure

```
trigger-events/test_run/
├── trigger_events_v23_master.csv      # All segments flattened
├── trigger_events_v23_master.json     # Hierarchical structure
├── scrape.log                          # Detailed execution log
└── events/
    ├── v23_ADT_A01.json               # Individual event JSON
    ├── v23_ADT_A01.csv                # Individual event CSV
    ├── v23_ADT_A02.json
    ├── v23_ADT_A02.csv
    └── ...
```

## Data Schema

### Master JSON Structure
```json
{
  "version": "2.3",
  "scraped_date": "2024-01-15 14:30:00",
  "event_count": 165,
  "total_segments": 2450,
  "events": [
    {
      "code": "ADT_A01",
      "title": "Admit/visit notification",
      "chapter": "Patient Administration",
      "description": "...",
      "detail_url": "https://...",
      "segment_count": 15,
      "segments": [
        {
          "segment_code": "MSH",
          "segment_desc": "Message Header",
          "optionality": "R",
          "repeatability": "-",
          "is_group": false,
          "group_path": [],
          "level": 0,
          "order_index": 0
        },
        {
          "segment_code": "PROCEDURE",
          "segment_desc": "Procedure Group",
          "optionality": "O",
          "repeatability": "∞",
          "is_group": true,
          "group_path": [],
          "level": 0,
          "order_index": 5
        },
        {
          "segment_code": "PR1",
          "segment_desc": "Procedures",
          "optionality": "R",
          "repeatability": "-",
          "is_group": false,
          "group_path": ["PROCEDURE"],
          "level": 1,
          "order_index": 6
        }
      ]
    }
  ]
}
```

### Master CSV Columns
- `event_code`: Trigger event code (e.g., ADT_A01)
- `event_title`: Human-readable event name
- `chapter`: HL7 chapter category
- `segment_code`: Segment identifier
- `segment_desc`: Segment description
- `optionality`: R (Required), O (Optional), C (Conditional)
- `repeatability`: "-" (single) or "∞" (multiple)
- `is_group`: Boolean indicating group row
- `group_path`: Pipe-delimited nesting path
- `level`: Nesting depth (0=root)
- `order_index`: Sequential position
- `detail_url`: Source URL

## Features

### Automatic Group Expansion
The scraper intelligently detects and expands collapsible group rows:
- Detects expandable indicators (aria-expanded, toggle buttons)
- Clicks to expand nested segments
- Preserves hierarchy in group_path field
- Tracks nesting level for proper structure

### Resilience & Error Handling
- Retry failed requests up to 3 times with exponential backoff
- Handles timeouts gracefully
- Continues on individual page failures
- Comprehensive error logging

### Performance Optimizations
- Headless Chrome by default
- Image loading disabled for speed
- Smart scrolling for lazy-loaded content
- Polite delays between requests

## Validation

After scraping, validate the data:

```python
import json
import pandas as pd

# Check master JSON
with open("trigger-events/test_run/trigger_events_v23_master.json", "r") as f:
    data = json.load(f)
    print(f"Events: {data['event_count']}")
    print(f"Total segments: {data['total_segments']}")

    # Check for groups
    groups = sum(1 for e in data['events']
                 for s in e['segments'] if s.get('is_group'))
    print(f"Group rows: {groups}")

# Check master CSV
df = pd.read_csv("trigger-events/test_run/trigger_events_v23_master.csv")
print(f"\nCSV rows: {len(df)}")
print(f"Unique events: {df['event_code'].nunique()}")
print(f"Events with groups: {df[df['group_path'] != '']['event_code'].nunique()}")
```

## Troubleshooting

### Chrome Driver Issues
- The script uses webdriver-manager to auto-download ChromeDriver
- Ensure Chrome browser is installed and up-to-date
- For manual driver: Download from https://chromedriver.chromium.org/

### Timeout Errors
- Increase wait times in the code
- Check internet connection stability
- Use --debug flag to see detailed errors

### Missing Groups
- Run with --headful to watch expansion behavior
- Some events may not have groups (normal)
- Check scrape.log for expansion counts

### Cookie Banner Blocking
- The scraper attempts to close cookie banners automatically
- If still blocking, run with --headful and close manually

## Integration with Pidgeon

The scraped data can be transformed into Pidgeon trigger event templates:

```python
# Transform to Pidgeon template format
def to_pidgeon_template(event_data):
    return {
        "triggerEvent": event_data["code"],
        "name": event_data["title"],
        "description": event_data["description"],
        "chapter": event_data["chapter"],
        "standard": "hl7v23",
        "version": "2.3",
        "messageStructure": [
            {
                "segment": seg["segment_code"],
                "optionality": seg["optionality"],
                "repeatability": seg["repeatability"],
                "groupPath": seg["group_path"]
            }
            for seg in event_data["segments"]
        ]
    }
```

## Legal Notice
This scraper is for educational and development purposes. Please respect Caristix's terms of service and use polite scraping practices (delays, no parallelization). Consider reaching out to Caristix for official data access if available.