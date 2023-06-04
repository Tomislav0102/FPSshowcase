using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;
using FirstCollection;

public class EnemyAnimOriginal : MonoBehaviour
{
    EnemyRef _eRef;
    [SerializeField] Collider[] ragdollColls;
    RagdollBodyPart[] _bodyParts;
    [SerializeField] EnemyWeaponUsed weaponAnimType;
    public SoItem weaponUsed;
    public GameObject muzzle;
    [SerializeField] Rig rigRightHandAiming;
    [SerializeField] Rig rigLeftHand;
    [SerializeField] MultiAimConstraint multiAimConstraintRightHand; //needed for accuracy (together with '_spreadWeapon')
    Transform _aimIK;
    float _spreadWeapon = 0f;
    Vector3 _offsetTar;
    /*[HideInInspector]*/ public bool isHit;
    float _weightRightHandAim, _weightLeftHand;
    const float CONST_WEIGHTSPEED = 2f;

    void GetIK(bool attack)
    {
        _weightRightHandAim = _weightLeftHand = 0f;
        if (isHit) return;

        if (attack)
        {
            _weightRightHandAim = _weightLeftHand = 1f;
        }
        else
        {
            switch (weaponAnimType)
            {
                case EnemyWeaponUsed.Melee:
                    break;
                case EnemyWeaponUsed.Pistol:
                    break;
                case EnemyWeaponUsed.Rifle:
                    _weightLeftHand = 1f;
                    break;
            }
        }
    }
    public void InitAwake(EnemyRef eRef, out HashSet<Collider> hs)
    {
        _eRef = eRef;
        _aimIK = multiAimConstraintRightHand.data.sourceObjects[0].transform;
        _bodyParts = new RagdollBodyPart[ragdollColls.Length];
        HashSet<Collider> colls = new HashSet<Collider>();
        for (int i = 0; i < _bodyParts.Length; i++)
        {
            _bodyParts[i] = ragdollColls[i].GetComponent<RagdollBodyPart>();
            _bodyParts[i].InitializeMe(_eRef);
            colls.Add(_bodyParts[i].GetComponent<Collider>());
        }
        hs = colls;

        _eRef.anim.SetFloat("rof", weaponUsed.rofModifier);

    }
    //public void AE_Attacking()
    //{
    //    _eRef.enemyBehaviour.PassFromAE_Attacking();
    //}

    public void Attack(bool isAttacking)
    {
        //   print(isAttacking);
        // IkActive = isAttacking;
        GetIK(isAttacking);
        _eRef.anim.SetBool("attack", isAttacking);
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
        _eRef.anim.SetInteger("movePhase", (int)movetype);
    }


    private void Update()
    {
         rigRightHandAiming.weight = _weightRightHandAim;
        //  rigLeftHand.weight = _weightLeftHand;
        //  rigRightHandAiming.weight = Mathf.MoveTowards(rigRightHandAiming.weight, _weightRightHandAim, CONST_WEIGHTSPEED * Time.deltaTime);
        rigLeftHand.weight = Mathf.MoveTowards(rigLeftHand.weight, _weightLeftHand, CONST_WEIGHTSPEED * Time.deltaTime);
    }
}
