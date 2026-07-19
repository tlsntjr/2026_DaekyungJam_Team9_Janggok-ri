using System;
using UnityEngine;

public static class EventBus
{
	// ===== 오염도 이벤트 =====
	public static event Action<float> OnContaminationChanged;				// 오염도 변동 발생 이벤트 (range 0~1)
	public static event Action<int> OnContaminationStageChanged;			// 오염도 단계 변동 발생 시 (1/2/3 단계)

	// ===== 구역 진행 =====
	public static event Action<string, int> OnHauntPhaseAdvanced;			// 괴담별 페이즈 진행도 (string -> 괴담 id, int -> 페이즈 인덱스)
	public static event Action<string> OnHauntCleared;                         // 괴담 최종 클리어 (string -> 괴담 id)

	// ===== AI 및 소음 =====
	public static event Action<string, int> OnThreatStateChanged;			// 플레이어 위험 상태 체크 (string -> 괴담 id, int -> 경계 레벨 0/1/2)
	public static event Action<Vector2, float> OnNoiseEmitted;				// 던지기, 뛰기, 발전기 발동 등 소음 발생 (Vector2 -> 발생 위치, float -> radius)
	public static event Action<Vector2> OnMonsterScreamed;					// 크리쳐 괴성 -> 인식할 범위 필요 없음

	// ===== 목표 진행 =====
	public static event Action<string> OnObjectiveFlagSet;						// 목표 달성을 위한 오브젝트 획득 (string -> 목표 id)

	// ===== 플레이어 사망 =====
	public static event Action OnPlayerDeath;										// 플레이어 사망


	/* 
	 * Raise 호출 선언부 
	 * 액션 호출은 하기 Raise문을 이용하여 호출해주세요 !!
	 */

	public static void RaiseContaminationChanged(float v)			=> OnContaminationChanged?.Invoke(v);
	public static void RaiseContaminationStageChanged(int s)		=> OnContaminationStageChanged?.Invoke(s);
	public static void RaiseHauntPhaseAdvanced(string id, int p)	=> OnHauntPhaseAdvanced?.Invoke(id, p);
	public static void RaiseHauntCleared(string id)						=> OnHauntCleared?.Invoke(id);
	public static void RaiseThreatStateChanged(string id, int s)		=> OnThreatStateChanged?.Invoke(id, s);
	public static void RaiseNoiseEmitted(Vector2 pos, float r)		=> OnNoiseEmitted?.Invoke(pos, r);
	public static void RaiseObjectiveFlagSet(string id)					=> OnObjectiveFlagSet?.Invoke(id);
	public static void RaisePlayerDeath()									=> OnPlayerDeath?.Invoke();
	public static void RaiseMonsterScreamed(Vector2 pos)			=> OnMonsterScreamed?.Invoke(pos);
}
