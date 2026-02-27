using UnityEngine;
using System.Collections;

public enum AmebaState { Trophozoite, Digesting, Dead } // <-- AÑADIDO DEAD

public class AmebaController2 : MonoBehaviour, IResetable
{
    [HideInInspector] public AmebaStats stats;
    [HideInInspector] public AmebaVisuals visuals;
    [HideInInspector] public AmebaMovement movement;
    [HideInInspector] public AmebaActions actions;
    [HideInInspector] public AmebaBrain brain;

    public AmebaState currentState = AmebaState.Trophozoite;

    private AmebaBehavior currentBehavior;
    private Vector2 lastPosition;

    void Awake()
    {
        stats = GetComponent<AmebaStats>();
        visuals = GetComponent<AmebaVisuals>();
        movement = GetComponent<AmebaMovement>();
        actions = GetComponent<AmebaActions>();
        brain = GetComponent<AmebaBrain>();
        
        GetComponent<Rigidbody2D>().sleepMode = RigidbodySleepMode2D.NeverSleep;
        GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.SetParent(null);
        
        gameObject.tag = "Ameba"; // IMPORTANTE: Volver a ser ameba al respawnear de la pool
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;

        stats.InitStats();
        
        GeneType species = stats.brain.data.species;
        if (species == GeneType.Predator) currentBehavior = new PredatorBehavior(this);
        else if (species == GeneType.Neutral) currentBehavior = new NeutralBehavior(this);
        else currentBehavior = new PacifistBehavior(this);

        movement.ResetMovement();
        visuals.ResetVisuals(species);
        visuals.UpdateSize(stats.maxEnergy);
        
        lastPosition = transform.position;
        currentState = AmebaState.Trophozoite;
    }

    void Update()
    {
        if (currentState == AmebaState.Dead) 
        {
            movement.ApplyDigestingFriction(); // Para que el cadáver frene si lo empujan
            return; 
        }

        float step = Vector2.Distance(transform.position, lastPosition);
        if (step > 0) { stats.UpdateMetrics(step); lastPosition = transform.position; }

        // Chequeo de muerte natural
        if (stats.energy <= 0) { BecomeCorpse(); return; }

        switch (currentState)
        {
            case AmebaState.Trophozoite:
                movement.ApplyFriction();

                if (currentBehavior != null)
                {
                    float currentVisionRadius = stats.sensorRadius * transform.localScale.x;
                    float currentMoveInterval = Mathf.Max(0.2f, stats.moveInterval * transform.localScale.x);

                    Vector2 intention = currentBehavior.CalculateDesires(currentVisionRadius);
                    
                    movement.HandleMovement(intention, currentMoveInterval, stats.moveForce);
                    if(movement.HasMovedJustNow()) stats.energy -= transform.localScale.x * 0.1f;

                    actions.CheckSurroundings(currentBehavior);
                }
                break;

            case AmebaState.Digesting:
                movement.ApplyDigestingFriction();
                break;
        }
    }

    public void SetState(AmebaState newState) { currentState = newState; }

    public void BecomeCorpse()
    {
        if (currentState == AmebaState.Dead) return;
        
        brain.SaveBrain(); 
        currentState = AmebaState.Dead;
        gameObject.tag = "Cadaver"; 
        movement.StopImmediate();

        // ---> SOLUCIÓN COLOR: Detenemos la corrutina de parpadeo de daño y gelatina <---
        visuals.StopAllCoroutines();
        actions.StopAllCoroutines();

        visuals.SetCorpseVisuals();
    }

    public void CompletelyDestroy()
    {
        gameObject.SetActive(false); // Desaparece del todo (usado al comer el cadáver o fagocitosis)
    }

    public void Mitosis()
    {
        AmebaData childMemories = brain.data.Clone();
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector2 spawnPos = (Vector2)transform.position + randomDir * 0.6f;

        GameObject childObj = ObjectPooler2.Instance.SpawnFromPool("Ameba", spawnPos, Quaternion.identity);
        if (childObj != null)
        {
            AmebaController2 child = childObj.GetComponent<AmebaController2>();
            child.brain.LoadBrainData(childMemories);
            child.ResetState();

            child.stats.maxEnergy = stats.maxEnergy / 2f;
            child.stats.energy = child.stats.maxEnergy;
            child.visuals.UpdateSize(child.stats.maxEnergy);
            child.GetComponent<Rigidbody2D>().AddForce(randomDir * 5f, ForceMode2D.Impulse);
        }

        stats.maxEnergy /= 2f;
        stats.energy = stats.maxEnergy;
        visuals.UpdateSize(stats.maxEnergy);
        GetComponent<Rigidbody2D>().AddForce(-randomDir * 5f, ForceMode2D.Impulse);
    }

    void OnDisable()
    {
        if (brain != null && brain.data.timeAlive > 1.0f && currentState != AmebaState.Dead) brain.SaveBrain();
    }
    
    void OnDrawGizmosSelected()
    {
        if(stats) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.sensorRadius * transform.localScale.x);
        }
        if(actions) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, actions.GetAttackRange());
        }
    }
}