using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableObject : MonoBehaviour, IFactionTarget
{
    public Transform MyTransform { get ; set; }
    public Transform MyHead { get; set; }
    public Faction Fact {
        get => _fact;
        set
        {
            _fact = value;
            switch (value)
            {
                case Faction.Player:
                    _myGameObject.layer = _gm.layerPl;
                    break;
                case Faction.Enemy:
                    _myGameObject.layer = _gm.layerEn;
                    break;
            }
        }
    }
    Faction _fact;

    GameManager _gm;
    GameObject _myGameObject;
    Transform _myTransform;
    SphereCollider _collider;

    void Awake()
    {
        _gm = GameManager.gm;
        _myGameObject = gameObject;
        _myTransform = transform;
        _collider = GetComponent<SphereCollider>();
        _collider.enabled = false;
    }

    public void PositionMe(Vector3 pos, Faction ownerFaction, Transform ownerTransform)
    {
        _myTransform.position = pos;
        Fact = ownerFaction;
        MyTransform = ownerTransform;
        _collider.enabled = true;
        Invoke(nameof(EndMe), 0.5f);
    }
    void EndMe()
    {
        _collider.enabled = false;
    }
}
