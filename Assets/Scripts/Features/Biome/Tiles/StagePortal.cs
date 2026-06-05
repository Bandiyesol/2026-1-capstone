using UnityEngine;

public class StagePortal : MonoBehaviour
{
    [Header("플레이어 감지 태그")]
    [SerializeField] private string playerTag = "Player";

    // 쿨타임이나 중복 실행 방지를 위한 플래그
    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 3D 프로젝트라면 Collider2D -> Collider, OnTriggerEnter2D -> OnTriggerEnter로 변경하세요.

        // 이미 트리거되었거나 플레이어가 아니라면 무시
        if (isTriggered || !collision.CompareTag(playerTag)) return;

        if (StageManager.instance != null)
        {
            isTriggered = true; // 중복 호출 방지

            // 다음 스테이지로 이동 시도
            bool hasNext = StageManager.instance.NextStage();

            // 만약 다음 스테이지가 성공적으로 켜졌다면, 마법진 플래그 초기화
            // (스테이지 이동 후 플레이어 위치가 리셋되므로 다시 마법진을 밟을 때를 대비)
            if (hasNext)
            {
                isTriggered = false;
            }
        }
        else
        {
            Debug.LogWarning("씬에 StageManager 인스턴스가 존재하지 않습니다!");
        }
    }

    // 스테이지가 바뀔 때 마법진이 꺼졌다 켜지거나 재생성된다면 플래그를 리셋해줍니다.
    private void OnEnable()
    {
        isTriggered = false;
    }
}