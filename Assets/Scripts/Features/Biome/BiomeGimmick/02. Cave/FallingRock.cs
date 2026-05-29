using UnityEngine;
using System.Collections;

// 낙석 오브젝트 관리 스크립트
public class FallingRock : BiomeGimmick
{
    [Header("낙석 데미지")]
    [SerializeField] float fallDamage = 50f;

    [Header("경고 시간")]
    [SerializeField] float warningTime = 1f;

    [Header("경고 표시")]
    [SerializeField] GameObject warningCircle;

    [Header("낙석 게임오브젝트")]
    [SerializeField] GameObject stone;

    [Header("낙석 비활성화 시간")]
    [SerializeField] float deactivateTime = 3f;

    // 낙석 애니메이터
    Animator anim;

    // 낙석 콜라이더
    Collider2D stoneColl;

    // 이미 충돌했는지
    bool hit;

    protected override void Awake()
    {
        // 부모 공통 초기화
        base.Awake();

        // stone 하위 컴포넌트 캐시
        if (stone != null)
        {
            anim = stone.GetComponent<Animator>();
            stoneColl = stone.GetComponent<Collider2D>();
        }
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnSpawn()
    {
        StopAllCoroutines();

        hit = false;

        // 부모 콜라이더는 비활성화
        DisableCollider();

        // 낙석 콜라이더 비활성화
        if (stoneColl != null)
            stoneColl.enabled = false;

        // 경고 원 표시
        if (warningCircle != null)
            warningCircle.SetActive(true);

        // 낙석 숨김
        if (stone != null)
            stone.SetActive(false);

        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        // 경고 시간 대기
        yield return new WaitForSeconds(warningTime);

        // 경고 원 숨김
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 낙석 표시
        if (stone != null)
            stone.SetActive(true);

        // 애니메이터 초기화 및 애니메이션 재생
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.Play("Falling Stone", 0, 0f);
        }

        // 콜라이더 활성화
        if (stoneColl != null)
        {
            stoneColl.enabled = true;
        }
        else
        {
            Debug.LogError("stoneColl이 NULL입니다!");
        }
    }

    public void OnStoneCollision(Collider2D collision)
    {
        // 이미 충돌했으면 무시
        if (hit)
            return;

        // 플레이어만 처리
        if (!collision.CompareTag("Player"))
            return;

        Player player = collision.GetComponent<Player>();

        if (player == null)
            return;

        hit = true;

        // [방어 시스템 연동] 즉사 대신 플레이어의 스탯 방어 처리를 거친 유효 낙석 데미지 차감
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(fallDamage);
        }
        else
        {
            GameManager.instance.Health -= fallDamage;
        }

        // 콜라이더 비활성화
        if (stoneColl != null)
            stoneColl.enabled = false;
    }

    // 부모 호출 함수 구현
    protected override void OnPlayerTrigger(Player player) { }

    // 애니메이션 이벤트 콜백: 애니메이션 종료 후 호출
    public void EndFall()
    {
        // 낙석 콜라이더 비활성화
        if (stoneColl != null)
            stoneColl.enabled = false;

        // 일정 시간 후 낙석 오브젝트 비활성화
        StartCoroutine(DeactivateStoneAfterDelay());
    }

    IEnumerator DeactivateStoneAfterDelay()
    {
        yield return new WaitForSeconds(deactivateTime);

        // 부모 오브젝트 전체 비활성
        gameObject.SetActive(false);
    }
}