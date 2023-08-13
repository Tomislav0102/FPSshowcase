using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float gameSpeed = 1f;
    public Color[] gizmoColorsByState;
    [HideInInspector] public Vector3 pointForGizmo;
    public Player player;
    public IFaction plFaction;
    public Camera mainCam, weaponCam;
    public Transform wayPointParent;
    [HideInInspector] public Transform camTr, camRigTr;
    [HideInInspector] public CameraBehaviour cameraBehaviour;
    [HideInInspector] public Animator weaponCamAnim;
    [HideInInspector] public LevelManager levelManager;

    [GUIColor("cyan")]
    public UImanager uiManager;
    [GUIColor("blue")]
    public PoolManager poolManager;
    [GUIColor("lightred")]
    public PostprocessMan postProcess;
    [BoxGroup]
    [GUIColor("lightgreen")]
    [LabelWidth(100)]
    public LayerMask layFOV_Overlap, layFOV_Ray, layFOV_RayAll, layShooting, layCover;
    int layerPl, layerEn;


    private void Awake()
    {
        Instance = this;
        plFaction = player.GetComponent<IFaction>();
        camTr = mainCam.transform;
        camRigTr = camTr.parent.transform;
        cameraBehaviour = mainCam.GetComponent<CameraBehaviour>();
        weaponCamAnim = weaponCam.GetComponent<Animator>();
        poolManager.Init();
        postProcess.Init();

        Time.timeScale = gameSpeed;
        layerPl = LayerMask.NameToLayer("Player");
        layerEn = LayerMask.NameToLayer("Enemy");
    }
    private void Start()
    {
#if (UNITY_EDITOR)
        if (!SceneManager.GetSceneByBuildIndex(1).isLoaded) SceneManager.LoadScene(1, LoadSceneMode.Additive);
#else
SceneManager.LoadScene(1, LoadSceneMode.Additive);
#endif
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = 1f;
                player.IsActive = true;
            }
            else
            {
                Time.timeScale = 0f;
                player.IsActive = false;

            }
            uiManager.ShowPauseInfo(!player.IsActive);
        }

    }

    void OnDrawGizmos()
    {
       // Gizmos.DrawWireCube(pointForGizmo, 0.2f * Vector3.one);
    }

}
#region GENERAL 
[System.Serializable]
public struct WeaponDetail<T>
{
    [BoxGroup("Weapon details")]
    public T flameArrester;
    [BoxGroup("Weapon details")]
    public T flashlight;
    [BoxGroup("Weapon details")]
    public T scope;
    [BoxGroup("Weapon details")]
    public T silencer;
}
[System.Serializable]
public class DamageOverTime
{
    public ElementType elementType;
    public int damagePerTick;
    public float effectDuration;
}

[System.Serializable]
public class PostprocessMan
{
    [SerializeField] PostProcessProfile mainProfile;
    [SerializeField] Animator playerDamageVol;
    DepthOfField _depth;
    Vignette _vignette;

    public void Init()
    {
        _depth = mainProfile.GetSetting<DepthOfField>();
        _vignette = mainProfile.GetSetting<Vignette>();
        _depth.active = false;
        _vignette.intensity.value = 0.4f;
    }

    public void ShowDepth(bool show)
    {
        if(_depth.active ==  show) return;
        _depth.active = show;
        _vignette.intensity.value = show ? 0.55f : 0.4f;
    }
    public void ShowPlayerDamage()
    {
        playerDamageVol.SetTrigger("hit");
    }
}
public class AttackClass
{
    GameManager _gm;
    Transform _camTr;
    EnemyRef _targetEnemyRef;
    RaycastHit _hit;
    RaycastHit[] _multipleHits = new RaycastHit[10];
    GameObject _projec; 
    IFaction _myFactionInterface;
    HashSet<Collider> _allColliders = new HashSet<Collider>();
    public Transform bulletSpawnPosition;

