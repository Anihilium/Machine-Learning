using UnityEngine;
using System.Collections;

public class PaddleController : MonoBehaviour
{
    [HideInInspector] public GameMgr gameMgr;

    [HideInInspector] public int Score;

    public float Speed = 8f;

    protected float Height = 1f;
    protected float Border = 0f;

    protected bool isP1;

    virtual protected void Start()
    {
        isP1 = gameObject.name.Contains('1');
        Height = transform.localScale.y;
        Border = gameMgr.CourtHeight / 2f - Height / 2f;
    }

    protected void MoveUp(float _multiplier = 1f)
    {
        if (transform.localPosition.y < Border)
        {
            transform.Translate(Speed * _multiplier * Time.deltaTime * Vector3.up);
        }
    }

    protected void MoveDown(float _multiplier = 1f)
    {
        if (transform.localPosition.y > -Border)
        {
            transform.Translate(Speed * _multiplier * Time.deltaTime * Vector3.down);
        }
    }

    protected void Move(float _multiplier)
    {
        if(_multiplier > 0f)
        {
            MoveUp(Mathf.Abs(_multiplier));
        }
        else if(_multiplier < 0f)
        {
            MoveDown(Mathf.Abs(_multiplier));
        }
    }

    public float GetPos0to1()
    {
        return transform.localPosition.y / (2f * Border) + 1f;
    }
}
