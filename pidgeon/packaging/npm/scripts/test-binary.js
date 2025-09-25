#!/usr/bin/env node

const { execSync } = require("child_process");
const path = require("path");
const fs = require("fs");
const os = require("os");

const binDir = path.join(__dirname, "..", "bin");
const binaryName = os.platform() === "win32" ? "pidgeon.exe" : "pidgeon";
const binaryPath = path.join(binDir, binaryName);

function runTest(name, command) {
  console.log(`🧪 Testing: ${name}`);
  try {
    const output = execSync(command, {
      encoding: 'utf8',
      cwd: binDir,
      stdio: 'pipe'
    });
    console.log(`✅ ${name}: PASSED`);
    return output;
  } catch (error) {
    console.error(`❌ ${name}: FAILED`);
    console.error(`Command: ${command}`);
    console.error(`Error: ${error.message}`);
    if (error.stdout) console.error(`Stdout: ${error.stdout}`);
    if (error.stderr) console.error(`Stderr: ${error.stderr}`);
    throw error;
  }
}

function main() {
  console.log("🚀 Testing Pidgeon CLI binary installation...");
  console.log("");

  // Check if binary exists
  if (!fs.existsSync(binaryPath)) {
    console.error(`❌ Binary not found at: ${binaryPath}`);
    process.exit(1);
  }

  console.log(`📍 Binary location: ${binaryPath}`);

  try {
    // Test version command
    const version = runTest("Version check", `"${binaryPath}" --version`);
    console.log(`   Version: ${version.trim()}`);

    // Test help command
    runTest("Help command", `"${binaryPath}" --help`);

    // Test basic functionality
    runTest("Generate command", `"${binaryPath}" generate message --type "ADT^A01" --count 1`);

    console.log("");
    console.log("🎉 All tests passed! Pidgeon CLI is working correctly.");

  } catch (error) {
    console.log("");
    console.error("💥 Binary test failed!");
    process.exit(1);
  }
}

main();