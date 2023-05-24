using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthEnemy : HealthMain, ITakeDamage
{
    public override bool IsDead
    {
        get => base.IsDead;
        set
        {
            base.IsDead = value;
            if (value) Die();
        }
    }
    Transform _myTransform;
    EnemyBehaviour _enemyBehaviour;

    protected override void Init()
    {
        _myTransform = transform;
        base.Init();
        _enemyBehaviour = GetComponent<EnemyBehaviour>();
    }
    public override void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        base.TakeDamage(elementType, damage, attackerTransform, damageOverTime);

        if (damage > 0)
        {
            _enemyBehaviour.PassFromHealth_Attacked(attackerTransform);
            _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString(), elementType);
        }

    }

    void Die()
    {
       _myTransform.parent.gameObject.SetActive(false);
    }
}
