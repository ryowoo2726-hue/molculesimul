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
    private bool touchRotating;
    private bool leftMouseRotating;
    private Camera orbitCamera;

    private void Start()
    {
        if (workspace == null)
            workspace = FindObjectOfType<MoleculeWorkspace>();

        orbitCamera = GetComponent<Camera>();
        if (orbitCamera == null)
            orbitCamera = Camera.main;

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
            HandleSinglePointerOrbit();

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

    private void HandleSinglePointerOrbit()
    {
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchRotating = !IsTouchOverUi(touch.fingerId) && !IsWorldControlAt(touch.position);
                lastPointer = touch.position;
                return;
            }

            if (touchRotating && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                var pointer = touch.position;
                var delta = pointer - lastPointer;
                lastPointer = pointer;
                Orbit(delta, 0.8f);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                touchRotating = false;

            return;
        }

        touchRotating = false;

        if (Input.GetMouseButtonDown(0))
        {
            leftMouseRotating = !IsPointerOverUi() && !IsWorldControlAt(Input.mousePosition);
            lastPointer = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && leftMouseRotating)
        {
            var pointer = (Vector2)Input.mousePosition;
            var delta = pointer - lastPointer;
            lastPointer = pointer;
            Orbit(delta, 1f);
        }

        if (Input.GetMouseButtonUp(0))
            leftMouseRotating = false;
    }

    private void HandleTwoFingerTouch()
    {
        var a = Input.GetTouch(0);
        var b = Input.GetTouch(1);
        touchRotating = false;
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
        target = Vector3.Lerp(target, GetMoleculeCenter(), t);
        yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);
        distance = Mathf.Lerp(distance, targetDistance, t);
        ApplyPose();
    }

    private Vector3 GetMoleculeCenter()
    {
        if (workspace == null || workspace.Atoms.Count == 0)
            return Vector3.zero;

        var center = Vector3.zero;
        foreach (var atom in workspace.Atoms)
            center += atom.transform.position;

        return center / workspace.Atoms.Count;
    }

    private void ApplyPose()
    {
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target + rotation * new Vector3(0f, 0f, -distance);
        transform.LookAt(target, Vector3.up);
    }

    private bool IsWorldControlAt(Vector2 screenPosition)
    {
        if (orbitCamera == null)
            return false;

        var ray = orbitCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit, 100f))
            return false;

        return hit.collider.GetComponentInParent<AttachmentSlot>() != null ||
               hit.collider.GetComponentInParent<AtomParticle>() != null;
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
