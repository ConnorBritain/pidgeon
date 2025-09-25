# Implementation Plan for Distributing Pidgeon (Cross-Platform CLI)

**Pidgeon** is a cross-platform C#/.NET CLI tool for healthcare message testing. Our goal is to make Pidgeon **easy to install (under 30 seconds)** on any platform and trusted by enterprise users. We'll use a three-phase strategy - **Developer Adoption, Enterprise Validation, and Mainstream Adoption** - to progressively expand distribution channels. Each phase introduces new distribution methods, CI/CD enhancements, and security measures. This plan is written like a tutorial for a beginner, with clear steps, examples, and links to tools.

## Phase 1: Developer Adoption (Weeks 1-4)

**Goal:** Get individual developers and consultants using Pidgeon quickly. We'll focus on the most convenient channels for developers: **Homebrew (for macOS/Linux)**, **Direct downloads**, and **GitHub Releases**. By the end of Phase 1, any developer should be able to install Pidgeon in one step and run it immediately.

### 1\. Self-Contained Binaries and GitHub Releases

- **Build single-file executables:** Configure Pidgeon's .NET project to produce **self-contained binaries** for each OS. This means the binary includes the .NET runtime, so users _don't need to install .NET separately_. In the .csproj, enable settings like PublishSingleFile and SelfContained=true so that the output is a standalone pidgeon executable (about ~50 MB per platform). Target runtime identifiers for each build: e.g. win-x64 for Windows, osx-x64/osx-arm64 for Mac (Intel/Apple Silicon), and linux-x64/linux-arm64 for Linux.
- **Automate builds with GitHub Actions:** Set up a CI workflow that triggers on new version tags. Use a matrix build on **Windows, macOS, and Linux runners** to compile the self-contained binaries for each platform. After building, have the workflow **create a GitHub Release** and attach the binaries. Include **SHA-256 checksum files** for each binary as release assets for verification. For example, you can generate checksums via a script (sha256sum) and upload a file like pidgeon-v1.0.0-checksums.txt with entries for each platform. This ensures anyone can verify downloads by comparing hashes. (We will also use these checksums in package managers and scripts to validate integrity.)
- **Provide platform-specific bundles (optional):** Instead of raw binaries, consider packaging them in .tar.gz (for Unix) or .zip (for Windows) with a simple directory structure. This can simplify Homebrew and other integrations (they often expect a tarball URL). The GitHub Action can zip the binary and attach, and generate the corresponding checksum. Each release on GitHub will then contain:
- pidgeon-windows-x64.exe (or zip containing it) + its .sha256 hash
- pidgeon-macos-arm64 (Mac Apple Silicon) + hash
- pidgeon-macos-x64 (Mac Intel) + hash
- pidgeon-linux-x64 + hash (and similarly for linux-arm64)
- A short **Installation Guide** for each platform (text or a link to docs) - e.g. how to run the installer script or use Homebrew, etc.
- **Test the binaries:** Before distribution, on each OS, run the binary (./pidgeon --help) to ensure it works without needing any external dependency. The startup time should be only a few seconds.

### 2\. Homebrew Tap for macOS and Linux

- **Why Homebrew:** Many healthcare developers use macOS, and Homebrew is the "gold standard" for installing CLI tools on Mac (and also works on Linux). We want brew install pidgeon to work on day one.
- **Homebrew tap setup:** Create a new GitHub repository, for example **pidgeon-health/homebrew-tap**, to host the Homebrew formula (recipe). Homebrew will use this as a "tap". The formula file (e.g. Formula/pidgeon.rb) will describe how to download and install Pidgeon. For instance:

class Pidgeon < Formula  
desc "Healthcare data generator and validator CLI"  
homepage "<https://pidgeon.dev>"  
url "<https://github.com/pidgeon-health/pidgeon/releases/download/v1.0.0/pidgeon-macos-x64.tar.gz>"  
sha256 "&lt;<<SHA-256 hash&gt;>>"  
license "MIT" # or appropriate license  
def install  
bin.install "pidgeon"  
\# if shell completion scripts are provided, install them:  
bash_completion.install "pidgeon.bash" => "pidgeon" if File.exist? "pidgeon.bash"  
zsh_completion.install "pidgeon.zsh" => "\_pidgeon" if File.exist? "pidgeon.zsh"  
fish_completion.install "pidgeon.fish" if File.exist? "pidgeon.fish"  
end  
end

This formula downloads the pre-built tarball from GitHub Releases and installs the pidgeon binary to Homebrew's bin directory. We also attempt to install completion scripts (more on shell completions below) if they are packaged.

- **Homebrew tap usage:** Once the tap and formula are published, users can do:

brew tap pidgeon-health/tap # add our tap repository  
brew install pidgeon  
pidgeon --version # should show the version immediately

This is the target UX for Homebrew. The brew command will also automatically verify the download via the SHA256 in the formula. We plan to support both x64 and ARM Macs - Homebrew can handle this by pointing to the appropriate URL or using the cellar :any_skip_relocation with a single binary if the tar contains both; but simplest is to have separate formula or a unified one if naming conventions allow. For now, we might maintain one formula that defaults to x64 tar for Intel Macs and use Homebrew's ability to handle ARM if we provide a separate URL or universal binary.

