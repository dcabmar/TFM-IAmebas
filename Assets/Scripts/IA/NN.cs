using System.Collections.Generic;
using System;

[System.Serializable]
public class NN
{
    public int[] layers; // Ej: [4, 6, 2] -> 4 inputs, 6 neuronas ocultas, 2 outputs
    public float[][] neurons; // Valores de las neuronas
    public float[][][] weights; // Pesos de las conexiones
    
    // Constructor
    public NN(int[] layers)
    {
        this.layers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            this.layers[i] = layers[i];

        InitNeurons();
        InitWeights();
    }

    // Inicializar neuronas vacías
    private void InitNeurons()
    {
        List<float[]> neuronsList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            neuronsList.Add(new float[layers[i]]);
        }
        neurons = neuronsList.ToArray();
    }

    // Inicializar pesos aleatorios (El "ADN" inicial)
    private void InitWeights()
    {
        List<float[][]> weightsList = new List<float[][]>();
        for (int i = 1; i < layers.Length; i++)
        {
            List<float[]> layerWeightsList = new List<float[]>();
            int neuronsInPreviousLayer = layers[i - 1];
            
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float[] neuronWeights = new float[neuronsInPreviousLayer];
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    // Pesos aleatorios entre -0.5 y 0.5
                    neuronWeights[k] = UnityEngine.Random.Range(-0.5f, 0.5f);
                }
                layerWeightsList.Add(neuronWeights);
            }
            weightsList.Add(layerWeightsList.ToArray());
        }
        weights = weightsList.ToArray();
    }

    // EL PENSAMIENTO (Feed Forward)
    // Recibe inputs (lo que ve) y devuelve outputs (lo que decide hacer)
    public float[] FeedForward(float[] inputs)
    {
        // 1. Poner los inputs en la primera capa
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        // 2. Procesar capa por capa
        for (int i = 1; i < layers.Length; i++)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float value = 0f;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    // Suma ponderada: valor anterior * peso
                    value += weights[i - 1][j][k] * neurons[i - 1][k];
                }
                
                // Función de activación (Tanh hace que el resultado esté entre -1 y 1)
                // Perfecto para movimiento (izquierda -1, derecha 1)
                neurons[i][j] = (float)Math.Tanh(value);
            }
        }

        // 3. Devolver la última capa (Outputs)
        return neurons[neurons.Length - 1];
    }
    
    // --- AQUÍ AÑADIREMOS LA MUTACIÓN MÁS TARDE ---
}