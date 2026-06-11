using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AtomDragController : MonoBehaviour
{
    public static bool IsDraggingAnyAtom { get; private set; }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private MoleculeRecognizer recognizer;
    [SerializeField] private MoleculeStatusView statusView;
    [SerializeField] private LayerMask atomLayerMask = ~0;
    [SerializeField] private float dragPlaneY = 0f;
    [SerializeField] private float bondBreakMultiplier = 1.8f;

    private AtomParticle selected;
    private System.Collections.Generic.List<AtomParticle> dragGroup = new System.Collections.Generic.List<AtomParticle>();
    private Vector3 dragOffset;
    private Vector3 lastDragPosition;
    private bool dragging;
    private int activeFingerId = -1;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnDisable()
    {
        if (dragging)
            IsDraggingAnyAtom = false;
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
        dragGroup.Clear();
        dragging = false;
        IsDraggingAnyAtom = false;
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
        IsDraggingAnyAtom = true;
        dragGroup = selected == workspace.PrimaryAtom
            ? workspace.GetConnectedAtoms(selected)
            : new System.Collections.Generic.List<AtomParticle> { selected };

        if (TryGetPointOnDragPlane(screenPosition, out var point))
        {
            dragOffset = selected.transform.position - point;
            lastDragPosition = selected.transform.position;
        }
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

        if (!TryGetPointOnDragPlane(screenPosition, out var point))
            return;

        var targetPosition = point + dragOffset;
        var delta = targetPosition - lastDragPosition;
        foreach (var atom in dragGroup)
            atom.transform.position += delta;

        lastDragPosition = targetPosition;
    }

    private void EndDrag()
    {
        if (selected == null || !dragging)
            return;

        workspace.DeleteDistantBonds(selected, workspace.BondDistance * bondBreakMultiplier);

        workspace.TryCreateNearbyBonds(selected);

        dragging = false;
        IsDraggingAnyAtom = false;
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
        var focusAtoms = selected != null ? workspace.GetConnectedAtoms(selected) : workspace.Atoms;
        if (focusAtoms.Count == 0)
            focusAtoms = new System.Collections.Generic.List<AtomParticle>(workspace.Atoms);

        var match = recognizer.FindMatch(workspace, focusAtoms);
        if (match != null)
            workspace.ApplyLayout(match);

        if (statusView != null)
            statusView.Show(match, focusAtoms.Count, workspace.CountBondsWithin(focusAtoms));
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
