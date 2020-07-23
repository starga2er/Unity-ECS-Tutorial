using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    public float speed;
    public float accSpeed;
    public float xBound = 7.5f;

    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = Random.insideUnitCircle * speed;
    }

    // Update is called once per frame
    void Update()
    {
        speed += accSpeed * Time.deltaTime;

        rb.velocity = rb.velocity.normalized * speed;

        if (transform.position.x >= xBound)
        {
            GameManger.singleton.PlayerScored(0);
            Destroy(gameObject);
        }
        else if (transform.position.x <= -xBound)
        {
            GameManger.singleton.PlayerScored(0);
            Destroy(gameObject);
        }
    }
}
