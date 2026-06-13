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
            SelectAtom(Input.mousePosition);
    }

    public void DeleteSelected()
    {
        if (selected == null)
            return;

        workspace.DeleteAtom(selected);
        selected = null;
        RefreshRecognition();
    }

    private void SelectAtom(Vector2 screenPosition)
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
        RefreshRecognition();
    }

    private void SelectAtom(Touch touch)
    {
        if (IsTouchOverUi(touch.fingerId))
            return;

        SelectAtom(touch.position);
    }

    private void HandleTouch()
    {
        if (Input.touchCount > 1)
            return;

        var touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
            SelectAtom(touch);
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
}
