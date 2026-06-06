using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 라바 티라노 골렘 - 부모 코어 시스템 (전체 유닛 추적, 전멸 판정 및 최종 보상 포탈 스폰 관리)
/// </summary>
public class LavaTyranoCore : MonoBehaviour
{
    // 현재 필드상에서 활동 중인 라바 티라노의 모든 유닛(분열체 포함)들을 실시간 담아두는 중앙 저장소
    public List<LavaTyranoUnit> units = new List<LavaTyranoUnit>();

    // 생성된 용암 유도탄 추적
    public List<GameObject> activeBullets = new List<GameObject>();

    // 포탈 스폰의 기준점이 될 '가장 마지막에 숨진 분열 개체의 월드 좌표' 저장용
    public Vector2 lastDeathPosition;

    bool portalSpawned; // 클리어 포탈이 중복 스폰되는 현상을 원천 방지하기 위한 플래그

    // 신생 유닛 리스트 등록 가동
    public void RegisterUnit(LavaTyranoUnit unit)
    {
        if (!units.Contains(unit))
            units.Add(unit);
    }
    // 유도탄 등록
    public void RegisterBullet(GameObject bullet)
    {
        if (bullet != null &&
            !activeBullets.Contains(bullet))
        {
            activeBullets.Add(bullet);
        }
    }

    // 유닛 소멸 및 전멸 추적 신호 수신
    public void UnregisterUnit(LavaTyranoUnit unit)
    {
        units.Remove(unit); // 리스트에서 제거

        // 마지막 유닛이 죽은 절대 좌표를 실시간 덮어쓰기 백업
        lastDeathPosition = unit.transform.position;

        CheckAllDead(); // 리스트 상태 기반 최종 전멸 체크
    }

    // ==========================================
    // 필드 내 보스 객체 완전 소멸 여부 확인
    // ==========================================
    void CheckAllDead()
    {
        // 1차 안전 장치: 예기치 못하게 미제거된 널(null) 데이터 정리
        units.RemoveAll(u => u == null);

        // 추적 중인 리스트 카운트가 0이고 포탈이 아직 안 열렸다면 보스 레이드 완전 클리어 판정
        if (units.Count == 0 && !portalSpawned)
        {
            portalSpawned = true;

            ClearAllBullets();

            SpawnPortal();
        }
    }

    // 마지막 조각이 쓰러진 자리에 다음 방 이동 포탈 생성
    void SpawnPortal()
    {
        if (PoolManager.Instance == null)
            return;

        // 오브젝트 풀에서 환경 기믹 아이템(포탈 프리팹) 추출
        GameObject portal =
            PoolManager.Instance.GetGimmick(13);

        if (portal == null)
            return;

        // 최종 사망 위치에 포탈을 정확히 안착시키고 활성화
        portal.transform.position = lastDeathPosition;
    }

    // 모든 유도탄 제거
    void ClearAllBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            GameObject bullet = activeBullets[i];

            if (bullet != null)
                bullet.SetActive(false);
        }

        activeBullets.Clear();
    }
}