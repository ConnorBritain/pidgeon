#!/usr/bin/env python3
"""
Enhanced Caristix HL7 v2.3 Trigger Events Scraper
Scrapes all HL7 v2.3 trigger events with full segment layouts including nested groups
"""

import time
import json
import os
import sys
import random
import argparse
from urllib.parse import urlparse
from pathlib import Path
from typing import List, Dict, Any, Optional, Set

from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException, NoSuchElementException, ElementClickInterceptedException
from webdriver_manager.chrome import ChromeDriverManager

from tenacity import retry, stop_after_attempt, wait_exponential, retry_if_exception_type
from loguru import logger

# Constants
BASE_URL = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents"
DEFAULT_DEST = Path(r"C:\Users\Connor.England.FUSIONMGT\OneDrive - Fusion\Documents\Code\CRE Code\hl7generator\scripts\caristix\trigger-events\test_run")

# Selectors - Updated for Angular tree-table structure
SEL_EVENT_LINKS = "a[href*='/v2/HL7v2.3/TriggerEvents/']"
SEL_TABLE = ".tree-table.table-with-margins"  # Angular tree table container
SEL_TABLE_ROWS = ".flex-segment"  # Segment rows in flexbox layout
SEL_EXPANDABLE = "button[aria-expanded], .tree-table-cell-content-with-button, [class*='expandable']"  # Tree expansion buttons
SEL_COOKIE_BANNER = "[class*='cookie'], [id*='cookie'], .consent-banner"
SEL_CLOSE_BUTTON = "button[aria-label*='lose'], button[title*='lose'], .close, button:has(svg)"
SEL_CHAPTER = "h2, h3, .chapter-title"
SEL_DESCRIPTION = "main p, .content p, .description"


