using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SimpleOrbitCamera : MonoBehaviour
{
    [SerializeField] private Vector3 target = Vector3.zero;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private float distance = 8f;
    [SerializeField] private float yaw = 35f;
    [SerializeField] private float pitch = 55f;
    [SerializeField] private float rotateSpeed = 0.18f;
    [SerializeField] private float zoomSpeed = 0.4f;
    [SerializeField] private float minDistance = 4.5f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float smoothing = 14f;

    private Vector2 lastPointer;
    private float lastPinchDistance;
    private float targetDistance;
    private float targetYaw;
    private float targetPitch;
    private bool rotating;

    private void Start()
    {
        if (workspace == null)
            workspace = FindObjectOfType<MoleculeWorkspace>();

        targetDistance = distance;
        targetYaw = yaw;
        targetPitch = pitch;
        ApplyPose();
    }

    private void Update()
    {
        if (Input.touchCount >= 2)
        {
            HandleTwoFingerTouch();
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                rotating = !IsPointerOverUi();
                lastPointer = Input.mousePosition;
            }

            if (Input.GetMouseButton(1) && rotating)
            {
                var pointer = (Vector2)Input.mousePosition;
                var delta = pointer - lastPointer;
                lastPointer = pointer;

                Orbit(delta, 1f);
            }

            if (Input.GetMouseButtonUp(1))
                rotating = false;

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        SmoothPose();
    }

    private void HandleTwoFingerTouch()
    {
        var a = Input.GetTouch(0);
        var b = Input.GetTouch(1);
        if (IsTouchOverUi(a.fingerId) || IsTouchOverUi(b.fingerId))
            return;

        var midpoint = (a.position + b.position) * 0.5f;
        var pinchDistance = Vector2.Distance(a.position, b.position);

        if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
        {
            lastPointer = midpoint;
            lastPinchDistance = pinchDistance;
            return;
        }

        var delta = midpoint - lastPointer;
        lastPointer = midpoint;

        Orbit(delta, 0.65f);

        if (lastPinchDistance > 1f)
        {
            var pinchDelta = pinchDistance - lastPinchDistance;
            targetDistance = Mathf.Clamp(targetDistance - pinchDelta * 0.01f, minDistance, maxDistance);
        }

        lastPinchDistance = pinchDistance;
    }

    private void Orbit(Vector2 delta, float multiplier)
    {
        targetYaw += delta.x * rotateSpeed * multiplier;
        targetPitch -= delta.y * rotateSpeed * multiplier;
        targetPitch = Mathf.Clamp(targetPitch, -85f, 85f);
    }

    private void SmoothPose()
    {
        var t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        if (!AtomDragController.IsDraggingAnyAtom)
            target = Vector3.Lerp(target, GetPrimaryAtomCenter(), t);

        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);
        distance = Mathf.Lerp(distance, targetDistance, t);
        ApplyPose();
    }

    private Vector3 GetPrimaryAtomCenter()
    {
        if (workspace == null || workspace.Atoms.Count == 0)
            return Vector3.zero;

        return workspace.Atoms[0].transform.position;
    }

    private void ApplyPose()
    {
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target + rotation * new Vector3(0f, 0f, -distance);
        transform.LookAt(target, Vector3.up);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsTouchOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}