    Ray MeleeDirection()
    {
        Ray r = new Ray();
        switch (_myFactionInterface.Fact)
        {
            case Faction.Player:
                return _gm.player.offense.RayShooting();
            default:
                r.origin = _myFactionInterface.MyHead.position;
                r.direction = _myFactionInterface.MyTransform.forward;
                break;
        }
        return r;
    }

    Ray ShootDirection()
    {
        Ray r = new Ray();
        switch (_myFactionInterface.Fact)
        {
            case Faction.Player:
                return _gm.player.offense.RayShooting();
            default:
                r.origin = bulletSpawnPosition.position;
                r.direction = bulletSpawnPosition.forward;
                break;
        }

        return r;
    }
    public AttackClass(IFaction factionTarget)
    {
        Ini(factionTarget);
    }
    public AttackClass(IFaction factionTarget, HashSet<Collider> colls, Transform bulletSpawn)
    {
        Ini(factionTarget);
        foreach (Collider item in colls)
        {
            _allColliders.Add(item);
        }
        bulletSpawnPosition = bulletSpawn;
    }
    void Ini(IFaction factionTarget)
    {
        _gm = GameManager.Instance;
        _camTr = _gm.mainCam.transform;
        _myFactionInterface = factionTarget;
        _allColliders.Add(_myFactionInterface.MyCollider);
    }


    public void Attack(SoItem weaponItem)
    {
        if (weaponItem == null) return;
        _targetEnemyRef = null;
        switch (weaponItem.weaponType)
        {
            case WeaponMechanics.Melee:
                int num = Physics.SphereCastNonAlloc(MeleeDirection(), weaponItem.areaOfEffect * 0.5f, _multipleHits, weaponItem.range, _gm.layShooting, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < num; i++)
                {
                    Collider coll = _multipleHits[i].collider;
                    if (coll == null || _allColliders.Contains(coll)) continue;
                   // Debug.Log(coll.name);
                    ApplyDamage(weaponItem, _multipleHits[i], false);
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
                        _projec = _gm.poolManager.GetProjectile(weaponItem.ammoType);
                        bulletSpawnPosition.LookAt(HitPoint(weaponItem));
                        _projec.GetComponent<ProjectilePhysical>().IniThrowable(bulletSpawnPosition, _myFactionInterface.MyCollider);
                        _projec.SetActive(true);
                        break;
                }
                break;
        }

        void SingleRaycast(SoItem weapon, int pellets)
        {
            Vector3 endPosLineRenderer;
            for (int i = 0; i < pellets; i++)
            {
                if(RayHitsSomething(weapon, out Ray ray)) 
                {
                    endPosLineRenderer = _hit.point;
                    ApplyDamage(weapon, _hit, true);
                }
                else
                {
                    endPosLineRenderer = _camTr.position + ray.direction * weapon.range;
                }
                if(_myFactionInterface.Fact == Faction.Enemy) _gm.poolManager.GetLineRenderer(bulletSpawnPosition.position, endPosLineRenderer);
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
            return Physics.Raycast(r, out _hit, weapon.range, _gm.layShooting, QueryTriggerInteraction.Ignore);
        }
    }

    public void ApplyDamage(SoItem weaponItem, RaycastHit hit, bool showBulletHole)
    {
        Collider col = hit.collider;
        if (col == _myFactionInterface.MyCollider || col.GetComponent<DetectableObject>() != null) return;

        if (col.TryGetComponent(out ITakeDamage damagable))
        {
            if (_targetEnemyRef == null || _targetEnemyRef != damagable.EnRef)
            {
                _targetEnemyRef = damagable.EnRef;
                damagable.TakeDamage(ElementType.Normal, HelperScript.Damage(weaponItem.damage), _myFactionInterface.MyTransform, null);
            }
            else return;
        }
        if (col.TryGetComponent(out IMaterial iMat))
        {
            _gm.poolManager.GetImpactObject(iMat.MaterialType, hit, showBulletHole);
        }
        else
        {
            _gm.poolManager.GetImpactObject(MatType.Plaster, hit, showBulletHole);
        }

        _gm.poolManager.GetDetecable(hit.point/* + 0.01f * hit.normal*/, 2f, _myFactionInterface);
    }

    public Vector3 GetLauchVelocity(Vector3 targetPos, Vector3 myPos)
    {
        float height = Mathf.Max(targetPos.y, myPos.y) + 1f;
        float gravity = Physics.gravity.magnitude;

        float displacementY = targetPos.y - myPos.y;
        Vector3 displacementXZ = new Vector3(targetPos.x - myPos.x, 0f, targetPos.z - myPos.z);

        Vector3 velY = Mathf.Sqrt(2 * gravity * height) * Vector3.up;
        Vector3 velXZ = displacementXZ / (Mathf.Sqrt(2 * height / gravity) + Mathf.Sqrt(2 * Mathf.Abs(displacementY - height) / gravity));
        return velXZ + velY;
    }
}


[System.Serializable]
public class UImanager
{
    public Canvas canvasGame;
    [SerializeField] TextMeshProUGUI displayHP, displaySpeed, displayWeaponName, displayAmmo, displayAllAmo, displayPause;
    [SerializeField] Image pain;
    const float CONST_PAINLENGTH = 0.5f;
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
        pain.DOFade(0f, CONST_PAINLENGTH)
            .From(0.5f);

