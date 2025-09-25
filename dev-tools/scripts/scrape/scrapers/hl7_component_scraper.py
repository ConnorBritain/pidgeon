#!/usr/bin/env python3
"""
Base Caristix Scraper Framework
Abstracted scraper for all HL7 v2.3 domains: Trigger Events, Segments, Data Types, Tables
"""

import json
import time
from abc import ABC, abstractmethod
from pathlib import Path
from typing import Dict, List, Optional, Any
import sqlite3
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.wait import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
from selenium.common.exceptions import NoSuchElementException
from webdriver_manager.chrome import ChromeDriverManager
from loguru import logger


class BaseCaristixScraper(ABC):
    """
    Base class for scraping all Caristix HL7 domains

    Supports:
    - Trigger Events: Message structure definitions
    - Segments: Field definitions and constraints
    - Data Types: Primitive and composite type definitions
    - Tables: Code value lookups
    """

    def __init__(self, domain: str, dest_path: Path, headless: bool = True, delay_ms: int = 800):
        """
        Initialize base scraper

        Args:
            domain: One of 'TriggerEvents', 'Segments', 'DataTypes', 'Tables'
            dest_path: Output directory for results
            headless: Run browser in headless mode
            delay_ms: Delay between operations
        """
        self.domain = domain
        self.dest_path = Path(dest_path)
        self.headless = headless
        self.delay_ms = delay_ms
        self.base_url = f"https://hl7-definition.caristix.com/v2/HL7v2.3/{domain}"

        # Create output directory
        self.dest_path.mkdir(parents=True, exist_ok=True)

        # Initialize driver
        self.driver = None
        self.by = By

        logger.info(f"Initializing {domain} scraper with destination: {dest_path}")

    def setup_driver(self) -> Optional[webdriver.Chrome]:
        """Setup Chrome WebDriver with optimized settings"""
        try:
            logger.info(f"Starting Chrome driver (headless={self.headless})")

            options = Options()
            if self.headless:
                options.add_argument("--headless")

            options.add_argument("--no-sandbox")
            options.add_argument("--disable-dev-shm-usage")
            options.add_argument("--disable-gpu")
            options.add_argument("--window-size=1920,1080")
            options.add_argument("--disable-blink-features=AutomationControlled")
            options.add_experimental_option("excludeSwitches", ["enable-automation"])
            options.add_experimental_option('useAutomationExtension', False)

            try:
                driver = webdriver.Chrome(ChromeDriverManager().install(), options=options)
            except Exception as e:
                logger.warning(f"webdriver-manager failed: {e}, trying system driver")
                driver = webdriver.Chrome(options=options)

            driver.execute_script("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})")
            return driver

        except Exception as e:
            logger.error(f"Failed to setup Chrome driver: {e}")
            return None

    def random_delay(self, factor: float = 1.0):
        """Add random delay to avoid detection"""
        import random
        delay = (self.delay_ms / 1000) * factor * random.uniform(0.8, 1.2)
        time.sleep(delay)

    def _collect_urls_with_virtual_scroll(self, virtual_scroll, link_selector: str, url_pattern: str) -> List[str]:
        """Collect URLs using virtual scrolling (based on trigger events scraper logic)"""
        # Get virtual scroll dimensions
        scroll_height = self.driver.execute_script("return arguments[0].scrollHeight", virtual_scroll)
        client_height = self.driver.execute_script("return arguments[0].clientHeight", virtual_scroll)

        logger.info(f"Virtual scroll: height={scroll_height}, viewport={client_height}")

        # Collect URLs while scrolling through virtual list
        all_urls = set()
        scroll_position = 0
        scroll_step = client_height // 2  # Scroll by half viewport to ensure overlap
        max_position = scroll_height - client_height
        no_new_urls_count = 0

        logger.info("Scrolling through virtual list to collect all items...")

        while scroll_position <= max_position and no_new_urls_count < 5:
            # Scroll to position
            self.driver.execute_script(f"arguments[0].scrollTop = {scroll_position}", virtual_scroll)
            time.sleep(0.5)  # Allow virtual scrolling to render

            # Collect URLs currently visible
            current_links = self.driver.find_elements(By.CSS_SELECTOR, link_selector)
            current_urls = {link.get_attribute("href") for link in current_links}

            # Filter valid URLs
            valid_urls = {url for url in current_urls
                         if url and url_pattern in url}

            previous_count = len(all_urls)
            all_urls.update(valid_urls)

            if len(all_urls) == previous_count:
                no_new_urls_count += 1
            else:
                no_new_urls_count = 0

            logger.info(f"Scroll position {scroll_position}/{max_position}: {len(all_urls)} total items")

            scroll_position += scroll_step

        # Final scroll to bottom to catch any remaining items
        self.driver.execute_script("arguments[0].scrollTop = arguments[0].scrollHeight", virtual_scroll)
        time.sleep(1)

        final_links = self.driver.find_elements(By.CSS_SELECTOR, link_selector)
        final_urls = {link.get_attribute("href") for link in final_links}
        final_valid = {url for url in final_urls if url and url_pattern in url}
        all_urls.update(final_valid)

        urls = sorted(list(all_urls))
        logger.info(f"Found {len(urls)} unique URLs with virtual scrolling")
        return urls

    def _collect_urls_standard(self, link_selector: str, url_pattern: str) -> List[str]:
        """Fallback URL collection for non-virtual scroll pages"""
        logger.info("Using standard scrolling method")

        # Traditional document scrolling
        last_height = 0
        scroll_attempts = 0
        max_scrolls = 20

        while scroll_attempts < max_scrolls:
            self.driver.execute_script("window.scrollTo(0, document.body.scrollHeight)")
            time.sleep(1)

            new_height = self.driver.execute_script("return document.body.scrollHeight")
            if new_height == last_height:
                break

            last_height = new_height
            scroll_attempts += 1

        # Collect all links after scrolling
        links = self.driver.find_elements(By.CSS_SELECTOR, link_selector)
        urls = []
        for link in links:
            href = link.get_attribute("href")
            if href and url_pattern in href and href not in urls:
                urls.append(href)

        logger.info(f"Found {len(urls)} URLs with standard scrolling")
        return urls

    def collect_item_urls(self) -> List[str]:
        """Collect all item URLs for the domain"""
        try:
            logger.info(f"Loading main page: {self.base_url}")
            self.driver.get(self.base_url)
            self.random_delay()

            return self._collect_domain_specific_urls()

        except Exception as e:
            logger.error(f"Failed to collect URLs: {e}")
            return []

    @abstractmethod
    def _collect_domain_specific_urls(self) -> List[str]:
        """Domain-specific URL collection logic"""
        pass

    def parse_item_page(self, url: str) -> Optional[Dict[str, Any]]:
        """Parse a single item page"""
        try:
            item_code = url.rstrip('/').split('/')[-1]
            logger.info(f"Parsing {self.domain.lower()}: {item_code}")

            self.driver.get(url)
            self.random_delay()

            return self._parse_domain_specific_page(url, item_code)

        except Exception as e:
            logger.error(f"Failed to parse {url}: {e}")
            return None

    @abstractmethod
    def _parse_domain_specific_page(self, url: str, item_code: str) -> Optional[Dict[str, Any]]:
        """Domain-specific page parsing logic"""
        pass

    def save_item_data(self, item_data: Dict[str, Any]):
        """Save individual item data"""
        if not item_data:
            return

        item_code = item_data.get('code', 'unknown')

        # Save JSON
        json_file = self.dest_path / f"v23_{item_code}.json"
        with open(json_file, 'w', encoding='utf-8') as f:
            json.dump(item_data, f, ensure_ascii=False, indent=2)

        # Save CSV (if applicable)
        if hasattr(self, '_save_csv_data'):
            self._save_csv_data(item_data)

        logger.info(f"Saved {item_code}: {self._get_item_summary(item_data)}")

    @abstractmethod
    def _get_item_summary(self, item_data: Dict[str, Any]) -> str:
        """Get summary string for logging"""
        pass

    def create_master_files(self, all_items: List[Dict[str, Any]]):
        """Create master JSON and CSV files"""
        if not all_items:
            return

        # Master JSON
        master_data = {
            "domain": self.domain,
            "version": "2.3",
            "scraped_date": time.strftime("%Y-%m-%d %H:%M:%S"),
            "item_count": len(all_items),
            "total_elements": sum(self._count_elements(item) for item in all_items),
            "items": all_items
        }

        master_json = self.dest_path / f"{self.domain.lower()}_v23_master.json"
        with open(master_json, 'w', encoding='utf-8') as f:
            json.dump(master_data, f, ensure_ascii=False, indent=2)

        logger.info(f"Saved master JSON: {len(all_items)} total items")

        # Master CSV (if applicable)
        if hasattr(self, '_create_master_csv'):
            self._create_master_csv(all_items)

    @abstractmethod
    def _count_elements(self, item_data: Dict[str, Any]) -> int:
        """Count sub-elements for statistics"""
        pass

    def run(self, resume: bool = False, limit: Optional[int] = None):
        """Main execution flow"""
        try:
            # Setup driver
            self.driver = self.setup_driver()
            if not self.driver:
                logger.error("Failed to setup driver")
                return

            # Collect URLs
            urls = self.collect_item_urls()
            if not urls:
                logger.error("No URLs collected")
                return

            logger.info(f"Collected {len(urls)} {self.domain.lower()} URLs")

            if limit:
                urls = urls[:limit]
                logger.info(f"Limited to {limit} items for testing")

            # Process each item
            all_items = []

            for i, url in enumerate(urls, 1):
                try:
                    logger.info(f"Processing {i}/{len(urls)}: {url.split('/')[-1]}")

                    item_data = self.parse_item_page(url)
                    if item_data:
                        self.save_item_data(item_data)
                        all_items.append(item_data)

                    self.random_delay()

                except Exception as e:
                    logger.error(f"Failed to process {url}: {e}")
                    continue

            # Create master files
            logger.info("Creating master files...")
            self.create_master_files(all_items)

            logger.info(f"Scraping complete: {len(all_items)} items, {sum(self._count_elements(item) for item in all_items)} total elements")

        except Exception as e:
            logger.error(f"Error during scraping: {e}")

        finally:
            if self.driver:
                self.driver.quit()
                logger.info("Driver closed")




