using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MoleculePresetDropdown : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private AtomSpawner spawner;
    [SerializeField] private MoleculeRecognizer recognizer;
    [SerializeField] private MoleculeStatusView statusView;
    [SerializeField] private MoleculeBuilderController builderController;
    [SerializeField] private TextAsset moleculeJson;
    [SerializeField] private float layoutScale = 1.15f;

    private readonly List<MoleculeDefinition> definitions = new List<MoleculeDefinition>();
    private bool populating;
    private Font optionFont;

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<Dropdown>();

        if (moleculeJson == null)
            moleculeJson = Resources.Load<TextAsset>("molecules");

        optionFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Start()
    {
        PopulateOptions();
    }

    private void LateUpdate()
    {
        FixOpenDropdownText();
    }

    private void OnEnable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.AddListener(LoadSelectedMolecule);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(LoadSelectedMolecule);
    }

    private void PopulateOptions()
    {
        if (dropdown == null)
            return;

        populating = true;
        definitions.Clear();
        dropdown.ClearOptions();

        var options = new List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("분자모형 불러오기")
        };

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
                options.Add(new Dropdown.OptionData($"{definition.nameKo} ({definition.formula})"));
            }
        }

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
        FixTemplateText(dropdown.captionText, new Color(0.05f, 0.07f, 0.09f));
        FixTemplateText(dropdown.itemText, Color.white);
        populating = false;
    }

    private void FixOpenDropdownText()
    {
        var list = GameObject.Find("Dropdown List");
        if (list == null)
            return;

        foreach (var text in list.GetComponentsInChildren<Text>(true))
            FixTemplateText(text, Color.white);
    }

    private void FixTemplateText(Text text, Color color)
    {
        if (text == null)
            return;

        if (text.font == null && optionFont != null)
            text.font = optionFont;

        text.color = color;
        text.enabled = true;
        text.raycastTarget = false;
    }

    private void LoadSelectedMolecule(int optionIndex)
    {
        if (populating || optionIndex <= 0 || optionIndex > definitions.Count)
            return;

        var definition = definitions[optionIndex - 1];
        LoadMolecule(definition);
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
