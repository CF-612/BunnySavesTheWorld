## 文档说明
本文档用于描述《兔兔拯救世界》中吹风机（Blower / 空调外机）机关的功能优化需求，便于 AI 助手理解现有代码架构、识别问题根因，并指导实现代码。

---

## 功能名称
吹风机（Blower）机关物理交互优化

## 功能概述
优化 Blower 机关与玩家的物理交互体验，使其从目前"仅通过 AreaEffector2D 微弱减速"升级为"真实风力推进系统"。核心目标是：**地面时玩家越靠近 Blower 阻力越大，最终无法接近；空中时玩家被强风吹飞，无法抵抗**。同时保持现有状态机系统的解耦架构不受破坏，并为后续滑翔系统预留扩展点。

---

## 功能需求

### 1. 核心功能

- [ ] **风力数据组件（WindZoneData）：** 新建独立的风力数据组件挂载在 Blower 的碰撞体上，替代单纯依赖 AreaEffector2D。该组件负责定义风力的方向、强度曲线、最大影响半径、空中/地面倍率等参数。

- [ ] **玩家风力响应系统：** 在 Player/Entity 层增加对风力区域的感知与响应能力，不硬编码在任何具体状态中。通过检测玩家当前是否处于风力区域内、距离风源的距离，计算外部风速向量。

- [ ] **地面渐进阻力：**
  - 玩家在地面（特别是 MoveState）时，水平移动速度受风力方向与距离共同影响
  - 玩家**顺风**时获得加速加成（速度 > MoveSpd）
  - 玩家**逆风**（顶风）时受到减速惩罚，越靠近风源惩罚越大，最终 net velocity ≤ 0，无法前进
  - 玩家垂直于风向移动时几乎不受影响（或仅轻微偏移）

- [ ] **空中吹飞：**
  - 玩家在空中（AirState / FallState / JumpState / GlideState）时，风力直接施加到玩家速度上
  - 玩家空中输入仅能微弱影响水平位移，无法抵消风力
  - 表现为玩家被风吹离原地，产生"被吹跑"的视觉反馈

- [ ] **滑翔兼容（GlideState 预留）：**
  - 将风力响应逻辑设计为可重用的工具方法或组件，不耦合在特定状态
  - 滑翔状态可通过读取风力数据实现"在风中借力上浮"或"逆风失速坠落"等进阶行为
  - GlideState 的后续实现可直接调用风力接口获取当前风速向量

### 2. UI/交互需求

- [ ] **视觉反馈：** Blower 开启时播放现有的粒子特效（windVFX）和音效，不需要额外 UI
- [ ] **无 HUD 指示：** 本功能不涉及 UI 面板变更（风力属于关卡机制，玩家通过视觉和操控体感感受风力）

### 3. 数据需求

- [ ] **风力参数结构：**
  - 风力方向（Vector2，Blower 的面朝方向）
  - 最大风力强度（float，核心推力值，单位：速度单位/秒）
  - 风力衰减曲线（AnimationCurve，x=0~1 表示从风源到边缘的归一化距离，y=0~1 表示风力倍率）
  - 最大影响半径（float，超过此距离风力为 0）
  - 地面阻力倍率（float，默认 0.4：地面时可抵抗部分风力）
  - 空中风力倍率（float，默认 1.5：空中时风力效果放大）
  - 最小接近距离（float，玩家逆风时能接近风源的最近距离）

- [ ] **数据结构关系：**
  - Blower 挂载 `WindZoneData` 组件配置风力参数
  - Player/Entity 通过触发器检测进入/离开风力区域
  - 在状态 Update 中通过统一的"外部速度修正"方法计算最终速度

---

## 技术实现要求

### 脚本结构

