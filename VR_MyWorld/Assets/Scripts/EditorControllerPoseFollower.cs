using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EditorControllerPoseFollower : MonoBehaviour
{
    public enum FollowSide
    {
        Left,
        Right,
        Custom
    }

    [Header("Enable")]
    [Tooltip("是否启用 Editor 下的控制器姿态模拟。关闭后不会改控制器位置。")]
    public bool enableEditorFollow = true;

    [Tooltip("是否只在按住鼠标右键时跟随头显。建议开启，这样不影响你平时操作。")]
    public bool onlyFollowWhileRightMouseHeld = true;

    [Header("References")]
    [Tooltip("拖入 XR Origin/Camera Offset/Main Camera。")]
    public Transform head;

    [Header("Hand Side")]
    public FollowSide followSide = FollowSide.Right;

    [Header("Pose")]
    [Tooltip("是否跟随头显位置。")]
    public bool followPosition = true;

    [Tooltip("是否跟随头显朝向。")]
    public bool followRotation = true;

    [Tooltip("右手默认偏移。单位是米。")]
    public Vector3 rightHandLocalOffset = new Vector3(0.28f, -0.25f, 0.45f);

    [Tooltip("左手默认偏移。单位是米。")]
    public Vector3 leftHandLocalOffset = new Vector3(-0.28f, -0.25f, 0.45f);

    [Tooltip("自定义偏移。followSide 选择 Custom 时使用。")]
    public Vector3 customLocalOffset = new Vector3(0.28f, -0.25f, 0.45f);

    [Header("Rotation Fine Tune")]
    [Tooltip("控制器朝向额外旋转。如果射线方向不对，可以调这里。")]
    public Vector3 extraEulerRotation = Vector3.zero;

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (!enableEditorFollow)
            return;

        if (head == null)
            return;

        if (onlyFollowWhileRightMouseHeld && !IsRightMousePressed())
            return;

        Vector3 offset = GetLocalOffset();

        if (followPosition)
        {
            transform.position = head.TransformPoint(offset);
        }

        if (followRotation)
        {
            transform.rotation = head.rotation * Quaternion.Euler(extraEulerRotation);
        }
#endif
    }

    private Vector3 GetLocalOffset()
    {
        switch (followSide)
        {
            case FollowSide.Left:
                return leftHandLocalOffset;

            case FollowSide.Right:
                return rightHandLocalOffset;

            case FollowSide.Custom:
                return customLocalOffset;

            default:
                return rightHandLocalOffset;
        }
    }

    private bool IsRightMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.rightButton.isPressed;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1);
#else
        return false;
#endif
    }
}