class SegmentsScraper(BaseCaristixScraper):
    """Scraper for HL7 Segment definitions"""

    def __init__(self, dest_path: Path, **kwargs):
        super().__init__("Segments", dest_path, **kwargs)

    def _collect_domain_specific_urls(self) -> List[str]:
        """Collect segment URLs from main segments page with virtual scrolling support"""
        try:
            # Wait for initial links to load
            WebDriverWait(self.driver, 15).until(
                EC.presence_of_all_elements_located((By.CSS_SELECTOR, "a[href*='/Segments/']"))
            )

            # Check for virtual scrolling viewport
            try:
                virtual_scroll = WebDriverWait(self.driver, 5).until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, ".cdk-virtual-scroll-viewport"))
                )
                logger.info("Found virtual scroll viewport for segments")
                return self._collect_urls_with_virtual_scroll(virtual_scroll, "a[href*='/Segments/']", "/Segments/")
            except TimeoutException:
                logger.info("No virtual scroll detected, using standard collection")
                return self._collect_urls_standard("a[href*='/Segments/']", "/Segments/")

        except Exception as e:
            logger.error(f"Failed to collect segment URLs: {e}")
            return []

    def _parse_domain_specific_page(self, url: str, segment_code: str) -> Optional[Dict[str, Any]]:
        """Parse segment definition page with field table"""
        try:
            # Wait for page to load
            WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.TAG_NAME, "h1"))
            )

            # Extract basic info from the main content header
            title = ""
            try:
                # Look for the main content header with the proper title format
                title_selectors = [
                    "h2.content-header-title-centered",
                    ".content-header h2",
                    "h2.cx-h2",
                    "main h2",
                    ".header-left h2",
                    "content-header h2",
                    "h1"  # Fallback to h1 if h2 not found
                ]

                for selector in title_selectors:
                    try:
                        title_element = self.driver.find_element(By.CSS_SELECTOR, selector)
                        if title_element and title_element.text.strip():
                            title = title_element.text.strip()
                            break
                    except NoSuchElementException:
                        continue

                if not title:
                    # Ultimate fallback to any h1 or h2
                    title_element = self.driver.find_element(By.TAG_NAME, "h1")
                    title = title_element.text.strip() if title_element else ""

            except Exception as e:
                logger.warning(f"Failed to extract title: {e}")
                title = "Unknown"

            # Extract description
            description = ""
            try:
                desc_elements = self.driver.find_elements(By.CSS_SELECTOR, "p, .description")
                if desc_elements:
                    description = desc_elements[0].text.strip()
            except Exception as e:
                logger.warning(f"Failed to extract description: {e}")

            # Extract chapter from detail page sidebar
            chapter = "Unknown"
            try:
                chapter_element = self.driver.find_element(By.CSS_SELECTOR, "app-segment-detail div.detail-text-container a[href*='chapters=']")
                if chapter_element:
                    chapter = chapter_element.text.strip()
                else:
                    chapter_element = self.driver.find_element(By.XPATH, "//app-segment-detail//div//a[contains(@href, 'chapters=')]")
                    if chapter_element:
                        chapter = chapter_element.text.strip()
            except Exception as e:
                logger.warning(f"Failed to extract chapter: {e}")

            # Parse fields table
            fields = self._parse_segment_fields()

            # Extract version and name from title pattern: "HL7 v2.3 - CODE - Descriptive Name"
            version = "Unknown"
            name = ""
            if " - " in title:
                parts = title.split(" - ")
                if len(parts) >= 3:
                    version = parts[0]  # "HL7 v2.3"
                    name = " - ".join(parts[2:])  # Everything after the code
                elif len(parts) == 2:
                    version = parts[0]  # "HL7 v2.3"
                    name = parts[1]  # Just the second part

            if not name:
                name = title  # Fallback to full title

            return {
                "code": segment_code,
                "name": name,
                "version": version,
                "chapter": chapter,
                "description": description,
                "fields": fields,
                "field_count": len(fields)
            }

        except Exception as e:
            logger.error(f"Failed to parse segment page {url}: {e}")
            return None

    def _parse_segment_fields(self) -> List[Dict[str, Any]]:
        """Parse segment fields table"""
        fields = []

        try:
            # Look for fields table - it should have columns: FIELD, LENGTH, DATA TYPE, OPTIONALITY, REPEATABILITY, TABLE
            table = self.driver.find_element(By.CSS_SELECTOR, "table, .table, [role='table']")
            rows = table.find_elements(By.CSS_SELECTOR, "tr")

            field_position = 1  # Track actual field position

            for i, row in enumerate(rows):
                try:
                    # Skip header rows
                    if row.find_elements(By.TAG_NAME, "th"):
                        continue

                    cells = row.find_elements(By.TAG_NAME, "td")
                    if len(cells) < 4:  # Need at least Field, Length, Data Type, Optionality
                        continue

                    # Parse field name and description
                    field_text = cells[0].text.strip()
                    field_name = ""
                    field_description = ""

                    if " - " in field_text:
                        # Split "ACC.1 - Accident Date/Time" into name and description
                        parts = field_text.split(" - ", 1)
                        field_name = parts[0].strip()
                        field_description = parts[1].strip()
                    else:
                        field_name = field_text
                        field_description = ""

                    field_info = {
                        "position": field_position,
                        "field_name": field_name,
                        "field_description": field_description,
                        "length": cells[1].text.strip(),
                        "data_type": cells[2].text.strip(),
                        "optionality": cells[3].text.strip(),
                        "repeatability": cells[4].text.strip() if len(cells) > 4 else "-",
                        "table": cells[5].text.strip() if len(cells) > 5 else ""
                    }

                    field_position += 1  # Increment actual field position

                    # Try to expand row for additional details if it's clickable
                    try:
                        if row.find_elements(By.CSS_SELECTOR, "[class*='expand'], [class*='toggle']"):
                            row.click()
                            self.random_delay(0.3)

                            # Look for expanded details
                            expanded_details = self._extract_field_details(row)
                            field_info.update(expanded_details)

                    except Exception as e:
                        logger.debug(f"No expandable details for field {field_info['field_name']}: {e}")

                    if field_info['field_name']:  # Only add if we have a field name
                        fields.append(field_info)

                except Exception as e:
                    logger.warning(f"Failed to parse field row {i}: {e}")
                    continue

            return fields

        except Exception as e:
            logger.error(f"Failed to parse segment fields: {e}")
            return []

    def _extract_field_details(self, row_element) -> Dict[str, Any]:
        """Extract additional field details from expanded row"""
        details = {}

        try:
            # Look for detailed information in the expanded area
            detail_elements = row_element.find_elements(By.CSS_SELECTOR, ".details, .expanded, .field-detail")

            for detail in detail_elements:
                text = detail.text.strip()

                # Extract specific details based on common patterns
                if "LENGTH" in text.upper():
                    details["length_detail"] = text
                elif "DATA TYPE" in text.upper():
                    details["data_type_detail"] = text
                elif "OPTIONALITY" in text.upper():
                    details["optionality_detail"] = text
                elif "REPEATABILITY" in text.upper():
                    details["repeatability_detail"] = text
                elif "TABLE" in text.upper():
                    details["table_detail"] = text

        except Exception as e:
            logger.debug(f"Failed to extract field details: {e}")

        return details

    def _get_item_summary(self, item_data: Dict[str, Any]) -> str:
        return f"{len(item_data.get('fields', []))} fields"

    def _count_elements(self, item_data: Dict[str, Any]) -> int:
        return len(item_data.get('fields', []))


