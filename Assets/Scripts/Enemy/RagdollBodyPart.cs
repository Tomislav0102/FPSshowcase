using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollBodyPart : MonoBehaviour, ITakeDamage, IMaterial
{
    GameManager _gm;
    [SerializeField] BodyPartRagdoll bodyPart;
    public bool IsDead { get; set; }
    public EnemyRef EnRef { get; set; }
    public MatType MaterialType { get ; set; }

    Transform _myTransform;
    Rigidbody _rigid;
    Collider _collider;


    void Awake()
    {
        _gm = GameManager.Instance;
        MaterialType = MatType.Blood;

    }
    void OnEnable()
    {
        IsDead = false;
    }
    void OnDisable()
    {
        EnRef.enemyHealth.Dead -= Dead;
    }
    void Dead()
    {
        _rigid.isKinematic = false;
        _collider.isTrigger = false;
        IsDead = true;
    }
    public IEnumerator PushbackDeath() 
    {
        yield return null;
        if (EnRef.enemyHealth.attacker == null) yield break;
        Vector3 dir = (_myTransform.position - EnRef.enemyHealth.attacker.position).normalized;
        _rigid.AddForce(50f * dir, ForceMode.VelocityChange);
    }
    public void InitializeMe(EnemyRef eRef)
    {
        EnRef = eRef;
        _myTransform = transform;
        _rigid = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        EnRef.enemyHealth.Dead += Dead;
        _rigid.isKinematic = true;
        _collider.isTrigger = false;
    }
    public void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        if (IsDead)
        {
            StartCoroutine(PushbackDeath());
            return;
        }

        if (damage > 0)
        {
            _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString(), elementType);
        }

        EnRef.enemyHealth.PassFromBodyPart(this, elementType, damage, attackerTransform, damageOverTime);
    }
}
