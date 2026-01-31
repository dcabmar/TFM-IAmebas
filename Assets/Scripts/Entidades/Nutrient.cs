using UnityEngine;

public class Nutrient : MonoBehaviour
{
    // Valor público para que la IA de la ameba pueda "leerlo" a distancia
    public float energyValue = 50f; 
    
    // Evita que dos amebas intenten comerse la misma bola a la vez
    public bool isBeingDigested = false; 

    void OnEnable()
    {
        isBeingDigested = false;
        transform.localScale = Vector3.one * 0.2f; // Tamaño estándar
        
        // Aseguramos que tenga collider y trigger
        if (GetComponent<Collider2D>() == null) gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
    }
}