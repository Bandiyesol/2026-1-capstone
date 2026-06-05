using UnityEngine;
using System.Collections;

// 모래 가리개(화면 오버레이) 전역 관리 스크립트
public class SandOverlayController : MonoBehaviour
{
    // 어디서든 클래스 이름으로 쉽게 접근할 수 있는 싱글톤 인스턴스
    public static SandOverlayController Instance;

    [Header("모래 가리개 UI 오브젝트")]
    [SerializeField] GameObject overlayImage;

    // 현재 실행 중인 가리개 타이머 코루틴을 기억할 변수 (중복 실행 방지 및 시간 갱신용)
    private Coroutine overlayRoutine;

    void Awake()
    {
        // 자기 자신을 전역 인스턴스로 등록 (싱글톤)
        Instance = this;

        // 게임 시작 시 모래 가리개 UI를 안전하게 숨김 처리
        if (overlayImage != null)
            overlayImage.SetActive(false);
    }

    // ========================================================
    // [새로 추가] 모래바람 기믹들이 플레이어를 쳤을 때 호출할 핵심 메서드
    // ========================================================
    public void TriggerOverlay(float duration)
    {
        // 만약 이미 가리개가 켜져서 흐르는 중이라면, 기존 타이머를 취소시킴
        // (이유: 모래바람을 연속으로 맞았을 때, 이전 타이머 때문에 가리개가 도중에 갑자기 꺼지는 버그 방지)
        if (overlayRoutine != null)
        {
            StopCoroutine(overlayRoutine);
        }

        // 새로운 유지 시간을 적용하여 오버레이 코루틴 실행 및 변수에 저장
        overlayRoutine = StartCoroutine(ShowOverlayRoutine(duration));
    }

    // 지정된 시간(duration) 동안 UI를 켜고 대기한 뒤 꺼주는 내부 코루틴
    private IEnumerator ShowOverlayRoutine(float duration)
    {
        // 화면 가리개 UI 활성화
        if (overlayImage != null)
            overlayImage.SetActive(true);

        // 기믹이 전달해 준 유지 시간만큼 프레임을 대기
        yield return new WaitForSeconds(duration);

        // 시간이 다 지나면 화면 가리개 UI 비활성화
        if (overlayImage != null)
            overlayImage.SetActive(false);

        // 코루틴 종료 후 참조 변수 비화성화 초기화
        overlayRoutine = null;
    }

    // ========================================================
    // 기존 레거시 함수들 (기존 코드와의 호환성을 위해 유지)
    // ========================================================
    public void Show()
    {
        if (overlayImage != null)
            overlayImage.SetActive(true);
    }

    public void Hide()
    {
        // 수동으로 끌 때도 혹시 돌고 있을 코루틴을 안전하게 정리
        if (overlayRoutine != null)
        {
            StopCoroutine(overlayRoutine);
            overlayRoutine = null;
        }

        if (overlayImage != null)
            overlayImage.SetActive(false);
    }
}