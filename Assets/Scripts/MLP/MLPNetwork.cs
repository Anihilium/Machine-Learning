using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public enum FUNCTION : int
{
    SIGMOID = 0,
    TANH = 1,
    RELU = 2,
}

public class MLPNetwork
{
    private List<Perceptron> inputPerceptrons;
    private List<List<Perceptron>> hiddenPerceptrons;
    private List<Perceptron> outputPerceptrons;

    public bool UseBias = false;
    public float InitWeightRange = 0.2f;

    public int InputAmount = 2;
    public int OutputAmount = 1;

    public float SigmoidSteepness = 1f;

    public List<int> PerceptronPerHiddenLayer = new List<int>();

    public FUNCTION OutputFunction = FUNCTION.SIGMOID;
    public FUNCTION HiddenLayerFunction = FUNCTION.RELU;
    
    [HideInInspector]
    public float FitnessScore = 0f;

    #region Constructors
    public MLPNetwork()
    {

    }

    public MLPNetwork(bool useBias, float initWeightRange, int inputAmount, int outputAmount, float sigmoidSteepness, List<int> perceptronPerHiddenLayer, FUNCTION outputFunction, FUNCTION hiddenLayerFunction)
    {
        UseBias = useBias;
        InitWeightRange = initWeightRange;
        InputAmount = inputAmount;
        OutputAmount = outputAmount;
        SigmoidSteepness = sigmoidSteepness;
        PerceptronPerHiddenLayer.Clear();
        PerceptronPerHiddenLayer.AddRange(perceptronPerHiddenLayer);
        OutputFunction = outputFunction;
        HiddenLayerFunction = hiddenLayerFunction;
        Init();
    }

    public MLPNetwork(in MLPNetwork networkToCopy)
    {
        UseBias = networkToCopy.UseBias;
        InitWeightRange = networkToCopy.InitWeightRange;
        InputAmount = networkToCopy.InputAmount;
        OutputAmount = networkToCopy.OutputAmount;
        SigmoidSteepness = networkToCopy.SigmoidSteepness;
        PerceptronPerHiddenLayer.Clear();
        PerceptronPerHiddenLayer.AddRange(networkToCopy.PerceptronPerHiddenLayer);
        OutputFunction = networkToCopy.OutputFunction;
        HiddenLayerFunction = networkToCopy.HiddenLayerFunction;
        Init();
    }
    #endregion Constructors

    #region Initialization functions
    private void Init()
    {
        inputPerceptrons = new List<Perceptron>();
        for (int i = 0; i < InputAmount; ++i)
        {
            inputPerceptrons.Add(new Perceptron(this));
        }

        hiddenPerceptrons = new List<List<Perceptron>>();
        for (int j = 0; j < PerceptronPerHiddenLayer.Count; ++j)
        {
            hiddenPerceptrons.Add(new List<Perceptron>());
            for (int i = 0; i < PerceptronPerHiddenLayer[j]; ++i)
            {
                List<Perceptron> inputsList = j == 0 ? inputPerceptrons : hiddenPerceptrons[j - 1];
                hiddenPerceptrons[j].Add(PerceptronWithFilledInputs(inputsList, j == 0, true));
            }
        }

        outputPerceptrons = new List<Perceptron>();
        for (int i = 0; i < OutputAmount; ++i)
        {
            List<Perceptron> inputsList = hiddenPerceptrons.Count > 0 ? hiddenPerceptrons.Last() : inputPerceptrons;
            outputPerceptrons.Add(PerceptronWithFilledInputs(inputsList, hiddenPerceptrons.Count < 1));
        }
    }

    private Perceptron PerceptronWithFilledInputs(in List<Perceptron> inputPercetronList, bool isInputList, bool isHiddenPerceptron = false)
    {
        Perceptron perceptron = new Perceptron(this, isHiddenPerceptron);
        if (!isInputList && UseBias)
        {
            perceptron.inputs.Add(CreatePerceptronInput());
        }
        foreach (Perceptron inputPerceptron in inputPercetronList)
        {
            perceptron.inputs.Add(CreatePerceptronInput(inputPerceptron));
        }
        return perceptron;
    }

