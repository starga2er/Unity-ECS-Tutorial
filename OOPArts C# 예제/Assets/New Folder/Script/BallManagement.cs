using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManagement : MonoBehaviour
{
    public Rigidbody rb;
    public float speed;
    public float acc;

    public float Xpos;

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        rb.velocity = Random.insideUnitCircle * speed;
    }

    // Update is called once per frame
    void Update()
    {
        speed += acc * Time.deltaTime;

        rb.velocity = rb.velocity.normalized * speed;

        if (transform.position.x < -Xpos)
        {
            GameManger.singleton.PlayerScored(1);
            Destroy(gameObject);
        }
        else if (transform.position.x > Xpos)
        {
            GameManger.singleton.PlayerScored(0);
            Destroy(gameObject);
        }
    }
}