class DataTypesScraper(BaseCaristixScraper):
    """Scraper for HL7 Data Type definitions"""

    def __init__(self, dest_path: Path, **kwargs):
        super().__init__("DataTypes", dest_path, **kwargs)

    def _collect_domain_specific_urls(self) -> List[str]:
        """Collect data type URLs from main data types page with virtual scrolling support"""
        try:
            # Wait for initial links to load
            WebDriverWait(self.driver, 15).until(
                EC.presence_of_all_elements_located((By.CSS_SELECTOR, "a[href*='/DataTypes/']"))
            )

            # Check for virtual scrolling viewport
            try:
                virtual_scroll = WebDriverWait(self.driver, 5).until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, ".cdk-virtual-scroll-viewport"))
                )
                logger.info("Found virtual scroll viewport for data types")
                return self._collect_urls_with_virtual_scroll(virtual_scroll, "a[href*='/DataTypes/']", "/DataTypes/")
            except TimeoutException:
                logger.info("No virtual scroll detected, using standard collection")
                return self._collect_urls_standard("a[href*='/DataTypes/']", "/DataTypes/")

        except Exception as e:
            logger.error(f"Failed to collect data type URLs: {e}")
            return []

    def _parse_domain_specific_page(self, url: str, type_code: str) -> Optional[Dict[str, Any]]:
        """Parse data type definition page with component structure"""
        try:
            # Wait for page to load
            WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.TAG_NAME, "h1"))
            )

            # Extract basic info from the main content header
            title = ""
            try:
                # Look for the main content header with the proper title format
                title_selectors = [
                    "h2.content-header-title-centered",
                    ".content-header h2",
                    "h2.cx-h2",
                    "main h2",
                    ".header-left h2",
                    "content-header h2",
                    "h1"  # Fallback to h1 if h2 not found
                ]

                for selector in title_selectors:
                    try:
                        title_element = self.driver.find_element(By.CSS_SELECTOR, selector)
                        if title_element and title_element.text.strip():
                            title = title_element.text.strip()
                            break
                    except NoSuchElementException:
                        continue

                if not title:
                    # Ultimate fallback to any h1 or h2
                    title_element = self.driver.find_element(By.TAG_NAME, "h1")
                    title = title_element.text.strip() if title_element else ""

            except Exception as e:
                logger.warning(f"Failed to extract title: {e}")
                title = "Unknown"

            # Extract version and name from title pattern: "HL7 v2.3 - CODE - Descriptive Name"
            version = "Unknown"
            name = ""
            if " - " in title:
                parts = title.split(" - ")
                if len(parts) >= 3:
                    version = parts[0]  # "HL7 v2.3"
                    name = " - ".join(parts[2:])  # Everything after the code
                elif len(parts) == 2:
                    version = parts[0]  # "HL7 v2.3"
                    name = parts[1]  # Just the second part

            if not name:
                name = title  # Fallback to full title

            # Extract description
            description = ""
            try:
                # Look for description paragraphs
                desc_elements = self.driver.find_elements(By.CSS_SELECTOR, "p")
                for desc in desc_elements:
                    text = desc.text.strip()
                    if text and len(text) > 20 and not text.startswith("Note:"):
                        description = text
                        break
            except Exception as e:
                logger.warning(f"Failed to extract description: {e}")

            # Check if this is a composite type by looking for fields table
            fields = self._parse_datatype_components()

            # Determine category
            category = "composite" if fields else "primitive"

            return {
                "code": type_code,
                "name": name,
                "version": version,
                "description": description,
                "category": category,
                "fields": fields,
                "field_count": len(fields)
            }

        except Exception as e:
            logger.error(f"Failed to parse data type page {url}: {e}")
            return None

    def _parse_datatype_components(self) -> List[Dict[str, Any]]:
        """Parse data type fields table (for composite types)"""
        fields = []

        try:
            # Look for fields table - similar structure to segments
            table = self.driver.find_element(By.CSS_SELECTOR, "table, .table, [role='table']")
            rows = table.find_elements(By.CSS_SELECTOR, "tr")

            field_position = 1  # Track actual field position

            for i, row in enumerate(rows):
                try:
                    # Skip header rows
                    if row.find_elements(By.TAG_NAME, "th"):
                        continue

                    cells = row.find_elements(By.TAG_NAME, "td")
                    if len(cells) < 4:  # Need at least Field, Length, Data Type, Optionality
                        continue

                    # Parse field name and description
                    field_text = cells[0].text.strip()
                    field_name = ""
                    field_description = ""

                    if " - " in field_text:
                        # Split "CCD.1 - Invocation Event" into name and description
                        parts = field_text.split(" - ", 1)
                        field_name = parts[0].strip()
                        field_description = parts[1].strip()
                    else:
                        field_name = field_text
                        field_description = ""

                    field_info = {
                        "position": field_position,
                        "field_name": field_name,
                        "field_description": field_description,
                        "length": cells[1].text.strip(),
                        "data_type": cells[2].text.strip(),
                        "optionality": cells[3].text.strip(),
                        "repeatability": cells[4].text.strip() if len(cells) > 4 else "-",
                        "table": cells[5].text.strip() if len(cells) > 5 else ""
                    }

                    field_position += 1  # Increment actual field position

                    # Try to expand for additional details
                    try:
                        if row.find_elements(By.CSS_SELECTOR, "[class*='expand'], [class*='toggle']"):
                            row.click()
                            self.random_delay(0.3)

                            # Look for expanded details
                            expanded_details = self._extract_component_details(row)
                            field_info.update(expanded_details)

                    except Exception as e:
                        logger.debug(f"No expandable details for field {field_info['field_name']}: {e}")

                    if field_info['field_name']:  # Only add if we have a field name
                        fields.append(field_info)

                except Exception as e:
                    logger.warning(f"Failed to parse field row {i}: {e}")
                    continue

            return fields

        except Exception as e:
            logger.debug(f"No fields table found (likely primitive type): {e}")
            return []

    def _extract_component_details(self, row_element) -> Dict[str, Any]:
        """Extract additional component details from expanded row"""
        details = {}

        try:
            # Look for detailed information in the expanded area
            detail_elements = row_element.find_elements(By.CSS_SELECTOR, ".details, .expanded, .component-detail")

            for detail in detail_elements:
                text = detail.text.strip()

                # Extract specific details based on common patterns
                if "LENGTH" in text.upper():
                    details["length_detail"] = text
                elif "DATA TYPE" in text.upper():
                    details["data_type_detail"] = text
                elif "OPTIONALITY" in text.upper():
                    details["optionality_detail"] = text
                elif "REPEATABILITY" in text.upper():
                    details["repeatability_detail"] = text
                elif "TABLE" in text.upper():
                    details["table_detail"] = text

        except Exception as e:
            logger.debug(f"Failed to extract component details: {e}")

        return details

    def _get_item_summary(self, item_data: Dict[str, Any]) -> str:
        fields = len(item_data.get('fields', []))
        return f"{fields} fields" if fields > 0 else "primitive type"

    def _count_elements(self, item_data: Dict[str, Any]) -> int:
        return len(item_data.get('fields', []))