    Perceptron.Input CreatePerceptronInput(Perceptron _inputPerceptron = null)
    {
        Perceptron.Input input;
        input.inputPerceptron = _inputPerceptron;
        input.weight = UnityEngine.Random.Range(-InitWeightRange, InitWeightRange);
        return input;
    }
    #endregion Initialization functions

    public void GenerateOutput(in List<float> inputList)
    {
        for(int i = 0; i < inputPerceptrons.Count; i++)
        {
            inputPerceptrons[i].SetState(inputList[i]);
        }

        foreach(List<Perceptron> perceptronList in hiddenPerceptrons)
        {
            foreach(Perceptron perceptron in perceptronList)
            {
                perceptron.FeedForward();
            }
        }

        foreach (Perceptron perceptron in outputPerceptrons)
        {
            perceptron.FeedForward();
        }
    }

    public float[] GetOutputs()
    {
        float[] outputs = new float[outputPerceptrons.Count];
        for(int i = 0; i < outputPerceptrons.Count; ++i)
        {
            outputs[i] = outputPerceptrons[i].GetState();
        }
        return outputs;
    }

    #region Utility functions
    public int GetInputAmountOnHiddenNeuron(in int _layerIter, in int _neuronIter)
    {
        return hiddenPerceptrons[_layerIter][_neuronIter].inputs.Count;
    }

    public int GetInputAmountOnOutputNeuron(in int _neuronIter)
    {
        return outputPerceptrons[_neuronIter].inputs.Count;
    }

    public void SetOutputNeuronWeight(in int _neuronIter, in int _inputIter, in float _value)
    {
        Perceptron.Input input = outputPerceptrons[_neuronIter].inputs[_inputIter];
        input.weight = _value;
        outputPerceptrons[_neuronIter].inputs[_inputIter] = input;
    }

    public float GetOutputNeuronWeight(in int _neuronIter, in int _inputIter)
    {
        return outputPerceptrons[_neuronIter].inputs[_inputIter].weight;
    }

    public void SetHiddenNeuronWeight(in int _layerIter, in int _neuronIter, in int _inputIter, in float _value)
    {
        Perceptron.Input input = hiddenPerceptrons[_layerIter][_neuronIter].inputs[_inputIter];
        input.weight = _value;
        hiddenPerceptrons[_layerIter][_neuronIter].inputs[_inputIter] = input;
    }

    public float GetHiddenNeuronWeight(in int _layerIter, in int _neuronIter, in int _inputIter)
    {
        return hiddenPerceptrons[_layerIter][_neuronIter].inputs[_inputIter].weight;
    }

    public void CopyWeights(in MLPNetwork _network)
    {
        for(int i = 0; i < PerceptronPerHiddenLayer.Count; i++)
        {
            for(int j = 0; j < PerceptronPerHiddenLayer[i]; j++)
            {
                for(int k = 0; k < hiddenPerceptrons[i][j].inputs.Count; k++)
                {
                    SetHiddenNeuronWeight(i, j, k, _network.GetHiddenNeuronWeight(i, j, k));
                }
            }    
        }

        for (int i = 0; i < outputPerceptrons.Count; i++)
        {
            for (int j = 0; j < outputPerceptrons[i].inputs.Count; j++)
            {
                SetOutputNeuronWeight(i, j, _network.GetOutputNeuronWeight(i, j));
            }
        }
    }
    #endregion Utility functions

