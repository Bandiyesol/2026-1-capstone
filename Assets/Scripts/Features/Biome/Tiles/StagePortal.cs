using UnityEngine;

public class StagePortal : MonoBehaviour
{
    [Header("플레이어 감지 태그")]
    [SerializeField] private string playerTag = "Player";

    WaveManager waveManager;
    bool isTriggered;

    void Awake()
    {
        waveManager = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered || !collision.CompareTag(playerTag))
            return;

        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);

        if (waveManager == null)
        {
            Debug.LogWarning("[StagePortal] WaveManager를 찾지 못했습니다.");
            return;
        }

        if (!waveManager.AwaitingStagePortal)
            return;

        isTriggered = true;
        waveManager.TryAdvanceStageViaPortal();
    }

    void OnEnable()
    {
        isTriggered = false;
    }

    void OnDisable()
    {
        isTriggered = false;
    }
}
