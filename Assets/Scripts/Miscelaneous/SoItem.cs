using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu]
public class SoItem : ScriptableObject
{
    //general
    public string itemName;
    [HideInInspector] public int ordinalLookup;

    //weapons
    public WeaponMechanics weaponType;
    [HideIf("weaponType", WeaponMechanics.Melee)]
    public AmmoType ammoType;
    [Space]
    public bool has2ndAttack;
    public bool animEventForShooting;
    public bool canAim = true;
    public bool partialReload; //used only for grenade gun
    public bool hasCrosshair = true;
    [ShowIf("hasCrosshair")]
    public float startSpread = 50f;
    [Space]
    public Vector2Int damage;
    public DamageOverTime damageOverTime;
    public float range;
    public float rofModifier = 1f;
    [ShowIf("weaponType", WeaponMechanics.Shotgun)]
    public int noOfPelletsPerShot = 1;

    [ShowIf("MeleeOrThrown")]
    public float areaOfEffect;
    [HideIf("weaponType", WeaponMechanics.Melee)]
    public int clipCapacity;
    [Space]
    [HideLabel]
    public WeaponDetail<bool> weaponDetail;
    [Space]
    public EnemyWeaponUsed enemyWeaponUsed;

    bool MeleeOrThrown() //don't delete, it is being used
    {
        return weaponType == WeaponMechanics.Melee || weaponType == WeaponMechanics.Thrown;
    }

}
