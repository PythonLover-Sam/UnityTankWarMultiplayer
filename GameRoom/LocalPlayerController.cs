using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Multiplay;
using UnityEngine.UI;

public class LocalPlayerController : Player
{
    static PlayerTankRequest ptr = new PlayerTankRequest();
    public Text RebornTimeText;
    public Text nickNameText;
    public Camera camera;
    public float shootTime;
    private float shootCoolDown;
    public GameObject[] SpawnPoint;
    public float RebornTime;
    private float rebornTimer=0;
    [SerializeField]
    public static List<RemotePlayer> remotePlayerList = new List<RemotePlayer>();
    private void Awake()
    {
        Transform parentObject = GameObject.Find("RemotePlayers").transform;
        remotePlayerList.Add(parentObject.Find("RemotePlayer1").GetComponent<RemotePlayer>());
        remotePlayerList.Add(parentObject.Find("RemotePlayer2").GetComponent<RemotePlayer>());
        remotePlayerList.Add(parentObject.Find("RemotePlayer3").GetComponent<RemotePlayer>());
        remotePlayerList.Add(parentObject.Find("RemotePlayer4").GetComponent<RemotePlayer>());
        remotePlayerList.Add(parentObject.Find("RemotePlayer5").GetComponent<RemotePlayer>());
    }
        

    void Start()
    {
        RemotePlayer.bullet = Bullet;
        
        try
        {
            nickNameText.text = NetworkPlayer.Instance.NickName;
        }
        catch { }
        SpawnRandomly();
        StartCoroutine(SendStatusToServer());
    }
    // 更新远程玩家
    public static void UpdateRemotePlayerStatus(PlayerTankRequest ptr)
     {
          foreach (RemotePlayer rp in remotePlayerList)
        {
             if (rp.GUID == ptr.GUID)
            {
                rp.MAX_HEALTH = ptr.MaxHealth;
                rp.hp = ptr.Hp;
                rp.transform.position = new Vector3(ptr.Vec3_x, ptr.Vec3_y, ptr.Vec3_z);
                //Vector3 des = new Vector3(ptr.Vec3_x-rp.transform.position.x,
                //                          ptr.Vec3_y-rp.transform.position.y,
                //                          ptr.Vec3_z-rp.transform.position.z);
                //rp.transform.Translate(des/3f, Space.World);
                rp.transform.rotation = new Quaternion(ptr.Rot4_a, ptr.Rot4_b, ptr.Rot4_c, ptr.Rot4_d);
                if(ptr.AttackNormal == true)
                {
                    GameObject.Instantiate(RemotePlayer.bullet, rp.FirePosition.transform.position, rp.FirePosition.transform.rotation);
                }
                break;
            }
             else if (rp.GUID == 0)
            {
                rp.GUID = ptr.GUID;
                rp.nickName = ptr.NickName;
                 rp.gameObject.SetActive(true);
                 break; 
            }
            
            //if (!rp.gameObject.activeInHierarchy) ;
        }
    }

    void Update()
    {
        if (hp >= MAX_HEALTH) hp = MAX_HEALTH;
        if(shootCoolDown <= shootTime)
        {
            shootCoolDown += Time.deltaTime;
        }
        hpBar.fillAmount = hp / MAX_HEALTH;
        //设置相机跟随
        Vector3 camera_pos = new Vector3(this.transform.position.x, camera.transform.position.y, this.transform.position.z-5f);
        camera.transform.position = camera_pos;

        // 鼠标点位检测
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        Physics.Raycast(ray, out hitInfo);
        Debug.DrawLine(ray.origin, hitInfo.point);
        if (Vector3.Distance(this.transform.position, hitInfo.point) > 1.5f)
        {
            transform.LookAt(new Vector3(hitInfo.point.x, 0.5f, hitInfo.point.z));
        }
        // 用户移动
        if (Input.GetKey(KeyCode.Space) && hp > 0)
        {
            Debug.Log(hitInfo.point);
            Vector3 des = new Vector3(hitInfo.point.x - transform.position.x, 0, hitInfo.point.z - transform.position.z);
            Player.MoveToDestination(this, des);
        }
        if(Input.GetMouseButtonDown(0))
        {
            if(shootCoolDown >= shootTime && hp > 0)
            {
                GameObject.Instantiate(Bullet,FirePosition.transform.position, FirePosition.transform.rotation);
                shootCoolDown = 0;
                ptr.AttackNormal = true;
            }
        }
        if (Input.GetMouseButtonDown(1))
            hp -= 10;
        if(hp <= 0)
        {
            if (!explosionEffectExist)
            {
                GameObject.Instantiate(ExplosionEffect, transform.position, transform.rotation);
                explosionEffectExist = true;
            }
            RebornTimeText.gameObject.SetActive(true);
            this.transform.position = new Vector3(transform.position.x, 10f, transform.position.z);
            Reborn();
        }
    }
    private IEnumerator SendStatusToServer()
    {
        while(true)
        {
            if(Network.isConnected)
            {              
                ptr.RoomId = NetworkPlayer.Instance.RoomId;
                ptr.NickName = NetworkPlayer.Instance.NickName;
                ptr.GUID = NetworkPlayer.Instance.GUID;
                ptr.MaxHealth = this.MAX_HEALTH;
                ptr.Hp = this.hp;
                ptr.Vec3_x = gameObject.transform.position.x;
                ptr.Vec3_y = gameObject.transform.position.y;
                ptr.Vec3_z = gameObject.transform.position.z;
                ptr.Rot4_a = gameObject.transform.rotation.x;
                ptr.Rot4_b = gameObject.transform.rotation.y;
                ptr.Rot4_c = gameObject.transform.rotation.z;
                ptr.Rot4_d = gameObject.transform.rotation.w;
                //ptr.AttackNormal = false;
                ptr.SlainEnemy = false;

                Network.UploadTankStatus(ptr);
                if (ptr.AttackNormal == true) ptr.AttackNormal = false;
            }
            yield return new WaitForSeconds(0.06f);
        }
    }

    private void Reborn()
    {
        if (rebornTimer < RebornTime)
        {
            rebornTimer += Time.deltaTime;
            RebornTimeText.text = "重生时间：" + ((int)(RebornTime - rebornTimer)).ToString();
        }
        else
        {
            // 重生
            SpawnRandomly();
            RebornTimeText.gameObject.SetActive(false);
            rebornTimer = 0;
            explosionEffectExist = false;
        }
    }
    private void SpawnRandomly()
    {
        int c = Random.Range(0, 7);
        this.transform.position = SpawnPoint[c].transform.position;
        shootCoolDown = shootTime;
        this.MAX_HEALTH = 100f;
        this.hp = MAX_HEALTH;
    }
}
