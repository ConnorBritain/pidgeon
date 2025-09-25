Absolutely—let’s make *pidgeon.health* work the way you want:

* **Root domain** = marketing site (Wix or Squarespace).
* **get.pidgeon.health** = fast, script‑friendly downloads + `install.sh`.
* **packages.pidgeon.health** = apt/yum repositories (Cloudsmith).
* **docs.pidgeon.health** = Mintlify docs.

Below is a simple, battle‑tested architecture and a step‑by‑step you can follow. I’ll show both **how to wire DNS** and **where to host what**, with curl‑friendly URLs, checksums, TLS, and caching.

---

## 0) Topology blueprint (what goes where)

* **DNS provider**: Use **Cloudflare DNS** for the whole zone `pidgeon.health`. It gives you fast DNS, easy subdomain management, and built‑in TLS for the subdomains you proxy through Cloudflare.
* **Root site (`pidgeon.health`)**: Wix *or* Squarespace (more on exact records below).
* **Docs (`docs.pidgeon.health`)**: **Mintlify** (map CNAME to Mintlify’s target). Mintlify’s docs show: set **`CNAME docs → cname.vercel-dns.com`** and if Cloudflare is your DNS, set SSL mode to **Full (strict)**. ([Mintlify][1])
* **Downloads (`get.pidgeon.health`)**: Host static files + `install.sh` on a CDN‑backed object store. Easiest path: **Cloudflare R2 + Pages/Worker** with a **custom domain** mapped to your bucket. Cloudflare’s tutorial covers pointing a custom domain to an **R2 bucket**; you can also attach a custom domain to R2 via API. ([Cloudflare Docs][2])
* **Packages (`packages.pidgeon.health`)**: **Cloudsmith** (apt/yum). Cloudsmith supports **custom domains** (Velocity/Ultra plans) and will give you **two CNAMEs** to add (one to authorize and one for traffic). You choose account‑wide or per‑repo custom domain. ([Cloudsmith Help][3])

> Why separate `get.` and `packages.`?
> `get.` serves human‑friendly installers and release assets. `packages.` serves Debian/RPM repos via apt/yum with GPG and proper repo metadata. Separation keeps each channel clean and auditable (and lets you move services later without breaking others).

---

## 1) Root domain on a website builder

### Option A — **Wix** as the marketing site (root)

1. Keep DNS at Cloudflare (recommended), and **connect to Wix via “pointing”** (not nameserver change). This keeps control of all subdomains in Cloudflare. Wix’s “pointing method” guide explains the flow. ([Wix Help Center][4])
2. In Cloudflare DNS:

   * Set the A and CNAME **values Wix shows in your Wix dashboard**. Wix docs (SSL troubleshooting) show a common pattern when pointing:

     * **A (apex `@`) → `185.230.63.107`**
     * **CNAME `www` → `pointing.wixdns.net`**
       (Wix sometimes varies these; use the exact values Wix shows for your site.) ([Wix Help Center][5])
   * **Very important**: for records that point to Wix, set Cloudflare proxy to **DNS only** (gray cloud), or Wix may not validate/issue SSL correctly. Wix has a specific note about Cloudflare setups: records must be DNS‑only. ([Wix Help Center][6])

### Option B — **Squarespace** as the marketing site (root)

1. Keep DNS in Cloudflare; **connect a third‑party domain** to Squarespace. ([Squarespace Help][7])
2. In Cloudflare DNS, add:

   * **A records (apex `@`)** to Squarespace’s **four IPs**:
     `198.185.159.144`, `198.185.159.145`, `198.49.23.144`, `198.49.23.145`. ([Squarespace Help][8])
   * **CNAME `www` → `ext-cust.squarespace.com`**. (Squarespace’s “connect third‑party domain” guides include this.) ([Squarespace Help][9])
   * Like Wix, leave these records **DNS‑only** in Cloudflare to avoid TLS handshakes getting confused.

> Both options let you keep **Cloudflare DNS** for the zone and still map subdomains (docs/get/packages) to different providers.

---

## 2) Docs on Mintlify (`docs.pidgeon.health`)

* In **Mintlify Dashboard → Settings → Domain Setup**, set `docs.pidgeon.health`.
* In **Cloudflare DNS**, add: **`CNAME docs → cname.vercel-dns.com.`**
* If Cloudflare is DNS, set SSL/TLS mode to **Full (strict)** (Mintlify calls this out). ([Mintlify][1])

