using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 绑定 <c>InputSystem_Actions</c> 的 <c>Player</c> 地图：Move、Sprint、Reverse（倒车）。
/// </summary>
[DisallowMultipleComponent]
public class RcCarInputSystemPlayer : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;

    InputActionMap _playerMap;
    InputAction _move;
    InputAction _sprint;
    InputAction _reverse;

    public InputAction MoveAction => _move;
    public InputAction SprintAction => _sprint;
    public InputAction ReverseAction => _reverse;

    [Header("Debug")]
    [Tooltip("Sprint 按下/松开各打一行")]
    [SerializeField] bool logSprintToConsole;
    [Tooltip("Reverse 按下/松开各打一行")]
    [SerializeField] bool logReverseToConsole;

    bool _loggedSprintPrev;
    bool _loggedReversePrev;

    void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogWarning($"{nameof(RcCarInputSystemPlayer)}: 请指定 Input Actions 资源。", this);
            return;
        }

        _playerMap = inputActions.FindActionMap("Player");
        if (_playerMap == null)
        {
            Debug.LogError($"{nameof(RcCarInputSystemPlayer)}: 未找到 Player 地图。", this);
            return;
        }

        _move = _playerMap.FindAction("Move");
        _sprint = _playerMap.FindAction("Sprint");
        _reverse = _playerMap.FindAction("Reverse");
        if (_move == null || _sprint == null)
            Debug.LogError($"{nameof(RcCarInputSystemPlayer)}: Player 需包含 Move、Sprint。", this);
        if (_reverse == null)
            Debug.LogWarning($"{nameof(RcCarInputSystemPlayer)}: 未找到 Reverse 动作，倒车不可用（请在 Input Actions 中添加）。", this);

        if (_sprint != null)
            _loggedSprintPrev = _sprint.IsPressed();
        if (_reverse != null)
            _loggedReversePrev = _reverse.IsPressed();
    }

    void Update()
    {
        if (logSprintToConsole && _sprint != null)
        {
            bool now = _sprint.IsPressed();
            if (now != _loggedSprintPrev)
            {
                Debug.Log($"[RcCarInput] Sprint={(now ? "ON" : "OFF")}", this);
                _loggedSprintPrev = now;
            }
        }

        if (logReverseToConsole && _reverse != null)
        {
            bool now = _reverse.IsPressed();
            if (now != _loggedReversePrev)
            {
                Debug.Log($"[RcCarInput] Reverse={(now ? "ON" : "OFF")}", this);
                _loggedReversePrev = now;
            }
        }
    }

    void OnEnable()
    {
        _playerMap?.Enable();
    }

    void OnDisable()
    {
        _playerMap?.Disable();
    }

    public Vector2 ReadMove()
    {
        if (_move == null)
            return Vector2.zero;
        Vector2 v = _move.ReadValue<Vector2>();
        if (v.sqrMagnitude > 1f)
            v = v.normalized;
        return v;
    }

    public bool ReadSprint()
    {
        return _sprint != null && _sprint.IsPressed();
    }

    public bool ReadReverse()
    {
        return _reverse != null && _reverse.IsPressed();
    }
}
