# PenCliBook 屏幕标注工具架构审批文档

状态：待审批  
目标版本：MVP v0.1  
运行平台：Windows 10/11  
技术栈：Java 21 + JavaFX + JNA + JNativeHook

## 1. 背景与假设

本项目是一个 Windows 桌面屏幕标注工具，核心体验参考用户提供的 UI 图：透明置顶覆盖层、右侧小工具栏、画笔/橡皮/颜色/粗细/清屏/撤销/鼠标穿透/隐藏能力。

第一版只交付稳定可用的 MVP：

- `Ctrl + Alt + P`：进入画笔模式，显示透明覆盖层，拦截鼠标并绘制。
- `Ctrl + Alt + M`：进入鼠标模式，窗口仍可见但鼠标穿透到底层应用。
- `Ctrl + Alt + C`：清除所有笔迹。
- `Ctrl + Z`：撤销上一笔或上一条擦除操作。
- `Esc`：隐藏 overlay。
- 小工具栏：笔、橡皮、颜色、粗细、清屏、撤销、鼠标模式。

明确不放入第一版：

- 数位板压感。第二版再接 JPen/Wintab。
- 多屏复杂 DPI 校准。MVP 优先支持主屏，架构预留多屏扩展点。
- 云同步、用户账号、远程协作。
- 复杂截图编辑器能力，例如文字框、箭头库、马赛克、OCR。
- Spring Boot。该项目是本地桌面应用，引入 Spring 会增加启动、打包和配置复杂度，不符合最简目标。

关键假设：

- 用户使用 Windows 10/11，已安装或可随应用捆绑 JRE 21。
- MVP 只需要本地内存状态，不需要数据库。
- 笔迹保存为对象列表，用于撤销、清除、后续保存和渲染。
- 全局热键需要在应用未聚焦时仍可触发。

## 2. 架构决策与取舍

### 2.1 总体架构

采用轻量 DDD + Ports and Adapters：

```text
com.pencli.book
├─ bootstrap                 # 启动、依赖装配
├─ domain                    # 纯领域模型，不依赖 JavaFX/JNA/JNativeHook
│  ├─ model                  # Stroke、Point、BrushStyle、ToolMode、OverlayState
│  └─ service                # EraseStrokeService、StrokeHitTestService
├─ application               # 用例编排
│  ├─ port.in                # StartDrawingUseCase、ClearUseCase、UndoUseCase
│  ├─ port.out               # OverlayWindowPort、CanvasRenderPort、HotkeyPort
│  └─ service                # AnnotationSessionService、ToolModeService
├─ adapter
│  ├─ in.javafx              # JavaFX UI、Canvas、Toolbar、鼠标事件
│  └─ in.hotkey              # JNativeHook 热键输入
└─ infrastructure
   └─ windows                # JNA Windows 样式、置顶、透明、鼠标穿透
```

分层原则：

- 领域层只表达“笔迹是什么、如何新增、撤销、擦除、清除”。
- 应用层只表达“用户触发了什么用例，应该改变什么状态并通知哪些端口”。
- JavaFX、JNA、JNativeHook 全部在适配层/基础设施层，避免污染核心逻辑。
- UI 状态可以重建，领域状态必须可测试。

### 2.2 UI 技术选择

选择 JavaFX `Canvas + GraphicsContext`：

- 单个透明全屏 Stage 承载 Canvas 和工具栏。
- Canvas 只负责渲染，不直接持有不可恢复业务状态。
- 每次笔迹变化后全量 redraw：MVP 笔迹数量有限，全量重绘简单、稳定、容易修复。
- 当前正在绘制的 stroke 作为临时对象显示，鼠标释放后提交到领域会话。

拒绝方案：

- 用 JavaFX Shape 节点表示每条笔迹：命中检测和擦除分段会更复杂，节点数量上来后管理成本更高。
- 用 Swing/AWT：和 JavaFX 工具栏、现代打包体验不一致。
- 用原生 C++/Win32：可控性更强，但开发成本和维护门槛明显高于 Java MVP。

### 2.3 笔迹模型

核心对象：

- `StrokeId`：笔迹唯一标识。
- `CanvasPoint`：屏幕坐标点，包含 `x/y/timestamp`，第二版可扩展 pressure。
- `BrushStyle`：颜色、粗细、透明度、线帽、线连接。
- `Stroke`：一条连续绘制轨迹，由点列表和样式构成。
- `AnnotationDocument`：当前所有笔迹及操作历史。
- `UndoableCommand`：新增笔迹、擦除笔迹、清屏都走命令历史，统一撤销。

