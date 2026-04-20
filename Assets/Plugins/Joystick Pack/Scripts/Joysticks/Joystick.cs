using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    /// x方向的位移
    public float Horizontal => snapX ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x;
    /// y方向的位移
    public float Vertical => snapY ? SnapFloat(input.y, AxisOptions.Vertical) : input.y;
    /// 位移的方向
    public Vector2 Direction => new(Horizontal, Vertical);

    [NonSerialized] public Action OnDragStart;
    [NonSerialized] public Action<Vector2> OnDragging;
    [NonSerialized] public Action OnDragEnd;

    public float HandleRange
    {
        get => handleRange;
        set => handleRange = Mathf.Abs(value);
    }

    public float DeadZone
    {
        get => deadZone;
        set => deadZone = Mathf.Abs(value);
    }

    public AxisOptions AxisOptions { 
        get => axisOptions;
        set => axisOptions = value;
    }
    
    public bool SnapX { 
        get => snapX;
        set => snapX = value;
    }
    
    public bool SnapY { 
        get => snapY;
        set => snapY = value;
    }

    [SerializeField] private float handleRange = 1;
    [SerializeField] private float deadZone;
    [SerializeField] private AxisOptions axisOptions = AxisOptions.Both;
    [SerializeField] private bool snapX;
    [SerializeField] private bool snapY;

    [SerializeField] protected RectTransform background;
    [SerializeField] private RectTransform handle;

    [SerializeField] private bool changeColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color maxColor;

    private Image backgroundImage;

    private RectTransform baseRect;

    private Canvas canvas;
    private Camera cam;

    // 记录初始按下的位置
    private Vector2 initialPointerPosition;
    private bool inDeadZone = false;

    private Vector2 input = Vector2.zero;
    public float inputMagnitude => input.magnitude;

    protected virtual void Start()
    {
        HandleRange = handleRange;
        DeadZone = deadZone;
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        backgroundImage = background.GetComponent<Image>();
        if (canvas == null)
            Debug.LogError("The Joystick is not placed inside a canvas");

        Vector2 center = new Vector2(0.5f, 0.5f);
        background.pivot = center;
        handle.anchorMin = center;
        handle.anchorMax = center;
        handle.pivot = center;
        handle.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 初始化相机，用于在需要时确保相机已正确初始化
    /// 子类可以在调用 ScreenPointToAnchoredPosition 之前调用此方法
    /// </summary>
    protected void InitializeCamera()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        
        cam = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        // 记录初始按下位置
        initialPointerPosition = eventData.position;
        inDeadZone = true;

        OnDragStart?.Invoke();
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;

        Vector2 radius = background.sizeDelta / 2;

        // 计算输入值，区分固定摇杆和其他摇杆
        Vector2 referencePosition;
        if (!inDeadZone)
        {
            referencePosition = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        }
        else
        {
            // 非固定摇杆：以初始按下位置为原点
            referencePosition = initialPointerPosition;
        }

        input = (eventData.position - referencePosition) / (radius * canvas.scaleFactor);
        FormatInput();

        var magnitude = input.magnitude;
        if (changeColor)
        {
            if (magnitude >= HandleRange)
            {
                backgroundImage.color = maxColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }

        HandleInput(magnitude, input.normalized, radius, cam);
        handle.anchoredPosition = input * radius * handleRange;

        OnDragging?.Invoke(Direction);
    }

    protected virtual void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam_)
    {
        if (magnitude > deadZone)
        {
            inDeadZone = false;
            if (magnitude > 1)
                input = normalised;
        }
        else
            input = Vector2.zero;
    }

    private void FormatInput()
    {
        if (axisOptions == AxisOptions.Horizontal)
            input = new Vector2(input.x, 0f);
        else if (axisOptions == AxisOptions.Vertical)
            input = new Vector2(0f, input.y);
    }

    private float SnapFloat(float value, AxisOptions snapAxis)
    {
        if (value == 0)
            return value;

        if (axisOptions == AxisOptions.Both)
        {
            float angle = Vector2.Angle(input, Vector2.up);
            if (snapAxis == AxisOptions.Horizontal)
            {
                if (angle < 22.5f || angle > 157.5f)
                    return 0;
                else
                    return value > 0 ? 1 : -1;
            }
            else if (snapAxis == AxisOptions.Vertical)
            {
                if (angle > 67.5f && angle < 112.5f)
                    return 0;
                else
                    return value > 0 ? 1 : -1;
            }
            return value;
        }
        else
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
        }
        return 0;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        inDeadZone = false;
        OnDragEnd?.Invoke();
    }

    /// <summary>
    /// 强制重置摇杆状态
    /// 用于在特殊情况下（如角色死亡）强制清除摇杆的拖拽状态
    /// 不会触发 OnDragEnd 事件
    /// </summary>
    public virtual void Reset()
    {
        input = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        inDeadZone = false;
        initialPointerPosition = Vector2.zero;
        
        // 重置背景颜色
        if (changeColor && backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out var localPoint))
        {
            Vector2 sizeDelta = baseRect.sizeDelta;
            Vector2 pivotOffset = baseRect.pivot * sizeDelta;
            return localPoint - background.anchorMax * sizeDelta + pivotOffset;
        }
        return Vector2.zero;
    }
}

public enum AxisOptions { Both, Horizontal, Vertical }