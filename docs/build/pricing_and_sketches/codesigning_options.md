# phase 0: best-for-free (ship this week)

### targets covered

* **macOS + Linux** via **Homebrew tap** and **direct download**
* **Windows** via **winget** and **portable/ZIP** (unsigned → warnings possible)
* **All** via **GitHub Releases** as single source of truth
* **Integrity** via **SHA-256 + optional GPG signatures** (free)

### what you’ll do

1. **build once, ship everywhere**

* Produce self-contained single-file binaries for: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`. Keep Git tags + GitHub Releases as the source of truth. Include a `checksums.txt`.&#x20;

2. **homebrew tap (macOS/Linux) — no paid certs required**

* Create `pidgeon-health/homebrew-tap` formula pointing to your GitHub tarballs + SHA256.
* CI updates the formula on each tag. Users: `brew tap pidgeon-health/tap && brew install pidgeon`.&#x20;

3. **winget (Windows) — unsigned is allowed, hash-verified**

* Use `wingetcreate` in CI to open a PR to `microsoft/winget-pkgs` each release.
* Users can install/upgrade with `winget install/upgrade PidgeonHealth.Pidgeon`. (Windows may still show “Unknown publisher”/SmartScreen prompts for first-time runs because binaries are **unsigned**.)&#x20;

4. **npm global wrapper (optional, free)**

* Publish a tiny npm package that, in `postinstall`, downloads the right binary from GitHub Releases and verifies its SHA256 before putting it on PATH.&#x20;

5. **direct download (curl) + checksums**

* Put an `install.sh` that detects OS/arch, fetches the correct asset from Releases, verifies SHA-256, and installs to `/usr/local/bin`. Use a `latest` pointer (file or redirect). (You can host on GitHub Pages now; later you can move to `get.pidgeon.health`.)&#x20;

6. **optional but great: GPG-sign your artifacts**

* Generate a GPG key, publish your public key, and attach `.sig` files to releases. This is free and standard on Linux.&#x20;

> why this works: brew + winget validate **hashes**; users get fast installs without you paying day-one. macOS CLIs installed by Homebrew generally run fine without notarization; Windows installs via winget work but **may show warnings** until you add Authenticode. The integrity story (SHA-256 + optional GPG) is solid from day one.&#x20;

---

# phase 1: lowest-cost trust upgrade (when you’re ready)

### goal

remove the biggest user-facing friction (Windows SmartScreen, macOS Gatekeeper), while keeping spend tight.

### the minimal paid bundle

1. **Windows OV code-signing cert (\~\$130–\$300/yr)**

* Sign your EXE/MSI so users see a verified publisher and fewer SmartScreen scares.
* Start with **OV** (Organization Validation) to keep cost down; upgrade to **EV** later if you need kernel drivers or want “instant” SmartScreen reputation.
* Wire into CI (`signtool`) with **timestamping**.

2. **Apple Developer Program (\$99/yr) → Developer ID + notarization**

* Sign and **notarize** your macOS binary; staple the ticket. This removes Gatekeeper warnings for users installing outside the App Store.
* Wire into CI (`codesign`, `xcrun notarytool`, `xcrun stapler`).

3. **(optional) apt/yum repos with GPG (Cloudsmith or later)**

* Stay free by pushing Linux through Homebrew + direct script for now, or add a professional apt/yum repo when you want enterprise polish/audit logs.&#x20;

### rough budget (+ what to buy first)

* Month 1–2: **Windows OV** (\~\$129–\$300) → immediate UX improvement for winget/EXE.
* Month 2–3: **Apple Dev** (\$99) → clean macOS experience.
* Month 4–6: optional **Cloudsmith** for apt/yum hosting; or keep free channels until traction requires it.

---

# concrete 30-day checklist

**week 1 (free)**

* Tag → GitHub Release pipeline with multi-RID artifacts + `checksums.txt`.&#x20;
* Homebrew tap repo + CI bump job.&#x20;
* Winget submit PR from CI with `wingetcreate`.&#x20;
* npm wrapper (optional).&#x20;
* (Nice) GPG key + `.sig` files.&#x20;

**week 2–3 (free polish)**

* Add **update checker** in the CLI (opt-in) and a `pidgeon update` self-update for direct installs. Detect channel (brew/winget/npm/direct) and advise the right upgrade command.&#x20;

**week 3–4 (paid, minimal)**

* Purchase **Windows OV** code-signing; integrate `signtool` + timestamp server in CI.
* Test winget + direct EXE on a clean Windows VM—warnings should drop noticeably.
* If budget allows, enroll **Apple Developer** and add `codesign` + `notarytool` to CI; test on a clean mac.

---

# how to actually do each piece (fast copy/paste)

* **release workflow scaffold** (tag → build matrix → upload → release → tap/winget automations) is laid out here; adapt directly:&#x20;
* **homebrew formula + auto-bump** examples are in your plan:&#x20;
* **winget submit** step example is in your CI outline:&#x20;
* **shell completions auto-enable** (brew/postinst, etc.) are ready to wire in now:&#x20;
* **domain split** (later): `get.pidgeon.health` for scripts/assets, `packages.pidgeon.health` for apt/yum (Cloudsmith). Not needed for day-one, but the blueprint’s ready:&#x20;

---

# what to have ready for the paid step (so purchase goes smoothly)

### windows OV code-signing (first purchase)

* **org legal name** exactly as registered
* **registered business address & phone** (you’ll get a verification call)
* **proof of existence** (articles of incorporation / state registration)
* **CSR / key plan** (many CAs require keys on a hardware token/HSM; they’ll ship one if needed)
* **Admin who can respond** to CA verification emails/calls quickly

### apple developer program (\$99/yr)

* **D-U-N-S number** (free from Dun & Bradstreet if you don’t have one)
* **org legal docs** and a person with authority to enroll
* A **mac** with Xcode CLI tools to generate cert requests and run `notarytool`

### gpg (free)

* Decide **email/identity** to attach to the key (e.g., `release@pidgeon.health`)
* A secure place to **store/backup the private key** + strong passphrase
* Publish your **public key** on your docs and GitHub org

---

# recommended “starter” vendor picks (cheap but reputable)

> you can absolutely price-shop, but these are commonly used & straightforward:

* **Windows OV cert (budget)**: SSL.com (OV) — often \~**\$129/yr**; integrates fine with `signtool`.
* **Windows OV/EV (mid-tier)**: Sectigo (via reputable resellers) — often **\$250–400/yr** OV; EV a bit more.
* **Apple**: Apple Developer Program — **\$99/yr** direct with Apple (no middlemen).

(when you’re ready to step up to **EV** for instant SmartScreen reputation or driver signing, you can upgrade later—don’t overpay month one.)

---

## tl;dr

* **ship now for free**: GitHub Releases + checksums (+ GPG), Homebrew tap, winget manifests, optional npm wrapper. Expect Windows warnings until you sign.&#x20;
* **first paid upgrade**: **Windows OV** cert → biggest UX win for minimal spend.
* **second paid**: **Apple Developer** → clean macOS notarization.
* **later**: apt/yum repo hosting; EV/WHQL only if you move into drivers or want instant reputation.