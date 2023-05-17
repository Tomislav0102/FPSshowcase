using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstCollection;

public class GlobalEventManager : MonoBehaviour
{
    public static System.Action PlayerDead;
  //  public static System.Action<bool> PlayerAiming;


    protected virtual void OnEnable()
    {
        PlayerDead += Death;
      //  PlayerAiming += Aiming;
    }
    protected virtual void OnDisable()
    {
        PlayerDead -= Death;
      //  PlayerAiming -= Aiming;
    }

    protected virtual void Death()
    {

    }
    //protected virtual void Aiming(bool isAiming)
    //{
        
    //}
}
