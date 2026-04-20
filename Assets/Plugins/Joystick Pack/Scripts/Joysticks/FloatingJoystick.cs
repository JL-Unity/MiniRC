using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        // 先初始化相机，确保 ScreenPointToAnchoredPosition 能正确计算位置
        InitializeCamera();
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }

    public override void Reset()
    {
        base.Reset();
        // 隐藏背景，与 OnPointerUp 行为一致
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }
}