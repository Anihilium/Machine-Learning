using UnityEngine;
using TMPro;

public class GameMgr : MonoBehaviour
{
    [HideInInspector] public float CourtHeight = 10f;

    [HideInInspector] public bool bP1LastScored = true;
    [HideInInspector] public bool IsBallLaunched = false;
    public bool LaunchRandom = false;

    [HideInInspector] public Ball Ball;
    private ExitTrigger leftTrigger;
    private ExitTrigger rightTrigger;
    [HideInInspector] public PaddleController P1Paddle;
    [HideInInspector] public PaddleController P2Paddle;
    private TMP_Text p1ScoreText;
    private TMP_Text p2ScoreText;

    public int TotalScore;
    [HideInInspector] public int MaxScore = int.MaxValue;

    // Use this for initialization
    void Awake ()
    {
        TotalScore = 0;

        Transform parentTransform = transform.parent;

        Ball = parentTransform.Find("Ball").GetComponent<Ball>();
        Ball.gameMgr = this;

        leftTrigger = parentTransform.Find("LeftBound").GetComponent<ExitTrigger>();
        leftTrigger.gameMgr = this;

        rightTrigger = parentTransform.Find("RightBound").GetComponent<ExitTrigger>();
        rightTrigger.gameMgr = this;

        P1Paddle = parentTransform.Find("Paddle1").GetComponent<PaddleController>();
        P1Paddle.gameMgr = this;

        P2Paddle = parentTransform.Find("Paddle2").GetComponent<PaddleController>();
        P2Paddle.gameMgr = this;

        p1ScoreText = parentTransform.Find("Score1").GetComponent<TMP_Text>();
        p1ScoreText.text = "0";

        p2ScoreText = parentTransform.Find("Score2").GetComponent<TMP_Text>();
        p2ScoreText.text = "0";

        CourtHeight = Mathf.Abs(parentTransform.Find("UpperBound").position.y - parentTransform.Find("LowerBound").position.y);
    }

    void Update()
    {
        if (HasGameEnded())
        {
            return;
        }

        if(!IsBallLaunched && Time.deltaTime < 1f / 60f)
        {
            TryLaunchBall();
        }
    }

    public void TryLaunchBall()
    {
        if (IsBallLaunched == false)
        {
            Ball.Launch(LaunchRandom);
            IsBallLaunched = true;
        }
    }

    public void OnBallExit(bool isLeftSide)
    {
        Ball.rigidBody.velocity = Vector2.zero;
        IsBallLaunched = false;
        Ball.transform.position = transform.parent.position;

        bP1LastScored = !isLeftSide;

        if (isLeftSide)
        {
            P2Paddle.Score++;
            p2ScoreText.text = P2Paddle.Score.ToString();
        }
        else
        {
            P1Paddle.Score++;
            p1ScoreText.text = P1Paddle.Score.ToString();
        }
        ++TotalScore;
    }

    public bool HasGameEnded()
    {
        return TotalScore >= MaxScore;
    }

    public void ResetGame()
    {
        P1Paddle.Score = 0;
        p1ScoreText.text = "0";
        P2Paddle.Score = 0;
        p2ScoreText.text = "0";
        Ball.rigidBody.velocity = Vector2.zero;
        IsBallLaunched = false;
        Ball.transform.position = transform.parent.position;
        TotalScore = 0;
    }
}
