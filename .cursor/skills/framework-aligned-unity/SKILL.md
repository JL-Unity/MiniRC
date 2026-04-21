---
name: framework-aligned-unity
description: >-
  Aligns MiniRC gameplay and UI work with existing GameMode, GameManager,
  UIManager, and BasePanel layers. Use when adding features, drafting or
  refining requirements, implementing HUD or result screens, pause, persistence,
  or any RC car / gameplay flow; or when code would otherwise mix rules with
  direct UI manipulation in one MonoBehaviour.
---

# MiniRC：需求与实现要对齐框架分层

在写**需求**、拆任务或**写代码**前，先对照项目里已有结构，避免把规则、存档、流程和界面揉在一个脚本里（尤其是「Gameplay 脚本里到处 `SetUIText` / 拖引用改面板」）。

## 项目里已有的职责边界（必须优先复用）

| 层级 | 典型代码位置 | 负责什么 |
|------|----------------|----------|
| 玩法规则、胜负、暂停、与关卡绑定的存档键 | `GameMode` 子类（如 `RcCarRaceGameMode`）、`GameManager` 注册的当前 Mode | 局内流程、`StopGame`/`ResumeGame`、最佳成绩等**与玩法绑定**的持久化 |
| UI 栈、面板生命周期 | `UIManager`、`BasePanel`（`OnEnter`/`OnPause`/`OnResume`/`OnExit`） | 打开/关闭界面、面板入栈出栈；**展示与交互入口** |
| 玩法实体 / 回合数据 | 具体玩法组件（如 `RcCarRaceSession2D`） | 圈速、触发器回调、**领域数据**；通过事件或调用 GameMode **把「要显示什么」交给 UI 层**，而不是长期持有大量 `SerializeField` 去改 Text |
| 全局消息 | `EventCenter`、`*Message` 结构体 | 解耦「发生的事」与订阅方 |

基类参考：`Assets/Scripts/Base/GamePlay/GameMode.cs`、`Assets/Scripts/Base/UIControl/UIManager.cs`、`Assets/Scripts/Base/UIControl/BasePanel.cs`。

## 写需求时的自检清单（交给产品/自己拆故事前过一遍）

1. **状态与规则归谁？** 暂停是否只允许某状态、结算条件、存档是否新纪录——写在 **GameMode（或玩法+GameMode 明确 API）**，不要写成「在某个 Button 的 OnClick 里算完再改 Text」。
2. **哪些是「数据」、哪些是「呈现」？** 需求里区分：例如「总成绩秒数」是 Session/GameMode 算出的值；「结果页两行字」是某个 **Panel** 根据传入数据刷新。
3. **UI 怎么进场？** 新菜单是 `UIManager` + `BasePanel` 资源路径，还是与现有场景内物体一致——提前选定，避免又在随机 `MonoBehaviour` 上堆 `GameObject` 引用。
4. **会不会和现有生命周期打架？** 与 `Time.timeScale`、栈顶 `OnPause` 等是否一致；冲突时遵循仓库规则 `.cursor/rules/framework-conflict-discussion.mdc`：**先说明冲突再实现**。

## 实现时的硬性偏好

- **不要**：在玩法脚本里长篇累牍操作具体 HUD/结果 `Text`/`TMP`（除非项目尚未引入面板、且用户明确接受临时方案）。
- **要**：Gameplay 产出数据或调用 `GameMode`；需要显示时 **推数据到面板**（方法参数、事件、`EventCenter`），由 **Panel** 或薄层 Presenter 刷新 UI。
- **暂停 / 继续**：入口放在 **GameMode**（项目已有 `StopGame`/`ResumeGame` 模式）；UI 只调用公开方法，不直接改 `timeScale`。

## 与「讨论后再改代码」规则的关系

用户规则若要求**先给方案再动代码**：在方案里就应写明**数据流**（谁算、谁存、谁显示），并点名将用到的框架类，避免事后整文件都是 UI 引用。

---

若框架能力不足（例如缺少「赛事 HUD」基类），应按 `framework-conflict-discussion.mdc` 提出**补框架**或**接受临时耦合**的选择，而不是默认继续堆在单个组件上。
