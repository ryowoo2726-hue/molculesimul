# Standing Instructions

## Language And Reporting

- While coding, debugging, or running tools, Codex does not need to explain every step in Korean.
- English progress updates are acceptable.
- Keep progress updates short and practical.
- Final responses should include a Korean summary of what was done.
- In the Korean final summary, include key changes, verification results, and any remaining caveats.

## Project Context

- Project: Unity WebGL molecule simulator for middle school grade 8 science.
- Repository path: `c:\src\vscode\science_class\simuloation3molcule`.
- Unity project path: `molculesimul`.
- GitHub remote: `https://github.com/ryowoo2726-hue/molculesimul.git`.
- Main branch: `main`.
- GitHub Pages/WebGL output path: `docs/webgl`.

## Product Goals

- Students use tablets to explore 3D molecule models.
- Elements 1 through 20 are represented as atoms.
- Students build molecules by selecting available bond slots and pressing element buttons.
- Atoms should generally not require direct dragging; camera rotation is the main 3D interaction.
- When a valid stored molecule is matched, show its Korean name and chemical formula.
- When the structure is not recognized or chemically unsuitable, show that it is unstable.
- Teachers can extend molecule definitions through `molculesimul/Assets/Resources/molecules.json`.
- Stored molecule presets should be available from a compact dropdown menu so students can view difficult molecules.

## Important Files

- `molculesimul/Assets/Resources/molecules.json`: molecule definition database.
- `molculesimul/Assets/Scripts/MoleculePresetDropdown.cs`: molecule preset dropdown loading.
- `molculesimul/Assets/Editor/MoleculeSceneBuilder.cs`: generated scene builder.
- `molculesimul/Assets/Editor/WebGLBuild.cs`: WebGL build automation.
- `molculesimul/Assets/Scenes/MoleculeSimulator.unity`: generated main scene.
- `molculesimul/Assets/Scenes/SampleScene.unity`: synced scene for Unity default loading.
- `docs/webgl`: built WebGL deployment files.

## Current Behavior Notes

- Background should be fully black, like outer space.
- Camera should rotate around the molecule center for 3D inspection.
- The UI is designed for tablet use and should avoid overlapping controls.
- Element labels should appear inside atoms and face the camera.
- Bond slots should hide when no more atoms can attach.
- Dropdown option text was previously invisible; keep option rows dark and dropdown text visible.

## Usual Verification

- Regenerate the Unity scene after scene-builder changes.
- Rebuild WebGL after runtime, scene, resource, or UI changes that affect deployment.
- Check local WebGL output through a simple HTTP server, for example `http://localhost:8013/index.html`.
- Commit and push deployment-related changes when the user asks for GitHub updates or deployment readiness.

## Known Non-Critical Warnings

- `SimpleOrbitCamera.cs` may warn that `FindObjectOfType<T>()` is obsolete.
- `AtomDragController.cs` may warn about unused serialized fields.
- These warnings are not currently known to block the WebGL build.
