using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FirstCollection;
using TMPro;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.Rendering.PostProcessing;

public class GameManager : MonoBehaviour
{
    public static GameManager gm;
    
    [BoxGroup("Colors for gizmo")]
    [HideLabel]
    public Color[] gizmoColorsByState;

    public Player player;
    public Camera mainCam;
    [HideInInspector] public Transform camTr, camRigTr;
    [HideInInspector] public CameraBehaviour cameraBehaviour;
    public UImanager uiManager;
    public PoolManager poolManager;
    public PostprocessMan postProcess;

    public Transform wayPointParent;
    private void Awake()
    {
        gm = this;
        camTr = mainCam.transform;
        camRigTr = camTr.parent.transform;
        cameraBehaviour = mainCam.GetComponent<CameraBehaviour>();
        poolManager.Init();
        postProcess.Init();

        Time.timeScale = 1f;

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        //else if (Input.GetKeyDown(KeyCode.P))
        //{
        //    if (Mathf.Approximately(Time.timeScale, 0f))
        //    {
        //        Time.timeScale = 1f;
        //        player.IsActive = true;
        //    }
        //    else
        //    {
        //        Time.timeScale = 0f;
        //        player.IsActive = false;

        //    }
        //}

    }

    private void OnTriggerEnter(Collider other)
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
        
    }

}
#region//GENERAL 
[System.Serializable]
public struct WeaponDetail<T>
{
    public T flameArrester;
    public T flashlight;
    public T scope;
    public T silencer;
}
[System.Serializable]
public class DamageOverTime
{
    [EnumToggleButtons]
    public ElementType elementType;
    [BoxGroup("@st", CenterLabel =true)]
    [HorizontalGroup("@st/b")]
    [Indent(3)]
    public int damagePerTick;
    [BoxGroup("@st")]
    [HorizontalGroup("@st/b")]
    [Indent(3)]
    public float effectDuration;

    readonly string st = "--2 ticks per second--";
}

[System.Serializable]
public class PostprocessMan
{
    [SerializeField] PostProcessProfile _mainProfile;
    DepthOfField _depth;
    Vignette _vignette;
    public void Init()
    {
        _depth = _mainProfile.GetSetting<DepthOfField>();
        _vignette = _mainProfile.GetSetting<Vignette>();
        _depth.active = false;
        _vignette.intensity.value = 0.4f;
    }

    public void ShowDepth(bool show)
    {
        if(_depth.active ==  show) return;
        _depth.active = show;
        _vignette.intensity.value = show ? 0.55f : 0.4f;
    }
}
public class AttackClass
{
    GameManager _gm;
    Transform _camTr;
    RaycastHit _hit;
    RaycastHit[] _multipleHits = new RaycastHit[4];
    readonly HashSet<Collider> _colliders = new HashSet<Collider>();
   // [ShowInInspector]
    GameObject projec; 
    Vector2 _screenCenter;
    Faction _faction = Faction.Player;
    public Transform bulletSpawnPosition;

    Ray ShootDirection()
    {
        switch (_faction)
        {
            case Faction.Player:
                if (_gm.player.offense.IsAiming)
                {
                    Ray r = new Ray();
                    r.origin = _camTr.position;
                    r.direction = _gm.player.offense.AimDirection();
                    return r;
                }
                else
                {
                    Vector2 pos = _screenCenter + _gm.uiManager.crosshairObject.Spread * Random.insideUnitCircle;
                    return _gm.mainCam.ScreenPointToRay(pos);
                }
            case Faction.Enemy:
                Ray ra = new Ray();
                ra.origin = bulletSpawnPosition.position;
                ra.direction = bulletSpawnPosition.forward;
                return ra;

            default:
                return new Ray();
        }

    }
    public AttackClass(Collider[] attackerColliders, IFactionTarget factionTarget)
    {
        _gm = GameManager.gm;
        _camTr = _gm.mainCam.transform;
        for (int i = 0; i < attackerColliders.Length; i++)
        {
            _colliders.Add(attackerColliders[i]);
        }
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        _faction = factionTarget.Fact;
    }

