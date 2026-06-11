using System.Collections.Generic;
using UnityEngine;

public sealed class ElementLibrary : MonoBehaviour
{
    [SerializeField] private List<ElementInfo> elements = new List<ElementInfo>();

    private readonly Dictionary<string, ElementInfo> bySymbol = new Dictionary<string, ElementInfo>();

    public IReadOnlyList<ElementInfo> Elements => elements;

    private void Awake()
    {
        if (elements.Count == 0)
            LoadDefaultElements();

        bySymbol.Clear();
        foreach (var element in elements)
            bySymbol[element.symbol] = element;
    }

    public bool TryGet(string symbol, out ElementInfo element)
    {
        return bySymbol.TryGetValue(symbol, out element);
    }

    private void LoadDefaultElements()
    {
        elements = new List<ElementInfo>
        {
            New(1, "H", "수소", "#E8EEF7", 0.28f),
            New(2, "He", "헬륨", "#D8C9FF", 0.32f),
            New(3, "Li", "리튬", "#B7A4FF", 0.42f),
            New(4, "Be", "베릴륨", "#9AD9A1", 0.38f),
            New(5, "B", "붕소", "#F4B183", 0.36f),
            New(6, "C", "탄소", "#333333", 0.38f),
            New(7, "N", "질소", "#4F7DFF", 0.37f),
            New(8, "O", "산소", "#E94B4B", 0.37f),
            New(9, "F", "플루오린", "#8DE06B", 0.35f),
            New(10, "Ne", "네온", "#F3B2FF", 0.34f),
            New(11, "Na", "나트륨", "#8FA7FF", 0.45f),
            New(12, "Mg", "마그네슘", "#7ED77D", 0.43f),
            New(13, "Al", "알루미늄", "#BFC4CC", 0.41f),
            New(14, "Si", "규소", "#D29A5A", 0.40f),
            New(15, "P", "인", "#FF9A3D", 0.39f),
            New(16, "S", "황", "#FFD43B", 0.39f),
            New(17, "Cl", "염소", "#5FC85F", 0.40f),
            New(18, "Ar", "아르곤", "#8FD7FF", 0.38f),
            New(19, "K", "칼륨", "#B388FF", 0.48f),
            New(20, "Ca", "칼슘", "#83D18C", 0.46f)
        };
    }

    private static ElementInfo New(int number, string symbol, string nameKo, string hex, float radius)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return new ElementInfo
        {
            atomicNumber = number,
            symbol = symbol,
            nameKo = nameKo,
            color = color,
            radius = radius
        };
    }
}

