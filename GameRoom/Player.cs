using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//public delegate void MoveController(GameObject obj, Vector3 des);
public class Player : MonoBehaviour
{

    public Image hpBar;
    public float moveSpeed;
    [HideInInspector]
    public float MAX_HEALTH;
    [HideInInspector]
    public float hp;
    public GameObject FirePosition;
    public GameObject Bullet;
    public GameObject ExplosionEffect;
    protected bool explosionEffectExist = false;

    public static void MoveToDestination(Player obj, Vector3 des)
    {
        obj.transform.Translate(des.normalized * obj.moveSpeed * Time.deltaTime, Space.World);
    }
}
