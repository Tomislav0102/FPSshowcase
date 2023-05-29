using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;
using System.Linq;

public class ProjectilePhysical : MonoBehaviour, IMaterial, ITakeDamage
{
    GameManager _gm;
    Rigidbody _rigid;
    Transform _myTransform;
    [SerializeField] SoItem weaponUsingThisProjectile;
    ElementType _elType;
    [field:SerializeField] public MatType MaterialType { get; set; }
    public bool IsDead { get; set; }

    [SerializeField] float forceSpeed = 50f;
    HashSet<Collider> _colliders = new HashSet<Collider>();
    RaycastHit[] _hitsForContactPoint = new RaycastHit[1];

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
        _gm = GameManager.gm;
        _myTransform = transform;
        ReturnToPool();
    }
    public void IniThrowable(Transform spawPoint, HashSet<Collider> colsToIgnore)
    {
        _myTransform.SetPositionAndRotation(spawPoint.position, spawPoint.rotation);
        _colliders = colsToIgnore;
        switch (weaponUsingThisProjectile.ammoType)
        {
            case AmmoType.Rocket:
                _elType = ElementType.Explosion;
                break;
            case AmmoType.HandGrenade:
                _elType = ElementType.Explosion;
                break;
            case AmmoType.Fuel:
                _elType = ElementType.Fire;
                break;
            default:
                _elType = ElementType.Normal;
                break;
        }
    }
    void OnEnable()
    {
        _rigid.AddRelativeForce(forceSpeed * Vector3.forward, ForceMode.VelocityChange);
        Invoke(nameof(ReturnToPool), 10f);
        
        if (weaponUsingThisProjectile.ammoType != AmmoType.HandGrenade) return;
        _rigid.AddTorque(1000f * new Vector3(Random.value, Random.value, Random.value));
    }

    public void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime)
    {
        if (damage > 0)
        {
            switch (weaponUsingThisProjectile.ammoType)
            {
                case AmmoType.Rocket:
                    _gm.poolManager.GetExplosion(ExplosionType.Big, _myTransform.position);
                    break;
                case AmmoType.HandGrenade:
                    _gm.poolManager.GetExplosion(ExplosionType.Small, _myTransform.position);
                    break;
            }

            ReturnToPool();
        }

    }

    void ReturnToPool()
    {
        CancelInvoke();
        _rigid.velocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    RaycastHit GetHit()
    {
        Physics.RaycastNonAlloc(_myTransform.position - 2 * _myTransform.forward, _myTransform.forward, _hitsForContactPoint, 2.1f, _gm.layAllWithoutDetectables);
        return _hitsForContactPoint[0];
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_colliders.Contains(other) || other.isTrigger) return;

        switch (weaponUsingThisProjectile.ammoType)
        {
            case AmmoType.Bolt:
                _gm.player.offense.attack.ApplyDamage(weaponUsingThisProjectile, GetHit(), false);
                break;
            default:
                print($"Collision with {other.name}. I AM EXPLODING NOW!!!");
                Collider[] allAffectedCollider = Physics.OverlapSphere(_myTransform.position, weaponUsingThisProjectile.areaOfEffect);
                foreach (Collider item in allAffectedCollider)
                {
                    if (item.TryGetComponent(out ITakeDamage damagable))
                    {
                        damagable.TakeDamage(_elType, HelperScript.Damage(weaponUsingThisProjectile.damage), _myTransform, null);
                    }
                    if (item.TryGetComponent(out Rigidbody rigids) && !rigids.isKinematic)
                    {
                        rigids.AddExplosionForce(200f, _myTransform.position, weaponUsingThisProjectile.areaOfEffect, 0.2f);
                    }
                }
                _gm.poolManager.GetExplosion(weaponUsingThisProjectile.ammoType == AmmoType.Rocket ? ExplosionType.Big : ExplosionType.Small, _myTransform.position);
                break;
        }

        ReturnToPool();
    }

}
