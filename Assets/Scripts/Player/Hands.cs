using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using DG.Tweening;
using Sirenix.OdinInspector;

public class Hands : GlobalEventManager
{
    Player _player;
    PoolManager _poolMan;
    Animator _anim;
    [SerializeField] SoItem weapon;
    [HideIf("@weapon.weaponType", WeaponMechanics.Melee)]
    [SerializeField] Transform aimPoint;
    [SerializeField] GameObject muzzleFlash;
    [HideIf("@weapon.weaponType", WeaponMechanics.Melee)]
    [SerializeField] Transform sleeveSpawnPoint;
    [ShowIf("@weapon.weaponType", WeaponMechanics.Thrown)]
    [SerializeField] GameObject throwableMesh;

    GameObject _sleeveGameobject;
    Rigidbody _rigidSleeve;

    [SerializeField] Attachments attachments;

    [ShowIf("IsFlamethrower")]
   // [ShowIf("@weapon.weaponType", WeaponMechanics.BreathWeapon)]
    public FlamethrowerControl flamethrowerControl;

    bool IsFlamethrower()
    {
        return weapon.weaponType == WeaponMechanics.BreathWeapon;
    }

    public void SetAwakeGetData(out SoItem wea, out Animator anim, out Transform bulletSpawn, out Transform aimP)
    {
        _player = GameManager.Instance.player;
        _poolMan = GameManager.Instance.poolManager;
        _anim = GetComponent<Animator>();

        wea = weapon;
        anim = _anim;
        bulletSpawn = muzzleFlash.transform;
        aimP = aimPoint;
        if (IsFlamethrower()) flamethrowerControl.Init(this, weapon, muzzleFlash.transform);

        attachments.Init(this, _player.offense, aimPoint);
        attachments.UpdateGameobjectVisiblity(weapon);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        muzzleFlash.SetActive(false);
    }
    public void AE_Attacking(int a)
    {
      //  print("AE_Attacking");
        _player.offense.WeaponDischarge(a);

        ThrowableMethod(a, false);
    }
    public void AE_ReloadComplete()
    {
        if (weapon.partialReload)
        {
            if (!_player.offense.CheckForPartialReload()) _anim.CrossFade("Idle", 0.02f);
        }
        else
        {
            _player.offense.Reload();
        }

        ThrowableMethod(99, true);
    }

    public void ThrowableMethod(int attackOrdinal, bool activateThrowable)
    {
        if (throwableMesh == null) return;
        if (activateThrowable) throwableMesh.SetActive(true);
        else if (attackOrdinal == 0) throwableMesh.SetActive(false);
    }

    public void ReturnToHands()
    {
        EjectSleeve();
        MuzzleFlashActivation();

        if (!IsFlamethrower()) return;
        flamethrowerControl.Flame(true);
    }
    void EjectSleeve()
    {
        if (sleeveSpawnPoint == null) return;
        _poolMan.GetSleeve(out _sleeveGameobject, weapon.ammoType);
        if (_sleeveGameobject == null) return;

        _rigidSleeve = _sleeveGameobject.GetComponent<Rigidbody>();
        _rigidSleeve.velocity = Vector3.zero;
        _rigidSleeve.transform.SetPositionAndRotation(sleeveSpawnPoint.position, sleeveSpawnPoint.rotation);
        _sleeveGameobject.SetActive(true);

        _rigidSleeve.AddRelativeForce(2f * Vector3.right, ForceMode.VelocityChange);
        _rigidSleeve.AddRelativeTorque(20f * new Vector3(Random.value, Random.value, Random.value), ForceMode.VelocityChange);

    }
    void MuzzleFlashActivation()
    {
        if (muzzleFlash == null) return;
        muzzleFlash.SetActive(false);
        muzzleFlash.SetActive(true);
    }
    public void ChangeMuzzleByFlameArrester(bool hasFlameArester)
    {
        if (muzzleFlash == null) return;
        muzzleFlash.transform.localScale = (hasFlameArester ? 0.3f : 0.8f) * Vector3.one;
    }

