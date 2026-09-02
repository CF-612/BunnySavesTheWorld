# 移动与跳跃手感优化需求

## 文档说明

本文档是 Bunny Saves the World 玩家移动与跳跃优化的最终需求案，也是后续实现、调参与验收的唯一权威说明。两篇外部资料仅用于理解加减速、跳跃缓冲、土狼时间和可变跳高等概念，其中的直接修改 Transform、八方向归一化、非线性曲线、最大高度限制和联机预测方案不作为本项目实现要求。

当前代码功能已经按本文档实现；初始参数仍需在 Unity Play Mode 中结合实际关卡调校。

---

## 功能名称

玩家移动与跳跃手感优化

## 功能概述

在保留现有纯 C# 玩家状态机、Rigidbody2D、Input System、风区、滑翔、跺脚、管道和单向平台下落的基础上，将基础移动调整为偏灵敏的平台动作手感，并为普通跳跃增加操作容错和可变高度。同时彻底删除原有蓄力跳跃，并让地面移动动画、脚步频率和实际水平速度保持一致。

本需求仅包含：

- 地面加速、减速和反向制动。
- 空中加速和减速。
- 按下立即起跳。
- 跳跃缓冲和土狼时间。
- 松手截断上升速度形成的小跳/大跳。
- 蓄力跳跃代码与专用动画资源的完整移除。
- Idle 状态下的地面风力推动。
- 地面移动动画播放速度与脚步频率联动。

本次不包含二段跳、风区参数化重构、专用受风状态或动画、调试 UI、额外特效或输入绑定扩展。

## 功能需求

### 1. 核心功能

- [x] 地面加速：有水平输入时，当前速度使用 `GroundAccel` 逐步接近 `MoveSpd` 对应的目标速度。
- [x] 地面减速：松开水平输入后，角色使用 `GroundDecel` 在短距离内停止，不在进入 Idle 时瞬间清零速度。
- [x] 反向制动：输入方向与当前速度方向相反时，使用 `ReverseBrake` 快速完成反向。
- [x] 空中控制：无风环境中，水平速度使用 `AirAccel` 和 `AirDecel` 逐步接近目标速度。
- [x] 立即起跳：地面按下空格时立即进入 `Player_JumpState`，不等待松键，不再检测长按阈值。
- [x] 跳跃缓冲：落地前 `JumpBufferDuration` 秒内按下空格，正常落地后自动消费一次输入并起跳。
- [x] 土狼时间：离开平台后 `CoyoteTimeDuration` 秒内按下空格，仍允许消费一次地面跳跃。
- [x] 可变跳高：上升阶段松开空格时，将当前正向纵向速度乘以 `JumpCutMultiplier`；持续按住则保留完整跳跃高度。
- [x] 单次消费：一次地面跳跃输入只能执行一次；普通起跳后再次按空格不能形成二段跳。
- [x] 蓄力跳移除：删除蓄力状态、参数、力度接口、Animator 参数、状态、过渡和专用动画资源。
- [x] 状态切换保护：状态发生切换后，原状态不得继续写入速度或播放脚步声。
- [x] Idle 受风：没有有效水平输入时保持 Idle 状态和动画，但活动风区仍可通过现有直接速度路径推动角色。
- [x] 地面风力共用：MoveState 与 IdleState 使用 `GetGroundTargetSpd()` 计算地面目标速度，不复制顺风、逆风或无输入受风规则。
- [x] 移动动画联动：`Player_Move` 使用实际水平速度计算出的 `moveSpdMultiplier` 调整播放速度，倍率限制为 `0.5～1.5`。
- [x] 脚步联动：移动状态下的脚步计时使用相同速度倍率；实际水平速度接近零时不推进计时或播放脚步声。
- [x] 既有风区兼容：不新增或修改风区参数体系，保留空中风力与朝向防抖逻辑。

### 2. UI/交互需求

- [x] 不新增面向玩家的常驻 UI、调试 UI 或蓄力反馈。
- [x] Inspector 中为新增移动和跳跃参数提供中文 Tooltip。
- [x] 普通空格只负责普通跳跃的小跳/大跳，不存在任何蓄力组合输入。
- [x] `JumpDown` 保持现有 S 键绑定和单向平台下落行为，不增加键盘下方向键或手柄绑定。