    public void Attack(SoItem weaponItem)
    {
        if (weaponItem == null) return;
        switch (weaponItem.weaponType)
        {
            case WeaponMechanics.Melee:
                Physics.SphereCastNonAlloc(_camTr.position, weaponItem.areaOfEffect * 0.5f, _camTr.forward, _multipleHits, weaponItem.range);
                foreach (RaycastHit item in _multipleHits)
                {
                    if (item.collider == null || _colliders.Contains(item.collider)) continue;
                    Debug.Log(item.collider.name);
                    ApplyDamage(weaponItem, item);
                }
                break;

            case WeaponMechanics.Gun:
                SingleRaycast(weaponItem,  weaponItem.noOfPelletsPerShot);
                break;

            case WeaponMechanics.Shotgun:
                SingleRaycast(weaponItem, weaponItem.noOfPelletsPerShot);
                break;

            case WeaponMechanics.Thrown:
                switch (weaponItem.ammoType)
                {
                    //case AmmoType.Bolt:
                    //    break;
                    default:
                        projec = _gm.poolManager.GetProjectile(weaponItem.ammoType);
                        bulletSpawnPosition.LookAt(HitPoint(weaponItem));
                        projec.GetComponent<ProjectilePhysical>().IniThrowable(bulletSpawnPosition, _colliders);
                        projec.SetActive(true);
                        break;
                }
                break;
        }

        void SingleRaycast(SoItem weapon, int pellets)
        {
            Vector3 endPosLineRenderer;
            for (int i = 0; i < pellets; i++)
            {
                if(RayHitsSomething(weapon, out Ray ray)) //out Ray ray needed only for line renderer
                {
                    endPosLineRenderer = _hit.point;
                    ApplyDamage(weapon, _hit);
                }
                else
                {
                    endPosLineRenderer = _camTr.position + ray.direction * weapon.range;
                }
                if(_faction == Faction.Enemy) _gm.poolManager.GetLineRenderer(bulletSpawnPosition.position, endPosLineRenderer);
            }
        }

        Vector3 HitPoint(SoItem waepon)
        {
            if (RayHitsSomething(waepon, out Ray ray)) return _hit.point;
            return _camTr.position + ray.direction * waepon.range;
        }

        bool RayHitsSomething(SoItem weapon, out Ray r)
        {
            r = ShootDirection();
            var allLayers = ~0;
            return Physics.Raycast(r, out _hit, weapon.range, allLayers, QueryTriggerInteraction.Ignore);
        }
    }

    public void ApplyDamage(SoItem weaponItem, RaycastHit hit)
    {
        Collider col = hit.collider;
        if (_colliders.Contains(col)) return;

        if (col.TryGetComponent(out ITakeDamage damagable))
        {
            damagable.TakeDamage(ElementType.Normal, HelperScript.Damage(weaponItem.damage), null, null);
        }
        if (col.TryGetComponent(out IMaterial iMat))
        {
            _gm.poolManager.GetImpactObject(iMat.MaterialType, hit);
        }
        else _gm.poolManager.GetImpactObject(MatType.Plaster, hit);

    }
}


[System.Serializable]
public class UImanager
{
    public Canvas canvasGame;
    [SerializeField] TextMeshProUGUI displayHP, displaySpeed, displayWeaponName, displayAmmo, displayAllAmo;
    [SerializeField] Image pain;
    public Crosshair crosshairObject;

    public void ShowHitPoints(int hp, int maxHp)
    {
        displayHP.text = $"{hp} / {maxHp}";
    }
    public void ShowSpeed(float speed)
    {
        displaySpeed.text = $"Speed - {speed.ToString("0.0")}";
    }
    public void ShowAmmo(SoItem weapon, int clip, int ammoCurrent)
    {
        displayWeaponName.enabled = false;
        displayAmmo.enabled = false;
        if (weapon == null) return;
        else
        {
            displayWeaponName.enabled = true;
            displayAmmo.enabled = true;
        }

        displayWeaponName.text = weapon.itemName;
        displayAmmo.text = weapon.ammoType == AmmoType.None ? "" : $"{clip}/{ammoCurrent}";
    }
    public void ShowAllAmmo(Dictionary<AmmoType, int> current, Dictionary<AmmoType, int> capacity)
    {
        displayAllAmo.text = "";
        int a = 1;
        foreach (KeyValuePair<AmmoType, int> item in current)
        {
            if (item.Key == AmmoType.None) continue;
            displayAllAmo.text += $"{item.Key} - {item.Value} / <b><color=#F8F606>{capacity[(AmmoType)a]}max </color></b>}}\n";
            a++;
        }
    }
    public void ShowPain()
    {
        pain.DOFade(0f, 0.5f)
            .From(0.8f);
    }
}

[System.Serializable]
public class PoolManager
{
    [SerializeField] Transform poolFloatDamage, poolLineRenderers, poolRockets, poolExplosionBig, poolExplosionSmall, poolGreandes, 
        poolSleeveAutomatic, poolSleeveShotgun, poolSleeveSniper, poolSleeve9mm, poolBolts, poolDecals, poolWildfire,
        poolImpactBlood, poolImpactBrick, poolImpactDirt, poolImpactPlaster, poolImpactWater;

    GameObject[] _floatingDamage;
    int _cFloatDam;

    Transform[] _impBlood;
    int _cImpBlood;
    Transform[] _impBrick;
    int _cImpBrick;
    Transform[] _impDirt;
    int _cImpDirt;
    Transform[] _impPlaster;
    int _cImpPlaster;
    Transform[] _impWater;
    int _cImpWater;

