#!/usr/bin/env python3
"""
Debug specific scroll containers and virtual scrolling behavior
"""

import time
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.common.action_chains import ActionChains

def debug_scroll_containers():
    """Debug scroll containers and virtual scrolling"""
    print("Debugging scroll containers and virtual scrolling...")

    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-gpu")

    driver = webdriver.Chrome(options=options)

    try:
        url = "https://hl7-definition.caristix.com/v2/HL7v2.3/TriggerEvents"
        print(f"Loading: {url}")

        driver.get(url)
        time.sleep(5)  # Give more time for initial load

        print(f"Page title: {driver.title}")

        # Check viewport and document dimensions
        viewport_height = driver.execute_script("return window.innerHeight")
        viewport_width = driver.execute_script("return window.innerWidth")
        document_height = driver.execute_script("return document.body.scrollHeight")
        document_width = driver.execute_script("return document.body.scrollWidth")

        print(f"Viewport: {viewport_width}x{viewport_height}")
        print(f"Document: {document_width}x{document_height}")

        # Look for scrollable containers
        print(f"\n=== Scrollable Container Analysis ===")

        container_selectors = [
            "div[style*='overflow']",
            "[style*='scroll']",
            ".scroll",
            ".scrollable",
            ".overflow",
            ".list-container",
            ".events-container",
            ".content-container",
            "main",
            "article",
            ".mat-list",  # Angular Material
            ".cdk-virtual-scroll-viewport",  # Angular CDK virtual scrolling
            "[class*='virtual']",
            "[class*='scroll']"
        ]

        scrollable_elements = []
        for selector in container_selectors:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, selector)
                for elem in elements:
                    # Check if element is scrollable
                    scroll_height = driver.execute_script("return arguments[0].scrollHeight", elem)
                    client_height = driver.execute_script("return arguments[0].clientHeight", elem)
                    overflow_y = driver.execute_script("return getComputedStyle(arguments[0]).overflowY", elem)

                    if scroll_height > client_height or overflow_y in ['scroll', 'auto']:
                        scrollable_elements.append((selector, elem, scroll_height, client_height))
                        print(f"Found scrollable {selector}: scrollHeight={scroll_height}, clientHeight={client_height}, overflow={overflow_y}")
            except:
                pass

        # Try scrolling each scrollable container
        print(f"\n=== Testing Scrollable Containers ===")
        for selector, elem, scroll_height, client_height in scrollable_elements:
            print(f"\nTesting {selector}...")

            initial_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
            print(f"  Initial links: {len(initial_links)}")

            # Scroll this specific element
            scroll_steps = max(10, (scroll_height - client_height) // 100)
            for step in range(scroll_steps):
                scroll_position = (step + 1) * (scroll_height // scroll_steps)
                driver.execute_script(f"arguments[0].scrollTop = {scroll_position}", elem)
                time.sleep(0.2)

                if step % 5 == 0:
                    current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                    print(f"    Step {step}: {len(current_links)} links")

            final_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
            print(f"  Final links after scrolling {selector}: {len(final_links)}")

            # Check if this container helped load more content
            if len(final_links) > len(initial_links):
                print(f"  [SUCCESS] Found {len(final_links) - len(initial_links)} additional links!")

        # Try mouse wheel scrolling (sometimes different from script scrolling)
        print(f"\n=== Testing Mouse Wheel Scrolling ===")
        initial_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
        print(f"Initial links: {len(initial_links)}")

        # Use ActionChains to simulate mouse wheel
        actions = ActionChains(driver)
        body = driver.find_element(By.TAG_NAME, "body")

        for i in range(20):
            actions.move_to_element(body).perform()
            actions.scroll_by_amount(0, 500).perform()
            time.sleep(0.3)

            if i % 5 == 0:
                current_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
                print(f"  Mouse wheel step {i}: {len(current_links)} links")

        final_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/TriggerEvents/']")
        print(f"Final links after mouse wheel: {len(final_links)}")

        # Check for intersection observer or lazy loading indicators
        print(f"\n=== Lazy Loading Analysis ===")

        lazy_indicators = [
            ".loading",
            ".spinner",
            ".skeleton",
            ".placeholder",
            "[class*='loading']",
            "[class*='lazy']",
            "[data-loading]"
        ]

        for indicator in lazy_indicators:
            try:
                elements = driver.find_elements(By.CSS_SELECTOR, indicator)
                if elements:
                    print(f"Found {len(elements)} '{indicator}' elements - possible lazy loading")
            except:
                pass

        # Check page source for more events that might be hidden
        print(f"\n=== Hidden Content Analysis ===")
        page_source = driver.page_source

        # Look for CSS that might hide content
        hidden_patterns = [
            'display: none',
            'visibility: hidden',
            'height: 0',
            'overflow: hidden'
        ]

        for pattern in hidden_patterns:
            if pattern in page_source:
                print(f"Found '{pattern}' in page source - content might be hidden")

        # Look for Angular/React components that might need activation
        framework_indicators = [
            'ng-',
            'angular',
            'react',
            'vue',
            '_ngcontent',
            'mat-',
            'cdk-'
        ]

        for indicator in framework_indicators:
            count = page_source.lower().count(indicator.lower())
            if count > 0:
                print(f"Found {count} instances of '{indicator}' - modern framework detected")

        return len(final_links)

    except Exception as e:
        print(f"Debug failed: {e}")
        return 0

    finally:
        driver.quit()

if __name__ == "__main__":
    count = debug_scroll_containers()
    print(f"\nFinal result: {count} trigger events found")