using UnityEngine;
using System.Collections;

public enum AmebaState { Trophozoite, Digesting }

public class AmebaController2 : MonoBehaviour, IResetable
{
    [Header("Components")]
    public AmebaBrain brain;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    public Transform visualTransform; // Arrastrar el hijo "Visuals" aquí
    private Color defaultColor;

    [Header("Biological Stats")]
    public AmebaState currentState = AmebaState.Trophozoite;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float baseMaxEnergy = 100f; // Genético base

    [Header("Movement Physics")]
    public float baseMoveForce = 5f;
    public float maxSpeed = 2f;
    public float baseMoveInterval = 0.5f;
    private float currentMoveInterval;
    private float moveTimer = 0f;
    private Vector2 lastPosition; // Para medir distancia recorrida

    [Header("Phagocytosis")]
    public float digestionTime = 2.0f;

    [Header("Senses & Personality")]
    public float sensorRadius = 15f;
    private float baseSensorRadius;
    
    // Genes de Personalidad
    public float totalPersonalityPoints = 10f;
    [SerializeField] private float curiosityWeight;
    [SerializeField] private float greedWeight;
    [SerializeField] private float fearWeight;

    // Navegación
    private Vector2 wanderTarget;
    private bool isWandering = false;
    private bool foodNearby = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<AmebaBrain>();
        
