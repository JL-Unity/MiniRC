using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 菜单场景：展示三辆车属性条，确认后写入 GameManager 并加载 Race 场景。
/// UI 控件可在 Inspector 绑定；亦可仅填数据由代码驱动。
/// </summary>
public class RcCarSelectPanel : BasePanel
{
    [SerializeField] RcCarRoster roster;
    [SerializeField] string raceLevelId = "default";
    [Tooltip("sceneName 指向 Race 场景的 SceneStateAsset")]
    [SerializeField] SceneStateAsset raceSceneAsset;

    [Header("可选：三辆车按钮，点击切换选中")]
    [SerializeField] Button[] carSelectButtons;

    [Header("可选：每辆一行（与 roster 顺序一致）")]
    [SerializeField] StatRowUi[] rows;

    [SerializeField] Button confirmButton;

    [System.Serializable]
    public class StatRowUi
    {
        public Image speedFill;
        public Image driftFill;
        public Image accelFill;
        public Text speedText;
        public Text driftText;
        public Text accelText;
    }

    int _selectedIndex;

    void Awake()
    {
        if (carSelectButtons != null)
        {
            for (int i = 0; i < carSelectButtons.Length; i++)
            {
                int idx = i;
                if (carSelectButtons[i] != null)
                    carSelectButtons[i].onClick.AddListener(() => SelectCar(idx));
            }
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmStartRace);
    }

    void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmStartRace);
    }

    void SelectCar(int index)
    {
        if (roster == null)
            return;
        _selectedIndex = Mathf.Clamp(index, 0, roster.Count - 1);
        RefreshStatDisplays();
    }

    public override void OnEnter()
    {
        RefreshStatDisplays();
    }

    public override void OnPause() { }

    public override void OnResume() { }

    public override void OnExit() { }

    void RefreshStatDisplays()
    {
        if (roster == null || roster.cars == null || rows == null)
            return;

        for (int i = 0; i < rows.Length && i < roster.cars.Length; i++)
        {
            var def = roster.cars[i];
            var row = rows[i];
            if (def == null || row == null)
                continue;

            SetBar(row.speedFill, def.speedPercent);
            SetBar(row.driftFill, def.driftPercent);
            SetBar(row.accelFill, def.accelPercent);

            SetInt(row.speedText, def.speedPercent);
            SetInt(row.driftText, def.driftPercent);
            SetInt(row.accelText, def.accelPercent);
        }
    }

    static void SetBar(Image img, int pct)
    {
        if (img == null)
            return;
        img.type = Image.Type.Filled;
        img.fillAmount = Mathf.Clamp01(pct / 100f);
    }

    static void SetInt(Text t, int v)
    {
        if (t == null)
            return;
        t.text = v.ToString();
    }

    /// <summary>写入 GameManager 并异步加载 Race 场景。</summary>
    void OnConfirmStartRace()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPendingRaceIntent(raceLevelId, _selectedIndex);

        if (SceneStateController.Instance != null && raceSceneAsset != null)
            SceneStateController.Instance.StartLoadingScene(raceSceneAsset);
        else if (raceSceneAsset != null && !string.IsNullOrEmpty(raceSceneAsset.sceneName))
            SceneManager.LoadScene(raceSceneAsset.sceneName);
        else
            Debug.LogWarning("RcCarSelectPanel: 请配置 SceneStateController（DontDestroy）与 raceSceneAsset，或直接填 raceSceneAsset 的场景名。");
    }
}