- **Auto-update formula:** Each time we release a new version, we should update the formula's URL and sha256. We can **automate this with GitHub Actions**. For example, after the release is published, an Action can run to modify pidgeon.rb in the tap repo to the new version and create a commit. This ensures brew upgrade pidgeon will pull the latest. The plan is to wire this into our CI/CD: on successful release, trigger a workflow (or use a tool like brew bump-formula-pr) to update our tap formula. This way, Homebrew users can update Pidgeon via the standard brew update && brew upgrade pidgeon flow.
- **Testing Homebrew install:** Try installing via brew on a Mac (and Linux) to verify it downloads the correct binary and runs. The CLI should start without extra configuration. If something fails (e.g., formula error), fix it before announcing this method. Having this channel early helps drive developer adoption because it's so convenient.

### 3\. Direct Download Script and Website

- **Why direct downloads:** Many enterprise users or security-conscious folks prefer not to use a package manager, and instead download a verified binary directly. Also, if other methods fail, a direct download is a reliable fallback. We'll provide an **official install script** and download page that makes this easy.
- **Domain setup:** Register a domain such as **pidgeon.health** (or use the existing one, e.g. pidgeon.dev). Set up subdomains:
- get.pidgeon.health - for hosting the install script (like how get.docker.com or get.nvm.sh work).
- packages.pidgeon.health - for hosting package repositories (will be used in Phase 2).

These domains make the install commands memorable and professional. Host a simple static page at pidgeon.health/download that offers links and instructions for each platform (with auto-detect if possible).

- **Create an install script:** Write a shell script (e.g. install.sh) that can be fetched via cURL. This script will:
- Detect the user's OS and architecture (e.g., by checking uname and perhaps arch).
- Download the appropriate Pidgeon binary from the GitHub Release. For example, if on Linux x64: fetch pidgeon-linux-x64 from the latest release URL. We can embed logic to find the latest version (maybe hitting a static URL like latest release or using GitHub API) or maintain a text file with the latest version number.
- Verify the SHA256 checksum of the downloaded file against a known value. For security, the script can download the checksum file from GitHub and grep the matching line for that platform binary, then compare. If the checksum doesn't match, abort (to prevent tampering).
- Put the pidgeon binary in a directory on the PATH. Commonly, these scripts install to /usr/local/bin (for Linux/macOS). It may require sudo for that; or we install to ~/.pidgeon/bin and advise the user to add it to PATH if not already.
- (Optional) Add an **uninstall** script or instructions (e.g., just remove the binary and any supporting files).

The user will run this with a one-liner:

curl -fsSL <https://get.pidgeon.health/install.sh> | sh

which will download and run the installer. After it's done, the user can immediately run pidgeon --help. This approach is similar to installers for tools like Node Version Manager or oh-my-zsh. It provides a super quick setup for those who trust the source.

- **Include shell completions in script:** The install script can also set up shell auto-completion. For example, after placing the binary, it can:
- If using Bash: append source <(pidgeon completions bash) to the user's ~/.bashrc (if not already present).
- If Zsh: write a \_pidgeon completion file to ~/.zsh/completions and ensure that directory is added to the fpath (and perhaps add to ~/.zshrc if needed).
- If Fish: output completions to ~/.config/fish/completions/pidgeon.fish.
- If PowerShell (on Linux via PowerShell Core): add a line to profile, though that might be beyond scope for the shell script (PowerShell users might prefer manual or a PowerShell-specific script).

This way, as soon as installation finishes, tab-completion works without extra steps. (We know Pidgeon's CLI is built on System.CommandLine which supports generating completion scripts easily.) We'll be careful to only append to shell config files once, and perhaps inform the user that "Shell completions have been installed 🎉".

- **Self-update command:** Implement a simple **pidgeon update** command in the CLI for those who installed via direct download. This command can essentially repeat the process above: check for the latest version (by querying our GitHub releases or an API), download the new binary, verify checksum, and atomically replace the current binary. By "atomically," we mean download to a temp file, then swap it in so that if something fails, the old binary remains intact. This gives users a convenient upgrade path: they run pidgeon update and get the newest version without manually re-downloading.
- **Windows direct install:** For Windows users not using a package manager, we can provide a PowerShell one-liner similar to the shell script. For example:

iwr -useb <https://get.pidgeon.health/install.ps1> | iex

This PowerShell script would download the Windows .exe from GitHub, check the hash, and place it in C:\\Program Files\\Pidgeon\\ (then add to PATH, or we could put it in the user's %LocalAppData%\\pidgeon and update the PATH for the current user). It could also register an uninstall entry or shortcuts if we want to be fancy, but for a CLI probably not needed. At minimum, it should ensure the pidgeon command is available in the next PowerShell session (which might mean adding to the user's PATH or current session \$env:PATH). We can also instruct the user to restart the shell or run RefreshEnv if using scoop's shim (but that's more for other tools).

- **Download page:** Along with the script, maintain a webpage (or README section) listing all installation options for Phase 1:
- Homebrew instructions,
- Direct script usage,
- Manual download links for each OS (some users might prefer to click a link and manually place the binary).
- Basic usage example to verify installation (like pidgeon --version or a quick command to generate a sample message in 60 seconds).

By the end of Phase 1, we have addressed the initial blockers: no need for a dev environment to run Pidgeon and no manual configuration. Developers can get started with a single command on their OS. We will likely achieve our metric of a core user base forming (e.g. aiming for hundreds of installs via these methods).

## Phase 2: Enterprise Validation (Weeks 5-8)

**Goal:** Build trust and ease of deployment for enterprise environments. In Phase 2, we add distribution channels and features that enterprises expect: official Linux packages, strong code signing, and audited delivery. We will also enhance our CI/CD for security (supply chain integrity) and introduce more robust update mechanisms suitable for enterprise use. By the end of this phase, Pidgeon should be compliant with typical enterprise IT requirements (signed binaries, installable via standard tools, etc.).