### 3. 数据需求

- [x] 保留现有序列化字段 `MoveSpd`、`JumpForce` 和 `InAirMoveMultiplier`，兼容既有字段，不重命名。
- [x] 在共享 `Player.prefab` 保存五个移动参数和三个跳跃参数。
- [x] 最近跳跃输入时间、最近接地时间和地面跳跃资格仅为运行时状态，不序列化、不存档。
- [x] 输入锁定、组件禁用、死亡、进入管道、传送和重生时清除运行时跳跃上下文。

## 技术实现要求

### 脚本结构

```text
Player.cs
├── 移动参数
│   ├── MoveSpd：既有地面最大速度，兼容既有字段，不重命名
│   ├── GroundAccel：地面加速度（Accel = Acceleration）
│   ├── GroundDecel：地面减速度（Decel = Deceleration）
│   ├── ReverseBrake：反向制动加速度
│   ├── AirAccel：空中加速度（Accel = Acceleration）
│   └── AirDecel：空中减速度（Decel = Deceleration）
├── 跳跃参数
│   ├── JumpForce：既有跳跃初速度，兼容既有字段，不重命名
│   ├── InAirMoveMultiplier：既有空中输入倍率，兼容既有字段，不重命名
│   ├── JumpBufferDuration：跳跃缓冲时长
│   ├── CoyoteTimeDuration：土狼时间
│   └── JumpCutMultiplier：松手后保留的纵向速度比例
├── 运行时跳跃上下文
│   ├── lastJumpPressedTime：最近跳跃输入时间
│   ├── lastGroundedTime：最近接地时间
│   └── canUseGroundJump：当前是否仍有一次地面跳资格
└── 核心方法
    ├── ApplyHorizontalVelocity()：按移动阶段平滑逼近水平目标速度
    ├── GetGroundTargetSpd()：合并输入与当前风力，生成地面目标速度
    ├── GetMoveAnimSpdMultiplier()：按实际水平速度生成移动表现倍率
    ├── RecordJumpInput()：记录跳跃按下时间
    ├── RefreshGroundJumpWindow()：刷新接地时间和地面跳资格
    ├── TryConsumeGroundJump()：检查并消费一次地面跳
    └── ResetJumpContext()：清除未消费输入和跳跃资格

Player_GroundedState.cs
├── 地面时刷新跳跃资格
├── 记录空格按下输入
├── 优先消费普通跳跃
├── 未起跳时再检测物理下落
└── 保留啃咬、跺脚、S 键下穿平台和刨坑入口

Player_MoveState.cs / Player_idleState.cs
├── MoveState：使用共用目标速度执行地面加速、反向制动、风区移动及脚步联动
├── IdleState：无风时减速至零，有风时保持 Idle 动画并接受风力推动
└── 状态切换后停止执行旧状态逻辑

Player_AirState.cs
├── 记录空中跳跃输入
├── 在土狼时间内消费地面跳
├── 无风时应用空中加减速
└── 有风时保留现有直接速度与朝向防抖逻辑

Player_JumpState.cs
├── Enter()：保留当前水平速度并施加 JumpForce
├── ApplyJumpCut()：松手时截断正向纵向速度
└── Update()：上升结束后进入 FallState

Player_FallState.cs
├── 保留坠落死亡判定
├── 正常落地时刷新地面跳资格
└── 优先消费仍有效的跳跃缓冲，否则进入 IdleState
```

### 依赖关系

- `Player`：移动参数、地面目标速度、移动表现倍率、跳跃参数和运行时跳跃上下文的唯一所有者。
- `Player_GroundedState`、`Player_AirState`、`Player_JumpState`、`Player_FallState`：采集输入并根据 `Player` 提供的结果切换状态。
- `Entity.SetVelocity()`：继续负责 Rigidbody2D 速度写入、击退保护和角色翻转。
- `PlayerWindReceiver`、`WindZoneData`：保持既有实现，不新增字段或接口。
- `PlayerInputSet`：保持现有 Input Action 和 ID，不重新生成包装代码。
- `Player.controller`：保留普通 `jumpFall` BlendTree；`Player_Move` 通过 `moveSpdMultiplier` 调整播放速度。

