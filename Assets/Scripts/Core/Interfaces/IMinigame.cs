public interface IMinigame
{
    void StartOrResume();
    void Interrupt();       // 진행 상태 보존한 채 멈춤 - 0으로 리셋되면 안 됨
    bool IsComplete { get; }
    int SuccessCount { get; }
}