### 1\. Linux Package Repositories (apt and yum)

- **Why apt/yum:** Enterprise servers (and developer VMs) often run Linux, and administrators prefer using native package managers (APT for Debian/Ubuntu, YUM/DNF for Red Hat/CentOS/Fedora). Providing Pidgeon as a .deb and .rpm package allows easy installation and updates via system packages. It also integrates with their vulnerability scanning and config management.
- **Choose a hosting solution:** We will use **Cloudsmith** or a similar service to host our Linux package repository. Cloudsmith offers private apt/yum repositories with audit logs and reliable CDN distribution. It can also handle GPG signing for us, or we can upload our own GPG key. (Alternative: host our own apt repo on an S3 bucket or server, but Cloudsmith saves time and adds enterprise credibility with features like access control if needed).
- **Generate .deb and .rpm packages:** Use the **nfpm** tool to create Debian and RPM packages from our binary. nfpm is a simple CLI (written in Go) that can take a config file and output .deb/.rpm without needing dpkg or rpmbuild directly. We will create an nfpm.yaml config in our repo specifying details:
- Name, version (match Pidgeon version), description, homepage.
- The files to include: basically the pidgeon binary into /usr/bin/ (or /usr/local/bin/ depending on convention; many .deb use /usr/bin for system-wide tools).
- Possibly an entry to install shell completion scripts: e.g., place pidgeon.bash into /etc/bash_completion.d/ for bash, and similar for zsh (/usr/share/zsh/site-functions/\_pidgeon) and fish (/usr/share/fish/completions/pidgeon.fish). This way, when the package is installed, shell completions are **immediately available** system-wide.
- Post-installation steps (if needed): not much, maybe just echo a message about "Pidgeon installed. Type pidgeon --help to get started."
- Any dependencies: Since Pidgeon binary is self-contained, we have no runtime dependency on dotnet, so the package doesn't need to depend on anything heavy. We might only require something like libc which is ubiquitous. If we were linking to specific libs, we'd list them, but ideally our self-contained binary avoids that.
- For RPM, similar fields.
- **Integrate packaging into CI:** Extend the GitHub Actions pipeline: when we do a release, add a job to run nfpm (we can use a pre-made GitHub Action for nfpm or install it in a container). This job will produce pidgeon_&lt;version&gt;\_amd64.deb, pidgeon_&lt;version&gt;\_arm64.deb, and corresponding .rpm files. After building, use Cloudsmith's API (or their GitHub Action if available) to upload these packages to our repositories. We'll likely have one apt repo and one rpm repo on Cloudsmith (or separate repos per distribution).
- **GPG signing:** Cloudsmith can manage the GPG signing of the repo index. Alternatively, we generate a GPG keypair specifically for Pidgeon packages (e.g., an official "Pidgeon Package Signing Key") and upload the public key. Users will need this to verify packages. The **apt repository** will provide a public key (often downloadable as key.asc or via a keyserver). Cloudsmith typically exposes a URL for the key. For RPM, the repository metadata includes the GPG signature.
- **Usage for users:** Document the installation steps for Debian/Ubuntu and RHEL-based systems. For example, the flow would be:
- **Debian/Ubuntu:** Add our GPG key and apt source, then install:
- curl -fsSL <https://packages.pidgeon.health/key.asc> | sudo apt-key add - # Add signing key  
    echo "deb <https://packages.pidgeon.health/apt/> stable main" | sudo tee /etc/apt/sources.list.d/pidgeon.list  
    sudo apt update && sudo apt install pidgeon
- After this, pidgeon is on the system and kept up-to-date via sudo apt upgrade. We will likely use the stable channel in our apt repo (and could have beta for pre-releases if needed later).
- **RHEL/CentOS (YUM/DNF):** Add the repo and install:
- sudo rpm --import <https://packages.pidgeon.health/key.asc> # add GPG key  
    sudo yum-config-manager --add-repo <https://packages.pidgeon.health/rpm/pidgeon.repo>  
    sudo yum install pidgeon
- Or the equivalent using dnf. This will install Pidgeon and users can update via yum update.

These commands are very similar to the snippet in our strategy document, confirming the expected UX. We will include these instructions in our docs and website for easy copy-paste.

- **Testing:** Try setting up the apt repository on a test VM or Docker container. Make sure apt update successfully fetches our package index and the package installs without issues. Do the same with an RPM-based test. Also verify that shell completion files are in place after install (e.g., which pidgeon works and pidgeon &lt;tab&gt; auto-completes if shell completion is configured in the shell, which it usually is for bash in modern systems by default).
- **Enterprise features from this:** By delivering via apt/yum, enterprises get **audit logs** (via Cloudsmith or their own proxy) of what was installed, and they can mirror the repo internally if needed. This addresses the enterprise need for controlled deployments. Also, the GPG-signed repo assures them of integrity (no tampering).

### 2\. Code Signing for Windows and macOS

