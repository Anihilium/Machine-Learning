using UnityEngine;
using System.Collections;

public class ExitTrigger : MonoBehaviour {

    [HideInInspector] public GameMgr gameMgr;
    public bool IsLeftSide = true;

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            gameMgr.OnBallExit(IsLeftSide);
        }
    }
}
