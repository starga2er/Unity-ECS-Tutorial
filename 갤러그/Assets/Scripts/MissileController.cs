using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{

    public float acc;
    public float speed;
    public float up_max;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        speed += acc * Time.deltaTime;

        float new_y = this.transform.position.y + speed * Time.deltaTime;
        if (new_y > up_max)
            Destroy(this.gameObject);
        else
            this.transform.position = new Vector3(this.transform.position.x, new_y, 0);

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        
        if (collision.gameObject.tag == "Enemy")
        {
            collision.gameObject.SendMessage("Attacked", 10);
            Destroy(this.gameObject);
            // Destroy(collision.gameObject);
        }

    }
}

