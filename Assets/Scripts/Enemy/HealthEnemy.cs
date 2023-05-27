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

    ElementType _elType = ElementType.Normal;
    [SerializeField] SkinnedMeshRenderer[] skins;
    [SerializeField] Material[] standardMats;
    [SerializeField] Material[] explosionMats;

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
            _elType = elementType;
            _enemyBehaviour.PassFromHealth_Attacked(attackerTransform);
            _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString(), elementType);
        }

    }

    void Die()
    {
        switch (_elType)
        {
            case ElementType.Fire:
                break;
            case ElementType.Explosion:
                for (int i = 0; i < skins.Length; i++)
                {
                    skins[i].material = explosionMats[i];
                }
                break;
        }
        _enemyBehaviour.IsActive = false;
    }
}