- **Why code signing:** Enterprises require that software is from a verified publisher. Unsigned executables can trigger security warnings (SmartScreen on Windows, or Gatekeeper on macOS will say the app is from an unidentified developer). In Phase 2, we'll implement proper code signing:
- **Windows Authenticode signing:** Sign the Windows pidgeon.exe with an Authenticode certificate from a trusted Certificate Authority. We will obtain a **code signing certificate** - ideally an Extended Validation (EV) certificate, since EV-signed apps immediately get reputation to avoid SmartScreen warnings. This might require purchasing a cert (e.g. from DigiCert, Sectigo, etc.) and using a hardware token (for EV). If EV is too complex initially, a standard code-sign cert is still okay (just might show a warning until reputation is built). We'll register the publisher as, say, "Pidgeon Health, Inc." so that users see this name on install prompts.
- **macOS codesign and notarization:** Use an Apple Developer ID certificate to sign the Mac binaries. We must join the Apple Developer Program to get a Developer ID Application certificate (as an individual or company). Once we have it, our CI runner on macOS can use the certificate to run codesign -s "Developer ID Application: \[Name\] (TeamID)" --options runtime --timestamp pidgeon-macos-x64. The --timestamp option ensures a timestamp from Apple, so the signature remains valid after the certificate expires. After signing, we will **notarize** the binary with Apple: upload the binary to Apple's notary service (xcrun altool --notarize-app ...) and upon success, staple the notarization ticket (xcrun stapler staple pidgeon-macos-x64). This extra step tells macOS Gatekeeper that the app was scanned and approved by Apple. The outcome: when a user runs pidgeon on Mac, it won't be blocked as unidentified - it will trust our Developer ID. Notarization is required on modern macOS for distributing outside the App Store, so this is important for a smooth install.
- **Linux package signing:** We already cover this by GPG-signing the packages/repo. There's no concept of Authenticode on Linux, but for completeness, ensure our .deb and .rpm themselves are signed by our GPG key (nfpm can sign the packages, or it might only sign the repo metadata - signing the repo is usually sufficient). We might also distribute a GPG-signed checksum file for tarballs in GitHub releases for Linux users who manually download. This gives an extra layer if they want to verify the binary with our public key.
- **Integrate signing in CI/CD:** Add steps in our GitHub Actions workflow:
- For Windows builds: Use signtool.exe to sign the .exe. Because our build runs on GitHub-hosted runners, handling the certificate is tricky. One approach: use a self-hosted runner that has the certificate, or use Azure Key Vault or similar with a signing service. For simplicity, we might start by manually signing releases (download the exe, sign it locally, re-upload) if automation is too hard. But let's assume we automate: we'll store the certificate (if non-EV) in a secure format (password-protected PFX file) as a GitHub secret (as base64). The workflow on Windows will decode it, import it into the certificate store or use it directly with signtool (providing the password), then sign the binary. We also sign the installer script for PowerShell if possible (PowerShell script signing uses a different code-signing cert though, so might skip that).
- For macOS builds: Store the .p12 for the Apple certificate (or use an API key for notarization). The Mac job will import the cert into the keychain (using security import with the cert and key password from secrets). Then run the codesign command on the built pidgeon binary. After that, for notarization, use an API key (Apple provides App Store Connect API key for notarization now) or use username/password (less ideal). Automating notarization means our CI will wait for Apple's response (which can take a few minutes). We might do this asynchronously or in a separate workflow. Given we want fully automated releases, we can integrate it: after notarization is successful, download the notarized binary (or the CI can fetch the ticket and staple it).
- For Linux packages: Cloudsmith signing takes care of apt and yum. If we provide standalone binaries on GitHub, we could add a step to sign those with a GPG key (attach .asc files). That might be overkill, but it's a nice-to-have for security-savvy users.
- **Verification:** Once code signing is in place, test on a fresh Windows machine: downloading the signed pidgeon.exe should no longer show "Unknown publisher" but show our company name. On macOS, test opening the binary (or running it via Terminal) on a machine that hasn't seen it - it should run without the "unidentified developer" warning. If any signing issues arise, adjust entitlements or ensure the notarization is done.
- **Outcome:** Code-signed binaries mean **no security pop-ups** and build trust. Many enterprise IT policies block unsigned executables, so this is critical for getting into those environments. We will highlight in documentation that our binaries are signed and how to verify the signatures (like providing our signing cert fingerprint so users can double-check if they want).

### 3\. Advanced CI/CD - Updates and Security

Now that we have multiple distribution channels, we need a more advanced CI/CD pipeline to manage them and ensure everything stays in sync and secure.

- **Channel-aware update logic:** Update the pidgeon CLI to recognize how it was installed and handle updates appropriately. One idea (from our strategy) is to **embed a small marker file** during installation indicating the channel. For example, when installing via Homebrew, we could drop a file like \$INSTALL_DIR/install.json containing {"channel": "homebrew"}. The CLI at runtime can check for this file:
- If channel: homebrew, then pidgeon update can echo "Please run brew upgrade pidgeon to update."
- If channel: apt or yum, it can instruct the user to update via their package manager (apt upgrade or similar).
- If channel: direct (script or GitHub release), then pidgeon update will perform the self-update as implemented.
- If channel: winget or others (to be added in Phase 3), handle accordingly.
- If no marker (perhaps the user just ran binary from zip), we treat it like direct.

This **channel-aware updater** provides a seamless experience across channels. Implementing it might involve a small addition to the CLI code and including the install.json in our various installation methods (Homebrew formula can install a file to e.g. share/pidgeon/, apt package can put it under /usr/share/pidgeon/, etc., and our binary can look for it in a known location or relative to itself if packaged).

- **Continuous integration for multi-channel:** Our GitHub Actions will become more complex as we now have to build, sign, package, and deploy to many targets. We should structure the workflow with multiple jobs (some in parallel, some sequential). A possible breakdown:
- **Build job(s):** runs on each platform to produce binaries (as we did in Phase 1).
- **Package jobs:** once binaries are ready, package them: e.g. a job that runs on Linux to use nfpm to make .deb/.rpm (needs the Linux binary artifact from build), a job on Windows to create a Scoop manifest or Chocolatey package, etc.
- **Signing jobs:** part of build or post-build on each platform (we might integrate signing in the build jobs themselves).
- **Release job:** coordinates attaching all artifacts to GitHub Release and perhaps publishing to other repositories (Cloudsmith, Homebrew tap, etc.).
- Use artifacts to pass files between jobs. For instance, the Linux build job artifacts the binary, the packaging job downloads that artifact to create .deb.

