using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    void Start()
    {
        // 애니메이션 길이만큼 재생 후 자동 삭제
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            float length = anim.GetCurrentAnimatorStateInfo(0).length;
            Destroy(gameObject, length);
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }
}