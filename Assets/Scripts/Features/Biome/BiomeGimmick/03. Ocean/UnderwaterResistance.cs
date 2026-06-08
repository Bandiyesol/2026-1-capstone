using UnityEngine;

// 바다 바이옴 - 수중 저항 (물속에서 이동속도를 감소시키는 효과)
public class UnderwaterResistance : BiomeEffect
{
    [Header("이동속도 감소 배율")]
    [SerializeField] float moveSpeedMultiplier = 0.7f;

    [Header("특정 악세사리 장착 여부")]
    [SerializeField] bool hasAccessory;

    protected override void ApplyEffect()
    {
        if (player == null)
            return;

        // [에러 해결] player.speed 직접 변조 대신, 플레이어의 실시간 이동 속도 배율에 곱 연산 적용
        player.moveSpeedMultiplier *= moveSpeedMultiplier;

        // TODO: 특정 악세사리 장착 시 공격력 2배 (무기 담당 팀원과 연동 예정)
        if (hasAccessory)
        {
        }
    }

    protected override void RemoveEffect()
    {
        if (player == null)
            return;

        // [에러 해결] 효과 해제 시 기존 배율을 되돌리기 위해 원복 나눗셈 연산 처리
        player.moveSpeedMultiplier /= moveSpeedMultiplier;

        // TODO: 공격력 원복 (무기 담당 팀원과 연동 예정)
        if (hasAccessory)
        {
        }
    }
}