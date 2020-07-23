using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleManagement : MonoBehaviour
{
    public KeyCode upkey;
    public KeyCode downkey;
    public float speed;
    public float height;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float dir = 0;
        if (Input.GetKey(downkey))
            dir -= 1;
        if (Input.GetKey(upkey))
            dir += 1;

        float moveY = this.transform.position.y + dir * speed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, Mathf.Clamp( moveY, -height, height), 0);
    }
}