Mintlify will provision/validate and serve your docs at the custom domain.

---

## 3) Downloads + install script (`get.pidgeon.health`)

### The goal

* A simple one‑liner:

  ```bash
  curl -fsSL https://get.pidgeon.health/install.sh | sh
  ```
* Static, versioned binaries and checksums at stable URLs:

  * `https://get.pidgeon.health/releases/v1.2.3/pidgeon-linux-x64`
  * `https://get.pidgeon.health/releases/v1.2.3/sha256sums.txt`
  * A “latest” convenience redirect:
    `https://get.pidgeon.health/releases/latest/pidgeon-linux-x64`

### Recommended hosting (two easy paths)

**Path A — Cloudflare R2 (+ Pages/Workers)**

* Create an **R2 bucket** (e.g., `pidgeon-get`) and upload:

  ```
  /install.sh
  /releases/v1.2.3/pidgeon-macos-arm64
  /releases/v1.2.3/pidgeon-linux-x64
  /releases/v1.2.3/pidgeon-windows-x64.exe
  /releases/v1.2.3/sha256sums.txt
  ```
* Attach a **custom domain** `get.pidgeon.health` to the bucket (Cloudflare has a step‑by‑step “point to R2 with a custom domain” guide). This gives HTTPS + CDN without extra egress fees between R2 and Cloudflare. ([Cloudflare Docs][2])
* If you want **pretty redirects** (for a `/releases/latest/...` path that always points to the latest), add a tiny **Cloudflare Worker** on `get.pidgeon.health` that 302‑redirects “latest” to the newest version path—or directly to your GitHub Release asset. Workers provide a simple redirect example, and you can update a `LATEST` KV key on each release. ([Cloudflare Docs][10])
* Alternative: skip code and just upload a text file like `/releases/latest.txt` and have `install.sh` read that file to resolve the version.

**Path B — AWS S3 + CloudFront**

* Put the same folder layout in **S3**, put **CloudFront** in front, add `get.pidgeon.health` as an Alternate Domain Name and attach an ACM cert. (AWS’s static hosting + CloudFront guides walk through this.) ([AWS Documentation][11])

> **Tip:** If most artifacts live on GitHub Releases, you can **redirect** from `get.pidgeon.health` to GitHub assets and still keep short, branded URLs. GitHub supports a stable “latest asset” URL pattern:
> `https://github.com/<org>/<repo>/releases/latest/download/<asset-name>` (official doc). A Worker can map `get.pidgeon.health/latest/pidgeon-linux-x64` → that URL. ([GitHub Docs][12])

**HTTP headers to set (important for curl + browsers)**

* **Versioned binaries**:
  `Cache-Control: public, max-age=31536000, immutable`
  `Content-Type: application/octet-stream`
  `Content-Disposition: attachment; filename="pidgeon-linux-x64"`
* **install.sh / latest endpoints**:
  `Cache-Control: public, max-age=300` (keep fresh)
  `Content-Type: text/plain; charset=utf-8`

---

## 4) Linux repos (`packages.pidgeon.health` with Cloudsmith)

* Create your public repo(s) in **Cloudsmith** and upload `.deb`/`.rpm` produced by nfpm.
* Ask Cloudsmith Support to enable a **custom domain** `packages.pidgeon.health`. They’ll provision it and show you **two CNAMEs** to add in your DNS (one for authorization, one for traffic). Available on Velocity/Ultra. ([Cloudsmith Help][3])
* After DNS propagates, your apt/yum instructions can use the **branded** host:

  ```bash
  # Debian/Ubuntu
  curl -fsSL https://packages.pidgeon.health/key.asc | sudo gpg --dearmor -o /usr/share/keyrings/pidgeon.gpg
  echo "deb [signed-by=/usr/share/keyrings/pidgeon.gpg] https://packages.pidgeon.health/apt stable main" | sudo tee /etc/apt/sources.list.d/pidgeon.list
  sudo apt update && sudo apt install pidgeon
  ```

  Cloudsmith’s Debian repo docs cover best practices (Acquire‑By‑Hash, etc.). ([Cloudsmith][13])

---

## 5) DNS: the exact records to create in Cloudflare

> **Rule of thumb:**
>
> * For third‑party hosts that terminate TLS for you (Wix, Squarespace, Mintlify, Cloudsmith), set records to **DNS only** (gray cloud).
> * For your own Cloudflare‑hosted subdomains (R2/Pages/Workers), you can keep **proxied** (orange cloud).

