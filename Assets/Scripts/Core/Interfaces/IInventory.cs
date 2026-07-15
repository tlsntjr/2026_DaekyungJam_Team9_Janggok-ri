using System;

/// <summary>
/// 인벤토리 구현체
/// </summary>
public interface IInventory
{
    /// <summary>
    /// 아이템 소유 여부
    /// </summary>
    /// <param name="itemId">아이템 id</param>
    /// <returns></returns>
    bool Has(string itemId);

    /// <summary>
    /// 아이템 추가
    /// </summary>
    /// <param name="itemId">아이템 id</param>
    void Add(string itemId);

    /// <summary>
    /// 아이템 사용 혹은 제거
    /// </summary>
    /// <param name="itemId">아이템 id</param>
    void Remove(string itemId);

    event Action OnChanged;
}