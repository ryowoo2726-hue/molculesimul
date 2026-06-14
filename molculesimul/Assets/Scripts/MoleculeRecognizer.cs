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
        return FindMatch(workspace, workspace.Atoms);
    }

    public bool HasPartialCandidate(MoleculeWorkspace workspace, IReadOnlyList<AtomParticle> atoms)
    {
        if (workspace == null || atoms == null || atoms.Count == 0 || database == null || database.molecules == null)
            return false;

        var symbols = new string[atoms.Count];
        for (var i = 0; i < atoms.Count; i++)
            symbols[i] = atoms[i].Symbol;

        return HasCompatiblePartialDefinition(symbols, workspace.BuildAdjacency(atoms));
    }

    public bool CanAttachAndRemainPossible(MoleculeWorkspace workspace, AtomParticle parent, string newSymbol)
    {
        if (workspace == null || parent == null || string.IsNullOrEmpty(newSymbol) || database == null || database.molecules == null)
            return true;

        var atoms = workspace.GetConnectedAtoms(parent);
        var parentIndex = atoms.IndexOf(parent);
        if (parentIndex < 0)
            return true;

        var symbols = new string[atoms.Count + 1];
        for (var i = 0; i < atoms.Count; i++)
            symbols[i] = atoms[i].Symbol;
        symbols[symbols.Length - 1] = newSymbol;

        var existingAdjacency = workspace.BuildAdjacency(atoms);
        var adjacency = new int[symbols.Length, symbols.Length];
        for (var y = 0; y < existingAdjacency.GetLength(0); y++)
        {
            for (var x = 0; x < existingAdjacency.GetLength(1); x++)
                adjacency[y, x] = existingAdjacency[y, x];
        }

        var newIndex = symbols.Length - 1;
        adjacency[parentIndex, newIndex] = 1;
        adjacency[newIndex, parentIndex] = 1;

        return HasCompatiblePartialDefinition(symbols, adjacency);
    }

    public HashSet<string> GetAllowedAttachmentSymbols(MoleculeWorkspace workspace, AtomParticle parent)
    {
        var allowed = new HashSet<string>();
        if (workspace == null || parent == null || database == null || database.molecules == null)
            return allowed;

        foreach (var definition in database.molecules)
        {
            if (definition.atoms == null)
                continue;

            foreach (var symbol in definition.atoms)
            {
                if (allowed.Contains(symbol))
                    continue;

                if (CanAttachAndRemainPossible(workspace, parent, symbol))
                    allowed.Add(symbol);
            }
        }

        return allowed;
    }

    public MoleculeMatch FindMatch(MoleculeWorkspace workspace, IReadOnlyList<AtomParticle> atoms)
    {
        if (atoms.Count == 0 || database == null || database.molecules == null)
            return null;

        var currentAdjacency = workspace.BuildAdjacency(atoms);

        foreach (var definition in database.molecules)
        {
            if (definition.atoms == null || definition.atoms.Length != atoms.Count)
                continue;

            if (!HasSameAtomCounts(definition, atoms))
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

    private bool HasCompatiblePartialDefinition(string[] partialSymbols, int[,] partialAdjacency)
    {
        foreach (var definition in database.molecules)
        {
            if (definition.atoms == null || definition.atoms.Length < partialSymbols.Length)
                continue;

            if (!HasEnoughAtomCounts(definition, partialSymbols))
                continue;

            var definitionAdjacency = BuildDefinitionAdjacency(definition);
            var order = BuildPartialMappingOrder(partialSymbols, partialAdjacency);
            var partialToDefinition = new int[partialSymbols.Length];
            for (var i = 0; i < partialToDefinition.Length; i++)
                partialToDefinition[i] = -1;

            var usedDefinitionAtoms = new bool[definition.atoms.Length];
            if (TryMapPartial(0, order, partialSymbols, partialAdjacency, definition, definitionAdjacency, partialToDefinition, usedDefinitionAtoms))
                return true;
        }

        return false;
    }

    private static bool HasSameAtomCounts(MoleculeDefinition definition, IReadOnlyList<AtomParticle> atoms)
    {
        var counts = new Dictionary<string, int>();
        foreach (var atom in atoms)
        {
            if (!counts.ContainsKey(atom.Symbol))
                counts[atom.Symbol] = 0;
            counts[atom.Symbol]++;
        }

        foreach (var symbol in definition.atoms)
        {
            if (!counts.TryGetValue(symbol, out var count) || count == 0)
                return false;
            counts[symbol] = count - 1;
        }

        foreach (var pair in counts)
        {
            if (pair.Value != 0)
                return false;
        }

        return true;
    }

    private static bool HasEnoughAtomCounts(MoleculeDefinition definition, IReadOnlyList<string> symbols)
    {
        var counts = new Dictionary<string, int>();
        foreach (var symbol in definition.atoms)
        {
            if (!counts.ContainsKey(symbol))
                counts[symbol] = 0;
            counts[symbol]++;
        }

        foreach (var symbol in symbols)
        {
            if (!counts.TryGetValue(symbol, out var count) || count == 0)
                return false;
            counts[symbol] = count - 1;
        }

        return true;
    }

    private static int[] BuildPartialMappingOrder(string[] partialSymbols, int[,] partialAdjacency)
    {
        var order = new int[partialSymbols.Length];
        for (var i = 0; i < order.Length; i++)
            order[i] = i;

        System.Array.Sort(order, (a, b) =>
        {
            var degreeCompare = CountDegree(partialAdjacency, b).CompareTo(CountDegree(partialAdjacency, a));
            if (degreeCompare != 0)
                return degreeCompare;

            return string.CompareOrdinal(partialSymbols[a], partialSymbols[b]);
        });

        return order;
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

    private static bool TryMapPartial(
        int orderIndex,
        int[] order,
        string[] partialSymbols,
        int[,] partialAdjacency,
        MoleculeDefinition definition,
        int[,] definitionAdjacency,
        int[] partialToDefinition,
        bool[] usedDefinitionAtoms)
    {
        if (orderIndex >= order.Length)
            return true;

        var partialIndex = order[orderIndex];
        var symbol = partialSymbols[partialIndex];
        var partialDegree = CountDegree(partialAdjacency, partialIndex);

        for (var definitionIndex = 0; definitionIndex < definition.atoms.Length; definitionIndex++)
        {
            if (usedDefinitionAtoms[definitionIndex] || definition.atoms[definitionIndex] != symbol)
                continue;

            if (CountDegree(definitionAdjacency, definitionIndex) < partialDegree)
                continue;

            if (!IsPartialCompatible(partialIndex, definitionIndex, partialAdjacency, definitionAdjacency, partialToDefinition))
                continue;

            partialToDefinition[partialIndex] = definitionIndex;
            usedDefinitionAtoms[definitionIndex] = true;

            if (TryMapPartial(orderIndex + 1, order, partialSymbols, partialAdjacency, definition, definitionAdjacency, partialToDefinition, usedDefinitionAtoms))
                return true;

            partialToDefinition[partialIndex] = -1;
            usedDefinitionAtoms[definitionIndex] = false;
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

    private static bool IsPartialCompatible(
        int partialIndex,
        int definitionIndex,
        int[,] partialAdjacency,
        int[,] definitionAdjacency,
        int[] partialToDefinition)
    {
        for (var previousPartialIndex = 0; previousPartialIndex < partialToDefinition.Length; previousPartialIndex++)
        {
            var previousDefinitionIndex = partialToDefinition[previousPartialIndex];
            if (previousDefinitionIndex < 0)
                continue;

            if (partialAdjacency[partialIndex, previousPartialIndex] == 1 &&
                definitionAdjacency[definitionIndex, previousDefinitionIndex] != 1)
                return false;
        }

        return true;
    }

    private static int CountDegree(int[,] adjacency, int index)
    {
        var degree = 0;
        var count = adjacency.GetLength(0);
        for (var i = 0; i < count; i++)
        {
            if (adjacency[index, i] == 1)
                degree++;
        }

        return degree;
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
