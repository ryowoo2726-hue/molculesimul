using UnityEngine;

public sealed class AtomSpawner : MonoBehaviour
{
    [SerializeField] private ElementLibrary elementLibrary;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private AtomParticle atomPrefab;
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private Vector3 spawnCenter = new Vector3(0f, 1f, 0f);
    [SerializeField] private float spawnSpread = 1.5f;

    public void Spawn(string symbol)
    {
        if (!workspace.CanAddAtom())
            return;

        if (!elementLibrary.TryGet(symbol, out var element))
            return;

        var offset = new Vector3(
            Random.Range(-spawnSpread, spawnSpread),
            Random.Range(-0.25f, 0.25f),
            Random.Range(-spawnSpread, spawnSpread));

        var atom = atomPrefab != null
            ? Instantiate(atomPrefab, spawnCenter + offset, Quaternion.identity, spawnRoot)
            : CreateFallbackAtom(spawnCenter + offset);

        atom.Initialize(workspace.GetNextAtomId(), element);
        workspace.RegisterAtom(atom);
    }

    private AtomParticle CreateFallbackAtom(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "AtomFallback";
        go.transform.SetParent(spawnRoot, false);
        go.transform.position = position;
        return go.AddComponent<AtomParticle>();
    }
}