    LineRenderer[] _lrs;
    int _cLR;
    GameObject[] _rockets;
    int _cRockets;
    GameObject[] _grenades;
    int _cGrenades;
    GameObject[] _explosionBig;
    int _cExplosionBig;
    GameObject[] _explosionSmall;
    int _cExplosionSmall;
    GameObject[] _sleevesAutomatic;
    int _cSleevesAutomatic;
    GameObject[] _sleevesShotgun;
    int _cSleevesShotgun;
    GameObject[] _sleevesSniper;
    int _cSleevesSniper;
    GameObject[] _sleeves9mm;
    int _cSleeves9mm;
    GameObject[] _bolts;
    int _cBolts;
    Transform[] _decals;
    int _cDecals;
    Transform[] _wildFire;
    int _cWildFire;

    public void Init()
    {
        _floatingDamage = HelperScript.AllChildrenGameObjects(poolFloatDamage);
        _impBlood = HelperScript.AllChildren(poolImpactBlood);
        _impBrick = HelperScript.AllChildren(poolImpactBrick);
        _impDirt = HelperScript.AllChildren(poolImpactDirt);
        _impPlaster = HelperScript.AllChildren(poolImpactPlaster);
        _impWater = HelperScript.AllChildren(poolImpactWater);
        _lrs = poolLineRenderers.GetComponentsInChildren<LineRenderer>();
        _explosionBig = HelperScript.AllChildrenGameObjects(poolExplosionBig);
        _explosionSmall = HelperScript.AllChildrenGameObjects(poolExplosionSmall);
        _rockets = HelperScript.AllChildrenGameObjects(poolRockets);
        _grenades = HelperScript.AllChildrenGameObjects(poolGreandes);
        _sleevesAutomatic = HelperScript.AllChildrenGameObjects(poolSleeveAutomatic);
        _sleevesShotgun = HelperScript.AllChildrenGameObjects(poolSleeveShotgun);
        _sleevesSniper = HelperScript.AllChildrenGameObjects(poolSleeveSniper);
        _sleeves9mm = HelperScript.AllChildrenGameObjects(poolSleeve9mm);
        _bolts = HelperScript.AllChildrenGameObjects(poolBolts);
        _decals = HelperScript.AllChildren(poolDecals);
        _wildFire = HelperScript.AllChildren(poolWildfire);
    }
    public void GetFloatingDamage(Vector3 position, string st, ElementType elementType)
    {
        int dur = 5000;
        GameObject g = GetGenericObject<GameObject>(_floatingDamage, ref _cFloatDam, dur);
        g.GetComponent<FloatingDamage>().FloatingDisplay(position, st, dur/1000, elementType);
        g.SetActive(true);
    }
    public Transform GetWildFire()
    {
        return GetGenericObject<Transform>(_wildFire, ref _cWildFire, 0);
    }
    public void GetBulletDecal(Transform hitTarget, Vector3 hitPosition, Vector3 hitNormal)
    {
        Transform decal = GetGenericObject<Transform>(_decals, ref _cDecals, 2000);
        decal.parent = null;
        decal.localScale = 0.1f * Vector3.one;
        decal.SetPositionAndRotation(hitPosition + 0.001f * hitNormal, Quaternion.LookRotation(hitNormal));
        decal.parent = hitTarget.transform;
        decal.gameObject.SetActive(true);
    }
    public void GetLineRenderer(Vector3 startPos, Vector3 endPos)
    {
        LineRenderer line = GetGenericObject<LineRenderer>(_lrs, ref _cLR, 200);

        line.positionCount = 2;
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
        line.enabled = true;
    }

    public void GetImpactObject(MatType matType, RaycastHit hit)
    {
        Transform tr = null;
        switch (matType)
        {
            case MatType.Blood:
                tr = GetGenericObject<Transform>(_impBlood, ref _cImpBlood, 3500);
                break;
            case MatType.Brick:
                tr = GetGenericObject<Transform>(_impBrick, ref _cImpBrick, 3500);
                break;
            case MatType.Concrete:
                break;
            case MatType.Dirt:
                tr = GetGenericObject<Transform>(_impDirt, ref _cImpDirt, 3500);
                break;
            case MatType.Foliage:
                break;
            case MatType.Glass:
                break;
            case MatType.Metal:
                break;
            case MatType.Plaster:
                tr = GetGenericObject<Transform>(_impPlaster, ref _cImpPlaster, 3500);
                break;
            case MatType.Rock:
                break;
            case MatType.Water:
                tr = GetGenericObject<Transform>(_impWater, ref _cImpWater, 3500);
                break;
            case MatType.Wood:
                break;
            case MatType.NoMaterial:
                return;
        }
        if (tr == null) return;
        tr.parent = null;
       // tr.localScale = Vector3.one;
        //tr.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.point + hit.normal));
        tr.position = hit.point;
        tr.LookAt(hit.point + hit.normal);
        tr.parent = hit.transform;
        tr.gameObject.SetActive(true);

    }
    public GameObject GetProjectile(AmmoType ammoType) 
    {
        GameObject g = null;
        switch (ammoType)
        {
            case AmmoType.Rocket:
                g = GetGenericObject<GameObject>(_rockets, ref _cRockets, 0);
                break;
            case AmmoType.HandGrenade:
                g = GetGenericObject<GameObject>(_grenades, ref _cGrenades, 0);
                break;
            case AmmoType.Bolt:
                g = GetGenericObject<GameObject>(_bolts, ref _cBolts, 0);
                break;
        }
        return g;

    }
    public void GetExplosion(ExplosionType explosionType, Vector3 pos)
    {
        GameObject currentPs = null;
        switch (explosionType)
        {
            case ExplosionType.Big:
                currentPs = GetGenericObject<GameObject>(_explosionBig, ref _cExplosionBig, 2000);
                break;
            case ExplosionType.Small:
                currentPs = GetGenericObject<GameObject>(_explosionSmall, ref _cExplosionSmall, 2000);
                break;
        }
        currentPs.transform.position = pos;
        currentPs.SetActive(true);
    }

