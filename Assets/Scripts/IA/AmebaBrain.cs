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

    // --- NUEVO: Decisión de Supervivencia ---
    // Retorna TRUE si el ambiente es hostil y debe enquistarse
    public bool ShouldEncyst(float currentEnergy, float maxEnergy, bool foodNearby)
    {
        // Regla biológica: Si tengo poca energía Y no veo comida -> Me hago Quiste
        if (currentEnergy < (maxEnergy * 0.2f) && !foodNearby)
        {
            return true;
        }
        return false;
    }

    // Retorna TRUE si las condiciones mejoraron (veo comida)
    public bool ShouldWakeUp(bool foodNearby)
    {
        return foodNearby;
    }
    // ----------------------------------------

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
        else opinion = 0.1f;

        if (!data.memoryBank.ContainsKey(objectTag))
            data.memoryBank.Add(objectTag, opinion);
        else
            data.memoryBank[objectTag] = (data.memoryBank[objectTag] + opinion) / 2f;

        SaveBrain();
    }

    public void SaveBrain()
    {
        string path = Application.persistentDataPath + "/" + data.name + ".json";
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        // Descomenta esto si quieres guardar de verdad (cuidado con el rendimiento)
        File.WriteAllText(path, json); 
    }
    
    public void DeleteBrainFile()
    {
        string path = Application.persistentDataPath + "/" + data.name + ".json";
        if (File.Exists(path)) File.Delete(path);
    }
}