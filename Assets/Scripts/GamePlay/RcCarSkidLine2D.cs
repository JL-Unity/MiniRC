using UnityEngine;

// -----------------------------------------------------------------------------
// 单段胎痕线：由 RcCarSkidEmitter 从对象池取出，作为一个 LineRenderer 段
// · 取出后进入"活动期"：Emitter 持有引用、持续往 LineRenderer 追加点
// · Emitter 调 StartFade() 后进入"淡出期"：hold 秒保持 → fade 秒线性淡 alpha → 归还池
// · 使用 Time.time，timeScale=0（游戏暂停）时自动停在原地，与 GameMode 暂停语义一致
// · LineRenderer 必须在预制体上勾 Use World Space，这样段切出去留在原地、不会跟车动
// -----------------------------------------------------------------------------

[RequireComponent(typeof(LineRenderer))]
[DisallowMultipleComponent]
public class RcCarSkidLine2D : MonoBehaviour
{
    [Header("Lifetime · 淡出开始后的寿命")]
    [Tooltip("StartFade 后保持满 alpha 的秒数")]
    [SerializeField] float holdTime = 1.5f;
    [Tooltip("hold 之后线性淡到 alpha=0 所需秒数；到期归还对象池")]
    [SerializeField] float fadeTime = 1.0f;

    LineRenderer _lr;
    Color _baseStartColor;
    Color _baseEndColor;
    float _fadeStartAt;
    bool _fading;
    string _poolKey;

    /// <summary>给 Emitter 追加点用；不在"活动期"也允许拿到，调用方自己判断。</summary>
    public LineRenderer Line => _lr;

    /// <summary>当前是否在淡出（不再接受追加点）。</summary>
    public bool IsFading => _fading;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _baseStartColor = _lr.startColor;
        _baseEndColor = _lr.endColor;
    }

    /// <summary>Emitter 生成本段后调用，记录归还时的池 key（即 Resources 预制体名）。</summary>
    public void SetPoolKey(string key) => _poolKey = key;

    void OnEnable()
    {
        _fading = false;
        _fadeStartAt = 0f;
        if (_lr != null)
        {
            // 从池复用时清空上次的点与 alpha
            _lr.positionCount = 0;
            _lr.startColor = _baseStartColor;
            _lr.endColor = _baseEndColor;
        }
    }

    /// <summary>Emitter 在"漂停止"或"段满了切出"时调用，本段开始计时淡出。</summary>
    public void StartFade()
    {
        if (_fading)
        {
            return;
        }
        _fading = true;
        _fadeStartAt = Time.time;
    }

    void Update()
    {
        if (!_fading)
        {
            return;
        }

        float age = Time.time - _fadeStartAt;
        if (age <= holdTime)
        {
            return;
        }

        float t = (age - holdTime) / Mathf.Max(0.0001f, fadeTime);
        if (t >= 1f)
        {
            Recycle();
            return;
        }

        Color s = _baseStartColor;
        Color e = _baseEndColor;
        s.a = _baseStartColor.a * (1f - t);
        e.a = _baseEndColor.a * (1f - t);
        _lr.startColor = s;
        _lr.endColor = e;
    }

    void Recycle()
    {
        if (!string.IsNullOrEmpty(_poolKey))
        {
            PoolManager.GetInstance().PushObj(_poolKey, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
