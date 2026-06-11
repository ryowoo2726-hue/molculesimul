# Unity 씬 구성 가이드

## 1. 프로젝트 만들기

Unity Hub에서 3D 프로젝트를 만들고, 이 저장소의 `Assets` 폴더를 프로젝트 안에 둡니다.

권장 Unity 설정:

- Template: 3D 또는 3D Core
- Platform: WebGL
- Color Space: Gamma 또는 Linear 모두 가능
- Quality: WebGL에서는 그림자와 후처리 효과를 낮게 설정

## 2. 원자 프리팹 만들기

1. `GameObject > 3D Object > Sphere`를 만듭니다.
2. 이름을 `AtomPrefab`으로 바꿉니다.
3. `AtomParticle` 스크립트를 붙입니다.
4. Sphere의 `Renderer`를 `Target Renderer`에 연결합니다.
5. 원소 기호를 3D로 보이게 하려면 자식 오브젝트로 `TextMesh`를 만들고 `Label`에 연결합니다.
6. Sphere에 `Collider`가 있어야 드래그 선택이 됩니다.
7. 완성한 오브젝트를 `Assets/Prefabs` 폴더로 드래그해서 프리팹으로 만듭니다.

## 3. 결합 프리팹 만들기

1. `GameObject > 3D Object > Cylinder`를 만듭니다.
2. 이름을 `BondPrefab`으로 바꿉니다.
3. `BondView` 스크립트를 붙입니다.
4. 얇은 회색 재질을 적용합니다.
5. `Assets/Prefabs` 폴더로 드래그해서 프리팹으로 만듭니다.

## 4. 실험 공간 만들기

빈 오브젝트를 만들고 이름을 `MoleculeWorkspace`로 바꿉니다.

아래 스크립트를 붙입니다.

- `ElementLibrary`
- `MoleculeWorkspace`
- `MoleculeRecognizer`
- `AtomSpawner`
- `AtomDragController`

연결할 필드:

- `MoleculeWorkspace > Bond Prefab`: `BondPrefab`
- `AtomSpawner > Element Library`: 같은 오브젝트의 `ElementLibrary`
- `AtomSpawner > Workspace`: 같은 오브젝트의 `MoleculeWorkspace`
- `AtomSpawner > Atom Prefab`: `AtomPrefab`
- `AtomDragController > Workspace`: 같은 오브젝트의 `MoleculeWorkspace`
- `AtomDragController > Recognizer`: 같은 오브젝트의 `MoleculeRecognizer`

## 5. UI 만들기

Canvas를 만들고 다음을 배치합니다.

- 원소 버튼 영역
- 삭제 버튼
- 분자 이름 표시 Text
- 화학식/원자 수/결합 수 표시 Text

원소 버튼 자동 생성을 쓰려면:

1. 버튼 하나를 `ElementButtonPrefab`으로 만듭니다.
2. 빈 오브젝트에 `ElementButtonBinder`를 붙입니다.
3. `Button Prefab`, `Button Root`, `Element Library`, `Spawner`를 연결합니다.

삭제 버튼에는 `AtomDragController.DeleteSelected()`를 연결합니다.

## 6. 분자 추가 방법

`Assets/Resources/molecules.json`에 새 분자를 추가합니다.

예시:

```json
{
  "id": "hcl",
  "nameKo": "염화 수소",
  "formula": "HCl",
  "atoms": ["H", "Cl"],
  "bonds": [{"a": 0, "b": 1}],
  "layout": [
    {"x": -0.5, "y": 0.0, "z": 0.0},
    {"x": 0.5, "y": 0.0, "z": 0.0}
  ]
}
```

`atoms`의 순서와 `layout`의 순서는 서로 대응합니다.

`bonds`의 숫자는 `atoms` 배열의 번호입니다. 첫 번째 원자는 0번입니다.

## 7. WebGL 최적화 체크리스트

- 한 번에 만들 수 있는 원자 수는 `MoleculeWorkspace > Max Atoms`에서 제한합니다.
- 원자 프리팹은 고해상도 구체 대신 낮은 세그먼트 구체를 씁니다.
- 조명은 Directional Light 1개부터 시작합니다.
- 실시간 그림자는 꺼도 충분합니다.
- 카메라는 고정 시점으로 시작하는 것이 태블릿에서 편합니다.
- `Project Settings > Player > WebGL`에서 Compression Format을 `Gzip` 또는 `Brotli`로 설정합니다.
- GitHub Pages 배포 시 압축 파일 MIME 설정 문제가 생기면 `Decompression Fallback`을 켭니다.

## 8. 자동 생성 도구

상단 메뉴에서 다음을 실행할 수 있습니다.

- `Tools > Molecule Simulator > Build Complete Scene`: 씬, 프리팹, UI를 자동 생성합니다.
- `Tools > Molecule Simulator > Build WebGL`: GitHub Pages용 WebGL 결과물을 `docs/webgl`에 생성합니다.

배포할 때는 GitHub 저장소 Settings의 Pages에서 `Deploy from a branch`, 폴더는 `/docs`로 설정하면 됩니다. 앱 주소는 보통 `https://계정명.github.io/저장소명/webgl/` 형태가 됩니다.

