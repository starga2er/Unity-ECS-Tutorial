using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public KeyCode leftmove;
    public KeyCode rightmove;
    public KeyCode shooting;
    public GameObject missile;
    public float shoottime;
    public float speed;

    private float now_shoottime;

    // private Rigidbody2D rd2;

    // Start is called before the first frame update
    void Start()
    {
        // rd2 = this.GetComponent<Rigidbody2D>();
        now_shoottime = shoottime;
    }

    // Update is called once per frame
    void Update()
    {
        now_shoottime += Time.deltaTime;
        float dir = 0;
        if (Input.GetKey(leftmove))
            dir -= 1;
        if (Input.GetKey(rightmove))
            dir += 1;

        float new_x = this.transform.position.x + dir * speed * Time.deltaTime;
        this.transform.position = new Vector3(new_x, this.transform.position.y, 0);

        if (Input.GetKey(shooting) && now_shoottime >= shoottime)
        {
            Instantiate(missile, this.transform.position, Quaternion.identity);
            now_shoottime = 0;
        }
    }
}
