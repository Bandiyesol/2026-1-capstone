using UnityEngine;

// 바다 바이옴 - 수중 저항
public class UnderwaterResistance : BiomeEffect
{
    [Header("이동속도 감소 배율")]
    [SerializeField] float moveSpeedMultiplier = 0.7f;

    [Header("특정 악세사리 장착 여부")]
    [SerializeField] bool hasAccessory;

    float originSpeed;

    protected override void ApplyEffect()
    {
        if (player == null)
            return;

        // 원래 이동속도 저장
        originSpeed = player.speed;

        // 이동속도 감소
        player.speed *= moveSpeedMultiplier;

        // TODO:
        // 특정 악세사리 장착 시 공격력 2배
        // 무기 담당 팀원과 연동 예정
        if (hasAccessory)
        {
        }
    }

    protected override void RemoveEffect()
    {
        if (player == null)
            return;

        // 이동속도 원복
        player.speed = originSpeed;

        // TODO:
        // 공격력 원복
        // 무기 담당 팀원과 연동 예정
        if (hasAccessory)
        {
        }
    }
}
