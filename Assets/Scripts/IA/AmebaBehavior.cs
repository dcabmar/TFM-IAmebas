using UnityEngine;

// CLASE BASE
public abstract class AmebaBehavior
{
    protected AmebaController2 controller;
    public AmebaBehavior(AmebaController2 c) { controller = c; }
    public abstract Vector2 CalculateDesires(float radius);
    public abstract void HandleProximity(AmebaController2 other, float dist);
}

// COMPORTAMIENTO PACIFISTA
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

            // Calculamos la distancia básica para el peso
            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            // LOGICA DIFERENCIADA POR TIPO
            if (hit.CompareTag("Muro"))
            {
                // CORRECCIÓN CLAVE: Usamos el punto más cercano del borde, no el centro
                Vector2 closestPoint = hit.ClosestPoint(controller.transform.position);
                Vector2 wallDir = (closestPoint - (Vector2)controller.transform.position).normalized;
                
                // Huimos perpendicularmente a la superficie del muro
                force -= wallDir * 15f * weight; 
            }
            else 
            {
                // Para objetos redondos (Amebas/Comida) el centro está bien
                Vector2 dir = (hit.transform.position - controller.transform.position).normalized;

                if (hit.CompareTag("Comida")) 
                {
                    force += dir * 2.0f * weight; 
                }
                else if (hit.CompareTag("Ameba"))
                { 
                    AmebaController2 otherAmeba = hit.GetComponent<AmebaController2>();
                    if (otherAmeba != null && otherAmeba.stats != null && otherAmeba.stats.brain != null)
                    {
                        if (otherAmeba.stats.brain.data.species == GeneType.Predator)
                        {
                             force -= dir * 7f * weight;
                        }
                    }
                }
            }
        }
        return force.normalized;
    }
    public override void HandleProximity(AmebaController2 other, float dist) { }
}

// COMPORTAMIENTO DEPREDADOR
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

            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            if (hit.CompareTag("Muro"))
            {
                // CORRECCIÓN CLAVE TAMBIÉN AQUÍ
                Vector2 closestPoint = hit.ClosestPoint(controller.transform.position);
                Vector2 wallDir = (closestPoint - (Vector2)controller.transform.position).normalized;
                force -= wallDir * 15f * weight;
            }
            else
            {
                Vector2 dir = (hit.transform.position - controller.transform.position).normalized;

                if (hit.CompareTag("Ameba")) 
                {
                    AmebaController2 otherAmeba = hit.GetComponent<AmebaController2>();
                    if (otherAmeba != null && otherAmeba.stats != null && otherAmeba.stats.brain != null)
                    {
                        if (otherAmeba.stats.brain.data.species == GeneType.Predator)
                        {
                            force -= dir * 1.0f * weight; 
                        }
                        else
                        {
                            force += dir * 5.0f * weight; 
                            preyFound = true;
                        }
                    }
                }
                else if (hit.CompareTag("Comida") && !preyFound) 
                {
                    force += dir * 0.5f * weight; 
                }
            }
        }
        return force.normalized;
    }

    public override void HandleProximity(AmebaController2 other, float dist)
    {
        if (other.stats.brain.data.species == GeneType.Predator) return;

        if (dist < controller.actions.GetAttackRange())
        {
            controller.actions.TryPerformAttack(other);
        }
    }
}

public class NeutralBehavior : AmebaBehavior
{
    public NeutralBehavior(AmebaController2 c) : base(c) { }

    public override Vector2 CalculateDesires(float radius)
    {
        Vector2 force = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == controller.gameObject) continue;

            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            if (hit.CompareTag("Muro"))
            {
                Vector2 closestPoint = hit.ClosestPoint(controller.transform.position);
                Vector2 wallDir = (closestPoint - (Vector2)controller.transform.position).normalized;
                force -= wallDir * 15f * weight;
            }
            else if (hit.CompareTag("Comida"))
            {
                // Muy atraídas por la comida
                Vector2 dir = (hit.transform.position - controller.transform.position).normalized;
                force += dir * 2.5f * weight; 
            }
            // NOTA: Ignoran por completo a las demás Amebas (ni huyen ni atacan activamente)
        }
        return force.normalized;
    }

    public override void HandleProximity(AmebaController2 other, float dist)
    {
        // DEFENSA PROPIA: Si la otra ameba es un Depredador y YO tengo genes de ataque (canAttack)...
        if (other.stats.brain.data.species == GeneType.Predator && controller.stats.canAttack)
        {
            // Si el depredador se acerca mucho, ¡le muerdo de vuelta!
            if (dist < controller.actions.GetAttackRange())
            {
                controller.actions.TryPerformAttack(other);
            }
        }
    }
}