public interface ISaveModel
{
	void SaveProgress();			//  현재 진행도 저장
	void RestoreOnDeath();		// 사망 시 무엇을 되돌릴지
	string GetCheckpoint();		// 현재 복귀 지점 식별자
}