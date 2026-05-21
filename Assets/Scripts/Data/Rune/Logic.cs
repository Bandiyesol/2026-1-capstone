using UnityEngine;


[CreateAssetMenu(fileName = "LogicRune", menuName = "RuneData/Logic")]
public class LogicRuneData : RuneData {
    public float interval;         // 워프 주기 (n초 마다)
    public float distance;         // 워프 거리 (m 거리)
}