    #region Serialization functions
    public void SaveWeights(in string _fileName)
    {
        string[] splitPath = _fileName.Split('.');
        if (splitPath.Length < 2 || splitPath[1] != "bin")
        {
            Debug.LogError("Incorrect file format: should end in \".bin\"");
            return;
        }

        string fullPath = "Assets/SavedNetworks/" + _fileName;

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        FileStream stream = File.Open(fullPath, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false);

        writer.Write(UseBias);

        writer.Write(InputAmount);

        writer.Write(PerceptronPerHiddenLayer.Count);
        for(int i = 0; i < PerceptronPerHiddenLayer.Count; ++i)
        {
            writer.Write(PerceptronPerHiddenLayer[i]);
        }

        writer.Write(OutputAmount);

        for(int i = 0; i < hiddenPerceptrons.Count; ++i)
        {
            for (int j = 0; j < hiddenPerceptrons[i].Count; ++j)
            {
                for (int k = 0; k < hiddenPerceptrons[i][j].inputs.Count; ++k)
                {
                    writer.Write(hiddenPerceptrons[i][j].inputs[k].weight);
                }
            }
        }

        for (int i = 0; i < outputPerceptrons.Count; ++i)
        {
            for (int j = 0; j < outputPerceptrons[i].inputs.Count; ++j)
            {
                writer.Write(outputPerceptrons[i].inputs[j].weight);
            }
        }

        writer.Close();
        stream.Close();

        PrintClass("Writer");
    }

    public bool ImportWeights(in string _fileName)
    {
        string fullPath = "Assets/SavedNetworks/" + _fileName;

        if (!File.Exists(fullPath))
        {
            Debug.LogError("File not found");
            return false;
        }

        FileStream stream = File.Open(fullPath, FileMode.Open);
        BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false);

        UseBias = reader.ReadBoolean();

        InputAmount = reader.ReadInt32();

        int nbHiddenLayers = reader.ReadInt32();

        PerceptronPerHiddenLayer.Clear();
        for (int i = 0; i < nbHiddenLayers; ++i)
        {
            PerceptronPerHiddenLayer.Add(reader.ReadInt32());
        }

        OutputAmount = reader.ReadInt32();

        Init();

        for (int i = 0; i < hiddenPerceptrons.Count; ++i)
        {
            for (int j = 0; j < hiddenPerceptrons[i].Count; ++j)
            {
                for (int k = 0; k < hiddenPerceptrons[i][j].inputs.Count; ++k)
                {
                    SetHiddenNeuronWeight(i, j, k, reader.ReadSingle());
                }
            }
        }

        for (int i = 0; i < outputPerceptrons.Count; ++i)
        {
            for (int j = 0; j < outputPerceptrons[i].inputs.Count; ++j)
            {
                SetOutputNeuronWeight(i, j, reader.ReadSingle());
            }
        }

        reader.Close();
        stream.Close();

        PrintClass("Reader");

        return true;
    }

    private void PrintClass(string _caller)
    {
        string message = "Date: " + DateTime.Now.ToString("dd/MM/yyyy hh:mmtt") + '\n';

        message += _caller + ":\n";

        message += "Use bias: " + (UseBias ? "true" : "false") + "\n";
        message += "Input amount: " + InputAmount + "\n";
        message += "Hidden layer amount: " + PerceptronPerHiddenLayer.Count + " (";
        foreach(int hiddenPerceptronAmount in PerceptronPerHiddenLayer)
        {
            message += hiddenPerceptronAmount + ",";
        }
        message += ")\n";
        message += "Output amount:" + OutputAmount + "\n";

        message += "Weights:\n";

        for (int i = 0; i < hiddenPerceptrons.Count; ++i)
        {
            message += "Hidden layer " + i + ":\n";
            for (int j = 0; j < hiddenPerceptrons[i].Count; ++j)
            {
                message += "\t- Perceptron " + j + ":\n";
                for (int k = 0; k < hiddenPerceptrons[i][j].inputs.Count; ++k)
                {
                    message += "\t\t-> " + hiddenPerceptrons[i][j].inputs[k].weight + "\n";
                }
            }
        }

        message += "Output layer:\n";
        for (int i = 0; i < outputPerceptrons.Count; ++i)
        {
            message += "\t- Perceptron " + i + ":\n";
            for (int j = 0; j < outputPerceptrons[i].inputs.Count; ++j)
            {
                message += "\t\t-> " + outputPerceptrons[i].inputs[j].weight + "\n";
            }
        }

        Debug.Log(message);
    }
    #endregion Serialization functions
}