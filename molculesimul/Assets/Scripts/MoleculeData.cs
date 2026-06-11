using System;

[Serializable]
public sealed class MoleculeDatabase
{
    public MoleculeDefinition[] molecules;
}

[Serializable]
public sealed class MoleculeDefinition
{
    public string id;
    public string nameKo;
    public string formula;
    public string[] atoms;
    public BondDefinition[] bonds;
    public LayoutPoint[] layout;
}

[Serializable]
public sealed class BondDefinition
{
    public int a;
    public int b;
}

[Serializable]
public sealed class LayoutPoint
{
    public float x;
    public float y;
    public float z;
}
