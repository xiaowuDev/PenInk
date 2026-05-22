param(
    [ValidateSet("osx-arm64", "osx-x64")]
    [string] $Runtime = "osx-arm64"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactRoot = Join-Path $root "artifacts\macos\$Runtime"
$publish = Join-Path $artifactRoot "publish"
$app = Join-Path $artifactRoot "PenInk.app"
$contents = Join-Path $app "Contents"
$macos = Join-Path $contents "MacOS"
$resources = Join-Path $contents "Resources"

dotnet publish (Join-Path $root "PenInk.Mac\PenInk.Mac.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publish
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (Test-Path $app) {
    Remove-Item -LiteralPath $app -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $macos | Out-Null
New-Item -ItemType Directory -Force -Path $resources | Out-Null
Copy-Item -Path (Join-Path $publish "*") -Destination $macos -Recurse -Force

$plist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>PenInk</string>
  <key>CFBundleIdentifier</key>
  <string>com.penink.app</string>
  <key>CFBundleName</key>
  <string>PenInk</string>
  <key>CFBundleDisplayName</key>
  <string>PenInk</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>0.1.0</string>
  <key>CFBundleVersion</key>
  <string>1</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
"@

Set-Content -LiteralPath (Join-Path $contents "Info.plist") -Value $plist -Encoding UTF8
Compress-Archive -Path $app -DestinationPath (Join-Path $artifactRoot "PenInk-$Runtime.zip") -Force

Write-Host "macOS app: $app"
Write-Host "macOS zip: $artifactRoot\PenInk-$Runtime.zip"
