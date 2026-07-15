using System;
using UnityEngine;

/// <summary>
/// 타이머 기능이 필요한 경우, 사용할 수 있는 타이머 클래스
/// </summary>
public class DeadlineTimer : MonoBehaviour
{
	public event Action<float> OnTick;	// 남은 시간
	public event Action OnExpired;		// 시간 만료

	private float remaining;
	private bool running;

	public bool IsRunning	=> running;
	public float Remaining	=> remaining;

	/// <summary>
	/// 타이머 시작하는 함수
	/// </summary>
	/// <param name="duration">타이머 지속 시간</param>
	public void StartTimer(float duration)
	{
		remaining = duration;
		running = true;
	}

	/// <summary>
	/// 기믹 성공 후 타이머 중단
	/// </summary>
	public void Cancel() => running = false;

	void Update()
	{
		if (!running) return;

		remaining -= Time.deltaTime;
		OnTick?.Invoke(remaining);

		if (remaining <= 0f)
		{
			running = false;
			OnExpired?.Invoke();
		}
	}
}