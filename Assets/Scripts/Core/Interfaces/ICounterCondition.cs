public interface ICounterCondition
{
    bool IsSatisfied { get; }       // HauntController가 매 프레임 폴링
}