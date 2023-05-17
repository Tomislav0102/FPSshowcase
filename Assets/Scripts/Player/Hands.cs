using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro.EditorUtilities;

public class Hands : GlobalEventManager
{
    Player _player;
    PoolManager _poolMan;
    Animator _anim;
    [SerializeField] SoItem weapon;
    [ShowIf("CanAim", true)]
    [SerializeField] Transform aimPoint;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] Transform sleeveSpawnPoint;
    [SerializeField] GameObject throwableMesh;

    GameObject _sleeveGameobject;
    Rigidbody _rigidSleeve;

    [HideLabel]
    [BoxGroup("Attachments")]
    [SerializeField] Attachments attachments;

    [ShowIf("IsFlamethrower", true)]
    public FlamethrowerControl flamethrowerControl;

    bool CanAim() //only for Odin
    {
        return weapon.canAim;
    }
    bool IsFlamethrower()
    {
        return weapon.weaponType == WeaponMechanics.BreathWeapon;
    }
    public void SetAwakeGetData(out SoItem wea, out Animator anim, out Transform bulletSpawn, out Transform aimP)
    {
        _player = GameManager.gm.player;
        _poolMan = GameManager.gm.poolManager;
        _anim = GetComponent<Animator>();

        wea = weapon;
        anim = _anim;
        bulletSpawn = muzzleFlash.transform;
        aimP = aimPoint;
        if (IsFlamethrower()) flamethrowerControl.Init(weapon, muzzleFlash.transform);

        attachments.Init(this, _player.offense, aimPoint);
        attachments.UpdateGameobjectVisiblity(weapon);
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

    public void ThrowableMethod(int attackOrdinal, bool activateThrowbale)
    {
        if (throwableMesh == null) return;
        if (activateThrowbale) throwableMesh.SetActive(true);
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
        [SerializeField] ParticleSystem flameMain;
        [SerializeField] ParticleSystem[] smokeEmbers;
        [SerializeField] Transform collParent;
        [SerializeField] Collider coll;
        BreathCollider _breathCollider;
        Transform _spawnPoint;
        SoItem _weapon;
        RaycastHit _hit;
        readonly Vector2 _startSpeedRange = new Vector2(1f, 30f);
        readonly Vector2 _startSizeRange = new Vector2(0.2f, 3f);

        public void Init(SoItem wea, Transform aimPoint)
        {
            _weapon = wea;
            _spawnPoint = aimPoint;
            _breathCollider = coll.GetComponent<BreathCollider>();
            Collider[] plcols = new Collider[1];
            plcols[0] = GameManager.gm.player.capsuleCollider;
            _breathCollider.Init(plcols, wea);
        }
        public void Flame(bool startFire)
        {
            float dist = _weapon.range;
            if (startFire)
            {
                if (Physics.Raycast(_spawnPoint.position, _spawnPoint.forward, out _hit, _weapon.range))
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
            Fire(dist/_weapon.range);
        }
        void Fire(float distNormalized)
        {
            var m = flameMain.main;

            if (distNormalized == 0) //not shooting
            {
                coll.enabled = false;
                for (int i = 0; i < smokeEmbers.Length; i++)
                {
                    if (smokeEmbers[i].isPlaying) smokeEmbers[i].Stop();
                }
                m.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            else
            {

                coll.enabled = true;
                for (int i = 0; i < smokeEmbers.Length; i++)
                {
                    if (smokeEmbers[i].isStopped) smokeEmbers[i].Play();
                }
                m.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            collParent.localScale = new Vector3(1f, 1f, Mathf.Lerp(0, 0.5f * _weapon.range, distNormalized));
            m.startSpeed = Mathf.Lerp(_startSpeedRange.x, _startSpeedRange.y, distNormalized);
            m.startSize = Mathf.Lerp(_startSizeRange.x, _startSizeRange.y, distNormalized);

        }
    }

}
