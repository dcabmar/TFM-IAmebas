using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime; 

public class UIXChartsManager : MonoBehaviour
{
    public static UIXChartsManager Instance;

    [Header("Gráfico y UI")]
    public BarChart barChart;
    public RectTransform panelRect;
    public Button toggleButton;

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
    }

    void Start()
    {
        // 2. Configurar el Gráfico por código
        // LO MOVEMOS AL START: Así le damos tiempo a Unity a inicializar las fuentes de texto
        // y evitamos el error del "Main Thread" y los Jobs de Texto.
        SetupChart();
    }

    void SetupChart()
    {
        if (barChart == null) return;
        barChart.ClearData();
        barChart.RemoveData();

        // 0 = Pacíficas, 1 = Depredadoras, 2 = Neutras
        barChart.theme.sharedTheme.colorPalette[0] = Color.green;
        barChart.theme.sharedTheme.colorPalette[1] = Color.red;
        barChart.theme.sharedTheme.colorPalette[2] = Color.cyan; // NUEVO COLOR

        var serie = barChart.AddSerie<Bar>("Población");
        serie.colorBy = SerieColorBy.Data;
        
        barChart.AddXAxisData("Pacíficas");
        barChart.AddXAxisData("Depredadoras");
        barChart.AddXAxisData("Neutras"); // NUEVA BARRA

        barChart.AddData(0, 0); 
        barChart.AddData(0, 0); 
        barChart.AddData(0, 0); 
    }

    void Update()
    {
        Vector2 target = isExpanded ? shownPos : hiddenPos;
        panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, target, Time.deltaTime * 10f);
    }

    // AÑADIDO PARÁMETRO 'neutrals'
    public void UpdateChartValues(int pacifists, int predators, int neutrals)
    {
        if (barChart == null) return;
        barChart.UpdateData(0, 0, pacifists);
        barChart.UpdateData(0, 1, predators);
        barChart.UpdateData(0, 2, neutrals);
    }

    public void ToggleGraph() { isExpanded = !isExpanded; }
}