We'll also set up **failure notifications** - if any part fails (like signing fails due to certificate issue), the team is alerted to fix it quickly, as distribution is a critical pipeline.

- **Reproducible builds:** To enforce supply chain security, make our builds reproducible and verifiable. This involves locking down build inputs and documenting the environment:
- Use specific versions of the .NET SDK and ensure our build process is deterministic (dotnet can produce deterministic builds if configured, meaning the same source will generate byte-for-byte identical binaries on different machines, given the same environment). We can test this by building the same tag on two machines and comparing checksums.
- Keep track of build container or VM images. Possibly use Docker containers for build steps to have a consistent environment each time (e.g., use the official mcr.microsoft.com/dotnet/sdk:8.0 container for Linux build).
- Generate a **Software Bill of Materials (SBOM)** for the build. .NET may have a tool (e.g. dotnet sbom or we can use 3rd party like Syft) to list all dependencies and their versions that went into the build. This SBOM can be published with the release for transparency, which some enterprise compliance checks appreciate.
- **SLSA Level 3 compliance:** Follow the Supply-chain Levels for Software Artifacts guidelines to reach level 3. In practice, this means:
- **Provenance attestation:** our CI system should produce a signed statement about how the build was performed. We can use GitHub's built-in **OIDC tokens** with Sigstore Cosign to sign our artifacts. For example, use cosign attest to attach a provenance file (which includes info like source Git commit, build workflow, etc.). The attestation is cryptographically signed by our CI (which can use keyless signing via Sigstore, tying the signature to our GitHub workflow identity).
- Ensure all our dependencies (like actions used, base container images) are trustworthy and version-pinned.
- Two-person code reviews for changes (where possible) to avoid single bad actor - as a junior founder, you might be solo now, but keep this principle as team grows.
- These steps ensure that the **provenance of the build is traceable and tamper-evident**, which is what SLSA3 is about. It gives enterprise users confidence that the binary they download was indeed built from the source code in our repo and hasn't been altered.
- **Security scanning:** Integrate **dependency scanning and virus scanning** into CI. We can enable GitHub Dependabot alerts for NuGet packages to catch vulnerabilities early (though as a CLI tool, our dependency surface may be small and mostly .NET packages). Also, before releasing, run an antivirus scan on the binaries (especially for Windows) to ensure no false positives - some CI tools or actions can do this, or we can manually test with Windows Defender. This prevents surprises where our tool gets flagged in an enterprise environment.
- **Checksum verification everywhere:** We already use SHA256 in many places (Homebrew, winget manifests, our install script). We will continue to enforce that **every distribution channel either verifies the SHA256 or signature**:
- Homebrew does it via formula hash.
- APT/YUM via GPG signatures.
- Direct script via built-in hash check.
- Winget (in next phase) requires providing SHA256 in manifest.
- Scoop requires SHA256 hash in manifest as well.
- NPM wrapper will download and verify the hash of the binary (we should code the install script to do so).
- This ensures even if an attacker intercepts the download, it won't be executed unless it matches the known hash.
- **Monitoring and rollbacks:** As we start distributing widely, set up monitoring for issues:
- Monitor our GitHub Issues or discussions for any installation problems reported.
- Possibly set up a simple **download analytics** to track how many installations per channel (e.g., GitHub release download counts, Homebrew analytics, etc.) - this can be a future improvement.
- Plan a **rollback strategy:** For example, if a bad release occurs (buggy or security issue), be ready to yank a Homebrew formula update, or push a quick fix version. In package repos, mark the bad version as deprecated. Having an emergency pipeline to publish a fix is part of Phase 2 readiness (enterprises like to know that issues can be responded to swiftly).

By the end of Phase 2, we expect to have a handful of enterprise trial customers comfortably installing Pidgeon via their preferred methods, and no security blockers in doing so. We will have solidified trust through signed, verified deliveries. Our distribution approach will meet compliance standards (no critical security incidents allowed!). In short, Pidgeon will be **enterprise-ready** from an installation and upgrade standpoint.

## Phase 3: Mainstream Adoption (Weeks 9-12)

**Goal:** Make Pidgeon available everywhere a developer might look for it. This phase completes the distribution matrix by adding Windows-specific package managers and other ecosystems: **Windows Package Manager (winget)**, **Scoop**, **npm (Node.js)**, and **Docker images**. These channels will maximize our reach across communities (Windows devs, JavaScript devs, DevOps/CI pipelines, etc.). We will also refine the user experience so that installing and updating Pidgeon is as smooth as possible across all these avenues.

### 1\. Windows Package Manager (winget)

- **What is winget:** Winget is the built-in package manager on Windows 10/11. Many developers use it to quickly install tools via winget install &lt;package&gt;. Having Pidgeon on winget means Windows users can get it without browsing a website or using third-party installers. We want a one-command install for Windows like we achieved with Homebrew.
- **Create a winget manifest:** Winget uses a community repository of package manifests (YAML files) on GitHub. We need to contribute a manifest for Pidgeon. The manifest describes the package metadata and where to get the installer. Key fields include:
- PackageIdentifier (in Reverse-DNS style, e.g. **PidgeonHealth.Pidgeon** if we use "PidgeonHealth" as publisher),
- PackageVersion (e.g. 1.0.0),
- Publisher (e.g. Pidgeon Health, Inc.),
- PackageName (Pidgeon CLI),
- License,
- Installers: here we specify the URL to the installer and its SHA256 hash. For installer type, since we have just an .exe binary (no MSI), we might treat it as a "portable" package. Winget supports installing portable executables: we provide the URL to the zip/exe and specify "InstallerType": "portable" with an install script that basically just places the file.

