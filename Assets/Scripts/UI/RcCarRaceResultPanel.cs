using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结果面板：由 <see cref="RcCarRaceGameMode"/> 在收到 <see cref="RaceFinishedMessage"/> 后 Push；
/// OnEnter 时从 <see cref="RcCarRaceGameMode.LastRaceResult"/> 读最新成绩填 UI。
/// </summary>
public class RcCarRaceResultPanel : BasePanel
{
    [SerializeField] Component currentLine;
    [SerializeField] Component bestLine;
    [Tooltip("破纪录时激活；非破纪录保持隐藏")]
    [SerializeField] GameObject newRecordIndicator;
    [SerializeField] Button playAgainButton;

    static Type _tmpUguiType;

    static Type TmpUguiType
    {
        get
        {
            if (_tmpUguiType == null)
            {
                _tmpUguiType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            }
            return _tmpUguiType;
        }
    }

    RcCarRaceGameMode Mode => GameManager.Instance?.GetGameMode() as RcCarRaceGameMode;

    public override void OnEnter()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        var mode = Mode;
        if (mode != null)
        {
            var r = mode.LastRaceResult;
            SetUIText(currentLine, "本次成绩： " + RcCarRaceSession2D.FormatTime(r.Total));
            SetUIText(bestLine, "最好成绩： " + RcCarRaceSession2D.FormatTime(r.BestShown));
            if (newRecordIndicator != null)
            {
                newRecordIndicator.SetActive(r.NewRecord);
            }
        }
    }

    public override void OnExit()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
        if (newRecordIndicator != null)
        {
            newRecordIndicator.SetActive(false);
        }
    }

    public override void OnPause() { }

    public override void OnResume() { }

    void OnPlayAgainClicked()
    {
        // 先关自己，再让 Mode 复位 Session（顺序无关紧要，但先 Pop 可以避免面板残留一帧）
        UIManager.GetInstance().PopPanel();
        Mode?.PlayAgainFromResult();
    }

    static void SetUIText(Component c, string value)
    {
        if (c == null)
        {
            return;
        }

        if (c is Text leg)
        {
            leg.text = value;
            return;
        }

        var go = c.gameObject;
        var ugui = go.GetComponent<Text>() ?? go.GetComponentInChildren<Text>(true);
        if (ugui != null)
        {
            ugui.text = value;
            return;
        }

        var tmpType = TmpUguiType;
        if (tmpType == null)
        {
            return;
        }

        var tmp = go.GetComponent(tmpType) ?? go.GetComponentInChildren(tmpType, true);
        if (tmp != null)
        {
            tmpType.GetProperty("text")?.SetValue(tmp, value);
        }
        else if (tmpType.IsInstanceOfType(c))
        {
            tmpType.GetProperty("text")?.SetValue(c, value);
        }
    }
}
