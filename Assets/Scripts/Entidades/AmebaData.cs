using System.Collections.Generic;

[System.Serializable]
public class AmebaData
{
    public string name;
    public int generation;
    public bool isCyst; // Estado guardado (si se guardó dormida)
    
    // Diccionario para guardar experiencias (Nombre -> Valor Promedio)
    public Dictionary<string, float> memoryBank = new Dictionary<string, float>();

    public AmebaData()
    {
        name = "Ameba_" + System.Guid.NewGuid().ToString().Substring(0, 4);
        memoryBank = new Dictionary<string, float>();
        isCyst = false;
    }
}