    T GetGenericObject<T>(T[] arr, ref int count, int miliSecondsDelayEnd)
    {
        T obj = arr[count];
        count = (1 + count) % arr.Length;

        if (miliSecondsDelayEnd > 0) End(obj, miliSecondsDelayEnd);
        return obj;
    }

    public void GetSleeve(out GameObject g, AmmoType ammotype)
    {
        switch (ammotype)
        {
            case AmmoType._9mm:
                g = GetGenericObject<GameObject>(_sleeves9mm, ref _cSleeves9mm, 3000);
                break;
            case AmmoType._44cal:
                g = GetGenericObject<GameObject>(_sleeves9mm, ref _cSleeves9mm, 3000);
                break;
            case AmmoType._762mm:
                g = GetGenericObject<GameObject>(_sleevesAutomatic, ref _cSleevesAutomatic, 3000);
                break;
            case AmmoType._303REM:
                g = GetGenericObject<GameObject>(_sleevesSniper, ref _cSleevesSniper, 3000);
                break;
            case AmmoType._12gauge:
                g = GetGenericObject<GameObject>(_sleevesShotgun, ref _cSleevesShotgun, 3000);
                break;
            default:
                g = null;
                break;
        }

        
    }
    async void End<T>(T tip, int miliSecondsDelay)
    {
        await Task.Delay(miliSecondsDelay);
        if (tip.GetType() == typeof(GameObject))
        {
            GameObject go = tip as GameObject;
            go.SetActive(false);
        }
        else if (tip.GetType() == typeof(Transform))
        {
            Transform tr = tip as Transform;
            tr.gameObject.SetActive(false);
        }
        else if (tip.GetType() == typeof(ParticleSystem))
        {
            ParticleSystem ps = tip as ParticleSystem;
            ps.Stop();
        }
        else if (tip.GetType() == typeof(LineRenderer))
        {
            LineRenderer lr = tip as LineRenderer;
            lr.enabled = false;
        }
    }
}
#endregion


#region//PLAYER CLASSES
[System.Serializable]
public class Controls
{
    Player _player;
    GameManager _gm;
    [SerializeField] LayerMask layersGrounded;
    [SerializeField] float moveSpeed, turnSpeed, jumpForce;
    const int CONST_DOWNFORCE = 2000;
    float _downForce;

    bool IsDucked
    {
        get => _isDucked;
        set
        {
            _isDucked = value;
            if (IsDucked)
            {
                _moveDuck = 0.3f;
                _player.camPosition.DOLocalMoveY(_camHeights.y, 0.1f)
                    .SetEase(Ease.InFlash);
                _player.capsuleCollider.center = 0.5f * Vector3.up;
                _player.capsuleCollider.height = 1f;
            }
            else
            {
                _moveDuck = 1f;
                _player.camPosition.DOLocalMoveY(_camHeights.x, 0.1f)
                       .SetEase(Ease.InFlash);
                _player.capsuleCollider.center = Vector3.up;
                _player.capsuleCollider.height = 2f;
            }
        }
    }
    bool CanStandUp()
    {
        if (Physics.Raycast(_player.myTransform.position + Vector3.up, Vector3.up, 0.1f, layersGrounded)) return false;

        return true;
    }
    bool IsSprinting
    {
        get => _isSprinting;
        set
        {
            _isSprinting = value;
            if (_isSprinting)
            {
                _moveSprint = 2f;
                _player.offense.IsAiming = false;
                return;
            }
            _moveSprint = 1f;
        }
    }
    bool IsGrounded
    {
        get => _isGrounded;
        set
        {
            _isGrounded = value;
            if (_isGrounded)
            {
                 _downForce = CONST_DOWNFORCE;
            }
            else
            {
                _rigid.AddForce(_downForce * Vector3.down);
                if (Mathf.Abs(_rigid.velocity.y) > -70f) _downForce += 0.25f * CONST_DOWNFORCE;
            }
        }
    }
    bool _isGrounded, _isDucked, _isSprinting;
    float _moveDuck = 1f;
    float _moveSprint = 1f;
    [HideInInspector] public float moveAim = 1f;
    [HideInInspector] public float airMove = 1f;
    [HideInInspector] public Vector3 _camHeights;
    Rigidbody _rigid;
    float _hor, _ver, _mouseX, _mouseY;
    Vector3 _projectedCamForward, _projectedCamRight, _moveDir;
    RaycastHit hit;
    RaycastHit[] _hitsGrounded = new RaycastHit[1];
    float _lastVelocityY;
    [SerializeField] float fallDamageTreshold = -20f;


