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
    [SerializeField] Rig rigAiming;
    [SerializeField] Rig rigLeftHand;
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand;
    Transform _aimIK;
    float _spreadWeapon = 50f;
    Vector3 _offsetTar;

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
        _anim.SetBool("attack", isAttacking);
        rigAiming.weight = isAttacking ? 1 : 0;
        _offsetTar = new Vector3(Random.Range(-_spreadWeapon, _spreadWeapon), 0f, Random.Range(-_spreadWeapon, _spreadWeapon));

        switch (weaponAnimType)
        {
            case EnemyWeaponUsed.Pistol:
                rigLeftHand.weight = isAttacking ? 1 : 0;
                break;
        }
    }

    public void SetAim(Vector3 pos)
    {
        _aimIK.position = pos;
        multiAimConstraintRightHand.data.offset =
            Vector3.Lerp(multiAimConstraintRightHand.data.offset, _offsetTar, 0.3f *Time.deltaTime);

    }
    public void SetSpeed(MoveType movetype)
    {
        _anim.SetInteger("movePhase", (int)movetype);
    }

}
