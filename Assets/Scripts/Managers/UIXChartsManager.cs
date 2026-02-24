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

        // 1. Limpiamos TODO el gráfico
        barChart.ClearData();
        barChart.RemoveData();

        // 2. Cambiamos la paleta de colores del Tema por código
        // Índice 0 = Blanco (Comida), 1 = Verde (Pacíficas), 2 = Rojo (Depredadoras)
        // barChart.theme.sharedTheme.colorPalette[0] = Color.white;
        barChart.theme.sharedTheme.colorPalette[0] = Color.green;
        barChart.theme.sharedTheme.colorPalette[1] = Color.red;

        // 3. Añadimos la serie y le activamos el "ColorByData" automáticamente
        var serie = barChart.AddSerie<Bar>("Población");
        serie.colorBy = SerieColorBy.Data;
        // 4. Configuramos el Eje X (Nombres de las barras)
        // barChart.AddXAxisData("Comida");
        barChart.AddXAxisData("Pacíficas");
        barChart.AddXAxisData("Depredadoras");

        // 5. Añadimos los datos iniciales a 0
        // barChart.AddData(0, 0); // Comida
        barChart.AddData(0, 0); // Pacíficas
        barChart.AddData(0, 0); // Depredadoras
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
        // barChart.UpdateData(0, 0, food);
        barChart.UpdateData(0, 0, pacifists);
        barChart.UpdateData(0, 1, predators);
    }

    public void ToggleGraph()
    {
        isExpanded = !isExpanded;
    }
}
