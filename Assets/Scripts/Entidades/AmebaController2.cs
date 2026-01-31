using UnityEngine;
using System.Collections;

public enum AmebaState { Trophozoite, Cyst, Digesting }

public class AmebaController2 : MonoBehaviour, IResetable
{
    
    [Header("Components")]
    public AmebaBrain brain;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor;

    [Header("Biological Stats")]
    public AmebaState currentState = AmebaState.Trophozoite;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float baseMaxEnergy = 100f;
    
    [Header("Gel Movement (Pseudopods)")]
    public float moveForce = 10f; // Un poco más de fuerza por impulso
    public float maxSpeed = 2f;
    public float moveInterval = 3.0f; // <--- CAMBIO CLAVE: 3 segundos de pausa
    private float moveTimer = 0f;

    [Header("Movement & Agility")]
    public float baseMoveForce = 5f;

    public float baseMoveInterval = 0.5f; // Intervalo base para tamaño 1
    private float currentMoveInterval;    // Intervalo real calculado

    
    [Header("Phagocytosis")]
    public float digestionTime = 2.0f;
    
    [Header("Personality")]
    public float sensorRadius = 15f;
    private float baseSensorRadius;
    public float totalPersonalityPoints = 10f;
    [SerializeField] private float curiosityWeight;
    [SerializeField] private float greedWeight;
    [SerializeField] private float fearWeight;

    [Header("Exploration")]
    private Vector2 wanderTarget; // El punto imaginario al que quiero ir
    private bool isWandering = false; // ¿Tengo un destino fijo ahora mismo?

    private bool foodNearby = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<AmebaBrain>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultColor = spriteRenderer.color;
        
        // Guardamos el rango de visión original definido en el Inspector
        baseSensorRadius = sensorRadius; // <--- AÑADE ESTO
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; 
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    void Start()
    {
        // 1. ASIGNAR ADN (PERSONALIDAD)
        // Se ejecuta una sola vez al iniciar el juego. 
        // Esta ameba siempre será "Curiosa" o "Miedosa" toda su vida.
        RandomizePersonality();

        // 2. ASIGNAR APARIENCIA (COLOR)
        // Su color también es genético y no cambia.
        // AssignRandomColor();
        
        // Inicializamos el estado base
        UpdateStats();
    }
    public void ResetState()
    {
        StopAllCoroutines();
        transform.localScale = Vector3.one;
        transform.SetParent(null);
        // isBeingEaten = false;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        if(GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;

        // Reset de variables vitales
        UpdateStats();
        energy = maxEnergy;
        
        // Borramos la memoria (Cerebro vacío), pero mantenemos la personalidad (Tendencias)
        if(brain != null) 
        {
            brain.DeleteBrainFile();
            brain.data = new AmebaData();
        }
        
        RandomizePersonality(); // Para que nazcan con nuevos genes
        // AssignRandomColor();

        EnterTrophozoiteState();
    }

    void UpdateStats()
    {
        float currentSize = transform.localScale.x;
        
        // 1. Energía Máxima
        maxEnergy = baseMaxEnergy + ((currentSize - 1f) * 50f);

        // 2. Velocidad (Las grandes son más lentas entre pasos)
        currentMoveInterval = baseMoveInterval * currentSize; 
        
        // 3. Masa física
        rb.mass = currentSize;
        
        // 4. RANGO DE VISIÓN (NUEVO)
        // El radio crece linealmente con el tamaño.
        // Si mide 1.0 -> Radio Base (15)
        // Si mide 2.0 -> Doble de Radio (30)
        sensorRadius = baseSensorRadius * currentSize; 
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
        switch (currentState)
        {
            case AmebaState.Trophozoite:
                HandleActiveState();
                break;
            case AmebaState.Cyst:
                HandleCystState();
                break;
            case AmebaState.Digesting:
                energy -= Time.deltaTime * 1f; 
                // Frenar suavemente mientras digiere
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime);
                break;
        }

