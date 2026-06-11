using UnityEngine;
using UnityEngine.UI;

public sealed class ElementButtonBinder : MonoBehaviour
{
    [SerializeField] private ElementLibrary elementLibrary;
    [SerializeField] private AtomSpawner spawner;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Transform buttonRoot;

    private void Start()
    {
        foreach (var element in elementLibrary.Elements)
        {
            var button = Instantiate(buttonPrefab, buttonRoot);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = $"{element.atomicNumber}\n{element.symbol}";

            var symbol = element.symbol;
            button.onClick.AddListener(() => spawner.Spawn(symbol));
        }
    }
}

