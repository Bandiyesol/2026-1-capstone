using UnityEngine;

/// <summary>
/// 상자 Open 애니메이션 클립 마지막에 Animation Event로 연결하세요.
/// Function 이름: OnChestOpenFinished
/// </summary>
public class ChestAnimationEvent : MonoBehaviour
{
    DroppedChest chest;

    void Awake()
    {
        chest = GetComponentInParent<DroppedChest>();
    }

    public void OnChestOpenFinished()
    {
        if (chest != null)
            chest.NotifyOpenFinished();
    }
}
