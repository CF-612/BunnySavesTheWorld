# Bunny Saves the World：AI 项目上下文

> 本文件是其他对话中的 AI 接手本工程时的首要入口。文件名使用英文以便检索，内容使用中文以便项目成员阅读。

## 接手前先做什么

1. 阅读本文件，了解工程边界和最近一次系统化改造。
2. 涉及检查点、死亡重生、对话、NPC、主菜单、场景转换或开场剧情 UI 时，再阅读：
   - `Docs/Architecture/UdemySystemComparison.md`
   - `Docs/AI/UnityProjectContext.md`
3. 修改 Unity 场景、预制体、Animator Controller 或 ProjectSettings 前，先确认现有引用关系；这些序列化资源风险较高。
4. 不要把用户已有且未纳入版本控制的 `.claude/` 目录加入提交。

## 项目概况

- 工程路径：`D:/GameCreator/Document/BunnySavesTheWorld`
- Unity：`6000.4.0f1`
- 类型：2D 单机平台冒险游戏
- 渲染管线：Built-in Render Pipeline
- 输入：Input System 与旧 Input Manager 同时启用
- 对话：NodeCanvas Dialogue Trees
- UI：uGUI
- 第一方运行时代码：`Assets/Resources/Scripts`
- 启用场景：`MainMenu`、`Home_Light`、`Home_Dark`、`City`、`City_Giant`、`Giant`

## 当前核心结构

| 模块 | 主要职责 | 位置 |
| --- | --- | --- |
| `GameProgressService` | JSON 进度、续玩场景、检查点记录 | `Assets/Resources/Scripts/GameFlow/GameProgressService.cs` |
| `SceneTransitionService` | 跨场景常驻的淡入淡出和异步加载 | `Assets/Resources/Scripts/GameFlow/SceneTransitionService.cs` |
| `StorySequence` | 菜单与传送门共用的剧情页顺序和显隐 | `Assets/Resources/Scripts/GameFlow/StorySequence.cs` |
| `LevelManager` | 当前场景内的死亡、等待和重生流程 | `Assets/Resources/Scripts/LevelManager/LevelManager.cs` |
| `CheckPoint` | 检查点触发、稳定标识、表现和存档通知 | `Assets/Resources/Scripts/LevelManager/CheckPoint.cs` |
| `Player` | 玩家状态、输入生命周期和按持有者区分的输入锁 | `Assets/Resources/Scripts/Player/Player.cs` |
| 菜单、传送门、NPC、自动对话脚本 | 保留场景配置入口，将系统工作委托给上述模块 | `Assets/Resources/Scripts/UI`、`Assets/Resources/Scripts/LevelManager` |

## 2026-08-31 系统化改造

功能提交：`240e643fec84e8a9cc49b1175338367b4dd3e770`

本次改造参考了 Udemy 示例工程：

`D:/GameCreator/资源/7. Udemy Course - RPG New - Dialogue System/Udemy Course - RPG New`

采纳的是“职责集中、调用方变薄”的结构思路，没有直接复制整套 RPG 框架：

- 新增窄范围 JSON 进度服务，只保存场景和检查点信息。
- 检查点拥有稳定标识；未手动填写 ID 的旧场景对象使用“场景名 + 层级路径”生成标识。
- 死亡后仍采用当前场景内快速重生；从主菜单续玩时才消费一次检查点定位请求。
- 场景加载统一交给常驻服务，使用异步加载、全屏黑幕和不受时间缩放影响的淡入淡出。
- 主菜单和传送门重复的剧情翻页逻辑提取为 `StorySequence`。
- 保留 NodeCanvas 作为唯一对话制作系统；NPC 与自动对话只监听自己启动的对话完成回调。
- 玩家输入锁按持有者管理，避免对话结束时误解除死亡或过场剧情的输入锁。
- Input System 回调改为只订阅一次，并在对象销毁时取消订阅和释放资源。

明确未迁移：战斗、背包、装备、制作、技能、商店、任务，以及 Udemy 示例中用于替换 NodeCanvas 的对话系统。

## 存档与菜单行为

- 存档位置：`Application.persistentDataPath/bunny-progress.json`
- 激活检查点时立即记录检查点 ID、场景和重生位置。
- 普通场景传送只更新最近进入的场景，不会强制把玩家传回检查点。
- 主菜单现在分为两个按钮：`startButton`（开始新游戏）会重置进度并播放开场剧情；`continueButton`（继续游玩）只在有存档时可用，并加载存档场景。
- `MainMenuController.StartNewGame()` 和 `MainMenuController.ContinueGame()` 分别是两个按钮的独立入口；`StartGame()` 仅保留为旧 UnityEvent 的兼容别名。
- 当前没有多存档槽和面向玩家的存档选择界面；继续按钮无存档时自动禁用。

本次未提交的生命周期修复：对话脚本只释放自己实际取得的输入锁，使用 Unity 对象有效性判断；`Player` 在销毁期间不再刷新输入状态。待在 Unity Play Mode 中验证场景切换、对话中切换和 NPC 对话退出后，再与主菜单改动一起提交。

本次未提交的开场剧情过渡修复：`StorySequence` 在确认没有下一页之前不再隐藏当前页。这样开场剧情或传送门剧情结束时，最后一页会继续留在画面上，由常驻 `SceneTransitionService` 的黑幕渐变覆盖，避免剧情页先隐藏而黑幕下一帧才开始时短暂露出主菜单背景。

## 修改时必须遵守

- 不要重命名现有序列化字段、脚本 GUID 或 UnityEvent 已绑定的方法，除非同步完成场景和预制体迁移。
- 不要用 Udemy 示例对话系统替换 NodeCanvas。
- 不要为了这些功能引入示例工程的战斗、任务、商店等依赖。
- 优先保持菜单、传送门和对话脚本为轻量场景适配器；共享状态和跨场景生命周期应放在所属服务中。
- 只提交当前任务相关文件，保留用户的其他未提交改动。
- 面向用户的说明文档、代码注释、交付总结和 Git commit 标题/正文默认使用中文；文件名、类名、方法名等技术标识符保留英文。

## 验证现状与已知问题

- 本次功能改造已使用 Unity 工程生成的 Roslyn 响应文件完成 C# 编译，结果无错误。
- 当时工程正在另一个 Unity Editor 实例中打开，因此独立批处理实例无法完成完整导入和 Play Mode 验证。
- 仍需人工覆盖：死亡动画时序、检查点表现、所有传送门模式、菜单续玩/新游戏体验、各类 NodeCanvas 对话的输入锁。
- 已知既有问题：`Assets/Resources/Animations/Player/Player.controller` 约第 1058 行附近存在超出范围的本地 FileID；这不是本次改造引入的。
- 既有编译警告包括自定义 `DialogueUGUI` 与 NodeCanvas 程序集同名类型冲突，以及 SmartLighting2D 旧 API 警告；本次改造未扩大这些问题。

## 更详细的资料

- 工程环境、目录和程序集：`Docs/AI/UnityProjectContext.md`
- 与 Udemy 示例的逐项比较、迁移决策和人工检查清单：`Docs/Architecture/UdemySystemComparison.md`
