# Middle School Molecule Simulator

Unity WebGL project for a tablet-friendly 3D molecule building activity.

## Goal

Students create atoms from elements 1-20, drag them in 3D, connect and separate them, and see whether the current structure matches a known molecule.

The first version focuses on:

- 3D atom particles for elements 1-20
- Touch/mouse drag interaction
- Snap-to-bond behavior near valid connection points
- Molecule recognition by atom type, atom count, and bond structure
- Teacher-editable molecule definitions in JSON
- Lightweight WebGL-friendly runtime design

## Project Structure

- Unity project: `molculesimul/`
- Main scene: `molculesimul/Assets/Scenes/MoleculeSimulator.unity`
- Molecule data: `molculesimul/Assets/Resources/molecules.json`
- GitHub Pages build output: `docs/webgl/`
- GitHub Pages redirect page: `docs/index.html`

## Unity Setup

1. Open `molculesimul/` in Unity.
2. Open `Assets/Scenes/MoleculeSimulator.unity`.
3. To regenerate the scene, run `Tools > Molecule Simulator > Build Complete Scene`.
4. To rebuild WebGL, run `Tools > Molecule Simulator > Build WebGL`.

Detailed Korean setup guide: `molculesimul/docs/UNITY_SETUP_KO.md`

## Teacher Data

Molecules can be added in:

`molculesimul/Assets/Resources/molecules.json`

Each molecule defines:

- Display name
- Formula
- Required atoms
- Required bonds
- Optional 3D layout positions for snapping into a familiar model shape

## Recommended First Classroom Molecules

- H2
- O2
- N2
- H2O
- CO2
- NH3
- CH4
- HCl
- NaCl
- CaO

## GitHub Pages Deployment

1. Create a new GitHub repository.
2. Push this folder to the repository.
3. In GitHub, open `Settings > Pages`.
4. Set Source to `Deploy from a branch`.
5. Select branch `main` and folder `/docs`.
6. Save.

After GitHub Pages finishes publishing, open:

`https://YOUR_USERNAME.github.io/YOUR_REPOSITORY/`

The root page redirects to:

`https://YOUR_USERNAME.github.io/YOUR_REPOSITORY/webgl/`

## Local Web Test

From the repository root:

```powershell
python -m http.server 8013 --directory docs/webgl
```

Then open:

`http://localhost:8013/`
