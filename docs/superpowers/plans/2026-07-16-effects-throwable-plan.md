# 이펙트/엔딩/투척물 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 오염도 기반 안개/화면왜곡/비네트 연출, 오염 2단계 점프스케어, 엔딩 평가+로비 복귀, 조개껍데기 투척을 EventBus/Systems만 구독하는 새 스크립트로 구현한다.

**Architecture:** `Assets/Scripts/Effects/`에 EventBus·Systems를 구독만 하는 3개 디렉터(연출) 스크립트를 추가하고, `Assets/Scripts/Player/Throwable.cs`에 투척 액션을 추가한다. 기존 파일은 `ObjectiveSystem`에 인터페이스 선언 1줄, `GameManager`에 메서드 1개만 추가한다. 안개 파티클/실루엣 스프라이트/사운드 이벤트는 전부 nullable 필드로 두어 지금은 스킵되고 에셋이 들어오면 바로 반영된다.

**Tech Stack:** Unity 6000.4.9f1, C#, URP 17.4.0 (Volume/Vignette/ColorAdjustments, Full Screen Pass Renderer Feature), FMOD (FMODUnity.EventReference).

## Global Constraints

- 오염도 관련 수치는 항상 0~1 스케일 그대로 사용한다 (변환 금지).
- EventBus 구독은 반드시 `OnEnable`에서 걸고 `OnDisable`에서 해제한다.
- 사운드는 전부 `SoundManager.Instance`를 경유하며 FMOD API를 직접 호출하지 않는다.
- 소음 발생은 `EventBus.RaiseNoiseEmitted(Vector2 pos, float radius)` 한 줄로만 알린다 (누가 반응하는지 모른다).
- 이 코드베이스 전체에 자동화 테스트 프레임워크가 쓰인 적이 없다 (`com.unity.test-framework` 패키지는 있지만 Tests 폴더/asmdef 없음). 따라서 이 계획은 기존 관례를 깨고 새 테스트 어셈블리를 만들지 않는다. 대신 각 태스크마다 실제 API 시그니처와 정확히 일치하는지 `grep`으로 기계적으로 검증하고, 마지막 태스크에서 실제 Unity 배치모드 컴파일로 전체를 검증한다.
- Unity 실행 파일 경로(로컬 확인됨): `/c/Program Files/Unity/Hub/Editor/6000.4.9f1/Editor/Unity.exe`

---

### Task 1: ObjectiveSystem에 IObjective 구현 선언 + GameManager에 SetEnding() 추가

**Files:**
- Modify: `Assets/Scripts/Systems/ObjectiveSystem.cs:9`
- Modify: `Assets/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: 기존 `IObjective` 인터페이스(`Assets/Scripts/Core/Interfaces/IObjective.cs`) — `bool HasFlag(string id)`, `void SetFlag(string id)`, `event Action<string> OnFlagChanged` (이미 `ObjectiveSystem`에 동일 시그니처로 존재).
- Produces: `ObjectiveSystem.Instance`를 `IObjective` 타입으로 넘길 수 있게 됨 (Task 5의 `EndingEvaluator.Evaluate(IObjective)` 호출에 필요). `GameManager.Instance.SetEnding()` — `GameState.Ending`으로 전환하는 public 메서드 (Task 5에서 사용).

- [ ] **Step 1: ObjectiveSystem 클래스 선언 수정**

`Assets/Scripts/Systems/ObjectiveSystem.cs` 9번째 줄을 다음과 같이 변경한다:

변경 전:
```csharp
public class ObjectiveSystem : MonoBehaviour
```

변경 후:
```csharp
public class ObjectiveSystem : MonoBehaviour, IObjective
```

- [ ] **Step 2: 변경 확인**

Run: `grep -n "class ObjectiveSystem" "/c/Janggok-ri-main/Assets/Scripts/Systems/ObjectiveSystem.cs"`
Expected: `public class ObjectiveSystem : MonoBehaviour, IObjective` 한 줄만 출력.

- [ ] **Step 3: GameManager에 SetEnding() 메서드 추가**

`Assets/Scripts/Core/GameManager.cs`에서 `Resume()` 메서드 바로 뒤(닫는 `}` 다음, 클래스가 끝나는 `}` 이전)에 다음 메서드를 추가한다:

```csharp
	/// <summary>
	/// 엔딩 연출 진입 시 상태 전환
	/// </summary>
	public void SetEnding()
	{
		CurrentState = GameState.Ending;
	}
