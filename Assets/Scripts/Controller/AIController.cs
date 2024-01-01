using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : PaddleController
{
    [HideInInspector] public MLPNetwork MLPNetwork;

    private PaddleController opponent;

    private List<float> inputs = new List<float>();
    private int lastScore = 0;
    private List<float> timeToScore = new List<float>();
    private float time;

    private bool lastGameState;

    private float survivingTime;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        opponent = isP1 ? gameMgr.P2Paddle : gameMgr.P1Paddle;
        time = survivingTime = 0f;
        lastGameState = gameMgr.HasGameEnded();
    }

    // Update is called once per frame
    private void Update()
    {
        bool curGameState = gameMgr.HasGameEnded();
        
        if(lastGameState != curGameState)
        {
            if(curGameState) // Game ended
            {
                ComputeFitnessScore();
            }
            else // Game started
            {
                lastScore = 0;
                time = survivingTime = 0f;
                timeToScore.Clear();
                transform.localPosition = transform.localPosition - Vector3.up * transform.localPosition.y;
            }
        }

        if (!curGameState) // Move and check score while game is running
        {
            EvaluateStateOfGame();
        }

        lastGameState = curGameState;
    }

    private void EvaluateStateOfGame()
    {
        FillInputList();
        MLPNetwork.GenerateOutput(inputs);
        float output = MLPNetwork.GetOutputs()[0];
        if (output > 0f)
        {
            MoveUp(output);
        }
        else if (output < 0f)
        {
            MoveDown(-output);
        }

        time += Time.deltaTime;
        if (lastScore != Score)
        {
            lastScore = Score;
            timeToScore.Add(time);
            time = 0f;
        }

        if(opponent.Score < 1)
        {
            survivingTime += Time.deltaTime;
        }
    }

    private void FillInputList()
    {
        inputs.Clear();
        inputs.Add(GetPos0to1());
        inputs.Add(opponent.GetPos0to1());
        Vector2 ballPos = gameMgr.Ball.GetPos0to1();
        Vector2 ballDir = gameMgr.Ball.GetDir0to1();
        inputs.Add(isP1 ? ballPos.x : 1f - ballPos.x);
        inputs.Add(ballPos.y);
        inputs.Add(isP1 ? ballDir.x : 1f - ballDir.x);
        inputs.Add(ballDir.y);
        inputs.Add(gameMgr.Ball.GetSpeed0to1());
    }

    private float AddScoringTime()
    {
        float sum = 0f;
        foreach(float scoringTime in timeToScore)
        {
            sum += scoringTime;
        }
        return sum;
    }

    public void ComputeFitnessScore()
    {
        MLPNetwork.FitnessScore = survivingTime + survivingTime * Score - opponent.Score * 10000f - AddScoringTime();
    }
}
