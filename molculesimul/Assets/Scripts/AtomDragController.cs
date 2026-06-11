using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AtomDragController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private MoleculeRecognizer recognizer;
    [SerializeField] private MoleculeStatusView statusView;
    [SerializeField] private LayerMask atomLayerMask = ~0;
    [SerializeField] private float dragPlaneY = 0f;
    [SerializeField] private float bondBreakMultiplier = 1.8f;

    private AtomParticle selected;
    private Vector3 dragOffset;
    private bool dragging;
    private int activeFingerId = -1;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouch();
            return;
        }

        if (Input.GetMouseButtonDown(0))
            BeginDrag(Input.mousePosition);

        if (Input.GetMouseButton(0))
            ContinueDrag(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    public void DeleteSelected()
    {
        if (selected == null)
            return;

        workspace.DeleteAtom(selected);
        selected = null;
        dragging = false;
        activeFingerId = -1;
        RefreshRecognition();
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var ray = targetCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit, 100f, atomLayerMask))
            return;

        var hitAtom = hit.collider.GetComponentInParent<AtomParticle>();
        if (hitAtom == null)
            return;

        if (selected != null && selected != hitAtom)
            selected.SetSelected(false);

        selected = hitAtom;
        selected.SetSelected(true);
        dragging = true;

        if (TryGetPointOnDragPlane(screenPosition, out var point))
            dragOffset = selected.transform.position - point;
    }

    private void BeginDrag(Touch touch)
    {
        if (IsTouchOverUi(touch.fingerId))
            return;

        BeginDrag(touch.position);
        if (dragging)
            activeFingerId = touch.fingerId;
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        if (selected == null || !dragging)
            return;

        if (TryGetPointOnDragPlane(screenPosition, out var point))
            selected.transform.position = point + dragOffset;
    }

    private void EndDrag()
    {
        if (selected == null || !dragging)
            return;

        workspace.DeleteDistantBonds(selected, workspace.BondDistance * bondBreakMultiplier);

        var candidate = workspace.FindNearestBondCandidate(selected);
        if (candidate != null)
            workspace.TryCreateBond(selected, candidate);

        dragging = false;
        activeFingerId = -1;
        RefreshRecognition();
    }

    private void HandleTouch()
    {
        if (Input.touchCount > 1)
        {
            if (dragging)
                EndDrag();
            return;
        }

        var touch = Input.GetTouch(0);
        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginDrag(touch);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (activeFingerId == touch.fingerId)
                    ContinueDrag(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (activeFingerId == touch.fingerId)
                    EndDrag();
                break;
        }
    }

    private static bool IsTouchOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private void RefreshRecognition()
    {
        var match = recognizer.FindMatch(workspace);
        if (match != null)
            workspace.ApplyLayout(match);

        if (statusView != null)
            statusView.Show(match, workspace.Atoms.Count, workspace.Bonds.Count);
    }

    private bool TryGetPointOnDragPlane(Vector2 screenPosition, out Vector3 point)
    {
        var ray = targetCamera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));
        if (plane.Raycast(ray, out var enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = Vector3.zero;
        return false;
    }
}
