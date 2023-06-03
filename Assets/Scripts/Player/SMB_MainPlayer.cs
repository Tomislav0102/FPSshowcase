using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMB_MainPlayer : StateMachineBehaviour
{
    internal GameManager _gm;
    internal Player _player;
    internal Offense _offense;
    internal SoItem _currentWeapon;

    private void Awake()
    {
        Init();
    }
    protected virtual void Init()
    {
        _gm = GameManager.Instance;
        _player = _gm.player;
        _offense = _player.offense;
        _currentWeapon = _offense.weapons[_offense.Windex];
    }
}
