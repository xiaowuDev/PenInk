# PenInk.Mac

macOS 桌面端，使用 Avalonia 做透明 overlay、自绘工具栏和笔迹渲染，使用 SharpHook 监听全局热键。

## 快捷键

启动后会显示一个置顶悬浮工具栏。拖动工具栏顶部手柄可以移动位置；点 `P`/`E`/`M` 切换画笔、橡皮和鼠标穿透，点 `H` 只隐藏画布并保留工具栏。

| 快捷键 | 功能 |
| --- | --- |
| `Command + Option + P` | 画笔 |
| `Command + Option + E` | 橡皮 |
| `Command + Option + M` | 鼠标穿透 |
| `Command + Option + Delete` | 清屏 |
| `Command + Option + H` | 隐藏 |
| `Command + Option + Z` | 撤销 |

## 打包

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-macos.ps1 -Runtime osx-arm64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-macos.ps1 -Runtime osx-x64
```

macOS 本机 shell：

```bash
./scripts/package-macos.sh osx-arm64
./scripts/package-macos.sh osx-x64
```

输出 `.app`、`.zip`、`.dmg` 和 `.pkg` 到 `artifacts/macos/<runtime>/`。

## 注意

当前包未签名、未公证。第一次在 Mac 上运行时需要允许打开，并在系统设置里授予辅助功能/输入监控权限。