```
WindZoneData.cs（新建 — 挂载在 Blower 风力碰撞体上）
├── 参数配置
│   ├── windDirection：Vector2 — 风力方向（自动从 Blower 的 facingDir 或 transform.right 读取）
│   ├── maxWindForce：float — 最大风力强度
│   ├── windForceCurve：AnimationCurve — 风力随距离衰减曲线
│   ├── maxRadius：float — 最大影响半径
│   ├── groundResistMultiplier：float — 地面时玩家抵抗系数（0~1，越小玩家越能抗风）
│   ├── airForceMultiplier：float — 空中时风力放大系数（>1）
│   └── minApproachDistance：float — 玩家逆风能接近风源的最近距离
├── 核心方法
│   ├── GetWindForceAt(Vector2 worldPosition)：Vector2 — 计算某世界位置的风力向量（方向和大小已组合）
│   ├── GetNormalizedDistance(Vector2 worldPosition)：float — 返回归一化距离（0=风源，1=最大半径边缘，>1=范围外）
│   └── IsInWindZone(Vector2 worldPosition)：bool — 判断某位置是否在风力区域内
└── Gizmos
    └── OnDrawGizmos：在编辑器中可视化风力范围与方向

Blower.cs（修改 — 添加 WindZoneData 引用）
├── 新增引用
│   └── windZoneData：WindZoneData — 风力数据组件引用
├── 修改方法
│   ├── TurnOn()：同时启用 WindZoneData 的检测（如设置 enabled=true）
│   └── TurnOff()：同时禁用 WindZoneData

PlayerWindReceiver.cs（新建 — 挂载在 Player 上）
├── 参数配置
│   ├── activeWindZones：List<WindZoneData> — 当前玩家处于的风力区域列表
│   └── cachedWindVelocity：Vector2 — 缓存的合成风速向量（每帧计算一次）
├── 核心方法
│   ├── GetExternalWindVelocity()：Vector2 — 供状态机调用的统一接口，返回最终风速向量
│   ├── RegisterWindZone(WindZoneData zone)：进入风力区域
│   ├── UnregisterWindZone(WindZoneData zone)：离开风力区域
│   ├── CalculateCompositeWind()：Vector2 — 计算多个风力区域的合成风速（取最大影响力的风区，不叠加）
│   └── IsBeingBlown()：bool — 是否正在受风力影响
└── Unity 消息
    ├── OnTriggerEnter2D / OnTriggerStay2D / OnTriggerExit2D
    └── Update：每帧刷新 cachedWindVelocity

Player.cs（修改 — 添加 PlayerWindReceiver 引用）
├── 新增引用
│   └── windReceiver：PlayerWindReceiver — 风力接收器引用
└── 新增属性
    └── externalWindVelocity：Vector2 — 便捷访问属性，转发 windReceiver.GetExternalWindVelocity()

Player_MoveState.cs（修改 — 地面风力阻力）
├── 修改 Update()
│   └── 在调用 SetVelocity 前，根据 externalWindVelocity 和玩家移动方向计算实际速度：
│       ├── 计算玩家期望速度 desireVelocity = moveInput.x * MoveSpd
│       ├── 将 windVelocity 投影到水平方向得到 windX
│       ├── 逆风（desireVelocity 与 windX 方向相反）：
│       │   └── 应用 groundResistMultiplier 削减风力，使玩家可缓慢前进
│       │   └── 当距离 < minApproachDistance 时，netVelocity = 0（完全无法前进）
│       ├── 顺风（方向相同）：
│       │   └── 玩家速度 = desireVelocity + windX（加速效果）
│       └── 垂直风：仅轻微偏移

Player_AirState.cs（修改 — 空中吹飞）
├── 修改 Update()
│   └── 在调用 SetVelocity 前，将 externalWindVelocity 的 x 分量直接叠加到玩家输入速度上：
│       ├── float inputX = moveInput.x * MoveSpd * InAirMoveMultiplier
│       ├── float finalX = inputX + windVelocity.x * airForceMultiplier（注意倍率从 WindZoneData 获取）
│       └── player.SetVelocity(finalX, rb.linearVelocity.y)
```

### 依赖关系

- `WindZoneData` 依赖：无（独立组件，仅依赖 Unity 的 Collider2D 作为 Trigger）
- `PlayerWindReceiver` 依赖：`WindZoneData`（通过 OnTrigger 检测并注册）
- `Player` 依赖：`PlayerWindReceiver`（新增组件引用）
- `Player_MoveState` / `Player_AirState` 依赖：`Player.externalWindVelocity`（通过 Player 转发获取风速）
- `Player_GlideState`（未来）依赖：`Player.externalWindVelocity`（可复用的风速接口）

---

## 参数详细说明

### WindZoneData 参数

