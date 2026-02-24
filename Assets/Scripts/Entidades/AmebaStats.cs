using UnityEngine;
using System.Collections.Generic;

public class AmebaStats : MonoBehaviour
{
    [Header("Components")]
    public AmebaBrain brain;

    [Header("Biological Stats")]
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float reproductionThreshold = 180f;
    public float baseInterval = 2.0f;
    public float baseVision = 2f;
    public float baseForce = 2f;

    [Header("Gene Stats (Calculated)")]
    public float moveForce;
    public float moveInterval;
    public float sensorRadius;
    public float attackDamage;
    public bool canAttack;

    void Awake()
    {
        brain = GetComponent<AmebaBrain>();
    }

    public void InitStats()
    {
        // 1. Reseteamos el umbral base por si acaso
        reproductionThreshold = 180f;

        if (brain != null)
        {
            // CASO A: Es una Ameba NUEVA (Generación 0, spawneada por el Manager)
            if (brain.data == null || brain.data.genome.Count == 0)
            {
                brain.data = new AmebaData();
                brain.data.memoryBank.Add("Ameba", 0f);
                GenerateRandomGenome();
                DetermineSpeciesAndStats();
                
                // --> BONO DE TAMAÑO INICIAL (Solo Gen 0) <--
                // +20 extra de energía máxima/masa por cada gen neutro
                maxEnergy = 100f + (brain.data.countNeutral * 20f);
            }
            // CASO B: Es una hija nacida por Mitosis
            else
            {
                DetermineSpeciesAndStats();
                // NOTA: Aquí no tocamos "maxEnergy" porque la función Mitosis() 
                // ya se encarga de darle a la hija exactamente la mitad de la energía de la madre.
            }
        }
        
        // Llenamos la energía al máximo al nacer
        energy = maxEnergy; 
    }

    void GenerateRandomGenome()
    {
        brain.data.genome.Clear();
        // Ahora generamos números del 0 al 3 (excluye el 3) para incluir a la Neutra
        for (int i = 0; i < 9; i++)
            brain.data.genome.Add((GeneType)Random.Range(0, 3)); 
    }

    void DetermineSpeciesAndStats()
    {
        var d = brain.data;
        d.countPacifist = 0;
        d.countPredator = 0;
        d.countNeutral = 0; // NUEVO

        foreach (GeneType g in d.genome)
        {
            if (g == GeneType.Pacifist) d.countPacifist++;
            else if (g == GeneType.Predator) d.countPredator++;
            else if (g == GeneType.Neutral) d.countNeutral++;
        }

        // Stats Pacifista
        moveInterval = Mathf.Max(0.5f, baseInterval - (d.countPacifist * 0.2f));
        sensorRadius = baseVision + (d.countPacifist * 1f);

        // Stats Depredador
        moveForce = baseForce + (d.countPredator * 1f);
        attackDamage = 5f + (d.countPredator * 3f);
        canAttack = (d.countPredator > 0);

        // STATS NEUTRA (Aumenta el umbral de mitosis para que se hagan más grandes antes de dividirse)
        reproductionThreshold += (d.countNeutral * 30f); 

        // ASIGNAR ESPECIE DOMINANTE
        if (d.countPredator > d.countPacifist && d.countPredator > d.countNeutral) 
            d.species = GeneType.Predator;
        else if (d.countNeutral >= d.countPacifist && d.countNeutral >= d.countPredator) 
            d.species = GeneType.Neutral; // En caso de empate, tienden a ser neutras
        else 
            d.species = GeneType.Pacifist;
    }

    public void UpdateMetrics(float dist)
    {
        if (brain != null)
        {
            brain.data.timeAlive += Time.deltaTime;
            brain.data.distanceTraveled += dist;
        }
    }

    public void AddEnergyConsumed(float amount)
    {
        if (brain != null) brain.data.energyConsumed += amount;
    }
}