## 参数详细说明

| 参数名 | 类型 | 初始值 | 说明 | 目标手感 |
|---|---|---:|---|---|
| `MoveSpd` | float | 8 | 既有地面最大速度 | 保持当前最高速度 |
| `GroundAccel` | float | 80 | 地面起步加速度（Accel = Acceleration） | 约 0.10 秒达到最大速度 |
| `GroundDecel` | float | 100 | 松手后的地面减速度（Decel = Deceleration） | 约 0.08 秒从全速停止 |
| `ReverseBrake` | float | 140 | 反向输入时的制动加速度 | 约 0.11 秒从全速正向切到全速反向 |
| `AirAccel` | float | 50 | 无风时的空中加速度（Accel = Acceleration） | 空中可修正但弱于地面 |
| `AirDecel` | float | 30 | 无风时的空中减速度（Decel = Deceleration） | 松手后保留少量惯性 |
| `JumpForce` | float | 15 | 既有普通跳跃初速度 | 保持当前完整跳高度基线 |
| `InAirMoveMultiplier` | float | 0.8 | 既有空中目标速度倍率 | 空中最大目标速度为地面的 80% |
| `JumpBufferDuration` | float | 0.12 | 落地前可缓存跳跃输入的时间 | 容忍提前数帧按键 |
| `CoyoteTimeDuration` | float | 0.10 | 离开平台后的起跳宽限时间 | 容忍平台边缘的轻微延迟 |
| `JumpCutMultiplier` | float | 0.5 | 松手后保留的正向纵向速度比例 | 短按明显低于长按 |
| `moveSpdMultiplier` | Animator Float | 1 | `abs(linearVelocity.x) / MoveSpd`，运行时限制为 0.5～1.5 | 满速为原动画速度，低速和顺风超速同步变化 |

以上数值是灵敏平台动作的第一轮 Play Mode 调校基线。后续只在这些字段内调整手感，不在本需求中扩展新机制。

## 行为描述

### 场景1：地面起步、松手与反向

1. 用户操作：按住左/右方向、松开方向键，或在全速移动时输入反方向。
2. 系统响应：分别使用 `GroundAccel`、`GroundDecel` 或 `ReverseBrake` 逼近目标速度。
3. 预期结果：起步灵敏、松手短距离停止、反向清晰，不出现瞬间速度跳变或长距离滑行。

### 场景2：普通小跳与大跳

1. 用户操作：按下空格后立即松开，或持续按住空格。
2. 系统响应：按下时立即以 `JumpForce` 起跳；上升阶段松手时应用 `JumpCutMultiplier`。
3. 预期结果：短按形成小跳，长按形成完整大跳；长按不会进入蓄力状态。

### 场景3：跳跃缓冲

1. 用户操作：角色下落且即将落地时提前按下空格。
2. 系统响应：记录输入；若在 `JumpBufferDuration` 内正常落地，则刷新地面跳资格并立即消费本次输入。
3. 预期结果：自动执行一次跳跃；超时输入不执行，且一次输入不会重复起跳。

### 场景4：土狼时间

1. 用户操作：角色刚离开平台后按下空格。
2. 系统响应：若仍在 `CoyoteTimeDuration` 内且尚未消费地面跳资格，则立即起跳。
3. 预期结果：边缘起跳符合玩家预期；窗口结束后按键不会成为二段跳。

### 场景5：下穿单向平台

1. 用户操作：站在单向平台上按下 S。
2. 系统响应：调用现有 `TryDropThroughPlatform()` 暂时忽略平台碰撞。
3. 预期结果：正常下穿平台；该输入不参与普通跳跃或任何蓄力组合。

### 场景6：输入被锁定或玩家重生

1. 系统操作：对话、过场、死亡或管道流程锁定输入，随后恢复输入或重生。
2. 系统响应：锁定、禁用、传送和重生时清除跳跃上下文。
3. 预期结果：恢复控制后不会执行锁定前残留的跳跃输入。

