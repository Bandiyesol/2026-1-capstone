using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // 한 번 충돌할 때 적에게 줄 피해량
    public float damage;
    // 관통 가능 횟수(감소하다가 -1이 되면 비활성화)
    public int per;
    // 근접 무기 판정 여부(칼/근접 타격 등)
    public bool isMelee;
    // 원거리 투사체가 살아있는 최대 시간(초)
    float lifetime = 2f;
    // 활성화 이후 경과 시간 누적용
    float timer;

    // 물리 이동 제어용 리지드바디
    Rigidbody2D rigid;
    // 프리팹의 기본 스케일(재사용 시 스케일 복원에 사용)
    Vector3 defaultScale;

    void Awake()
    {
        // 컴포넌트 캐싱: 매번 GetComponent 호출하지 않도록 한 번만 가져옴
        rigid = GetComponent<Rigidbody2D>();
        // 최초 생성 시점의 원본 스케일 저장
        defaultScale = transform.localScale;
    }
    // 탄환/근접 오브젝트 공통 초기화 진입점
    // damage: 피해량, per: 관통 횟수, dir: 진행 방향,
    // weaponCount: 강화/중첩 수치, isCustomScale: 스케일 보정 사용 여부
    public void Init(float damage, int per, Vector2 dir, int weaponCount, bool isCustomScale = false)
    {
        // 전달받은 전투 스탯 저장
        this.damage = damage;
        this.per = per;
        // per <= -1인 경우 근접 무기로 간주
        isMelee = per <= -1;

        // 풀 재사용 시 누적 스케일이 남지 않도록 기본값으로 복원
        transform.localScale = defaultScale;

        // 명시적으로 요청된 경우에만 무기 수치에 비례해 크기 증가
        if (isCustomScale)
        {
            float scaleMultiplier = 1.0f + (weaponCount * 0.2f);
            transform.localScale = defaultScale * scaleMultiplier;
        }

        // 근접 무기이거나 물리 바디가 없으면 투사체 속도/회전 설정은 생략
        if (isMelee || rigid == null)
            return;

        // 방향 벡터가 0에 가까우면 기본값(+X) 사용
        Vector2 n = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.right;
        // 일정 속도로 발사
        rigid.linearVelocity = n * 15f;
        // 스프라이트의 전방(+X)을 비행 방향과 일치시켜 시각적으로 정렬
        float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 적이 아니거나(벽/플레이어 등), 이미 소멸 상태면 무시
        if (!collision.CompareTag("Enemy") || per == -1)
            return;

        // 적과 충돌할 때마다 관통 횟수 차감
        per--;

        // 관통 횟수를 모두 소진하면 이동 정지 후 비활성화(오브젝트 풀 반납)
        if (per == -1)
        {
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        // 리지드바디 없는 근접 무기는 짧은 애니메이션성 히트박스로 사용
        if (isMelee && rigid == null) 
        {
            // 휘두르기 지속시간 이후 자동 비활성화
            Invoke("AutoDisable", 0.5f); 
        }

        // 재활성화될 때 생존 타이머 초기화
        timer = 0f;
    }

    void AutoDisable()
    {
        // 근접 히트박스 수명 종료 처리
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        // 풀에 반환될 때 스케일 흔적 제거(다음 사용 시 Init에서 다시 설정)
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        // 근접 무기는 개별 수명(Invoke/애니메이션)으로 관리하므로 여기서 제외
        if (isMelee) return; 

        // 원거리 투사체는 일정 시간이 지나면 자동 반납
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }
}
