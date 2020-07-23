using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float HP;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Attacked(float num)
    {
        HP = Mathf.Clamp(HP - num, 0, HP);

        if (HP == 0)
            Destroy(this.gameObject);
    }
}
