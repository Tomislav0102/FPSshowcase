using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPlayer : HealthMain, ITakeDamage
{
    public override bool IsDead 
    { 
        get => base.IsDead; 
        set
        {
            base.IsDead = value;
            if (value) PlayerDead?.Invoke();
        }
    }
    [SerializeField] ParticleSystem psOnFire;


    protected override void Init()
    {
        base.Init();
        _gm.uiManager.ShowHitPoints(_hitPoints, _maxHitPoints);
    }
    public override void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        base.TakeDamage(elementType, damage, attackerTransform, damageOverTime);
        _gm.uiManager.ShowHitPoints(_hitPoints, _maxHitPoints);
        if  (damage > 0) _gm.uiManager.ShowPain();


        switch (elementType)
        {
            case ElementType.Fire:
                if (damageOverTime == null) return;

                if (damage == 0) psOnFire.Stop();
                else if (!psOnFire.isPlaying) psOnFire.Play();
                break;
        }

    }
}
