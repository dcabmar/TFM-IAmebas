using UnityEngine;

// CLASE BASE (Cerebro Genérico)
public abstract class AmebaBehavior
{
    protected AmebaController2 controller;
    public AmebaBehavior(AmebaController2 c) { controller = c; }
    public abstract Vector2 CalculateDesires(float radius);
    public abstract void HandleProximity(AmebaController2 other, float dist);
}

public class PacifistBehavior : AmebaBehavior
{
    public PacifistBehavior(AmebaController2 c) : base(c) { }

    public override Vector2 CalculateDesires(float radius)
    {
        Vector2 force = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == controller.gameObject) continue;
            Vector2 dir = (hit.transform.position - controller.transform.position).normalized;
            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            if (hit.CompareTag("Comida")) force += dir * 2.0f * weight; 
            else if (hit.CompareTag("Ameba") || hit.CompareTag("Muro")) force -= dir * 4f * weight; 
        }
        return force.normalized;
    }
    public override void HandleProximity(AmebaController2 other, float dist) { }
}

// ---------------------------------------------------------
// ESPECIE 2: DEPREDADOR (Caza y Ataca)
// ---------------------------------------------------------
public class PredatorBehavior : AmebaBehavior
{
    public PredatorBehavior(AmebaController2 c) : base(c) { }

    public override Vector2 CalculateDesires(float radius)
    {
        Vector2 force = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, radius);
        bool preyFound = false;

        foreach (var hit in hits)
        {
            if (hit.gameObject == controller.gameObject) continue;
            Vector2 dir = (hit.transform.position - controller.transform.position).normalized;
            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            if (hit.CompareTag("Ameba")) 
            {
                // Obtenemos el script de la otra ameba para ver su ADN
                AmebaController2 otherAmeba = hit.GetComponent<AmebaController2>();
                
                if (otherAmeba != null && otherAmeba.brain != null)
                {
                    // CASO A: ES OTRA DEPREDADORA (Compañera)
                    if (otherAmeba.brain.data.species == GeneType.Predator)
                    {
                        // Opción 1: Ignorarla (fuerza 0).
                        // Opción 2 (Recomendada): Leve repulsión para que no se solapen ("Espacio personal")
                        force -= dir * 1.0f * weight; 
                    }
                    // CASO B: ES UNA PRESA (Pacífica)
                    else
                    {
                        force += dir * 5.0f * weight; // ¡A CAZAR!
                        preyFound = true;
                    }
                }
            }
            else if (hit.CompareTag("Comida") && !preyFound) 
            {
                // Merodea comida solo si no hay presas a la vista
                force += dir * 0.5f * weight; 
            }
            else if (hit.CompareTag("Muro")) 
            {
                force -= dir * 5.0f * weight;
            }
        }
        return force.normalized;
    }

    public override void HandleProximity(AmebaController2 other, float dist)
    {
        // SEGURIDAD: NO ATACAR A OTRAS DEPREDADORAS
        if (other.brain.data.species == GeneType.Predator) return;

        // Si es pacífica y está a tiro, ataca
        if (dist < controller.GetAttackRange())
        {
            controller.TryPerformAttack(other);
        }
    }
}