Create these **now** (you can switch Wix ↔ Squarespace later):

**Apex (root) marketing site**

* **If Wix**:

  * `A @ → 185.230.63.107` (or whatever Wix shows for your site) — **DNS only**.
  * `CNAME www → pointing.wixdns.net` — **DNS only**. ([Wix Help Center][5])
  * (Wix + Cloudflare note: use DNS‑only for these records.) ([Wix Help Center][6])
* **If Squarespace**:

  * `A @ → 198.185.159.144`
  * `A @ → 198.185.159.145`
  * `A @ → 198.49.23.144`
  * `A @ → 198.49.23.145` — **all DNS only**. ([Squarespace Help][8])
  * `CNAME www → ext-cust.squarespace.com` — **DNS only**. ([Squarespace Help][9])

**Docs**

* `CNAME docs → cname.vercel-dns.com` — DNS only. (Mintlify custom domain). ([Mintlify][1])

**Downloads**

* **If Cloudflare R2**: add the CNAME/record per Cloudflare’s “point to R2 with a custom domain” guide (often done via Origin Rules + DNS record in dashboard). Keep **proxied** if you want Cloudflare TLS/CDN in front. ([Cloudflare Docs][2])
* **If S3 + CloudFront**: `CNAME get → your-cloudfront-distribution.cloudfront.net` — DNS only. ([Repost][14])

**Packages**

* Follow Cloudsmith’s custom domain instructions (they’ll give you **two CNAMEs** to add for `packages.pidgeon.health`). Set them **DNS only**. ([Cloudsmith Help][3])

**Email (optional but good hygiene, even if you don’t send yet)**

* Add **SPF/DKIM/DMARC** once you pick an email provider (Google Workspace, Fastmail, etc.). This protects the brand’s domain reputation for later.

---

## 6) How the `curl` downloads will actually work

* Place `install.sh` at `https://get.pidgeon.health/install.sh`. The script:

  1. Detects OS/arch.
  2. Pulls `https://get.pidgeon.health/releases/latest.txt` to learn the latest semver.
  3. Downloads the matching binary from `get.pidgeon.health/releases/<version>/...`
  4. Verifies SHA256 vs `sha256sums.txt`.
  5. Installs to `/usr/local/bin` (or a user bin) and drops shell completions (bash/zsh/fish) into the standard completion folders.
* If you prefer to host **binaries on GitHub Releases** only, your `install.sh` can instead hit **GitHub’s “latest asset” URL** pattern:
  `https://github.com/<org>/<repo>/releases/latest/download/<asset>` (official GitHub doc). You can *optionally* front this with a **Cloudflare Worker redirect** so users only ever see your domain. ([GitHub Docs][12])

---

## 7) What resources you’ll need (clean separation)

* **Cloudflare Account** (DNS for the zone, plus R2/Pages/Workers if you choose the Cloudflare path for `get.`).

  * Use **R2** for object storage, **Workers/Pages** for redirects and a small download site. ([Cloudflare Docs][2])
* **Website builder**: Wix *or* Squarespace. (Both support third‑party DNS via “pointing”/A‑records.) ([Wix Help Center][4])
* **Mintlify** for docs with custom domain mapping (`docs.`). ([Mintlify][1])
* **Cloudsmith** for apt/yum with **custom domain** (`packages.`). ([Cloudsmith Help][3])
* **GitHub** for Releases (if you want to keep assets there too), and to back `install.sh` logic using the “latest/download” pattern. ([GitHub Docs][12])

---

## 8) Brand + namespace “lockdown” checklist (do this now)

**Domains**

* Primary: `pidgeon.health` (you’re buying this).
* Defensive: `pidgeon.dev`, `pidgeon.io`, `pidgeon.app`, `pidgeon.ai` (optional), `pidgeon.so` (optional).
* Misspelling safety: people *will* type “pigeon”. If budget allows, consider `pigeon.health` redirecting to `pidgeon.health`.
* Long‑term: keep an eye on **pidgeon.com**; set a calendar reminder to re‑evaluate budget post‑traction.

**Critical subdomains (create even if empty to reserve)**

* `www`, `docs`, `get`, `packages`, `api`, `status`, `cdn`.
  (Point them to parking pages or DNS‑only placeholders so no one else “owns” your shape of URLs.)

