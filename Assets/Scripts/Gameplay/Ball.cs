using UnityEngine;

public class Ball : MonoBehaviour
{
    [HideInInspector] public GameMgr gameMgr;
    public float InitialSpeed = 10f;
    public float MaxSpeed = 30f;
    public float HitAcceleration = 1f;
    private float currentSpeed;

    [HideInInspector] public Rigidbody2D rigidBody;
    private Transform parentTransform;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        parentTransform = transform.parent;
    }

    public void Launch(bool _randAngle = false)
    {
        float angle = gameMgr.bP1LastScored ? Mathf.PI : 0f;

        if(_randAngle)
        {
            angle = Random.Range(-Mathf.PI / 8f, Mathf.PI / 8f) + (gameMgr.bP1LastScored ? Mathf.PI : 0f);
        }

        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        rigidBody.velocity = dir * InitialSpeed;
        currentSpeed = InitialSpeed;
    }

    void Update()
    {
        if (IsBallStuck())
            ForceBallBounceBack();
        else if (IsBallOut())
            transform.position = parentTransform.position;
    }

    public Vector2 GetPos0to1()
    {
        return new Vector2(transform.localPosition.x / (2f * gameMgr.CourtHeight) + 1f, transform.localPosition.y / (2f * gameMgr.CourtHeight) + 1f);
    }

    public Vector2 GetDir0to1()
    {
        Vector3 dir = rigidBody.velocity.normalized;
        return new Vector2(dir.x / 2f + 1f, dir.y / 2f + 1f);
    }

    public Vector2 GetDir()
    {
        return new Vector2(rigidBody.velocity.x, rigidBody.velocity.y);
    }

    public float GetSpeed0to1()
    {
        return (currentSpeed - InitialSpeed) / (MaxSpeed - InitialSpeed);
    }

    private bool IsBallStuck()
    {
        return gameMgr.IsBallLaunched && rigidBody.velocity.magnitude <= 0.001f;
    }

    private bool IsBallOut()
    {
        return Mathf.Abs(transform.position.x - parentTransform.position.x) > 15f || Mathf.Abs(transform.position.y - parentTransform.position.y) > 15f;
    }

    private void ForceBallBounceBack()
    {
        rigidBody.velocity = Vector2.left * currentSpeed;
        Vector3 newPos = transform.position;
        newPos.x -= transform.localScale.x;
        transform.position = newPos;
    }

    float ComputeHitFactor(Vector2 racketPos, float racketHeight)
    {
        return (transform.position.y - racketPos.y) / racketHeight;
    }


    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.tag != "Player")
            return;

        if (IsBallStuck())
            ForceBallBounceBack();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag != "Player")
            return;

        Vector3 colNormal = col.contacts[0].normal;
        Vector2 dir;

        // deal collision with upper or lower part of paddle
        if (colNormal.x == 0f)
        {
            float x = ComputeHitFactor(col.transform.position, col.collider.bounds.size.x);
            dir = new Vector2(x, colNormal.y).normalized;
        }
        // collision with front part of the paddle
        else
        {
            float y = ComputeHitFactor(col.transform.position, col.collider.bounds.size.y);
            dir = new Vector2(colNormal.x, y).normalized;
        }

        if (dir.magnitude > 0f)
        {
            currentSpeed = Mathf.Min(MaxSpeed, currentSpeed + HitAcceleration);
            rigidBody.velocity = dir * currentSpeed;
        }
        else
        {
            Debug.LogWarning("magnitude <= 0 " + dir.magnitude);
        }
    }

}