```

- [ ] **Step 4: 변경 확인**

Run: `grep -n "SetEnding" "/c/Janggok-ri-main/Assets/Scripts/Core/GameManager.cs"`
Expected: 메서드 선언과 `CurrentState = GameState.Ending;` 줄이 함께 출력됨 (최소 2줄).

- [ ] **Step 5: 두 파일 모두 문법적으로 닫혀 있는지 중괄호 개수로 확인**

Run: `for f in "/c/Janggok-ri-main/Assets/Scripts/Systems/ObjectiveSystem.cs" "/c/Janggok-ri-main/Assets/Scripts/Core/GameManager.cs"; do o=$(grep -o "{" "$f" | wc -l); c=$(grep -o "}" "$f" | wc -l); echo "$f open=$o close=$c"; done`
Expected: 두 파일 모두 `open`과 `close` 숫자가 동일.

---

### Task 2: ContaminationEffectsDirector (안개·왜곡·비네트)

**Files:**
- Create: `Assets/Scripts/Effects/ContaminationEffectsDirector.cs`

**Interfaces:**
- Consumes: `EventBus.OnContaminationChanged` (event, `Action<float>`, 값 0~1) — 구독/해제만, `Raise`는 호출하지 않음. `UnityEngine.Rendering.Volume`(씬 배치용, nullable), `UnityEngine.Rendering.Universal.Vignette`/`ColorAdjustments` (URP 내장 VolumeComponent, `TryGet`/`Add<T>`로 접근).
- Produces: 아무 것도 다른 태스크가 참조하지 않는 독립 컴포넌트. 인스펙터 필드 `fogParticles`, `distortionMaterial`, `volume`, `fogMaxRateOverTime`, `maxDistortion`, `maxVignetteIntensity`, `minSaturation`.

- [ ] **Step 1: 폴더 생성 및 파일 작성**

`Assets/Scripts/Effects/` 폴더를 만들고 `ContaminationEffectsDirector.cs`를 다음 내용으로 작성한다:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 오염도 값(0~1)에 비례해 안개 파티클, 풀스크린 왜곡, Vignette/색보정을 동시에 구동한다.
/// 안개 파티클/왜곡 머티리얼은 아직 준비되지 않았으면 필드를 비워두면 스킵된다.
/// </summary>
public class ContaminationEffectsDirector : MonoBehaviour
{
    [Header("안개 파티클 (없으면 스킵)")]
    [SerializeField] private ParticleSystem fogParticles;
    [SerializeField] private float fogMaxRateOverTime = 20f;

    [Header("풀스크린 왜곡 (없으면 스킵, Full Screen Pass Renderer Feature의 Pass Material)")]
    [SerializeField] private Material distortionMaterial;
    [SerializeField] private float maxDistortion = 1f;

    [Header("Vignette / 색보정 (씬의 Global Volume, 없으면 스킵)")]
    [SerializeField] private Volume volume;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private float minSaturation = -60f;

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (volume == null || volume.profile == null) return;

        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;

        if (!volume.profile.TryGet(out colorAdjustments))
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;
    }

    private void OnEnable()  => EventBus.OnContaminationChanged += HandleContaminationChanged;
    private void OnDisable() => EventBus.OnContaminationChanged -= HandleContaminationChanged;

    private void HandleContaminationChanged(float value)
    {
        if (fogParticles != null)
        {
            var emission = fogParticles.emission;
            emission.rateOverTimeMultiplier = fogMaxRateOverTime * value;
        }

        if (distortionMaterial != null)
            distortionMaterial.SetFloat("_Distortion", value * maxDistortion);

        if (vignette != null)
            vignette.intensity.value = value * maxVignetteIntensity;

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = Mathf.Lerp(0f, minSaturation, value);
    }
}
```

