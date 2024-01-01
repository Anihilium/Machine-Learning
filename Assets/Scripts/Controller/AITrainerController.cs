using UnityEngine;

public class AITrainerController : PaddleController
{
    private Transform ballTransform;
    private float randomBias;

    protected override void Start()
    {
        base.Start();
        transform.localPosition = new Vector3(transform.localPosition.x, Random.Range(-Border, Border), transform.localPosition.z);
        ballTransform = gameMgr.Ball.transform;
        randomBias = Random.Range(-Height, Height);
        randomBias = 0f;
    }

    // Update is called once per frame
    private void Update()
    {
        if (gameMgr.HasGameEnded())
        {
            return;
        }

        if ((isP1 && gameMgr.Ball.GetDir().x < 0f) || (!isP1 && gameMgr.Ball.GetDir().x > 0f))
        {
            float verticalDifference = ballTransform.position.y - transform.position.y + randomBias;
        
            if (verticalDifference > 0f)
            {
                MoveUp();
            }
            else if (verticalDifference < 0f)
            {
                MoveDown();
            }
        }
    }
}
