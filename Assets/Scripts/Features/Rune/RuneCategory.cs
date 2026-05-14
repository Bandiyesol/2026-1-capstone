public enum RuneCategory
{
    Movement, // 지속적으로 움직임을 변화 (Update 중심)
    Trigger,  // 충돌 시 즉시 발동 (Collision 중심)
    Final     // 파괴 시점에 발동
}