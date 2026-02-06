using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AmebaState { Trophozoite, Digesting }
// public enum GeneType { Pacifist, Predator } 

public class AmebaController2 : MonoBehaviour, IResetable
{
    [Header("Components")]
    public AmebaBrain brain;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    public Transform visualTransform;

    [Header("Biological Stats")]
    public AmebaState currentState = AmebaState.Trophozoite;
    public float energy = 100f;
    public float maxEnergy = 100f;       
    public float reproductionThreshold = 120f;  
    
    public float baseInterval = 2.0f;
    public float baseVision = 10f;
    public float baseForce = 2f; // Fuerza bajada para control
    [Header("Gene Stats (Calculated)")]
    public float moveForce;         
    public float moveInterval;      
    public float sensorRadius;      
    public float attackDamage;      
    public bool canAttack;          

    // --- CEREBRO INTERCAMBIABLE ---
    private AmebaBehavior currentBehavior; 
    // ------------------------------

    // Variables Internas
    private float moveTimer = 0f;
    private float lastAttackTime = 0f;
    private float attackCooldown = 1.0f;
    private Vector2 lastPosition; 

    // --- RECUPERADO: Variables de Exploración Visual ---
    private Vector2 wanderTarget;
    private bool isWandering = false;
    // ---------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<AmebaBrain>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualTransform == null && spriteRenderer != null) 
            visualTransform = spriteRenderer.transform;
        
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.SetParent(null);
        rb.bodyType = RigidbodyType2D.Dynamic;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;
        
        if (brain != null)
        {
            if (brain.data == null || brain.data.genome.Count == 0)
            {
                brain.data = new AmebaData();
                brain.data.memoryBank.Add("Ameba", 0f);
                GenerateRandomGenome();
            }
            DetermineSpeciesAndStats(); 
        }
        
