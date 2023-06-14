using FirstCollection;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthMain : GlobalEventManager
{
    internal GameManager _gm;

    [ReadOnly][ShowInInspector] internal int _hitPoints;
    [SerializeField] internal int _maxHitPoints = 100;
    [SerializeField] internal int _startingPoints = 100;

    //fire
    float _timerFire;
    float _fireDuration;
    DamageOverTime _currentDot;
    Coroutine _coroutine; //maybe it will have to be stopped (e.g. entering water)
    protected virtual bool OnFire
    {
        get => _onFire;
        set
        {
            _onFire = value;
            if (value) _timerFire = 0;
            else
            {
                _fireDuration = 0f;
                _currentDot = null;
            }
        }
    }
    bool _onFire;
    const float CONST_TICKFIRE = 0.5f;

    public virtual bool IsDead { get; set; }
    internal bool _floatingDamageWhileBurning;

    private void Awake()
    {
        Init();
    }
    protected virtual void Init()
    {
        _gm = GameManager.Instance;
        if (_startingPoints > 0 && _startingPoints > _maxHitPoints) _maxHitPoints = _startingPoints;
        TakeDamage(ElementType.Normal, -_startingPoints, null, null);
    }

    public virtual void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        if (IsDead) return;

        _hitPoints -= damage;
        if (_hitPoints > _maxHitPoints) _hitPoints = _maxHitPoints;
        else if (_hitPoints <= 0)
        {
            IsDead = true;
            return;
        }

        if (damageOverTime != null && _currentDot != damageOverTime)
        {
            _currentDot = damageOverTime;
            _fireDuration = Mathf.Max(_fireDuration, _currentDot.effectDuration);
            if (!OnFire) _coroutine = StartCoroutine(BurningFire());
        }
    }


    IEnumerator BurningFire()
    {
        OnFire = true;
        WaitForSeconds w = HelperScript.GetWait(CONST_TICKFIRE);
        while (_timerFire < _fireDuration)
        {
            if (IsDead) break;

            _timerFire +=Time.deltaTime;
            yield return w;
            _fireDuration -= CONST_TICKFIRE;
            TakeDamage(ElementType.Fire, _currentDot.damagePerTick, null, _currentDot);
            if(_floatingDamageWhileBurning) _gm.poolManager.GetFloatingDamage(transform.position, _currentDot.damagePerTick.ToString(), ElementType.Fire);
        }
        TakeDamage(ElementType.Fire, 0, null, _currentDot);
        OnFire = false;
    }
}
