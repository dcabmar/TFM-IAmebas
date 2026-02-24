using System.Collections.Generic;

[System.Serializable]
public class AmebaData
{
    public string name;
    public int generation;
    public bool isCyst; 
    public float timeAlive;          
    public float distanceTraveled;   
    public float energyConsumed;     
    
    // --- GENÉTICA ---
    public List<GeneType> genome = new List<GeneType>();
    public GeneType species; 
    
    // Contadores de caché
    public int countPacifist;
    public int countPredator;
    public int countNeutral; // NUEVO
    
    public Dictionary<string, float> memoryBank = new Dictionary<string, float>();

    public AmebaData()
    {
        name = "Ameba_" + System.Guid.NewGuid().ToString().Substring(0, 4);
        memoryBank = new Dictionary<string, float>();
        genome = new List<GeneType>();
        generation = 0;
        isCyst = false;
        timeAlive = 0f;
        distanceTraveled = 0f;
        energyConsumed = 0f;
    }

    public AmebaData Clone()
    {
        AmebaData clone = new AmebaData();
        clone.generation = this.generation + 1;
        clone.memoryBank = new Dictionary<string, float>(this.memoryBank);
        clone.genome = new List<GeneType>(this.genome);
        clone.species = this.species;
        
        clone.countPacifist = this.countPacifist;
        clone.countPredator = this.countPredator;
        clone.countNeutral = this.countNeutral; // NUEVO
        
        return clone;
    }
}