For example, a minimal manifest might look like:

PackageIdentifier: PidgeonHealth.Pidgeon  
PackageVersion: 1.0.0  
PackageLocale: en-US  
Publisher: Pidgeon Health, Inc.  
PackageName: Pidgeon CLI  
License: MPL-2.0  
ShortDescription: CLI for healthcare message generation and validation  
Installers:  
\- Architecture: x64  
InstallerType: exe  
InstallerUrl: <https://github.com/pidgeon-health/pidgeon/releases/download/v1.0.0/pidgeon-windows-x64.exe>  
InstallerSha256: &lt;SHA256 of the exe&gt;  
Scope: User  
ManifestType: singleton  
ManifestVersion: 1.2.0

(If we want to support Windows on ARM in the future, we could add another installer entry with Architecture: ARM64 and the URL to an ARM64 binary.)

- **Submission to winget repository:** We use Microsoft's **winget-create** tool or manually fork the [winget-pkgs GitHub repo](https://github.com/microsoft/winget-pkgs). Using wingetcreate new is convenient: it asks for our GitHub release URL and auto-generates the YAML, then submits a PR to the winget-pkgs repository for us. We'll do this for the first version. Microsoft's bots will verify the installer (they will check that the hash matches and the installer runs silently if possible - for portable, they might just check extraction). Once the PR is merged, Pidgeon becomes installable via winget for everyone.
- **Automating winget updates:** For each new release, we should update the manifest. We can automate this via GitHub Actions as well. One approach: use the winget-create CLI in a CI job to update the version and hash, then push a PR. Alternatively, use a custom script to modify our YAML and gh CLI to create a PR. Even if we automate, someone (or Microsoft's bot) will review and merge it. This process can take a few hours to a day. We should plan around that delay for release day (maybe release in the morning so winget update PR can be merged by end of day).
- **User experience:** Once published, a Windows user can simply run:
- winget install PidgeonHealth.Pidgeon
- This will download our signed installer/binary and place it on the system (for portable, winget typically puts it in a standard location and adds to PATH). We should test this once available: on a fresh Windows machine, run the command and verify pidgeon becomes available in the command prompt/PowerShell. Winget handles updates via winget upgrade - when we push a new manifest, users running winget upgrade will see Pidgeon can be updated. (We will ensure the PackageIdentifier remains the same so upgrades link to the right package.)
- **Note on installer vs portable:** If enterprises prefer an **MSI installer**, we might in the future create an MSI (with a proper installer UI or silent install) and use that in winget. For now, distributing the raw exe as a portable package is simpler. Winget can still install it (it will just copy the exe to %LOCALAPPDATA%\\Microsoft\\WinGet\\Packages\\... by default and shim it). We'll document that Pidgeon can also be uninstalled via winget uninstall PidgeonHealth.Pidgeon.

### 2\. Scoop Bucket (Windows)

- **Why Scoop:** Scoop is a popular alternative package manager for developers on Windows (especially those who use PowerShell). It's simpler than winget and works by reading app manifests from Git repos (called "buckets"). We add Scoop mainly to ensure even those who prefer Scoop can get Pidgeon easily. It's also useful because Scoop doesn't require our app to be in the central repo - we can host our own bucket.
- **Create a Scoop manifest:** A Scoop manifest is a JSON file describing how to install the app. For Pidgeon, it will include:
- version: the version string.
- architecture: separate URLs and hashes for 64-bit (and 32-bit or ARM if we support them).
- url: the direct link to the binary (or zip).
- hash: the SHA256 of the file (Scoop will verify this).
- bin: the name of the binary that Scoop should add to PATH (for us, "pidgeon.exe").
- Optionally, installer scripts if needed, but not necessary for a single binary.
- Possibly persist if we want to preserve any user config files on upgrade (if Pidgeon uses a config file in the home directory, we might not need any persist rules since that's outside install dir).

Example pidgeon.json (simplified):

{  
"version": "1.0.0",  
"description": "Healthcare message testing CLI",  
"homepage": "<https://pidgeon.dev>",  
"license": "MIT",  
"architecture": {  
"64bit": {  
"url": "<https://github.com/pidgeon-health/pidgeon/releases/download/v1.0.0/pidgeon-windows-x64.zip>",  
"hash": "&lt;SHA256&gt;",  
"bin": "pidgeon.exe"  
}  
}  
}

We may use a zip for Windows in this case, containing pidgeon.exe. Alternatively, Scoop can handle exe directly, but typically it expects an archive if there are multiple files. Since we might include a README or completion script, a zip is fine.

- **Host a bucket:** Create a repository like pidgeon-health/scoop-bucket (or include it in an existing one) to host our manifest JSON. Users can then add our bucket:
- scoop bucket add pidgeon <https://github.com/pidgeon-health/scoop-bucket.git>  
    scoop install pidgeon
- This tells scoop about our manifest and then installs Pidgeon. We'll add instructions for this to our docs as well.
- **Update the manifest for new versions:** Similar to Homebrew, whenever we do a release, update the version, url, and hash in pidgeon.json. This can be automated: we can use a GitHub Action (there are existing ones, or with a small Node script) to update the file and push a commit to the bucket repo when a release happens. Also, Scoop supports an "autoupdate" field where we can specify a template URL that can auto-check the latest version, but since we have automation, a direct update is fine. With this, scoop update pidgeon will fetch the new manifest and show if a new version is available.
- **Testing Scoop:** On a Windows machine with Scoop installed, test the above commands. After scoop install pidgeon, running pidgeon should work. Because we placed the binary and Scoop shimmed it, it should be on PATH (Scoop usually links the binary into its apps directory). Also test upgrade path by simulating a version bump.
- **Note:** Some Windows users favor **Chocolatey** as well. If time permits, we could also publish Pidgeon on Chocolatey (similar to winget manifest, you create a NuGet package with the binary and submit). However, the question specifically mentioned winget and Scoop, so we prioritize those. Chocolatey was considered in earlier plans, but winget has largely overtaken it for many users, and scoop covers the power-users. We can add Choco support later if there is demand.

### 3\. npm Package (Node.js Global Install)

- **Why npm:** Many developers (especially those in web or JavaScript environments) have Node.js and use npm or Yarn to install CLI tools. By publishing a wrapper on npm, we make Pidgeon accessible via a simple npm install -g command. This is not for running in Node.js, but just a convenient distribution mechanism.
- **Create a wrapper package:** We will create an npm package, perhaps named **@pidgeon-health/cli** (scoped under our org for uniqueness). This package won't contain the actual Pidgeon logic; instead, it will include:
- A small JavaScript file (e.g. install-binary.js) that runs on post-install.
- Scripts in package.json: a "postinstall" script that triggers our binary download logic.
- A bin entry in package.json to expose the pidgeon command. On Unix, npm will create a symlink named pidgeon to a script we provide; on Windows it will create a pidgeon.cmd shim automatically.

The idea is: when someone runs npm install -g @pidgeon-health/cli, npm will install our package globally. The postinstall script then detects the OS (using process.platform and process.arch in Node), downloads the appropriate Pidgeon binary from our GitHub releases, verifies its checksum, and saves it inside the npm package directory. For example, it might download to node_modules/@pidgeon-health/cli/bin/pidgeon.exe on Windows or .../bin/pidgeon on Linux. The bin config in package.json would point to a launcher script or directly to the binary.

- **Platform handling:** We must ensure the npm package delivers the right binary:
- We could include all binaries in the npm package, but that would bloat it. Better to download at install time.
- The postinstall script can fetch from our GitHub release URL as we did in the shell script. We can even reuse similar logic.
- For Windows, after downloading pidgeon-windows-x64.exe, we might rename it to just pidgeon.exe and place in the package's bin directory. On \*nix, ensure the file has execute permissions (chmod +x).
- Provide a helpful message if something fails (e.g., no prebuilt binary for their platform).
- **Verify and publish:** Test the npm installation: on a machine with Node.js, run the local package install to see that it indeed puts the binary and that running pidgeon works. Then publish the package to npm registry. Users anywhere can then do:
- npm install -g @pidgeon-health/cli  
    pidgeon --version
- On first run or during install, it should download the binary automatically. If we prefer, we can have it download on first execution (some CLI wrappers do lazy download), but doing it in postinstall ensures the user doesn't experience a delay on first run.
- **Updates via npm:** If installed this way, users can get updates by upgrading the npm package:
- npm update -g @pidgeon-health/cli
- This will fetch the latest wrapper, and our postinstall can then download the new binary version. Alternatively, our CLI's internal pidgeon update could work, but since the wrapper manages the binary, it might be safer to rely on npm itself for updates. We will document that method. (We could also implement that if pidgeon update sees channel npm, it prints "Run npm update -g @pidgeon-health/cli to update.")
- **Note:** By having an npm distribution, we integrate with the Node ecosystem. For example, some might include Pidgeon in a project's devDependencies and call it from npm scripts. It also makes it easy to use in environments where installing global npm packages is standard (some CI or cloud shells).

### 4\. Docker Images

- **Why Docker:** Providing a Docker image for Pidgeon allows users to run it in containerized environments (like CI pipelines or when they don't want to install anything locally). It also helps in standardizing environments (no matter what OS, if they have Docker, they can run Pidgeon).
- **Create a Dockerfile:** We will make a minimal Docker image that includes the Pidgeon binary:
- Use a lightweight base image, such as **Ubuntu** or **Alpine**. .NET self-contained binaries do require glibc, so Alpine (which uses musl) might not work unless we specifically built a musl version. To keep it simple, use ubuntu:22.04 or debian:stable-slim as base.
- Copy the Linux pidgeon binary into the image (e.g., to /usr/local/bin/pidgeon).
- Set ENTRYPOINT \["pidgeon"\] in the Dockerfile, so that running the container will invoke the pidgeon command by default.
- Optionally, set a default CMD, or rely on entrypoint such that users can do docker run pidgeonhealth/pidgeon:latest --help.
- The image likely won't need additional dependencies since the binary is self-contained. At most, ensure ca-certificates are present in the image if Pidgeon makes HTTPS calls (for example, if it fetches healthcare standards or does any network). But that's part of runtime, not strictly needed for core functionality.
- **Build and publish images:** Use GitHub Actions to build the Docker images when we push a release:
- Possibly use Docker's Buildx to do multi-architecture builds (e.g., build for linux/amd64 and linux/arm64). This way, if someone is on an M1 Mac or using ARM servers, the Docker image still works natively. We can use QEMU if needed for cross-building.
- Tag the image with the version (like pidgeon:1.0.0) and also update the latest tag.
- Push to a container registry. We can use **Docker Hub** (create an org/repo like pidgeonhealth/pidgeon) or the GitHub Container Registry (GHCR). Docker Hub is more familiar to most users, so we'll likely use that and perhaps mirror to GHCR.
- Ensure to attach an SBOM to the container image too (Docker supports SBOM attestation with recent tooling, though this is an advanced step).
- **Using the Docker image:** Document usage examples for users:
- Run a one-off command:
- docker run --rm pidgeonhealth/pidgeon:latest pidgeon --version
- (Here we call pidgeon explicitly just in case, though with ENTRYPOINT one could shorten it).
- For more interactive use, maybe mount a volume if they want to work with local files. E.g., if Pidgeon is to diff two HL7 files on the host:
- docker run --rm -v "\$PWD:/data" pidgeonhealth/pidgeon:latest pidgeon diff /data/file1.hl7 /data/file2.hl7
- This allows the container to access host files in the current directory via the /data mount.
- In CI, a user could just use our image in a pipeline step to run tests without installing the CLI on the runner machine.
- **Updates:** To update, users pull a new Docker image tag. This is manual (or automated via their CI). We will update the latest tag on each release, so pulling :latest always gives the newest (with the caveat that latest is mutable). We will also version tags for deterministic builds (so :1.1.0 etc).
- **Test the image:** After building, run it locally to ensure the binary works inside the container. Check that the entrypoint is functioning (container runs and exits with the right output). Also ensure the image is reasonably small - using a slim base, we might get it down to ~60-80MB. If needed, we can explore using scratch or distroless base, but .NET might not fully static link, so an OS base is fine for now.

### 5\. Final Polishing of UX and Documentation

In the final phase, as we release these channels, we must ensure the **user experience is consistent and well-documented**:

- **Unified documentation:** Update Pidgeon's README and website to list **all installation options** in a clear way. Perhaps have a table or matrix by OS:
- Windows: via winget, via Scoop, or direct (PowerShell).
- Mac: via Homebrew, or direct download.
- Linux: via apt (Debian/Ubuntu), via yum (RHEL/CentOS), via Homebrew (as alternative), or direct.
- Any OS: via Docker, via npm. For each, provide the one-liner or steps, and note if any prerequisites (e.g., "Homebrew must be installed first", "Docker must be available"). Also include how to update for each channel (e.g., "use brew upgrade", "use winget upgrade", etc.), so users know the lifecycle.
- **Shell completions ready:** Double-check that after installation through each method, shell completions are either automatically set up or at least available:
- Homebrew: if we provided completion scripts in the formula, brew will place them and for bash/zsh, those will work if the user's shell loads the Homebrew completions (which by default, brew does advise adding to your bashrc/zshrc). We might include a note in our caveats section of formula like ohai "Bash/Zsh completions installed. You may need to restart your shell.".
- Apt/Yum: our package placed the files; on Ubuntu, the bash-completion package will auto-load anything in /etc/bash_completion.d/, so completions should work out of the box in new shells. Zsh might need manual fpath config if not already set for site-functions, but power users can handle that. We can mention how to activate if needed.
- Winget/Scoop/npm: these don't automatically integrate shell completions. We can, however, ship a copy of the completion script with the binary or have a command to generate it (pidgeon completions &lt;shell&gt; exists already). We will include a note in documentation: "For advanced CLI usage, Pidgeon supports tab-completion. If you installed via winget or npm, run pidgeon completions bash (or zsh/fish/powershell) and follow the output instructions to enable it, since those installers don't auto-configure it." This way, no one is stuck without completions if they want it.
- We aim that the easiest methods (Homebrew, apt, install script) did auto-setup completions. So the majority of users get the benefit without any extra config.
- **Consistent update experience:** We've built channel-specific update commands. We should test them:
- If installed via script (direct), run pidgeon update to ensure it fetches the new version and doesn't break anything (and retains executable permissions, etc.).
- If via brew, test brew upgrade.
- Winget: winget upgrade after we push a new version.
- Scoop: scoop update pidgeon.
- Npm: npm update -g.
- All should work without error. If our channel marker approach is implemented, test that pidgeon update on a Homebrew install correctly tells user to use brew (and does not try to self-update).
- **Performance check:** Ensure that the first run of Pidgeon (which might involve loading some data or models) is still quick. Our target was user going from never-heard-of-it to running first command in under 5 minutes. Installation is under 30 seconds, leaving plenty of time to run a sample command. We can include a quick start in docs, like "Run pidgeon generate "ADT^A01" to generate a sample message" to give instant gratification.
- **Feedback and support:** As our distribution goes mainstream, set up channels for feedback (like a GitHub Discussions or a Discord channel) and monitor them for any installation issues. Also gather analytics if possible - e.g., how many downloads per release on GitHub (to measure adoption), how many installs via Homebrew (Homebrew Cask provides some analytics), etc. This isn't directly in the install process, but it's part of measuring success and catching if a channel isn't being used or has a problem.
- **Continuous improvement:** After Phase 3, we'll have essentially _10 different distribution channels_. It's important to maintain them:
- Keep automation scripts up to date (if e.g. winget changes manifest format or Homebrew changes something).
- Periodically review if any channel can be merged or simplified (maybe we won't need both Scoop and winget long-term, but for now we support both).
- Ensure security certificates are renewed (code signing certs expire yearly or every few years, Apple cert too, and GPG key might expire - set reminders to renew these to avoid downtime).
- Keep an eye on supply chain: update our CI base images and tools to get security updates, and re-issue SBOMs each release.