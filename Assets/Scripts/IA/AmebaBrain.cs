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

    public bool IsUnknown(string objectTag)
    {
        return !data.memoryBank.ContainsKey(objectTag);
    }

    public float GetMemoryOpinion(string objectTag)
    {
        if (data.memoryBank.ContainsKey(objectTag))
        {
            return data.memoryBank[objectTag];
        }
        return 0f;
    }

    public void Learn(string objectTag, float energyDelta)
    {
        float opinion = 0f;
        if (energyDelta > 0) opinion = 1f;       
        else if (energyDelta < 0) opinion = -1f; 
        else opinion = 0f;                       

        if (!data.memoryBank.ContainsKey(objectTag))
            data.memoryBank.Add(objectTag, opinion);
        else
            data.memoryBank[objectTag] = (data.memoryBank[objectTag] + opinion) / 2f;
    }

    public void SaveBrain()
    {
        // --- CAMBIO CLAVE: Usamos la ruta de la sesión actual ---
        if (string.IsNullOrEmpty(SimulationManager2.CurrentSessionPath)) return;

        string path = SimulationManager2.CurrentSessionPath + "/" + data.name + ".json";
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        
        // Escribimos el archivo
        File.WriteAllText(path, json); 
    }
    
    public void DeleteBrainFile()
    {
        // También buscamos en la carpeta correcta para borrar si hace falta
        if (string.IsNullOrEmpty(SimulationManager2.CurrentSessionPath)) return;

        string path = SimulationManager2.CurrentSessionPath + "/" + data.name + ".json";
        if (File.Exists(path)) File.Delete(path);
    }

    public void LoadBrainData(AmebaData newData)
    {
        this.data = newData;
    }
}