橡皮擦策略：

- MVP 不只删除整条笔迹，而是按橡皮路径和笔迹线段的距离做命中，命中部分会被分段删除。
- 这样体验接近常见标注工具，同时逻辑集中在 `EraseStrokeService`，可用单元测试覆盖。
- 若实现复杂度超出审批范围，可降级为“命中即删除整条 stroke”，但默认不采用降级方案。

### 2.4 窗口与鼠标穿透

JavaFX Stage 配置：

- `StageStyle.TRANSPARENT`
- always-on-top
- 全屏覆盖主屏
- Scene 背景透明

Windows JNA 配置：

- 画笔/橡皮模式：移除 `WS_EX_TRANSPARENT`，overlay 拦截鼠标。
- 鼠标模式：增加 `WS_EX_TRANSPARENT`，鼠标事件穿透到底层应用。
- 隐藏模式：隐藏 Stage 或只保留托盘图标，避免遮挡用户。

该逻辑通过 `OverlayWindowPort` 暴露给应用层，具体 Windows API 只放在 `infrastructure.windows`。

### 2.5 热键策略

使用 JNativeHook：

- 统一在 `GlobalHotkeyAdapter` 中注册全局热键。
- 转成应用层命令：`SHOW_DRAWING_MODE`、`MOUSE_PASSTHROUGH_MODE`、`CLEAR`、`UNDO`、`HIDE`。
- 热键回调不能直接改 JavaFX UI，必须通过 `Platform.runLater` 回到 FX 线程。

拒绝方案：

- JavaFX `Accelerator`：只能在窗口聚焦时触发，不能满足全局热键。
- JNA 直接注册 Windows hotkey：依赖更少，但键盘事件处理和跨版本细节更多，MVP 先用成熟库。

### 2.6 构建与打包

建议使用 Maven：

- Java 21 toolchain。
- `javafx-maven-plugin` 本地运行。
- `maven-surefire-plugin` 跑单元测试。
- 后续用 `jpackage` 输出 Windows 安装包或免安装目录。

MVP 交付形态：

- 开发期：`mvn javafx:run`
- 测试：`mvn test`
- 打包候选：`mvn package`，第二阶段再补完整 `jpackage`

## 3. 风险登记

| 风险 | 影响 | 概率 | 优先级 | 缓解方案 | 验证方式 |
| --- | ---: | ---: | ---: | --- | --- |
| JavaFX 透明窗口与 Windows 鼠标穿透行为不稳定 | 5 | 3 | 15 | 将 JNA 样式切换封装为单一适配器；模式切换后强制刷新窗口样式 | 手工验证画笔/鼠标/隐藏三种模式；增加 Windows adapter 边界日志 |
| JNativeHook 全局热键和 JavaFX 线程冲突 | 4 | 3 | 12 | 热键线程只投递应用命令，UI 更新统一 `Platform.runLater` | 热键连续触发测试，验证无 FX 线程异常 |
| 擦除分段算法导致笔迹断裂异常或性能下降 | 4 | 3 | 12 | 领域服务独立实现，先用线段距离算法；大笔迹按点数阈值采样 | 单元测试覆盖相交、不相交、端点命中、连续擦除 |
| DPI 缩放导致鼠标位置和 Canvas 坐标偏移 | 4 | 3 | 12 | MVP 明确主屏支持；坐标转换集中在 JavaFX adapter | 100%、125%、150% 缩放手工验收 |
| 全量 redraw 在大量笔迹下卡顿 | 3 | 3 | 9 | MVP 接受全量 redraw；超过阈值后可引入离屏缓存 | 压测 200 条 stroke，每条 200 点，观察延迟 |
| 置顶透明窗口可能影响系统快捷键或其他应用 | 4 | 2 | 8 | 提供 `Esc` 隐藏和托盘退出；鼠标模式默认穿透 | 手工验证浏览器、PPT、IDE 中切换 |
| 应用异常退出后热键未释放 | 3 | 2 | 6 | JVM shutdown hook 注销 JNativeHook；托盘退出走统一关闭流程 | 退出后热键不再响应，进程无残留 |

发布阻塞项：

- 优先级 15 的穿透窗口风险必须在 MVP 验收前通过手工验证。
- 擦除算法必须有单元测试，不接受只靠手工试用。

## 4. 待实现代码计划

审批通过后按以下顺序实现：

