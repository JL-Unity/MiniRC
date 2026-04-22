# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概况

Unity **6000.3.5f2**（Unity 6）2D 俯视角遥控车计时赛。URP + 2D 系列包（SpriteShape 画赛道、Tilemap、新 Input System）。场景：`Assets/Scenes/StartScene.unity`（菜单）与 `GameScene.unity`（比赛）。**没有 CLI 构建/测试入口**——请在 Unity 编辑器里打开；`com.unity.test-framework` 虽在依赖里，但工程内没有 PlayMode/EditMode 测试。

## 协作规则（来自 `.cursor/rules/*.mdc`，均为 `alwaysApply: true`）

- **先讨论再改代码。** 第一轮不要直接改文件。先用中文说明：理解的目标、可选方案、推荐做法、风险或需要用户拍板的点；等用户明确同意（「按方案 A」「可以改」「帮我实现」）再动手。例外：用户说「直接改」「马上实现」。
- **与框架冲突时先对齐，不硬塞实现。** 如果需求会破坏下文的分层（例如在玩法脚本里长期持有 `Text` 引用、UI 直接改 `Time.timeScale`），**先提出冲突**并建议补/改框架，再让用户选「hack 还是重构基础层」。
- **只给复杂、非显而易见的代码加注释。** 中文为主，解释**为什么**——物理单位、FixedUpdate/Update 时序、状态机意图、临时方案。简单赋值不要重复叙述。
- **遵循 `.cursor/skills/framework-aligned-unity/SKILL.md`** 的分层：规则在 GameMode、呈现在 Panel、跨模块解耦用 EventCenter；不要在玩法脚本里堆 `SerializeField Text`。

## 架构——需要跨多个文件阅读才能看清的要点

代码做了明确的分层，这里说清楚几个关键关系。

### 全局服务与启动（`Assets/Scripts/Base/`）

- `GameManager`（MonoBehaviour 单例，`DontDestroyOnLoad`）是入口。`Init()` 里按顺序初始化纯 C# 单例：`PoolManager`、`UIManager`（注入 `uiCanvasRoot`）、`TimerManager`、`SkillManager`。它还负责在菜单与 Race 场景之间传递**本局意图**：`PendingLevelId`、`PendingCarIndex`（菜单写入、Race 场景内消费）。
- `BaseManager<T>` 是**纯 C# 单例基类**（不是 MonoBehaviour）。`UIManager`、`EventCenter`、`PoolManager`、`TimerManager`、`SkillManager` 都继承它，通过 `GetInstance()` 取得。
- `EventCenter`——按消息类型分发的强类型事件总线。`Subscribe<T>(handler, clearOnSceneChange)` 区分「随场景清理」与「常驻」两套订阅；`Clear()` 只清前者。消息载荷定义在 `Base/Events/GameEventMessages.cs`，统一为 `readonly struct`，无参信号用 `default(T)` 发布。
- `PoolManager`——通过 `Resources.Load` 加载预制体，**任何要被池化、或当 Panel 使用的预制体必须放在某个 `Resources` 目录下**。`GetUIObject(name, parent)` 是 `UIManager` 使用的入口。
- `UIManager`——纯 C# 的面板栈。`PushPanel(name)` → `PoolManager.GetUIObject` → 原栈顶 `OnPause`、新面板 `OnEnter`；`PopPanel` 把实例还回池并恢复下一个。Canvas 根由 MonoBehaviour 注入：StartScene 里是 `GameManager`，Race 场景里是 `RcRaceUiBootstrap`。**所有面板必须是带 `BasePanel` 的预制体，并位于 `Resources/` 下**，名字就是 `PushPanel` 传入的字符串。
- `SceneStateController` + `SceneStateAsset`（带 `sceneName` 的 ScriptableObject）——异步切场景 + 假进度条。状态模式保留主要是为了「场景退出时存档」这类钩子，玩法本身在 `GameMode` 而非 State 里。

### 玩法层（`Assets/Scripts/GamePlay/` + `Base/GamePlay/GameMode.cs`）