| 参数名 | 类型 | 默认值 | 说明 | 示例 |
|--------|------|--------|------|------|
| maxWindForce | float | 10f | 风源中心的最大推力，单位与 velocity 一致 | 12 |
| windForceCurve | AnimationCurve | Linear 1→0 | 风力沿风向的衰减曲线；x=0=上风边缘（风源，最大风力），x=1=下风边缘（风力最小） | EaseOut(1→0.2→0) |
| groundResistMultiplier | float | 0.4f | 地面时玩家对抗风力的系数（0~1），越小越容易顶风前进 | 0.35 |
| minApproachNormalized | float | 0.1f | 玩家逆风能接近风源的最小归一化距离，达到时地面速度归零 | 0.15 |
| airForceMultiplier | float | 1.5f | 空中时风力放大系数，使空中更易被吹飞 | 2.0 |
| overrideDirection | bool | false | 是否手动覆盖风向（默认自动从物体朝向推导） | false |
| customWindDirection | Vector2 | (-1, 0) | 手动指定的风力方向（世界空间），仅 overrideDirection=true 时生效 | (1, 0) |

> **注意**：风力区域边界由本 GameObject 上的 Collider2D（IsTrigger）定义，不再使用独立的 maxRadius 参数。风力仅在该 Collider 范围内生效。

### Player 现有参数（需了解，不改动）

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| MoveSpd | float | 8f | 玩家基础移动速度 |
| InAirMoveMultiplier | float | 0.8f | 空中移动速度倍率 |

---

## 行为描述

### 场景1：玩家在地面逆风接近 Blower

1. **用户操作：** 玩家按住 A/D 向 Blower 方向移动（逆风），Blower 处于开启状态
2. **系统响应：**
   - `PlayerWindReceiver` 检测到玩家处于风力区域，计算当前距离下的风速向量
   - `Player_MoveState.Update()` 获取 `externalWindVelocity.x`
   - 判定玩家移动方向与风向相反（逆风）
   - 计算减速后的 netVelocity：
     - 距离 > maxRadius：无影响
     - 距离在 minApproachDistance ~ maxRadius 之间：`netVelocity = desireVelocity - windX * groundResistMultiplier`
     - 距离 < minApproachDistance：`netVelocity = 0`（强制归零，无法接近）
   - 调用 `SetVelocity(netVelocity, rb.linearVelocity.y)`
3. **预期结果：** 玩家从远处接近 Blower 时移动速度逐渐降低，靠近到一定距离后完全无法前进，只能后退或选择绕路

### 场景2：玩家在地面顺风远离 Blower

1. **用户操作：** 玩家按住 A/D 向远离 Blower 方向移动（顺风），Blower 处于开启状态
2. **系统响应：**
   - `PlayerWindReceiver` 计算风速向量
   - 判定玩家移动方向与风向相同（顺风）
   - `netVelocity = desireVelocity + windX * groundResistMultiplier`（加速效果）
3. **预期结果：** 玩家顺风移动时速度明显快于正常移动速度，产生"被风推着走"的体感

### 场景3：玩家在空中被风吹飞

1. **用户操作：** 玩家在 Blower 风力范围内起跳，Blower 处于开启状态
2. **系统响应：**
   - `Player_AirState.Update()` 获取 `externalWindVelocity`
   - 将空中风力（`windVelocity * airForceMultiplier`）叠加到玩家输入速度上
   - 玩家输入 `moveInput.x * MoveSpd * InAirMoveMultiplier` 远小于风力，无法抵消
   - `finalX = inputX + windVelocity.x * airForceMultiplier`
3. **预期结果：** 玩家在空中被风吹离 Blower 方向，水平位移显著，无法通过方向键抵抗风力，营造"被吹跑"的视觉效果

### 场景4：玩家在风力范围外不受影响

1. **用户操作：** 玩家在 Blower 的 maxRadius 之外正常移动
2. **系统响应：** `PlayerWindReceiver` 的当前活跃风力列表为空，`GetExternalWindVelocity()` 返回 `Vector2.zero`
3. **预期结果：** 玩家移动行为与现有版本完全一致，无任何性能开销

### 场景5：Blower 关闭后风力消失

1. **用户操作：** 玩家啃咬电线使 Blower 断电关闭
2. **系统响应：** `Blower.TurnOff()` → 设置 `WindZoneData.enabled = false` → 触发器区域的 Collider 被禁用 → `OnTriggerExit2D` 触发 → `PlayerWindReceiver.UnregisterWindZone()`
3. **预期结果：** 风力立即消失，玩家恢复完全自由的移动能力

---

## 边界条件

