using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public const float LIFE_TIME = 0.5f;
    private float time=0;
    public float speed;
    public float Damage;
    public GameObject Explosion;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            GameObject.Instantiate(Explosion, transform.position, Quaternion.identity);
            GameObject.Destroy(this.gameObject);
            try
            {
                collision.gameObject.GetComponent<LocalPlayerController>().hp -= Damage;
            }
            catch { }
        }
        else if(collision.gameObject.CompareTag("Ground"))
        {
            GameObject.Instantiate(Explosion, transform.position, Quaternion.identity);
            GameObject.Destroy(this.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up*speed*Time.deltaTime, Space.Self);
        time += Time.deltaTime;
        if(time >= LIFE_TIME)
        {
            GameObject.Instantiate(Explosion, transform.position, Quaternion.identity);
            GameObject.Destroy(this.gameObject);
        }
    }
}
