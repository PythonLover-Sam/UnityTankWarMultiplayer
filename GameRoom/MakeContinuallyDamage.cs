using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeContinuallyDamage : MonoBehaviour
{
    public float damageRate;
    public int damageType = 0;
    public GameObject Effect;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "LocalPlayer" && damageType == 0)
        {
            other.gameObject.GetComponent<LocalPlayerController>().hp -= damageRate * Time.deltaTime;

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && damageType == 1)
        {
            other.gameObject.GetComponent<LocalPlayerController>().hp -= damageRate;
            GameObject.Instantiate(Effect, other.transform.position, other.transform.rotation);
        }
    }
}
