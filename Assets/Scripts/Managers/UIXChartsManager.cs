using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime; // IMPORTANTE: La librería de XCharts

public class UIXChartsManager : MonoBehaviour
{
    public static UIXChartsManager Instance;

    [Header("Gráfico y UI")]
    public BarChart barChart;
    public RectTransform panelRect;
    public Button toggleButton;

    // Variables de animación
    private bool isExpanded = false;
    private Vector2 hiddenPos;
    private Vector2 shownPos;

    void Awake()
    {
        Instance = this;

        // 1. Configurar posiciones del panel retráctil
        shownPos = panelRect.anchoredPosition; 
        hiddenPos = new Vector2(shownPos.x - panelRect.rect.width, shownPos.y);
        panelRect.anchoredPosition = hiddenPos; // Empezar oculto

        if(toggleButton) toggleButton.onClick.AddListener(ToggleGraph);

        // 2. Configurar el Gráfico por código
        SetupChart();
    }

    void SetupChart()
    {
        if (barChart == null) return;

        // Borramos los datos de ejemplo que trae XCharts por defecto
        barChart.ClearData();

        // Creamos una nueva serie (grupo de barras)
        barChart.AddSerie<Bar>("Población");

        // Añadimos las categorías al Eje X (Abajo)
        barChart.AddXAxisData("Comida");
        barChart.AddXAxisData("Verdes");
        barChart.AddXAxisData("Rojas");

        // Añadimos los datos iniciales (empezamos en 0)
        barChart.AddData(0, 0); // Índice 0 = Comida
        barChart.AddData(0, 0); // Índice 1 = Verdes
        barChart.AddData(0, 0); // Índice 2 = Rojas
    }

    void Update()
    {
        // Animación suave del panel
        Vector2 target = isExpanded ? shownPos : hiddenPos;
        panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, target, Time.deltaTime * 10f);
    }

    // Esta función la llamaremos desde el SimulationManager
    public void UpdateChartValues(int food, int pacifists, int predators)
    {
        if (barChart == null) return;

        // UpdateData(índiceSerie, índiceCategoría, valor)
        barChart.UpdateData(0, 0, food);
        barChart.UpdateData(0, 1, pacifists);
        barChart.UpdateData(0, 2, predators);
    }

    public void ToggleGraph()
    {
        isExpanded = !isExpanded;
    }
}
