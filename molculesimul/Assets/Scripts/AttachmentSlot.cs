using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AttachmentSlot : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    private MoleculeBuilderController controller;

    public AtomParticle ParentAtom { get; private set; }
    public Vector3 Direction { get; private set; }

    public void Initialize(MoleculeBuilderController owner, AtomParticle parentAtom, Vector3 direction)
    {
        controller = owner;
        ParentAtom = parentAtom;
        Direction = direction.normalized;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
            targetRenderer.material.color = selected
                ? new Color(1f, 0.86f, 0.22f, 0.95f)
                : new Color(0.35f, 0.75f, 1f, 0.72f);
    }

    private void OnMouseDown()
    {
        if (controller != null)
            controller.SelectSlot(this);
    }
}