    public void Init()
    {
        _gm = GameManager.gm;
        _player = _gm.player;

        _rigid = _player.rigid;
        _camHeights = new Vector3(1.6f, 0.8f, 0.2f); //normal, duck, dead
        IsDucked = false;
        IsSprinting = false;
    }
    public void Activation(bool isActive)
    {
        if (isActive)
        {

        }
        else
        {
            _hor = _ver = 0f;
            _moveDir = Vector3.zero;
            _gm.cameraBehaviour.speed = 0f;
        }
    }
    public void Motion()
    {
        _hor = Input.GetAxis("Horizontal");
        _ver = Input.GetAxis("Vertical");

        DefineMoveDirection();

        _gm.cameraBehaviour.speed = (_ver != 0f && _isGrounded && _rigid.velocity.magnitude > 0.1f) ? 1f * _moveSprint * _moveDuck * moveAim : 0f;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (IsDucked)
            {
                if (CanStandUp()) IsDucked = false;
            }
            else IsDucked = true;
        }
        IsSprinting = Input.GetKey(KeyCode.LeftShift);

        if (IsGrounded && Input.GetKey(KeyCode.Space))
        {
             _rigid.velocity = new Vector3(_rigid.velocity.x, jumpForce, _rigid.velocity.z);
           // _rigid.AddForce(jumpForce * Vector3.up, ForceMode.VelocityChange);
        }

        DefineLookRotation();

    }


    private void DefineMoveDirection()
    {
        _projectedCamForward = Vector3.ProjectOnPlane(_gm.camRigTr.forward, hit.normal);
        float pcfX = _projectedCamForward.x;
        float pcfZ = _projectedCamForward.z;
        if (Mathf.Abs(pcfX) < 0.3f) pcfX = 0;
        if (Mathf.Abs(pcfZ) < 0.3f) pcfZ = 0;
        _projectedCamForward = new Vector3(pcfX, 0f, pcfZ);
        _projectedCamRight = new Vector3(pcfZ, 0f, -pcfX);
        _moveDir = _ver * _projectedCamForward + _hor * _projectedCamRight;
    }
    private void DefineLookRotation()
    {
        _mouseX += turnSpeed * Input.GetAxis("Mouse X");
        _mouseY += turnSpeed * Input.GetAxis("Mouse Y");
        _mouseY = Mathf.Clamp(_mouseY, -60f, 60f);
        _gm.camRigTr.localEulerAngles = new Vector3(-_mouseY, _mouseX, 0f);
    }

    public void MotionFixedUpdate()
    {
        _rigid.AddForce(airMove * moveAim * moveSpeed * _moveDuck * _moveSprint * 1000f * _moveDir);

        int num = Physics.SphereCastNonAlloc(_player.myTransform.position, 0.4f, Vector3.down, _hitsGrounded, 0f, layersGrounded);
        IsGrounded = num > 0;

        if (Mathf.Approximately(_moveDir.sqrMagnitude, 0f) || !IsGrounded) _player.offense.MoveSpeed(MoveType.Stationary);
        else if (_moveSprint > 1f) _player.offense.MoveSpeed(MoveType.Run);
        else _player.offense.MoveSpeed(MoveType.Walk);

        FallDamage();

        //  Vector3 horVel = new Vector3(_rigid.velocity.x, _rigid.velocity.y, _rigid.velocity.z);
        //  Vector3 horVel = new Vector3(_rigid.velocity.x, 0f, _rigid.velocity.z);
        //Vector3 horVel = new Vector3(0f, _rigid.velocity.y, 0f);

        //_gm.uiManager.ShowSpeed(horVel.magnitude);
    }

    void FallDamage()
    {
        if (IsGrounded)
        {
            if (_lastVelocityY < fallDamageTreshold)
            {
               // _player.TakeDamage(ElementType.Normal, -3 * (int)(_lastVelocityY - fallDamageTreshold), (ElementType e, int b) => { });
            }
            _lastVelocityY = 0f;
        }
        else _lastVelocityY = _rigid.velocity.y;
    }
}

