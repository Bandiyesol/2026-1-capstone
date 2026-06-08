using UnityEngine;

public class StagePortal : MonoBehaviour
{
    [Header("플레이어 감지 태그")]
    [SerializeField] private string playerTag = "Player";

    // 쿨타임이나 중복 실행 방지를 위한 플래그
    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered || !collision.CompareTag(playerTag)) return;

        if (StageManager.instance != null)
        {
            isTriggered = true;
            bool moved = StageManager.instance.NextStage();

            if (moved)
            {
                StageManager.instance.waveManager.StartStage();
            }

            // 작동 후 비활성화
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("씬에 StageManager 인스턴스가 존재하지 않습니다!");
        }
    }

    // 스테이지가 바뀔 때 마법진이 꺼졌다 켜지거나 재생성된다면 플래그를 리셋해줍니다.
    private void OnEnable()
    {
        ResetPortal();
    }
    public void ResetPortal()
    {
        isTriggered = false;
    }
}