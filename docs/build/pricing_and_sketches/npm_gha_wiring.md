let’s wire `checksums.json` into your **release pipeline** so the npm wrapper always has the right hashes.

---

## 1. extend your release job

add this step **after building all artifacts and generating checksums.txt**:

```yaml
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

      - name: Generate checksums.json
        run: |
          node <<'EOF'
          const fs = require('fs');
          const path = require('path');

          const version = process.env.GITHUB_REF_NAME.replace(/^v/, ''); // e.g. "1.0.0"
          const map = {};
          fs.readFileSync('dist/checksums.txt', 'utf8')
            .trim()
            .split('\n')
            .forEach(line => {
              const [hash, filename] = line.split(/\s+/);
              if (filename.includes('darwin-x64')) map['darwin-x64'] = hash;
              if (filename.includes('darwin-arm64')) map['darwin-arm64'] = hash;
              if (filename.includes('linux-x64')) map['linux-x64'] = hash;
              if (filename.includes('linux-arm64')) map['linux-arm64'] = hash;
              if (filename.includes('win-x64')) map['win32-x64'] = hash;
              if (filename.includes('win-arm64')) map['win32-arm64'] = hash;
            });

          const obj = {};
          obj[`v${version}`] = map;
          fs.writeFileSync('dist/checksums.json', JSON.stringify(obj, null, 2));
          EOF

      - name: Upload release assets
        uses: softprops/action-gh-release@v2
        with:
          files: dist/*
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## 2. sync into your npm wrapper repo

now that `checksums.json` exists in the release artifacts, you can **pull it into your npm wrapper repo** automatically:

```yaml
  npm:
    name: Publish npm wrapper
    needs: release
    runs-on: ubuntu-latest
    steps:
      - name: Checkout npm wrapper
        uses: actions/checkout@v4
        with:
          repository: pidgeon-health/pidgeon-npm-wrapper
          token: ${{ secrets.NPM_WRAPPER_PAT }}  # personal access token with repo write

      - name: Download checksums.json
        uses: actions/download-artifact@v4
        with:
          path: dist

      - name: Update checksums.json + bump version
        run: |
          VERSION=${GITHUB_REF_NAME#v}
          cp dist/checksums.json checksums.json
          npm version $VERSION --no-git-tag-version

      - name: Publish to npm
        run: npm publish --access public
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

---

## 3. how it plays out

* release tag `v1.2.3` → CI builds binaries + `checksums.txt` + `checksums.json`
* `checksums.json` is attached to the GitHub Release **and** synced into the npm wrapper repo
* npm wrapper version auto-bumped to `1.2.3` and published to npm
* `postinstall.js` always finds the exact matching hash

---

✅ this way, your **npm package stays in lockstep with GitHub Releases**, no manual editing.

===