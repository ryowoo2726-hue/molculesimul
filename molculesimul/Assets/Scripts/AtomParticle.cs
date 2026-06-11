using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AtomParticle : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private TextMesh label;

    public int Id { get; private set; }
    public string Symbol { get; private set; }
    public ElementInfo Element { get; private set; }

    private void LateUpdate()
    {
        FaceLabelToCamera();
    }

    public void Initialize(int id, ElementInfo element)
    {
        EnsureVisuals();

        Id = id;
        Element = element;
        Symbol = element.symbol;
        name = $"Atom_{id}_{Symbol}";

        transform.localScale = Vector3.one * (element.radius * 2f);

        if (targetRenderer != null)
            targetRenderer.material.color = element.color;

        if (label != null)
        {
            label.text = element.symbol;
            label.color = GetReadableLabelColor(element.color);
        }
    }

    private void FaceLabelToCamera()
    {
        if (label == null)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        var atomRadius = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.5f;
        var toCamera = (camera.transform.position - transform.position).normalized;
        label.transform.position = transform.position + toCamera * (atomRadius * 0.53f);
        label.transform.rotation = Quaternion.LookRotation(label.transform.position - camera.transform.position, camera.transform.up);
    }

    private void EnsureVisuals()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (label != null)
            return;

        var labelObject = new GameObject("SymbolLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.62f);
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one * 0.32f;

        label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = 0.08f;
        label.color = Color.white;
    }

    private static Color GetReadableLabelColor(Color atomColor)
    {
        var brightness = atomColor.r * 0.299f + atomColor.g * 0.587f + atomColor.b * 0.114f;
        return brightness > 0.55f ? new Color(0.04f, 0.05f, 0.06f) : Color.white;
    }

    public void SetSelected(bool selected)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.material.SetFloat("_Glossiness", selected ? 0.75f : 0.25f);
    }
}
