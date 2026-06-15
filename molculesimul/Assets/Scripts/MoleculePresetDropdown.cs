using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MoleculePresetDropdown : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private Text captionText;
    [SerializeField] private RectTransform listPanel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Button optionButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private AtomSpawner spawner;
    [SerializeField] private MoleculeRecognizer recognizer;
    [SerializeField] private MoleculeStatusView statusView;
    [SerializeField] private MoleculeBuilderController builderController;
    [SerializeField] private TextAsset moleculeJson;
    [SerializeField] private float layoutScale = 1.15f;

    private readonly List<MoleculeDefinition> definitions = new List<MoleculeDefinition>();
    private readonly List<Button> optionButtons = new List<Button>();
    private Font optionFont;
    private bool isOpen;

    private void Awake()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();

        if (captionText == null)
            captionText = GetComponentInChildren<Text>(true);

        if (moleculeJson == null)
            moleculeJson = Resources.Load<TextAsset>("molecules");

        optionFont = captionText != null && captionText.font != null
            ? captionText.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (optionButtonPrefab != null)
            optionButtonPrefab.gameObject.SetActive(false);

        SetListVisible(false);
    }

    private void Start()
    {
        PopulateOptions();
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            SetListVisible(false);
    }

    private void OnEnable()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleList);
    }

    private void OnDisable()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleList);
    }

    private void PopulateOptions()
    {
        ClearOptionButtons();
        definitions.Clear();

        var database = moleculeJson != null
            ? JsonUtility.FromJson<MoleculeDatabase>(moleculeJson.text)
            : null;

        if (database != null && database.molecules != null)
        {
            foreach (var definition in database.molecules)
            {
                if (definition == null || definition.atoms == null || definition.atoms.Length == 0)
                    continue;

                definitions.Add(definition);
                CreateOptionButton(definition, definitions.Count - 1);
            }
        }

        SetCaption(definitions.Count > 0 ? "분자모형 불러오기" : "분자 데이터 없음");
        RebuildListLayout();
    }

    private void CreateOptionButton(MoleculeDefinition definition, int definitionIndex)
    {
        if (contentRoot == null || optionButtonPrefab == null)
            return;

        var optionButton = Instantiate(optionButtonPrefab, contentRoot);
        optionButton.name = string.IsNullOrEmpty(definition.id)
            ? $"Option_{definitionIndex + 1}"
            : $"Option_{definition.id}";
        optionButton.gameObject.SetActive(true);
        optionButton.onClick.RemoveAllListeners();
        optionButton.onClick.AddListener(() => SelectDefinition(definitionIndex));

        var image = optionButton.GetComponent<Image>();
        if (image != null)
            image.color = new Color(0.1f, 0.14f, 0.18f, 0.98f);

        var label = optionButton.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = $"{definition.nameKo} ({definition.formula})";
            ConfigureText(label, Color.white);
        }

        optionButtons.Add(optionButton);
    }

    private void SelectDefinition(int definitionIndex)
    {
        if (definitionIndex < 0 || definitionIndex >= definitions.Count)
            return;

        var definition = definitions[definitionIndex];
        SetCaption($"{definition.nameKo} ({definition.formula})");
        SetListVisible(false);
        LoadMolecule(definition);
    }

    private void ToggleList()
    {
        SetListVisible(!isOpen);
    }

    private void SetListVisible(bool visible)
    {
        isOpen = visible;

        if (listPanel != null)
            listPanel.gameObject.SetActive(visible);

        if (!visible)
            return;

        transform.SetAsLastSibling();
        RebuildListLayout();

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetCaption(string value)
    {
        if (captionText == null)
            return;

        captionText.text = value;
        ConfigureText(captionText, new Color(0.05f, 0.07f, 0.09f));
    }

    private void ConfigureText(Text text, Color color)
    {
        if (text == null)
            return;

        if (text.font == null && optionFont != null)
            text.font = optionFont;

        text.color = color;
        text.enabled = true;
        text.raycastTarget = false;
    }

    private void ClearOptionButtons()
    {
        for (var i = optionButtons.Count - 1; i >= 0; i--)
        {
            if (optionButtons[i] != null)
                Destroy(optionButtons[i].gameObject);
        }

        optionButtons.Clear();
    }

    private void RebuildListLayout()
    {
        if (contentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void LoadMolecule(MoleculeDefinition definition)
    {
        if (workspace == null || spawner == null || definition == null || definition.atoms == null)
            return;

        if (definition.atoms.Length > workspace.MaxAtoms)
        {
            if (statusView != null)
                statusView.ShowMessage("불러올 수 없음", $"{definition.nameKo}은 원자 수가 너무 많아요.");
            return;
        }

        workspace.ClearAll();

        var atoms = new AtomParticle[definition.atoms.Length];
        for (var i = 0; i < definition.atoms.Length; i++)
        {
            var atom = spawner.SpawnAt(definition.atoms[i], GetLayoutPosition(definition, i));
            if (atom == null)
            {
                workspace.ClearAll();
                if (statusView != null)
                    statusView.ShowMessage("불러올 수 없음", $"{definition.formula} 데이터를 확인해 주세요.");
                return;
            }

            atoms[i] = atom;
        }

        if (definition.bonds != null)
        {
            foreach (var bond in definition.bonds)
            {
                if (bond == null || bond.a < 0 || bond.b < 0 || bond.a >= atoms.Length || bond.b >= atoms.Length)
                    continue;

                workspace.TryCreateBond(atoms[bond.a], atoms[bond.b]);
            }
        }

        if (builderController != null)
            builderController.RebuildSlots();

        var match = recognizer != null ? recognizer.FindMatch(workspace, workspace.Atoms) : null;
        if (statusView != null)
        {
            if (match != null)
                statusView.Show(match, workspace.Atoms.Count, workspace.Bonds.Count);
            else
                statusView.ShowMessage(definition.nameKo, $"{definition.formula} / 원자 {workspace.Atoms.Count}개, 결합 {workspace.Bonds.Count}개");
        }
    }

    private Vector3 GetLayoutPosition(MoleculeDefinition definition, int index)
    {
        if (definition.layout != null && index < definition.layout.Length && definition.layout[index] != null)
        {
            var point = definition.layout[index];
            return new Vector3(point.x, point.y, point.z) * layoutScale;
        }

        var angle = index * Mathf.PI * 2f / Mathf.Max(1, definition.atoms.Length);
        var radius = Mathf.Max(0.9f, definition.atoms.Length * 0.12f);
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }
}
