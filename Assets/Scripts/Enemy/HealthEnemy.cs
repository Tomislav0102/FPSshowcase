using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthEnemy : HealthMain
{
    public System.Action Dead;
    EnemyRef _eRef;
    public override bool IsDead
    {
        get => base.IsDead;
        set
        {
            base.IsDead = value;
            if (value) Die();
        }
    }
    RagdollBodyPart _lastBodyPart;
    Transform _attackerTr;
    ElementType _elType = ElementType.Normal;
    //[SerializeField] SkinnedMeshRenderer[] skins;
    //[SerializeField] Material[] standardMats;
    //[SerializeField] Material[] explosionMats;

    float _parDamage;
    float _timerAgro;

    protected override void Init()
    {
        base.Init();
        _eRef = GetComponent<EnemyRef>();
    }
    public void PassFromBodyPart(RagdollBodyPart ragdoll, ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        _lastBodyPart = ragdoll;
        TakeDamage(elementType, damage, attackerTransform, damageOverTime);
    }
    public override void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        _attackerTr = attackerTransform;
        base.TakeDamage(elementType, damage, attackerTransform, damageOverTime);

        if (damage > 0)
        {
            _elType = elementType;
            _eRef.anim.SetTrigger("hit");
            _eRef.anim.SetLayerWeight(1, 1f);
            _eRef.enemyBehaviour.PassFromHealth_Attacked(attackerTransform, CanSwitchAgro());
        }

        bool CanSwitchAgro() 
        {
            _parDamage = damage / _maxHitPoints;
            if (_timerAgro == 0f && Random.value < _parDamage)
            {
                StartCoroutine(AgroCooldown());
                return true;
            }

            return false;
        }
    }


    IEnumerator AgroCooldown()
    {
        while(_timerAgro < 10f)
        {
            _timerAgro += Time.deltaTime;
            yield return null;
        }
        _timerAgro = 0f;
    }

    void Die()
    {
        //print(_attackerTr);
        _lastBodyPart.attacker = _attackerTr;
        Dead?.Invoke();

        switch (_elType)
        {
            case ElementType.Fire:
                break;
            case ElementType.Explosion:
                //for (int i = 0; i < skins.Length; i++)
                //{
                //    skins[i].material = explosionMats[i];
                //}
                break;
            default:
              //  _lastBodyPart.attacker = _attackerTr;
                break;
        }
      //  _enemyBehaviour.IsActive = false;
    }
}
