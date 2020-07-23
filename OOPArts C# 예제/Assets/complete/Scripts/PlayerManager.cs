using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public KeyCode downkey;
    public KeyCode upkey;
    public float speed;
    public float height;
    
    private float xPositon;

    // Start is called before the first frame update
    void Start()
    {
        xPositon = transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        float direction = 0;
        direction += Input.GetKey(downkey) ? 1 : 0; // this is if (Input.GetKey(downkey)) then 1 else 0
        direction -= Input.GetKey(upkey) ? 1 : 0;

        float moveY = transform.position.y + direction * speed * Time.deltaTime;
        transform.position = new Vector3(xPositon, Mathf.Clamp(moveY, -height, height));
    }
}
