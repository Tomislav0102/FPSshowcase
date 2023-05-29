using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;
using FirstCollection;

public class EnemyAnim : MonoBehaviour
{
    public System.Action<bool> ActivateIK;
    Animator _anim;
    EnemyBehaviour _enemyBehaviour;
    [SerializeField] EnemyWeaponUsed weaponAnimType;
    [field: SerializeField] Transform MyHead { get; set; }
    [SerializeField] SoItem weaponUsed;
    [SerializeField] GameObject muzzle;
    [SerializeField] Rig rigRightHandAiming;
    [SerializeField] Rig rigLeftHand;
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand;
    Transform _aimIK;
    float _spreadWeapon = 50f;
    Vector3 _offsetTar;
    bool IkActive
    {
        get => _ikActive;
        set
        {
            _ikActive = value;
            rigRightHandAiming.weight = value ? 1f : 0f;
            switch (weaponAnimType)
            {
                case EnemyWeaponUsed.Melee:
                    break;
                case EnemyWeaponUsed.Pistol:
                    rigLeftHand.weight = value ? 1f : 0f;
                    break;
                case EnemyWeaponUsed.Rifle:
                    rigLeftHand.weight = value ? 1f : 0f;
                    break;
            }

        }
    }
    bool _ikActive;


    private void OnEnable()
    {
        ActivateIK += (bool b) => { IkActive = b; };
    }
    private void OnDisable()
    {
        ActivateIK -= (bool b) => { IkActive = b; };
    }

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
