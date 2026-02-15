using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configuración Zoom")]
    public float zoomSpeed = 5f;      // Sensibilidad de la rueda
    public float minZoom = 2f;        // Zoom máximo (cerca)
    public float maxZoom = 20f;       // Zoom mínimo (lejos)

    [Header("Configuración Arrastre")]
    // 0 = Click Izquierdo, 1 = Click Derecho, 2 = Click Central (Rueda)
    // Recomendado: 1 (Derecho) o 2 (Central) para no interferir con clicks del juego
    public int dragButton = 1;        

    private Camera cam;
    private Vector3 dragOrigin;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    void HandlePan()
    {
        // 1. Al pulsar el botón, guardamos el punto exacto del mundo donde hicimos click
        if (Input.GetMouseButtonDown(dragButton))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // 2. Mientras mantenemos pulsado, calculamos la diferencia
        if (Input.GetMouseButton(dragButton))
        {
            Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
            
            // La diferencia entre donde empezamos y donde estamos ahora
            Vector3 difference = dragOrigin - currentPos;

            // Movemos la cámara esa diferencia para crear el efecto de arrastre
            transform.position += difference;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0f)
        {
            float targetSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
        }
    }
}