- `GameMode`（抽象 MonoBehaviour）是每个场景的规则所有者。生命周期：`Awake → OnAwake`（订阅 `PlayerDeadMessage`）→ `Start` 自动 `GameManager.Instance.RegistGameMode(this)` → `OnStart`。子类需实现 `GameStart / GameEnd / CheckGameReady`。暂停集中在这里：`StopGame` 设 `isGameStop`、`Time.timeScale = 0`、发布 `GameStopMessage`。**UI 不要直接改 `Time.timeScale`——调用 GameMode 上的公开方法。**
- `RcCarRaceGameMode` 是目前唯一的具体 Mode。`OnStart` 里：
  1. 读 `GameManager.Instance.PendingLevelId` / `PendingCarIndex`；
  2. 从 `RcTrackCatalog`（ScriptableObject）里按 `levelId` 找关卡预制体，在 `levelAnchor` 下实例化。关卡根上必须挂 `RcRaceLevelRoot`，它暴露 `CarSpawn`、可选 `FinishTrigger`，以及 `GetFinishLinesInLevel()`；
  3. 从 `RcCarRoster`（ScriptableObject）取车辆预制体，在 spawn 处实例化，再调用 `raceSession.BindPlayerCar(rb, ctrl, inp, uiJoystick)`，并绑定所有终点线。
  - 同时负责 暂停/继续/重开/退出，以及**按赛道记录最佳总成绩**到 `PlayerPrefs`，键名 `MiniRC_RcRace_BestTotal_{trackId}`。
- `RcCarRaceSession2D`（虽然放在 `Assets/Scripts/UI/` 下，但语义上是玩法数据）维护三圈计时状态机 `WaitingFirstInput → Racing → Finished`：检测首次输入才开表、逐圈计时、把显示内容推给 HUD/结果面板。**车辆是运行时绑定的，不要在 Session 上拖车引用。**
- `RcCarController2D` 是 2D 俯视车辆物理（车身坐标系下横向指数衰减、街机式角速度）。调手感前先看文件顶部注释，所有字段都标了单位与意图。
- `RcCarInputSystemPlayer` 把新 Input System 接到 Controller；`RcCarFinishLine2D` 是终点触发器，回调 Session 记一圈。

### UI 层（`Assets/Scripts/UI/`）

菜单流程：`RcLevelSelectPanel.OnLevelChosen` → `GameManager.SetPendingLevelId` → `UIManager.PushPanel("RcCarSelectPanel")` → `RcCarSelectPanel` 确认 → `SetPendingCarIndex` + `SceneManager.LoadScene(raceSceneAsset.sceneName)`。暂停菜单 `RcCarRacePauseMenu2D` 在 `RcCarRaceGameMode.OnStart` 里通过 `Bind(this)` 接上——它只调 GameMode 公开方法，不直接动 `Time.timeScale` 或 Session。

## 添加内容——常见做法

- **加关卡：** 做关卡根预制体，挂 `RcRaceLevelRoot`（设置 `CarSpawn`、可选 `FinishTrigger` 与若干 `RcCarFinishLine2D` 子物体）；在 `RcCarRaceGameMode` 引用的 `RcTrackCatalog` 资产里加一条 `TrackEntry { levelId, displayName, levelPrefab }`；在 `RcLevelSelectPanel.levelButtons` 上按**与 `catalog.tracks` 相同顺序**挂按钮。
- **加车：** 做一份带 `Rigidbody2D` + `RcCarController2D` + `RcCarInputSystemPlayer` 的车预制体，填 `RcCarDefinition`；加入 `RcCarRoster.cars`；`RcCarSelectPanel.rows` 相应扩一行。
- **加面板：** 继承 `BasePanel`（实现 `OnEnter/OnPause/OnResume/OnExit`），预制体放到某个 `Resources/` 目录下，用资源名 `PushPanel`。**不要手动 `Instantiate` 面板。**
- **加跨模块信号：** 在 `GameEventMessages.cs` 里加 `readonly struct XxxMessage`，`EventCenter.GetInstance().Publish(new XxxMessage(...))` 发布，`Subscribe<XxxMessage>` 订阅。默认随场景清理；真正全局的常驻订阅传 `clearOnSceneChange: false`。

## 日志

统一用 `LogClass.LogGame(GameLogCategory.X, msg)`，分类在 `Base/Global.cs` 里是字符串常量（`PlayerState`、`UIManger`、`SceneStateController`、`System`、`RcCar`）。`LogGame` / `LogWarning` 只在 `UNITY_EDITOR` 下输出；`LogImport` / `LogError` 始终打印。
