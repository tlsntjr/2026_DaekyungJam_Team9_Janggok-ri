using System;
public interface IObjective
{
    /// <summary>
    /// 플래그 체크
    /// </summary>
    /// <param name="id">플래그 id</param>
    /// <returns></returns>
    bool HasFlag(string id);

    /// <summary>
    /// 플래그 지정
    /// </summary>
    /// <param name="id">플래그 id</param>
    void SetFlag(string id);
    event Action<string> OnFlagChanged;
}
