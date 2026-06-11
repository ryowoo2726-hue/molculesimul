using System.Collections.Generic;
using UnityEngine;

public sealed class MoleculeWorkspace : MonoBehaviour
{
    [SerializeField] private BondView bondPrefab;
    [SerializeField] private float bondDistance = 0.9f;
    [SerializeField] private float snapLerp = 0.35f;
    [SerializeField] private int maxAtoms = 40;

    private readonly List<AtomParticle> atoms = new List<AtomParticle>();
    private readonly List<BondView> bonds = new List<BondView>();
    private int nextAtomId = 1;

    public IReadOnlyList<AtomParticle> Atoms => atoms;
    public IReadOnlyList<BondView> Bonds => bonds;
    public float BondDistance => bondDistance;
    public int MaxAtoms => maxAtoms;

    public AtomParticle RegisterAtom(AtomParticle atom)
    {
        atoms.Add(atom);
        return atom;
    }

    public int GetNextAtomId()
    {
        return nextAtomId++;
    }

    public bool CanAddAtom()
    {
        return atoms.Count < maxAtoms;
    }

    public void ClearAll()
    {
        for (var i = bonds.Count - 1; i >= 0; i--)
            Destroy(bonds[i].gameObject);

        for (var i = atoms.Count - 1; i >= 0; i--)
            Destroy(atoms[i].gameObject);

        bonds.Clear();
        atoms.Clear();
    }

    public void DeleteAtom(AtomParticle atom)
    {
        for (var i = bonds.Count - 1; i >= 0; i--)
        {
            if (bonds[i].A == atom || bonds[i].B == atom)
            {
                Destroy(bonds[i].gameObject);
                bonds.RemoveAt(i);
            }
        }

        atoms.Remove(atom);
        Destroy(atom.gameObject);
    }

    public bool TryCreateBond(AtomParticle a, AtomParticle b)
    {
        if (a == null || b == null || a == b || HasBond(a, b))
            return false;

        var bond = bondPrefab != null ? Instantiate(bondPrefab, transform) : CreateFallbackBond();
        bond.Initialize(a, b);
        bonds.Add(bond);
        return true;
    }

    public void DeleteBond(AtomParticle a, AtomParticle b)
    {
        for (var i = bonds.Count - 1; i >= 0; i--)
        {
            if (IsSameBond(bonds[i], a, b))
            {
                Destroy(bonds[i].gameObject);
                bonds.RemoveAt(i);
                return;
            }
        }
    }

    public void DeleteDistantBonds(AtomParticle atom, float breakDistance)
    {
        for (var i = bonds.Count - 1; i >= 0; i--)
        {
            var bond = bonds[i];
            if (bond.A != atom && bond.B != atom)
                continue;

            if (Vector3.Distance(bond.A.transform.position, bond.B.transform.position) <= breakDistance)
                continue;

            Destroy(bond.gameObject);
            bonds.RemoveAt(i);
        }
    }

    public AtomParticle FindNearestBondCandidate(AtomParticle source)
    {
        AtomParticle best = null;
        var bestDistance = bondDistance;

        foreach (var atom in atoms)
        {
            if (atom == source || HasBond(source, atom))
                continue;

            var distance = Vector3.Distance(source.transform.position, atom.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = atom;
            }
        }

        return best;
    }

    public void ApplyLayout(MoleculeMatch match)
    {
        if (match == null || match.Definition.layout == null)
            return;

        var layout = match.Definition.layout;
        if (layout.Length != match.AtomsInDefinitionOrder.Length)
            return;

        var center = Vector3.zero;
        foreach (var atom in match.AtomsInDefinitionOrder)
            center += atom.transform.position;
        center /= match.AtomsInDefinitionOrder.Length;

        for (var i = 0; i < layout.Length; i++)
        {
            var p = layout[i];
            if (p == null)
                continue;

            var target = center + new Vector3(p.x, p.y, p.z);
            var atom = match.AtomsInDefinitionOrder[i];
            atom.transform.position = Vector3.Lerp(atom.transform.position, target, snapLerp);
        }
    }

    public int[,] BuildAdjacency(IReadOnlyList<AtomParticle> orderedAtoms)
    {
        var indexByAtom = new Dictionary<AtomParticle, int>();
        for (var i = 0; i < orderedAtoms.Count; i++)
            indexByAtom[orderedAtoms[i]] = i;

        var adjacency = new int[orderedAtoms.Count, orderedAtoms.Count];
        foreach (var bond in bonds)
        {
            if (!indexByAtom.TryGetValue(bond.A, out var ai))
                continue;
            if (!indexByAtom.TryGetValue(bond.B, out var bi))
                continue;

            adjacency[ai, bi] = 1;
            adjacency[bi, ai] = 1;
        }

        return adjacency;
    }

    private bool HasBond(AtomParticle a, AtomParticle b)
    {
        foreach (var bond in bonds)
        {
            if (IsSameBond(bond, a, b))
                return true;
        }

        return false;
    }

    private BondView CreateFallbackBond()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "BondFallback";
        go.transform.SetParent(transform, false);
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.72f, 0.76f, 0.82f);

        return go.AddComponent<BondView>();
    }

    private static bool IsSameBond(BondView bond, AtomParticle a, AtomParticle b)
    {
        return (bond.A == a && bond.B == b) || (bond.A == b && bond.B == a);
    }
}
