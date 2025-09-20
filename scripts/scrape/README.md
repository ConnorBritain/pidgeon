# HL7 v2.3 Caristix Web Scraper Suite

## 🎯 Overview

This comprehensive scraping system extracts complete HL7 v2.3 standard definitions from the Caristix documentation website. The system is designed for overnight bulk collection of the entire HL7 v2.3 dataset, organized into four domain-specific scrapers with timestamped run tracking.

## 📁 Directory Structure

```
/scripts/scrape/
├── README.md                           # This file
├── requirements.txt                    # Python dependencies
├── scrapers/                          # Core scraper modules
│   ├── hl7_component_scraper.py       # Base scraper (Tables, DataTypes, Segments)
│   └── hl7_event_scraper.py          # Enhanced scraper (TriggerEvents)
├── runs/                              # Executable runner scripts
│   ├── run_tables_scraper.py          # Tables domain runner
│   ├── run_datatypes_scraper.py       # DataTypes domain runner
│   ├── run_segments_scraper.py        # Segments domain runner
│   ├── run_trigger_events_scraper.py  # TriggerEvents domain runner
│   ├── run_all_scrapers.py           # Master orchestrator
│   └── run_enhanced_scraper.py       # Legacy runner (deprecated)
└── outputs/                           # Timestamped run outputs
    ├── tables/
    │   ├── 20250918_021800_test/      # Limited test run
    │   └── 20250918_143000_prod/      # Full production run
    ├── data_types/
    ├── segments/
    └── trigger_events/
```

## 🚀 What We've Accomplished

### ✅ Core Infrastructure
- **Specialized Architecture**: Separated base scraper (simple components) from enhanced scraper (complex hierarchical data)
- **Domain-Specific Runners**: Four dedicated scripts for each HL7 v2.3 domain
- **Timestamped Outputs**: Dynamic directory generation with test/production mode tracking
- **Error Resilience**: Comprehensive retry logic and failure handling
- **Path Management**: Clean import structure with relative path resolution

### ✅ Scraper Capabilities
- **Tables Scraper**: HL7 lookup tables with code/value mappings
- **DataTypes Scraper**: Primitive and composite data type definitions
- **Segments Scraper**: Message segment structures with field definitions
- **TriggerEvents Scraper**: Complex message hierarchies with chapter organization

### ✅ Advanced Features
- **Dynamic DOM Parsing**: Angular tree table expansion and content extraction
- **Metadata Extraction**: Names, descriptions, chapters, versions, field details
- **Progress Tracking**: Real-time console output with completion statistics
- **Output Formats**: Individual JSON files + consolidated master files
- **Production Ready**: Headless browser operation with configurable delays

## 🎯 Current Objective: Complete Dataset Collection

### **Goal**: Overnight parallel execution to collect the entire HL7 v2.3 standard

**Target Data**:
- **~475 Tables** (lookup codes and values)
- **~65 DataTypes** (primitive and composite structures)
- **~120 Segments** (message segment definitions)
- **~275 TriggerEvents** (complete message hierarchies with segment mappings)

**Total Expected**: ~935 HL7 v2.3 standard components with full metadata

## 🏃‍♂️ Quick Start

### Prerequisites
```bash
# Ensure Python 3.8+ is installed
python --version

# Install dependencies (if not already installed)
cd /path/to/scripts/scrape
pip install -r requirements.txt
```

### Limited Testing (3 items per domain)
```bash
cd runs/

# Test each domain with limit (30 seconds each)
python run_tables_scraper.py --limit 3
python run_datatypes_scraper.py --limit 3
python run_segments_scraper.py --limit 3
python run_trigger_events_scraper.py --limit 3
```

### Production Runs (Full Dataset)

#### Option 1: Sequential Execution
```bash
# Run domains one at a time (safer, slower)
python run_all_scrapers.py
```