- [ ] **Step 2: 이벤트 구독/해제 짝 확인**

Run: `grep -c "EventBus.OnContaminationChanged" "/c/Janggok-ri-main/Assets/Scripts/Effects/ContaminationEffectsDirector.cs"`
Expected: `2` (구독 1줄 + 해제 1줄).

- [ ] **Step 3: OnEnable/OnDisable 짝 확인**

Run: `grep -n "OnEnable\|OnDisable" "/c/Janggok-ri-main/Assets/Scripts/Effects/ContaminationEffectsDirector.cs"`
Expected: `OnEnable`과 `OnDisable` 각각 정확히 1줄씩 출력.

---

### Task 3: FullscreenDistortion.shader + 에디터 연결 안내

**Files:**
- Create: `Assets/Art/Effects/FullscreenDistortion.shader`
- Create: `docs/superpowers/plans/2026-07-16-distortion-editor-setup.md` (사람이 에디터에서 수행할 단계 안내)

**Interfaces:**
- Consumes: Unity 내장 `Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl` (URP 코어에 포함된 파일, `Vert`/`Varyings`/`_BlitTexture`/`sampler_LinearClamp` 제공).
- Produces: `_Distortion` (Range 0~1) 셰이더 프로퍼티 — Task 2의 `ContaminationEffectsDirector.distortionMaterial.SetFloat("_Distortion", ...)`가 이 이름을 그대로 사용하므로 정확히 일치해야 한다.

- [ ] **Step 1: 셰이더 작성**

`Assets/Art/Effects/FullscreenDistortion.shader`:

```shaderlab
Shader "Hidden/Janggokri/FullscreenDistortion"
{
    Properties
    {
        _Distortion ("Distortion", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FullscreenDistortion"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Distortion;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float wave = sin((uv.y + _Time.y * 0.3) * 40.0) * 0.01 * _Distortion;
                uv.x += wave;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
```

- [ ] **Step 2: 프로퍼티 이름이 Task 2와 정확히 일치하는지 확인**

Run: `grep -o "_Distortion" "/c/Janggok-ri-main/Assets/Art/Effects/FullscreenDistortion.shader" "/c/Janggok-ri-main/Assets/Scripts/Effects/ContaminationEffectsDirector.cs"`
Expected: 양쪽 파일 모두에서 `_Distortion`이 최소 1회 이상씩 출력됨 (이름 불일치 시 왜곡이 조용히 무시되므로 반드시 동일해야 함).

- [ ] **Step 3: 에디터 연결 안내 문서 작성**

`docs/superpowers/plans/2026-07-16-distortion-editor-setup.md`:

```markdown
# 왜곡 셰이더 에디터 연결 안내 (팀원2 직접 수행)

1. Project 창에서 `Assets/Art/Effects/FullscreenDistortion.shader`로 머티리얼을 하나
   만든다 (우클릭 → Create → Material, Shader 드롭다운에서
   `Hidden/Janggokri/FullscreenDistortion` 선택). 이름은 `M_FullscreenDistortion`
   추천.
2. `Assets/Settings/Renderer2D.asset` 선택 → Inspector 맨 아래
   `Add Renderer Feature` 버튼 → `Full Screen Pass Renderer Feature` 선택.
3. 방금 추가된 Feature의 `Pass Material` 필드에 1번에서 만든 머티리얼을 드래그.
   `Injection Point`는 기본값(After Rendering Post Processing) 그대로 두면 된다.
4. 씬의 Main Camera를 선택 → Camera 컴포넌트에서 `Rendering > Post Processing`이
   켜져 있는지 확인 (꺼져있으면 왜곡/비네트 전부 안 보임).
5. 씬에 빈 GameObject를 하나 만들고 `Volume` 컴포넌트를 추가, `Is Global` 체크,
   `Profile` 필드에 아무 프로필이나 연결(비어있는 새 프로필도 됨 — Vignette/색보정은
   코드가 자동으로 추가함).
6. `ContaminationEffectsDirector` 컴포넌트(Task 2에서 생성)를 아무 매니저
   오브젝트에 붙이고, `Distortion Material`에 1번 머티리얼, `Volume`에 5번
   오브젝트를 드래그해서 연결.
7. Play 모드에서 오염도가 오르면(디버그로 `ContaminationSystem.Instance.Add(0.1f)`
   호출해봐도 됨) 화면이 흔들리고 Vignette가 진해지는지 확인.
```

