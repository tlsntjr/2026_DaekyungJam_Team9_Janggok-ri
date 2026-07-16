# 이펙트/엔딩/투척물 구현 설계

날짜: 2026-07-16
담당 범위: 안개·화면왜곡·비네트 연출, 오염 2단계 점프스케어, 엔딩 연출, 조개껍데기 투척

## 배경

팀장이 작성한 코어/뼈대 코드(EventBus, ContaminationSystem, EndingEvaluator, InventorySystem,
SoundManager 등)는 이미 존재하며 수정하지 않는다. 이 스펙은 그 위에 얹는 "위쪽" 레이어
(Region/Threats/Counters/Player/Sounds와 동급) 4개 기능을 다룬다. 새 스크립트는 전부
EventBus 이벤트 또는 Systems의 public API만 구독/호출하며, 반대 방향 의존은 만들지 않는다.

안개 파티클, 실루엣 스프라이트, 엔딩 컷신 아트, FMOD 사운드 이벤트는 아직 제공되지 않았으므로
전부 nullable/optional 필드로 만들어 지금은 스킵되고 나중에 에셋이 들어오면 바로 반영되게 한다.

## 필요한 기존 파일 수정 (2건)

1. `Assets/Scripts/Systems/ObjectiveSystem.cs`
   클래스 선언을 `public class ObjectiveSystem : MonoBehaviour` →
   `public class ObjectiveSystem : MonoBehaviour, IObjective` 로 변경.
   기존 멤버(`HasFlag`, `SetFlag`, `OnFlagChanged`)가 이미 `IObjective` 시그니처와 일치하므로
   순수 인터페이스 선언 추가일 뿐 동작 변경 없음. 이게 없으면
   `EndingEvaluator.Evaluate(IObjective)`에 `ObjectiveSystem.Instance`를 넘길 수 없어 엔딩
   기능이 컴파일되지 않는다.

2. `Assets/Scripts/Core/GameManager.cs`
   이미 정의만 되어 있고 아무도 세팅하지 않는 `GameState.Ending`을 실제로 쓸 수 있게
   `Pause()/Resume()`과 같은 패턴으로 `public void SetEnding() => CurrentState = GameState.Ending;`
   한 줄 추가.

## 신규 파일

### `Assets/Scripts/Effects/ContaminationEffectsDirector.cs` (기능1)

- `EventBus.OnContaminationChanged(float)` 하나만 `OnEnable`/`OnDisable`로 구독 (규칙 ① 준수).
- `[SerializeField] ParticleSystem fogParticles` (nullable) — 할당돼 있으면 emission
  `rateOverTimeMultiplier`를 오염도 값에 비례하여 설정. null이면 스킵.
- `[SerializeField] Material distortionMaterial` (nullable) — 할당돼 있으면
  `material.SetFloat("_Distortion", value)` 호출. 이 머티리얼은 아래 셰이더 +
  Unity 내장 Full Screen Pass Renderer Feature에 꽂는 용도.
- `[SerializeField] Volume volume` (nullable) — 할당돼 있으면 프로필에서
  `Vignette`/`ColorAdjustments` 오버라이드를 `TryGet` 하고 없으면 `profile.Add<T>(true)`로
  런타임에 직접 추가(에셋 파일을 손대지 않음). 값에 비례해 `vignette.intensity`,
  `colorAdjustments.saturation`(오염도가 오를수록 채도 하락) 등을 구동.
- 값이 0~1이므로 변환 없이 그대로 강도로 사용.

### `Assets/Art/Effects/FullscreenDistortion.shader` (기능1 보조 에셋)

- 순수 ShaderLab/HLSL로 작성하는 풀스크린 웨이브 왜곡 셰이더. `_Distortion`(float),
  `_BlitTexture`(URP Full Screen Pass Renderer Feature가 자동 바인딩) 프로퍼티 사용.
  Shader Graph 파일(.shadergraph)은 내부 JSON을 손으로 만들면 에디터 밖에서 검증이
  안 되어 깨질 위험이 커서 제외.
- 완성 후 에디터에서 해야 할 일(별도로 단계별 안내 예정): Full Screen Pass Renderer
  Feature를 `Renderer2D.asset`에 추가하고 이 셰이더로 만든 머티리얼을 Pass Material에
  연결, 카메라의 Post Processing 옵션 확인.

### `Assets/Scripts/Effects/JumpscareDirector.cs` (기능2)

- `EventBus.OnContaminationStageChanged(int)` 구독.
- 스테이지 2 진입 시 랜덤 인터벌(`minInterval`~`maxInterval`, 인스펙터) 코루틴 시작.
  스테이지가 2 밖으로 벗어나면 코루틴 정지.