#### Option 2: Parallel Execution (Recommended for Overnight)
```bash
# Run all domains in parallel background processes
python run_tables_scraper.py --prod &
python run_datatypes_scraper.py --prod &
python run_segments_scraper.py --prod &
python run_trigger_events_scraper.py --prod &

# Monitor progress (optional)
jobs
```

## 📊 Output Structure

Each run creates a timestamped directory with:

```
outputs/{domain}/{YYYYMMDD_HHMMSS}_{test|prod}/
├── v23_*.json                    # Individual component files
├── {domain}_v23_master.json      # Consolidated master file
└── (trigger_events only)
    └── events/                   # Subdirectory for trigger event files
```

**Example Output**:
```
outputs/tables/20250918_143022_prod/
├── v23_0001.json                 # Table 0001 definition
├── v23_0002.json                 # Table 0002 definition
├── ...
└── tables_v23_master.json       # All tables in one file
```

## ⚙️ Configuration Options

### Command Line Arguments
```bash
# Limit items (for testing)
--limit N                         # Process only N items

# Production mode
--prod                           # Production run (default: test mode)

# Resume functionality (trigger events only)
--resume                         # Resume from previous interrupted run
```

### Timing Expectations
- **Tables**: ~30 minutes (simple structure)
- **DataTypes**: ~45 minutes (moderate complexity)
- **Segments**: ~60 minutes (field definitions)
- **TriggerEvents**: ~180 minutes (complex hierarchies)

**Total Estimated Time**: ~5 hours for complete dataset

## 🔧 Technical Details

### Browser Automation
- **Selenium WebDriver**: Chrome/Chromium automation
- **Headless Mode**: No GUI required for server operation
- **WebDriver Manager**: Automatic driver version management
- **Retry Logic**: Tenacity-based failure recovery

### Data Processing
- **JSON Output**: Structured data for database integration
- **Pandas Processing**: CSV/JSON conversion capabilities
- **Path Management**: Robust file system operations
- **Logging**: Loguru-enhanced debug output

### Error Handling
- **Network Resilience**: Automatic retry on connection failures
- **DOM Parsing**: Graceful handling of dynamic content loading
- **Timeout Management**: Configurable wait periods
- **Progress Recovery**: Resume capability for long-running scrapes

## 🚦 Status Monitoring

### Real-Time Progress
Each scraper provides:
- **Start/End Timestamps**
- **Items Processed Count**
- **Success/Failure Status**
- **Output Directory Location**
- **Performance Statistics**

### Completion Verification
```bash
# Check run completion
ls -la outputs/*/20250918_*_prod/

# Verify master files exist
find outputs/ -name "*_master.json" -exec wc -l {} \;

# Sample data structure
head outputs/tables/20250918_*_prod/tables_v23_master.json
```

## 🎯 Next Steps

1. **✅ Fix Complete**: Trigger events scraper parameter issue resolved
2. **🚀 Ready for Production**: All 4 scrapers tested and operational
3. **🌙 Overnight Execution**: Parallel background runs for complete dataset
4. **📚 Database Integration**: JSON output ready for SQLite import
5. **🔄 Validation**: Compare scraped data against known HL7 v2.3 standards

## 🛠️ Troubleshooting

### Common Issues
- **Import Errors**: Ensure all requirements.txt dependencies are installed
- **Path Issues**: Run commands from the `/runs` directory
- **Browser Errors**: Check Chrome/Chromium installation and webdriver-manager
- **Permission Errors**: Ensure write access to `/outputs` directory

### Debug Mode
```bash
# Add verbose logging (modify scraper files)
logger.setLevel("DEBUG")

# Monitor browser activity (remove headless)
headless=False
```

### Recovery Procedures
```bash
# Clean failed runs
rm -rf outputs/*/20250918_*_failed/

# Restart specific domain
python run_{domain}_scraper.py --prod

# Resume trigger events (if interrupted)
python run_trigger_events_scraper.py --prod --resume
```

---

**Ready for overnight production run! 🚀**

*Last Updated: September 18, 2025*