---

### Task 4: JumpscareDirector (오염 2단계 점프스케어)

**Files:**
- Create: `Assets/Scripts/Effects/JumpscareDirector.cs`

**Interfaces:**
- Consumes: `EventBus.OnContaminationStageChanged` (event, `Action<int>`, 값 0/1/2/3). `SoundManager.Instance.PlayOneShot(FMODUnity.EventReference evt, Vector3 worldPos, string localParam = null, float paramValue = 0f)`. `FMODUnity.EventReference.IsNull` (bool, 비어있는 레퍼런스인지 확인 — 실제 FMOD 프로젝트 소스 `Assets/Plugins/FMOD/src/EventReference.cs`에 정의된 public 프로퍼티, 확인 완료).
- Produces: 아무 것도 다른 태스크가 참조하지 않는 독립 컴포넌트. 인스펙터 필드 `flashOverlay`, `silhouette`, `silhouetteRenderer`, `jumpscareSfx`, `minInterval`, `maxInterval`, `stage3FrequencyMultiplier`, `flashDuration`, `silhouetteDuration`.

- [ ] **Step 1: 파일 작성**

`Assets/Scripts/Effects/JumpscareDirector.cs`:

```csharp
using System.Collections;
using UnityEngine;
using FMODUnity;

/// <summary>
/// 오염 2단계 진입 시 랜덤 간격으로 풀스크린 플래시(+실루엣, +효과음)를 재생한다.
/// 실루엣 스프라이트/효과음은 아직 없으면 필드를 비워두면 스킵된다.
/// </summary>
public class JumpscareDirector : MonoBehaviour
{
    [Header("연출 대상 (없으면 스킵)")]
    [SerializeField] private CanvasGroup flashOverlay;
    [SerializeField] private Sprite silhouette;
    [SerializeField] private SpriteRenderer silhouetteRenderer;

    [Header("사운드 (팀장에게 요청 예정, 비워두면 스킵)")]
    [SerializeField] private EventReference jumpscareSfx;

    [Header("타이밍")]
    [SerializeField] private float minInterval = 4f;
    [SerializeField] private float maxInterval = 10f;
    [SerializeField] private float stage3FrequencyMultiplier = 1.5f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float silhouetteDuration = 0.3f;

    private Coroutine loop;
    private int currentStage;

    private void OnEnable()  => EventBus.OnContaminationStageChanged += HandleStageChanged;
    private void OnDisable() => EventBus.OnContaminationStageChanged -= HandleStageChanged;

    private void HandleStageChanged(int stage)
    {
        currentStage = stage;

        if (stage >= 2 && loop == null)
            loop = StartCoroutine(ScareLoop());
        else if (stage < 2 && loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator ScareLoop()
    {
        while (true)
        {
            float interval = Random.Range(minInterval, maxInterval);
            if (currentStage >= 3)
                interval /= stage3FrequencyMultiplier;

            yield return new WaitForSeconds(interval);
            TriggerScare();
        }
    }

    private void TriggerScare()
    {
        if (flashOverlay != null)
            StartCoroutine(FlashRoutine());

        if (silhouette != null && silhouetteRenderer != null)
            StartCoroutine(SilhouetteRoutine());

        if (!jumpscareSfx.IsNull)
            SoundManager.Instance.PlayOneShot(jumpscareSfx, Camera.main.transform.position);
    }

    private IEnumerator FlashRoutine()
    {
        flashOverlay.alpha = 1f;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashOverlay.alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
            yield return null;
        }
        flashOverlay.alpha = 0f;
    }

    private IEnumerator SilhouetteRoutine()
    {
        silhouetteRenderer.sprite = silhouette;
        silhouetteRenderer.enabled = true;
        yield return new WaitForSeconds(silhouetteDuration);
        silhouetteRenderer.enabled = false;
    }
}
```

