#!/usr/bin/env node

// Simple proxy to the binary for programmatic usage
const { spawn } = require('child_process');
const path = require('path');
const os = require('os');

const binaryName = os.platform() === 'win32' ? 'pidgeon.exe' : 'pidgeon';
const binaryPath = path.join(__dirname, 'bin', binaryName);

function runPidgeon(args = [], options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(binaryPath, args, {
      stdio: 'inherit',
      ...options
    });

    child.on('close', (code) => {
      if (code === 0) {
        resolve(code);
      } else {
        reject(new Error(`pidgeon exited with code ${code}`));
      }
    });

    child.on('error', (error) => {
      reject(error);
    });
  });
}

module.exports = {
  run: runPidgeon,
  binaryPath
};

// If called directly, proxy to the binary
if (require.main === module) {
  const args = process.argv.slice(2);
  runPidgeon(args).catch(() => process.exit(1));
}