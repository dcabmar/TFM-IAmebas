using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System.Text; // Necesario para crear el CSV
using System.Globalization; // Para que los puntos decimales sean puntos (1.5) y no comas

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

    // --- RUTA DE GUARDADO ---
    // Variable estática para que las amebas sepan dónde guardar sin buscar referencias
    public static string CurrentSessionPath;

    private float timer = 0f;

    void Awake()
    {
        Instance = this;
        SetupDataFolder();
    }

    void Start()
    {
        StartCoroutine(StartSimulation());
    }

    // 1. CONFIGURACIÓN DE CARPETA ÚNICA POR SESIÓN
    void SetupDataFolder()
    {
        // 1. OBTENER LA RUTA DEL PROYECTO
        // Application.dataPath nos da ".../TuProyecto/Assets"
        // Directory.GetParent nos sube un nivel a ".../TuProyecto"
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        
        // 2. CREAR UNA CARPETA MAESTRA "DATASETS"
        // Para no llenar la raíz de carpetas, las metemos todas en una llamada "Simulation_Logs"
        string baseDataFolder = Path.Combine(projectPath, "Simulation_Logs");
        
        if (!Directory.Exists(baseDataFolder))
            Directory.CreateDirectory(baseDataFolder);

        // 3. CREAR LA CARPETA DE LA SESIÓN ACTUAL
        string folderName = "Sim_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        CurrentSessionPath = Path.Combine(baseDataFolder, folderName);

        // Crear el directorio físico
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

    // 2. EVENTO AL CERRAR LA SIMULACIÓN
    void OnApplicationQuit()
    {
        // 1. Antes de nada, obligamos a las amebas vivas a guardar sus datos
        ForceSaveAllActiveAmebas();

        // 2. Ahora que seguro que están los JSONs, generamos el CSV
        GenerateDatasetCSV();
    }

    void ForceSaveAllActiveAmebas()
    {
        // Buscamos todas las amebas que siguen vivas en la escena
        GameObject[] activeAmebas = GameObject.FindGameObjectsWithTag("Ameba");

        foreach (GameObject obj in activeAmebas)
        {
            AmebaController2 controller = obj.GetComponent<AmebaController2>();
            if (controller != null && controller.brain != null)
            {
                // Guardamos manualmente solo si han vivido lo suficiente
                if (controller.brain.data.timeAlive > 1.0f)
                {
                    controller.brain.SaveBrain();
                }
            }
        }
        
        Debug.Log($"<color=orange>FORZADO GUARDADO: {activeAmebas.Length} amebas vivas han guardado sus datos.</color>");
    }

    // 3. GENERADOR DE DATASET ACUMULATIVO (GLOBAL)
    public void GenerateDatasetCSV()
    {
        // A. Obtener los JSONs de ESTA sesión
        DirectoryInfo dir = new DirectoryInfo(CurrentSessionPath);
        FileInfo[] files = dir.GetFiles("*.json");

        if (files.Length == 0) return;

        // B. Definir la ruta del CSV MAESTRO (Fuera de la carpeta de sesión)
        // Estará en ".../Simulation_Logs/Global_Dataset.csv"
        string parentFolder = Directory.GetParent(CurrentSessionPath).FullName;
        string globalCsvPath = Path.Combine(parentFolder, "Global_Dataset.csv");

        StringBuilder csvContent = new StringBuilder();

        // C. Lógica de Cabecera (Header)
        // Solo escribimos los títulos si el archivo es nuevo (no existe aún)
        if (!File.Exists(globalCsvPath))
        {
            csvContent.AppendLine("ID,Generation,TimeAlive,Distance,EnergyConsumed");
        }

        // Usamos el nombre de la carpeta (ej: "Sim_2023-10-25...") como ID de la partida
        // string simID = new DirectoryInfo(CurrentSessionPath).Name;

        // D. Procesar los datos
        foreach (FileInfo file in files)
        {
            string json = File.ReadAllText(file.FullName);
            AmebaData data = Newtonsoft.Json.JsonConvert.DeserializeObject<AmebaData>(json);

            if (data != null)
            {

                string newLine = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4:F2}",               // Nueva columna para identificar la partida
                    data.name,
                    data.generation,
                    data.timeAlive,
                    data.distanceTraveled,
                    data.energyConsumed
                );

                csvContent.AppendLine(newLine);
            }
        }

        // E. GUARDADO EN MODO "APPEND" (Añadir al final)
        // Esto agrega las nuevas líneas sin borrar lo anterior.
        File.AppendAllText(globalCsvPath, csvContent.ToString());

        Debug.Log($"<color=green>DATOS AÑADIDOS AL DATASET GLOBAL: {globalCsvPath}</color>");
    }
    
}