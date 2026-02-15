using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System.Text; 
using System.Globalization; 

public class SimulationManager2 : MonoBehaviour
{
    public static SimulationManager2 Instance;

    [Header("Settings")]
    public int maxFood = 100;
    public int maxAmebas = 20;
    public Vector2 spawnArea = new Vector2(20, 10);

    [Header("UI Stats")]
    public TMP_Text amebaCountText;
    public TMP_Text foodCountText;
    public TMP_Text speedText; // <--- NUEVO: Texto para la velocidad

    // --- RUTA DE GUARDADO ---
    public static string CurrentSessionPath;

    private float timer = 0f;
    private float timeScaleBeforePause = 1f;
    private bool isPaused = false;

    void Awake()
    {
        Instance = this;
        SetupDataFolder();
    }

    void Start()
    {
        StartCoroutine(StartSimulation());
        UpdateSpeedUI(1f); // Iniciar texto a 1x
    }

    void SetupDataFolder()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string baseDataFolder = Path.Combine(projectPath, "Simulation_Logs");
        
        if (!Directory.Exists(baseDataFolder))
            Directory.CreateDirectory(baseDataFolder);

        string folderName = "Sim_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        CurrentSessionPath = Path.Combine(baseDataFolder, folderName);

        if (!Directory.Exists(CurrentSessionPath))
        {
            Directory.CreateDirectory(CurrentSessionPath);
        }

        Debug.Log($"<color=cyan>SESIÓN INICIADA: Datos guardados en: {CurrentSessionPath}</color>");
    }

    IEnumerator StartSimulation()
    {
        yield return null; 
        SpawnBatch("Comida", maxFood);
        SpawnBatch("Ameba", maxAmebas);
    }

    void Update()
    {
        // 1. CONTROL DE TIEMPO (NUEVO)
        HandleTimeInput();

        // 2. LOGICA DE BALANCEO
        timer += Time.deltaTime; // Time.deltaTime escala con el tiempo, así que esto sigue funcionando bien
        if (timer > 0.5f)
        {
            MaintainBalance();
            timer = 0f;
        }
    }

    // --- NUEVA FUNCIÓN: CONTROL DE VELOCIDAD ---
    void HandleTimeInput()
    {
        // Teclas de número para velocidad
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTimeScale(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTimeScale(2f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTimeScale(5f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTimeScale(10f); // Velocidad "Warp"

        // Barra espaciadora para Pausa
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }
    }

    void SetTimeScale(float scale)
    {
        if (isPaused) isPaused = false; // Si cambiamos velocidad, quitamos pausa
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // IMPORTANTE: Ajustar físicas para que no se rompan
        UpdateSpeedUI(scale);
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            if (speedText != null) speedText.text = "Velocidad: PAUSA";
        }
        else
        {
            SetTimeScale(timeScaleBeforePause);
        }
    }

    void UpdateSpeedUI(float scale)
    {
        if (speedText != null)
            speedText.text = $"Velocidad: {scale}x";
    }
    // -------------------------------------------

    void MaintainBalance()
    {
        int currentFood = GameObject.FindGameObjectsWithTag("Comida").Length;
        int currentAmebas = GameObject.FindGameObjectsWithTag("Ameba").Length;

        if (amebaCountText != null) amebaCountText.text = "Amebas: " + currentAmebas;
        if (foodCountText != null) foodCountText.text = "Comida: " + currentFood;

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

    void OnApplicationQuit()
    {
        ForceSaveAllActiveAmebas();
        GenerateDatasetCSV();
    }

    void ForceSaveAllActiveAmebas()
    {
        GameObject[] activeAmebas = GameObject.FindGameObjectsWithTag("Ameba");
        foreach (GameObject obj in activeAmebas)
        {
            AmebaController2 controller = obj.GetComponent<AmebaController2>();
            if (controller != null && controller.brain != null)
            {
                if (controller.brain.data.timeAlive > 1.0f)
                {
                    controller.brain.SaveBrain();
                }
            }
        }
        Debug.Log($"<color=orange>FORZADO GUARDADO: {activeAmebas.Length} amebas vivas han guardado sus datos.</color>");
    }

    public void GenerateDatasetCSV()
    {
        DirectoryInfo dir = new DirectoryInfo(CurrentSessionPath);
        FileInfo[] files = dir.GetFiles("*.json");

        if (files.Length == 0) return;

        string parentFolder = Directory.GetParent(CurrentSessionPath).FullName;
        string globalCsvPath = Path.Combine(parentFolder, "Global_Dataset.csv");

        StringBuilder csvContent = new StringBuilder();

        if (!File.Exists(globalCsvPath))
        {
            csvContent.AppendLine("ID,Generation,TimeAlive,Distance,EnergyConsumed");
        }

        foreach (FileInfo file in files)
        {
            string json = File.ReadAllText(file.FullName);
            AmebaData data = Newtonsoft.Json.JsonConvert.DeserializeObject<AmebaData>(json);

            if (data != null)
            {
                string newLine = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4:F2}",
                    data.name,
                    data.generation,
                    data.timeAlive,
                    data.distanceTraveled,
                    data.energyConsumed
                );
                csvContent.AppendLine(newLine);
            }
        }
        File.AppendAllText(globalCsvPath, csvContent.ToString());
        Debug.Log($"<color=green>DATOS AÑADIDOS AL DATASET GLOBAL: {globalCsvPath}</color>");
    }
}