using UnityEngine;

/// <summary>
/// 菜单与 Race 共用的可选车辆列表（通常配置 3 辆）。
/// </summary>
[CreateAssetMenu(menuName = "MiniRC/RC Car Roster", fileName = "RcCarRoster")]
public class RcCarRoster : ScriptableObject
{
    public RcCarDefinition[] cars = new RcCarDefinition[3];

    public RcCarDefinition GetCar(int index)
    {
        if (cars == null || cars.Length == 0)
        {
            return null;
        }
        index = Mathf.Clamp(index, 0, cars.Length - 1);
        return cars[index];
    }

    public int Count => cars != null ? cars.Length : 0;
}
