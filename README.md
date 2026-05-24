# PenInk 屏幕批注

PenInk 是一个精简的 Windows 屏幕手写批注工具，当前版本使用 C# WPF `InkCanvas` 获取原生 Windows Ink 手写体验。

## 演示

<video src="docs/music/penink-demo.mp4" controls muted playsinline width="100%"></video>

[无法播放时打开演示视频](docs/music/penink-demo.mp4)

## 项目结构

```text
PenInk.slnx
PenInk.Core/                 # 跨平台核心层
  Input/                     # 输入点、短触点画补偿算法
PenInk/                      # Windows WPF 应用
  MainWindow.xaml            # 透明 overlay 和右侧工具栏
  MainWindow.xaml.cs         # UI 编排、模式切换、笔刷配置
  Inking/                    # WPF InkCanvas 笔迹历史
  Infrastructure/Windows/    # Win32 热键、置顶、鼠标穿透
PenInk.Mac/                  # macOS Avalonia 应用
  OverlayCanvas.cs           # 自绘笔迹和工具栏
  MacHotkeyService.cs        # Command + Option 全局热键
scripts/                     # Windows exe 和 macOS app 打包脚本
```

拆包原则：

- `PenInk.Core` 不依赖 WPF、Win32，可以被未来 Mac 版复用。
- `PenInk` 只放 Windows 专属实现，包括 WPF、InkCanvas、Win32。
- `PenInk.Mac` 只放 macOS 桌面实现，包括 Avalonia overlay 和 macOS 全局热键。
- UI 层只做编排，底层能力按职责放到 `Inking` 和 `Infrastructure/Windows`。

## 运行

```powershell
dotnet run --project .\PenInk\PenInk.csproj
```

## 构建

```powershell
dotnet build .\PenInk.slnx --configuration Release
```

## 打包

Windows 单文件 exe：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-windows.ps1
```

输出：

```text
artifacts/windows/win-x64/PenInk.exe
```

macOS Apple Silicon：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-macos.ps1 -Runtime osx-arm64
```

macOS 本机 shell：

```bash
./scripts/package-macos.sh osx-arm64
```

macOS Intel：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-macos.ps1 -Runtime osx-x64
```

macOS 本机 shell：

```bash
./scripts/package-macos.sh osx-x64
```

输出：

```text
artifacts/macos/osx-arm64/PenInk.app
artifacts/macos/osx-arm64/PenInk-osx-arm64.zip
artifacts/macos/osx-arm64/PenInk-osx-arm64.dmg
artifacts/macos/osx-arm64/PenInk-osx-arm64.pkg
artifacts/macos/osx-x64/PenInk.app
artifacts/macos/osx-x64/PenInk-osx-x64.zip
artifacts/macos/osx-x64/PenInk-osx-x64.dmg
artifacts/macos/osx-x64/PenInk-osx-x64.pkg
```

## 快捷键

| 快捷键 | 功能 |
| --- | --- |
| `Ctrl + Alt + P` | 画笔模式 |
| `Ctrl + Alt + E` | 橡皮模式 |
| `Ctrl + Alt + M` | 鼠标穿透 |
| `Ctrl + Alt + Backspace` | 清屏 |
| `Ctrl + Alt + H` | 隐藏 |
| `Ctrl + Alt + Z` | 在画笔/橡皮模式下撤销 |
| `Esc` | 当前激活时隐藏 |

Windows 版基于 WPF + Windows Ink；Mac 版基于 Avalonia，使用 `Command + Option` 快捷键体系。

## macOS 说明

Mac 版启动后会显示一个置顶悬浮工具栏，可以直接用按钮切换画笔、橡皮、鼠标穿透、撤销、清屏、颜色和粗细。拖动工具栏顶部的手柄可以移动位置；点 `H` 只隐藏画布，工具栏仍会保留，方便再次切回画笔。

Mac 版使用 `Command + Option` 作为全局热键前缀：

| 快捷键 | 功能 |
| --- | --- |
| `Command + Option + P` | 画笔模式 |
| `Command + Option + E` | 橡皮模式 |
| `Command + Option + M` | 鼠标穿透 |
| `Command + Option + Delete` | 清屏 |
| `Command + Option + H` | 隐藏 |
| `Command + Option + Z` | 撤销 |

当前 Mac 包是未签名开发包。首次运行时，macOS 可能要求在“隐私与安全性”里允许打开，并给 PenInk 授予“辅助功能/输入监控”权限，全局热键才会生效。