[System.Serializable]
public class Offense
{
    Player _player;
    GameManager _gm;
    [SerializeField] Transform parWeapons;
    public SoItem[] weapons;
    SoItem _currWeapon;
    Animator[] _wAnims;
    Hands[] _hands;
    Transform[] _bulletSpawnPositions;
    /*[HideInInspector]*/ public Transform[] aimPoints;
    public Vector3 AimDirection()
    {
        if (aimPoints[Windex] == null) return _gm.camTr.forward;
        else
        {
            if (_currWeapon.weaponDetail.scope) return aimPoints[Windex].forward;
            else return aimPoints[Windex].position - _gm.camTr.position;
        }
    }
    public int Windex
    {
        get => _wi;
        set
        {
            _wi = _nextWeaponIndex = value;
            _currWeapon = weapons[value];

            _gm.uiManager.ShowAmmo(_currWeapon, _clipCurrent[_currWeapon], _ammoCurrent[_currWeapon.ammoType]);
            for (int i = 0; i < parWeapons.childCount; i++)
            {
                parWeapons.GetChild(i).gameObject.SetActive(false);
            }
            parWeapons.GetChild(value).gameObject.SetActive(true);
            attack.bulletSpawnPosition = _bulletSpawnPositions[value];

            _wAnims[value].SetBool("has2ndAttack", _currWeapon.has2ndAttack);
            _wAnims[value].SetBool("isShotgun", _currWeapon.weaponType == WeaponMechanics.Shotgun);
            _wAnims[value].SetFloat("rofModifier", _currWeapon.rofModifier);

            _gm.uiManager.crosshairObject.Weapon = _currWeapon;
        }
    }
    int _wi;
    int _nextWeaponIndex;
    bool _isReloading; 
    Collider[] _actorColliders = new Collider[1];
    public AttackClass attack; 
    readonly Dictionary<AmmoType, int> _ammoCapacity = new Dictionary<AmmoType, int>();
    readonly Dictionary<AmmoType, int> _ammoCurrent = new Dictionary<AmmoType, int>();
    readonly Dictionary<SoItem, int> _clipCurrent = new Dictionary<SoItem, int>();
    readonly List<int> _acquiredWeapons = new List<int>();
    public bool IsAiming
    {
        get => _isAiming;
        set
        {
            _isAiming = _currWeapon.canAim ? value : false;

            _wAnims[Windex].SetBool("isAiming", _isAiming);
            _player.controls.moveAim = _isAiming ? 0.3f : 1f;

            if (/*_currWeapon.canAim && */_currWeapon.hasCrosshair) _gm.uiManager.crosshairObject.IsActive = !_isAiming;
            else _gm.uiManager.crosshairObject.IsActive = false;
        }
    }
    bool _isAiming;

