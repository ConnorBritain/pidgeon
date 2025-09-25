# The mental model (high-level)

Imagine your CLI is a product rolling off a factory line:

* **Factory** = your GitHub Actions build. It turns source into **sealed boxes** (binaries per OS/arch).
* **Shipping hub** = your GitHub Release. All couriers know to pick up packages from here.
* **Couriers** = package managers: npm, Homebrew, apt (Debian/Ubuntu), yum/dnf (RHEL/Fedora), winget (Windows). Each has its own intake form (manifests/formula/specs).
* **Store shelves** = users’ machines. They either:

  * let the OS/package manager periodically restock (**auto-update via the manager**), or
  * ask your tool to check and guide them (**on-run update checks**), or
  * let your tool replace itself (**self-update**) — **only** if it was installed by direct download.

The most important principle: **single source of truth = Git tags + GitHub Releases**. Everything (brew formula, apt repo metadata, winget manifests, npm version) should derive from (and therefore be triggered by) cutting a semver tag like `v1.2.3`.

## Update strategy that actually works in the real world

* **If installed via a package manager**, updates should flow **through that manager** (e.g., `brew upgrade`, `winget upgrade`, `apt upgrade`, `npm -g update`). This keeps your users in a consistent, secure world (checks, signatures, rollbacks).
* **If installed via a plain tar/zip download**, provide a **`mycli update` self-update** command that downloads the right artifact from GitHub Releases, verifies checksum/signature, and atomically replaces the binary (using a tiny helper “updater” process on Windows, because you can’t overwrite a running exe).
* Your CLI can **politely check for updates on launch** (e.g., once a day) if the user opts in: show “A new version X is available — run `mycli update` (or `brew upgrade mycli`)”.

This hybrid approach avoids fighting package managers while still giving a smooth, cross-platform update story.

---

# The concrete architecture (still high-level, but precise)

1. **Build once per OS/arch** via `.NET 8`:

   * RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.
   * Self-contained single-file builds: no .NET runtime required for users.
   * Outputs: versioned tar.gz/zip per platform, plus checksums and optional signatures.

2. **Publish a GitHub Release on tag push**:

   * Upload all artifacts + a `checksums.txt` and (optionally) signatures.

3. **Homebrew (macOS/Linux)**:

   * Maintain a small **tap** repo (`yourorg/homebrew-tap`) with a formula that points to your release tarball + SHA256.
   * A GitHub Action bumps the formula on each tag.

4. **apt (Debian/Ubuntu) & yum/dnf (RHEL/Fedora)**:

   * Build `.deb` and `.rpm` from the same artifacts using **nfpm** (simpler) or **fpm**.
   * Host repos on a managed service like **Cloudsmith** (easy, reliable) so users `apt-add-repository` once and then just `apt upgrade`.
   * The CI uploads new packages to the repo on each tag.

5. **Windows (winget)**:

   * On each release, auto-generate winget manifests (pointing to your GitHub artifact) and open a PR to the **winget-pkgs** repo via `wingetcreate`. Users update with `winget upgrade yourcli`.

6. **npm (for global installs)**:

   * Publish a tiny **wrapper npm package** (`yourcli`) whose `postinstall` script downloads the right binary from your GitHub Release to the package’s `bin` dir. `package.json`’s `bin` field exposes `yourcli`.
   * Users can `npm i -g yourcli` and update with `npm i -g yourcli@latest`.

7. **Update checker inside your CLI**:

   * Config file (opt-in): `$XDG_CONFIG_HOME/yourcli/config.json` (Linux), `~/Library/Application Support/yourcli/config.json` (macOS), `%AppData%\yourcli\config.json` (Windows). Keys like:

     ```json
     { "update": { "mode": "check|auto|off", "frequencyHours": 24, "channel": "stable|prerelease" } }
     ```
   * On run, if `mode != off` and the last check is older than N hours, do a fast HTTP GET to GitHub Releases (use ETag/If-None-Match) to see if a newer version exists.
   * If installed through a package manager (see below), recommend the right command; otherwise offer `mycli update` self-update.

