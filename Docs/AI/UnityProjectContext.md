# Unity 工程上下文

<!-- unity-onboarding:generated:start -->

## 工程摘要

- 工程根目录：`D:/GameCreator/Document/BunnySavesTheWorld`
- 最后整理日期：2026-08-31
- 最近一次功能改造提交：`240e643fec84e8a9cc49b1175338367b4dd3e770`
- 游戏类型：2D 单机平台冒险；玩家使用纯 C# 状态机，项目使用 NodeCanvas 对话和 uGUI，并由场景对象承载关卡配置。

## 已确认的运行环境

- Unity 版本：6000.4.0f1
- 渲染管线：Built-in Render Pipeline（`GraphicsSettings.asset` 未配置自定义渲染管线）
- 输入系统：Input System 与旧 Input Manager 同时启用
- 已生成工程中观察到的目标平台：Windows 64 位桌面端

## 重要包与框架

| 领域 | 结论 | 证据 |
| --- | --- | --- |
| 输入 | Input System 1.19.0，并保留旧输入系统 | `Packages/manifest.json`、`ProjectSettings/ProjectSettings.asset` |
| 对话 | NodeCanvas Dialogue Trees | `Assets/ParadoxNotion/NodeCanvas`、第一方 UI 脚本 |
| UI | uGUI 2.0.0 | 包清单和运行时 UI 脚本 |
| 场景/内容加载 | 已安装 Addressables；当前目标流程仍使用 `SceneManager` | 包清单和第一方场景脚本 |
| 相机/演出 | Cinemachine 3.1.6、Timeline 1.8.11 | 包清单 |
| 补间动画 | 已导入 DOTween 模块 | `Assets/Plugins/Demigiant` |

## 主要目录

| 路径 | 用途 |
| --- | --- |
| `Assets/Resources/Scripts` | 第一方运行时代码 |
| `Assets/Resources/Prefab` | 共享游戏预制体，包括 `LevelManager` |
| `Assets/Scenes` | 主菜单和游戏场景 |
| `Assets/ParadoxNotion` | NodeCanvas 第三方代码 |
| `Assets/FunkyCode` | SmartLighting2D 第三方代码与示例 |

## 程序集边界

| 程序集 | 职责 | 说明 |
| --- | --- | --- |
| `Assembly-CSharp` | 第一方运行时代码及 SmartLighting2D 脚本 | 当前第一方代码没有 asmdef，整体较集中 |
| `NodeCanvas` | 对话与图运行时 | 第三方程序集 |
| `ParadoxNotion` | NodeCanvas 基础层 | 第三方程序集 |
| `DOTween.Modules` | 补间动画集成 | 第三方程序集 |

## 场景与启动流程

- 构建列表中启用的场景：`MainMenu`、`Home_Light`、`Home_Dark`、`City`、`City_Giant`、`Giant`。
- 启动场景：`MainMenu`。
- 场景流转：`MainMenuController` 和 `ScenePortal` 调用共享的 `SceneTransitionService`；`SceneEntrance` 提供场景内淡入配置入口。
- 检查点续玩：`GameProgressService` 保存各场景最新检查点；从菜单续玩时，`LevelManager` 消费一次检查点定位请求。

## 当前架构

| 模式 | 结论 | 证据 |
| --- | --- | --- |
| 玩家行为 | `Player` MonoBehaviour 持有纯 C# 状态对象 | `Player.cs`、状态机脚本 |
| 场景组合 | MonoBehaviour 通过场景/预制体和序列化引用完成配置 | 游戏场景、`LevelManager.prefab` |
| 对话 | NodeCanvas 图负责内容，触发器/UI 脚本作为轻量适配器 | 对话脚本和场景引用 |
| 持久化 | 静态、窄范围 JSON 服务保存场景与检查点进度 | `GameFlow/GameProgressService.cs` |
| 跨场景表现 | 常驻场景过渡服务管理运行时黑幕 | `GameFlow/SceneTransitionService.cs` |

## 编码约定

- 第一方玩法脚本目前使用全局命名空间。
- 序列化字段混用 public 和 `[SerializeField] private`；为保持场景兼容，现有名称不能随意修改。
- 异步流程主要使用 Unity 协程和 `SceneManager.LoadSceneAsync`。
- 面向用户和项目成员的文档、代码注释、Inspector 标题、交付总结、Git commit 标题及正文默认使用中文；技术标识符保留英文。

## 测试与验证

- 未发现第一方 EditMode 或 PlayMode 自动化测试。
- 可以使用 `Library/Bee` 生成的响应文件进行 Unity Roslyn 编译验证。
- 原 `Player.controller` 中由蓄力跳 Entry 过渡使用的异常本地 FileID，已随蓄力跳功能和专用 Animator 状态的删除一并移除。

## 可用 Unity 工具

| 能力 | 状态 | 说明 |
| --- | --- | --- |
| Unity Editor 6000.4.0f1 | 可用 | `D:/GameCreator/Unity/Editor/Unity.exe` |
| Unity 批处理编译 | 工程未被其他 Editor 占用时可用 | 上次运行因工程锁而中止 |
| Unity MCP 读写工具 | 当前会话不可用 | 当前未暴露相关工具 |
| C# 响应文件编译 | 可用 | `Library/Bee/artifacts/*/Assembly-CSharp.rsp` |

## 重要限制

- 保留场景和预制体中的字段名，以及现有 UnityEvent 方法名。
- 不引入 Udemy 示例中的战斗、背包、任务、商店或技能依赖。
- 继续以 NodeCanvas 作为权威对话制作系统。
- 场景、预制体、Animator Controller 和 ProjectSettings 均按高风险序列化资源处理。
- 不要把 `.claude/` 或无关的编辑器生成变动加入功能提交。

## 尚需确认的运行时行为

- 仍需在 Play Mode 人工覆盖死亡时序、检查点表现、所有传送门路线，以及菜单续玩/新游戏体验。
- 仍需检查手动和自动 NodeCanvas 对话在各种组合情况下是否正确持有与释放玩家输入锁。
- 当前没有面向用户的存档槽选择界面；现有开始按钮为“有存档则续玩，否则开始”，另有 `StartNewGame` 方法可供未来单独按钮调用。

## 主要证据文件

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/manifest.json`
- `Assets/Resources/Scripts/Player/Player.cs`
- `Assets/Resources/Scripts/LevelManager/*`
- `Assets/Resources/Scripts/UI/*Dialogue*`
- `Assets/Resources/Scripts/UI/MainMenuController.cs`
- `Assets/Resources/Prefab/LevelManager.prefab`
- 相关游戏场景及 Udemy 参考工程的对应脚本

<!-- unity-onboarding:generated:end -->