**Package ecosystems / IDs**

* **GitHub org**: `pidgeon-health` (create now).
* **Docker**: Docker Hub org `pidgeonhealth` (push `pidgeonhealth/pidgeon`). Also enable **GHCR** `ghcr.io/pidgeon-health/pidgeon`. ([GitHub Docs][15])
* **npm**: Create the scope **`@pidgeon-health`** and publish a placeholder `@pidgeon-health/cli` (readme only) to reserve the name.
* **winget**: Reserve **PackageIdentifier** as `PidgeonHealth.Pidgeon` by submitting your first manifest (use `wingetcreate`/PR to `microsoft/winget-pkgs`). Microsoft’s docs outline the submission process; the repository enforces **hash verification** so users are protected. ([Microsoft Learn][16])
* **Scoop**: Create a `pidgeon-health/scoop-bucket` repo with an initial manifest `pidgeon.json`. Even if it points to a placeholder file now, you’ve staked the namespace.
* **Cloudsmith**: Org `pidgeon-health` and repo `pidgeon` for Debian/RPM; ask for the `packages.pidgeon.health` custom domain. ([Cloudsmith Help][3])

**Social / comms**

* GitHub: `pidgeon-health` (org).
* LinkedIn Page: “Pidgeon Health”.
* X/Twitter: `@pidgeonhealth`.
* YouTube: channel handle `@pidgeonhealth`.
* Discord server vanity (optional): `discord.gg/pidgeon`.
* Support inboxes (create early even if they forward to you):
  `support@`, `security@`, `press@`, `sales@`, `billing@` on `pidgeon.health`.
  Add **SPF/DKIM/DMARC** once you pick a mail provider.

**Security hygiene**

* Add `/.well-known/security.txt` on the root site with your security contact and policy.
* Publish your **GPG public key** for Linux repo verification at `https://packages.pidgeon.health/key.asc`.

---

## 9) A copy‑paste build plan you can execute

**Day 1–2 — DNS + roots**

1. Buy `pidgeon.health`. Set **Cloudflare** as nameserver for the zone.
2. Pick **Wix or Squarespace**. Add the required records in Cloudflare (**DNS‑only**). (Wix pointing guide; Squarespace IPs + CNAME.) ([Wix Help Center][4])
3. Add `docs` CNAME → `cname.vercel-dns.com` in Cloudflare; configure in Mintlify. Set Cloudflare SSL to **Full (strict)**. ([Mintlify][1])

**Day 3 — Downloads (`get.`)**
4\. Choose **Cloudflare R2**; create bucket `pidgeon-get`. Upload `install.sh`, `/releases/vX.Y.Z/...`, `sha256sums.txt`.
5\. Map **`get.pidgeon.health` to the R2 bucket** (Cloudflare guide). Keep proxied (orange cloud). ([Cloudflare Docs][2])
6\. Add an optional **Worker redirect** so `/releases/latest/<asset>` 302‑redirects to the newest version or to GitHub’s official `.../releases/latest/download/<asset>` URL. (Workers redirect example; GitHub latest asset pattern.) ([Cloudflare Docs][10])
7\. Set correct **headers** (Cache‑Control, Content‑Type, Content‑Disposition) for files as described above.

**Day 4 — Packages (`packages.`)**
8\. Create Cloudsmith org + repo. Upload test `.deb`/`.rpm`.
9\. Request **custom domain** `packages.pidgeon.health`; Cloudsmith will show **two CNAMEs** to add; add them in Cloudflare (DNS‑only). ([Cloudsmith Help][3])
10\. Test apt/yum with the new domain using Cloudsmith’s Debian/RPM docs. ([Cloudsmith][13])

**Day 5 — Reserve namespaces**
11\. Create `pidgeon-health` on GitHub; Docker Hub org `pidgeonhealth`; GHCR namespace `ghcr.io/pidgeon-health`. ([GitHub Docs][15])
12\. Publish placeholder npm package `@pidgeon-health/cli`.
13\. Submit the first **winget** manifest PR for `PidgeonHealth.Pidgeon`. ([Microsoft Learn][16])
14\. Create `scoop-bucket` repo with a placeholder `pidgeon.json`.

---

### FAQs you might hit

* **Can Cloudflare proxy my Wix/Squarespace records?**
  Use **DNS‑only** for those—Wix specifically calls out Cloudflare setups must be DNS‑only to work reliably. ([Wix Help Center][6])

