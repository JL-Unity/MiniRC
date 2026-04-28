using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在「等级图标」Image 上的视图组件：面板传入 <see cref="RcRaceGrade"/>，
/// 由它去 <see cref="RcRaceGradeStyleLibrary"/> 取对应 Sprite 设到 Image。
/// 面板不直接持有 Sprite / Library 引用，便于未来扩展等级或换图。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class RcRaceGradeIconView : MonoBehaviour
{
    [SerializeField] RcRaceGradeStyleLibrary library;

    [Tooltip("等级为 None 时是否隐藏整个 GameObject；关闭则只清空 Image.sprite")]
    [SerializeField] bool hideWhenNone = true;

    Image _img;

    Image Img
    {
        get
        {
            if (_img == null)
            {
                _img = GetComponent<Image>();
            }
            return _img;
        }
    }

    public void SetGrade(RcRaceGrade grade)
    {
        Sprite s = library != null ? library.GetSprite(grade) : null;

        if (s == null)
        {
            // None 或库里没配该等级：按设置选择隐藏 GameObject 还是仅清空 sprite
            if (hideWhenNone)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
            }
            else
            {
                Img.sprite = null;
                Img.enabled = false;
            }
            return;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Img.enabled = true;
        Img.sprite = s;
    }
}