- [ ] **Step 2: 이벤트 구독/해제 짝 확인**

Run: `grep -c "EventBus.OnContaminationStageChanged" "/c/Janggok-ri-main/Assets/Scripts/Effects/JumpscareDirector.cs"`
Expected: `2`

- [ ] **Step 3: SoundManager 경유 확인 (FMOD API 직접 호출 금지 규칙 준수)**

Run: `grep -n "RuntimeManager\.\|EventInstance" "/c/Janggok-ri-main/Assets/Scripts/Effects/JumpscareDirector.cs"`
Expected: 출력 없음 (아무것도 매치되지 않아야 함 — FMOD 저수준 API를 직접 쓰지 않았다는 뜻).

---

### Task 5: EndingDirector (엔딩 평가 + 로비 복귀)

**Files:**
- Create: `Assets/Scripts/Effects/EndingDirector.cs`

**Interfaces:**
- Consumes: `EndingEvaluator.Evaluate(IObjective state)` → `EndingType`(`Bad`=0/`Normal`=1/`True`=2, `Assets/Scripts/Systems/EndingEvaluator.cs`). `ObjectiveSystem.Instance`(Task 1에서 `IObjective` 구현 완료). `GameManager.Instance.SetEnding()`(Task 1에서 추가). `DialogueSystem.Instance.Show(string line)`. `SceneFlow.Instance.FadeAndLoad(string sceneName)`.
- Produces: 인스펙터 필드 `evaluator`(EndingEvaluator 참조), `endingVisuals`(GameObject[3]), `lobbySceneName`(string, 기본값 `"SCENE_LOBBY"`), `autoReturnDelay`(float).

- [ ] **Step 1: 파일 작성**

`Assets/Scripts/Effects/EndingDirector.cs`:

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// SCENE_ENDING에 배치. 씬 진입 시 엔딩을 평가하고, 타입별 연출을 보여준 뒤
/// 일정 시간(또는 아무 키 입력) 후 로비 씬으로 돌아간다.
/// 엔딩 컷신 콘텐츠가 아직 없으면 endingVisuals를 비워두면 텍스트로 대체된다.
/// </summary>
public class EndingDirector : MonoBehaviour
{
    [Header("엔딩 평가")]
    [SerializeField] private EndingEvaluator evaluator;

    [Header("타입별 연출 오브젝트 (Bad=0 / Normal=1 / True=2, 비어있으면 텍스트 폴백)")]
    [SerializeField] private GameObject[] endingVisuals;

    [Header("로비 복귀")]
    [SerializeField] private string lobbySceneName = "SCENE_LOBBY";
    [SerializeField] private float autoReturnDelay = 8f;

    private bool returning;