- **多个 Blower 重叠：** 当玩家同时处于多个风力区域时，`CalculateCompositeWind()` 取影响力最大的风区（距离最近/风力最强），不做叠加，避免不合理的高速
- **风力区域未挂载 WindZoneData：** `PlayerWindReceiver.OnTriggerEnter2D` 使用 `TryGetComponent<WindZoneData>` 安全获取，获取不到则忽略
- **风力衰减曲线异常：** `WindZoneData` 在 `Awake` 中验证曲线有效性，若未配置则自动创建默认线性衰减曲线
- **风源距离为 0（玩家紧贴 Blower）：** `minApproachDistance` 确保玩家不能完全站到风源正中心，地面归零；空中则直接以最大风力吹飞
- **Blower 翻转朝向：** `WindZoneData.windDirection` 应从 Blower 的 `transform.right`（或由 `WindZoneData` 自身的 `transform.right`）自动推导，支持 Blower 左右翻转
- **状态切换时的速度继承：** 风力速度在状态切换（如 Jump → Fall、Fall → Idle）时依赖同一 `PlayerWindReceiver` 实例，不会因状态切换而丢失
- **性能：** `PlayerWindReceiver` 在 Update 中每帧仅重新计算一次风速（缓存），多个状态在同一帧内多次访问只读缓存值

---

## 注意事项

- **不抛弃 AreaEffector2D：** 现有 Blower 上的 AreaEffector2D 可保留作为视觉/物理辅助（如影响轻物体、粒子方向），但玩家交互改为代码层控制
- **SetVelocity 方法不动：** `Entity.SetVelocity` 方法本身保持不变，仅在调用方（各状态的 Update）传入已计算好风力影响的最终速度值
- **状态机结构不动：** 不新增状态，不修改 StateMachine 核心逻辑。风力响应逻辑通过 `PlayerWindReceiver` 组件化注入，各状态仅从 Player 读取 `externalWindVelocity` 并自行决定如何使用
- **GlideState 扩展预留：** 滑翔状态实现时可读取 `externalWindVelocity` 的 y 分量（若有垂直风力配置）和 x 分量，实现"迎风爬升"或"顺风加速滑翔"的机制。`WindZoneData` 的风力方向目前为纯水平，后续可扩展为支持角度
- **编辑器可视化：** `WindZoneData.OnDrawGizmos` 需在 Scene 视图中绘制风力范围圆环、方向箭头和衰减区域颜色渐变，方便关卡设计师调试
- **触发器层级：** Blower 的 Trigger Collider（用于检测玩家进入/离开）需挂在与 `WindZoneData` 同一 GameObject 上，使用独立于视觉碰撞体的 Trigger
- **与现有物理的兼容：** 风力计算在 `MoveState` 和 `AirState` 的 Update 中执行，与 `SetVelocity` 保持同频，不干扰 Unity 的 Rigidbody2D 重力（y 分量保持 `rb.linearVelocity.y`）

---

## 参考代码

### 现有关键文件路径

| 文件 | 路径 | 说明 |
|------|------|------|
| Blower.cs | `Assets/Resources/Scripts/InteractiveObjects/Electrics/Blower.cs` | 当前 Blower 实现，挂载 AreaEffector2D |
| Player.cs | `Assets/Resources/Scripts/Player/Player.cs` | 玩家主控制器，初始化所有状态 |
| Player_MoveState.cs | `Assets/Resources/Scripts/Player/PlayerStates/Player_MoveState.cs` | 地面移动状态 |
| Player_AirState.cs | `Assets/Resources/Scripts/Player/PlayerStates/Player_AirState.cs` | 空中状态基类 |
| Player_GlideState.cs | `Assets/Resources/Scripts/Player/PlayerStates/Player_GlideState.cs` | 滑翔状态（空壳，待实现） |
| Entity.cs | `Assets/Resources/Scripts/Entity/Entity.cs` | 实体基类，含 SetVelocity |
| EntityState.cs | `Assets/Resources/Scripts/StateMachine/EntityState.cs` | 状态基类 |
| StateMachine.cs | `Assets/Resources/Scripts/StateMachine/StateMachine.cs` | 状态机核心 |
| EntityEle.cs | `Assets/Resources/Scripts/Entity/EntityEle.cs` | 电器基类，TurnOn/TurnOff 虚方法 |
| PlayerInputSet.cs | `Assets/Resources/Scripts/Player/PlayerStates/PlayerInputSet.cs` | 输入系统自动生成代码 |

