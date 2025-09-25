here’s a **ready-to-paste GitHub Actions CI workflow** scaffold for your repo. it builds multi-platform binaries, publishes them on GitHub Releases, attaches checksums, updates Homebrew + Winget, and leaves commented-out stubs for **signtool** (windows OV cert) and **notarytool** (apple dev).

this is written to be minimal but expandable. you can copy this as `.github/workflows/release.yml`.

---

```yaml
name: release

on:
  push:
    tags:
      - "v*.*.*"   # trigger on version tags, e.g. v1.0.0

jobs:
  build:
    name: Build binaries
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        include:
          - os: ubuntu-latest
            rid: linux-x64
          - os: ubuntu-latest
            rid: linux-arm64
          - os: macos-latest
            rid: osx-x64
          - os: macos-latest
            rid: osx-arm64
          - os: windows-latest
            rid: win-x64

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Publish self-contained binary
        run: |
          dotnet publish -c Release -r ${{ matrix.rid }} \
            -p:PublishSingleFile=true -p:SelfContained=true \
            -o out/${{ matrix.rid }}

      - name: Archive binary
        run: |
          cd out/${{ matrix.rid }}
          zip -r ../../pidgeon-${{ matrix.rid }}.zip .
          cd ../..

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: pidgeon-${{ matrix.rid }}
          path: pidgeon-${{ matrix.rid }}.zip

  release:
    name: Create GitHub Release
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4
        with:
          path: dist

      - name: Generate checksums
        run: |
          cd dist
          sha256sum * > checksums.txt

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: dist/*
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

  homebrew:
    name: Update Homebrew Tap
    needs: release
    runs-on: ubuntu-latest
    steps:
      - name: Bump Homebrew Formula
        run: |
          # Example using brew bump-formula-pr or custom script
          echo "TODO: implement homebrew bump script"
          # you can automate editing Formula/pidgeon.rb in your tap repo
          # set sha256 from dist/checksums.txt and update URL

  winget:
    name: Update Winget Manifest
    needs: release
    runs-on: windows-latest
    steps:
      - name: Setup WingetCreate
        run: dotnet tool install --global wingetcreate

      - name: Update Manifest
        run: |
          wingetcreate update PidgeonHealth.Pidgeon `
            --version ${{ github.ref_name }} `
            --urls "https://github.com/${{ github.repository }}/releases/download/${{ github.ref_name }}/pidgeon-win-x64.zip" `
            --submit

  # ---- future paid steps ----
  # windows signing
  # - name: Code Sign (Windows)
  #   run: |
  #     signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `
  #       /f $env:WIN_CERT_PATH /p $env:WIN_CERT_PASS pidgeon.exe
  #   env:
  #     WIN_CERT_PATH: ${{ secrets.WIN_CERT_PATH }}
  #     WIN_CERT_PASS: ${{ secrets.WIN_CERT_PASS }}

  # mac notarization
  # - name: Notarize (macOS)
  #   run: |
  #     xcrun notarytool submit "pidgeon-osx-x64.zip" \
  #       --apple-id ${{ secrets.APPLE_ID }} \
  #       --password ${{ secrets.APPLE_APP_SPECIFIC_PASSWORD }} \
  #       --team-id ${{ secrets.APPLE_TEAM_ID }} \
  #       --wait
  #
  #   # staple the notarization
  # - name: Staple
  #   run: |
  #     xcrun stapler staple "pidgeon-osx-x64.zip"
```

---

## how this works

* **phase 0 (free):**

  * tags trigger builds → self-contained binaries → zipped + checksums → GitHub Release
  * homebrew + winget jobs scaffolded; you can implement them as simple scripts
  * free today, but Windows users may see “unknown publisher”

* **phase 1 (paid later):**

  * uncomment **signtool** step once you buy a Windows OV cert (store PFX + password in GitHub secrets)
  * uncomment **notarytool** step once you join Apple Developer (\$99/yr), set up Apple ID/team ID/app-specific password as secrets

* **npm wrapper (optional):**

  * you can make a tiny npm package with a `postinstall` that fetches the right binary from GitHub Releases + verifies SHA-256 from `checksums.txt`