8. **Detecting install channel**:

   * During install, drop a tiny `install.json` next to the binary (or in config dir), e.g. `{ "channel": "homebrew" }`. Each package recipe (brew formula, nfpm, npm postinstall, winget manifest installer) writes a different channel string. Your CLI reads this to decide whether to self-update or delegate to the manager.

9. **Security**:

   * Always verify SHA256 of downloaded artifacts.
   * Optionally sign artifacts (e.g., **cosign** or GPG) and verify in self-update.
   * Use HTTPS with timeouts, handle partial downloads, and roll back on failure.

---

# Low-level, step-by-step implementation plan

Below is a concrete plan you can follow. You can copy-paste most of it with minimal edits.

## 0) Repository layout

```
/src/MyCli/                 # your C# project
/build/                      # scripts, shared helpers
/packaging/
  nfpm.yaml                  # deb/rpm recipe
  homebrew/Formula/mycli.rb  # auto-updated by CI
  winget/                    # generated manifests (CI will PR)
  npm/                       # the wrapper npm package
/scripts/
  compute_checksums.sh
  infer_channel.sh
  install_channel_marker.(ps1|sh)
.github/workflows/
  ci.yml                     # build+test on PR
  release.yml                # tag → build → release → publish to all channels
```

## 1) .NET publish (single-file, self-contained)

**`Directory.Build.props`** (ensures consistent publish flags):

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>false</PublishTrimmed> <!-- safer for reflection-heavy libs -->
    <InvariantGlobalization>true</InvariantGlobalization>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <DebugType>embedded</DebugType>
  </PropertyGroup>
</Project>
```

**Build script snippet** (bash):

```bash
#!/usr/bin/env bash
set -euo pipefail

APP=yourcli
VER="${1:?version required (e.g., 1.2.3)}"

rids=(win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64)

rm -rf out && mkdir -p out
for rid in "${rids[@]}"; do
  dotnet publish src/MyCli/MyCli.csproj -c Release -r "$rid" \
    /p:Version="$VER" \
    -o "out/$APP-$VER-$rid"
  pushd "out/$APP-$VER-$rid"
    # channel marker is added by package scripts; for plain archives we default to "direct"
    tar czf "../$APP-$VER-$rid.tar.gz" *
  popd
done

# checksums
pushd out
  for f in *.tar.gz; do shasum -a 256 "$f"; done > checksums.txt
popd
```

> On Windows runners use `7z`/`powershell` for zips and `Get-FileHash -Algorithm SHA256`.

## 2) GitHub Actions: CI and Release

**`.github/workflows/ci.yml`** — build/test on PRs:

```yaml
name: ci
on:
  pull_request:
  push:
    branches: [ main ]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
      - run: dotnet test --configuration Release --no-build --logger trx
```

**`.github/workflows/release.yml`** — tag → artifacts → release → downstream publishing:

```yaml
name: release
on:
  push:
    tags:
      - 'v*.*.*'

permissions:
  contents: write      # create GitHub release, update tap repo
  packages: write      # (if needed)
  pull-requests: write # for winget PR
  id-token: write      # for cosign if you use it

jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.meta.outputs.version }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - id: meta
        run: echo "version=${GITHUB_REF_NAME#v}" >> $GITHUB_OUTPUT
      - run: bash build/build_all.sh ${{ steps.meta.outputs.version }}
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: dist
          path: out/*

  release:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with: { name: dist, path: dist }
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ github.ref_name }}
          name: ${{ github.ref_name }}
          files: dist/*
          draft: false
          prerelease: ${{ contains(github.ref_name, '-') }} # treat v1.2.3-rc.1 as prerelease

  homebrew:
    needs: release
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { repository: yourorg/homebrew-tap, token: ${{ secrets.TAP_PAT }} }
      - name: Update formula
        run: |
          VERSION=${GITHUB_REF_NAME#v}
          TARBALL="https://github.com/yourorg/yourrepo/releases/download/v$VERSION/yourcli-$VERSION-osx-arm64.tar.gz"
          SHA=$(curl -fsSL "$TARBALL" | shasum -a 256 | cut -d' ' -f1)
          # generate mycli.rb from a template replacing VERSION/URL/SHA (shown below)
          ./update_formula.sh "$VERSION" "$SHA"
      - name: Commit & push
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add .
          git commit -m "yourcli ${{ github.ref_name }}"
          git push

  nfpm-deb-rpm:
    needs: release
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: |
          curl -sSfL https://github.com/goreleaser/nfpm/releases/latest/download/nfpm_$(uname -s)_$(uname -m).tar.gz | tar xz
      - uses: actions/download-artifact@v4
        with: { name: dist, path: dist }
      - name: Build deb/rpm from nfpm
        run: |
          ./nfpm package --packager deb --config packaging/nfpm.yaml --target dist/
          ./nfpm package --packager rpm --config packaging/nfpm.yaml --target dist/
      - name: Push to Cloudsmith (example)
        env:
          CLOUDSMITH_API_KEY: ${{ secrets.CLOUDSMITH_API_KEY }}
        run: |
          cloudsmith push deb yourorg/yourrepo/ubuntu/jammy dist/*.deb
          cloudsmith push rpm yourorg/yourrepo/rpm/el/9 dist/*.rpm

  winget:
    needs: release
    runs-on: windows-latest
    steps:
      - name: WingetCreate
        shell: pwsh
        run: |
          dotnet tool install --global wingetcreate
          $env:Path += ";$env:USERPROFILE\.dotnet\tools"
          wingetcreate submit \
            --urls https://github.com/yourorg/yourrepo/releases/download/${{ github.ref_name }}/yourcli-${{ needs.build.outputs.version }}-win-x64.zip \
            --version ${{ needs.build.outputs.version }} \
            --publisher "Your Org" --packageIdentifier "YourOrg.YourCli" \
            --token "${{ secrets.GITHUB_TOKEN }}" --pr
```

> Notes:
>
> * You’ll supply `TAP_PAT` (a Personal Access Token) that can push to your tap repo.
> * Replace Cloudsmith lines with your chosen host or skip if you use another service.
> * For `winget`, the command above is illustrative; you may prefer the GitHub Action wrapper.

## 3) Homebrew formula template (`packaging/homebrew/Formula/mycli.rb`)

CI will update the URL/SHA/version for each architecture. A simplified universal formula (you can add multiple `on_macos`/`on_linux` blocks):

```ruby
class Mycli < Formula
  desc "Your awesome CLI"
  homepage "https://github.com/yourorg/yourrepo"
  version "1.2.3"

  on_macos do
    on_arm do
      url "https://github.com/yourorg/yourrepo/releases/download/v1.2.3/yourcli-1.2.3-osx-arm64.tar.gz"
      sha256 "REPLACED_BY_CI"
    end
    on_intel do
      url "https://github.com/yourorg/yourrepo/releases/download/v1.2.3/yourcli-1.2.3-osx-x64.tar.gz"
      sha256 "REPLACED_BY_CI"
    end
  end

  on_linux do
    on_arm do
      url "https://github.com/yourorg/yourrepo/releases/download/v1.2.3/yourcli-1.2.3-linux-arm64.tar.gz"
      sha256 "REPLACED_BY_CI"
    end
    on_intel do
      url "https://github.com/yourorg/yourrepo/releases/download/v1.2.3/yourcli-1.2.3-linux-x64.tar.gz"
      sha256 "REPLACED_BY_CI"
    end
  end

  def install
    bin.install "yourcli"
    (etc/"yourcli").mkpath
    (etc/"yourcli/install.json").write <<~EOS
      { "channel": "homebrew" }
    EOS
  end

  test do
    system "#{bin}/yourcli", "--version"
  end
end
```

## 4) nfpm config for `.deb` and `.rpm` (`packaging/nfpm.yaml`)

```yaml
name: yourcli
arch: ${ARCH}         # CI will set ARCH per package; e.g., amd64, arm64, x86_64, aarch64
platform: linux
version: ${VERSION}   # CI injects
section: utils
priority: extra
maintainer: Your Name <you@example.com>
description: Your awesome CLI
homepage: https://github.com/yourorg/yourrepo
license: MIT
contents:
  - src: dist/yourcli-${VERSION}-linux-${ARCH}/yourcli
    dst: /usr/bin/yourcli
    file_info:
      mode: 0755
  - src: packaging/install_channel_apt.json
    dst: /etc/yourcli/install.json
scripts:
  postinstall: packaging/postinstall.sh
overrides:
  deb:
    depends: []
  rpm:
    depends: []
```

`packaging/postinstall.sh` could ensure permissions and maybe print a “installed via apt/yum” message.

`packaging/install_channel_apt.json`:

```json
{ "channel": "apt" }
```

Similar for RPM.

## 5) npm wrapper package (`/packaging/npm/`)

`package.json`:

```json
{
  "name": "yourcli",
  "version": "1.2.3",
  "description": "Wrapper for the yourcli native binary",
  "bin": { "yourcli": "bin/yourcli" },
  "scripts": {
    "postinstall": "node postinstall.js"
  },
  "publishConfig": { "access": "public" }
}
```

`postinstall.js` (downloads the correct tarball from GitHub Release, extracts to `bin/`):

```js
#!/usr/bin/env node
const os = require('os');
const path = require('path');
const fs = require('fs');
const https = require('https');
const { execSync } = require('child_process');
const version = require('./package.json').version;

function rid() {
  const plat = os.platform();
  const arch = os.arch();
  if (plat === 'win32') return arch === 'arm64' ? 'win-arm64' : 'win-x64';
  if (plat === 'darwin') return arch === 'arm64' ? 'osx-arm64' : 'osx-x64';
  if (plat === 'linux') return arch === 'arm64' ? 'linux-arm64' : 'linux-x64';
  throw new Error(`Unsupported platform/arch: ${plat}/${arch}`);
}

const base = `https://github.com/yourorg/yourrepo/releases/download/v${version}`;
const file = `yourcli-${version}-${rid()}.tar.gz`;
const url = `${base}/${file}`;
const destDir = path.join(__dirname, 'bin');
const destPath = path.join(destDir, file);

fs.mkdirSync(destDir, { recursive: true });

https.get(url, res => {
  if (res.statusCode !== 200) {
    console.error(`Failed to download ${url}: ${res.statusCode}`);
    process.exit(1);
  }
  const fileStream = fs.createWriteStream(destPath);
  res.pipe(fileStream);
  fileStream.on('finish', () => {
    fileStream.close();
    // extract
    execSync(`tar -xzf "${destPath}" -C "${destDir}"`);
    // set executable bit (unix)
    try { fs.chmodSync(path.join(destDir, 'yourcli'), 0o755); } catch {}
    // write channel marker
    fs.writeFileSync(path.join(destDir, 'install.json'), JSON.stringify({ channel: 'npm' }));
    // shim for Windows: create a .cmd launcher
    if (process.platform === 'win32') {
      fs.writeFileSync(path.join(destDir, 'yourcli.cmd'), '@echo off\r\n"%~dp0yourcli.exe" %*\r\n');
    }
    console.log(`Installed yourcli ${version} for ${rid()}`);
  });
}).on('error', err => {
  console.error(err);
  process.exit(1);
});
```

In `package.json`, the `bin` can point to `bin/yourcli` (Unix) and `bin/yourcli.cmd` (Windows); npm handles `.cmd` automatically when global.

**CI step to publish npm** (in `release.yml` add a job):

```yaml
  npm:
    needs: release
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: |
          cd packaging/npm
          npm version ${{ needs.build.outputs.version }} --no-git-tag-version
          npm publish
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## 6) Windows winget manifest (CI-generated)

You’ll rely on `wingetcreate` (as shown) to generate/update manifests that point to your `win-x64`/`win-arm64` zips and submit a PR automatically.

## 7) The CLI’s update subsystem (C#)

Key behaviors:

* Detect channel (read `install.json`).
* If `channel != direct`, suggest the manager command rather than self-update.
* Implement `yourcli update --check`, `yourcli update`, `yourcli update --channel prerelease`.
* Cache the latest release info with ETag to avoid rate limits.

**Config file paths**:

```csharp
static string ConfigDir() =>
    OperatingSystem.IsWindows()
      ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "yourcli")
      : OperatingSystem.IsMacOS()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "yourcli")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "yourcli");
```

**Release check**:

```csharp
record ReleaseInfo(string TagName, bool Prerelease, Asset[] Assets);
record Asset(string Name, string BrowserDownloadUrl);

static async Task<ReleaseInfo?> GetLatestReleaseAsync(HttpClient http, bool includePrerelease)
{
    using var req = new HttpRequestMessage(HttpMethod.Get,
      includePrerelease
        ? "https://api.github.com/repos/yourorg/yourrepo/releases"
        : "https://api.github.com/repos/yourorg/yourrepo/releases/latest");

    req.Headers.UserAgent.ParseAdd("yourcli-update-check");
    using var res = await http.SendAsync(req);
    res.EnsureSuccessStatusCode();
    var json = await res.Content.ReadAsStringAsync();

    using var doc = System.Text.Json.JsonDocument.Parse(json);
    if (!includePrerelease)
    {
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString()!;
        var prerelease = root.GetProperty("prerelease").GetBoolean();
        var assets = root.GetProperty("assets").EnumerateArray().Select(a =>
            new Asset(a.GetProperty("name").GetString()!,
                      a.GetProperty("browser_download_url").GetString()!)).ToArray();
        return new ReleaseInfo(tag, prerelease, assets);
    }
    else
    {
        // first non-draft release (including prerelease) at index 0
        var rel = doc.RootElement.EnumerateArray()
                    .FirstOrDefault(e => e.TryGetProperty("draft", out var d) && !d.GetBoolean());
        if (rel.ValueKind == System.Text.Json.JsonValueKind.Undefined) return null;
        var tag = rel.GetProperty("tag_name").GetString()!;
        var prerelease = rel.GetProperty("prerelease").GetBoolean();
        var assets = rel.GetProperty("assets").EnumerateArray().Select(a =>
            new Asset(a.GetProperty("name").GetString()!,
                      a.GetProperty("browser_download_url").GetString()!)).ToArray();
        return new ReleaseInfo(tag, prerelease, assets);
    }
}
```

**Choosing the right asset by RID**:

```csharp
static string CurrentRid() =>
    OperatingSystem.IsWindows()
      ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64")
      : OperatingSystem.IsMacOS()
        ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64")
        : (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");
```

**Self-update (direct installs only)**:

```csharp
static async Task<bool> SelfUpdateAsync(ReleaseInfo rel)
{
    var rid = CurrentRid();
    var asset = rel.Assets.FirstOrDefault(a => a.Name.Contains(rid) && a.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
                                            || a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    if (asset is null) { Console.Error.WriteLine("No asset for current platform"); return false; }

    var exePath = Environment.ProcessPath!;
    var workDir = Path.Combine(Path.GetTempPath(), "yourcli-update-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workDir);
    var archive = Path.Combine(workDir, asset.Name);

    using var http = new HttpClient(); http.DefaultRequestHeaders.UserAgent.ParseAdd("yourcli-updater");
    await using (var s = await http.GetStreamAsync(asset.BrowserDownloadUrl))
    await using (var f = File.Create(archive))
        await s.CopyToAsync(f);

    // Verify checksum if you published checksums.txt (left out here for brevity)
    // Extract:
    if (asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        System.IO.Compression.ZipFile.ExtractToDirectory(archive, workDir);
    else
        ExtractTarGz(archive, workDir); // implement or shell out to tar

    var newBin = Directory.GetFiles(workDir, OperatingSystem.IsWindows() ? "yourcli*.exe" : "yourcli").First();

    if (OperatingSystem.IsWindows())
    {
        // spawn helper to replace after exit
        var updater = Path.Combine(workDir, "updater.exe");
        File.WriteAllBytes(updater, Properties.Resources.UpdaterStub); // embed a tiny helper
        var cmd = $"\"{updater}\" \"{exePath}\" \"{newBin}\"";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", "/c " + cmd) { CreateNoWindow = true });
        Console.WriteLine("Updating... please re-run yourcli.");
        return true;
    }
    else
    {
        File.Copy(newBin, exePath, overwrite: true); // on Linux/mac we can overwrite
        Console.WriteLine("Updated. Version now: " + rel.TagName);
        return true;
    }
}
```

(For brevity the tar extraction and Windows updater stub aren’t fully shown, but this is the pattern: the helper waits for the parent PID to exit, then replaces the file and exits with a code.)

**Delegating to the installed channel**:

```csharp
static string? DetectChannel()
{
    // Look beside the binary first
    var exeDir = Path.GetDirectoryName(Environment.ProcessPath!)!;
    var paths = new[]{
      Path.Combine(exeDir, "install.json"),
      Path.Combine("/etc/yourcli", "install.json"),
      Path.Combine(ConfigDir(), "install.json")
    };
    foreach (var p in paths) if (File.Exists(p))
      return System.Text.Json.JsonDocument.Parse(File.ReadAllText(p)).RootElement.GetProperty("channel").GetString();
    return "direct";
}

static int RunUpdate(bool checkOnly)
{
    var channel = DetectChannel();
    if (channel is "homebrew")
    {
        Console.WriteLine("Installed via Homebrew. Update with: brew update && brew upgrade yourcli");
        return 0;
    }
    if (channel is "apt")
    {
        Console.WriteLine("Installed via apt. Update with: sudo apt update && sudo apt install --only-upgrade yourcli");
        return 0;
    }
    if (channel is "rpm")
    {
        Console.WriteLine("Installed via dnf/yum. Update with: sudo dnf upgrade yourcli");
        return 0;
    }
    if (channel is "npm")
    {
        Console.WriteLine("Installed via npm. Update with: npm i -g yourcli@latest");
        return 0;
    }
    if (channel is "winget")
    {
        Console.WriteLine("Installed via winget. Update with: winget upgrade yourcli");
        return 0;
    }

    // direct install -> self-update
    using var http = new HttpClient();
    var rel = GetLatestReleaseAsync(http, includePrerelease:false).GetAwaiter().GetResult();
    if (rel is null) { Console.WriteLine("No release found"); return 1; }

    var current = typeof(Program).Assembly.GetName().Version!;
    var latest = System.Version.Parse(rel.TagName.TrimStart('v'));
    if (latest <= current) { Console.WriteLine($"Already up to date ({current})"); return 0; }

    if (checkOnly) { Console.WriteLine($"Update available: {current} -> {latest}"); return 0; }
    return SelfUpdateAsync(rel).GetAwaiter().GetResult() ? 0 : 1;
}
```

## 8) Semantic versioning and version derivation

* Tag releases `vMAJOR.MINOR.PATCH` (optionally `-rc.1`).
* Use a versioning tool (e.g., **MinVer** or **GitVersion**) to propagate the tag into `AssemblyInformationalVersion` automatically, or pass `/p:Version` in publish (as shown).
* Cut releases only via tags; everything else flows from that.

## 9) Optional: code signing / notarization

* **Windows**: sign `yourcli.exe` with an EV code signing cert (optional but reduces SmartScreen friction).
* **macOS**: `codesign` and **notarize** if you distribute standalone. For Homebrew formula installs (untar into `/usr/local/bin`), codesign is optional.
* **Linux**: rely on repo GPG + RPM/DEB signing (nfpm can sign; your repo host often manages GPG).

## 10) User docs (one page)

Provide a simple install page:

* **Homebrew**: `brew tap yourorg/tap && brew install yourcli`
* **Debian/Ubuntu**: repo setup (from Cloudsmith) + `sudo apt install yourcli`
* **RHEL/Fedora**: repo setup + `sudo dnf install yourcli`
* **Windows**: `winget install YourOrg.YourCli`
* **npm (global)**: `npm i -g yourcli`
* **Direct**: link to Releases page.

Explain update behavior and `yourcli config set update.mode=check|auto|off`.

---

# Practical trade-offs & pitfalls (learned the hard way)

* **Don’t let self-update run if installed via a package manager.** You’ll confuse the package DB and break uninstalls.
* **Keep artifacts’ names stable and predictable** (the postinstall/winget/brew logic depends on them).
* **ETag caching** for update checks prevents hitting GitHub rate limits.
* **Windows replacement** must be done by a separate process; plan the helper stub now.
* **Hosting Linux repos yourself is non-trivial**; use Cloudsmith/packagecloud unless you truly need DIY.
* **Prereleases**: ensure your update code respects `channel` (stable vs prerelease).

---

# Your “one-sprint” checklist

1. Add publish flags & RID matrix; verify single-file binaries work on each OS.
2. Write build script + checksums; dry-run locally.
3. Create `release.yml` and publish a test tag `v0.1.0`.
4. Set up Homebrew tap repo; wire the formula updater.
5. Add nfpm, build `.deb`/`.rpm`, pick a repo host (Cloudsmith), upload from CI.
6. Add npm wrapper; obtain `NPM_TOKEN`; publish from CI.
7. Add winget automation; test PR flow to `winget-pkgs`.
8. Implement `yourcli update` path + channel detection + config file.
9. Add optional artifact signature & verification in self-update.
10. Write and publish a crisp install/update document.

---

## Quick “hello world” of the user experience

* First run:

  ```
  $ yourcli --version
  1.2.3
  ```
* A week later (auto-check on launch, if enabled):

  ```
  A new version 1.3.0 is available.
  Installed via Homebrew → run: brew update && brew upgrade yourcli
  (To disable checks: yourcli config set update.mode=off)
  ```
* For direct installs:

  ```
  $ yourcli update
  Downloading 1.3.0 (linux-x64)…
  Verified sha256.
  Updated. Restart yourcli.
  ```

---

# Where do you want to start?

If you like, I can tailor a first PR for your repo structure and the `release.yml` that builds and uploads the artifacts. But before that, I want to gauge your comfort with a few prerequisites so I can fill any gaps cleanly:

1. **.NET publishing & RIDs**

* What does `SelfContained=true` actually include in your binary, and how does it change file size/runtime behavior across `win-x64` vs `linux-arm64`?
* Why is `PublishTrimmed` risky for reflection-heavy code, and how would you annotate code to make trimming safe?

2. **Package managers**

* In Homebrew formulae, why do we need distinct URL/SHA blocks per `on_macos on_arm/on_intel` rather than a single universal tarball?
* For apt/yum, what’s the difference between **package** signing and **repository** signing, and which one gives users `apt update` trust?

3. **Update mechanics**

* On Windows, why can’t we overwrite a running exe and what process lifecycle trick reliably replaces it without admin rights?
* How would you implement an **ETag** cache for GitHub API requests, and what HTTP status code indicates “no new version”?

4. **Security**

* If you publish `checksums.txt`, how do you prevent a MitM from swapping both the binary and the checksums during a self-update?
* Compare cosign keyless signatures vs GPG — operationally, which is simpler to automate in CI and why?

5. **Winget/npm**

* How does the winget PR process work when you cut `v1.2.4` — what does success look like, and what’s the fastest way to retry if the manifest validation fails?
* In the npm wrapper, how does `package.json`’s `bin` mapping behave on Windows vs Unix, and how do we ensure the `.cmd` shim is used correctly?

Give me your answers (guesses are fine). Based on where you feel less confident, I’ll zoom in with deeper, analogy-heavy explanations and wire up the exact YAML/scripts you’d ship.
