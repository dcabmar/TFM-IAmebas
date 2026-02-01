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
    public TMP_Text amebaCountText;
    public TMP_Text foodCountText;

    private float timer = 0f;

    void Awake()
    {
        DeleteAllBrainFiles();
    }

    void Start()
    {
        StartCoroutine(StartSimulation());
    }

    IEnumerator StartSimulation()
    {
        yield return null; // Esperar un frame para asegurar inicialización
        SpawnBatch("Comida", maxFood);
        SpawnBatch("Ameba", maxAmebas);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 0.5f)
        {
            MaintainBalance();
            timer = 0f;
        }
    }

    void MaintainBalance()
    {
        int currentFood = GameObject.FindGameObjectsWithTag("Comida").Length;
        int currentAmebas = GameObject.FindGameObjectsWithTag("Ameba").Length;

        if (amebaCountText != null) amebaCountText.text = "Amebas: " + currentAmebas;
        if (foodCountText != null) foodCountText.text = "Comida: " + currentFood;

        // Solo reponemos comida. Las amebas dependen de la mitosis.
        if (currentFood < maxFood)
        {
            SpawnBatch("Comida", maxFood - currentFood);
        }
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

    void DeleteAllBrainFiles()
    {
        string path = Application.persistentDataPath;
        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] files = dir.GetFiles("*.json");
        foreach (FileInfo file in files) file.Delete();
        Debug.Log($"<color=yellow>LIMPIEZA: Se han borrado {files.Length} cerebros antiguos.</color>");
    }
}