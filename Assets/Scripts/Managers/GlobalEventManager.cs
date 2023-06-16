using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalEventManager : MonoBehaviour
{
    public static System.Action PlayerDead;
  //  public static System.Action<bool> PlayerAiming;


    protected virtual void OnEnable()
    {
        PlayerDead += CallEv_PlayerDead;
      //  PlayerAiming += Aiming;
    }
    protected virtual void OnDisable()
    {
        PlayerDead -= CallEv_PlayerDead;
      //  PlayerAiming -= Aiming;
    }

    protected virtual void CallEv_PlayerDead()
    {

    }
    //protected virtual void Aiming(bool isAiming)
    //{
        
    //}
}
