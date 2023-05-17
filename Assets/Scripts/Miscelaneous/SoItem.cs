using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;
using Sirenix.OdinInspector;

[CreateAssetMenu]
public class SoItem : ScriptableObject
{
    //general
    public string itemName;
    [ReadOnly] public int ordinalLookup;

    //weapons
    [EnumToggleButtons]
    [PropertySpace(SpaceAfter = 10, SpaceBefore = 10)]
    [HideLabel]
    [Title("Weapon type")]
    public WeaponMechanics weaponType;
    [HideIf("weaponType", WeaponMechanics.Melee)]
    [EnumToggleButtons]
    [HideLabel]
    [Title("Ammo type")]
    [PropertySpace(SpaceAfter = 10)]
    public AmmoType ammoType;

    [BoxGroup("Weapon specific")]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public bool has2ndAttack;
    [BoxGroup("Weapon specific")]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public bool animEventForShooting;
    [BoxGroup("Weapon specific")]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public bool canAim = true;
    [BoxGroup("Weapon specific")]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public bool partialReload; //used only for grenade gun
    [BoxGroup("Weapon specific")]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public bool hasCrosshair = true;
    [ShowIf("hasCrosshair", true)]
    [GUIColor(0.5f, 1f, 0f, 1f)]
    public float startSpread = 50f;

    [PropertySpace(SpaceBefore = 10)]
    [GUIColor(1f, 0f, 0f, 1f)]
    public Vector2Int damage;
    [ShowIf("weaponType", WeaponMechanics.BreathWeapon)]
    [HideLabel]
    [BoxGroup("Damage over time parameters")]
    [PropertySpace(SpaceAfter = 10)]
    [GUIColor(1f, 0f, 0f, 1f)]
    public DamageOverTime damageOverTime;
    public float range;
    public float rofModifier = 1f; 
    [ShowIf("weaponType", WeaponMechanics.Shotgun)]
    public int noOfPelletsPerShot = 1;


    [ShowIf("MeleeOrThrown", true)]
    public float areaOfEffect; 
    [HideIf("ammoType", AmmoType.None)]
    public int clipCapacity;

    [HideLabel]
    [BoxGroup("Weapon details")]
    [GUIColor(0.5f, 1f, 1f, 1f)]
    public WeaponDetail<bool> weaponDetail;

    bool MeleeOrThrown() //don't delete, it is being used
    {
        return weaponType == WeaponMechanics.Melee || weaponType == WeaponMechanics.Thrown;
    }

}
