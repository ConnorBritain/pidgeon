#!/usr/bin/env node

const os = require("os");
const path = require("path");
const fs = require("fs");
const crypto = require("crypto");
const { execSync } = require("child_process");
const https = require("https");

const version = require("../package.json").version;
const checksums = require("../checksums.json")[`v${version}`];

const platform = os.platform(); // 'darwin', 'linux', 'win32'
const arch = os.arch(); // 'x64', 'arm64'

// Map Node.js platform names to Pidgeon release names
const platformMap = {
  'darwin': 'osx',
  'linux': 'linux',
  'win32': 'win'
};

// Map Node.js arch names to Pidgeon release names
const archMap = {
  'x64': 'x64',
  'arm64': 'arm64'
};

const mappedPlatform = platformMap[platform];
const mappedArch = archMap[arch];

if (!mappedPlatform || !mappedArch) {
  console.error(`Unsupported platform: ${platform}-${arch}`);
  process.exit(1);
}

const key = `${mappedPlatform}-${mappedArch}`;
const expected = checksums[key];

if (!expected) {
  console.error(`No binary available for ${platform}-${arch} (${key})`);
  console.error(`Available platforms:`, Object.keys(checksums));
  process.exit(1);
}

const repo = "PidgeonHealth/pidgeon";
const filename = platform === "win32"
  ? `pidgeon-win-${mappedArch}.zip`
  : `pidgeon-${mappedPlatform}-${mappedArch}.tar.gz`;

const url = `https://github.com/${repo}/releases/download/v${version}/${filename}`;

const binDir = path.join(__dirname, "..", "bin");
if (!fs.existsSync(binDir)) {
  fs.mkdirSync(binDir, { recursive: true });
}

const tempFile = path.join(binDir, filename);

console.log(`📦 Downloading Pidgeon CLI v${version} for ${platform}-${arch}...`);
console.log(`🌐 URL: ${url}`);

function download(url, dest, callback) {
  const file = fs.createWriteStream(dest);

  https.get(url, (response) => {
    if (response.statusCode === 302 || response.statusCode === 301) {
      // Follow redirect
      download(response.headers.location, dest, callback);
      return;
    }

    if (response.statusCode !== 200) {
      callback(`HTTP ${response.statusCode}: ${response.statusMessage}`);
      return;
    }

    response.pipe(file);

    file.on("finish", () => {
      file.close(callback);
    });
  }).on("error", (err) => {
    fs.unlinkSync(dest);
    callback(err.message);
  });
}

function verifyChecksum(file, expected) {
  const data = fs.readFileSync(file);
  const hash = crypto.createHash("sha256").update(data).digest("hex");

  if (hash !== expected) {
    throw new Error(`Checksum mismatch! Expected ${expected}, got ${hash}`);
  }

  console.log("✅ Checksum verified");
}

function extractArchive(file, destDir) {
  console.log("📂 Extracting archive...");

  if (platform === "win32") {
    // Windows ZIP extraction
    if (process.platform === "win32") {
      execSync(`powershell -Command "Expand-Archive -Force '${file}' '${destDir}'"`, { stdio: 'inherit' });
    } else {
      // Cross-platform unzip fallback
      execSync(`unzip -o "${file}" -d "${destDir}"`, { stdio: 'inherit' });
    }
  } else {
    // Unix tar.gz extraction
    execSync(`tar -xzf "${file}" -C "${destDir}"`, { stdio: 'inherit' });
  }
}

function setupBinary() {
  const binaryName = platform === "win32" ? "pidgeon.exe" : "pidgeon";
  const binaryPath = path.join(binDir, binaryName);

  if (!fs.existsSync(binaryPath)) {
    throw new Error(`Binary not found at ${binaryPath} after extraction`);
  }

  // Make executable on Unix systems
  if (platform !== "win32") {
    fs.chmodSync(binaryPath, 0o755);
  }

  // Create wrapper script
  const wrapperPath = path.join(binDir, "pidgeon");
  if (platform === "win32") {
    // Windows batch wrapper
    const wrapperContent = `@echo off
"%~dp0pidgeon.exe" %*`;
    fs.writeFileSync(wrapperPath + ".cmd", wrapperContent);
  } else {
    // Unix shell wrapper (symlink)
    if (fs.existsSync(wrapperPath)) {
      fs.unlinkSync(wrapperPath);
    }
    fs.symlinkSync(binaryName, wrapperPath);
  }

  // Create install marker
  const markerPath = path.join(binDir, "install.json");
  fs.writeFileSync(markerPath, JSON.stringify({
    channel: "npm",
    version: version,
    platform: `${platform}-${arch}`,
    installedAt: new Date().toISOString()
  }, null, 2));

  console.log("✅ Binary setup completed");
}

function testInstallation() {
  const binaryName = platform === "win32" ? "pidgeon.exe" : "pidgeon";
  const binaryPath = path.join(binDir, binaryName);

  try {
    const output = execSync(`"${binaryPath}" --version`, { encoding: 'utf8' });
    console.log(`🎉 Installation successful! Version: ${output.trim()}`);
  } catch (error) {
    console.warn(`⚠️  Installation completed but version check failed: ${error.message}`);
  }
}

// Main installation flow
download(url, tempFile, (err) => {
  if (err) {
    console.error(`❌ Download failed: ${err}`);
    process.exit(1);
  }

  try {
    console.log("🔍 Verifying checksum...");
    verifyChecksum(tempFile, expected);

    extractArchive(tempFile, binDir);
    setupBinary();
    testInstallation();

    // Cleanup temp file
    fs.unlinkSync(tempFile);

    console.log("");
    console.log("🎊 Pidgeon CLI installed successfully via npm!");
    console.log("");
    console.log("Quick start:");
    console.log("  pidgeon --version");
    console.log("  pidgeon --help");
    console.log("  pidgeon generate message --type ADT^A01");
    console.log("");
    console.log("Documentation: https://docs.pidgeon.health");

  } catch (error) {
    console.error(`❌ Installation failed: ${error.message}`);

    // Cleanup on error
    if (fs.existsSync(tempFile)) {
      fs.unlinkSync(tempFile);
    }

    process.exit(1);
  }
});