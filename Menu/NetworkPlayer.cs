using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Multiplay;
using UnityEngine.SceneManagement;

public class NetworkPlayer : MonoBehaviour
{
    public static NetworkPlayer Instance { get; private set; }
    [HideInInspector]
    public string NickName="Undefined Name";
    [HideInInspector]
    public string RoomId="0";
    [HideInInspector]
    public bool Playing;
    [HideInInspector]
    public int GUID=0;

    public Action<string> OnRoomIdChange;
    public Action<string> OnNickNameChange;
    public Action<bool> OnPlayingChange;
    public Action<int> OnGUIDChange;
    private NetworkPlayer() { }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //nickname.text = NickName;
            DontDestroyOnLoad(this);

            OnRoomIdChange += (roomId) => RoomId = roomId;

            OnPlayingChange += (playing) => Playing = playing;

            OnNickNameChange += (nickname) => NickName = nickname;

            OnGUIDChange += (guid) => GUID = guid;
        }

    }
    private void Update()
    {
        if(Playing == true && SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0))
        {
            SceneManager.LoadScene(1);
        }
        else if(Playing == false && SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(1))
        {
            SceneManager.LoadScene(0);
            NetworkPlayer.Instance.RoomId = "0";
        }
    }


}
