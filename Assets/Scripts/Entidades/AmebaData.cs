using System.Collections.Generic;

[System.Serializable]
public class AmebaData
{
    public string name;
    public int generation;
    
    // Métricas para IA futura
    public float timeAlive;
    public float distanceTraveled;
    public float energyConsumed;
    
    // Cerebro: Nombre del Estímulo -> Opinión (-1 a 1)
    public Dictionary<string, float> memoryBank;

    public AmebaData()
    {
        name = "Ameba_" + System.Guid.NewGuid().ToString().Substring(0, 4);
        memoryBank = new Dictionary<string, float>();
        generation = 0;
        timeAlive = 0f;
        distanceTraveled = 0f;
        energyConsumed = 0f;
    }

    // Clonación profunda para la Mitosis
    public AmebaData Clone()
    {
        AmebaData clone = new AmebaData();
        clone.generation = this.generation + 1;
        // Copia exacta de los recuerdos actuales
        clone.memoryBank = new Dictionary<string, float>(this.memoryBank);
        // Las métricas físicas se reinician en 0 para el hijo
        return clone;
    }
}