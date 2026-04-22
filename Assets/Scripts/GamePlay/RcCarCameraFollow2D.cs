using UnityEngine;

/// <summary>
/// 2D 正交相机跟随目标（仅平移 XY，Z 保持为相机偏移）。在 <see cref="LateUpdate"/> 中 Lerp，避免与小车物理不同步。
/// </summary>
[DisallowMultipleComponent]
public class RcCarCameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("相对目标的偏移（通常 Z 为负，例如 (0,0,-10)）")]
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("越大跟得越紧；与 Time.deltaTime 相乘后作为 Lerp 的 t")]
    [SerializeField] float positionLerp = 8f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        float t = Mathf.Clamp01(positionLerp * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