        GameManager.Instance.postProcess.ShowPlayerDamage();
    }
    public void ShowPauseInfo(bool show)
    {
        displayPause.enabled = show;
    }
}

[System.Serializable]
public class PoolManager
{
    [SerializeField] Transform poolDetectables, poolFloatDamage, poolLineRenderers, poolRockets, poolExplosionBig, poolExplosionSmall, poolGreandes, 
        poolSleeveAutomatic, poolSleeveShotgun, poolSleeveSniper, poolSleeve9mm, poolBolts, poolDecalsBlood, poolWildfire,
        poolImpactBlood, poolImpactBrick, poolImpactDirt, poolImpactPlaster, poolImpactRock, poolImpactWater;

    DetectableObject[] _detectables;
    int _cDetectables;
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
    Transform[] _impRock;
    int _cImpRock;
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
    GameObject[] _decalsBlood;
    int _cDecalsBlood;
    Transform[] _wildFire;
    int _cWildFire;

    public void Init()
    {
        _detectables = poolDetectables.GetComponentsInChildren<DetectableObject>();
        _floatingDamage = HelperScript.AllChildrenGameObjects(poolFloatDamage);
        _impBlood = HelperScript.AllChildren(poolImpactBlood);
        _impBrick = HelperScript.AllChildren(poolImpactBrick);
        _impDirt = HelperScript.AllChildren(poolImpactDirt);
        _impPlaster = HelperScript.AllChildren(poolImpactPlaster);
        _impRock = HelperScript.AllChildren(poolImpactRock);
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
        _decalsBlood = HelperScript.AllChildrenGameObjects(poolDecalsBlood);
        _wildFire = HelperScript.AllChildren(poolWildfire);
    }
    public void GetDetecable(Vector3 pos, float size, IFaction ownerInterface)
    {
        DetectableObject detectable = GetGenericObject<DetectableObject>(_detectables, ref _cDetectables, 0);
        detectable.PositionMe(pos, size, ownerInterface);
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
    public GameObject GetBloodDecals()
    {
        GameObject g = GetGenericObject<GameObject>(_decalsBlood, ref _cDecalsBlood, 10000);
        g.SetActive(false);
        g.SetActive(true);
        return g;
    }
    public void GetLineRenderer(Vector3 startPos, Vector3 endPos)
    {
        LineRenderer line = GetGenericObject<LineRenderer>(_lrs, ref _cLR, 200);

        line.positionCount = 2;
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
        line.enabled = true;
    }

    public void GetImpactObject(MatType matType, RaycastHit hit, bool showBulletHole)
    {
        Transform tr = null;
        switch (matType)
        {
            case MatType.Blood:
                tr = GetGenericObject<Transform>(_impBlood, ref _cImpBlood, 2000);
                tr.position = hit.point;
                tr.gameObject.SetActive(true);
                return; //blood particle is different, code below doesn't aplly to it
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
                tr = GetGenericObject<Transform>(_impRock, ref _cImpRock, 3500);
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
        tr.position = hit.point;
        tr.LookAt(hit.point + hit.normal);
        tr.parent = hit.transform;
        tr.GetChild(2).gameObject.SetActive(showBulletHole);
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

        Coroutine kor = null;
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

#region PLAYER CLASSES
[System.Serializable]
public class Controls
{
    Player _player;
    GameManager _gm;
    [SerializeField] LayerMask layersGrounded;
    [SerializeField] float moveSpeed, turnSpeed, jumpForce;
    const int CONST_DOWNFORCE = 2000;
    float _downForce;
    CapsuleCollider _plCapsuleColl;
    bool IsDucked
    {
        get => _isDucked;
        set
        {
            _isDucked = value;
            if (IsDucked)
            {
                _moveDuck = 0.3f;
                _player.camPosition.DOLocalMoveY(camHeights.y, 0.1f)
                    .SetEase(Ease.InFlash);
                _plCapsuleColl.height = 1f;
                _plCapsuleColl.center = 0.5f * Vector3.up;
            }
            else
            {
                _moveDuck = 1f;
                _player.camPosition.DOLocalMoveY(camHeights.x, 0.1f)
                       .SetEase(Ease.InFlash);
                _plCapsuleColl.center = Vector3.up;
                _plCapsuleColl.height = 2f;
            }
        }
    }
    bool CanStandUp()
    {
        if (Physics.Raycast(_player.MyTransform.position + Vector3.up, Vector3.up, 0.1f, layersGrounded)) return false;

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
    float _timerJump;
    [HideInInspector] public float moveAim = 1f;
    [HideInInspector] public float airMove = 1f;
    [HideInInspector] public Vector3 camHeights;
    Rigidbody _rigid;
    float _hor, _ver, _mouseX, _mouseY;
    Vector3 _projectedCamForward, _projectedCamRight, _moveDir;
    RaycastHit hit;
    RaycastHit[] _hitsGrounded = new RaycastHit[1];
    float _lastVelocityY;
    [SerializeField] float fallDamageTreshold = -20f;


    public void Init()
    {
        _gm = GameManager.Instance;
        _player = _gm.player;
        _plCapsuleColl = _player.GetComponent<IFaction>().MyCollider.GetComponent<CapsuleCollider>();
        _rigid = _player.rigid;
        camHeights = new Vector3(1.6f, 0.8f, 0.3f); //normal, duck, dead
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

        if (IsGrounded)
        {
            _timerJump += Time.deltaTime;
            if (Input.GetKey(KeyCode.Space) && _timerJump > 0.2f)
            {
                _timerJump = 0f;
                _rigid.velocity = new Vector3(_rigid.velocity.x, jumpForce, _rigid.velocity.z);
            }
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

        int num = Physics.SphereCastNonAlloc(_player.MyTransform.position, 0.4f, Vector3.down, _hitsGrounded, 0f, layersGrounded);
        IsGrounded = num > 0;

        if (Mathf.Approximately(_moveDir.sqrMagnitude, 0f)/* || !IsGrounded*/) _player.offense.MoveSpeed(MoveType.Stationary);
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
                _player.GetComponent<ITakeDamage>().TakeDamage(ElementType.Normal, -3 * (int)(_lastVelocityY - fallDamageTreshold), null, null);
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
    [SerializeField] GameObject syringe;
    [HideInInspector] public SoItem[] weapons;
    SoItem _currWeapon;
    Animator[] _wAnims;
    Hands[] _hands;
    Transform[] _bulletSpawnPositions;
    [HideInInspector] public Transform[] aimPoints;
    Vector2 _screenCenter;


    public Ray RayShooting()
    {
        Ray r = new Ray();
        if (_gm.player.offense.IsAiming)
        {
            if (_currWeapon.weaponDetail.scope) r.origin = aimPoints[Windex].position;
            else r.origin = _gm.camTr.position;

            r.direction = _gm.camTr.forward;
        }
        else
        {
            r.origin = _gm.camTr.position;
            Vector2 pos = _screenCenter + _gm.uiManager.crosshairObject.Spread * Random.insideUnitCircle;
            r = _gm.mainCam.ScreenPointToRay(pos);
        }


        return r;
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
        }
    }
    int _wi;
    int _nextWeaponIndex;
    bool _isReloading, _healSyringActive; 
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

            if (_currWeapon.hasCrosshair && !_currWeapon.weaponDetail.scope) _gm.uiManager.crosshairObject.IsActive = !_isAiming;
            else _gm.uiManager.crosshairObject.IsActive = false;

            _gm.weaponCamAnim.SetBool("zoom", IsAiming);
        }
    }
    bool _isAiming;

    public void Init(IFaction factionTarget)
    {
        _gm = GameManager.Instance;
        _player = _gm.player;
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

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

        attack = new AttackClass(factionTarget);

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
        _ammoCapacity.Add(AmmoType.HealShot, 10);
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
    public void Death()
    {
        parWeapons.gameObject.SetActive(false);
        syringe.SetActive(false);
    }
    public void MoveSpeed(MoveType moveType)
    {
        switch (moveType)
        {
            case MoveType.Stationary:
                _wAnims[Windex].SetInteger("movePhase", 0);
                _gm.uiManager.crosshairObject.Move(1f);
                break;
            case MoveType.Walk:
                _wAnims[Windex].SetInteger("movePhase", 1);
                _gm.uiManager.crosshairObject.Move(2f);
                break;
            case MoveType.Run:
                _wAnims[Windex].SetInteger("movePhase", 2);
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
    public void HealMethod(GenPhasePos phasePos)
    {
        switch (phasePos)
        {
            case GenPhasePos.Begin:
                if (_healSyringActive || !IdleAnimations() || _ammoCurrent[AmmoType.HealShot] == 0) return;
                _wAnims[Windex].SetTrigger("hide");
                _gm.uiManager.crosshairObject.IsActive = false;
                _healSyringActive = true;
                break;
            case GenPhasePos.Middle:
                break;
            case GenPhasePos.End:
                _ammoCurrent[AmmoType.HealShot]--;
                break;
        }
    }

    public void HideWeapon(int nextWeaponIndex)
    {
        if (!_acquiredWeapons.Contains(nextWeaponIndex) || !IdleAnimations() || _currWeapon == weapons[nextWeaponIndex]) return;
        if (weapons[nextWeaponIndex].ammoType != AmmoType.None && _ammoCurrent[weapons[nextWeaponIndex].ammoType] == 0 && _clipCurrent[weapons[nextWeaponIndex]] == 0) return;

        IsAiming = false;

        _wAnims[Windex].SetTrigger("hide");
        _nextWeaponIndex = nextWeaponIndex;
    }
    public void ReadyWeapon()
    {
        if (_healSyringActive)
        {
            syringe.SetActive(true);
            _healSyringActive = false;
            return;
        }
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
                    AmmoOwerFlow(amm);
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
                AmmoOwerFlow(pickUpAmmo);
                DisplayUIweapons();
                return true;

            case PuType.Health:
                if (_ammoCurrent[AmmoType.HealShot] >= _ammoCapacity[AmmoType.HealShot])
                {
                    return false;
                }

                _ammoCurrent[AmmoType.HealShot]++;
                AmmoOwerFlow(AmmoType.HealShot);
                break;

            case PuType.Armor:
                break;
            case PuType.Key:
                break;
        }

        return false;

        void AmmoOwerFlow(AmmoType ammoType)
        {
            if (_ammoCurrent[ammoType] > _ammoCapacity[ammoType])
            {
                _ammoCurrent[ammoType] = _ammoCapacity[ammoType];
            }
        }
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

#region AI CLASSES
[System.Serializable]
public class FieldOvView
{
    GameManager _gm;
    EnemyRef _eRef;
    EnemyBehaviour _enemyBehaviour;
    IFaction _myIFactionTarget;
    Transform _myTransform;
    [BoxGroup("Field of view")]
    [GUIColor("yellow")]
    [SerializeField] Transform sightSphere, hearSphere;
    float _sightRange, _hearingRange;
    [BoxGroup("Field of view")]
    [GUIColor("yellow")]
    [SerializeField] float sightAngle = 120f;
    float _sightAngleTrigonometry;

    Ray _ray;
    RaycastHit _hit;
    Collider[] _colls = new Collider[30];

    List<IFaction> _allChars = new List<IFaction>();
    List<DetectableObject> _allDetects = new List<DetectableObject>();
    IFaction _currTarget;
    DetectableObject _currDetect;


    bool _conseoleDisplay;
    public void Init(EnemyRef eRef, bool consoleDis)
    {
        _gm = GameManager.Instance;
        _eRef = eRef;
        _enemyBehaviour = _eRef.enemyBehaviour;
        _myIFactionTarget = _eRef.myFactionInterface;
        _myTransform = _myIFactionTarget.MyTransform;
        _sightRange = sightSphere.localScale.x * 0.5f;
        _hearingRange = hearSphere.localScale.x * 0.5f;
        sightSphere.gameObject.SetActive(false);
        hearSphere.gameObject.SetActive(false);
        _sightAngleTrigonometry = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
        _conseoleDisplay = consoleDis;

        _enemyBehaviour.sm.attackState.InitAttackRange(_sightRange);
    }
    public Transform Cover()
    {
        Collider[] colls = new Collider[10];
        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, 10f, colls, _gm.layCover);
        List<Transform> lista = new List<Transform>();

        if (_enemyBehaviour.attackTarget == null) return null;
        Vector3 attackerPos = _enemyBehaviour.attackTarget.MyTransform.position;

        for (int i = 0; i < num; i++)
        {
            Transform colTr = colls[i].transform;
            if (Vector3.Dot(colTr.forward, (colTr.position - attackerPos).normalized) > 0.7) lista.Add(colTr);
        }
        if (lista.Count == 0) return null;

        return HelperScript.GetClosestMember(_myTransform.position, lista);
    }
    float EffectiveRange(Vector3 targetPos)
    {
        float r = _sightRange;
        if (Vector3.Dot(_myIFactionTarget.MyHead.forward, (targetPos - _myTransform.position).normalized) < _sightAngleTrigonometry)
        {
            r = _hearingRange;
        }
        return r;
    }
    public bool TargetVisible(Transform targetTr, Collider targetColl, LayerMask layerMask)
    {
         _ray.direction = (targetTr.position - _myTransform.position).normalized;
        for (int i = 0; i < 2; i++)
        {
            _ray.origin = _myTransform.position + (i + 0.6f) * Vector3.up;
            if (Physics.Raycast(_ray, out _hit, EffectiveRange(targetTr.position), layerMask, QueryTriggerInteraction.Ignore))
            {
                if (_eRef.allColliders.Contains(_hit.collider))
                {
                    Debug.Log(_hit.collider.name);
                    continue;
                }
                if (_hit.collider == targetColl) return true;
            }
        }
        return false;
    }
    public void GetAllTargets(out IFaction tarCharacter, out DetectableObject tarDetectable, ref bool frienDetectsEnemy)
    {
        IFaction character = null;
        DetectableObject detect = null;
        frienDetectsEnemy = false;

        int num = Physics.OverlapSphereNonAlloc(_myTransform.position, _sightRange, _colls, _gm.layFOV_Overlap, QueryTriggerInteraction.Ignore);
        _allChars.Clear();
        _allDetects.Clear();
        for (int i = 0; i < num; i++)
        {
            if (_colls[i].TryGetComponent(out IFaction ifa)) _allChars.Add(ifa);
            else if (_colls[i].TryGetComponent(out DetectableObject det)) _allDetects.Add(det);
        }

        for (int i = 0; i < _allChars.Count; i++)
        {
            _currTarget = _allChars[i];
            if (_currTarget == null ||
            _currTarget == _myIFactionTarget ||
            !TargetVisible(_currTarget.MyTransform, _currTarget.MyCollider, _gm.layFOV_Ray)) continue;

            if (EnemyRef.HostileFaction(_myIFactionTarget.Fact, _currTarget.Fact))
            {
                character = _currTarget;
            }
            else if (_currTarget.MyTransform.TryGetComponent(out EnemyBehaviour en))
            {
                if (en.sm.currentState == en.sm.attackState)
                {
                    character = en.attackTarget;
                    frienDetectsEnemy = true;
                }
                else if (en.sm.currentState == en.sm.searchState)
                {
                    if (_enemyBehaviour.sm.currentState == _enemyBehaviour.sm.searchState || _enemyBehaviour.hasSearched)
                    {
                        character = null;
                    }
                    else
                    {
                        detect = en.detectObject;
                    }

                }
            }

            if (character != null) break;
        }

        for (int i = 0; i < _allDetects.Count; i++)
        {
            if (character != null) break;

            _currDetect = _allDetects[i];
            if (_currDetect == null ||
                _currDetect.owner == null ||
                _currDetect.owner == _myIFactionTarget ||
                !EnemyRef.HostileFaction(_myIFactionTarget.Fact, _currDetect.owner.Fact) ||
                !TargetVisible(_currDetect.myTransform, _currDetect.myCollider, _gm.layFOV_RayAll)) continue;

            detect = _currDetect;
            break;
        }

        tarCharacter = character;
        tarDetectable = detect;
    }
}
public class RagToAnimTranstions
{
    struct BoneTransforms
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    EnemyRef _eRef;
    Transform _myTransform;
    Animator _anim;
    readonly string[] _clipNames = { "Stand Up", "Zombie Stand Up" };
    string _currentClip;
    Rigidbody[] _ragdollRigids;
    Transform _hipsBone;

    List<Transform> _bones = new List<Transform>();
    BoneTransforms[] _standingFaceUpBones;
    BoneTransforms[] _standingFaceDownBones;
    BoneTransforms[] _standingCurrent;
    BoneTransforms[] _ragdollBones;

    bool FacingUp() => _hipsBone.forward.y > 0f;
    bool _readyToStandUp;
    const float CONST_TIMETOSTANDUP = 1f;
    float _timer;
    float _elapsedPercentage;

    public RagToAnimTranstions(EnemyRef eRef, Transform[] ragParts)
    {
        _eRef = eRef;
        _myTransform = _eRef.animTr;
        _anim = _eRef.anim;

        _hipsBone = _anim.GetBoneTransform(HumanBodyBones.Hips);

        _ragdollRigids = new Rigidbody[ragParts.Length];
        for (int i = 0; i < ragParts.Length; i++)
        {
            _ragdollRigids[i] = ragParts[i].GetComponent<Rigidbody>();
        }

        Transform[] bon = _hipsBone.GetComponentsInChildren<Transform>();
        for (int i = 0; i < bon.Length; i++)
        {
            if (bon[i].name.StartsWith("mixamorig")) _bones.Add(bon[i]);
        }
        _standingFaceUpBones = new BoneTransforms[_bones.Count];
        _standingFaceDownBones = new BoneTransforms[_bones.Count];
        _ragdollBones = new BoneTransforms[_bones.Count];

        PopulateAnimationBones(true);
        PopulateAnimationBones(false);

    }

    public void RagdollStandingUp()
    {
        if (!_readyToStandUp) return;

        _timer += Time.deltaTime;
        _elapsedPercentage = _timer / CONST_TIMETOSTANDUP;
        for (int i = 0; i < _bones.Count; i++)
        {
            _bones[i].localPosition = Vector3.Lerp(_ragdollBones[i].pos, _standingCurrent[i].pos, _elapsedPercentage);
            _bones[i].localRotation = Quaternion.Lerp(_ragdollBones[i].rot, _standingCurrent[i].rot, _elapsedPercentage);
        }

        if (_elapsedPercentage >= 1f)
        {
            _readyToStandUp = false;
            _anim.Play(_currentClip, 0, 0);
            _timer = 0f;
            _anim.enabled = true;
            for (int i = 0; i < _ragdollRigids.Length; i++)
            {
                _ragdollRigids[i].isKinematic = true;
            }
        }

    }

    public void RagdollMe(Rigidbody ragRigid, Transform attackerTr)
    {
        if (!_readyToStandUp)
        {
            for (int i = 0; i < _ragdollRigids.Length; i++)
            {
                _ragdollRigids[i].isKinematic = false;
            }

            Vector3 dir = (attackerTr.position - _myTransform.position).normalized;
            ragRigid.AddForce(-40f * dir, ForceMode.VelocityChange);
            _anim.enabled = false;
            _eRef.enemyBehaviour.sm.ChangeState(_eRef.enemyBehaviour.sm.immobileState);
            BeginStandUp();
        }

    }
    async void BeginStandUp()
    {
        await Task.Delay(2000);
        _readyToStandUp = true;
        _standingCurrent = FacingUp() ? _standingFaceUpBones : _standingFaceDownBones;
        AlignRotationToHips();
        AlignPositionToHips();
        PopulateBones(_ragdollBones);
        _currentClip = FacingUp() ? _clipNames[0] : _clipNames[1];

    }

    //void ActivateRagdoll(bool activ)
    //{
    //    for (int i = 0; i < _ragdollRigids.Length; i++)
    //    {
    //        _ragdollRigids[i].isKinematic = !activ;
    //    }
    //    if (activ)
    //    {
    //        if (!_readyToStandUp)
    //        {
    //            _ragdollRigids[8].AddForce(-40f * Vector3.forward, ForceMode.VelocityChange);
    //            _anim.enabled = false;
    //        }
    //    }
    //    else
    //    {
    //        _standingCurrent = FacingUp() ? _standingFaceUpBones : _standingFaceDownBones;
    //        AlignRotationToHips();
    //        AlignPositionToHips();
    //        PopulateBones(_ragdollBones);
    //        _readyToStandUp = true;
    //        _currentClip = FacingUp() ? _clipNames[0] : _clipNames[1];
    //    }
    //}
    void PopulateBones(BoneTransforms[] bon)
    {
        for (int i = 0; i < _bones.Count; i++)
        {
            bon[i].pos = _bones[i].localPosition;
            bon[i].rot = _bones[i].localRotation;
        }
    }
    void PopulateAnimationBones(bool isFacingUp)
    {
        Vector3 posBeforeSampling = _myTransform.position;
        Quaternion rotBeforeSampling = _myTransform.rotation;

        _currentClip = isFacingUp ? _clipNames[0] : _clipNames[1];

        foreach (AnimationClip item in _anim.runtimeAnimatorController.animationClips)
        {
            if (item.name == _currentClip)
            {
                item.SampleAnimation(_myTransform.gameObject, 0f);
                PopulateBones(isFacingUp ? _standingFaceUpBones : _standingFaceDownBones);
                break;
            }
        }

        _myTransform.position = posBeforeSampling;
        _myTransform.rotation = rotBeforeSampling;
    }

    private void AlignRotationToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        Quaternion originalHipsRotation = _hipsBone.rotation;

        Vector3 desiredDirection = _hipsBone.up * -1;
        if (!FacingUp()) desiredDirection *= -1f;
        desiredDirection.y = 0;
        desiredDirection.Normalize();

        Quaternion fromToRotation = Quaternion.FromToRotation(_myTransform.forward, desiredDirection);
        _myTransform.rotation *= fromToRotation;

        _hipsBone.position = originalHipsPosition;
        _hipsBone.rotation = originalHipsRotation;
    }

    private void AlignPositionToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        _myTransform.position = _hipsBone.position;

        Vector3 positionOffset = _standingCurrent[0].pos;
        positionOffset.y = 0;
        positionOffset = _myTransform.rotation * positionOffset;
        _myTransform.position -= positionOffset;

        if (Physics.Raycast(_myTransform.position, Vector3.down, out RaycastHit hitInfo))
        {
            _myTransform.position = new Vector3(_myTransform.position.x, hitInfo.point.y, _myTransform.position.z);
        }

        _hipsBone.position = originalHipsPosition;
    }

}

#endregion






























































