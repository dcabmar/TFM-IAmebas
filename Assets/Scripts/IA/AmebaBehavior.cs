using UnityEngine;

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

            float dist = Vector2.Distance(controller.transform.position, hit.transform.position);
            float weight = 1f / (dist + 0.1f);

            if (hit.CompareTag("Muro"))
            {
                Vector2 closestPoint = hit.ClosestPoint(controller.transform.position);
                Vector2 wallDir = (closestPoint - (Vector2)controller.transform.position).normalized;
                force -= wallDir * 15f * weight; 
            }
            else 
            {
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
                        if (otherAmeba.stats.brain.data.species == GeneType.Predator) force -= dir * 7f * weight;
                    }
                }
            }
        }
        return force.normalized;
    }
    public override void HandleProximity(AmebaController2 other, float dist) { }
}

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
                Vector2 closestPoint = hit.ClosestPoint(controller.transform.position);
                Vector2 wallDir = (closestPoint - (Vector2)controller.transform.position).normalized;
                force -= wallDir * 15f * weight;
            }
            else
            {
                Vector2 dir = (hit.transform.position - controller.transform.position).normalized;

                // --- NUEVO: ATRACCIÓN POR LOS CADÁVERES ---
                if (hit.CompareTag("Cadaver"))
                {
                    force += dir * 6.0f * weight; // Muy atraídos por la carroña fácil
                    preyFound = true;
                }
                else if (hit.CompareTag("Ameba")) 
                {
                    AmebaController2 otherAmeba = hit.GetComponent<AmebaController2>();
                    if (otherAmeba != null && otherAmeba.stats != null && otherAmeba.stats.brain != null)
                    {
                        if (otherAmeba.stats.brain.data.species == GeneType.Predator) force -= dir * 1.0f * weight; 
                        else
                        {
                            force += dir * 5.0f * weight; 
                            preyFound = true;
                        }
                    }
                }
                else if (hit.CompareTag("Comida") && !preyFound) force += dir * 0.5f * weight; 
            }
        }
        return force.normalized;
    }

    public override void HandleProximity(AmebaController2 other, float dist)
    {
        // Si el objetivo es un Cadáver O es una especie diferente a Predator, ataca.
        if (other.gameObject.CompareTag("Cadaver") || other.stats.brain.data.species != GeneType.Predator)
        {
            if (dist < controller.actions.GetAttackRange())
            {
                controller.actions.TryPerformAttack(other);
            }
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
                Vector2 dir = (hit.transform.position - controller.transform.position).normalized;
                force += dir * 2.5f * weight; 
            }
        }
        return force.normalized;
    }

    public override void HandleProximity(AmebaController2 other, float dist)
    {
        // Las neutras solo se defienden de las Amebas vivas que sean depredadores. Ignoran cadáveres.
        if (other.gameObject.CompareTag("Ameba") && other.stats.brain.data.species == GeneType.Predator && controller.stats.canAttack)
        {
            if (dist < controller.actions.GetAttackRange())
            {
                controller.actions.TryPerformAttack(other);
            }
        }
    }
}