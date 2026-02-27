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

    public void CheckSurroundings(AmebaBehavior behavior)
    {
        float range = GetAttackRange();
        Collider2D[] close = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in close)
        {
            if (hit.gameObject == gameObject) continue;
            
            // AHORA DETECTAMOS TANTO AMEBAS VIVAS COMO CADÁVERES
            if (hit.CompareTag("Ameba") || hit.CompareTag("Cadaver"))
            {
                AmebaController2 other = hit.GetComponent<AmebaController2>();
                if (other != null)
                {
                    // ---> CORRECCIÓN CLAVE: Distancia al BORDE de la presa, no al centro <---
                    Vector2 closestPoint = hit.ClosestPoint(transform.position);
                    float distanceToEdge = Vector2.Distance(transform.position, closestPoint);

                    behavior.HandleProximity(other, distanceToEdge);
                }
            }
        }
    }

    public void TryPerformAttack(AmebaController2 target)
    {
        attackCooldown = 1.0f + (0.5f * controller.brain.data.countNeutral);
        if (!stats.canAttack) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        // Si es una presa viva mucho más pequeña, instakill (Fagocitosis)
        if (target.currentState != AmebaState.Dead && target.transform.localScale.x <= transform.localScale.x * 0.6f && stats.brain.data.species == GeneType.Predator)
        {
            StartCoroutine(PhagocytosisAmeba(target));
        }
        else // Si es un cadáver O una ameba grande, le damos un bocado (daño)
        {
            Vector2 knockDir = (target.transform.position - transform.position).normalized;
            float scaledDamage = stats.attackDamage * transform.localScale.x;
            target.actions.TakeDamage(scaledDamage, knockDir, controller);
        }
        lastAttackTime = Time.time;
    }

    public void TakeDamage(float amount, Vector2 dir, AmebaController2 attacker)
    {
        // CASO A: SI YA ESTOY MUERTO (CADÁVER)
        if (controller.currentState == AmebaState.Dead)
        {
            // El daño se convierte en energía máxima que me roban
            float bittenAmount = Mathf.Min(amount, stats.maxEnergy);
            stats.maxEnergy -= bittenAmount;
            visuals.UpdateSize(stats.maxEnergy); // Me encojo al ser devorado

            if (attacker != null && attacker.stats.brain.data.species == GeneType.Predator)
            {
                attacker.stats.energy += bittenAmount;
                attacker.stats.AddEnergyConsumed(bittenAmount);
                
                // Si el depredador está lleno, que el exceso de carne le haga crecer en tamaño máximo
                if (attacker.stats.energy > attacker.stats.maxEnergy) {
                    attacker.stats.maxEnergy += bittenAmount * 0.5f;
                    attacker.visuals.UpdateSize(attacker.stats.maxEnergy);
                }

                // ---> NUEVO: COMPROBACIÓN DE MITOSIS AL COMER CARROÑA <---
                if (attacker.stats.maxEnergy >= attacker.stats.reproductionThreshold)
                {
                    attacker.Mitosis();
                }
            }

            // Si se me han comido toda la masa, desaparezco por completo
            if (stats.maxEnergy <= 0.1f) controller.CompletelyDestroy();
            return;
        }

        // CASO B: SI ESTOY VIVO (COMBATE NORMAL)
        stats.energy -= amount;
        rb.AddForce(dir * attacker.transform.localScale.x * 3f, ForceMode2D.Impulse);
        
        // Iniciamos el parpadeo de daño
        StartCoroutine(visuals.FlashColor(Color.white, stats.brain.data.species));

        if (stats.energy <= 0)
        {
            controller.BecomeCorpse();
        }
    }

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
        prey.CompletelyDestroy(); // <--- LÍNEA CORREGIDA
        
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

        float efficiencyMultiplier = 1f; 
        if (stats.brain.data.species == GeneType.Neutral) efficiencyMultiplier = 2f; 
        
        float finalEnergyValue = nutrient.energyValue * efficiencyMultiplier;

        stats.energy += finalEnergyValue;
        stats.AddEnergyConsumed(finalEnergyValue);
        
        if (stats.energy > stats.maxEnergy) stats.energy = stats.maxEnergy;
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

    void OnTriggerEnter2D(Collider2D col)
    {
        if (controller.currentState != AmebaState.Trophozoite) return;

        if (col.CompareTag("Comida"))
        {
            if (stats.brain.data.species == GeneType.Predator) return;
            Nutrient2 n = col.GetComponent<Nutrient2>();
            if (n != null && !n.isBeingDigested) StartCoroutine(PhagocytosisNutrient(n));
        }
        else if (col.CompareTag("Muro")) stats.brain.Learn("Muro", -5f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Muro")) stats.brain.Learn("Muro", -10f);
    }
}