class CaristixScraper:
    """Enhanced Caristix scraper with group expansion and retry logic"""

    def __init__(self, dest_path: Path, headless: bool = True, delay_ms: int = 500):
        self.dest = dest_path
        self.headless = headless
        self.delay_ms = delay_ms
        self.driver = None
        self.processed_events: Set[str] = set()

        # Setup logging
        log_path = self.dest / "scrape.log"
        logger.add(log_path, rotation="10 MB", retention="7 days", level="INFO")
        logger.info(f"Initializing scraper with destination: {self.dest}")

    def setup_driver(self) -> webdriver.Chrome:
        """Setup Chrome driver with optimal settings"""
        opts = Options()
        if self.headless:
            opts.add_argument("--headless=new")
        opts.add_argument("--no-sandbox")
        opts.add_argument("--disable-gpu")
        opts.add_argument("--disable-dev-shm-usage")
        opts.add_argument("--window-size=1920,1080")
        opts.add_argument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0")

        # Disable images for faster loading
        prefs = {"profile.managed_default_content_settings.images": 2}
        opts.add_experimental_option("prefs", prefs)

        logger.info(f"Starting Chrome driver (headless={self.headless})")

        # Try multiple driver setup methods for better compatibility
        try:
            # Method 1: webdriver-manager
            driver_path = ChromeDriverManager().install()
            if os.path.exists(driver_path) and os.path.getsize(driver_path) > 1000:
                return webdriver.Chrome(service=Service(driver_path), options=opts)
            else:
                logger.warning("webdriver-manager provided invalid driver, trying system driver")
        except Exception as e:
            logger.warning(f"webdriver-manager failed: {e}, trying system driver")

        # Method 2: System ChromeDriver
        try:
            return webdriver.Chrome(options=opts)
        except Exception as e:
            logger.error(f"System ChromeDriver failed: {e}")
            raise Exception("Could not initialize ChromeDriver")

    def close_cookie_banner(self):
        """Attempt to close cookie/consent banners"""
        for selector in [SEL_COOKIE_BANNER, SEL_CLOSE_BUTTON]:
            try:
                elements = self.driver.find_elements(By.CSS_SELECTOR, selector)
                for el in elements:
                    if "cookie" in el.get_attribute("class").lower() or \
                       "consent" in el.get_attribute("class").lower() or \
                       "close" in el.text.lower():
                        self.driver.execute_script("arguments[0].click()", el)
                        logger.debug("Closed cookie/consent banner")
                        time.sleep(0.5)
                        return
            except Exception:
                pass

    def random_delay(self):
        """Add randomized delay for politeness"""
        delay = self.delay_ms / 1000.0 + random.uniform(-0.1, 0.1)
        time.sleep(max(0.3, delay))

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=2, max=10),
        retry=retry_if_exception_type(TimeoutException)
    )
    def collect_event_urls(self) -> List[str]:
        """Collect all trigger event URLs using Angular CDK virtual scrolling"""
        logger.info(f"Loading main page: {BASE_URL}")
        self.driver.get(BASE_URL)

        # Wait for initial load
        WebDriverWait(self.driver, 15).until(
            EC.presence_of_all_elements_located((By.CSS_SELECTOR, SEL_EVENT_LINKS))
        )

        self.close_cookie_banner()

        # Find the virtual scroll viewport (Angular CDK)
        logger.info("Locating virtual scroll container...")
        try:
            virtual_scroll = WebDriverWait(self.driver, 10).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, ".cdk-virtual-scroll-viewport"))
            )
            logger.info("Found Angular CDK virtual scroll viewport")
        except TimeoutException:
            logger.warning("Virtual scroll viewport not found, using fallback scrolling")
            return self._fallback_collect_urls()

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

        logger.info("Scrolling through virtual list to collect all trigger events...")

        while scroll_position <= max_position and no_new_urls_count < 5:
            # Scroll to position
            self.driver.execute_script(f"arguments[0].scrollTop = {scroll_position}", virtual_scroll)
            time.sleep(0.5)  # Allow virtual scrolling to render

            # Collect URLs currently visible
            current_links = self.driver.find_elements(By.CSS_SELECTOR, SEL_EVENT_LINKS)
            current_urls = {link.get_attribute("href") for link in current_links}

            # Filter valid trigger event URLs
            valid_urls = {url for url in current_urls
                         if url and url.rstrip('/') != BASE_URL and '/TriggerEvents/' in url}

            previous_count = len(all_urls)
            all_urls.update(valid_urls)

            if len(all_urls) == previous_count:
                no_new_urls_count += 1
            else:
                no_new_urls_count = 0

            logger.info(f"Scroll position {scroll_position}/{max_position}: {len(all_urls)} total events")

            scroll_position += scroll_step

        # Final scroll to bottom to catch any remaining items
        self.driver.execute_script("arguments[0].scrollTop = arguments[0].scrollHeight", virtual_scroll)
        time.sleep(1)

        final_links = self.driver.find_elements(By.CSS_SELECTOR, SEL_EVENT_LINKS)
        final_urls = {link.get_attribute("href") for link in final_links}
        final_valid = {url for url in final_urls
                      if url and url.rstrip('/') != BASE_URL and '/TriggerEvents/' in url}
        all_urls.update(final_valid)

        urls = sorted(list(all_urls))
        logger.info(f"Found {len(urls)} unique trigger event URLs")
        return urls

    def _fallback_collect_urls(self) -> List[str]:
        """Fallback URL collection for non-virtual scroll pages"""
        logger.info("Using fallback scrolling method")

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

        # Collect URLs
        links = self.driver.find_elements(By.CSS_SELECTOR, SEL_EVENT_LINKS)
        urls = {link.get_attribute("href") for link in links}
        urls = [url for url in urls if url and url.rstrip('/') != BASE_URL and '/TriggerEvents/' in url]

        return sorted(set(urls))

    def expand_groups(self, table_element) -> List[Dict[str, Any]]:
        """Expand all collapsible groups and extract complete segment hierarchy from Angular tree-table"""
        segments = []

        # First pass: identify and expand all groups in the Angular tree structure
        max_attempts = 15  # Increase attempts for complex expansions
        attempt = 0
        expanded_count = 0
        previous_row_count = 0

        while attempt < max_attempts:
            # Look for expandable buttons - try multiple selectors
            expandable_selectors = [
                "button[aria-expanded='false']",
                ".tree-table-cell-content-with-button button",
                "[class*='chevron']",
                "[class*='expand']",
                "button"
            ]

            expandables = []
            for selector in expandable_selectors:
                elements = table_element.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    expandables = elements
                    logger.debug(f"Found {len(elements)} expandable elements with: {selector}")
                    break

            # Also look for rows that contain chevron_right text and try to click them directly
            chevron_rows = table_element.find_elements(By.XPATH, ".//*[contains(text(), 'chevron_right')]")
            for row in chevron_rows:
                # Try multiple click strategies for this row
                try:
                    # Strategy 1: Look for buttons within the row
                    buttons = row.find_elements(By.CSS_SELECTOR, "button, [role='button'], [class*='expand'], [class*='chevron']")
                    expandables.extend(buttons)

                    # Strategy 2: Try clicking the row itself if it has tree-table-cell-content-with-button class
                    if 'tree-table-cell-content-with-button' in row.get_attribute('class'):
                        expandables.append(row)

                    # Strategy 3: Look for clickable elements with mat-icon-button
                    mat_buttons = row.find_elements(By.CSS_SELECTOR, "[class*='mat-icon-button']")
                    expandables.extend(mat_buttons)

                except Exception as e:
                    logger.debug(f"Error finding clickable elements in chevron row: {e}")

            logger.debug(f"Total expandable elements found: {len(expandables)}")

            any_expanded = False
            for button in expandables:
                try:
                    # Check if this button is actually expandable and not already expanded
                    aria_expanded = button.get_attribute("aria-expanded")
                    if aria_expanded == "true":
                        continue

                    # Get context about what we're trying to expand
                    button_text = button.text.strip()
                    parent_text = ""
                    try:
                        parent_text = button.find_element(By.XPATH, "./..").text.strip()[:50]
                    except:
                        pass

                    # Try multiple click strategies
                    click_success = False

                    # Strategy 1: Direct click
                    try:
                        self.driver.execute_script("arguments[0].scrollIntoView(true);", button)
                        time.sleep(0.3)
                        button.click()
                        click_success = True
                        logger.debug(f"Direct click succeeded on: {parent_text}")
                    except:
                        pass

                    # Strategy 2: JavaScript click
                    if not click_success:
                        try:
                            self.driver.execute_script("arguments[0].click();", button)
                            click_success = True
                            logger.debug(f"JS click succeeded on: {parent_text}")
                        except:
                            pass

                    # Strategy 3: ActionChains click
                    if not click_success:
                        try:
                            from selenium.webdriver.common.action_chains import ActionChains
                            actions = ActionChains(self.driver)
                            actions.move_to_element(button).click().perform()
                            click_success = True
                            logger.debug(f"ActionChains click succeeded on: {parent_text}")
                        except:
                            pass

                    if click_success:
                        expanded_count += 1
                        any_expanded = True
                        time.sleep(1.0)  # Wait longer for Angular animation
                        logger.debug(f"Expanded tree node {expanded_count}: {parent_text}")

                except Exception as e:
                    logger.debug(f"Could not expand tree node: {e}")
                    continue

            # After each expansion round, check if we have new rows
            current_rows = table_element.find_elements(By.CSS_SELECTOR, ".flex-segment")
            current_row_count = len(current_rows)
            logger.debug(f"After expansion round {attempt}: {current_row_count} total rows (was {previous_row_count})")

            # Check for any remaining expandable groups by looking for chevron_right text
            remaining_chevrons = table_element.find_elements(By.XPATH, ".//*[contains(text(), 'chevron_right')]")
            logger.debug(f"Remaining unexpanded groups: {len(remaining_chevrons)}")

            if not any_expanded:
                # If no buttons were clicked, try to find any remaining expandable content
                if remaining_chevrons:
                    logger.debug("No buttons clicked but chevrons remain - trying alternative expansion")

                    # Try clicking on the chevron rows directly
                    for chevron_row in remaining_chevrons[:3]:  # Try first 3
                        try:
                            # Try clicking the entire row
                            self.driver.execute_script("arguments[0].click();", chevron_row)
                            time.sleep(1.0)
                            any_expanded = True
                            expanded_count += 1
                            logger.debug("Alternative chevron click succeeded")
                        except:
                            pass

                    # Also try any button-like elements
                    all_clickables = table_element.find_elements(By.XPATH, ".//button | .//*[@role='button'] | .//*[contains(@class, 'clickable')] | .//*[contains(@class, 'expand')]")
                    logger.debug(f"Last resort: found {len(all_clickables)} potentially clickable elements")

                    for clickable in all_clickables[:5]:  # Try first 5
                        try:
                            self.driver.execute_script("arguments[0].click();", clickable)
                            time.sleep(0.5)
                            expanded_count += 1
                            any_expanded = True
                            logger.debug(f"Last resort click succeeded")
                        except:
                            pass

                if not any_expanded:
                    logger.debug("No expansion possible - stopping")
                    break

            # Update row count tracking
            previous_row_count = current_row_count
            attempt += 1

            # Give extra time for complex expansions to settle
            if any_expanded:
                time.sleep(0.5)

        logger.info(f"Expanded {expanded_count} tree nodes")

        # Second pass: extract all segments from the table structure
        # Look for table rows that contain segment information
        try:
            # Try multiple selectors for table rows
            row_selectors = [
                ".flex-segment",
                ".tree-table-row",
                "[class*='row']",
                "tr",
                ".segment-row"
            ]

            segment_elements = []
            for selector in row_selectors:
                elements = table_element.find_elements(By.CSS_SELECTOR, selector)
                if elements:
                    segment_elements = elements
                    logger.debug(f"Found {len(elements)} rows using selector: {selector}")
                    break

            if not segment_elements:
                logger.warning("No table rows found, trying alternative parsing")
                # Fallback to parsing all visible text
                return self._parse_table_text_fallback(table_element)

        except Exception as e:
            logger.error(f"Error finding table rows: {e}")
            return []

        order_index = 0
        group_stack = []

        for segment_elem in segment_elements:
            try:
                segment_data = self._extract_segment_from_row(segment_elem, order_index, group_stack)
                if segment_data:
                    segments.append(segment_data)
                    order_index += 1

                    # Update group stack based on hierarchy
                    # If this is a group, add it to the stack at the appropriate level
                    if segment_data and segment_data.get('is_group'):
                        current_level = segment_data.get('level', 0)

                        # Trim group stack to current level (exit higher-level groups)
                        if current_level < len(group_stack):
                            group_stack = group_stack[:current_level]

                        # Add this group to the stack
                        group_stack.append(segment_data['segment_code'])

                        logger.debug(f"Added group '{segment_data['segment_code']}' to stack at level {current_level}: {group_stack}")

            except Exception as e:
                logger.debug(f"Error processing segment row: {e}")
                continue

        return segments

    def _extract_segment_from_row(self, row_element, order_index: int, group_stack: list) -> Optional[Dict[str, Any]]:
        """Extract segment information from a table row with improved column parsing"""
        try:
            # Try to find individual cells/columns in the row
            cell_selectors = [
                ".tree-table-cell",
                ".cell",
                ".column",
                "td",
                "[class*='cell']"
            ]

            cells = []
            for selector in cell_selectors:
                cells = row_element.find_elements(By.CSS_SELECTOR, selector)
                if cells:
                    break

            # If no cells found, try to parse from the row text directly
            if not cells:
                return self._parse_row_text(row_element, order_index, group_stack)

            # Extract data from cells (expected: segment, optionality, repeatability)
            seg_code = ""
            seg_desc = ""
            optionality = "-"
            repeatability = "-"
            is_group = False
            level = len(group_stack)

            for i, cell in enumerate(cells):
                cell_text = cell.text.strip()

                if i == 0:  # First cell: segment code and description
                    if ' - ' in cell_text:
                        parts = cell_text.split(' - ', 1)
                        seg_code = parts[0].strip()
                        seg_desc = parts[1].strip()
                    else:
                        seg_code = cell_text
                        seg_desc = ""

                    # Filter out UI elements
                    if seg_code in ['chevron_right', 'expand_more', 'expand_less', '']:
                        return None

                    # Check if this is a group
                    is_group = seg_code.isupper() and len(seg_code) > 3 and not seg_code.startswith(('MSH', 'PID', 'EVN'))

                elif i == 1:  # Second cell: optionality
                    if cell_text in ['R', 'O', 'C']:
                        optionality = cell_text

                elif i == 2:  # Third cell: repeatability
                    if cell_text in ['-', '∞', '*']:
                        repeatability = cell_text

            # Skip if no valid segment code found
            if not seg_code or len(seg_code) < 2:
                return None

            return {
                "segment_code": seg_code,
                "segment_desc": seg_desc,
                "optionality": optionality,
                "repeatability": repeatability,
                "is_group": is_group,
                "group_path": group_stack.copy(),
                "level": level,
                "order_index": order_index
            }

        except Exception as e:
            logger.debug(f"Error extracting segment from row: {e}")
            return None

    def _parse_row_text(self, row_element, order_index: int, group_stack: list) -> Optional[Dict[str, Any]]:
        """Parse segment info from flex-segment row text - handles Caristix format with hierarchy detection"""
        try:
            full_text = row_element.text.strip()
            if not full_text:
                return None

            # Extract hierarchy level from CSS padding
            hierarchy_level, current_group_stack = self._detect_hierarchy_level(row_element, group_stack)

            # Handle UI elements and expandable groups
            if 'chevron_right' in full_text or 'expand_more' in full_text:
                # This is an expandable group - extract group name
                # Format: "chevron_right PATIENT"
                lines = full_text.split('\n')
                for line in lines:
                    line = line.strip()
                    if line and line not in ['chevron_right', 'expand_more', 'expand_less']:
                        seg_code = line
                        return {
                            "segment_code": seg_code,
                            "segment_desc": f"{seg_code} group",
                            "optionality": "R",  # Groups are typically required
                            "repeatability": "inf",  # Groups can typically repeat (use 'inf' instead of ∞)
                            "is_group": True,
                            "group_path": current_group_stack.copy(),
                            "level": hierarchy_level,
                            "order_index": order_index
                        }
                return None

            # Skip header rows
            if full_text.upper() in ['SEGMENT', 'OPTIONALITY', 'REPEATABILITY']:
                return None

            # Parse segment format: "MSH\n - Message header segment" with optionality/repeatability
            lines = full_text.split('\n')
            if not lines:
                return None

            # First line should contain segment code
            first_line = lines[0].strip()
            if not first_line:
                return None

            seg_code = first_line
            seg_desc = ""
            optionality = "R"  # Default to Required
            repeatability = "-"  # Default to non-repeating

            # Look for description in subsequent lines
            # Format: "MSH\n - Message header segment" or "MSH - Message header segment"
            if len(lines) > 1:
                # Check if second line contains description
                second_line = lines[1].strip()
                if second_line.startswith('- '):
                    seg_desc = second_line[2:].strip()  # Remove "- " prefix
                elif second_line.startswith(' - '):
                    seg_desc = second_line[3:].strip()  # Remove " - " prefix

            # Also handle single-line format "MSH - Message header segment"
            if not seg_desc and ' - ' in first_line:
                parts = first_line.split(' - ', 1)
                seg_code = parts[0].strip()
                seg_desc = parts[1].strip()

            # Filter out invalid segment codes
            if not seg_code or len(seg_code) < 2 or seg_code in ['chevron_right', 'expand_more', 'expand_less']:
                return None

            # Look for optionality and repeatability in subsequent lines or in a structured layout
            # For Caristix, these might be in separate visual columns
            for line in lines[1:]:
                line = line.strip()
                if line in ['R', 'O', 'C']:
                    optionality = line
                elif line in ['-', '∞', '*']:
                    repeatability = line

            # Determine if this is a group based on naming conventions
            is_group = (seg_code.isupper() and
                       len(seg_code) > 3 and
                       not seg_code.startswith(('MSH', 'EVN', 'PID', 'PV1', 'NK1', 'OBX', 'ORC', 'OBR', 'RXE', 'RXR', 'RXA', 'RXC')))

            return {
                "segment_code": seg_code,
                "segment_desc": seg_desc,
                "optionality": optionality,
                "repeatability": repeatability,
                "is_group": is_group,
                "group_path": current_group_stack.copy(),
                "level": hierarchy_level,
                "order_index": order_index
            }

        except Exception as e:
            logger.debug(f"Error parsing row text: {e}")
            return None

    def _detect_hierarchy_level(self, row_element, current_group_stack: list) -> tuple[int, list]:
        """
        Detect hierarchy level from CSS padding and maintain group stack
        Returns: (hierarchy_level, updated_group_stack)
        """
        try:
            # Extract padding-left from element style
            style = row_element.get_attribute("style")
            padding_left = "0px"

            if style and "padding-left:" in style:
                import re
                match = re.search(r'padding-left:\s*(\d+)px', style)
                if match:
                    padding_left = match.group(1) + "px"

            # Convert padding to hierarchy level
            # Based on debug output: 0px=level 0, 16px=level 1, 32px=level 2, etc.
            padding_pixels = int(padding_left.replace('px', ''))
            hierarchy_level = padding_pixels // 16  # Each level is 16px indentation

            # Update group stack based on hierarchy level
            # If level decreased, we've exited some groups
            if hierarchy_level < len(current_group_stack):
                # Trim group stack to match current level
                current_group_stack = current_group_stack[:hierarchy_level]

            # If this is a group element, it will be added to the stack later
            # For now, just return the current state

            return hierarchy_level, current_group_stack

        except Exception as e:
            logger.debug(f"Error detecting hierarchy level: {e}")
            # Fallback to original group stack length
            return len(current_group_stack), current_group_stack

    def _parse_table_text_fallback(self, table_element) -> List[Dict[str, Any]]:
        """Last resort: parse the entire table text when structured parsing fails"""
        try:
            table_text = table_element.text.strip()
            lines = [line.strip() for line in table_text.split('\n') if line.strip()]

            segments = []
            order_index = 0

            for line in lines:
                # Skip header lines and empty lines
                if not line or 'SEGMENT' in line.upper() or 'OPTIONALITY' in line.upper():
                    continue

                # Try to extract segment info from the line
                parts = line.split()
                if len(parts) >= 1:
                    seg_code = parts[0]

                    # Filter out UI elements
                    if seg_code in ['chevron_right', 'expand_more', 'expand_less', '']:
                        continue

                    # Basic segment info
                    segment_data = {
                        "segment_code": seg_code,
                        "segment_desc": "",
                        "optionality": "-",
                        "repeatability": "-",
                        "is_group": False,
                        "group_path": [],
                        "level": 0,
                        "order_index": order_index
                    }

                    segments.append(segment_data)
                    order_index += 1

            return segments

        except Exception as e:
            logger.error(f"Fallback parsing failed: {e}")
            return []

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=2, max=10),
        retry=retry_if_exception_type(TimeoutException)
    )
    def parse_event_page(self, url: str) -> Optional[Dict[str, Any]]:
        """Parse a single trigger event page with retry logic"""
        code = url.rstrip('/').split('/')[-1]
        logger.info(f"Parsing event: {code}")

        self.driver.get(url)
        self.random_delay()

        # Wait for tree-table to load
        WebDriverWait(self.driver, 15).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, SEL_TABLE))
        )

        # Extract metadata using correct selectors
        title = ""
        version = ""
        name = ""
        chapter = ""
        description = ""

        try:
            # Extract title from the specific h2 element
            title_selector = "h2.cx-h2.content-header-title-centered"
            title_element = self.driver.find_element(By.CSS_SELECTOR, title_selector)
            title = title_element.text.strip()

            # Parse title: "HL7 v2.3 - ADT_A01 - Admit/visit notification"
            if " - " in title:
                parts = title.split(" - ")
                if len(parts) >= 3:
                    version = parts[0].strip()  # "HL7 v2.3"
                    name = parts[2].strip()     # "Admit/visit notification"
                elif len(parts) == 2:
                    version = parts[0].strip()
                    name = parts[1].strip()

            logger.debug(f"Extracted title: '{title}', version: '{version}', name: '{name}'")

            # Extract description from the specific span element
            desc_selector = "span.cx-body.text--preserve-paragraphs"
            desc_element = self.driver.find_element(By.CSS_SELECTOR, desc_selector)
            description = desc_element.text.strip()

            logger.debug(f"Extracted description: {len(description)} characters")

            # For chapter, we'll extract from breadcrumbs or navigation if available
            # Try common chapter selectors
            chapter_selectors = [
                ".breadcrumb",
                ".navigation",
                "[class*='breadcrumb']",
                ".chapter-title"
            ]

            for selector in chapter_selectors:
                try:
                    chapter_elements = self.driver.find_elements(By.CSS_SELECTOR, selector)
                    for elem in chapter_elements:
                        text = elem.text.strip()
                        # Look for "Patient Administration" or similar chapter names
                        if text and "Administration" in text and len(text) < 50:
                            chapter = text
                            break
                    if chapter:
                        break
                except:
                    continue

            # Fallback chapter extraction from known patterns
            if not chapter:
                chapter = "Patient Administration"  # Default for ADT events

        except Exception as e:
            logger.warning(f"Could not extract title/description: {e}")
            # Fallback to browser title if the specific elements aren't found
            title = self.driver.title
            if " - " in title:
                parts = title.split(" - ")
                if len(parts) >= 3:
                    version = parts[0].strip()
                    name = parts[2].strip()
                elif len(parts) == 2:
                    version = parts[0].strip()
                    name = parts[1].strip()

        # Extract segments from Angular tree-table with group expansion
        segments = []
        try:
            table = self.driver.find_element(By.CSS_SELECTOR, SEL_TABLE)
            segments = self.expand_groups(table)
        except Exception as e:
            logger.error(f"Failed to extract segments for {code}: {e}")
            return None

        return {
            "code": code,
            "name": name,
            "version": version,
            "chapter": chapter,
            "description": description,
            "segments": segments,
            "segment_count": len(segments)
        }

    def save_event_data(self, event_data: Dict[str, Any]):
        """Save individual event data as JSON"""
        code = event_data["code"]
        events_dir = self.dest / "events"
        events_dir.mkdir(exist_ok=True)

        # Save JSON
        json_path = events_dir / f"v23_{code}.json"
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(event_data, f, ensure_ascii=False, indent=2)

        logger.info(f"Saved {code}: {len(event_data['segments'])} segments")

    def run(self, resume: bool = False, limit: Optional[int] = None):
        """Main execution method"""
        # Setup destination
        self.dest.mkdir(parents=True, exist_ok=True)

        # Check for existing data if resuming
        if resume:
            events_dir = self.dest / "events"
            if events_dir.exists():
                existing = [f.stem.replace("v23_", "") for f in events_dir.glob("v23_*.json")]
                self.processed_events = set(existing)
                logger.info(f"Resuming: skipping {len(self.processed_events)} already processed events")

        # Initialize driver
        self.driver = self.setup_driver()

        try:
            # Collect all event URLs
            urls = self.collect_event_urls()

            if limit:
                urls = urls[:limit]
                logger.info(f"Limiting to {limit} events for testing")

            # Process each event
            all_data = []

            for i, url in enumerate(urls, 1):
                code = url.rstrip('/').split('/')[-1]

                # Skip if already processed
                if code in self.processed_events:
                    logger.info(f"Skipping {code} (already processed)")
                    continue

                logger.info(f"Processing {i}/{len(urls)}: {code}")

                try:
                    event_data = self.parse_event_page(url)
                    if event_data:
                        self.save_event_data(event_data)
                        all_data.append(event_data)
                        self.processed_events.add(code)

                except Exception as e:
                    logger.error(f"Failed to process {code}: {e}")
                    continue

                # Random delay between events
                self.random_delay()

            # Save master files
            logger.info("Creating master file...")

            # Calculate total segments from all events
            total_segments = sum(len(event.get("segments", [])) for event in all_data)

            # Master JSON
            master_json_path = self.dest / "trigger_events_v23_master.json"
            master_data = {
                "version": "2.3",
                "scraped_date": time.strftime("%Y-%m-%d %H:%M:%S"),
                "event_count": len(all_data),
                "total_segments": total_segments,
                "events": all_data
            }
            with open(master_json_path, "w", encoding="utf-8") as f:
                json.dump(master_data, f, ensure_ascii=False, indent=2)

            logger.info(f"Scraping complete: {len(all_data)} events, {total_segments} total segments")

        finally:
            if self.driver:
                self.driver.quit()
                logger.info("Driver closed")


