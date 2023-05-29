using DG.Tweening;
using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Player : GlobalEventManager, IActivation, IFactionTarget, IMaterial
{
    GameManager _gm;
    public Transform camPosition;
    public CapsuleCollider capsuleCollider;
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
    public IFactionTarget Owner { get; set; }
    [field: SerializeField] public MatType MaterialType { get; set; }
    public Transform MyHead { get => _gm.camTr; set { } }

    bool _isActive;

    public Controls controls;
    public Offense offense;
    readonly KeyCode[] _alfaKeys = new KeyCode[9];


    bool _isDead;

    void Awake()
    {
        _gm = GameManager.gm;

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
    protected override void Death()
    {
        IsActive = false;
        _gm.camRigTr.DOLocalMoveY(controls._camHeights.z, 1.5f)
              .SetSpeedBased(true)
              .SetEase(Ease.OutBounce);
    }

    void Start()
    {
        Invoke(nameof(BeganActive), 1f);
        offense.AddWeapon(offense.weapons[1]);
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

      //  offense.BeginAttackAnimation(Input.GetMouseButtonDown(0));
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

