#!/usr/bin/env bash
set -euo pipefail
export COPYFILE_DISABLE=1

runtime="${1:-osx-arm64}"
case "$runtime" in
  osx-arm64|osx-x64) ;;
  *)
    echo "Usage: $0 [osx-arm64|osx-x64]" >&2
    exit 2
    ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$script_dir/.." && pwd)"
artifact_root="$root/artifacts/macos/$runtime"
publish="$artifact_root/publish"
app="$artifact_root/PenInk.app"
contents="$app/Contents"
macos="$contents/MacOS"
resources="$contents/Resources"
dmg_root="$artifact_root/dmg-root"
pkg_root="$artifact_root/pkg-root"
pkg_work="$artifact_root/pkg-work"
version="0.1.0"
bundle_id="com.penink.app"
dotnet_exe="${DOTNET_EXE:-dotnet}"

"$dotnet_exe" publish "$root/PenInk.Mac/PenInk.Mac.csproj" \
  --configuration Release \
  --runtime "$runtime" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  --output "$publish"

rm -rf "$app" "$dmg_root" "$pkg_root" "$pkg_work"
mkdir -p "$macos" "$resources"
cp -R "$publish"/. "$macos"/

cat > "$contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>PenInk</string>
  <key>CFBundleIdentifier</key>
  <string>$bundle_id</string>
  <key>CFBundleName</key>
  <string>PenInk</string>
  <key>CFBundleDisplayName</key>
  <string>PenInk</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$version</string>
  <key>CFBundleVersion</key>
  <string>1</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

chmod +x "$macos/PenInk"
find "$app" -name '._*' -delete
xattr -cr "$app" 2>/dev/null || true

if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$app"
fi
xattr -cr "$app" 2>/dev/null || true

ditto --norsrc -c -k --keepParent "$app" "$artifact_root/PenInk-$runtime.zip"

mkdir -p "$dmg_root"
ditto --norsrc "$app" "$dmg_root/PenInk.app"
ln -s /Applications "$dmg_root/Applications"
find "$dmg_root" -name '._*' -delete
xattr -cr "$dmg_root" 2>/dev/null || true
hdiutil create \
  -volname "PenInk" \
  -srcfolder "$dmg_root" \
  -ov \
  -format UDZO \
  "$artifact_root/PenInk-$runtime.dmg"

mkdir -p "$pkg_root/Applications"
ditto --norsrc "$app" "$pkg_root/Applications/PenInk.app"
find "$pkg_root" -name '._*' -delete
xattr -cr "$pkg_root" 2>/dev/null || true

mkdir -p "$pkg_work"
payload_files="$(cd "$pkg_root" && find . -print | wc -l | tr -d ' ')"
install_kbytes="$(du -sk "$pkg_root" | awk '{print $1}')"
cat > "$pkg_work/PackageInfo" <<PKGINFO
<?xml version="1.0" encoding="utf-8"?>
<pkg-info overwrite-permissions="true" relocatable="false" identifier="$bundle_id" postinstall-action="none" version="$version" format-version="2" install-location="/" auth="root">
    <payload numberOfFiles="$payload_files" installKBytes="$install_kbytes"/>
    <bundle path="./Applications/PenInk.app" id="$bundle_id" CFBundleShortVersionString="$version" CFBundleVersion="1"/>
    <bundle-version>
        <bundle id="$bundle_id"/>
    </bundle-version>
    <upgrade-bundle>
        <bundle id="$bundle_id"/>
    </upgrade-bundle>
    <update-bundle/>
    <atomic-update-bundle/>
    <strict-identifier>
        <bundle id="$bundle_id"/>
    </strict-identifier>
    <relocate>
        <bundle id="$bundle_id"/>
    </relocate>
</pkg-info>
PKGINFO

mkbom -s "$pkg_root" "$pkg_work/Bom"
(cd "$pkg_root" && find . -print | sort | cpio -o -H odc -R 0:0 --quiet | gzip -c > "$pkg_work/Payload")
(cd "$pkg_work" && xar --compression none -cf "$artifact_root/PenInk-$runtime.pkg" Bom Payload PackageInfo)

echo "macOS app: $app"
echo "macOS zip: $artifact_root/PenInk-$runtime.zip"
echo "macOS dmg: $artifact_root/PenInk-$runtime.dmg"
echo "macOS pkg: $artifact_root/PenInk-$runtime.pkg"
