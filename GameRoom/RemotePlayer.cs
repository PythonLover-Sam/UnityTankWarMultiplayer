using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemotePlayer : Player
{
    [HideInInspector]
    public int GUID=0;
    [HideInInspector]
    public string nickName="";
    private Transform[] head;
    public static GameObject bullet;
    // Start is called before the first frame update
    void Start()
    {
        head = this.GetComponentsInChildren<Transform>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(GUID != 0 || nickName != "")
        {
            foreach (Transform node in head)
            {
                if (node.gameObject.name == "NickName")
                {
                    node.gameObject.GetComponent<Text>().text = nickName;
                }
            }
        }
        hpBar.fillAmount = hp / MAX_HEALTH;

        if(hp <= 0)
        {
            if (!explosionEffectExist)
            {
                GameObject.Instantiate(ExplosionEffect, transform.position, transform.rotation);
                explosionEffectExist = true;
            }
        }
        else if(hp>=0)
        {
            explosionEffectExist = false;
        }
    }
}
