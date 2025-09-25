here’s a lightweight **npm wrapper** setup. this lets users do

```bash
npm install -g @pidgeon/cli
```

and it will download the right binary from your GitHub Releases, verify its hash, and put it on PATH.

---

# 1. npm package structure

```
pidgeon-npm-wrapper/
├── package.json
├── index.js        # optional (for help/version proxy)
├── postinstall.js  # downloads + verifies binary
├── checksums.json  # generated per release (sha256 values)
```

---

# 2. `package.json`

```json
{
  "name": "@pidgeon/cli",
  "version": "1.0.0",
  "description": "Pidgeon Health CLI wrapper for npm",
  "bin": {
    "pidgeon": "pidgeon-bin/pidgeon"
  },
  "scripts": {
    "postinstall": "node postinstall.js"
  },
  "os": [
    "darwin",
    "linux",
    "win32"
  ],
  "cpu": [
    "x64",
    "arm64"
  ],
  "author": "Pidgeon Health",
  "license": "MIT"
}
```

* `bin` points to the downloaded binary (`pidgeon-bin/pidgeon` or `pidgeon.exe`).
* `os` / `cpu` fields restrict to supported platforms.

---

# 3. `checksums.json`

this file should be generated during your CI release pipeline. example:

```json
{
  "v1.0.0": {
    "darwin-x64": "abc123sha...",
    "darwin-arm64": "def456sha...",
    "linux-x64": "ghi789sha...",
    "linux-arm64": "jkl012sha...",
    "win32-x64": "mno345sha...",
    "win32-arm64": "pqr678sha..."
  }
}
```

---

# 4. `postinstall.js`

```js
#!/usr/bin/env node
const os = require("os");
const path = require("path");
const fs = require("fs");
const crypto = require("crypto");
const { execSync } = require("child_process");
const https = require("https");

const version = require("./package.json").version;
const checksums = require("./checksums.json")[`v${version}`];

const platform = os.platform(); // 'darwin', 'linux', 'win32'
const arch = os.arch(); // 'x64', 'arm64'

const key = `${platform}-${arch}`;
const expected = checksums[key];
if (!expected) {
  console.error(`No binary available for ${platform}-${arch}`);
  process.exit(1);
}

const repo = "pidgeon-health/pidgeon"; // replace with your repo
const filename =
  platform === "win32"
    ? `pidgeon-${platform}-${arch}.zip`
    : `pidgeon-${platform}-${arch}.tar.gz`;
const url = `https://github.com/${repo}/releases/download/v${version}/${filename}`;

const outDir = path.join(__dirname, "pidgeon-bin");
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir);

const outFile = path.join(outDir, filename);

function download(url, dest, cb) {
  const file = fs.createWriteStream(dest);
  https
    .get(url, (res) => {
      res.pipe(file);
      file.on("finish", () => file.close(cb));
    })
    .on("error", (err) => {
      fs.unlinkSync(dest);
      cb(err.message);
    });
}

function verifyChecksum(file, expected) {
  const data = fs.readFileSync(file);
  const hash = crypto.createHash("sha256").update(data).digest("hex");
  if (hash !== expected) {
    throw new Error(`Checksum mismatch! expected ${expected}, got ${hash}`);
  }
}

download(url, outFile, (err) => {
  if (err) {
    console.error(`Download failed: ${err}`);
    process.exit(1);
  }
  try {
    verifyChecksum(outFile, expected);
    console.log("Checksum verified ✅");

    // unzip/tar extract
    if (platform === "win32") {
      execSync(`powershell -Command "Expand-Archive -Force '${outFile}' '${outDir}'"`);
    } else {
      execSync(`tar -xzf ${outFile} -C ${outDir}`);
    }

    // make executable on *nix
    const binPath = path.join(outDir, "pidgeon");
    if (platform !== "win32") {
      fs.chmodSync(binPath, 0o755);
    }

    console.log("Installed Pidgeon CLI 🎉");
  } catch (e) {
    console.error(e);
    process.exit(1);
  }
});
```

---

# 5. CI integration

* in your **release workflow**, after building binaries:

  1. generate `checksums.json` from the `sha256sum` file and commit it into the npm wrapper repo.
  2. bump `package.json` version to match release tag.
  3. `npm publish` the wrapper.

---

# flow for a user

```bash
npm install -g @pidgeon/cli
pidgeon --version
```

they get the latest GitHub release, verified against your `checksums.json`.