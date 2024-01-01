using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    private MLPNetwork MLPNet;

    #region Genetic algorithm
    [Header("Genetic algorithm")]

    private int GenerationSize = 8;

    [Range(2, 10)]
    public int ParentAmount = 4;

    [Range(1, 50)]
    public int MaxMutations = 10;

    [Range(0.1f, 30f)]
    public float MaxMutationStrength = 0.2f;

    private List<MLPNetwork> currentGeneration = new List<MLPNetwork>();
    #endregion

    #region Neuron Display Parameters
    [Header("Neural network display")]

    public bool ShouldDisplay = false;

    public GameObject NeuronPrefab;
    public GameObject BiasPrefab;
    public GameObject WeightLinkPrefab;
    public GameObject LayerPrefab;

    public int SpacingBetweenNeurons = 25;
    public int SpacingBetweenLayers = 200;

    List<List<List<LineRenderer>>> hiddenNeuronWeights;
    List<List<LineRenderer>> outputNeuronWeights;
    #endregion

    #region MLPNetwork parameters
    [Header("Neural network parameters")]

    public bool UseBias = false;

    public float InitWeightRange = 0.2f;

    [Range(1, 10)]
    public int InputAmount = 2;

    [Range(1, 10)]
    public int OutputAmount = 1;

    [Range(1, 10)]
    public List<int> PerceptronPerHiddenLayer = new List<int>();

    public FUNCTION HiddenLayerFunction = FUNCTION.SIGMOID;
    public FUNCTION OutputFunction = FUNCTION.SIGMOID;
    public float SigmoidSteepness = 1f;
    #endregion

    #region Training parameters
    [Header("Serialization")]
    public string FileName;
    [Header("Gameplay")]
    public int MaxScore;
    public float TimeLimit;

    private float time;
    private TMP_Text timeText;
    private List<GameMgr> gameManagers = new List<GameMgr>();
    private List<GameMgr> activeGames = new List<GameMgr>();
    private List<AIController> AIPaddles = new List<AIController>();

    private int generationAmount;
    private TMP_Text generationText;
    #endregion Training parameters

    #region Monobehavior functions
    // Start is called before the first frame update
    void Start()
    {
        timeText = GameObject.Find("Display/Time").GetComponent<TMP_Text>();
        GameObject generationTextObj = GameObject.Find("Display/GenerationAmount");
        if(generationTextObj != null )
        {
            generationText = generationTextObj.GetComponent<TMP_Text>();
        }

        gameManagers.AddRange(FindObjectsOfType<GameMgr>());

        AIPaddles.AddRange(FindObjectsOfType<AIController>());
        GenerationSize = AIPaddles.Count;

        MLPNet = new MLPNetwork(UseBias, InitWeightRange, InputAmount, OutputAmount, SigmoidSteepness, PerceptronPerHiddenLayer, OutputFunction, HiddenLayerFunction);
        if (MLPNet.ImportWeights(FileName) == true)
        {
            for(int i = 0; i < GenerationSize; i++)
            {
                MLPNetwork pop = new MLPNetwork(MLPNet);
                pop.CopyWeights(MLPNet);
                pop.FitnessScore = 1f;
                currentGeneration.Add(pop);
                if(i == 0)
                {
                    MLPNet = pop;
                }
            }
        }

        if (ShouldDisplay)
        {
            InitDisplay();
            SetWeightLinks();
        }

        foreach (GameMgr manager in gameManagers)
        {
            manager.MaxScore = MaxScore;
        }

        StartGame();

        time = 0f;
    }

    private void OnApplicationQuit()
    {
        MLPNet.SaveWeights(FileName);
    }

    // Update is called once per frame
    void Update()
    {
        if(activeGames.Count < 1)
        {
            StartGame();
        }
        else if(time > TimeLimit)
        {
            EndActiveGames();
        }
        else
        {
            EvaluateActiveGames();
        }

        TimeManagement();
    }
    #endregion Monobehavior functions

    #region Training functions
    void StartGame()
    {
        GeneticTraining();

        for (int i = 0; i < currentGeneration.Count; ++i)
        {
            AIPaddles[i].MLPNetwork = currentGeneration[i];
        }

        foreach (GameMgr manager in gameManagers)
        {
            manager.ResetGame();
            activeGames.Add(manager);
        }

        time = 0f;
    }

    private void EndActiveGames()
    {
        foreach (GameMgr manager in activeGames)
        {
            manager.TotalScore = int.MaxValue;
        }

        activeGames.Clear();
    }

    private void EvaluateActiveGames()
    {
        for(int i = 0; i < activeGames.Count; ++i)
        {
            if (activeGames[i].HasGameEnded())
            {
                activeGames.RemoveAt(i);
            }    
        }
    }

    private void TimeManagement()
    {
        time += Time.deltaTime;
        int timeRounded = Mathf.RoundToInt(time);
        int minutes = timeRounded / 60;
        int seconds = timeRounded % 60;
        timeText.text = minutes + ":" + (seconds < 10 ? "0" : "") + seconds;
    }

    private void GeneticTraining()
    {
        if(GenerationSize < 1 || generationText == null)
        {
            return;
        }

        // First generation init
        if (currentGeneration.Count < 1)
        {
            currentGeneration.Add(MLPNet);
            for (int i = 1; i < GenerationSize; i++)
            {
                MLPNetwork network = new MLPNetwork(MLPNet);
                currentGeneration.Add(network);
            }

            generationAmount = 0;
            generationText.text = "Generation n°" + generationAmount;
            return;
        }

        // Select best parents based on their fitness scores
        List<float> bestGenerationFitnesses = new List<float>();
        List<int> bestNetworkIndices = new List<int>();
        bestGenerationFitnesses.Add(currentGeneration[0].FitnessScore);
        bestNetworkIndices.Add(0);
        for (int i = 1; i < GenerationSize; i++)
        {
            for (int j = 0; j < bestGenerationFitnesses.Count; j++)
            {
                if (bestGenerationFitnesses[j] < currentGeneration[i].FitnessScore)
                {
                    if (bestGenerationFitnesses.Count + 1 > ParentAmount)
                    {
                        bestGenerationFitnesses.RemoveAt(ParentAmount - 1);
                        bestNetworkIndices.RemoveAt(ParentAmount - 1);
                    }
                    bestGenerationFitnesses.Insert(j, currentGeneration[i].FitnessScore);
                    bestNetworkIndices.Insert(j, i);
                    break;
                }
                else if (bestNetworkIndices.Count < ParentAmount)
                {
                    bestGenerationFitnesses.Add(currentGeneration[i].FitnessScore);
                    bestNetworkIndices.Add(i);
                }
            }
        }

        List<MLPNetwork> parentNetworks = new List<MLPNetwork>();
        for (int i = 0; i < ParentAmount; i++)
        {
            parentNetworks.Add(currentGeneration[bestNetworkIndices[i]]);
        }

        // Set the most fitted network of this generation
        MLPNet.CopyWeights(parentNetworks[0]);

        // Create a new generation with the selected parents
        currentGeneration.Clear();
        currentGeneration.Add(MLPNet);
        for (int i = 1; i < GenerationSize; ++i)
        {
            // Randomly select two parents
            MLPNetwork parent1 = parentNetworks[Random.Range(0, ParentAmount - 1)];
            MLPNetwork parent2 = parentNetworks[Random.Range(0, ParentAmount - 1)];
            while (parent1 == parent2)
            {
                parent2 = parentNetworks[Random.Range(0, ParentAmount - 1)];
            }

            // Create a new network
            MLPNetwork newNetwork = new MLPNetwork(MLPNet);

            // Set its hidden neurons weights to one of his parents' value, randomly
            for (int j = 0; j < newNetwork.PerceptronPerHiddenLayer.Count; ++j)
            {
                for (int k = 0; k < newNetwork.PerceptronPerHiddenLayer[j]; ++k)
                {
                    int inputAmount = newNetwork.GetInputAmountOnHiddenNeuron(j, k);
                    for (int l = 0; l < inputAmount; ++l)
                    {
                        float parentWeight = (Random.value < 0.5f ? parent1 : parent2).GetHiddenNeuronWeight(j, k, l);
                        newNetwork.SetHiddenNeuronWeight(j, k, l, parentWeight);
                    }
                }
            }

            // Set its output neurons weights to one of his parents' value, randomly
            for (int j = 0; j < newNetwork.OutputAmount; ++j)
            {
                int inputAmount = newNetwork.GetInputAmountOnOutputNeuron(j);
                for (int k = 0; k < inputAmount; ++k)
                {
                    float parentWeight = (Random.value < 0.5f ? parent1 : parent2).GetOutputNeuronWeight(j, k);
                    newNetwork.SetOutputNeuronWeight(j, k, parentWeight);
                }
            }
            currentGeneration.Add(newNetwork);
        }

        // Mutate the new generation
        for (int i = 1; i < GenerationSize; ++i)
        {
            MLPNetwork curNewNetwork = currentGeneration[i];

            int mutationAmount = Random.Range(1, MaxMutations);
            for (int j = 0; j < mutationAmount; ++j)
            {
                bool bModifyHiddenWeight = Random.value >= 0.5f;

                if (bModifyHiddenWeight)
                {
                    int layerIndex = Random.Range(0, curNewNetwork.PerceptronPerHiddenLayer.Count);
                    int neuronIndex = Random.Range(0, curNewNetwork.PerceptronPerHiddenLayer[layerIndex]);
                    int inputIndex = Random.Range(0, curNewNetwork.GetInputAmountOnHiddenNeuron(layerIndex, neuronIndex));
                    float weightAtIndex = curNewNetwork.GetHiddenNeuronWeight(layerIndex, neuronIndex, inputIndex);
                    weightAtIndex += Mathf.Sign(Random.value - 0.5f) * Random.Range(0.1f, MaxMutationStrength);
                    curNewNetwork.SetHiddenNeuronWeight(layerIndex, neuronIndex, inputIndex, weightAtIndex);
                }
                else
                {
                    int neuronIndex = Random.Range(0, curNewNetwork.OutputAmount);
                    int inputIndex = Random.Range(0, curNewNetwork.GetInputAmountOnOutputNeuron(neuronIndex));
                    float weightAtIndex = curNewNetwork.GetOutputNeuronWeight(neuronIndex, inputIndex);
                    weightAtIndex += Mathf.Sign(Random.value - 0.5f) * Random.Range(0.1f, MaxMutationStrength);
                    curNewNetwork.SetOutputNeuronWeight(neuronIndex, inputIndex, weightAtIndex);
                }
            }
        }

        ++generationAmount;
        generationText.text = "Generation n°" + generationAmount;
    }
    #endregion Training functions

    #region Neural network display
    private void InitDisplay()
    {
        Transform layers = GameObject.Find("Display/Layers").transform;

        GameObject inputLayer = Instantiate(LayerPrefab, layers);
        inputLayer.name = "InputLayer";
        RectTransform inputLayerTransform = inputLayer.GetComponent<RectTransform>();
        inputLayerTransform.localPosition = (PerceptronPerHiddenLayer.Count + 1) * SpacingBetweenLayers / 2f * Vector3.left;

        Vector3 firstInputNeuronPos = (InputAmount - 1) * SpacingBetweenNeurons / 2f * Vector3.up;
        List<RectTransform> inputNeuronsTransforms = new List<RectTransform>();

        for (int i = 0; i < InputAmount; i++)
        {
            GameObject inputNeuron = Instantiate(NeuronPrefab, inputLayerTransform);
            inputNeuron.name = "Input[" + i + ']';

            RectTransform curNeuronTransform = inputNeuron.GetComponent<RectTransform>();
            curNeuronTransform.localPosition = i * SpacingBetweenNeurons * Vector3.down + firstInputNeuronPos;

            inputNeuronsTransforms.Add(curNeuronTransform);
        }

        List<List<RectTransform>> hiddenNeuronsTransforms = new List<List<RectTransform>>();
        hiddenNeuronWeights = new List<List<List<LineRenderer>>>();
        for (int i = 0; i < PerceptronPerHiddenLayer.Count; i++)
        {
            GameObject hiddenLayer = Instantiate(LayerPrefab, layers);
            hiddenLayer.name = "HiddenLayer[" + i + ']';
            RectTransform hiddenLayerTransform = hiddenLayer.GetComponent<RectTransform>();
            hiddenLayerTransform.localPosition = inputLayerTransform.localPosition + (i + 1) * SpacingBetweenLayers * Vector3.right;

            int totalHiddenNeurons = PerceptronPerHiddenLayer[i] + (UseBias ? 1 : 0);
            Vector3 firstHiddenNeuronPos = (totalHiddenNeurons - 1) * SpacingBetweenNeurons / 2f * Vector3.up;
            hiddenNeuronsTransforms.Add(new List<RectTransform>());

            hiddenNeuronWeights.Add(new List<List<LineRenderer>>());

            for (int j = 0; j < totalHiddenNeurons; j++)
            {
                GameObject hiddenNeuron = Instantiate(UseBias && j == 0 ? BiasPrefab : NeuronPrefab, hiddenLayerTransform);
                hiddenNeuron.name = "Neuron[" + j + ']';

                RectTransform curNeuronTransform = hiddenNeuron.GetComponent<RectTransform>();
                curNeuronTransform.localPosition = j * SpacingBetweenNeurons * Vector3.down + firstHiddenNeuronPos;

                hiddenNeuronsTransforms[i].Add(curNeuronTransform);

                if (UseBias && j == 0)
                {
                    continue;
                }

                hiddenNeuronWeights[i].Add(new List<LineRenderer>());
                List<RectTransform> inputListTransform = i == 0 ? inputNeuronsTransforms : hiddenNeuronsTransforms[i - 1];
                foreach (RectTransform curInputNeuronTransform in inputListTransform)
                {
                    GameObject weightLink = Instantiate(WeightLinkPrefab, transform);
                    LineRenderer renderer = weightLink.GetComponent<LineRenderer>();
                    renderer.SetPosition(0, curInputNeuronTransform.position);
                    renderer.SetPosition(1, curNeuronTransform.position);
                    hiddenNeuronWeights[i][j - (UseBias ? 1 : 0)].Add(renderer);
                }
            }
        }

        GameObject outputLayer = Instantiate(LayerPrefab, layers);
        outputLayer.name = "OuterLayer";
        RectTransform outputLayerTransform = outputLayer.GetComponent<RectTransform>();
        outputLayerTransform.localPosition = inputLayerTransform.localPosition + (PerceptronPerHiddenLayer.Count + 1) * SpacingBetweenLayers * Vector3.right;

        Vector3 firstOutputNeuronPos = (OutputAmount - 1) * SpacingBetweenNeurons / 2f * Vector3.up;
        outputNeuronWeights = new List<List<LineRenderer>>();
        for (int i = 0; i < OutputAmount; i++)
        {
            GameObject outputNeuron = Instantiate(NeuronPrefab, outputLayerTransform);
            outputNeuron.name = "Output[" + i + ']';

            RectTransform curNeuronTransform = outputNeuron.GetComponent<RectTransform>();
            curNeuronTransform.localPosition = i * SpacingBetweenNeurons * Vector3.down + firstOutputNeuronPos;

            outputNeuronWeights.Add(new List<LineRenderer>());
            List<RectTransform> inputListTransform = PerceptronPerHiddenLayer.Count == 0 ? inputNeuronsTransforms : hiddenNeuronsTransforms.Last();
            foreach (RectTransform curInputNeuronTransform in inputListTransform)
            {
                GameObject weightLink = Instantiate(WeightLinkPrefab, transform);
                LineRenderer renderer = weightLink.GetComponent<LineRenderer>();
                renderer.SetPosition(0, curInputNeuronTransform.position);
                renderer.SetPosition(1, curNeuronTransform.position);
                outputNeuronWeights[i].Add(renderer);
            }
        }
    }

    public void SetWeightLinks()
    {
        for (int i = 0; i < PerceptronPerHiddenLayer.Count; i++)
        {
            for (int j = 0; j < PerceptronPerHiddenLayer[i]; j++)
            {
                int inputAmount = MLPNet.GetInputAmountOnHiddenNeuron(i, j);
                for (int k = 0; k < inputAmount; k++)
                {
                    LineRenderer weightLink = hiddenNeuronWeights[i][j][k];
                    float weight = MLPNet.GetHiddenNeuronWeight(i, j, k);
                    float sign = Mathf.Sign(weight);
                    weight = 2f / (1f + Mathf.Exp(-0.3f * Mathf.Abs(weight))) - 1f;
                    Color linkColor = new Color(sign > 0f ? 1f : 0f, 0f, sign <= 0f ? 1f : 0f, weight);
                    weightLink.startColor = weightLink.endColor = linkColor;
                }
            }
        }
        for (int i = 0; i < OutputAmount; i++)
        {
            int inputAmount = MLPNet.GetInputAmountOnOutputNeuron(i);
            for (int j = 0; j < inputAmount; j++)
            {
                LineRenderer weightLink = outputNeuronWeights[i][j];
                float weight = MLPNet.GetOutputNeuronWeight(i, j);
                float sign = Mathf.Sign(weight);
                weight = 2f / (1f + Mathf.Exp(-0.3f * Mathf.Abs(weight))) - 1f;
                Color linkColor = new Color(sign > 0f ? 1f : 0f, 0f, sign <= 0f ? 1f : 0f, weight);
                weightLink.startColor = weightLink.endColor = linkColor;
            }
        }
    }
    #endregion Neural network display
}