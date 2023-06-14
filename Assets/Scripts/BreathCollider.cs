using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;

public class BreathCollider : MonoBehaviour
{
    PoolManager _poolManager;
    HashSet<Collider> _collidersToIgnore = new HashSet<Collider>();
    IFaction _owner;
    SoItem _weapon;
    Transform _wildFire;
    Transform _myTransform, _actingTransform;

    public void Init(IFaction Owner, Collider[] colIgnore, SoItem wea)
    {
        for (int i = 0; i < colIgnore.Length; i++)
        {
            _collidersToIgnore.Add(colIgnore[i]);
        }

        _owner = Owner;
        _weapon = wea;
        _poolManager = GameManager.Instance.poolManager;
        _myTransform = transform;
        _actingTransform = Owner == null ? transform : Owner.MyTransform;
    }

    public void SpawnFire(Vector3 point)
    {
        if (_wildFire == null)
        {
            _wildFire = _poolManager.GetWildFire();
            _wildFire.GetComponent<ParticleSystem>().Play();
        }

        _wildFire.position = point;
    }
    public void StopFire()
    {
        _wildFire = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ITakeDamage damagable) && !_collidersToIgnore.Contains(other))
        {
            damagable.TakeDamage(ElementType.Fire, HelperScript.Damage(_weapon.damage), _actingTransform, _weapon.damageOverTime);
          //  print(other.name);
        }
    }

}