    [System.Serializable]
    public class Attachments
    {
        [HideLabel]
        [SerializeField] WeaponDetail<GameObject> weaponDetailGameObj;
        Hands _hands;
        Offense _offense;
        Transform _aimPoint;

        public void Init(Hands hands, Offense off, Transform aimP)
        {
            _hands = hands;
            _offense = off;
            _aimPoint = aimP;
        }
        public void UpdateGameobjectVisiblity(SoItem weapon)
        {
            if (weaponDetailGameObj.flameArrester != null)
            {
                _hands.ChangeMuzzleByFlameArrester(weapon.weaponDetail.flameArrester);
                weaponDetailGameObj.flameArrester.SetActive(weapon.weaponDetail.flameArrester);
            }
            if (weaponDetailGameObj.flashlight != null) weaponDetailGameObj.flashlight.SetActive(weapon.weaponDetail.flashlight);
            if (weaponDetailGameObj.scope != null)
            {
                bool hasScope = weapon.weaponDetail.scope;
                _offense.aimPoints[weapon.ordinalLookup] = hasScope ? weaponDetailGameObj.scope.transform : _aimPoint;
                weaponDetailGameObj.scope.SetActive(hasScope);
            }
            if (weaponDetailGameObj.silencer != null) weaponDetailGameObj.silencer.SetActive(weapon.weaponDetail.silencer);
        }
    }

    [System.Serializable]
    public class FlamethrowerControl
    {
        GameManager _gm;
        Hands _hands;
        [SerializeField] ParticleSystem flameOn;
        [SerializeField] ParticleSystem flameOff;
        [SerializeField] ParticleSystem[] smokeEmbers;
        [SerializeField] Transform collParent;
        [SerializeField] Collider coll;
        BreathCollider _breathCollider;
        Transform _spawnPoint;
        SoItem _weapon;
        RaycastHit _hit;

        public void Init(Hands hands, SoItem wea, Transform aimPoint)
        {
            _gm = GameManager.Instance;
            _hands = hands;
            _weapon = wea;
            _spawnPoint = aimPoint;
            _breathCollider = coll.GetComponent<BreathCollider>();
            Collider[] plcols = new Collider[1];
            plcols[0] = _gm.plFaction.MyCollider;
            _breathCollider.Init(_gm.plFaction, plcols, wea);
        }
        public void Flame(bool startFire)
        {
            float dist = _weapon.range;
            if (startFire)
            {
                if (Physics.Raycast(_spawnPoint.position, _spawnPoint.forward, out _hit, _weapon.range, _gm.layShooting))
                {
                    dist = _hit.distance;
                    _breathCollider.SpawnFire(_hit.point);
                }
            }
            else
            {
                dist = 0f;
                _breathCollider.StopFire();
            }
            ParticlesMethod(dist/_weapon.range);
        }

        void ParticlesMethod(float distNormalized)
        {
            if (distNormalized == 0) //not shooting
            {
                coll.enabled = false;
                for (int i = 0; i < smokeEmbers.Length; i++)
                {
                    if (smokeEmbers[i].isPlaying) smokeEmbers[i].Stop();
                }
                if(flameOn.isPlaying) flameOn.Stop();
                if(flameOff.isStopped) flameOff.Play();
            }
            else
            {

                coll.enabled = true;
                for (int i = 0; i < smokeEmbers.Length; i++)
                {
                    if (smokeEmbers[i].isStopped) smokeEmbers[i].Play();
                }
                if (flameOn.isStopped) flameOn.Play();
                if (flameOff.isPlaying) flameOff.Stop();
            }
            collParent.localScale = new Vector3(1f, 1f, Mathf.Lerp(0, 0.5f * _weapon.range, distNormalized));

        }
    }

}