1. 初始化 Maven + Java 21 + JavaFX 项目骨架。
2. 实现领域模型：`Stroke`、`CanvasPoint`、`BrushStyle`、`AnnotationDocument`、`UndoableCommand`。
3. 实现领域服务：笔迹新增、撤销、清屏、橡皮擦分段。
4. 实现应用服务：模式切换、绘制会话、热键命令分发。
5. 实现 JavaFX UI：透明 Stage、Canvas 渲染、小工具栏、鼠标事件。
6. 实现 Windows JNA 适配：置顶、透明、鼠标穿透切换。
7. 实现 JNativeHook 适配：全局热键。
8. 补测试：领域层单元测试、应用服务状态切换测试。
9. 本机运行验证：画笔、鼠标穿透、清屏、撤销、隐藏。

建议第一版源码目录：

```text
src/main/java/com/pencli/book
src/main/resources
src/test/java/com/pencli/book
```

## 5. 验收标准

功能验收：

- 启动后默认处于鼠标穿透模式，不影响桌面操作。
- `Ctrl + Alt + P` 后可在屏幕上绘制红色默认笔迹。
- 工具栏可切换笔、橡皮、颜色、粗细。
- `Ctrl + Z` 能撤销上一条绘制或擦除操作。
- `Ctrl + Alt + C` 能清空所有笔迹。
- `Ctrl + Alt + M` 后笔迹仍显示，但鼠标可点击到底层应用。
- `Esc` 后 overlay 不再遮挡屏幕。

技术验收：

- 领域层不依赖 JavaFX、JNA、JNativeHook。
- 核心领域服务有单元测试。
- 全局热键回调不直接操作 JavaFX 控件。
- 模式切换逻辑集中，不散落在多个 UI 控件里。
- 没有数据库、没有 Spring、没有多余后台线程池。

## 6. 验证与回退计划

本地验证命令：

```powershell
mvn test
mvn javafx:run
```

手工验证矩阵：

- Windows 11 + 100% DPI
- Windows 11 + 125% DPI
- 浏览器、PPT、IDE、桌面空白区域上的画笔/穿透切换
- 连续快速触发 `Ctrl + Alt + P`、`Ctrl + Alt + M`、`Esc`

回退策略：

- 如果 JNativeHook 在目标机器不稳定，保留工具栏按钮作为所有能力的备用入口。
- 如果擦除分段性能不达标，先切换为“整条 stroke 命中删除”，保留接口不变。
- 如果透明穿透在个别系统版本异常，增加启动参数禁用穿透，仅保留显示/隐藏和绘制。

## 7. 领导质询预案

问：为什么桌面工具也要 DDD？

答：这里不是引入重型企业架构，而是用 DDD 保证核心笔迹状态和系统适配分离。JavaFX、JNA、JNativeHook 都是易变边界，领域层保持纯 Java 后，撤销、擦除、清屏可以被单测保护，后续加保存、压感、多屏不会重写核心逻辑。

问：为什么不直接所有逻辑写在 JavaFX Controller？

答：这样第一天最快，但热键线程、鼠标事件、窗口样式、笔迹状态会耦合在一起。撤销和橡皮擦最容易变成不可测状态机。当前方案只增加少量包结构，换来可测试和可扩展的边界。

问：为什么不用 Spring Boot？

答：这是单机桌面覆盖层，没有 HTTP 服务、数据库事务、IoC 复杂装配需求。Spring 会增加启动时间、打包体积和运行模型复杂度。MVP 用手写装配更直接。

问：为什么选择 Canvas 全量重绘？

答：MVP 目标是稳定和简单。笔迹数据量可控时，全量重绘更容易保证画面一致性和撤销正确性。性能风险通过点数压测验证；超过阈值后再加离屏缓存，不提前复杂化。

问：最大技术风险是什么？

答：透明置顶窗口和鼠标穿透在 Windows 上的行为。缓解方式是把 JNA 调用集中在一个适配器里，并把画笔、鼠标、隐藏三种状态作为明确状态机处理，避免 UI 各处直接切换窗口样式。

## 8. 需要审批的决策点

请确认以下决策后再进入编码：

- 是否同意第一版只支持 Windows 主屏，先不做完整多屏适配。
- 是否同意第一版不做压感。
- 是否同意第一版不引入 Spring Boot。
- 是否同意默认橡皮擦实现为“局部分段擦除”，若性能不达标再降级为整条删除。
- 是否同意使用 Maven 作为构建工具。