        if (energy <= 0) gameObject.SetActive(false);
    }

    void HandleActiveState()
    {


        // 1. PRIMERO: ¿Veo algo importante? (Comida, Enemigos, Muros)
        Vector2 intention = CalculateDesires();

        // 2. SEGUNDO: Si no veo nada, uso la Exploración Dirigida
        if (intention == Vector2.zero)
        {
            // No hay estímulos, así que sigo mi ruta de patrulla
            intention = GetWanderDirection();
            
            // Nota: No pongo 'isWandering = false' aquí, porque quiero mantener
            // el destino hasta llegar a él.
        }
        else
        {
            // ¡He visto algo! (Comida o Peligro)
            // Olvido mi ruta de patrulla anterior. Cuando termine con esto, buscaré una nueva.
            isWandering = false;
            
            // Dibujamos la intención prioritaria en Magenta
            Debug.DrawRay(transform.position, intention * 3f, Color.magenta);
        }

        // 3. MOVIMIENTO (El resto sigue igual)
        moveTimer += Time.deltaTime;
        
        // Usamos currentMoveInterval (que depende del tamaño)
        if (moveTimer >= currentMoveInterval)
        {
            // Ahora 'intention' nunca será cero (o es deseo o es patrulla)
            if (intention != Vector2.zero)
            {
                float push = baseMoveForce * rb.mass;
                rb.AddForce(intention * push, ForceMode2D.Impulse);
                StartCoroutine(JellyEffect(intention));
            }
            moveTimer = 0f;
        }

        rb.linearDamping = 5f; 
        LimitSpeed();
        energy -= Time.deltaTime * 3f;

        // Chequeo de supervivencia
        // if (brain.ShouldEncyst(energy, maxEnergy, foodNearby))
        // {
        //     EnterCystState();
        // }
    }

    // --- NUEVO MÉTODO PARA CONTROLAR EXCESOS ---
    void LimitSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    void HandleCystState()
    {
        energy -= Time.deltaTime * 0.1f;
        CalculateDesires(); 
        if (brain.ShouldWakeUp(foodNearby)) EnterTrophozoiteState();
    }

    void EnterCystState()
    {
        currentState = AmebaState.Cyst;
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = Color.gray;
        brain.data.isCyst = true;
    }

    void EnterTrophozoiteState()
    {
        currentState = AmebaState.Trophozoite;
        spriteRenderer.color = defaultColor; 
        brain.data.isCyst = false;
    }

    Vector2 CalculateDesires()
    {
        Vector2 totalForce = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sensorRadius);
        foodNearby = false; 

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || !hit.gameObject.activeSelf) continue; 

            // CALCULAR DIRECCIÓN INTELIGENTE
            Vector2 dir = Vector2.zero;
            float dist = 0f;

            // TRUCO MATEMÁTICO:
            // Si es una Zona de Peligro (Trigger grande), calculamos la distancia 
            // al punto más cercano de su borde, no a su centro.
            if (hit.CompareTag("Muro"))
            {
                Vector3 closestPoint = hit.ClosestPoint(transform.position);
                dir = (closestPoint - transform.position).normalized;
                dist = Vector2.Distance(transform.position, closestPoint);
            }
            else
            {
                // Para comida y amebas puntuales, usamos el centro
                dir = (hit.transform.position - transform.position).normalized;
                dist = Vector2.Distance(transform.position, hit.transform.position);
            }
            
            float prox = 1f / (dist + 0.1f);

            // Valor por defecto (para muros o desconocidos)
            float perceivedValue = 1f; 

            // 1. EVALUAR EL OBJETO
            if (hit.CompareTag("Comida")) 
            {
                foodNearby = true;
                Nutrient2 n = hit.GetComponent<Nutrient2>();
                if (n != null) perceivedValue = n.energyValue; // Ej: 10
            } else if (hit.CompareTag("Muro"))
            {
                // Si ya sé que es malo, huyo del borde
                float opinion = brain.GetMemoryOpinion(hit.tag); // Será -1
                if (opinion < 0)
                {
                    totalForce += (dir * -1) * Mathf.Abs(opinion) * fearWeight * prox;
                }
                // Si es desconocido (Curiosidad), iré hacia el borde a investigar... y me quemaré.
                else if (brain.IsUnknown(hit.tag))
                {
                    totalForce += dir * curiosityWeight * prox;
                }
            }
            

            // 2. APLICAR PESOS
            if (brain.IsUnknown(hit.tag))
            {
                totalForce += dir * curiosityWeight * prox;
            }
            else
            {
                float opinion = brain.GetMemoryOpinion(hit.tag); // Devuelve 1 o -1

                if (opinion > 0) 
                {
                    // (Dirección * (Opinión * VALOR REAL) * Avaricia)
                    totalForce += dir * (opinion * perceivedValue) * greedWeight * prox;
                }
                else if (opinion < 0) 
                {
                    // Repulsión (Muro)
                    totalForce += (dir * -1) * Mathf.Abs(opinion) * fearWeight * prox;
                }
            }
        }
        return totalForce.normalized; 
    }

    Vector2 GetWanderDirection()
    {
        // 1. ¿Necesito un nuevo destino?
        // Si no estoy vagando O estoy muy cerca de mi destino actual (menos de 1 unidad)
        if (!isWandering || Vector2.Distance(transform.position, wanderTarget) < 1f)
        {
            // Calculamos un punto aleatorio DENTRO del círculo de visión
            // Usamos .normalized * sensorRadius para que sea siempre EN EL BORDE del rango
            Vector2 randomPoint = Random.insideUnitCircle.normalized * sensorRadius;
            
            // El destino es mi posición actual + ese vector
            wanderTarget = (Vector2)transform.position + randomPoint;
            
            isWandering = true;
        }

        // 2. Visualización (Línea Cian hacia donde quiere ir)
        Debug.DrawLine(transform.position, wanderTarget, Color.cyan);

        // 3. Devolver la dirección hacia ese punto
        return (wanderTarget - (Vector2)transform.position).normalized;
    }

    IEnumerator JellyEffect(Vector2 direction)
    {
        // 1. Orientación
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 2. Guardamos el tamaño REAL actual antes de deformar
        Vector3 realScale = transform.localScale;
        
        // 3. Deformamos (Squash & Stretch)
        // Estiramos en X, aplastamos en Y
        transform.localScale = new Vector3(realScale.x * 1.3f, realScale.y * 0.7f, 1);
        
        // 4. Esperamos
        yield return new WaitForSeconds(0.2f);
        
        // 5. IMPORTANTE: Restauramos el tamaño que guardamos al principio
        // ¡NO llamamos a UpdateStats() aquí! Moverse no debe cambiar tus stats.
        transform.localScale = realScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{name} ha tocado {other.name} (Tag: {other.tag})");
        
        if (currentState != AmebaState.Trophozoite) return;

        if (other.CompareTag("Comida"))
        {
            Nutrient2 n = other.GetComponent<Nutrient2>();
            if (n != null && !n.isBeingDigested)
            {
                StartCoroutine(Phagocytosis(n));
            }
        }
        else if (other.CompareTag("Muro"))
        {
            // Aprendo inmediatamente que esto es malo
            brain.Learn("Muro", -5f);
        }
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Muro"))
        {
            // Daño por segundo (ej. 10 de daño por segundo)
            float damage = Time.deltaTime * 10f;
            energy -= damage;
            
            // Refuerzo negativo constante al cerebro
            // Esto asegura que si se queda dentro, el miedo aumenta drásticamente
            brain.Learn("Muro", -damage * 2f);
        }
    }

    IEnumerator Phagocytosis(Nutrient2 prey)
    {
        currentState = AmebaState.Digesting;
        prey.isBeingDigested = true;
        prey.GetComponent<Collider2D>().enabled = false;
        prey.transform.SetParent(this.transform);
        prey.transform.localPosition = Vector3.zero;

        float timer = 0;
        Vector3 startScale = prey.transform.localScale;

        while (timer < digestionTime)
        {
            timer += Time.deltaTime;
            prey.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / digestionTime);
            yield return null;
        }

        if (transform.localScale.x < 3f) 
        {
            transform.localScale += Vector3.one * 0.05f;
            UpdateStats();
        }

        energy += prey.energyValue;
        if (energy > maxEnergy) energy = maxEnergy;

        brain.Learn("Comida", prey.energyValue);
        
        prey.transform.SetParent(null);
        prey.gameObject.SetActive(false);
        currentState = AmebaState.Trophozoite;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sensorRadius);
    }
}