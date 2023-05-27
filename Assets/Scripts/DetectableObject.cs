using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableObject : MonoBehaviour, IFactionTarget
{
    [field: SerializeField] public Transform MyTransform { get ; set; }
    [field: SerializeField] public Transform MyHead { get; set; }
    public Faction Fact { get; set; }
    //public Faction Fact {
    //    get => _fact;
    //    set
    //    {
    //        _fact = value;
    //        switch (value)
    //        {
    //            case Faction.Player:
    //                _myGameObject.layer = _gm.layerPl;
    //                break;
    //            case Faction.Enemy:
    //                _myGameObject.layer = _gm.layerEn;
    //                break;
    //        }
    //    }
    //}
    //Faction _fact;
    public IFactionTarget Owner { get; set; }

    GameManager _gm;
    GameObject _myGameObject;
    [SerializeField] SphereCollider _collider;

    void Awake()
    {
        _gm = GameManager.gm;
        _myGameObject = gameObject;
        _collider.enabled = false;
    }

    public void PositionMe(Vector3 pos, float size, IFactionTarget ownerInterface)
    {
        MyTransform.position = pos;
        MyTransform.localScale = size * Vector3.one;
        Fact = ownerInterface.Fact;
        Owner = ownerInterface;
        _collider.enabled = true;
        Invoke(nameof(EndMe), 1f);
    }
    void EndMe()
    {
        _collider.enabled = false;
    }
}
