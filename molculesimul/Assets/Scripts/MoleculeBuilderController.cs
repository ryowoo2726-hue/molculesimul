using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MoleculeBuilderController : MonoBehaviour
{
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private AtomSpawner spawner;
    [SerializeField] private MoleculeRecognizer recognizer;
    [SerializeField] private MoleculeStatusView statusView;
    [SerializeField] private AttachmentSlot slotPrefab;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float slotDistance = 0.95f;
    [SerializeField] private bool restrictToKnownMolecules = true;
    [SerializeField] private int minDirectionChoices = 6;
    [SerializeField] private int maxVisibleSlotsPerAtom = 8;

    private readonly List<AttachmentSlot> slots = new List<AttachmentSlot>();
    private AttachmentSlot selectedSlot;
    private int lastWorkspaceVersion = -1;

    private static readonly Vector3[] LinearDirections =
    {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

    private static readonly Vector3[] BentDirections =
    {
        new Vector3(-0.75f, 0.55f, 0f).normalized,
        new Vector3(0.75f, 0.55f, 0f).normalized,
        new Vector3(-0.75f, -0.55f, 0f).normalized,
        new Vector3(0.75f, -0.55f, 0f).normalized,
        new Vector3(-0.75f, 0f, 0.55f).normalized,
        new Vector3(0.75f, 0f, 0.55f).normalized,
        new Vector3(-0.75f, 0f, -0.55f).normalized,
        new Vector3(0.75f, 0f, -0.55f).normalized
    };

    private static readonly Vector3[] TrigonalDirections =
    {
        Vector3.up,
        new Vector3(-0.87f, -0.5f, 0f).normalized,
        new Vector3(0.87f, -0.5f, 0f).normalized,
        Vector3.down,
        new Vector3(-0.87f, 0.5f, 0f).normalized,
        new Vector3(0.87f, 0.5f, 0f).normalized,
        new Vector3(0f, 0.5f, 0.87f).normalized,
        new Vector3(0f, 0.5f, -0.87f).normalized
    };

    private static readonly Vector3[] TetrahedralDirections =
    {
        new Vector3(1f, 1f, 1f).normalized,
        new Vector3(-1f, -1f, 1f).normalized,
        new Vector3(-1f, 1f, -1f).normalized,
        new Vector3(1f, -1f, -1f).normalized,
        new Vector3(-1f, 1f, 1f).normalized,
        new Vector3(1f, -1f, 1f).normalized,
        new Vector3(1f, 1f, -1f).normalized,
        new Vector3(-1f, -1f, -1f).normalized
    };

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        HandleSlotPointer();

        if (workspace != null && workspace.Version != lastWorkspaceVersion)
        {
            RebuildSlots();
            RefreshRecognition();
        }
    }

    public void CreateFromElementButton(string symbol)
    {
        if (workspace == null || spawner == null || !workspace.CanAddAtom())
            return;

        if (workspace.Atoms.Count == 0)
        {
            var atom = spawner.SpawnAt(symbol, Vector3.zero);
            SelectSlot(null);
            RefreshRecognition(atom);
            return;
        }

        if (selectedSlot == null)
            return;

        var parent = selectedSlot.ParentAtom;
        if (!CanAttachToParent(parent, symbol))
        {
            ShowBlockedAttachment(parent, symbol);
            return;
        }

        var position = selectedSlot.transform.position;
        var newAtom = spawner.SpawnAt(symbol, position);
        if (newAtom == null)
            return;

        if (!workspace.TryCreateBond(parent, newAtom))
        {
            workspace.DeleteAtom(newAtom);
            return;
        }

        SelectSlot(null);
        RefreshRecognition(parent);
    }

    public void SelectSlot(AttachmentSlot slot)
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;

        if (selectedSlot != null)
            selectedSlot.SetSelected(true);
    }

    public void RebuildSlots()
    {
        ClearSlots();

        if (workspace == null || workspace.Atoms.Count == 0)
        {
            lastWorkspaceVersion = workspace != null ? workspace.Version : -1;
            return;
        }

        foreach (var atom in workspace.Atoms)
            CreateSlotsForAtom(atom);

        lastWorkspaceVersion = workspace.Version;
    }

    private void CreateSlotsForAtom(AtomParticle atom)
    {
        var maxBonds = GetMaxBonds(atom.Symbol);
        var currentBonds = workspace.GetBondCount(atom);
        var freeSlots = Mathf.Max(0, maxBonds - currentBonds);
        if (freeSlots == 0)
            return;

        if (ShouldRestrictToDatabase(atom) && recognizer.GetAllowedAttachmentSymbols(workspace, atom).Count == 0)
            return;

        var createdSlots = 0;
        var maxChoices = GetVisibleSlotLimit(freeSlots, GetPreferredDirections(atom.Symbol).Length);
        var occupiedDirections = workspace.GetBondDirections(atom);
        foreach (var direction in GetPreferredDirections(atom.Symbol))
        {
            if (createdSlots >= maxChoices)
                break;

            if (IsDirectionOccupied(direction, occupiedDirections))
                continue;

            CreateSlot(atom, direction);
            createdSlots++;
        }
    }

    private void CreateSlot(AtomParticle atom, Vector3 direction)
    {
        var position = atom.transform.position + direction.normalized * slotDistance;
        AttachmentSlot slot;
        if (slotPrefab != null)
        {
            slot = Instantiate(slotPrefab, position, Quaternion.identity, slotRoot);
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(slotRoot, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.18f;
            slot = go.AddComponent<AttachmentSlot>();
        }

        slot.name = $"Slot_{atom.Symbol}_{slots.Count + 1}";
        slot.Initialize(this, atom, direction);
        slots.Add(slot);
    }

    private void ClearSlots()
    {
        SelectSlot(null);
        for (var i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i] != null)
                Destroy(slots[i].gameObject);
        }

        slots.Clear();
    }

    private void HandleSlotPointer()
    {
        if (targetCamera == null)
            return;

        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !IsTouchOverUi(touch.fingerId))
                TrySelectSlotAt(touch.position);
            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            TrySelectSlotAt(Input.mousePosition);
    }

    private void TrySelectSlotAt(Vector2 screenPosition)
    {
        var ray = targetCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit, 100f))
            return;

        var slot = hit.collider.GetComponentInParent<AttachmentSlot>();
        if (slot != null)
            SelectSlot(slot);
    }

    private void RefreshRecognition(AtomParticle focus = null)
    {
        if (workspace == null || recognizer == null)
            return;

        var focusAtoms = focus != null ? workspace.GetConnectedAtoms(focus) : workspace.Atoms;
        var match = recognizer.FindMatch(workspace, focusAtoms);
        if (match != null)
            workspace.ApplyLayout(match);

        if (statusView != null)
            statusView.Show(match, focusAtoms.Count, workspace.CountBondsWithin(focusAtoms));
    }

    private bool CanAttachToParent(AtomParticle parent, string symbol)
    {
        if (parent == null)
            return false;

        if (workspace.GetBondCount(parent) >= GetMaxBonds(parent.Symbol))
            return false;

        if (!ShouldRestrictToDatabase(parent))
            return true;

        return recognizer.CanAttachAndRemainPossible(workspace, parent, symbol);
    }

    private bool ShouldRestrictToDatabase(AtomParticle parent)
    {
        if (!restrictToKnownMolecules || recognizer == null || parent == null)
            return false;

        var focusAtoms = workspace.GetConnectedAtoms(parent);
        return recognizer.HasPartialCandidate(workspace, focusAtoms);
    }

    private void ShowBlockedAttachment(AtomParticle parent, string symbol)
    {
        if (statusView == null)
            return;

        var parentSymbol = parent != null ? parent.Symbol : "?";
        statusView.ShowMessage("이 위치에는 붙일 수 없음", $"{parentSymbol}에 {symbol}를 붙이면 등록된 분자로 완성하기 어려워요.");
    }

    private int GetVisibleSlotLimit(int freeBonds, int directionCount)
    {
        var requested = Mathf.Max(minDirectionChoices, freeBonds * 3 + 2);
        return Mathf.Min(directionCount, Mathf.Clamp(requested, 1, maxVisibleSlotsPerAtom));
    }

    private static bool IsDirectionOccupied(Vector3 direction, IReadOnlyList<Vector3> occupiedDirections)
    {
        foreach (var occupied in occupiedDirections)
        {
            if (Vector3.Dot(direction.normalized, occupied.normalized) > 0.65f)
                return true;
        }

        return false;
    }

    private static Vector3[] GetPreferredDirections(string symbol)
    {
        switch (symbol)
        {
            case "C":
            case "Si":
                return TetrahedralDirections;
            case "B":
            case "Al":
            case "N":
            case "P":
                return TrigonalDirections;
            case "O":
            case "S":
            case "Be":
            case "Mg":
            case "Ca":
                return BentDirections;
            default:
                return LinearDirections;
        }
    }

    private static int GetMaxBonds(string symbol)
    {
        switch (symbol)
        {
            case "He":
            case "Ne":
            case "Ar":
                return 0;
            case "H":
            case "F":
            case "Cl":
            case "Li":
            case "Na":
            case "K":
                return 1;
            case "O":
            case "S":
            case "Be":
            case "Mg":
            case "Ca":
                return 2;
            case "B":
            case "Al":
            case "N":
            case "P":
                return 3;
            case "C":
            case "Si":
                return 4;
            default:
                return 4;
        }
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
