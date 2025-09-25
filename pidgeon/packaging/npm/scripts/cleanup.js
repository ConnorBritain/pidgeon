#!/usr/bin/env node

const fs = require("fs");
const path = require("path");

const binDir = path.join(__dirname, "..", "bin");

console.log("🧹 Cleaning up Pidgeon CLI binary...");

if (fs.existsSync(binDir)) {
  // Remove all files in bin directory
  const files = fs.readdirSync(binDir);
  files.forEach(file => {
    const filePath = path.join(binDir, file);
    try {
      if (fs.lstatSync(filePath).isDirectory()) {
        fs.rmSync(filePath, { recursive: true, force: true });
      } else {
        fs.unlinkSync(filePath);
      }
    } catch (error) {
      console.warn(`Failed to remove ${file}: ${error.message}`);
    }
  });

  // Remove bin directory if empty
  try {
    fs.rmdirSync(binDir);
  } catch (error) {
    // Directory might not be empty, that's okay
  }

  console.log("✅ Cleanup completed");
} else {
  console.log("ℹ️  No binary directory found, nothing to clean");
}