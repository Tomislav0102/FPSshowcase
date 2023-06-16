using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

    public class HelperScript
    {

        static Camera _cam;
        public static Camera Cam
        {
            get
            {
                if (_cam == null) _cam = Camera.main;
                return _cam;
            }
        }
        public static Vector3 MousePoz(Camera cam, LayerMask lay)
        {
            Vector3 v3 = Vector3.up;
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, lay))
            {
                //  Debug.Log(hit.collider.name);
                v3 = new Vector3(hit.point.x, Mathf.Clamp(hit.point.y, -3.43f, hit.point.y), hit.point.z);
            }

            return v3;
        }

        public static Vector2 GetWorldPositionOfCanvasElement(RectTransform rectElement) //sets gameobject behind the UI element
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(rectElement, rectElement.position, null, out Vector3 result);
            return result;
        }

        static readonly Dictionary<float, WaitForSeconds> _waitDictionary = new Dictionary<float, WaitForSeconds>();
        public static WaitForSeconds GetWait(float time)
        {
            if (_waitDictionary.TryGetValue(time, out WaitForSeconds wait)) return wait;
            _waitDictionary[time] = new WaitForSeconds(time);
            return _waitDictionary[time];
        }

        public static void CursorVisible(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }
        public static GameObject[] AllChildrenGameObjects(Transform parGos)
        {
            GameObject[] gos = new GameObject[parGos.childCount];
            for (int i = 0; i < gos.Length; ++i)
            {
                gos[i] = parGos.GetChild(i).gameObject;
            }
            return gos;
        }
        public static Transform[] AllChildren(Transform parTransform)
        {
            Transform[] childTransforms = new Transform[parTransform.childCount];
            for (int i = 0; i < childTransforms.Length; ++i)
            {
                childTransforms[i] = parTransform.GetChild(i);
            }
            return childTransforms;
        }
        public static List<int> RandomList(int size)
        {
            List<int> brojevi = Enumerable.Range(0, size).ToList();
            var rnd = new System.Random();
            var randNums = brojevi.OrderBy(n => rnd.Next());
            List<int> list = new List<int>();
            foreach (var item in randNums)
            {
                list.Add(item);
            }

            return list;
        }
        public static List<T> RandomListByType<T>(List<T> pocetna)
        {
            var rnd = new System.Random();
            var randNums = pocetna.OrderBy(n => rnd.Next());
            List<T> list = new List<T>();
            foreach (var item in randNums)
            {
                list.Add(item);
            }

            return list;
        }
        public static int Damage(Vector2Int dam)
        {
            return Random.Range(dam.x, dam.y);
        }
    }
    #region//ENUMS
    public enum Faction
    {
        Player,
        Enemy,
        Ally
    }
    public enum EnemyState
    {
        Idle,
        Patrol,
        Roam,
        Search,
        Attack,
        Follow,
        Immobile,
        Flee
    }
    public enum BodyPartRagdoll
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    }
    public enum EnemyWeaponUsed
    {
        Melee,
        Pistol,
        Rifle
    }
    public enum ElementType
    {
        Normal,
        Fire,
        Explosion,
        Cold,
        Electricity,
        Poison
    }
    public enum ExplosionType
    {
        Big,
        Small
    }
    public enum MoveType
    {
        Stationary,
        Walk,
        Run
    }
    public enum GenPhasePos
    {
        Begin,
        Middle,
        End
    }
    public enum PuType
    {
        Weapon,
        Ammo,
        Health,
        Armor,
        Key
    }
    public enum KeyType
    {
        Blue,
        Red,
        Green
    }
    public enum WeaponMechanics
    {
        Melee, //all melee - spherecast
        Gun, //standard ranged - raycast
        Shotgun, //ranged with spread - multiple raycasts
        BreathWeapon, //ranged - trigger collider with ray that detects obstacles
        Thrown //ranged - physcal object moving (bow, grenade, bazooka)
    }
    public enum AmmoType
    {
        None,
        //ray
        _9mm,
        _44cal,
        _762mm,
        _303REM,
        _12gauge,
        //ray
        Rocket,
        HandGrenade,
        Bolt,
        Fuel,
        HealShot
    }
    public enum MatType //used for hit particles
    {
        Blood,
        Brick,
        Concrete,
        Dirt,
        Foliage,
        Glass,
        Metal,
        Plaster,
        Rock,
        Water,
        Wood,
        NoMaterial
    }
    #endregion

    #region//INTERFACES
    public interface IFaction 
    {
        Transform MyTransform { get; set; }
        Transform MyHead { get; set; }
        Collider MyCollider { get; set; }
        Faction Fact { get; set; }
    }

    public interface IMaterial
    {
        MatType MaterialType { get; set; }
    }
    public interface IActivation
    {
        bool IsActive { get; set; }
    }
    public interface ITakeDamage
    {
        void TakeDamage(ElementType elementType, int damage, Transform attackerTransform, DamageOverTime damageOverTime);
        bool IsDead { get; set; }
        EnemyRef EnRef { get; set; }
    }
    #endregion

