using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEvent : MonoBehaviour
{
    public InputField nicknameInput;
    public InputField roomId;
    public Image nickNameNotValid;
    public Image roomExist;
    public Image RoomNotExist;
    public Image networdFailed;
    public GameObject networkManager; 
    public Text nickname;
    public Text inRoomState;
    public Button btn_startGame;
    public Button btn_ExitRoom;
    public void ExitButtonPressed()
    {
        Network.Disconnected();
        Application.Quit();
    }

    public void NickNameButtonPressed()
    {
        if(nicknameInput.text.Length == 0)
        {
            nickNameNotValid.gameObject.SetActive(true);
            return;
        }
        if(Network.isConnected == false)
        {
            networdFailed.gameObject.SetActive(true);
            return;
        }
        Network.ChangeNickNameRequest(nicknameInput.text);
    }

    public void CreateRoomButtonClicked()
    {
        if(Network.isConnected == false)
        {
            networdFailed.gameObject.SetActive(true);
            return;
        }
        if (roomId.text.Length == 0)
        {
            nickNameNotValid.gameObject.SetActive(true);
            return;
        }
        Network.CreateRoomRequest(roomId.text);
    }

    public void EnterRoomButtonClicked()
    {
        if (Network.isConnected == false)
        {
            networdFailed.gameObject.SetActive(true);
            return;
        }
        if (roomId.text.Length == 0)
        {
            nickNameNotValid.gameObject.SetActive(true);
            return;
        }
        Network.EnterRoomRequest(roomId.text);
    }

    public void StartGameButtonClicked()
    {
        Network.StartGameRequest(NetworkPlayer.Instance.RoomId);
    }

    public void ExitRoomButtonClicked()
    {
        Network.ExitRoomRequest(NetworkPlayer.Instance.RoomId);
    }
    public void Update()
    {
        nickname.text = NetworkPlayer.Instance.NickName;
        if(NetworkPlayer.Instance.RoomId == "-1")
        {
            roomExist.gameObject.SetActive(true);
            NetworkPlayer.Instance.RoomId = "0";
            inRoomState.text = "还未进入房间";
            btn_startGame.gameObject.SetActive(false);
            btn_ExitRoom.gameObject.SetActive(false);
        }
        else if (NetworkPlayer.Instance.RoomId == "-2")
        {
            RoomNotExist.gameObject.SetActive(true);
            NetworkPlayer.Instance.RoomId = "0";
            inRoomState.text = "还未进入房间";
            btn_startGame.gameObject.SetActive(false);
            btn_ExitRoom.gameObject.SetActive(false);
        }
        else if (NetworkPlayer.Instance.RoomId != "0" && NetworkPlayer.Instance.RoomId != "")
        {
            inRoomState.text = "成功加入房间号：" + NetworkPlayer.Instance.RoomId;
            btn_startGame.gameObject.SetActive(true);
            btn_ExitRoom.gameObject.SetActive(true);
        }
        else if(NetworkPlayer.Instance.RoomId == "0" || NetworkPlayer.Instance.RoomId == "")
        {
            inRoomState.text = "还未进入房间";
            btn_startGame.gameObject.SetActive(false);
            btn_ExitRoom.gameObject.SetActive(false);

        }
    }

}
