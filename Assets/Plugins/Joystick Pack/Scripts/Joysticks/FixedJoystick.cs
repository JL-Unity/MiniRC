using UnityEngine;
using UnityEngine.EventSystems;

public class FixedJoystick : Joystick
{
    [SerializeField] private bool hideBackground;
    
    protected override void Start()
    {
        base.Start();
        if (hideBackground)
            background.gameObject.SetActive(false);
    }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (hideBackground)
            background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        if (hideBackground)
            background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
}