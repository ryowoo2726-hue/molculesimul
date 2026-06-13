using System.IO;
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Reflection;

public static class MoleculeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MoleculeSimulator.unity";
    private const string AtomPrefabPath = "Assets/Prefabs/AtomPrefab.prefab";
    private const string BondPrefabPath = "Assets/Prefabs/BondPrefab.prefab";
    private const string SlotPrefabPath = "Assets/Prefabs/AttachmentSlotPrefab.prefab";
    private const string KoreanFontPath = "Assets/Fonts/NotoSansKR-VF.ttf";

    [MenuItem("Tools/Molecule Simulator/Build Complete Scene")]
    public static void BuildCompleteScene()
    {
        EnsureFolders();
        var atomPrefab = CreateAtomPrefab();
        var bondPrefab = CreateBondPrefab();
        var slotPrefab = CreateSlotPrefab();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        var camera = CreateCamera();
        CreateStarfield();
        CreateLight();

        var workspaceObject = new GameObject("MoleculeWorkspace");
        var elementLibrary = workspaceObject.AddComponent<ElementLibrary>();
        var workspace = workspaceObject.AddComponent<MoleculeWorkspace>();
        var recognizer = workspaceObject.AddComponent<MoleculeRecognizer>();
        var spawner = workspaceObject.AddComponent<AtomSpawner>();
        var dragger = workspaceObject.AddComponent<AtomDragController>();
        var builder = workspaceObject.AddComponent<MoleculeBuilderController>();
        var controls = workspaceObject.AddComponent<WorkspaceControls>();

        var atomRoot = new GameObject("Atoms").transform;
        var slotRoot = new GameObject("AttachmentSlots").transform;
        var canvas = CreateCanvas();
        var statusView = CreateStatusPanel(canvas.transform);

        SetField(workspace, "bondPrefab", bondPrefab);
        SetField(spawner, "elementLibrary", elementLibrary);
        SetField(spawner, "workspace", workspace);
        SetField(spawner, "builderController", builder);
        SetField(spawner, "atomPrefab", atomPrefab);
        SetField(spawner, "spawnRoot", atomRoot);
        SetField(dragger, "targetCamera", camera);
        SetField(dragger, "workspace", workspace);
        SetField(dragger, "recognizer", recognizer);
        SetField(dragger, "statusView", statusView);
        SetField(builder, "workspace", workspace);
        SetField(builder, "spawner", spawner);
        SetField(builder, "recognizer", recognizer);
        SetField(builder, "statusView", statusView);
        SetField(builder, "slotPrefab", slotPrefab);
        SetField(builder, "slotRoot", slotRoot);
        SetField(builder, "targetCamera", camera);
        SetField(camera.GetComponent<SimpleOrbitCamera>(), "workspace", workspace);
        SetField(controls, "workspace", workspace);
        SetField(controls, "builderController", builder);
        SetField(controls, "statusView", statusView);

        CreateElementButtons(canvas.transform, spawner, elementLibrary);
        CreateActionButtons(canvas.transform, dragger, controls);
        CreateHintText(canvas.transform);
        CreateEventSystem();

        statusView.Show(null, 0, 0);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Molecule simulator scene created: " + ScenePath);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Editor");
        Directory.CreateDirectory("Assets/Fonts");

        if (!File.Exists(KoreanFontPath) && File.Exists(@"C:\Windows\Fonts\NotoSansKR-VF.ttf"))
            File.Copy(@"C:\Windows\Fonts\NotoSansKR-VF.ttf", KoreanFontPath);

        if (File.Exists(KoreanFontPath))
            AssetDatabase.ImportAsset(KoreanFontPath);
    }

    private static AtomParticle CreateAtomPrefab()
    {
        var material = CreateMaterial("AtomDefault", new Color(0.85f, 0.9f, 1f));
        var atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atom.name = "AtomPrefab";
        atom.transform.localScale = Vector3.one * 0.7f;

        var renderer = atom.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        var particle = atom.AddComponent<AtomParticle>();

        var labelObject = new GameObject("SymbolLabel");
        labelObject.transform.SetParent(atom.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.62f);
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one * 0.32f;

        var label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = 0.08f;
        label.color = Color.white;

        SetField(particle, "targetRenderer", renderer);
        SetField(particle, "label", label);

        var prefab = PrefabUtility.SaveAsPrefabAsset(atom, AtomPrefabPath);
        UnityEngine.Object.DestroyImmediate(atom);
        return prefab.GetComponent<AtomParticle>();
    }

    private static BondView CreateBondPrefab()
    {
        var material = CreateMaterial("BondDefault", new Color(0.72f, 0.76f, 0.82f));
        var bond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bond.name = "BondPrefab";
        bond.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(bond.GetComponent<Collider>());
        var view = bond.AddComponent<BondView>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(bond, BondPrefabPath);
        UnityEngine.Object.DestroyImmediate(bond);
        return prefab.GetComponent<BondView>();
    }

    private static AttachmentSlot CreateSlotPrefab()
    {
        var material = CreateUnlitMaterial("AttachmentSlot", new Color(0.35f, 0.75f, 1f, 0.72f));
        var slot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slot.name = "AttachmentSlotPrefab";
        slot.transform.localScale = Vector3.one * 0.18f;
        slot.GetComponent<MeshRenderer>().sharedMaterial = material;
        var view = slot.AddComponent<AttachmentSlot>();
        SetField(view, "targetRenderer", slot.GetComponent<MeshRenderer>());
        var prefab = PrefabUtility.SaveAsPrefabAsset(slot, SlotPrefabPath);
        UnityEngine.Object.DestroyImmediate(slot);
        return prefab.GetComponent<AttachmentSlot>();
    }

    private static Camera CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 45f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<SimpleOrbitCamera>();
        return camera;
    }

    private static void CreateLight()
    {
        var lightObject = new GameObject("Main Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.65f;
        light.shadows = LightShadows.None;
    }

    private static void CreateStarfield()
    {
        var starObject = new GameObject("Starfield");
        var meshFilter = starObject.AddComponent<MeshFilter>();
        var meshRenderer = starObject.AddComponent<MeshRenderer>();

        const int starCount = 180;
        const float radius = 42f;
        var vertices = new Vector3[starCount * 4];
        var uvs = new Vector2[starCount * 4];
        var triangles = new int[starCount * 6];
        var random = new System.Random(240611);

        for (var i = 0; i < starCount; i++)
        {
            var theta = random.NextDouble() * Math.PI * 2.0;
            var phi = Math.Acos(2.0 * random.NextDouble() - 1.0);
            var direction = new Vector3(
                (float)(Math.Sin(phi) * Math.Cos(theta)),
                (float)(Math.Cos(phi)),
                (float)(Math.Sin(phi) * Math.Sin(theta))).normalized;

            var center = direction * radius;
            var inward = (-direction).normalized;
            var tangent = Vector3.Cross(Vector3.up, inward);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(Vector3.right, inward);
            tangent.Normalize();
            var bitangent = Vector3.Cross(inward, tangent).normalized;
            var size = Mathf.Lerp(0.035f, 0.095f, (float)random.NextDouble());
            var v = i * 4;
            var t = i * 6;

            vertices[v] = center - tangent * size - bitangent * size;
            vertices[v + 1] = center + tangent * size - bitangent * size;
            vertices[v + 2] = center + tangent * size + bitangent * size;
            vertices[v + 3] = center - tangent * size + bitangent * size;

            uvs[v] = new Vector2(0f, 0f);
            uvs[v + 1] = new Vector2(1f, 0f);
            uvs[v + 2] = new Vector2(1f, 1f);
            uvs[v + 3] = new Vector2(0f, 1f);

            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        var mesh = new Mesh
        {
            name = "GeneratedStarfield"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = CreateUnlitMaterial("Starfield", new Color(0.9f, 0.94f, 1f, 1f));
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static MoleculeStatusView CreateStatusPanel(Transform parent)
    {
        var panel = CreateUiObject("StatusPanel", parent, new Vector2(24f, -24f), new Vector2(430f, 124f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 0.88f);

        var title = CreateText("Title", panel.transform, "불안정한 구조", 32, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(title.rectTransform, new Vector2(20f, -12f), new Vector2(390f, 48f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        var detail = CreateText("Detail", panel.transform, "원자 0개, 결합 0개", 23, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(detail.rectTransform, new Vector2(20f, -64f), new Vector2(390f, 40f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        var view = panel.AddComponent<MoleculeStatusView>();
        SetField(view, "titleText", title);
        SetField(view, "detailText", detail);
        return view;
    }

    private static void CreateElementButtons(Transform parent, AtomSpawner spawner, ElementLibrary library)
    {
        var root = CreateUiObject("ElementButtons", parent, new Vector2(20f, 20f), new Vector2(876f, 144f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        var symbols = new[] { "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne", "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar", "K", "Ca" };

        for (var i = 0; i < symbols.Length; i++)
        {
            library.TryGet(symbols[i], out var element);
            var col = i % 10;
            var row = i / 10;
            var button = CreateButton(root.transform, $"Element_{symbols[i]}", new Vector2(col * 88f, -row * 70f), new Vector2(80f, 62f), $"{i + 1}\n{symbols[i]}");
            button.GetComponent<Image>().color = element != null ? element.color : Color.white;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(button.GetComponent<Image>().color, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(button.GetComponent<Image>().color, Color.black, 0.15f);
            button.colors = colors;
            UnityEventTools.AddStringPersistentListener(button.onClick, spawner.Spawn, symbols[i]);
        }
    }

    private static void CreateActionButtons(Transform parent, AtomDragController dragger, WorkspaceControls controls)
    {
        var deleteButton = CreateButton(parent, "DeleteSelectedButton", new Vector2(-190f, 174f), new Vector2(156f, 64f), "선택삭제", new Vector2(1f, 0f), new Vector2(1f, 0f));
        UnityEventTools.AddPersistentListener(deleteButton.onClick, dragger.DeleteSelected);

        var clearButton = CreateButton(parent, "ClearAllButton", new Vector2(-24f, 174f), new Vector2(156f, 64f), "전체삭제", new Vector2(1f, 0f), new Vector2(1f, 0f));
        UnityEventTools.AddPersistentListener(clearButton.onClick, controls.ClearAll);
    }

    private static void CreateHintText(Transform parent)
    {
        var hint = CreateText("HintText", parent, "첫 원소를 만든 뒤 파란 결합 자리를 선택하고 다음 원소를 누르세요.", 20, FontStyle.Normal, TextAnchor.UpperRight);
        hint.color = new Color(0.9f, 0.94f, 0.98f, 0.86f);
        SetRect(hint.rectTransform, new Vector2(-24f, -24f), new Vector2(650f, 34f), new Vector2(1f, 1f), new Vector2(1f, 1f));
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label)
    {
        return CreateButton(parent, name, anchoredPosition, size, label, new Vector2(0f, 1f), new Vector2(0f, 1f));
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateUiObject(name, parent, anchoredPosition, size, anchorMin, anchorMax);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.94f, 0.96f, 0.98f, 0.97f);
        var button = go.AddComponent<Button>();
        var text = CreateText("Text", go.transform, label, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = new Color(0.05f, 0.07f, 0.09f);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 24;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = GetUiFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Font GetUiFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath);
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, anchoredPosition, size, anchorMin, anchorMax);
        return go;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        var path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        return material;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        var path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        return material;
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.Update();
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);

        field.SetValue(target, value);
        if (target is UnityEngine.Object unityObject)
            EditorUtility.SetDirty(unityObject);
    }
}
