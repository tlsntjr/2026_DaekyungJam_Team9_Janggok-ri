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
6. `ContaminationEffectsDirector` 컴포넌트(Assets/Scripts/Effects)를 아무 매니저
   오브젝트에 붙이고, `Distortion Material`에 1번 머티리얼, `Volume`에 5번
   오브젝트를 드래그해서 연결.
7. Play 모드에서 오염도가 오르면(디버그로 `ContaminationSystem.Instance.Add(0.1f)`
   호출해봐도 됨) 화면이 흔들리고 Vignette가 진해지는지 확인.
