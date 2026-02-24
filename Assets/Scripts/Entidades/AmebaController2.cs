using UnityEngine;
using System.Collections;

public enum AmebaState { Trophozoite, Digesting }

public class AmebaController2 : MonoBehaviour, IResetable
{
    // REFERENCIAS PÚBLICAS (Para que los Behaviors y Actions las vean)
    [HideInInspector] public AmebaStats stats;
    [HideInInspector] public AmebaVisuals visuals;
    [HideInInspector] public AmebaMovement movement;
    [HideInInspector] public AmebaActions actions;
    [HideInInspector] public AmebaBrain brain;

    public AmebaState currentState = AmebaState.Trophozoite;

    // ESTRATEGIA (Behavior)
    private AmebaBehavior currentBehavior;
    private Vector2 lastPosition;

    void Awake()
    {
        // Auto-detectar componentes hermanos
        stats = GetComponent<AmebaStats>();
        visuals = GetComponent<AmebaVisuals>();
        movement = GetComponent<AmebaMovement>();
        actions = GetComponent<AmebaActions>();
        brain = GetComponent<AmebaBrain>();
        
        // Configurar colisiones
        GetComponent<Rigidbody2D>().sleepMode = RigidbodySleepMode2D.NeverSleep;
        GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.SetParent(null);
        
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;

        // 1. Inicializar Stats
        stats.InitStats();
        
        // 2. Asignar Comportamiento (Strategy Pattern)
        GeneType species = stats.brain.data.species;
        if (species == GeneType.Predator) currentBehavior = new PredatorBehavior(this);
        else if (species == GeneType.Neutral) currentBehavior = new NeutralBehavior(this); // <-- NUEVA LÍNEA
        else currentBehavior = new PacifistBehavior(this);

        // 3. Resetear subsistemas
        movement.ResetMovement();
        visuals.ResetVisuals(species);
        visuals.UpdateSize(stats.maxEnergy);
        
        lastPosition = transform.position;
        currentState = AmebaState.Trophozoite;
    }

    void Update()
    {
        // Tracking de métricas
        float step = Vector2.Distance(transform.position, lastPosition);
        if (step > 0) { stats.UpdateMetrics(step); lastPosition = transform.position; }

        // Chequeo de muerte natural
        if (stats.energy <= 0) { Die(); return; }

        // Máquina de Estados
        switch (currentState)
        {
            case AmebaState.Trophozoite:
                movement.ApplyFriction();

                if (currentBehavior != null)
                {
                    // 1. Calculamos la visión "en el aire" (Ej: Base 5 * Escala 2 = 10)
                    float currentVisionRadius = stats.sensorRadius * transform.localScale.x;

                    // 2. Le pasamos ese "10" al cerebro para que busque cosas
                    Vector2 intention = currentBehavior.CalculateDesires(currentVisionRadius);
                    
                    // B. Moverse
                    movement.HandleMovement(intention, stats.moveInterval, stats.moveForce);
                    if(movement.HasMovedJustNow()) stats.energy -= transform.localScale.x * 0.1f;

                    // C. Interactuar (Combate)
                    actions.CheckSurroundings(currentBehavior);
                }
                break;

            case AmebaState.Digesting:
                movement.ApplyDigestingFriction();
                break;
        }
    }

    public void SetState(AmebaState newState)
    {
        currentState = newState;
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
            child.ResetState(); // Esto disparará el InitStats del hijo

            // Ajuste de energía post-parto en el hijo
            child.stats.maxEnergy = stats.maxEnergy / 2f;
            child.stats.energy = child.stats.maxEnergy;
            child.visuals.UpdateSize(child.stats.maxEnergy);
            child.GetComponent<Rigidbody2D>().AddForce(randomDir * 5f, ForceMode2D.Impulse);
        }

        // Ajuste en el padre
        stats.maxEnergy /= 2f;
        stats.energy = stats.maxEnergy;
        visuals.UpdateSize(stats.maxEnergy);
        GetComponent<Rigidbody2D>().AddForce(-randomDir * 5f, ForceMode2D.Impulse);
    }

    public void Die()
    {
        brain.SaveBrain();
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (brain != null && brain.data.timeAlive > 1.0f) brain.SaveBrain();
    }
    
    // Auxiliar para dibujar gizmos del controlador o delegar a subsistemas
    void OnDrawGizmosSelected()
    {
        if(stats) 
        {
            Gizmos.color = Color.yellow;
            // Dibuja el radio real de visión
            Gizmos.DrawWireSphere(transform.position, stats.sensorRadius * transform.localScale.x);
        }
        if(actions)
        {
            Gizmos.color = Color.red;
            // El de ataque lo actualizamos ahora
            Gizmos.DrawWireSphere(transform.position, actions.GetAttackRange());
        }
    }
}