    public void Init(IFactionTarget factionTarget)
    {
        _gm = GameManager.gm;
        _player = _gm.player;

        weapons = new SoItem[parWeapons.childCount];
        _wAnims = new Animator[weapons.Length];
        _hands = new Hands[weapons.Length];
        _bulletSpawnPositions = new Transform[weapons.Length];
        aimPoints = new Transform[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            _hands[i] = parWeapons.GetChild(i).GetComponent<Hands>();
            _hands[i].SetAwakeGetData(out weapons[i], out _wAnims[i], out _bulletSpawnPositions[i], out aimPoints[i]);
            weapons[i].ordinalLookup = i;
        }

        _actorColliders[0] = _player.capsuleCollider;
        attack = new AttackClass(_actorColliders, factionTarget);

        _ammoCapacity.Add(AmmoType.None, 0);
        _ammoCapacity.Add(AmmoType._9mm, 100);
        _ammoCapacity.Add(AmmoType._44cal, 90);
        _ammoCapacity.Add(AmmoType._762mm, 200);
        _ammoCapacity.Add(AmmoType._303REM, 150);
        _ammoCapacity.Add(AmmoType._12gauge, 80);
        _ammoCapacity.Add(AmmoType.Rocket, 3);
        _ammoCapacity.Add(AmmoType.HandGrenade, 100);
        _ammoCapacity.Add(AmmoType.Bolt, 120);
        _ammoCapacity.Add(AmmoType.Fuel, 1000);
        foreach (KeyValuePair<AmmoType, int> item in _ammoCapacity)
        {
          // _ammoCurrent.Add(item.Key, 0);
           _ammoCurrent.Add(item.Key, _ammoCapacity[item.Key]);
        }
        _gm.uiManager.ShowAmmo(null, 0, 0);
        _gm.uiManager.ShowAllAmmo(_ammoCurrent, _ammoCapacity);
        foreach (SoItem item in weapons)
        {
            _clipCurrent.Add(item, 0);
        }


        AddWeapon(weapons[0]);
    }
    public void MoveSpeed(MoveType moveType)
    {
        switch (moveType)
        {
            case MoveType.Stationary:
                _wAnims[Windex].SetInteger("movePhase", 0);
             //   _gm.uiManager.crosshairObject.moveSpread = 1f;
                _gm.uiManager.crosshairObject.Move(1f);
                break;
            case MoveType.Walk:
                _wAnims[Windex].SetInteger("movePhase", 1);
             //   _gm.uiManager.crosshairObject.moveSpread = 2f;
                _gm.uiManager.crosshairObject.Move(2f);
                break;
            case MoveType.Run:
                _wAnims[Windex].SetInteger("movePhase", 2);
            //    _gm.uiManager.crosshairObject.moveSpread = 4f;
                _gm.uiManager.crosshairObject.Move(4f);
                break;
        }
    }
    public void ChangeWeaponMouseScroll(bool increment)
    {
        if (_acquiredWeapons.Count <= 1) return;

        if (!IdleAnimations()) return;

        int ord = _acquiredWeapons.IndexOf(Windex);
        if (increment)
        {
            ord++;
            if (ord > _acquiredWeapons.Count - 1) ord = 0;
        }
        else
        {
            ord--;
            if (ord < 0) ord = _acquiredWeapons.Count - 1;
        }
        HideWeapon(_acquiredWeapons[ord]);
    }
    public void HideWeapon(int nextWeaponIndex)
    {
        if (!_acquiredWeapons.Contains(nextWeaponIndex) || !IdleAnimations()) return;
        if (weapons[nextWeaponIndex].ammoType != AmmoType.None && _ammoCurrent[weapons[nextWeaponIndex].ammoType] == 0 && _clipCurrent[weapons[nextWeaponIndex]] == 0) return;

        IsAiming = false;

        _wAnims[Windex].SetTrigger("hide");
        _nextWeaponIndex = nextWeaponIndex;
    }
    public void ReadyWeapon()
    {
        Windex = _nextWeaponIndex;
    }
    bool IdleAnimations()
    {
        return _wAnims[Windex].GetCurrentAnimatorStateInfo(0).IsTag("idle"); //no switching while shooting or reloading
    }
    public void BeginAttackAnimation(bool shoot)
    {
        if (_wAnims[Windex].GetCurrentAnimatorStateInfo(0).IsTag("reload")) return;

        if (!shoot) //these two conditions prevent attack anim on ranged weapons if ammo = 0
        {
            if (_currWeapon.weaponType == WeaponMechanics.BreathWeapon) _hands[Windex].flamethrowerControl.Flame(false);
            _wAnims[Windex].SetBool("isAttacking", false);

            return;
        }
        if (_currWeapon.weaponType == WeaponMechanics.Melee)
        {
            _wAnims[Windex].SetBool("isAttacking", shoot);
            return;
        }

        if (_clipCurrent[_currWeapon] == 0 && _currWeapon.ammoType != AmmoType.HandGrenade/* && _currWeapon.ammoType != AmmoType.Bolt*/)
        {
            _wAnims[Windex].SetBool("isAttacking", false);
            BeginReloadAnimation();
            return;
        }
        _wAnims[Windex].SetBool("isAttacking", shoot);
    }
    public void WeaponDischarge(int a) //parameter used only for hand grenade and xbow. 0 for all other weapons, 0 and 1 for hand grenade and xbow
    {
        if (_currWeapon.weaponType == WeaponMechanics.Melee)
        {
            attack.Attack(_currWeapon);
            return;
        }

        switch (a)
        {
            case 0:
                _clipCurrent[_currWeapon]--;
                DisplayUIweapons();
                _hands[Windex].ReturnToHands();
                attack.Attack(_currWeapon);
                _gm.uiManager.crosshairObject.Shoot();

                if (_currWeapon.ammoType == AmmoType.Bolt) BeginReloadAnimation();
                break;
            case 1: 
                switch (_currWeapon.ammoType)
                {
                    case AmmoType.HandGrenade:
                        if (_ammoCurrent[_currWeapon.ammoType] > 0)
                        {
                            BeginReloadAnimation();
                        }
                        else Windex = 0;
                        break;
                    case AmmoType.Bolt:
                        if (_ammoCurrent[_currWeapon.ammoType] > 0)
                        {
                            _hands[Windex].ThrowableMethod(99, true);
                        }
                        break;
                }
                break;
        }
        
    }

    public void BeginReloadAnimation()
    {
        if (_isReloading || _clipCurrent[_currWeapon] == _currWeapon.clipCapacity || _ammoCurrent[_currWeapon.ammoType] == 0) return;
        _isReloading = true;
        IsAiming = false;
        _wAnims[Windex].SetTrigger("reload");
    }
    public void Reload()
    {
        _isReloading = false;
        int reloadAmount = _currWeapon.clipCapacity - _clipCurrent[_currWeapon];
        if (reloadAmount > _ammoCurrent[_currWeapon.ammoType]) reloadAmount = _ammoCurrent[_currWeapon.ammoType];
        _clipCurrent[_currWeapon] += reloadAmount;
        _ammoCurrent[_currWeapon.ammoType] -= reloadAmount;

        DisplayUIweapons();
    }
    
    void DisplayUIweapons()
    {
        _gm.uiManager.ShowAmmo(_currWeapon, _clipCurrent[_currWeapon], _ammoCurrent[_currWeapon.ammoType]);
        _gm.uiManager.ShowAllAmmo(_ammoCurrent, _ammoCapacity);

    }

