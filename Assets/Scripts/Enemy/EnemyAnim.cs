using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;
using FirstCollection;

public class EnemyAnim : MonoBehaviour
{
    Animator _anim;
    EnemyBehaviour _enemyBehaviour;
    [SerializeField] EnemyWeaponUsed weaponAnimType;
    [field: SerializeField] Transform MyHead { get; set; }
    [SerializeField] SoItem weaponUsed;
    [SerializeField] GameObject muzzle;
    [SerializeField] Rig rigRightHandAiming;
    [SerializeField] Rig rigLeftHand;
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand; //needed for accuracy (together with '_spreadWeapon')
    Transform _aimIK;
    float _spreadWeapon = 0f;
    Vector3 _offsetTar;
    bool IkActive
    {
        get => _ikActive;
        set
        {
            _ikActive = isHit ? false : value;
            rigRightHandAiming.weight = _ikActive ? 1f : 0f;
            switch (weaponAnimType)
            {
                case EnemyWeaponUsed.Melee:
                    break;
                case EnemyWeaponUsed.Pistol:
                    rigLeftHand.weight = _ikActive ? 1f : 0f;
                    break;
                case EnemyWeaponUsed.Rifle:
                    break;
            }

        }
    }
    bool _ikActive;
    [HideInInspector] public bool isHit;


    public void Init(EnemyBehaviour enBeh, out Animator anim, out Transform myTran, out SoItem weapon, out GameObject weaponMuzzle)
    {
        _anim = GetComponent<Animator>();
        anim = _anim;
        _enemyBehaviour = enBeh;
        myTran = transform;
        _enemyBehaviour.GetComponent<IFactionTarget>().MyHead = MyHead;
        weapon = weaponUsed;
        weaponMuzzle = muzzle;
        
        _aimIK = multiAimConstraintRightHand.data.sourceObjects[0].transform;
        _anim.SetFloat("rof", weaponUsed.rofModifier);
    }

    public void AE_Attacking()
    {
        _enemyBehaviour.PassFromAE_Attacking();
    }

    public void Attack(bool isAttacking)
    {
        //   print(isAttacking);
        IkActive = isAttacking;
        _anim.SetBool("attack", isAttacking);
        if (weaponAnimType == EnemyWeaponUsed.Melee) return;
        _offsetTar = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));
    }
    public void SetAim(Vector3 pos)
    {
        _aimIK.position = pos;
        if (weaponAnimType == EnemyWeaponUsed.Melee) return;

        multiAimConstraintRightHand.data.offset =
            Vector3.Lerp(multiAimConstraintRightHand.data.offset, _offsetTar, 0.3f * Time.deltaTime);

    }
    public void SetSpeed(MoveType movetype)
    {
        _anim.SetInteger("movePhase", (int)movetype);
    }

}
