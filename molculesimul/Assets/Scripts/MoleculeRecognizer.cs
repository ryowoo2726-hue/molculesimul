using System.Collections.Generic;
using UnityEngine;

public sealed class MoleculeMatch
{
    public MoleculeDefinition Definition;
    public AtomParticle[] AtomsInDefinitionOrder;
}

public sealed class MoleculeRecognizer : MonoBehaviour
{
    [SerializeField] private TextAsset moleculeJson;

    private MoleculeDatabase database;

    private void Awake()
    {
        if (moleculeJson == null)
            moleculeJson = Resources.Load<TextAsset>("molecules");

        if (moleculeJson == null)
        {
            Debug.LogError("Missing molecule data. Put molecules.json under Assets/Resources.");
            database = new MoleculeDatabase { molecules = new MoleculeDefinition[0] };
            return;
        }

        database = JsonUtility.FromJson<MoleculeDatabase>(moleculeJson.text);
    }

    public MoleculeMatch FindMatch(MoleculeWorkspace workspace)
    {
        var atoms = workspace.Atoms;
        if (atoms.Count == 0 || database == null || database.molecules == null)
            return null;

        var currentAdjacency = workspace.BuildAdjacency(atoms);

        foreach (var definition in database.molecules)
        {
            if (definition.atoms == null || definition.atoms.Length != atoms.Count)
                continue;

            var definitionAdjacency = BuildDefinitionAdjacency(definition);
            var mapping = new int[definition.atoms.Length];
            for (var i = 0; i < mapping.Length; i++)
                mapping[i] = -1;

            var usedCurrentAtoms = new bool[atoms.Count];
            if (TryMap(0, definition, atoms, currentAdjacency, definitionAdjacency, mapping, usedCurrentAtoms))
            {
                var ordered = new AtomParticle[mapping.Length];
                for (var i = 0; i < mapping.Length; i++)
                    ordered[i] = atoms[mapping[i]];

                return new MoleculeMatch
                {
                    Definition = definition,
                    AtomsInDefinitionOrder = ordered
                };
            }
        }

        return null;
    }

    private static bool TryMap(
        int definitionIndex,
        MoleculeDefinition definition,
        IReadOnlyList<AtomParticle> atoms,
        int[,] currentAdjacency,
        int[,] definitionAdjacency,
        int[] mapping,
        bool[] usedCurrentAtoms)
    {
        if (definitionIndex >= definition.atoms.Length)
            return true;

        var symbol = definition.atoms[definitionIndex];
        for (var currentIndex = 0; currentIndex < atoms.Count; currentIndex++)
        {
            if (usedCurrentAtoms[currentIndex] || atoms[currentIndex].Symbol != symbol)
                continue;

            if (!IsCompatible(definitionIndex, currentIndex, definitionAdjacency, currentAdjacency, mapping))
                continue;

            mapping[definitionIndex] = currentIndex;
            usedCurrentAtoms[currentIndex] = true;

            if (TryMap(definitionIndex + 1, definition, atoms, currentAdjacency, definitionAdjacency, mapping, usedCurrentAtoms))
                return true;

            mapping[definitionIndex] = -1;
            usedCurrentAtoms[currentIndex] = false;
        }

        return false;
    }

    private static bool IsCompatible(
        int definitionIndex,
        int currentIndex,
        int[,] definitionAdjacency,
        int[,] currentAdjacency,
        int[] mapping)
    {
        for (var previousDefinitionIndex = 0; previousDefinitionIndex < definitionIndex; previousDefinitionIndex++)
        {
            var previousCurrentIndex = mapping[previousDefinitionIndex];
            if (previousCurrentIndex < 0)
                continue;

            var needsBond = definitionAdjacency[definitionIndex, previousDefinitionIndex] == 1;
            var hasBond = currentAdjacency[currentIndex, previousCurrentIndex] == 1;
            if (needsBond != hasBond)
                return false;
        }

        return true;
    }

    private static int[,] BuildDefinitionAdjacency(MoleculeDefinition definition)
    {
        var adjacency = new int[definition.atoms.Length, definition.atoms.Length];
        if (definition.bonds == null)
            return adjacency;

        foreach (var bond in definition.bonds)
        {
            if (bond == null)
                continue;

            var a = bond.a;
            var b = bond.b;
            if (a < 0 || b < 0 || a >= definition.atoms.Length || b >= definition.atoms.Length)
                continue;

            adjacency[a, b] = 1;
            adjacency[b, a] = 1;
        }

        return adjacency;
    }
}