    public void AddWeapon(SoItem weapon) 
    {
        if (!_acquiredWeapons.Contains(weapon.ordinalLookup))
        {
            _acquiredWeapons.Add(weapon.ordinalLookup);
            _acquiredWeapons.Sort();
        }

        Windex = weapon.ordinalLookup;
        _clipCurrent[_currWeapon] = _currWeapon.clipCapacity;
        DisplayUIweapons();
    }

    public bool CanPickUP(PickUp pu)
    {
        switch (pu.PUType)
        {
            case PuType.Weapon:
                if (!_acquiredWeapons.Contains(pu.weaponPU.ordinalLookup))
                {
                    AddWeapon(pu.weaponPU);
                    return true;
                }
                else
                {
                    AmmoType amm = pu.weaponPU.ammoType;
                    if (_ammoCurrent[amm] >= _ammoCapacity[amm])
                    {
                        return false;
                    }

                    _ammoCurrent[amm] += pu.weaponPU.clipCapacity;
                    if (_ammoCurrent[amm] > _ammoCapacity[amm])
                    {
                        _ammoCurrent[amm] = _ammoCapacity[amm];
                    }
                    DisplayUIweapons();
                    return true;
                }

            case PuType.Ammo:
                AmmoType pickUpAmmo = pu.ammoType;
                if (_ammoCurrent[pickUpAmmo] >= _ammoCapacity[pickUpAmmo])
                {
                    return false;
                }

                _ammoCurrent[pickUpAmmo] += pu.ammoQuantity[pickUpAmmo];
                if (_ammoCurrent[pickUpAmmo] > _ammoCapacity[pickUpAmmo])
                {
                    _ammoCurrent[pickUpAmmo] = _ammoCapacity[pickUpAmmo];
                }
                DisplayUIweapons();
                return true;

            case PuType.Health:
                //if (_player.health.HitPoints < _player.health.maxHP)
                //{
                //    _player.health.HitPoints += pu.healAmount;
                //    return true;
                //}
                break;

            case PuType.Armor:
                break;
            case PuType.Key:
                break;
        }

        return false;
    }

    public bool CheckForPartialReload()
    {
        _clipCurrent[_currWeapon] ++;
        _ammoCurrent[_currWeapon.ammoType] --;
        DisplayUIweapons();
        if (_ammoCurrent[_currWeapon.ammoType] > 0 && _clipCurrent[_currWeapon] <= _currWeapon.clipCapacity)
        {
            return true;
        }
        _isReloading = false;
        return false;
    }
}

#endregion
































































/*
[System.Serializable]
public class HealthClass
{
    System.Action _dead;
    System.Action<ElementType, int> _damCallback;
    [SerializeField] int startingHP = 100;
    public int maxHP = 100;
    public int HitPoints
    {
        get => _hitPoints;
        set
        {
            _hitPoints = value;
            if (_hitPoints > maxHP)
            {
                _hitPoints = maxHP;
            }
            else if (_hitPoints <= 0)
            {
                _dead();
            }
        }
    }
    int _hitPoints;

    int _durationFireInMiliSeconds;
    bool _fireActive;
    const int CONST_TICKFIRE = 500;
    const int CONST_FIREDAMAGE = 1;
    CancellationTokenSource _cancellationTokenSource;

    public void Init(System.Action isDead)
    {
        _dead = isDead;
        if (startingHP > maxHP) startingHP = maxHP;
        HitPoints = startingHP;
        _cancellationTokenSource = new CancellationTokenSource();

    }
    public void TakeDamage(ElementType elementType, int damage, System.Action<ElementType, int> damCallback)
    {
        HitPoints -= damage;
        _damCallback = damCallback;
        switch (elementType)
        {
            case ElementType.Fire:
                //if (damage == 0) _takeDamageParent.OnFire(false);
                //else 
                //{
                //    _takeDamageParent.OnFire(true);
                //    OnFire(damage);
                //}
                OnFire(damage);
                break;
            default:
                damCallback(elementType, damage);
                break;
        }
    }

    void OnFire(int durationSeconds)
    {
        _durationFireInMiliSeconds += durationSeconds * 1000;
        if (!_fireActive) DOT_Fire();
    }
    async void DOT_Fire()
    {
        if (_durationFireInMiliSeconds > 0)
        {
            _fireActive = true;
            _durationFireInMiliSeconds -= CONST_TICKFIRE;
            HitPoints -= CONST_FIREDAMAGE;
            _damCallback(ElementType.Fire, CONST_FIREDAMAGE);
            await Task.Delay(CONST_TICKFIRE, _cancellationTokenSource.Token);
            DOT_Fire();
        }
        else
        {
            _damCallback(ElementType.Fire, 0);
            _fireActive = false;
        }
    }
    public void CancelToken()
    {
        _cancellationTokenSource.Cancel();
    }
}*/
