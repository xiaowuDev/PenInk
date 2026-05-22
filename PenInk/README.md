# PenInk.Windows

这是 Windows 桌面端应用项目，负责透明置顶窗口、WPF `InkCanvas` 手写、工具栏、全局热键和鼠标穿透。

## 职责边界

- `MainWindow.xaml`：右侧悬浮工具栏和全屏透明画布。
- `MainWindow.xaml.cs`：窗口状态、工具模式、笔刷、颜色、粗细和事件编排。
- `Inking/InkHistory.cs`：监听 WPF `StrokeCollection`，统一处理撤销。
- `Infrastructure/Windows/HotkeyService.cs`：注册 Windows 全局热键。
- `Infrastructure/Windows/NativeMethods.cs`：封装 Win32 扩展窗口样式和鼠标穿透。

## 依赖方向

```text
PenInk.Windows -> PenInk.Core
```

Windows 项目可以依赖核心层；核心层不能依赖 Windows 项目。

## 构建

```powershell
dotnet build .\PenInk\PenInk.csproj --configuration Release
```