    private void Start()
    {
        EndingType result = evaluator.Evaluate(ObjectiveSystem.Instance);
        GameManager.Instance.SetEnding();
        ShowEnding(result);
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private void ShowEnding(EndingType type)
    {
        int index = (int)type;
        bool hasVisual = endingVisuals != null && index >= 0 && index < endingVisuals.Length && endingVisuals[index] != null;

        if (hasVisual)
        {
            for (int i = 0; i < endingVisuals.Length; i++)
                if (endingVisuals[i] != null)
                    endingVisuals[i].SetActive(i == index);
        }
        else
        {
            DialogueSystem.Instance.Show($"엔딩: {type}");
        }
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        float t = 0f;
        while (t < autoReturnDelay)
        {
            if (Input.anyKeyDown)
                break;
            t += Time.deltaTime;
            yield return null;
        }

        ReturnToLobby();
    }

    private void ReturnToLobby()
    {
        if (returning) return;
        returning = true;
        SceneFlow.Instance.FadeAndLoad(lobbySceneName);
    }
}
```

- [ ] **Step 2: EndingType 인덱스 매핑이 실제 enum 순서와 일치하는지 확인**

Run: `grep -n "enum EndingType" "/c/Janggok-ri-main/Assets/Scripts/Systems/EndingEvaluator.cs"`
Expected: `public enum EndingType { Bad, Normal, True }` — 순서가 Bad(0)/Normal(1)/True(2)로, `EndingDirector.ShowEnding`의 배열 인덱스 주석과 일치해야 함.

- [ ] **Step 3: Task 1 의존성이 실제로 존재하는지 확인**

Run: `grep -n "IObjective\|SetEnding" "/c/Janggok-ri-main/Assets/Scripts/Systems/ObjectiveSystem.cs" "/c/Janggok-ri-main/Assets/Scripts/Core/GameManager.cs"`
Expected: `ObjectiveSystem.cs`에서 `IObjective` 매치, `GameManager.cs`에서 `SetEnding` 매치가 각각 나와야 함 (Task 1이 먼저 끝나 있어야 이 태스크가 컴파일됨).

---

### Task 6: Throwable (조개껍데기 투척)

**Files:**
- Create: `Assets/Scripts/Player/Throwable.cs`

**Interfaces:**
- Consumes: `InventorySystem.Instance.Has(string itemId)` / `.Remove(string itemId)` (`Assets/Scripts/Systems/InventorySystem.cs`). `DialogueSystem.Instance.Show(string line)`. `SoundManager.Instance.PlayOneShot(EventReference evt, Vector3 worldPos, ...)`. `EventBus.RaiseNoiseEmitted(Vector2 pos, float radius)`.
- Produces: 인스펙터 필드 `shellItemId`(string, 기본값 `"shell"` — 실제 `ItemDefinition.itemId`와 맞춰야 함), `noiseRadius`, `throwVisualPrefab`, `visualLifetime`, `throwSfx`.

- [ ] **Step 1: 파일 작성**

`Assets/Scripts/Player/Throwable.cs`:

```csharp
using UnityEngine;
using FMODUnity;

/// <summary>
/// 마우스 왼쪽 클릭으로 조개껍데기를 소모해 마우스 위치에 투척하고 소음을 방송한다.
/// 누가 소음에 반응하는지는 몰라도 된다 (몬스터가 알아서 감지).
/// </summary>
public class Throwable : MonoBehaviour
{
    [Header("소모 아이템")]
    [SerializeField] private string shellItemId = "shell";

    [Header("소음")]
    [SerializeField] private float noiseRadius = 5f;

    [Header("연출 (없으면 스킵)")]
    [SerializeField] private GameObject throwVisualPrefab;
    [SerializeField] private float visualLifetime = 1f;

    [Header("사운드 (팀장에게 요청 예정, 비워두면 스킵)")]
    [SerializeField] private EventReference throwSfx;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryThrow();
    }

