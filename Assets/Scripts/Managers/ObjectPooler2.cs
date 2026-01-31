using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler2 : MonoBehaviour
{
    public static ObjectPooler2 Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
        // 1. Inicializamos la "caja" vacía AQUÍ. Así nunca dará error de NullReference.
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
    }

    void Start()
    {
        // 2. La operación pesada de crear objetos la dejamos en Start
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool con tag " + tag + " no existe.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Si tiene interfaz de reseteo (como la ameba), la llamamos
        IResetable resetable = objectToSpawn.GetComponent<IResetable>();
        if (resetable != null)
        {
            resetable.ResetState();
        }

        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}

// Pequeña interfaz para obligar a los objetos a reiniciarse al nacer
public interface IResetable
{
    void ResetState();
}
