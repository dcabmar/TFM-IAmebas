using UnityEngine;

public class AmebaMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private AmebaVisuals visuals;
    
    // Variables de patrulla
    private Vector2 wanderTarget;
    private bool isWandering = false;
    private float moveTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visuals = GetComponent<AmebaVisuals>();
    }

    public void ResetMovement()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        isWandering = false;
        moveTimer = 0f;
    }

    public void ApplyFriction()
    {
        rb.linearVelocity = rb.linearVelocity * 0.98f;
    }

    public void ApplyDigestingFriction()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
    }
    
    public void StopImmediate()
    {
         rb.linearVelocity *= 0.1f;
    }

    public void HandleMovement(Vector2 intention, float interval, float force)
    {
        if (intention != Vector2.zero)
        {
            isWandering = false;
            Debug.DrawRay(transform.position, intention * 3f, Color.magenta);
        }
        else
        {
            intention = GetWanderDirection(GetComponent<AmebaStats>().sensorRadius); // Acceso rápido al radio
        }

        moveTimer += Time.deltaTime;
        if (moveTimer >= interval)
        {
            rb.AddForce(intention * force * rb.mass, ForceMode2D.Impulse);
            if(visuals) StartCoroutine(visuals.JellyEffect(intention));
            
            // Consumo de energía por movimiento se notifica al controlador o stats
            // Lo haremos retornar true para que el Controller reste energía
            moveTimer = 0f;
        }
    }
    
    // Devuelve true si acaba de moverse (para restar energía en el controller)
    public bool HasMovedJustNow() { return moveTimer == 0f; }

    Vector2 GetWanderDirection(float radius)
    {
        if (!isWandering || Vector2.Distance(transform.position, wanderTarget) < 1f)
        {
            Vector2 randomPoint = Random.insideUnitCircle.normalized * radius;
            wanderTarget = (Vector2)transform.position + randomPoint;
            isWandering = true;
        }

        Debug.DrawLine(transform.position, wanderTarget, Color.cyan);
        Vector3 p = wanderTarget;
        Debug.DrawLine(new Vector3(p.x - 0.2f, p.y, 0), new Vector3(p.x + 0.2f, p.y, 0), Color.cyan);
        Debug.DrawLine(new Vector3(p.x, p.y - 0.2f, 0), new Vector3(p.x, p.y + 0.2f, 0), Color.cyan);

        return (wanderTarget - (Vector2)transform.position).normalized;
    }
}