        // Buscamos el renderer en los hijos por si usas la estructura visual separada
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualTransform == null && spriteRenderer != null) 
            visualTransform = spriteRenderer.transform;

        defaultColor = spriteRenderer.color;
        baseSensorRadius = sensorRadius;
        
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        RandomizePersonality();
        UpdateStats();
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.SetParent(null);
        rb.bodyType = RigidbodyType2D.Dynamic;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;

        // Resetear Valores
        UpdateStats();
        energy = maxEnergy;
        lastPosition = transform.position;

        // Limpieza de Cerebro
        if (brain != null)
        {
            brain.DeleteBrainFile();
            brain.data = new AmebaData();
            
            // --- INYECCIÓN DE INDIFERENCIA ---
            // Nace sabiendo que las amebas valen 0. Así las ignora desde el frame 1.
            brain.data.memoryBank.Add("Ameba", 0f);
        }

        RandomizePersonality();
        currentState = AmebaState.Trophozoite;
        
        // Reset visual
        if (visualTransform) visualTransform.localScale = Vector3.one;
        spriteRenderer.color = defaultColor;
    }

    void UpdateStats()
    {
        // El tamaño depende de la energía máxima (capacidad)
        float energySurplus = maxEnergy - baseMaxEnergy;
        float newSize = 1f + (energySurplus / 50f);
        newSize = Mathf.Max(newSize, 0.5f);

        // Escalamos el PADRE (Hitbox física)
        transform.localScale = Vector3.one * newSize;

        // Ajustamos stats físicos
        currentMoveInterval = baseMoveInterval * newSize;
        rb.mass = newSize;
        sensorRadius = baseSensorRadius * newSize;
    }

    void RandomizePersonality()
    {
        float rC = Random.Range(0.1f, 1f);
        float rG = Random.Range(0.1f, 1f);
        float rF = Random.Range(0.1f, 1f);
        float total = rC + rG + rF;
        
        curiosityWeight = (rC / total) * totalPersonalityPoints;
        greedWeight = (rG / total) * totalPersonalityPoints;
        fearWeight = (rF / total) * totalPersonalityPoints;
    }

    void Update()
    {
        // Recolección de datos para IA
        if (brain != null && brain.data != null)
        {
            brain.data.timeAlive += Time.deltaTime;
            float step = Vector2.Distance(transform.position, lastPosition);
            if (step > 0)
            {
                brain.data.distanceTraveled += step;
                lastPosition = transform.position;
            }
        }

        switch (currentState)
        {
            case AmebaState.Trophozoite:
                HandleActiveState();
                break;
            case AmebaState.Digesting:
                // Frenado mientras come
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
                break;
        }

        if (energy <= 0) OnDeath();
    }

    void HandleActiveState()
    {
        Vector2 intention = CalculateDesires();

        if (intention == Vector2.zero)
        {
            intention = GetWanderDirection();
        }
        else
        {
            isWandering = false;
            Debug.DrawRay(transform.position, intention * 3f, Color.magenta);
        }

        // Movimiento por impulsos
        moveTimer += Time.deltaTime;
        if (moveTimer >= currentMoveInterval)
        {
            if (intention != Vector2.zero)
            {
                float push = baseMoveForce * rb.mass;
                rb.AddForce(intention * push, ForceMode2D.Impulse);
                StartCoroutine(JellyEffect(intention));
                
                // Coste de energía proporcional al tamaño (f = float)
                energy -= transform.localScale.x * 0.07f; 
            }
            moveTimer = 0f;
        }

        rb.linearDamping = 5f;
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    Vector2 CalculateDesires()
    {
        Vector2 totalForce = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sensorRadius);
        foodNearby = false;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || !hit.gameObject.activeSelf) continue;

            Vector2 dir = (hit.transform.position - transform.position).normalized;
            float dist = Vector2.Distance(transform.position, hit.transform.position);

            // Mejora para Muros (usar punto más cercano)
            if (hit.CompareTag("Muro"))
            {
                Vector3 closest = hit.ClosestPoint(transform.position);
                dir = (closest - transform.position).normalized;
                dist = Vector2.Distance(transform.position, closest);
            }

            float prox = 1f / (dist + 0.1f);
            float perceivedValue = 1f;

            if (hit.CompareTag("Comida"))
            {
                foodNearby = true;
                Nutrient2 n = hit.GetComponent<Nutrient2>();
                if (n != null) perceivedValue = n.energyValue;
            }

            // Lógica de Cerebro
            if (brain.IsUnknown(hit.tag))
            {
                // Curiosidad (Si Ameba=0, IsUnknown es false, así que esto se salta)
                totalForce += dir * curiosityWeight * prox;
            }
            else
            {
                float opinion = brain.GetMemoryOpinion(hit.tag); // 1, -1, o 0

                if (opinion > 0) // Comida
                    totalForce += dir * (opinion * perceivedValue) * greedWeight * prox;
                else if (opinion < 0) // Peligro
                    totalForce += (dir * -1) * Mathf.Abs(opinion) * fearWeight * prox;
                
                // Si opinion == 0 (Ameba), no suma nada. Indiferencia total.
            }
        }

        // Solución Paradoja de Buridán (Empate Técnico)
        if (totalForce == Vector2.zero && foodNearby)
        {
            return Random.insideUnitCircle.normalized;
        }

        return totalForce.normalized;
    }

    Vector2 GetWanderDirection()
    {
        if (!isWandering || Vector2.Distance(transform.position, wanderTarget) < 1f)
        {
            Vector2 randomPoint = Random.insideUnitCircle.normalized * sensorRadius;
            wanderTarget = (Vector2)transform.position + randomPoint;
            isWandering = true;
        }
        return (wanderTarget - (Vector2)transform.position).normalized;
    }

    // Efecto visual separado de la física para evitar hitbox gigante
    IEnumerator JellyEffect(Vector2 direction)
    {
        if (visualTransform == null) yield break;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        visualTransform.rotation = Quaternion.Euler(0, 0, angle);

        Vector3 originalScale = Vector3.one; 
        
        // Deformar solo lo visual
        visualTransform.localScale = new Vector3(originalScale.x * 1.4f, originalScale.y * 0.6f, 1);
        yield return new WaitForSeconds(0.1f);
        visualTransform.localScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.1f, 1);
        yield return new WaitForSeconds(0.1f);
        visualTransform.localScale = originalScale;
        visualTransform.rotation = Quaternion.identity;
    }

    // --- COLISIONES Y ALIMENTACIÓN ---

    void GestionarComida(Collider2D other)
    {
        if (currentState != AmebaState.Trophozoite) return;

        if (other.CompareTag("Comida"))
        {
            Nutrient2 n = other.GetComponent<Nutrient2>();
            if (n != null && !n.isBeingDigested)
            {
                StartCoroutine(Phagocytosis(n));
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        GestionarComida(other);
        if (other.CompareTag("Muro")) brain.Learn("Muro", -5f);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        GestionarComida(other); // Clave para cuando termina de comer una y está sobre otra
        
        if (other.CompareTag("Muro"))
        {
            float damage = Time.deltaTime * 10f;
            energy -= damage;
            brain.Learn("Muro", -damage);
        }
    }

    IEnumerator Phagocytosis(Nutrient2 prey)
    {
        currentState = AmebaState.Digesting;
        
        if (prey == null || !prey.gameObject.activeSelf)
        {
            currentState = AmebaState.Trophozoite;
            yield break;
        }

        prey.isBeingDigested = true;
        if(prey.GetComponent<Collider2D>()) prey.GetComponent<Collider2D>().enabled = false;

        prey.transform.SetParent(this.transform);
        prey.transform.localPosition = Vector3.zero;

        // Animación digestión
        float timer = 0;
        Vector3 startScale = prey.transform.localScale;
        while (timer < digestionTime)
        {
            timer += Time.deltaTime;
            prey.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / digestionTime);
            yield return null;
        }

        // Crecimiento de capacidad
        if (maxEnergy < baseMaxEnergy * 2f) 
        {
            maxEnergy += prey.energyValue; 
            UpdateStats(); 
        }

        energy += prey.energyValue;
        if (energy > maxEnergy) energy = maxEnergy;

        // Limpieza presa
        brain.data.energyConsumed += prey.energyValue;
        brain.Learn("Comida", prey.energyValue);
        prey.transform.SetParent(null);
        prey.gameObject.SetActive(false);

        // Chequeo Mitosis
        if (maxEnergy >= baseMaxEnergy * 2f)
        {
            Mitosis();
        }
        else
        {
            currentState = AmebaState.Trophozoite;
        }
    }

    void Mitosis()
    {
        AmebaData childMemories = brain.data.Clone();

        // 1. Asegurar Indiferencia en el Hijo
        if (childMemories.memoryBank.ContainsKey("Ameba")) childMemories.memoryBank["Ameba"] = 0f;
        else childMemories.memoryBank.Add("Ameba", 0f);

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector2 spawnPos = (Vector2)transform.position + randomDir * 0.6f; // Un poco desplazado
        
        GameObject childObj = ObjectPooler2.Instance.SpawnFromPool("Ameba", spawnPos, Quaternion.identity);
        
        if (childObj != null)
        {
            AmebaController2 child = childObj.GetComponent<AmebaController2>();
            child.brain.LoadBrainData(childMemories);
            
            // Hijo hereda mitad de capacidad
            child.maxEnergy = this.maxEnergy / 2f;
            if (child.maxEnergy < baseMaxEnergy) child.maxEnergy = baseMaxEnergy;
            child.energy = child.maxEnergy; 
            child.UpdateStats();

            // EMPUJÓN FÍSICO AL HIJO
            if (child.GetComponent<Rigidbody2D>())
                child.GetComponent<Rigidbody2D>().AddForce(randomDir * 10f, ForceMode2D.Impulse);
        }

        // Padre reduce capacidad
        this.maxEnergy = this.maxEnergy / 2f;
        if (this.maxEnergy < baseMaxEnergy) this.maxEnergy = baseMaxEnergy;
        this.energy = this.maxEnergy; 
        this.brain.data.generation++;
        
        UpdateStats(); // Padre se encoge
        currentState = AmebaState.Trophozoite;

        // EMPUJÓN FÍSICO AL PADRE (Opuesto)
        rb.AddForce(-randomDir * 10f, ForceMode2D.Impulse);
        StartCoroutine(FlashColor(Color.green));
    }

    void OnDeath()
    {
        // Guardar datos solo si vivió lo suficiente
        if (brain != null && brain.data.timeAlive > 1.0f)
        {
            brain.SaveBrain();
        }
        gameObject.SetActive(false);
    }
    
    // Helper visual
    IEnumerator FlashColor(Color c)
    {
        Color old = spriteRenderer.color;
        spriteRenderer.color = c;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = old;
    }
}