    private void TryThrow()
    {
        if (!InventorySystem.Instance.Has(shellItemId))
        {
            DialogueSystem.Instance.Show("던질 것이 없다.");
            return;
        }

        InventorySystem.Instance.Remove(shellItemId);

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;

        if (throwVisualPrefab != null)
        {
            GameObject visual = Instantiate(throwVisualPrefab, worldPos, Quaternion.identity);
            Destroy(visual, visualLifetime);
        }

        if (!throwSfx.IsNull)
            SoundManager.Instance.PlayOneShot(throwSfx, worldPos);

        EventBus.RaiseNoiseEmitted(worldPos, noiseRadius);
    }
}
```

- [ ] **Step 2: 소음 방송 호출 확인**

Run: `grep -n "EventBus.RaiseNoiseEmitted" "/c/Janggok-ri-main/Assets/Scripts/Player/Throwable.cs"`
Expected: 정확히 1줄, `EventBus.RaiseNoiseEmitted(worldPos, noiseRadius);`

- [ ] **Step 3: 인벤토리 없을 때 아무 일도 안 일어나는지(early return) 확인**

Run: `grep -n "return;" "/c/Janggok-ri-main/Assets/Scripts/Player/Throwable.cs"`
Expected: `InventorySystem.Instance.Has` 체크 블록 안에 `return;`이 최소 1회 존재 (없으면 아이템 없어도 소음이 발생하는 버그).

---

### Task 7: 전체 프로젝트 Unity 배치모드 컴파일 검증 (최종)

**Files:**
- (파일 생성/수정 없음 — 검증 전용 태스크)

**Interfaces:**
- Consumes: Task 1~6에서 만든 모든 파일.
- Produces: 없음 (이 계획의 마지막 태스크).

- [ ] **Step 1: 배치모드로 프로젝트를 열어 컴파일만 확인**

Run:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.9f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "C:\Janggok-ri-main" \
  -logFile "C:\Janggok-ri-main\compile_check.log"
```

이 명령은 몇 분 정도 걸릴 수 있다 (Library 캐시가 이미 있어 재임포트는 아님).

- [ ] **Step 2: 로그에서 컴파일 에러 검색**

Run: `grep -n "error CS" "/c/Janggok-ri-main/compile_check.log"`
Expected: 출력 없음 (매치되는 줄이 없어야 함 = 컴파일 에러 없음).

만약 에러가 나오면: 에러 메시지에 나온 파일/줄 번호를 확인해 해당 태스크로 돌아가 수정하고, 이 Step을 다시 실행한다 (다른 태스크를 건드리지 않는다).

- [ ] **Step 3: 로그 파일 정리**

Run: `rm "/c/Janggok-ri-main/compile_check.log"`

- [ ] **Step 4: 최종 완료 기준 재확인**

다음 4가지가 모두 되어 있는지 육안으로 확인 (Unity 에디터에서 실제 플레이 테스트는 팀장/팀원이 위 `docs/superpowers/plans/2026-07-16-distortion-editor-setup.md` 안내를 수행한 뒤 가능):
- [ ] 오염도에 비례해 안개·화면왜곡·비네트가 반응하는 코드 경로 존재 (`ContaminationEffectsDirector`)
- [ ] 오염 2단계에서 점프스케어 코루틴이 시작되는 코드 경로 존재 (`JumpscareDirector`)
- [ ] 엔딩 평가 후 로비 복귀 흐름 존재 (`EndingDirector`)
- [ ] 조개껍데기 투척 시 `EventBus.RaiseNoiseEmitted` 호출 확인 (`Throwable`, Task 6 Step 2에서 이미 확인됨)

---

## Self-Review 결과

**스펙 커버리지:** 기능1(안개/왜곡/비네트) → Task 2·3, 기능2(점프스케어) → Task 4, 기능3(엔딩) → Task 5, 기능4(투척) → Task 6. 필요한 사전 수정(IObjective, SetEnding) → Task 1. 전부 매핑됨, 누락 없음.

**플레이스홀더 스캔:** "TBD/나중에 구현" 형태의 문구 없음. `lobbySceneName`, `endingVisuals`, `fogParticles` 등 nullable 필드는 스펙에서 의도적으로 합의된 스킵 동작이며 실제 스킵 로직(if null return/continue)이 코드에 구현되어 있음.

**타입 일관성:** `_Distortion`(Task 2/3), `EventBus.OnContaminationChanged`/`OnContaminationStageChanged`/`RaiseNoiseEmitted`(EventBus.cs 원본과 대조 완료), `EndingType.Bad/Normal/True` 순서(EndingEvaluator.cs 원본과 대조 완료), `IObjective`/`SetEnding`(Task1→Task5 의존성 명시) 모두 태스크 간 일치.