- 매 트리거마다: (a) 풀스크린 플래시(간단한 `CanvasGroup`/`Image` 알파 펄스, 아트 없이도
  동작) (b) `SoundManager.Instance.PlayOneShot(jumpscareSfx, ...)` — `EventReference`는
  일단 비워둠(팀장에게 사운드 요청 예정) (c) `[SerializeField] Sprite silhouette`
  (nullable) — 할당되면 화면 가장자리에 짧게 스폰, 없으면 스킵.
- 스테이지 3 진입 시 `stage3FrequencyMultiplier` 필드로 빈도만 올림(자유 재량 반영, 기본값 1.5).

### `Assets/Scripts/Effects/EndingDirector.cs` (기능3, SCENE_ENDING 배치용)

- `Start()`에서 `EndingEvaluator.Evaluate(ObjectiveSystem.Instance)` 호출 →
  `GameManager.Instance.SetEnding()`.
- `[SerializeField] GameObject[] endingVisuals` (Bad=0/Normal=1/True=2 인덱스) — 해당
  타입 오브젝트만 활성화. 배열이 비어있거나 해당 인덱스가 없으면
  `DialogueSystem.Instance.Show($"엔딩: {type}")`로 텍스트 폴백. 나중에 실제 컷신
  프리팹/애니메이터를 이 슬롯에 꽂기만 하면 되는 구조("추가 안내 예정" 대응).
- 일정 시간 경과 또는 임의 키 입력 후 `SceneFlow.Instance.FadeAndLoad(lobbySceneName)`.
- `[SerializeField] string lobbySceneName = "SCENE_LOBBY"` — 현재 존재하지 않는 씬 이름
  자리표시자. 팀장이 나중에 해당 이름으로 씬을 만들면 그대로 동작.

### `Assets/Scripts/Player/Throwable.cs` (기능4)

- `Update()`에서 `Input.GetMouseButtonDown(0)` 감지.
- `InventorySystem.Instance.Has(shellItemId)`가 false면
  `DialogueSystem.Instance.Show("던질 것이 없다.")` 후 종료.
- true면 `InventorySystem.Instance.Remove(shellItemId)` → 마우스 월드 좌표 계산
  (`Camera.main.ScreenToWorldPoint`) → `[SerializeField] GameObject throwVisualPrefab`
  (nullable, 기존 `Shell_Placeholder` 프리팹 연결 가능) 있으면 Instantiate 후 일정 시간
  뒤 파괴 → `SoundManager.Instance.PlayOneShot(throwSfx, worldPos)` (빈 EventReference) →
  `EventBus.RaiseNoiseEmitted(worldPos, noiseRadius)`. 누가 반응하는지는 알 필요 없음.
- `[SerializeField] string shellItemId`, `[SerializeField] float noiseRadius` 인스펙터 노출.

## 테스트/검증 계획

- Unity 에디터가 있는 실행 환경이 아니므로 자동 플레이 테스트는 불가. 대신:
  - 모든 신규 스크립트가 기존 public API(EventBus Raise 함수, Systems의 Instance API)와
    시그니처가 일치하는지 코드 리뷰 수준에서 교차 확인.
  - 가능하면 `Unity.exe -batchmode -quit -projectPath ... -executeMethod` 등으로 컴파일
    에러만이라도 확인 (Unity 설치 여부 확인 후 시도, 없으면 육안 검토로 대체).
- 완료 후 팀장 안내 문서: Full Screen Pass Renderer Feature 연결, Global Volume 배치,
  각 스크립트 인스펙터 필드(머티리얼/파티클/스프라이트/프리팹) 연결 단계를 별도로
  정리해 전달.

## 범위 제외

- 안개 파티클, 실루엣 아트, 엔딩 컷신 콘텐츠, FMOD 사운드 이벤트 제작 — 전부 팀장/외부
  제공 대기. 코드는 슬롯만 마련.
- DialogueUI, HUD 등 실제 화면 렌더링 — 리드 담당, 본 스펙에서 다루지 않음(단, 텍스트
  폴백은 `DialogueSystem.Show` 이벤트만 쏘고 실제 렌더링은 리드의 DialogueUI가 처리).
- `Renderer2D.asset`, `DefaultVolumeProfile.asset` 등 직렬화 에셋 파일 직접 편집 — 위험
  회피를 위해 제외, 대신 런타임 코드(Volume 오버라이드 자동 추가) 또는 에디터 GUI 안내로 대체.
