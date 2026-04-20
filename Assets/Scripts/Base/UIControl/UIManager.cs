using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 栈 + 对象池（纯 C# 单例，不挂 Mono）。
/// Canvas 引用须由 <see cref="SetCanvasRoot"/> 从 <b>MonoBehaviour</b>（如 GameManager）在 Init 前注入；
/// 未注入时首次使用 <see cref="CanvasTransform"/> 会 <c>Find("Canvas")</c> 并缓存。
/// </summary>
public class UIManager : BaseManager<UIManager>
{
    private Transform _canvasRoot;

    private Stack<BasePanel> _panelStack;

    public GameObject currentPanel { get; private set; }

    /// <summary>由 GameManager 等 Mono 在 Awake/Init 里赋值；传 null 表示走 Find。</summary>
    public void SetCanvasRoot(Transform canvasRoot)
    {
        _canvasRoot = canvasRoot;
    }

    private Transform ResolveCanvasRoot()
    {
        if (_canvasRoot != null)
        {
            return _canvasRoot;
        }

        GameObject go = GameObject.Find("Canvas");
        if (go != null)
        {
            _canvasRoot = go.transform;
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
        topPanel.OnExit();
        PoolManager.GetInstance().PushObj(topPanel.panelName, topPanel.gameObject);

        LogClass.LogGame(GameLogCategory.UIManager, "topPanel Exit" + topPanel.name);

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
    }
}
