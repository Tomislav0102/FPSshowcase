using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPlayer : HealthMain, ITakeDamage
{
    public static System.Action<int> HealSyringe;
    public override bool IsDead 
    { 
        get => base.IsDead; 
        set
        {
            base.IsDead = value;
            if (value) PlayerDead?.Invoke();
        }
    }
    public EnemyRef EnRef { get; set; }
    [SerializeField] ParticleSystem psOnFire;


    protected override void Init()
    {
        base.Init();
        _gm.uiManager.ShowHitPoints(_hitPoints, _maxHitPoints);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        HealSyringe += Heal;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        HealSyringe -= Heal;
    }
    void Heal(int healAmmount)
    {
        TakeDamage(ElementType.Normal, -healAmmount, transform, null);
        _gm.player.offense.HealMethod(GenPhasePos.End);
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
