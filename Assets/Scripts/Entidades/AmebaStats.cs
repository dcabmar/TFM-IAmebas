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
        if (brain != null)
        {
            if (brain.data == null || brain.data.genome.Count == 0)
            {
                brain.data = new AmebaData();
                brain.data.memoryBank.Add("Ameba", 0f);
                GenerateRandomGenome();
            }
            DetermineSpeciesAndStats();
        }
        energy = maxEnergy;
    }

    void GenerateRandomGenome()
    {
        brain.data.genome.Clear();
        for (int i = 0; i < 7; i++)
            brain.data.genome.Add((GeneType)Random.Range(0, 2));
    }

    void DetermineSpeciesAndStats()
    {
        var d = brain.data;
        d.countPacifist = 0;
        d.countPredator = 0;

        foreach (GeneType g in d.genome)
        {
            if (g == GeneType.Pacifist) d.countPacifist++;
            else if (g == GeneType.Predator) d.countPredator++;
        }

        // Stats Pacifista
        moveInterval = Mathf.Max(0.5f, baseInterval - (d.countPacifist * 0.2f));
        sensorRadius = baseVision + (d.countPacifist * 1f);

        // Stats Depredador
        moveForce = baseForce + (d.countPredator * 1f);
        attackDamage = 5f + (d.countPredator * 3f);
        canAttack = (d.countPredator > 0);

        // Asignar Especie
        if (d.countPredator > d.countPacifist) d.species = GeneType.Predator;
        else d.species = GeneType.Pacifist;
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