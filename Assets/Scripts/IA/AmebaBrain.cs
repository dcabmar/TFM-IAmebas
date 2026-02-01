using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class AmebaBrain : MonoBehaviour
{
    public AmebaData data;

    void Awake()
    {
        data = new AmebaData();
    }

    // Devuelve TRUE si no tenemos registro de este objeto
    public bool IsUnknown(string objectTag)
    {
        return !data.memoryBank.ContainsKey(objectTag);
    }

    // Devuelve la opinión (-1: Miedo, 1: Gusto, 0: Indiferencia)
    public float GetMemoryOpinion(string objectTag)
    {
        if (data.memoryBank.ContainsKey(objectTag))
        {
            return data.memoryBank[objectTag];
        }
        return 0f;
    }

    // Refuerzo positivo o negativo
    public void Learn(string objectTag, float energyDelta)
    {
        float opinion = 0f;
        if (energyDelta > 0) opinion = 1f;       // Comida
        else if (energyDelta < 0) opinion = -1f; // Daño
        else opinion = 0f;                       // Indiferencia (Amebas)

        if (!data.memoryBank.ContainsKey(objectTag))
            data.memoryBank.Add(objectTag, opinion);
        else
            // Promedio ponderado simple para suavizar el aprendizaje
            data.memoryBank[objectTag] = (data.memoryBank[objectTag] + opinion) / 2f;

        // Guardado opcional en tiempo real (puede causar lag si son muchas)
        // SaveBrain(); 
    }

    public void SaveBrain()
    {
        string path = Application.persistentDataPath + "/" + data.name + ".json";
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, json); 
    }
    
    public void DeleteBrainFile()
    {
        string path = Application.persistentDataPath + "/" + data.name + ".json";
        if (File.Exists(path)) File.Delete(path);
    }

    public void LoadBrainData(AmebaData newData)
    {
        this.data = newData;
    }
}