### 关键代码片段（当前实现）

**MoveState 当前设置速度的方式：**
```csharp
// Player_MoveState.Update()
player.SetVelocity(player.moveInput.x * player.MoveSpd, rb.linearVelocity.y);
```

**AirState 当前设置速度的方式：**
```csharp
// Player_AirState.Update()
player.SetVelocity(player.moveInput.x * player.MoveSpd * player.InAirMoveMultiplier, rb.linearVelocity.y);
```

**SetVelocity 定义（Entity.cs:83-91）：**
```csharp
public virtual void SetVelocity(float xVelocity, float yVelocity)
{
    if(isKnocked) return;
    rb.linearVelocity = new Vector2(xVelocity, yVelocity);
    HandleFlip(xVelocity);
}
```

---

## 验收标准

1. **地面逆风减速：** 玩家在 Blower 正面方向，距离越近移动速度越慢，在 minApproachDistance 以内完全无法朝 Blower 方向前进
2. **地面顺风加速：** 玩家背对 Blower 离开时移动速度明显快于正常速度
3. **空中吹飞：** 玩家在 Blower 风力范围内跳起后，被风吹离，方向键无法有效抵抗
4. **范围外无影响：** 玩家在 maxRadius 之外移动完全不受影响
5. **关闭后消失：** Blower 断电后风力效果立即停止
6. **参数可调：** 在 Inspector 中调整 `maxWindForce`、`windForceCurve`、`minApproachDistance` 等参数能直观改变风力体感
7. **多风区不叠加异常：** 同时处于多个 Blower 风力范围时速度不会异常放大
8. **状态切换不丢失：** 在风力区域内进行跳跃、落地等状态切换时风力效果持续生效
9. **架构完整：** 不修改 `StateMachine.cs` 和 `EntityState.cs`，不新增状态类，风力逻辑通过组件注入
10. **滑翔预留：** `PlayerWindReceiver.GetExternalWindVelocity()` 接口可直接被未来的 GlideState 调用

---

## 关键设计决策与修复记录

### 风力区域：从圆形半径 → Collider2D 边界
- **旧设计**：WindZoneData 使用 `maxRadius` 定义圆形风力范围，与 Trigger Collider 的实际形状不匹配，导致"看不见的力场"和范围内外边界模糊
- **新设计**：风力区域完全由 Trigger Collider（BoxCollider2D）定义，`IsInWindZone()` 使用 `collider.OverlapPoint()`，与玩家可见的碰撞体完全一致
- **衰减计算**：沿风向从 collider 的上风边缘（归一化 0，风力最大）到下风边缘（归一化 1，风力最小）线性采样

### 风力方向：修复 scale 翻转物体的 auto-direction
- **问题**：`transform.right` 仅受旋转影响。Blower 面朝左时使用 `scale.x = -1` 翻转，`transform.right` 仍返回 `(1,0)`
- **修复**：`WindDirection = transform.right * Mathf.Sign(lossyScale.x)`，正确推导包含缩放翻转的视觉朝向
- **手动覆盖**：`overrideDirection` 仅在特殊需求时勾选，默认不勾选，自动检测

### AreaEffector2D 角色：从主力 → 可选辅助
- **问题**：AreaEffector2D 的 `forceAngle` 可能与 WindZoneData 方向不一致，两套力对抗导致混乱
- **修复**：`Blower.Awake()` 中调用 `SyncAreaEffectorDirection()` 自动将 AreaEffector2D 方向同步为 WindZoneData 方向
- **定位**：AreaEffector2D 仅影响非玩家的轻物理物体（碎片等），玩家交互完全由 WindZoneData + PlayerWindReceiver 代码层控制

### 防闪动三层机制
1. **HandleFlip 死区**（`Entity.flipDeadZone = 0.05f`）：全局速度振荡保护
2. **空中对抗朝向锁定**（`AirState`）：风力与输入反向时，朝向固定为输入方向
3. **地面卡停朝向保持**（`MoveState`）：速度为 0 时手动按输入翻转

### Gizmos 可视化
- 白色虚线框 = Trigger Collider 边界
- 红→蓝渐变条 = 风力强度衰减预览
- 品红色大箭头 + 流动小箭头 = 风力方向
- 红色小球 = 风源位置（上风边缘）
- 橙色线 = 上风边缘，蓝色线 = 下风边缘
