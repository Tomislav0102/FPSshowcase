using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SoItem : ScriptableObject
{
    //general
    public string itemName;
    [HideInInspector] public int ordinalLookup;

    //weapons
    public WeaponMechanics weaponType;
    public AmmoType ammoType;

    public bool has2ndAttack;
    public bool animEventForShooting;
    public bool canAim = true;
    public bool partialReload; //used only for grenade gun
    public bool hasCrosshair = true;
    public float startSpread = 50f;

    public Vector2Int damage;
    public DamageOverTime damageOverTime;
    public float range;
    public float rofModifier = 1f; 
    public int noOfPelletsPerShot = 1;

    public float areaOfEffect; 
    public int clipCapacity;

    public WeaponDetail<bool> weaponDetail;

    public EnemyWeaponUsed enemyWeaponUsed;

    // bool MeleeOrThrown() //don't delete, it is being used
    // {
    //     return weaponType == WeaponMechanics.Melee || weaponType == WeaponMechanics.Thrown;
    // }

}
