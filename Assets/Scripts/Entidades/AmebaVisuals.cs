using UnityEngine;
using System.Collections;

public class AmebaVisuals : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Transform visualTransform;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (visualTransform == null && spriteRenderer != null)
            visualTransform = spriteRenderer.transform;
    }

    public void ResetVisuals(GeneType species)
    {
        if (visualTransform) visualTransform.localScale = Vector3.one;
        
        // ASIGNACIÓN DE COLORES POR ESPECIE
        if (species == GeneType.Pacifist) spriteRenderer.color = Color.green;
        else if (species == GeneType.Predator) spriteRenderer.color = Color.red;
        else if (species == GeneType.Neutral) spriteRenderer.color = Color.cyan; 
    }

    public void UpdateSize(float maxEnergy)
    {
        float size = Mathf.Max(0.5f, maxEnergy / 100f);
        transform.localScale = Vector3.one * size;
        if(rb != null) rb.mass = size;
    }

    public IEnumerator JellyEffect(Vector2 d)
    {
        if (!visualTransform) yield break;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        visualTransform.rotation = Quaternion.Euler(0, 0, angle);
        visualTransform.localScale = new Vector3(1.3f, 0.7f, 1);
        yield return new WaitForSeconds(0.1f);
        visualTransform.localScale = Vector3.one;
        visualTransform.rotation = Quaternion.identity;
    }

    public IEnumerator FlashColor(Color c, GeneType originalSpecies)
    {
        spriteRenderer.color = c;
        yield return new WaitForSeconds(0.1f);
        ResetVisuals(originalSpecies);
    }
    public void SetCorpseVisuals()
    {
        // Se vuelve de un color gris apagado cuando muere
        spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
    }
}