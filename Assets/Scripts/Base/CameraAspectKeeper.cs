using UnityEngine;

/// <summary>
/// 把相机的渲染区域（<see cref="Camera.rect"/>）锁定为指定宽高比，
/// 窗口与目标比例不一致时，自动产生左右（pillarbox）或上下（letterbox）黑边。
/// 配合 Camera.clearFlags = SolidColor + 黑色 + UI Canvas 设为 Screen Space - Camera 使用，
/// 可以让打包后无论窗口怎么拉伸，画面与 UI 始终维持目标比例。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraAspectKeeper : MonoBehaviour
{
    [Tooltip("目标宽高比的分子，例如 16:9 填 16")]
    [SerializeField] float targetWidth = 16f;

    [Tooltip("目标宽高比的分母，例如 16:9 填 9")]
    [SerializeField] float targetHeight = 9f;

    Camera _cam;
    int _lastScreenWidth;
    int _lastScreenHeight;
    float _lastTargetAspect;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        // 强制下一次 Update 重算（清掉缓存）
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        _lastTargetAspect = -1f;
        ApplyRectIfChanged();
    }

    void Update()
    {
        ApplyRectIfChanged();
    }

    void ApplyRectIfChanged()
    {
        if (_cam == null)
            return;

        int sw = Screen.width;
        int sh = Screen.height;
        float targetAspect = (targetHeight <= 0f) ? 1f : targetWidth / targetHeight;

        // 仅在窗口尺寸或目标比例变化时才改 rect，避免每帧赋值触发不必要的相机刷新
        if (sw == _lastScreenWidth && sh == _lastScreenHeight && Mathf.Approximately(targetAspect, _lastTargetAspect))
            return;

        _lastScreenWidth = sw;
        _lastScreenHeight = sh;
        _lastTargetAspect = targetAspect;

        if (sh <= 0 || sw <= 0)
            return;

        float windowAspect = (float)sw / sh;
        float scale = windowAspect / targetAspect;

        if (scale >= 1f)
        {
            // 窗口比目标更宽：左右各留黑边（pillarbox）
            float w = 1f / scale;
            _cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        else
        {
            // 窗口比目标更高：上下各留黑边（letterbox）
            _cam.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (targetWidth <= 0f) targetWidth = 16f;
        if (targetHeight <= 0f) targetHeight = 9f;
        // 让 Inspector 改完立刻生效
        if (isActiveAndEnabled)
        {
            _lastTargetAspect = -1f;
            ApplyRectIfChanged();
        }
    }
#endif
}
