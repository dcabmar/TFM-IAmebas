using System.Collections;
using UnityEngine;
using TMPro;
using System.IO;

public class SimulationManager2 : MonoBehaviour
{
    [Header("Settings")]
    public int maxFood = 100;
    public int maxAmebas = 20;
    public Vector2 spawnArea = new Vector2(20, 10);

    [Header("UI Stats")]
    public TMP_Text amebaCountText; // Arrastra aquí el TextoAmebas
    public TMP_Text foodCountText;  // Arrastra aquí el TextoComida

    private float timer = 0f;

    void Awake()
    {
        // Antes de que empiece nada, borramos el pasado
        DeleteAllBrainFiles();
    }

    void Start()
    {
        StartCoroutine(StartSimulation());
    }

    // --- FUNCIÓN DE LIMPIEZA ---
    void DeleteAllBrainFiles()
    {
        string path = Application.persistentDataPath;
        DirectoryInfo dir = new DirectoryInfo(path);
        
        // Buscamos solo los archivos .json
        FileInfo[] files = dir.GetFiles("*.json");
        
        foreach (FileInfo file in files)
        {
            file.Delete();
        }

        Debug.Log($"<color=yellow>LIMPIEZA INICIAL: Se han borrado {files.Length} archivos de memoria.</color>");
    }

    IEnumerator StartSimulation()
    {
        yield return null; 
        SpawnBatch("Comida", maxFood);
        SpawnBatch("Ameba", maxAmebas);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 0.5f) // Actualizamos cada 0.5s para que el contador se vea fluido
        {
            MaintainBalance();
            timer = 0f;
        }
    }

    void MaintainBalance()
    {
        // Contamos las entidades
        int currentFood = GameObject.FindGameObjectsWithTag("Comida").Length;
        int currentAmebas = GameObject.FindGameObjectsWithTag("Ameba").Length;

        // --- ACTUALIZAR LA UI ---
        if (amebaCountText != null) 
            amebaCountText.text = "Amebas: " + currentAmebas;
            
        if (foodCountText != null) 
            foodCountText.text = "Comida: " + currentFood;
        // ------------------------

        // Reponer si falta (Lógica de siempre)
        if (currentFood < maxFood)
        {
            SpawnBatch("Comida", maxFood - currentFood);
        }

        // if (currentAmebas < 5) 
        // {
        //     SpawnBatch("Ameba", maxAmebas - currentAmebas);
        // }
    }

    void SpawnBatch(string tag, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(-spawnArea.x, spawnArea.x),
                Random.Range(-spawnArea.y, spawnArea.y)
            );
            ObjectPooler2.Instance.SpawnFromPool(tag, randomPos, Quaternion.identity);
        }
    }
}