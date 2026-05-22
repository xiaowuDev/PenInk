# PenInk 架构说明

状态：已切换为 C# / WPF / Windows Ink 实现。

## 目标

当前目标是优先保证手写流畅度和系统级 overlay 体验：

- 使用 WPF `InkCanvas` 接收 Windows Ink 原生笔输入。
- 使用 Win32 扩展窗口样式实现透明置顶、工具窗口和鼠标穿透。
- 使用 Windows 全局热键切换画笔、橡皮、鼠标模式、清屏、撤销和隐藏。
- 将可复用的输入算法拆到 `PenInk.Core`，为后续 Mac 版保留空间。

## 分包结构

```text
PenInk.Core
  Input
    InkPoint
    PointerTap
    PointerTapGuard

PenInk
  MainWindow.xaml
  MainWindow.xaml.cs
  Inking
    InkHistory
  Infrastructure.Windows
    HotkeyService
    NativeMethods

PenInk.Mac
  MainWindow
  OverlayCanvas
  MacHotkeyService
```

## 设计原则

- 核心层不引用 WPF、Win32、AppKit 等平台 API。
- 平台层可以适配不同系统能力，但不能把平台类型泄漏回核心层。
- 当前 Windows 版保留 `InkCanvas`，因为它对数位板压感、曲线拟合、动态墨迹的体验明显优于手写采样再绘制。
- 撤销历史暂时留在 Windows 层，因为 WPF 的 `StrokeCollection` 是当前真实笔迹容器。

## 后续 Mac 版建议

Mac 版已经新增为 `PenInk.Mac`，使用 C# + Avalonia。系统能力仍然按平台单独适配：

- 透明置顶窗口
- 鼠标穿透
- 全局热键
- 数位板压感输入
- 辅助功能和输入监听权限

`PenInk.Mac` 只依赖 `PenInk.Core`，不依赖 Windows WPF 项目。
