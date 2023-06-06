using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollBodyPart : MonoBehaviour, ITakeDamage, IMaterial
{
    GameManager _gm;
    EnemyRef _eRef;
    [SerializeField] FirstCollection.BodyPart bodyPart;
    [SerializeField] bool usedForExplosions;
    public bool IsDead { get; set; }
    public MatType MaterialType { get ; set; }

    Transform _myTransform;
    Rigidbody _rigid;
    Collider _collider;
    [HideInInspector] public Transform attacker;


    void Awake()
    {
        _gm = GameManager.Instance;
        MaterialType = MatType.Blood;
        _myTransform = transform;
        _rigid = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

    }
    void OnEnable()
    {
        IsDead = false;
        attacker = null;
    }
    //void OnDisable()
    //{
    //    _eRef.enemyHealth.Dead -= Dead;
    //}
    void Dead()
    {
        _rigid.isKinematic = false;
        _collider.isTrigger = false;
        if (attacker != null) StartCoroutine(PushbackDeath());
        IsDead = true;
    }
    IEnumerator PushbackDeath()
    {
        yield return null;
        Vector3 dir = (-attacker.position + _myTransform.position).normalized;
        _rigid.AddForce(50f * dir, ForceMode.VelocityChange);

    }
    public void InitializeMe(EnemyRef eRef)
    {
        _eRef = eRef;
        _eRef.enemyHealth.Dead += Dead;
        _rigid.isKinematic = true;
        _collider.isTrigger = false;
    }
    public void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        if (IsDead)
        {
            attacker = attackerTransform;
            StartCoroutine(PushbackDeath());
        }
        else if (damage > 0)
        {
            switch (elementType)
            {
                case ElementType.Normal:
                    break;
                case ElementType.Fire:
                    break;
                case ElementType.Explosion:
                    if (!usedForExplosions) return;
                    break;
                case ElementType.Cold:
                    break;
                case ElementType.Electricity:
                    break;
                case ElementType.Poison:
                    break;
            }
            _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString(), elementType);
        }

        _eRef.enemyHealth.PassFromBodyPart(this, elementType, damage, attackerTransform, damageOverTime);
    }
}
