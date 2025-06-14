using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Player : GlobalEventManager, IActivation, IFaction, IMaterial
{
    GameManager _gm;
    public Transform camPosition;
    public Rigidbody rigid;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            controls.Activation(_isActive);
        }
    }

    [field: SerializeField] public Transform MyTransform { get; set; }
    [field:SerializeField] public Faction Fact { get; set; }
    [field: SerializeField] public Collider MyCollider { get; set; }
    [field: SerializeField] public MatType MaterialType { get; set; }
    public Transform MyHead { get => _gm.camTr; set { } }

    bool _isActive;

    [BoxGroup("Controls")]
    [HideLabel]
    [GUIColor("cyan")]
    public Controls controls;
    [BoxGroup("Offense")]
    [GUIColor("orange")]
    [HideLabel]
    public Offense offense;
    readonly KeyCode[] _alfaKeys = new KeyCode[9];


    bool _isDead;

    void Awake()
    {
        _gm = GameManager.Instance;

        HelperScript.CursorVisible(false);
        controls.Init();
        offense.Init(this);

        _alfaKeys[0] = KeyCode.Alpha1;
        _alfaKeys[1] = KeyCode.Alpha2;
        _alfaKeys[2] = KeyCode.Alpha3;
        _alfaKeys[3] = KeyCode.Alpha4;
        _alfaKeys[4] = KeyCode.Alpha5;
        _alfaKeys[5] = KeyCode.Alpha6;
        _alfaKeys[6] = KeyCode.Alpha7;
        _alfaKeys[7] = KeyCode.Alpha8;
        _alfaKeys[8] = KeyCode.Alpha9;
    }
    protected override void CallEv_PlayerDead()
    {
        IsActive = false;
        MyCollider.enabled = false;
        offense.Death();
        _gm.camRigTr.DOLocalMoveY(controls.camHeights.z, 1.5f)
              .SetSpeedBased(true)
              .SetEase(Ease.OutBounce);
    }

    void Start()
    {
        Invoke(nameof(BeganActive), 1f);
        for (int i = 1; i < 11; i++)
        {
            offense.AddWeapon(offense.weapons[i]);
        }
    }
    void BeganActive()
    {
        IsActive = true;
    }

    void Update()
    {

        if (!IsActive) return;

        controls.Motion();

        for (int i = 0; i < _alfaKeys.Length; i++)
        {
            if (Input.GetKeyDown(_alfaKeys[i])) offense.HideWeapon(i);
        }

        if (Input.mouseScrollDelta.y != 0f) offense.ChangeWeaponMouseScroll(Input.mouseScrollDelta.y > 0f);

        offense.BeginAttackAnimation(Input.GetMouseButton(0));

        if (Input.GetKeyDown(KeyCode.R)) offense.BeginReloadAnimation();
        if (Input.GetMouseButtonDown(1)) offense.IsAiming = !offense.IsAiming;
        if (Input.GetKeyDown(KeyCode.H)) offense.HealMethod(GenPhasePos.Begin);

    }
    void FixedUpdate()
    {
        if (!IsActive) return;
        controls.MotionFixedUpdate();
    }
    void LateUpdate()
    {
        if (!IsActive) return;
        _gm.camRigTr.position = camPosition.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PickUp pu))
        {
            if (offense.CanPickUP(pu)) pu.OnPickup();
        }
    }

}

