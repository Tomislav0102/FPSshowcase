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
    protected override bool OnFire 
    { 
        get => base.OnFire;
        set
        {
            base.OnFire = value;
            if (value)
            {
                if (_eRef.enemyBehaviour.EnState != EnemyState.Immobile) _eRef.anim.SetBool("onFire", true);
                _eRef.enemyBehaviour.psOnFire.Play();

            }
            else
            {
                _eRef.anim.SetBool("onFire", false);
                _eRef.enemyBehaviour.psOnFire.Stop();

            }
        }
    }
    RagdollBodyPart _lastBodyPart;
    ElementType _elType = ElementType.Normal;

    float _parDamage;
    float _timerAgro;
    [HideInInspector] public Transform attacker;

    protected override void Init()
    {
        base.Init();
        _eRef = GetComponent<EnemyRef>();
        _floatingDamageWhileBurning = true;
    }
    public void PassFromBodyPart(RagdollBodyPart ragdoll, ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        _lastBodyPart = ragdoll;
       // print($"{_lastBodyPart.name} hit by {attackerTransform.name}");
        TakeDamage(elementType, damage, attackerTransform, damageOverTime);

        if (!IsDead &&
            Random.value < damage / _maxHitPoints && 
            !OnFire &&
            elementType == ElementType.Normal &&
            _eRef.enemyBehaviour.EnState != EnemyState.Immobile)
        {
            _eRef.enemyBehaviour.ragToAnimTransition.RagdollMe(ragdoll.GetComponent<Rigidbody>(), attackerTransform);
            _eRef.anim.ResetTrigger("hit");
        }
    }
    public override void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        base.TakeDamage(elementType, damage, attackerTransform, damageOverTime);
        attacker = attackerTransform;
        _elType = elementType;

        if (damage > 0)
        {
            switch (_elType)
            {
                case ElementType.Fire:
                    break;
                default:
                    if (!_eRef.enemyBehaviour.isHit && _eRef.enemyBehaviour.EnState != EnemyState.Immobile)
                    {
                        _eRef.anim.SetTrigger("hit");
                        _eRef.anim.SetLayerWeight(1, 1f);
                    }
                    break;
            }
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

    //[SerializeField] SkinnedMeshRenderer[] skins;
    //[SerializeField] Material[] standardMats;
    //[SerializeField] Material[] explosionMats;
    void Die()
    {
        //print(_attackerTr);
        Dead?.Invoke();
        if(_lastBodyPart != null) StartCoroutine(_lastBodyPart.PushbackDeath());
        _eRef.enemyBehaviour.psOnFire.Stop();

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
    }
}
