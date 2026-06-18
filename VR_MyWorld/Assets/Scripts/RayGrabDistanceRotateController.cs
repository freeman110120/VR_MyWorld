using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class RayGrabDistanceRotateController : MonoBehaviour
{
    [Header("XR References")]
    public XRRayInteractor rayInteractor;

    [Tooltip("建议拖入 Right Controller 下的 RayAttach，并且它也要设置到 XR Ray Interactor 的 Attach Transform。")]
    public Transform attachTransform;

    [Tooltip("用于定义旋转参考方向，建议拖入 XR Origin/Camera Offset/Main Camera。")]
    public Transform rotateReference;

    [Header("Input Actions")]
    [Tooltip("右摇杆输入。可以拖 XRI RightHand Locomotion / Turn，使用它的 Y 轴做拉近拉远。")]
    public InputActionReference distanceAction;

    [Tooltip("左摇杆输入。可以拖 XRI LeftHand Locomotion / Move，用它控制物体旋转。")]
    public InputActionReference rotateAction;

    [Header("Distance Control")]
    public bool enableDistanceControl = true;
    public float distanceSpeed = 2f;
    public float minDistance = 0.4f;
    public float maxDistance = 8f;

    [Tooltip("如果右摇杆向后时方向反了，就切换这个。")]
    public bool invertDistanceInput = true;

    [Header("Rotation Control")]
    public bool enableRotationControl = true;
    public float rotationSpeed = 120f;

    [Header("Disable Locomotion While Grabbing")]
    [Tooltip("抓取物体时临时禁用这些组件，比如 Continuous Move Provider 和 Snap Turn Provider，避免摇杆同时控制玩家移动/转向。")]
    public Behaviour[] disableWhileSelecting;

    private bool isSelecting;
    private float currentDistance;
    private Quaternion currentRotation;

    private void Reset()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
    }

    private void Awake()
    {
        if (rayInteractor == null)
            rayInteractor = GetComponent<XRRayInteractor>();

        if (attachTransform == null && rayInteractor != null)
            attachTransform = rayInteractor.attachTransform;
    }

    private void OnEnable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.AddListener(OnSelectEntered);
            rayInteractor.selectExited.AddListener(OnSelectExited);
        }

        distanceAction?.action?.Enable();
        rotateAction?.action?.Enable();
    }

    private void OnDisable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.RemoveListener(OnSelectEntered);
            rayInteractor.selectExited.RemoveListener(OnSelectExited);
        }

        SetLocomotionEnabled(true);
    }

    private void LateUpdate()
    {
        if (!isSelecting || attachTransform == null || rayInteractor == null)
            return;

        Transform rayOrigin = GetRayOrigin();
        if (rayOrigin == null)
            return;

        HandleDistance(rayOrigin);
        HandleRotation(rayOrigin);

        attachTransform.position = rayOrigin.position + rayOrigin.forward * currentDistance;
        attachTransform.rotation = currentRotation;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelecting = true;

        Transform rayOrigin = GetRayOrigin();

        if (rayOrigin != null && attachTransform != null)
        {
            // 如果射线当前命中了物体，则从命中点开始计算距离，避免一抓就跳位置。
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                currentDistance = Vector3.Distance(rayOrigin.position, hit.point);
                attachTransform.position = hit.point;
            }
            else
            {
                currentDistance = Vector3.Distance(rayOrigin.position, attachTransform.position);
            }

            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            currentRotation = attachTransform.rotation;
        }

        SetLocomotionEnabled(false);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isSelecting = false;
        SetLocomotionEnabled(true);
    }

    private Transform GetRayOrigin()
    {
        if (rayInteractor.rayOriginTransform != null)
            return rayInteractor.rayOriginTransform;

        return rayInteractor.transform;
    }

    private void HandleDistance(Transform rayOrigin)
    {
        if (!enableDistanceControl || distanceAction == null)
            return;

        Vector2 input = distanceAction.action.ReadValue<Vector2>();

        float y = input.y;

        if (invertDistanceInput)
            y = -y;

        currentDistance += y * distanceSpeed * Time.deltaTime;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    private void HandleRotation(Transform rayOrigin)
    {
        if (!enableRotationControl || rotateAction == null)
            return;

        Vector2 input = rotateAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.01f)
            return;

        Transform reference = rotateReference != null ? rotateReference : rayOrigin;

        float yaw = input.x * rotationSpeed * Time.deltaTime;
        float pitch = -input.y * rotationSpeed * Time.deltaTime;

        currentRotation = Quaternion.AngleAxis(yaw, reference.up) * currentRotation;
        currentRotation = Quaternion.AngleAxis(pitch, reference.right) * currentRotation;
    }

    private void SetLocomotionEnabled(bool enabled)
    {
        if (disableWhileSelecting == null)
            return;

        foreach (Behaviour behaviour in disableWhileSelecting)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }
}