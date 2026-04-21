using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 左右后轮位置用 <see cref="LineRenderer"/> 画「线段拖尾」式胎印：内部点时间顺序为「老→新」，
/// 写入 LineRenderer 时反转为「新→老」，使 Gradient/Width 的 0～1 与「生命周期」（新→旧）一致。
/// 挂在有 <see cref="Rigidbody2D"/> 的车体上（或子物体，会自动 InParent 找刚体）。
/// </summary>
[DisallowMultipleComponent]
public class RcCarTireTracks2D : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("用于判断是否够快才打点；可空则 GetComponent/InParent")]
    [SerializeField] Rigidbody2D rb;

    [Header("Placement · 相对本物体本地（车头常为上 +y，后轮常为负 y）")]
    [SerializeField] float halfTrackSpacing = 0.07f;
    [SerializeField] float rearLocalOffset = -0.06f;

    [Header("拖尾外观")]
    [Tooltip("胎线最大宽度（LineRenderer widthMultiplier）")]
    [SerializeField] float trackWidth = 0.1f;
    [Tooltip("采样：与上一点至少隔开这么远（世界单位）才加点，避免点过密")]
    [SerializeField] float minVertexDistance = 0.04f;
    [Tooltip("最老的一段点超过该时间（秒）会被移除，拖尾长度由此控制")]
    [SerializeField] float trackLifetime = 1.75f;
    [Tooltip("沿「生命周期」：0=最新压痕（刚印下），1=最老即将消失；右侧 alpha=0 即淡出端")]
    [SerializeField] Gradient trackColorOverLifetime;
    [Tooltip("沿「生命周期」：0=最新一端，1=最老一端（与粒子 Color/Width over Lifetime 一致）")]
    [SerializeField] AnimationCurve widthOverNormalizedLength = null;

    [Header("Emit")]
    [SerializeField] float minSpeedToEmit = 0.12f;

    [Header("Sorting · 2D")]
    [SerializeField] int sortingOrder = -8;
    [SerializeField] string sortingLayerName = "";

    [Header("Material")]
    [Tooltip("需支持顶点色；留空则用 Sprites/Default")]
    [SerializeField] Material trackMaterial;

    struct TimedPoint
    {
        public Vector3 World;
        public float Time;
    }

    readonly List<TimedPoint> _leftPts = new List<TimedPoint>(256);
    readonly List<TimedPoint> _rightPts = new List<TimedPoint>(256);

    Transform _anchorL;
    Transform _anchorR;
    LineRenderer _lineL;
    LineRenderer _lineR;
    Vector3 _lastSampleL;
    Vector3 _lastSampleR;
    bool _hasLastL;
    bool _hasLastR;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
    }

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        if (trackColorOverLifetime.colorKeys == null || trackColorOverLifetime.colorKeys.Length == 0)
            trackColorOverLifetime = BuildDefaultGradient();

        if (widthOverNormalizedLength == null || widthOverNormalizedLength.keys.Length == 0)
            widthOverNormalizedLength = AnimationCurve.Constant(0f, 1f, 1f);

        _anchorL = new GameObject("TireTrackAnchor_L").transform;
        _anchorR = new GameObject("TireTrackAnchor_R").transform;
        _anchorL.SetParent(transform, false);
        _anchorR.SetParent(transform, false);
        _anchorL.localPosition = new Vector3(-halfTrackSpacing, rearLocalOffset, 0f);
        _anchorR.localPosition = new Vector3(halfTrackSpacing, rearLocalOffset, 0f);

        _lineL = _anchorL.gameObject.AddComponent<LineRenderer>();
        _lineR = _anchorR.gameObject.AddComponent<LineRenderer>();
        SetupLine(_lineL);
        SetupLine(_lineR);
    }

    void LateUpdate()
    {
        if (_lineL == null || rb == null)
            return;

        float now = Time.time;
        float life = Mathf.Max(0.05f, trackLifetime);
        PruneOlderThan(now - life);
        if (_leftPts.Count == 0)
            _hasLastL = false;
        if (_rightPts.Count == 0)
            _hasLastR = false;

        bool emit = rb.linearVelocity.sqrMagnitude >= minSpeedToEmit * minSpeedToEmit;
        if (emit)
        {
            TryAddPoint(_leftPts, ref _lastSampleL, ref _hasLastL, _anchorL.position, now, minVertexDistance);
            TryAddPoint(_rightPts, ref _lastSampleR, ref _hasLastR, _anchorR.position, now, minVertexDistance);
        }

        PushToLineRenderer(_lineL, _leftPts);
        PushToLineRenderer(_lineR, _rightPts);
    }

    void OnValidate()
    {
        if (_anchorL != null)
            _anchorL.localPosition = new Vector3(-halfTrackSpacing, rearLocalOffset, 0f);
        if (_anchorR != null)
            _anchorR.localPosition = new Vector3(halfTrackSpacing, rearLocalOffset, 0f);
        if (_lineL != null)
        {
            _lineL.widthMultiplier = trackWidth;
            _lineL.colorGradient = trackColorOverLifetime;
            _lineL.widthCurve = widthOverNormalizedLength;
        }

        if (_lineR != null)
        {
            _lineR.widthMultiplier = trackWidth;
            _lineR.colorGradient = trackColorOverLifetime;
            _lineR.widthCurve = widthOverNormalizedLength;
        }
    }

    static void TryAddPoint(List<TimedPoint> list, ref Vector3 lastSample, ref bool hasLast, Vector3 world, float now, float minDist)
    {
        if (!hasLast)
        {
            list.Add(new TimedPoint { World = world, Time = now });
            lastSample = world;
            hasLast = true;
            return;
        }

        if ((world - lastSample).sqrMagnitude >= minDist * minDist)
        {
            list.Add(new TimedPoint { World = world, Time = now });
            lastSample = world;
        }
    }

    void PruneOlderThan(float minTime)
    {
        PruneList(_leftPts, minTime);
        PruneList(_rightPts, minTime);
    }

    static void PruneList(List<TimedPoint> list, float minTime)
    {
        while (list.Count > 0 && list[0].Time < minTime)
            list.RemoveAt(0);
    }

    void PushToLineRenderer(LineRenderer lr, List<TimedPoint> list)
    {
        int n = list.Count;
        if (n == 0)
        {
            lr.positionCount = 0;
            return;
        }

        // LineRenderer 的 colorGradient/widthCurve：0 = 第一个顶点，1 = 最后一个顶点。
        // 列表为时间顺序「最老 → 最新」，故反转后：首点=最新（生命周期 0）、末点=最老（生命周期 1）。
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
            lr.SetPosition(i, list[n - 1 - i].World);
    }

    static Gradient BuildDefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 0.7f),
                new GradientColorKey(Color.black, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.45f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }

    void SetupLine(LineRenderer lr)
    {
        lr.widthMultiplier = trackWidth;
        lr.widthCurve = widthOverNormalizedLength;
        lr.colorGradient = trackColorOverLifetime;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lr.maskInteraction = SpriteMaskInteraction.None;

        var mat = trackMaterial != null
            ? trackMaterial
            : CreateRuntimeTrackMaterial();
        lr.material = mat;
        lr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            int id = SortingLayer.NameToID(sortingLayerName);
            if (id != 0)
                lr.sortingLayerID = id;
        }
    }

    static Material CreateRuntimeTrackMaterial()
    {
        var s = Shader.Find("Sprites/Default");
        if (s == null)
            s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null)
            s = Shader.Find("Particles/Standard Unlit");
        var m = new Material(s);
        m.color = Color.white;
        return m;
    }
}
