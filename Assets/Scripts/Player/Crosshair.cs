using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Crosshair : MonoBehaviour, IActivation
{
    GameManager _gm;
    [SerializeField] RectTransform[] _lineTransforms;
    public float Spread
    {
        get => _spread;
        set
        {
            if (!IsActive) return;
            _spread = value;
            _spread = Mathf.Clamp(_spread, _weapon.startSpread, 500f);

            _lineTransforms[0].localPosition = _spread * Vector3.right;
            _lineTransforms[1].localPosition = -_spread * Vector3.right;
            _lineTransforms[2].localPosition = _spread * Vector3.up;
            _lineTransforms[3].localPosition = -_spread * Vector3.up;
        }
    }
    float _spread;

    public SoItem Weapon
    {
        get => _weapon;
        set
        {
            _weapon = value;
            if (value.hasCrosshair)
            {
                IsActive = !value.weaponDetail.scope;
            }
            else
            {
                IsActive = false;
            }
            Spread = _shootSpread = value.startSpread;

        }
    }
    SoItem _weapon;
    float _shootSpread;
    float _moveSpread = 1f;
    float _timer;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            for (int i = 0; i < 4; i++)
            {
                _lineTransforms[i].gameObject.SetActive(value);
            }
        }
    }
    bool _isActive;

    void Awake()
    {
        _gm = GameManager.Instance;
    }

    void Update()
    {
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
            if(_timer < 0)
            {
                DOTween.To(() => _shootSpread, x => _shootSpread = x, _weapon.startSpread, 0.15f);
            }
        }
        Spread = _shootSpread * _moveSpread;
    }
    public void Shoot()
    {
        DOTween.To(() => _shootSpread, x => _shootSpread = x, _shootSpread + 10f, 0.05f);
        _timer = 0.5f;
        _gm.cameraBehaviour.Recoil(_weapon.recoilAmmount);

    }
    public void Move(float newMoveSpread)
    {
        _moveSpread = Mathf.MoveTowards(_moveSpread, newMoveSpread, 5f * Time.deltaTime);
    }
}
