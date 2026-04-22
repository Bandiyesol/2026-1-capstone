using UnityEngine;
// 모든 룬 동작 컴포넌트가 구현해야 하는 공통 인터페이스
// Bullet이 룬 종류에 관계없이 동일하게 Apply()를 호출할 수 있도록 함
public interface IBulletRune
{
    // 룬 데이터 주입 (발사 시점에 Weapon이 호출)
    void Init(RuneData data);

    // 매 Update에서 호출 — 이동 계산, 유도 등 지속 효과
    void OnUpdate(BulletRune bullet, float delta);

    // 적 충돌 시 호출 — 분열, 연쇄, 폭발 등 충돌 효과
    // 반환값: true → 탄환을 계속 살림 / false → 기본 소멸 처리 진행
    bool OnHit(BulletRune bullet, UnityEngine.Collider2D enemy);

    // 탄환 소멸 직전 호출 — Recursion 등 소멸 트리거 효과
    void OnExpire(BulletRune bullet);
}
