using UnityEngine;
using System.Collections.Generic;

public class food_maintain : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float spawnRange = 25f;

    public void spawnFood(Transform platform, GameObject food) {
        // Instantiate food clones
        float safeRange = spawnRange * 0.8f;
        for (int i = 0; i < 10; i++)
        {
            Vector3 spawnOffset = new Vector3(Random.Range(-safeRange, safeRange),0.5f,Random.Range(-safeRange, safeRange));
            Vector3 spawnPosition = platform.transform.position + spawnOffset;
            GameObject newFood = Instantiate(food ,spawnPosition, transform.rotation, parent: transform);
        }
    }

    public void clearFood() {
        List<Transform> foods = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).CompareTag("food"))
                foods.Add(transform.GetChild(i));
        }

        foreach(Transform foodObj in foods) {
            Destroy(foodObj.GetComponent<GameObject>());
        }
    }

    public int getCount() {
        int sum = 0;
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).CompareTag("food"))
                sum++;
        }

        return sum;
    }
}
