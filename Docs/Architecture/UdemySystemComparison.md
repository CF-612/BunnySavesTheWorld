# Udemy 系统比较与迁移决策

## 比较范围

本次比较覆盖当前工程与下列 Udemy 参考工程中的检查点/死亡重生、对话/NPC、主菜单、场景转换和开场剧情 UI：

`D:/GameCreator/资源/7. Udemy Course - RPG New - Dialogue System/Udemy Course - RPG New`

战斗、背包、装备、制作、技能、商店和任务系统明确排除在外。

## 总体判断

参考工程覆盖面更广，但并非所有实现都更成熟。真正值得吸收的是职责划分：常驻的游戏流程管理者协调存档和场景切换，检查点具有持久化身份，淡入淡出 UI 能够复用。

不建议直接复制参考实现。其对话流程依赖任务、商店和制作系统，存档对象发现方式只在管理器启动时收集当前场景对象，部分示例运行时代码还包含仅适用于编辑器或测试的引用。因此，本项目保留 NodeCanvas，只迁移能够解决当前重复与生命周期问题的窄范围结构。

## 逐项结构比较

| 功能 | 本项目改造前 | Udemy 参考工程 | 已实施决策 |
| --- | --- | --- | --- |
| 检查点状态 | `LevelManager` 只保存一个运行时 `Vector3`，重新进入游戏后丢失 | `Object_Checkpoint` 有 ID 并实现 `ISaveable` | 保留轻量场景协调者，增加稳定检查点身份和窄范围 JSON 进度服务 |
| 死亡/重生 | 静态死亡事件配合协程，重复死亡和输入所有权不明确 | 玩家死亡状态把场景重启/位置选择交给 `GameManager` | 保留当前场景内快速重生；防止重复死亡；明确输入锁所有者；只在菜单续玩时从存档检查点出生 |
| 存档数据 | 这些功能没有持久化系统 | 一个大型 `GameData` 混合背包、技能、任务、传送门和检查点 | 使用只包含场景/检查点进度的 `GameProgressData` |
| 场景过渡 | 各调用方自行创建 Canvas，并同步调用 `LoadScene` | 常驻 `GameManager` 配合可复用 `UI_FadeScreen` | 新增常驻 `SceneTransitionService`，使用异步加载、不受时间缩放影响的淡入淡出和输入遮罩 |
| 主菜单 | 一个类同时处理按钮效果、开场页面、音频和场景加载 | 轻量 UI 调用 `GameManager.ContinuePlay()` | 保留美术配置的按钮效果，把持久化、剧情页状态和场景加载委托给专属模块 |
| 开场/传送门剧情页 | 菜单和传送门重复维护数组与索引 | 使用数据驱动对话，但与 RPG 行为强耦合 | 提取 `StorySequence`，保留现有场景美术和操作体验 |
| NPC 交互 | 每个 NPC 轮询 E 键，并查看共享对话 UI 状态 | `IInteractable` NPC 基类，但依赖任务和玩家单例 | 保留 NodeCanvas 轻量适配器，改用当前对话实例的完成回调和玩家输入锁 |
| 自动对话 | 订阅全局 `DialogueTree.OnDialogueFinished` | 对话 UI 自行管理行推进 | 使用 `DialogueTreeController.StartDialogue(callback)`，确保只有该触发器启动的图能够完成自身流程 |
| 玩家输入生命周期 | 每次 `OnEnable` 添加匿名回调，重复启用会重复订阅 | 中央玩家输入管理者 | 在 `Awake` 订阅一次，在 `OnDestroy` 取消并释放；使用按持有者区分的输入锁 |

## 未迁移的参考代码

- `UI_Dialogue`、`DialogueLineSO`、`DialogueNpcData`：它们会替换而不是改进 NodeCanvas，并依赖 RPG 行为枚举、奖励、任务、商店和制作。
- 完整 `GameData` 与 `SaveManager`：无关 RPG 状态会造成额外耦合；其可保存对象只在管理器 `Start` 时发现，跨场景使用较脆弱。
- `Object_NPC`：基类假定存在 `Player.instance.questManager`，并每帧更新悬浮 UI 和朝向。
- `Object_Portal`：面向“城镇往返战斗区域”的 RPG 门户模型，与本项目线性编排的场景传送门不一致。
- 示例中的问题代码，例如运行时脚本引用 `UnityEditor` 或 `NUnit.Framework.Interfaces`，均未复制。

## 改造后的职责归属

- `GameProgressService`：文件读写，以及检查点/续玩数据的权威来源。
- `SceneTransitionService`：常驻黑幕和异步场景生命周期。
- `StorySequence`：剧情页当前索引和显隐状态。
- `LevelManager`：当前场景内死亡、等待和重生时序。
- `CheckPoint`：触发、稳定身份、可选动画与音频表现。
- `Player`：输入回调生命周期、按持有者区分的输入锁和死亡状态。
- 菜单、传送门和对话脚本：只保留场景配置与交互适配职责。

## 存档行为

- 存档文件：`Application.persistentDataPath/bunny-progress.json`。
- 激活检查点时立即记录 ID、场景和重生位置。
- 普通传送门更新最近进入场景，但不会强制执行检查点传送。
- 从主菜单续玩时，请求在存档场景中执行一次检查点出生。
- 开始新游戏时重置窄范围进度，并在加载第一个场景前覆盖存档。
- `MainMenuController.StartNewGame()` 已提供给未来单独的“新游戏”按钮；现有开始按钮默认在有存档时续玩。

## 人工 Play Mode 检查清单

1. 删除或移走 `bunny-progress.json`，从 `MainMenu` 开始，确认开场剧情页和首个场景淡入淡出正常。
2. 在同一场景依次激活两个检查点并分别死亡，确认总是使用最新检查点。
3. 激活检查点后退出并重新启动，确认菜单续玩进入对应场景及检查点位置。
4. 逐一测试接触触发、按键触发、手动激活和带剧情页的传送门。
5. 测试手动与自动 NodeCanvas 对话，确认只有正确对话结束后才恢复移动。
6. 连续触发两个可能锁定输入的系统，确认一个系统结束不会解除另一个系统的锁。
7. 将单独按钮绑定到 `StartNewGame()`，确认旧检查点不会在新游戏中恢复。
