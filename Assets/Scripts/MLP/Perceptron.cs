using System;
using System.Collections.Generic;
using UnityEngine;

public class Perceptron
{
    public struct Input
    {
        public Perceptron inputPerceptron;
        public float weight;
    }

    [HideInInspector]
    public List<Input> inputs = new List<Input>();
    private float state;
    private MLPNetwork mlpNet;
    private bool isInHiddenLayer;

    public Perceptron(MLPNetwork mlpNetwork, bool bHiddenLayer = false)
    {
        mlpNet = mlpNetwork;
        isInHiddenLayer = bHiddenLayer;
    }

    public void FeedForward()
    {
        float sum = 0f;
        foreach (Input input in inputs)
        {
            if (input.inputPerceptron == null)
            {
                sum += 1f * input.weight;
            }
            else
            {
                sum += input.inputPerceptron.GetState() * input.weight;
            }
        }

        state = Threshold(sum);
    }

    #region Activation functions
    public float Threshold(float input, bool _derivative = false)
    {
        FUNCTION function = isInHiddenLayer ? mlpNet.HiddenLayerFunction : mlpNet.OutputFunction;
        switch (function)
        {
            case FUNCTION.SIGMOID:
                return Sigmoid(input, _derivative);

            case FUNCTION.TANH:
                return Tanh(input, _derivative);

            case FUNCTION.RELU:
                return Relu(input, _derivative);

            default:
                Debug.LogError("Function not set");
                break;
        }
        return 0f;
    }

    private float Sigmoid(float input, bool _derivative)
    {
        if(!_derivative)
        {
            return 1f / (1f + Mathf.Exp(mlpNet.SigmoidSteepness * -input));
        }
        else
        {
            float sigmoid = Sigmoid(input, false);
            return sigmoid * (1f - sigmoid);
        }
    }

    private float Tanh(float input, bool _derivative)
    {
        if (!_derivative)
        {
            return (Mathf.Exp(input) - Mathf.Exp(-input)) / (Mathf.Exp(input) + Mathf.Exp(-input));
        }
        else
        {
            float tanh = Tanh(input, false);
            return 1f - tanh * tanh;
        }
    }

    private float Relu(float input, bool _derivative)
    {
        if (!_derivative)
        {
            return input < 0f ? 0f : input;
        }
        else
        {
            return input < 0f ? 0f : 1f;
        }
    }
    #endregion Activation functions

    public float GetState() { return state; }
    public void SetState(in float newState) { state = newState; }
}