def main():
    """CLI entry point"""
    parser = argparse.ArgumentParser(description="Scrape HL7 v2.3 trigger events from Caristix")
    parser.add_argument("--dest", type=str, help="Destination directory", default=str(DEFAULT_DEST))
    parser.add_argument("--resume", action="store_true", help="Resume from previous run")
    parser.add_argument("--headful", action="store_true", help="Run with visible browser")
    parser.add_argument("--limit", type=int, help="Limit number of events for testing")
    parser.add_argument("--delay-ms", type=int, default=500, help="Base delay between actions in ms")
    parser.add_argument("--debug", action="store_true", help="Enable debug logging")

    args = parser.parse_args()

    # Setup logging level
    if args.debug:
        logger.remove()
        logger.add(sys.stderr, level="DEBUG")

    # Create scraper and run
    dest_path = Path(args.dest)
    scraper = CaristixScraper(
        dest_path=dest_path,
        headless=not args.headful,
        delay_ms=args.delay_ms
    )

    logger.info(f"Starting Caristix scraper")
    logger.info(f"Destination: {dest_path}")
    logger.info(f"Headless: {not args.headful}")
    logger.info(f"Resume: {args.resume}")

    scraper.run(resume=args.resume, limit=args.limit)


if __name__ == "__main__":
    main()