        energy = maxEnergy; 
        lastPosition = transform.position;
        currentState = AmebaState.Trophozoite;
        // Reiniciamos variables de patrulla
        isWandering = false;
        UpdateVisuals();
    }

    // --- SISTEMA GENÉTICO ---
    void GenerateRandomGenome()
    {
        brain.data.genome.Clear();
        for (int i = 0; i < 7; i++) 
            brain.data.genome.Add((GeneType)Random.Range(0, 2)); 
    }

    void DetermineSpeciesAndStats()
    {
        var d = brain.data;
        d.countPacifist = 0; 
        d.countPredator = 0;

        foreach (GeneType g in d.genome)
        {
            if (g == GeneType.Pacifist) d.countPacifist++;
            else if (g == GeneType.Predator) d.countPredator++;
        }

        // Stats Pacifista
        moveInterval = Mathf.Max(0.5f, baseInterval - (d.countPacifist * 0.2f));
        sensorRadius = baseVision + (d.countPacifist * 2f);

        // Stats Depredador
        moveForce = baseForce + (d.countPredator * 0.5f); 
        attackDamage = 5f + (d.countPredator * 2f);
        canAttack = (d.countPredator > 0); 

        // Asignar Cerebro
        if (d.countPredator > d.countPacifist)
        {
            d.species = GeneType.Predator;
            currentBehavior = new PredatorBehavior(this); 
        }
        else
        {
            d.species = GeneType.Pacifist;
            currentBehavior = new PacifistBehavior(this); 
        }
        
        UpdateSize();
    }

    // --- UPDATE ---
    void Update()
    {
        if (brain != null)
        {
            brain.data.timeAlive += Time.deltaTime;
            float step = Vector2.Distance(transform.position, lastPosition);
            if (step > 0) { brain.data.distanceTraveled += step; lastPosition = transform.position; }
        }

        if (energy <= 0) { Die(); return; }

        switch (currentState)
        {
            case AmebaState.Trophozoite:
                // Frenado necesario
                rb.linearVelocity = rb.linearVelocity * 0.98f; 

                if (currentBehavior != null)
                {
                    // 1. Calcular Deseos
                    Vector2 intention = currentBehavior.CalculateDesires(sensorRadius);
                    
                    // 2. Moverse (Con visualización recuperada)
                    HandleMovement(intention);

                    // 3. Interactuar
                    CheckSurroundings();
                }
                break;
                
            case AmebaState.Digesting:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
                break;
        }
    }

    void HandleMovement(Vector2 intention)
    {
        if (intention != Vector2.zero)
        {
            isWandering = false; 
            Debug.DrawRay(transform.position, intention * 3f, Color.magenta);
        }
        else
        {
            intention = GetWanderDirection();
        }

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            rb.AddForce(intention * moveForce * rb.mass, ForceMode2D.Impulse);
            StartCoroutine(JellyEffect(intention));
            energy -= transform.localScale.x * 0.1f; 
            moveTimer = 0f;
        }
    }

    Vector2 GetWanderDirection()
    {
        if (!isWandering || Vector2.Distance(transform.position, wanderTarget) < 1f)
        {
            Vector2 randomPoint = Random.insideUnitCircle.normalized * sensorRadius;
            wanderTarget = (Vector2)transform.position + randomPoint;
            isWandering = true;
        }

        Debug.DrawLine(transform.position, wanderTarget, Color.cyan);
        // CORRECCIÓN: Usamos DrawLine para hacer una crucecita en lugar de DrawWireSphere
        Vector3 p = wanderTarget;
        Debug.DrawLine(new Vector3(p.x - 0.2f, p.y, 0), new Vector3(p.x + 0.2f, p.y, 0), Color.cyan);
        Debug.DrawLine(new Vector3(p.x, p.y - 0.2f, 0), new Vector3(p.x, p.y + 0.2f, 0), Color.cyan);

        return (wanderTarget - (Vector2)transform.position).normalized;
    }

    // --- COMBATE ---
    void CheckSurroundings()
    {
        float range = GetAttackRange();
        Collider2D[] close = Physics2D.OverlapCircleAll(transform.position, range);
        
        foreach(var hit in close)
        {
            if(hit.gameObject == gameObject) continue;
            
            if (hit.CompareTag("Ameba"))
            {
                AmebaController2 other = hit.GetComponent<AmebaController2>();
                if(other != null)
                {
                    currentBehavior.HandleProximity(other, Vector2.Distance(transform.position, other.transform.position));
                }
            }
        }
    }

    public void TryPerformAttack(AmebaController2 target)
    {
        if (!canAttack) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        if (target.transform.localScale.x <= transform.localScale.x * 0.6f)
        {
            StartCoroutine(PhagocytosisAmeba(target));
        }
        else
        {
            Vector2 knockDir = (target.transform.position - transform.position).normalized;
            target.TakeDamage(attackDamage, knockDir, this); 
        }
        
        lastAttackTime = Time.time;
    }

    public void TakeDamage(float amount, Vector2 dir, AmebaController2 attacker)
    {
        energy -= amount;
        rb.AddForce(dir * attacker.transform.localScale.x * 5f, ForceMode2D.Impulse); 
        StartCoroutine(FlashColor(Color.white)); 

        if (energy <= 0) 
        {
            if (attacker != null) attacker.ReceiveKillReward(this.maxEnergy);
            Die();
        }
    }

    public void ReceiveKillReward(float victimMaxEnergy)
    {
        float bonus = victimMaxEnergy * 0.5f;
        maxEnergy += bonus;
        energy = maxEnergy; 
        UpdateSize();
        StartCoroutine(FlashColor(Color.yellow)); 
        if (maxEnergy > reproductionThreshold) Mitosis();
    }

    // --- ALIMENTACIÓN ---
    
    IEnumerator PhagocytosisAmeba(AmebaController2 prey)
    {
        currentState = AmebaState.Digesting;
        prey.enabled = false;
        if(prey.GetComponent<Collider2D>()) prey.GetComponent<Collider2D>().enabled = false;
        prey.transform.SetParent(transform);
        prey.transform.localPosition = Vector3.zero;

        float t = 0;
        while(t < 4.0f) {
            t += Time.deltaTime;
            prey.transform.localScale = Vector3.Lerp(prey.transform.localScale, Vector3.zero, t);
            yield return null;
        }

        maxEnergy += prey.maxEnergy;
        energy += prey.maxEnergy;
        
        prey.transform.SetParent(null); // Soltar al pool
        prey.gameObject.SetActive(false);
        currentState = AmebaState.Trophozoite;
        UpdateSize();
        
        if (maxEnergy > reproductionThreshold) Mitosis();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (currentState != AmebaState.Trophozoite) return;

        if (col.CompareTag("Comida"))
        {
            // --- CORRECCIÓN CRÍTICA: LOS DEPREDADORES SON CARNÍVOROS STRICTOS ---
            if (brain.data.species == GeneType.Predator) return; // Ignoran la ensalada
            // -------------------------------------------------------------------

            Nutrient2 n = col.GetComponent<Nutrient2>();
            if(n != null && !n.isBeingDigested) StartCoroutine(PhagocytosisNutrient(n));
        }
        else if (col.CompareTag("Muro")) brain.Learn("Muro", -5f);
    }

    IEnumerator PhagocytosisNutrient(Nutrient2 nutrient)
    {
        currentState = AmebaState.Digesting;
        nutrient.isBeingDigested = true;
        if(nutrient.GetComponent<Collider2D>()) nutrient.GetComponent<Collider2D>().enabled = false;
        nutrient.transform.SetParent(transform);

        float t = 0;
        Vector3 startScale = nutrient.transform.localScale;
        while(t < 2.0f) {
            t += Time.deltaTime;
            nutrient.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        energy += nutrient.energyValue;
        if (energy > maxEnergy) energy = maxEnergy;
        if (maxEnergy < reproductionThreshold * 1.5f) { maxEnergy += nutrient.energyValue; UpdateSize(); } 

        nutrient.transform.SetParent(null); // Soltar al pool
        nutrient.gameObject.SetActive(false);
        currentState = AmebaState.Trophozoite;
        
        if (maxEnergy >= reproductionThreshold) Mitosis();
    }

    void Mitosis()
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
            child.maxEnergy = maxEnergy / 2f; 
            child.energy = child.maxEnergy;
            child.UpdateSize();
            child.GetComponent<Rigidbody2D>().AddForce(randomDir * 5f, ForceMode2D.Impulse);
        }

        maxEnergy /= 2f;
        energy = maxEnergy;
        UpdateSize();
        rb.AddForce(-randomDir * 5f, ForceMode2D.Impulse);
    }

    // --- UTILIDADES ---
    public float GetAttackRange() { return transform.localScale.x * 1.5f; }
    void UpdateSize() { 
        float size = Mathf.Max(0.5f, maxEnergy / 100f);
        transform.localScale = Vector3.one * size; 
        rb.mass = size; 
    }
    void Die() { brain.SaveBrain(); gameObject.SetActive(false); }
    void UpdateVisuals() { 
        if (brain.data.species == GeneType.Pacifist) spriteRenderer.color = Color.green;
        else spriteRenderer.color = Color.red;
    }
    void OnDisable() { if (brain != null && brain.data.timeAlive > 1.0f) brain.SaveBrain(); }

    IEnumerator JellyEffect(Vector2 d) 
    { 
        if(!visualTransform) yield break;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        visualTransform.rotation = Quaternion.Euler(0,0,angle);
        visualTransform.localScale = new Vector3(1.3f, 0.7f, 1);
        yield return new WaitForSeconds(0.1f);
        visualTransform.localScale = Vector3.one;
        visualTransform.rotation = Quaternion.identity;
    }
    
    IEnumerator FlashColor(Color c) 
    { 
        spriteRenderer.color = c; 
        yield return new WaitForSeconds(0.1f); 
        UpdateVisuals(); 
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensorRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetAttackRange());
    }
}