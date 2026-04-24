using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 栈 + 对象池（纯 C# 单例，不挂 Mono）。
/// Canvas 引用须由场景内的 <see cref="UiCanvasBootstrap"/> 在 Start 时通过 <see cref="SetCanvasRoot"/> 注入；
/// 未注入时 <see cref="PushPanel"/> 会打 warning 并返回 null，不做任何 Find 兜底。
/// </summary>
public class UIManager : BaseManager<UIManager>
{
    private Transform _canvasRoot;

    private Stack<BasePanel> _panelStack;

    public GameObject currentPanel { get; private set; }

    /// <summary>场景内 <see cref="UiCanvasBootstrap"/> 在 Start 时调用；切场景后旧 Canvas 被销毁，此处会被新场景 Bootstrap 覆盖。</summary>
    public void SetCanvasRoot(Transform canvasRoot)
    {
        _canvasRoot = canvasRoot;
    }

    private Transform ResolveCanvasRoot()
    {
        // 场景切换后旧 Canvas 会被销毁，Unity 的 == null 能识别 fake-null；此时返回 null 让调用方打 warning
        if (_canvasRoot == null)
        {
            _canvasRoot = null;
            return null;
        }
        return _canvasRoot;
    }

    public Transform CanvasTransform => ResolveCanvasRoot();

    public override void Init()
    {
        base.Init();
        EnsureStack();
    }

    private void EnsureStack()
    {
        if (_panelStack == null)
        {
            _panelStack = new Stack<BasePanel>();
        }
    }

    public GameObject GetUI(string panelName)
    {
        BasePanel panel = TryLoadPanel(panelName, ResolveCanvasRoot(), null);
        return panel != null ? panel.gameObject : null;
    }

    public GameObject GetUI(string panelName, Transform parentTransform, Vector3 localPosition)
    {
        BasePanel panel = TryLoadPanel(panelName, parentTransform, localPosition);
        return panel != null ? panel.gameObject : null;
    }

    private BasePanel TryLoadPanel(string panelName, Transform parent, Vector3? localPosition)
    {
        if (parent == null)
        {
            LogClass.LogWarning(GameLogCategory.UIManager, "TryLoadPanel: parent is null, path: " + panelName);
            return null;
        }

        GameObject go = PoolManager.GetInstance().GetUIObject(panelName, parent);
        if (go == null)
        {
            LogClass.LogWarning(GameLogCategory.UIManager, "GetUI failed (null instance), path: " + panelName);
            return null;
        }

        go.transform.SetParent(parent, false);

        if (localPosition.HasValue)
        {
            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition3D = localPosition.Value;
            }
            else
            {
                go.transform.localPosition = localPosition.Value;
            }
        }

        BasePanel panel = go.GetComponent<BasePanel>();
        if (panel == null)
        {
            LogClass.LogError(GameLogCategory.UIManager, "Missing BasePanel on prefab: " + panelName);
            return null;
        }

        panel.SetPanelName(panelName);
        return panel;
    }

    public GameObject PushPanel(string panelName)
    {
        EnsureStack();

        BasePanel panel = TryLoadPanel(panelName, ResolveCanvasRoot(), null);
        if (panel == null)
        {
            LogClass.LogWarning(GameLogCategory.UIManager, "PushPanel failed, path: " + panelName);
            return null;
        }

        DiscardDestroyedTop();
        if (_panelStack.Count > 0)
        {
            BasePanel topPanel = _panelStack.Peek();
            topPanel.OnPause();
            LogClass.LogGame(GameLogCategory.UIManager, "panelStack count > 0, pause top panel" + topPanel.name);
        }

        _panelStack.Push(panel);
        currentPanel = panel.gameObject;
        panel.OnEnter();
        return panel.gameObject;
    }

    public void PopPanel()
    {
        EnsureStack();

        if (_panelStack.Count <= 0)
        {
            return;
        }

        BasePanel topPanel = _panelStack.Pop();
        // 跨场景后 topPanel 可能已随旧 Canvas 销毁变 fake-null，直接丢弃不走 OnExit/PushObj
        if (topPanel != null)
        {
            topPanel.OnExit();
            PoolManager.GetInstance().PushObj(topPanel.panelName, topPanel.gameObject);
            LogClass.LogGame(GameLogCategory.UIManager, "topPanel Exit" + topPanel.name);
        }

        DiscardDestroyedTop();
        if (_panelStack.Count > 0)
        {
            BasePanel panel = _panelStack.Peek();
            currentPanel = panel.gameObject;
            panel.OnResume();
        }
        else
        {
            currentPanel = null;
        }
    }

    public override void Clear()
    {
        EnsureStack();

        while (_panelStack.Count > 0)
        {
            PopPanel();
        }
        currentPanel = null;
    }

    /// <summary>栈顶若是已销毁对象（fake-null）直接 Pop 丢弃，避免访问 MissingReference；连续丢到真实栈顶。</summary>
    private void DiscardDestroyedTop()
    {
        while (_panelStack.Count > 0 && _panelStack.Peek() == null)
        {
            _panelStack.Pop();
        }
    }
}