### 场景7：进入现有风区

1. 用户操作：角色进入已配置的风区，随后松开水平输入。
2. 系统响应：角色进入或保持 IdleState 和 Idle 动画，但水平速度继续使用无输入风力目标值。
3. 预期结果：角色可被风自然推动，不播放移动脚步声；重新输入后恢复既有顺风、逆风和朝向逻辑。

### 场景8：移动动画与脚步速度联动

1. 用户操作：角色从静止加速到满速，并在风区中顺风或逆风移动。
2. 系统响应：`moveSpdMultiplier` 按实际水平速度在 `0.5～1.5` 内变化，脚步计时使用相同倍率。
3. 预期结果：正常满速倍率为 `1`；低速动画与脚步变慢，顺风超速时变快，实际速度接近零时不播放脚步声。

## 边界条件

- 跳跃输入必须同时满足“缓冲仍有效”和“接地或土狼时间仍有效”，并具有尚未消费的地面跳资格。
- 普通起跳会立即消费地面跳资格；空中再次按键不能起跳，只能在接近落地且仍处于缓冲窗口时用于落地跳。
- 高处坠落达到死亡阈值时，死亡优先于落地跳跃缓冲。
- 缓冲跳触发时若空格已经松开，起跳首帧立即应用 `JumpCutMultiplier`，按小跳处理。
- 只有纵向速度大于零时才能应用跳跃截断；下落阶段松键不改变速度。
- 移动加速度、减速度和反向制动均限制为非负速率。
- 击退期间继续由 `Entity.SetVelocity()` 阻止普通状态覆盖击退速度。
- 状态切换后派生状态必须立即停止本帧旧状态逻辑。
- Idle 表示没有有效主动移动输入，不表示水平速度必须为零；风力可以在 Idle 状态写入水平速度。
- `moveSpdMultiplier` 只绑定 `Player_Move`，不得修改全局 `Animator.speed` 或其他状态速度。
- 不重命名现有序列化字段、UnityEvent 方法、Input Action 或资源 GUID。
- 不修改场景文件；所有启用场景继续继承共享 `Player.prefab` 参数。

## 注意事项

- 本项目使用 Rigidbody2D 速度驱动，不改为直接修改 Transform。
- 时间窗口使用受 `Time.timeScale` 影响的游戏时间；暂停期间不会消耗跳跃缓冲或土狼时间。
- 移动计算继续运行在现有状态机的 `Update()` 生命周期中，使用 `Time.deltaTime`；本需求不重构为 FixedUpdate 架构。
- 不新增 `PlayerMovementProfile`、二段跳计数、风区移动倍率、专用受风状态或调试面板。
- `moveSpdMultiplier`、`GetGroundTargetSpd()` 和 `GetMoveAnimSpdMultiplier()` 遵循工程缩写规范，速度相关新增标识符使用 `Spd`，动画相关新增标识符使用 `Anim`。

## 参考代码

- `Assets/Resources/Scripts/Player/Player.cs`：移动参数、跳跃参数、输入生命周期和跳跃上下文。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_GroundedState.cs`：地面跳跃与 S 键下穿平台入口。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_MoveState.cs`：地面移动、共用风区目标速度和脚步联动。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_idleState.cs`：无风减速与无输入受风。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_AirState.cs`：空中控制、土狼时间输入和既有空中风区逻辑。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_JumpState.cs`：跳跃初速度和松手截断。
- `Assets/Resources/Scripts/Player/PlayerStates/Player_FallState.cs`：坠落死亡、落地和跳跃缓冲消费。
- `Assets/Resources/Animations/Player/Player.controller`：普通跳跃状态机及 `Player_Move` 动画速度绑定。
- `C:/Users/fangcheng612/Downloads/游戏角色移动功能实现与手感调校详解.md`：加减速概念参考，不作为工程指令。
- `C:/Users/fangcheng612/Downloads/游戏开发中的跳跃机制详解.md`：跳跃缓冲、土狼时间和可变跳高概念参考，不作为工程指令。
