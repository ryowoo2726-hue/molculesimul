using UnityEngine;

public sealed class BondView : MonoBehaviour
{
    [SerializeField] private float radius = 0.045f;

    public AtomParticle A { get; private set; }
    public AtomParticle B { get; private set; }

    public void Initialize(AtomParticle a, AtomParticle b)
    {
        A = a;
        B = b;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (A == null || B == null)
            return;

        var start = A.transform.position;
        var end = B.transform.position;
        var delta = end - start;

        transform.position = start + delta * 0.5f;
        transform.up = delta.normalized;
        transform.localScale = new Vector3(radius, delta.magnitude * 0.5f, radius);
    }
}

