public interface IThreatBehavior
{
	void Activate();					// 페이즈 시작 시 HauntController가 호출
	void Tick();						// 활성 중 매 프레임
	void Neutralize();				// 페이즈 종료/파훼 시 정리
	bool IsNeutralized { get; }
	void SetProgress(float t);		// 0~1 진행도(등대 층수 등). 안 쓰면 빈 구현
}