using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;

public class Enemy : MonoBehaviour, IMaterial/*, ITakeDamage*/
{
    GameManager _gm;
    Transform _myTransform;
    [field:SerializeField]
    public MatType MaterialType { get; set; }
    //public bool IsDead
    //{
    //    get => _isDead;
    //    set
    //    {
    //        _isDead = value;
    //        gameObject.SetActive(false);
    //    }
    //}
    //bool _isDead;

    //[SerializeField] HealthClass _health;

    void Awake()
    {
        _gm = GameManager.gm;
        _myTransform = transform;
    }

    //public void TakeDamage(ElementType elementType, int damage, System.Action<ElementType, int> ddamCallback)
    //{
        
    //    //  print("hit");
    //        _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString());

    //    _health.TakeDamage(elementType, damage, (ElementType e, int b) =>
    //    {
    //        switch (e)
    //        {
    //            case ElementType.Fire:
    //                _gm.poolManager.GetFloatingDamage(_myTransform.position, b.ToString());
    //                break;
    //        }
    //    });
    //}


    //public void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, bool DOT)
    //{
    //    _gm.poolManager.GetFloatingDamage(_myTransform.position, damage.ToString());
    //}
}