class TablesScraper(BaseCaristixScraper):
    """Scraper for HL7 Code Tables"""

    def __init__(self, dest_path: Path, **kwargs):
        super().__init__("Tables", dest_path, **kwargs)

    def _collect_domain_specific_urls(self) -> List[str]:
        """Collect table URLs from main tables page with virtual scrolling support"""
        try:
            # Wait for initial links to load
            WebDriverWait(self.driver, 15).until(
                EC.presence_of_all_elements_located((By.CSS_SELECTOR, "a[href*='/Tables/']"))
            )

            # Check for virtual scrolling viewport
            try:
                virtual_scroll = WebDriverWait(self.driver, 5).until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, ".cdk-virtual-scroll-viewport"))
                )
                logger.info("Found virtual scroll viewport for tables")
                return self._collect_urls_with_virtual_scroll(virtual_scroll, "a[href*='/Tables/']", "/Tables/")
            except TimeoutException:
                logger.info("No virtual scroll detected, using standard collection")
                return self._collect_urls_standard("a[href*='/Tables/']", "/Tables/")

        except Exception as e:
            logger.error(f"Failed to collect table URLs: {e}")
            return []

    def _parse_domain_specific_page(self, url: str, table_number: str) -> Optional[Dict[str, Any]]:
        """Parse code table page with values"""
        try:
            # Wait for page to load
            WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.TAG_NAME, "h1"))
            )

            # Extract basic info from the main content header
            title = ""
            try:
                # Look for the main content header with the proper title format
                title_selectors = [
                    "h2.content-header-title-centered",
                    ".content-header h2",
                    "h2.cx-h2",
                    "main h2",
                    ".header-left h2",
                    "content-header h2",
                    "h1"  # Fallback to h1 if h2 not found
                ]

                for selector in title_selectors:
                    try:
                        title_element = self.driver.find_element(By.CSS_SELECTOR, selector)
                        if title_element and title_element.text.strip():
                            title = title_element.text.strip()
                            break
                    except NoSuchElementException:
                        continue

                if not title:
                    # Ultimate fallback to any h1 or h2
                    title_element = self.driver.find_element(By.TAG_NAME, "h1")
                    title = title_element.text.strip() if title_element else ""

            except Exception as e:
                logger.warning(f"Failed to extract title: {e}")
                title = "Unknown"

            # Extract version and name from title pattern: "HL7 v2.3 - CODE - Descriptive Name"
            version = "Unknown"
            name = ""
            if " - " in title:
                parts = title.split(" - ")
                if len(parts) >= 3:
                    version = parts[0]  # "HL7 v2.3"
                    name = " - ".join(parts[2:])  # Everything after the code
                elif len(parts) == 2:
                    version = parts[0]  # "HL7 v2.3"
                    name = parts[1]  # Just the second part

            if not name:
                name = title  # Fallback to full title

            # Extract chapter - look for CHAPTERS section
            chapter = "Unknown"
            try:
                # Try multiple selectors for chapter
                chapter_elements = self.driver.find_elements(By.XPATH, "//span[contains(text(), 'CHAPTERS')]/following-sibling::a")
                if not chapter_elements:
                    chapter_elements = self.driver.find_elements(By.CSS_SELECTOR, "a[href*='chapters=']")
                if chapter_elements:
                    chapter = chapter_elements[0].text.strip()
            except Exception as e:
                logger.warning(f"Failed to extract chapter: {e}")

            # Extract description
            description = ""
            try:
                # Look for description text above the table
                desc_elements = self.driver.find_elements(By.CSS_SELECTOR, "p")
                for desc in desc_elements:
                    text = desc.text.strip()
                    if text and "User Defined Tables" in text:
                        description = text
                        break
                    elif text and len(text) > 30 and not text.startswith("Note:"):
                        description = text
                        break
            except Exception as e:
                logger.warning(f"Failed to extract description: {e}")

            # Parse table values
            values = self._parse_table_values()

            # Determine table type
            table_type = "User" if "User Defined" in description else "HL7"

            return {
                "code": table_number,
                "name": name,
                "version": version,
                "chapter": chapter,
                "description": description,
                "type": table_type,
                "values": values,
                "value_count": len(values)
            }

        except Exception as e:
            logger.error(f"Failed to parse table page {url}: {e}")
            return None

    def _parse_table_values(self) -> List[Dict[str, Any]]:
        """Parse table values - simple VALUE, DESCRIPTION, COMMENT structure"""
        values = []

        try:
            # Look for the values table
            table = self.driver.find_element(By.CSS_SELECTOR, "table, .table, [role='table']")
            rows = table.find_elements(By.CSS_SELECTOR, "tr")

            for i, row in enumerate(rows):
                try:
                    # Skip header rows
                    if row.find_elements(By.TAG_NAME, "th"):
                        continue

                    cells = row.find_elements(By.TAG_NAME, "td")
                    if len(cells) < 2:  # Need at least VALUE and DESCRIPTION
                        continue

                    value_info = {
                        "value": cells[0].text.strip(),
                        "description": cells[1].text.strip(),
                        "comment": cells[2].text.strip() if len(cells) > 2 else "",
                        "sort_order": i
                    }

                    # Only add if we have a value
                    if value_info['value']:
                        values.append(value_info)

                except Exception as e:
                    logger.warning(f"Failed to parse table value row {i}: {e}")
                    continue

            return values

        except Exception as e:
            logger.error(f"Failed to parse table values: {e}")
            return []

    def _get_item_summary(self, item_data: Dict[str, Any]) -> str:
        return f"{len(item_data.get('values', []))} values"

    def _count_elements(self, item_data: Dict[str, Any]) -> int:
        return len(item_data.get('values', []))


# Factory function for easy instantiation
def create_scraper(domain: str, dest_path: Path, **kwargs) -> BaseCaristixScraper:
    """Factory function to create appropriate scraper"""
    scrapers = {
        'Segments': SegmentsScraper,
        'DataTypes': DataTypesScraper,
        'Tables': TablesScraper
    }

    if domain not in scrapers:
        raise ValueError(f"Unknown domain: {domain}. Must be one of {list(scrapers.keys())}")

    return scrapers[domain](dest_path, **kwargs)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description='Base Caristix Scraper')
    parser.add_argument('domain', choices=['Segments', 'DataTypes', 'Tables'],
                       help='HL7 domain to scrape')
    parser.add_argument('--output', '-o', type=Path, default='./scraped_data',
                       help='Output directory')
    parser.add_argument('--limit', type=int, help='Limit number of items (for testing)')
    parser.add_argument('--headless', action='store_true', default=True,
                       help='Run in headless mode')

    args = parser.parse_args()

    # Create and run scraper
    scraper = create_scraper(args.domain, args.output, headless=args.headless)
    scraper.run(limit=args.limit)