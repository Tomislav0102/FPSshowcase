using FirstCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableObject : MonoBehaviour, IFaction
{
    [field: SerializeField] public Transform MyTransform { get ; set; }
    public IFaction Owner { get; set; }
    [field: SerializeField] public Collider MyCollider { get; set; }
    public Transform MyHead { get; set; }
    public Faction Fact { get; set; }

    GameManager _gm;
    GameObject _myGameObject;

    void Awake()
    {
        _gm = GameManager.Instance;
        _myGameObject = gameObject;
        MyCollider.enabled = false;
    }

    public void PositionMe(Vector3 pos, float size, IFaction ownerInterface)
    {
        MyTransform.position = pos;
        MyTransform.localScale = size * Vector3.one;
        HookInterface(ownerInterface);
        Invoke(nameof(EndMe), 1f);
    }
    public void HookInterface(IFaction ownerInterface)
    {
        Owner = ownerInterface;
        Fact = ownerInterface.Fact;
        MyCollider.enabled = true;
    }
    void EndMe()
    {
        MyCollider.enabled = false;
    }
}
