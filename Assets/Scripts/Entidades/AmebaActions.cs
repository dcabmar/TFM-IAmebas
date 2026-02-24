using UnityEngine;
using System.Collections;

public class AmebaActions : MonoBehaviour
{
    private AmebaController2 controller;
    private AmebaStats stats;
    private AmebaVisuals visuals;
    private AmebaMovement movement;
    private Rigidbody2D rb;

    private float lastAttackTime = 0f;
    private float attackCooldown = 1.0f;

    void Awake()
    {
        controller = GetComponent<AmebaController2>();
        stats = GetComponent<AmebaStats>();
        visuals = GetComponent<AmebaVisuals>();
        movement = GetComponent<AmebaMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    public float GetAttackRange() { return transform.localScale.x * 1.25f; }

    // --- COMBATE ---
    public void CheckSurroundings(AmebaBehavior behavior)
    {
        float range = GetAttackRange();
        Collider2D[] close = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in close)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.CompareTag("Ameba"))
            {
                AmebaController2 other = hit.GetComponent<AmebaController2>();
                if (other != null)
                {
                    behavior.HandleProximity(other, Vector2.Distance(transform.position, other.transform.position));
                }
            }
        }
    }

    public void TryPerformAttack(AmebaController2 target)
    {
        if (!stats.canAttack) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        if (target.transform.localScale.x <= transform.localScale.x * 0.6f && stats.brain.data.species == GeneType.Predator)
        {
            StartCoroutine(PhagocytosisAmeba(target));
        }
        else
        {
            Vector2 knockDir = (target.transform.position - transform.position).normalized;
            target.actions.TakeDamage(stats.attackDamage, knockDir, controller);
        }
        lastAttackTime = Time.time;
    }

    public void TakeDamage(float amount, Vector2 dir, AmebaController2 attacker)
    {
        stats.energy -= amount;
        rb.AddForce(dir * attacker.transform.localScale.x * 3f, ForceMode2D.Impulse);
        StartCoroutine(visuals.FlashColor(Color.white, stats.brain.data.species));

        if (stats.energy <= 0)
        {
            if (attacker != null && attacker.stats.brain.data.species == GeneType.Predator) attacker.actions.ReceiveKillReward(stats.maxEnergy);
            controller.Die();
        }
    }

    public void ReceiveKillReward(float victimMaxEnergy)
    {
        float bonus = victimMaxEnergy * 0.5f;
        stats.maxEnergy += bonus;
        stats.energy = stats.maxEnergy;
        stats.AddEnergyConsumed(bonus);
        
        
        visuals.UpdateSize(stats.maxEnergy);
        StartCoroutine(visuals.FlashColor(Color.yellow, stats.brain.data.species));
        
        if (stats.maxEnergy > stats.reproductionThreshold) controller.Mitosis();
    }

    // --- ALIMENTACIÓN (CORRUTINAS) ---
    IEnumerator PhagocytosisAmeba(AmebaController2 prey)
    {
        controller.SetState(AmebaState.Digesting);
        movement.StopImmediate();

        prey.enabled = false;
        if (prey.GetComponent<Collider2D>()) prey.GetComponent<Collider2D>().enabled = false;
        prey.transform.SetParent(transform);
        prey.transform.localPosition = Vector3.zero;

        float t = 0;
        while (t < 4.0f)
        {
            t += Time.deltaTime;
            prey.transform.localScale = Vector3.Lerp(prey.transform.localScale, Vector3.zero, t);
            yield return null;
        }

        stats.maxEnergy += prey.stats.maxEnergy;
        stats.energy += prey.stats.maxEnergy;
        stats.AddEnergyConsumed(prey.stats.maxEnergy);

        prey.transform.SetParent(null);
        prey.gameObject.SetActive(false);
        
        controller.SetState(AmebaState.Trophozoite);
        visuals.UpdateSize(stats.maxEnergy);

        if (stats.maxEnergy > stats.reproductionThreshold) controller.Mitosis();
    }

    IEnumerator PhagocytosisNutrient(Nutrient2 nutrient)
    {
        controller.SetState(AmebaState.Digesting);
        movement.StopImmediate();

        nutrient.isBeingDigested = true;
        if (nutrient.GetComponent<Collider2D>()) nutrient.GetComponent<Collider2D>().enabled = false;
        nutrient.transform.SetParent(transform);

        float t = 0;
        Vector3 startScale = nutrient.transform.localScale;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            nutrient.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        // ---> MODIFICACIÓN: La energía extra SOLO se aplica si la especie es Neutra <---
        float efficiencyMultiplier = 1f; // Por defecto (Pacíficas y Depredadoras) ganan lo normal
        
        if (stats.brain.data.species == GeneType.Neutral)
        {
            // Las Neutras son súper eficientes procesando nutrientes (50% extra)
            efficiencyMultiplier = 2f; 
        }
        
        float finalEnergyValue = nutrient.energyValue * efficiencyMultiplier;

        stats.energy += finalEnergyValue;
        stats.AddEnergyConsumed(finalEnergyValue);
        
        if (stats.energy > stats.maxEnergy) stats.energy = stats.maxEnergy;
        
        // Crecimiento de tamaño
        if (stats.maxEnergy < stats.reproductionThreshold * 1.5f) 
        { 
            stats.maxEnergy += finalEnergyValue; 
            visuals.UpdateSize(stats.maxEnergy); 
        }

        nutrient.transform.SetParent(null);
        nutrient.gameObject.SetActive(false);
        
        controller.SetState(AmebaState.Trophozoite);

        if (stats.maxEnergy >= stats.reproductionThreshold) controller.Mitosis();
    }

    // --- TRIGGERS ---
    void OnTriggerEnter2D(Collider2D col)
    {
        if (controller.currentState != AmebaState.Trophozoite) return;

        if (col.CompareTag("Comida"))
        {
            if (stats.brain.data.species == GeneType.Predator) return; // Depredadores ignoran comida

            Nutrient2 n = col.GetComponent<Nutrient2>();
            if (n != null && !n.isBeingDigested) StartCoroutine(PhagocytosisNutrient(n));
        }
        else if (col.CompareTag("Muro"))
        {
            stats.brain.Learn("Muro", -5f);
        }
    }
        void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Muro"))
        {
            stats.brain.Learn("Muro", -10f);
        }
    }
}