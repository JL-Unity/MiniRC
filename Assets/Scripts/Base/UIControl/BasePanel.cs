using UnityEngine;

/// <summary>
/// 需要被 UI 管理器管理的面板基类：OnEnter / OnPause / OnResume / OnExit 时机见各类注释。
/// </summary>
public abstract class BasePanel : PoolObject
{
    public string panelName = "";

    public void SetPanelName(string panelName)
    {
        this.panelName = panelName;
    }

    public abstract void OnEnter();

    public abstract void OnPause();

    public abstract void OnResume();

    public abstract void OnExit();
}
