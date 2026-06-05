using UnityEngine;

public class GiantRockAnimationEvent : MonoBehaviour
{
    GiantRock giantRock;

    void Awake()
    {
        giantRock = GetComponentInParent<GiantRock>();
    }

    // Animation Event용
    public void EndFall()
    {
        if (giantRock != null)
            giantRock.EndFall();
    }
    public void EnableBulletDamage()
    {
        if (giantRock != null)
            giantRock.EnableBulletDamage();
    }
}