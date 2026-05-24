using UnityEngine;

// �ı� �� �� ���� ����
public class PoisonSpore : BiomeGimmick
{
    [Header("�� ���� Pool �ε���")]
    [SerializeField] int poisonFieldIndex = 2;

    // �̹� ��������
    bool exploded;

    Animator anim;

    protected override void Awake()
    {
        base.Awake();

        // ���� ���� ������Ʈ ĳ��
        anim = GetComponent<Animator>();
    }

    protected override void Update()
    {
        // �θ� Update ����
        base.Update();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // ���� �ʱ�ȭ
        exploded = false;

        // �ڶ�� ���� �浹 ��Ȱ��
        DisableCollider();
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // �÷��̾� ���� �� ����
        Explode();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // �̹� �������� �ߴ�
        if (exploded)
            return;

        // �� ���� �ý����� Motion�� ������ ����
        if (collision.GetComponent<Motion>() != null)
        {
            Explode();
        }
    }

    void Explode()
    {
        // �ߺ� ���� ����
        if (exploded)
            return;

        exploded = true;

        // �浹 ��Ȱ��
        if (coll != null)
            coll.enabled = false;

        // ������ �ִϸ��̼� ���
        if (anim != null)
            anim.SetTrigger("Explode");
    }

    // �ִϸ��̼� ������ �����ӿ��� ȣ��
    public void SpawnPoisonField()
    {
        // �� ���� ����
        GameObject field = GameManager.instance.pool.GetGimmick(poisonFieldIndex);

        // ���� ���ڿ� ���� �������� �θ�� ����
        field.transform.SetParent(transform.parent);

        // ��ġ ����
        field.transform.position = transform.position;

        // ���� ��Ȱ��
        gameObject.SetActive(false);
    }
}
