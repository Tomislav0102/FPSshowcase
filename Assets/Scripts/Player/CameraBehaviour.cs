using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraBehaviour : GlobalEventManager
{
    [HideInInspector] public float speed;
    Transform _myTransform;
    Transform _parTransform;
    float _posY;
    readonly Vector2 _range = new Vector2(0f, 0.2f);
    bool _up = true;
    bool _active = true;

    float _recoilAngle;
    public float coolDownSpeed, recoilSpeed;
    private void Awake()
    {
        _myTransform = transform; 
        _parTransform = transform.parent;
    }

    protected override void CallEv_PlayerDead()
    {
        base.CallEv_PlayerDead();
        _active = false;
    }

    private void LateUpdate()
    {
        if (!_active) return;
        _posY += (_up ? 1 : -1) * speed * Time.deltaTime;
        if (_posY < _range.x) _up = true;
        if (_posY > _range.y) _up = false;
        _myTransform.position = _parTransform.position + _posY * Vector3.up;

        if (_recoilAngle < 0f) _recoilAngle += Time.deltaTime * coolDownSpeed;
        _myTransform.localEulerAngles = _recoilAngle * Vector3.right;
    }

    public void Recoil(GenAmmount recoilAmmount)
    {
        if (!_active) return;

        float intensity = 0;
        switch (recoilAmmount)
        {
            case GenAmmount.None:
                break;
            case GenAmmount.Light:
                intensity = 4f;
                break;
            case GenAmmount.Medium:
                intensity = 5f;
                break;
            case GenAmmount.Heavy:
                intensity = 10f;
                break;
        }
        _recoilAngle -= intensity;
    }
}
