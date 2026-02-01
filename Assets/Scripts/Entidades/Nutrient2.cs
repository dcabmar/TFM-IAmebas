using UnityEngine;

public class Nutrient2 : MonoBehaviour
{
    public float energyValue = 10f; 
    public bool isBeingDigested = false; 

    void OnEnable()
    {
        // 1. Resetear estado lógico
        isBeingDigested = false;
        transform.localScale = Vector3.one * 0.2f; 
        
        // 2. RECUPERAR EL COLLIDER (Solución al bug de comida fantasma)
        Collider2D col = GetComponent<Collider2D>();
        
        if (col == null) 
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }
        
        col.enabled = true; // Aseguramos que sea detectable
    }
}