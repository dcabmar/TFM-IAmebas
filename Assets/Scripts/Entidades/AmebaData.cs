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
    
    // --- GENÉTICA (Añadido) ---
    public List<GeneType> genome = new List<GeneType>(); // La lista de genes
    public GeneType species; // Especie dominante
    
    // Contadores de caché para no tener que recalcular siempre
    public int countPacifist;
    public int countPredator;
    // --------------------------

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
        
        // 1. Heredar Generación (+1)
        clone.generation = this.generation + 1;
        
        // 2. Copiar Recuerdos (Deep Copy)
        clone.memoryBank = new Dictionary<string, float>(this.memoryBank);
        
        // 3. COPIAR GENES (ESTO ES LA HERENCIA)
        // Creamos una lista nueva copiando los elementos de la lista del padre
        clone.genome = new List<GeneType>(this.genome);
        
        // Copiamos también los contadores ya calculados para ahorrar CPU al nacer
        clone.species = this.species;
        clone.countPacifist = this.countPacifist;
        clone.countPredator = this.countPredator;
        
        return clone;
    }
}