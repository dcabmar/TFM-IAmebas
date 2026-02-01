using System.Collections.Generic;
using UnityEngine;

// Interfaz para obligar a reiniciar objetos reciclados
public interface IResetable
{
    void ResetState();
}

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
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
    }

    void Start()
    {
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

        // Resetear el objeto (limpiar cerebro, energía, etc.)
        IResetable resetable = objectToSpawn.GetComponent<IResetable>();
        if (resetable != null)
        {
            resetable.ResetState();
        }

        poolDictionary[tag].Enqueue(objectToSpawn);
        return objectToSpawn;
    }
}