* **Do I need both `get.` and `packages.`?**
  Yes—`get.` is for human‑readable installers and curl; `packages.` is for apt/yum with GPG and repo metadata. Clean separation helps enterprises audit and mirror repos. (Cloudsmith encourages custom domains for vendor distribution.) ([Cloudsmith Help][3])

* **Can I skip hosting binaries and just redirect to GitHub?**
  Yes. Use a Worker to redirect to GitHub Release **latest/download** links and keep your short URLs. It’s common and keeps your `install.sh` stable. ([GitHub Docs][12])

---

## TL;DR wiring map

* **`pidgeon.health`** → Wix (A/CNAME per Wix) *or* Squarespace (4×A + CNAME) — **DNS‑only**. ([Wix Help Center][5])
* **`docs.pidgeon.health`** → **CNAME** `cname.vercel-dns.com` (Mintlify) — **DNS‑only**; Cloudflare SSL **Full (strict)**. ([Mintlify][1])
* **`get.pidgeon.health`** → Cloudflare **R2 + Pages/Worker** with custom domain — **proxied**. ([Cloudflare Docs][2])
* **`packages.pidgeon.health`** → Cloudsmith **custom domain** (add **two CNAMEs**) — **DNS‑only**. ([Cloudsmith Help][3])

This gives you a high‑quality marketing site, a rock‑solid download host that works perfectly with `curl`, a proper Linux repo with branded URLs, and clean docs—all under one domain you control.

[1]: https://www.mintlify.com/docs/settings/custom-domain "Custom domain - Mintlify"
[2]: https://developers.cloudflare.com/rules/origin-rules/tutorials/point-to-r2-bucket-with-custom-domain/?utm_source=chatgpt.com "Point to R2 bucket with a custom domain - Cloudflare Docs"
[3]: https://help.cloudsmith.io/docs/custom-domains "Custom Domains"
[4]: https://support.wix.com/en/article/connecting-a-domain-to-wix-using-the-pointing-method?utm_source=chatgpt.com "Connecting a Domain to Wix Using the Pointing Method"
[5]: https://support.wix.com/en/article/troubleshooting-your-ssl-certificate?utm_source=chatgpt.com "Troubleshooting Your SSL Certificate | Help Center | Wix.com"
[6]: https://support.wix.com/en/article/verifying-your-cloudflare-settings-for-connecting-a-domain-to-wix?utm_source=chatgpt.com "Verifying Your Cloudflare Settings for Connecting a Domain to Wix"
[7]: https://support.squarespace.com/hc/en-us/articles/205812378-Connecting-a-third-party-domain-to-your-Squarespace-site?utm_source=chatgpt.com "Connecting a third-party domain to your Squarespace site"
[8]: https://support.squarespace.com/hc/en-us/articles/206541647-Connecting-a-DreamHost-domain-to-your-Squarespace-site?utm_source=chatgpt.com "Connecting a DreamHost domain to your Squarespace site"
[9]: https://support.squarespace.com/hc/en-us/articles/360035485391-DNS-records-for-connecting-third-party-domains?utm_source=chatgpt.com "DNS records for connecting third-party domains - Squarespace Help Center"
[10]: https://developers.cloudflare.com/workers/examples/redirect/?utm_source=chatgpt.com "Redirect · Cloudflare Workers docs"
[11]: https://docs.aws.amazon.com/AmazonS3/latest/userguide/HostingWebsiteOnS3Setup.html?utm_source=chatgpt.com "Tutorial: Configuring a static website on Amazon S3"
[12]: https://docs.github.com/en/repositories/releasing-projects-on-github/linking-to-releases?utm_source=chatgpt.com "Linking to releases - GitHub Docs"
[13]: https://docs.cloudsmith.com/formats/debian-repository?utm_source=chatgpt.com "Debian Repository | Cloudsmith Docs"
[14]: https://repost.aws/knowledge-center/cloudfront-serve-static-website?utm_source=chatgpt.com "Use CloudFront to serve a static website hosted on Amazon S3"
[15]: https://docs.github.com/enterprise-cloud%40latest/packages/working-with-a-github-packages-registry/working-with-the-container-registry?utm_source=chatgpt.com "Working with the Container registry - GitHub Docs"
[16]: https://learn.microsoft.com/en-us/windows/package-manager/package/repository?utm_source=chatgpt.com "Submit your manifest to the repository | Microsoft Learn"
