using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public int maxMonsterCount = 1;
    public GameObject bossExitPanel;
    public int curMonsterCount=3;
    public int curPlayerCount=2;
    GameObject[] monsters = new GameObject[3];
    bool isEnd = false;
    public GameObject MatchingPanel;
    public Text MatchingText;
    public GameObject BossPanel;
    public GameObject LobbyPanel;
    public Text NickNameTextInLobby;
    public GameObject playerDiePanel;
    public GameObject bossMapExitPanel;
    bool isMatch = false;
    public Vector3 bossMapVectorLocation;
    public GameObject Boss;
    public GameObject DisconnectPanel;
    public GameObject InGamePanel;
    public GameObject player;
    public GameObject canvas;
    PhotonView PV;
    bool started = true;
    //public Text NickNameText;
    private void Start()
    {
        //curMonsterCount = maxMonsterCount;
    }
    [Header("Disconnect")]
    public PlayerLeaderboardEntry MyPlayFabInfo;
    public List<PlayerLeaderboardEntry> PlayFabUserList = new List<PlayerLeaderboardEntry>();
    public InputField EmailInput, PasswordInput, UsernameInput;

    public void PlayerRespawn()
    {
        playerDiePanel.SetActive(false);
        player.SetActive(true);
        transform.position = new Vector3(Random.Range(-20, 20), 20, Random.Range(-20, 20));
        player.GetComponent<PlayerController>().setCurHP(player.GetComponent<PlayerController>().maxHp);
    }

    [PunRPC]
    void showExitPanel()
    {
        bossExitPanel.SetActive(true);
        BossPanel.SetActive(false);
    }

    #region 플레이팹
    void Awake()
    {
        Screen.SetResolution(1920, 1080, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        PV = GetComponent<PhotonView>();
        EmailInput.text = "qazwsx@aa.com"; PasswordInput.text = "qazwsx@"; UsernameInput.text = "PSJ";
    }

    public void Login()
    {
        PhotonNetwork.LocalPlayer.NickName = UsernameInput.text;
        PhotonNetwork.ConnectUsingSettings();
        var request = new LoginWithEmailAddressRequest { Email = EmailInput.text, Password = PasswordInput.text };
        PhotonNetwork.LocalPlayer.NickName = UsernameInput.text;
        PlayFabClientAPI.LoginWithEmailAddress(request, (result) => 
        {  PhotonNetwork.ConnectUsingSettings();  }, (error) => print("로그인 실패"));
    }

    public void Spawn()
    {
        player = PhotonNetwork.Instantiate("CharacterRoot", new Vector3(5,5,5), Quaternion.identity,0);
        
        //NickNameText.text = PhotonNetwork.LocalPlayer.NickName;
    }
    public void Register()
    {
        var request = new RegisterPlayFabUserRequest { Email = EmailInput.text, Password = PasswordInput.text, Username = UsernameInput.text, DisplayName = UsernameInput.text };
        PlayFabClientAPI.RegisterPlayFabUser(request, (result) =>
        { print("회원가입 성공");  }, (error) => print("회원가입 실패"));
    }
    #region 아이템 통신
    // 돈 주웠을때 등
    public void AddMoney(int amount)
    {
        var request = new AddUserVirtualCurrencyRequest() { VirtualCurrency = "GD", Amount = amount };
        PlayFabClientAPI.AddUserVirtualCurrency(request, (result) => print("돈 얻기 성공! 현재 돈 : " + result.Balance), (error) => print("돈 얻기 실패"));
    }

    public void SubtractMoney(int amount)
    {
        var request = new SubtractUserVirtualCurrencyRequest() { VirtualCurrency = "GD", Amount = amount };
        PlayFabClientAPI.SubtractUserVirtualCurrency(request, (result) => print("돈 빼기 성공! 현재 돈 : " + result.Balance), (error) => print("돈 빼기 실패"));
    }
    string[] InventoryID = new string[10];
    string[] InventoryItemName = new string[10];
    // 인벤토리 열었을 때 이거는 처음 시작할 때나 저장할때 연동이 필요할듯
    public void GetInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), (result) =>
        {
            print("현재금액 : " + result.VirtualCurrency["GD"]);
            
            for (int i = 0; i < result.Inventory.Count; i++)
            {
                
                var Inven = result.Inventory[i];
                // 인벤토리 정보 set
                InventoryID[i] = Inven.ItemInstanceId;
                InventoryItemName[i] = Inven.DisplayName;

                //Debug.Log(Inven.DisplayName + " / " + Inven.UnitCurrency + " / " + Inven.UnitPrice + " / " + Inven.ItemInstanceId + " / " + Inven.RemainingUses);
            }
        },
        (error) => print("인벤토리 불러오기 실패"));
    }
    // 상점 아이템 메뉴 불러오기
    public void GetCatalogItem()
    {
        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest() { CatalogVersion = "Main" }, (result) =>
        {
            for (int i = 0; i < result.Catalog.Count; i++)
            {
                var Catalog = result.Catalog[i];
                print(Catalog.ItemId + " / " + Catalog.DisplayName + " / " + Catalog.Description + " / " +
                    Catalog.VirtualCurrencyPrices["GD"] + " / " + Catalog.Consumable.UsageCount);
            }
        },
        (error) => print("상점 불러오기 실패"));
    }

    // 아이템 구매시, 얻을 때는 가격을 0으로 하면 될듯
    public void AddHPItem()
    {
        
        var request = new PurchaseItemRequest() { CatalogVersion = "Main", ItemId = "HP_portion", VirtualCurrency = "GD", Price = 0 };
        PlayFabClientAPI.PurchaseItem(request, (result) => Debug.Log("HP아이템 구입 성공"), (error) => Debug.Log("아이템 구입 실패"+ error));
        GetInventory();
    }
    public void AddMPItem()
    {
        
        var request = new PurchaseItemRequest() { CatalogVersion = "Main", ItemId = "MP_portion", VirtualCurrency = "GD", Price = 0 };
        PlayFabClientAPI.PurchaseItem(request, (result) => Debug.Log("MP아이템 구입 성공"), (error) => Debug.Log("아이템 구입 실패"+ error));
        GetInventory();
    }
    // 나중에 합칠 수 있을듯
    public void ConsumeHPItem()
    {// 아이템 인스턴스 아이디도 받아올 수 있음.
        GetInventory();
        string itemID=null;
        for (int i=0;i< InventoryID.Length;i++)
        {
            if (InventoryItemName[i] == "HP_portion")
            {
                itemID = InventoryID[i];break;
            }
        }
        Debug.Log(itemID);
        var request = new ConsumeItemRequest { ConsumeCount = 1, ItemInstanceId = itemID };
        PlayFabClientAPI.ConsumeItem(request, (result) => Debug.Log("HP아이템 사용 성공"), (error) => Debug.Log("H아이템 사용 실패"+error));
    }
    public void ConsumeMPItem()
    {// 아이템 인스턴스 아이디도 받아올 수 있음.
        GetInventory();
        string itemID=null;
        for (int i = 0; i < InventoryID.Length; i++)
        {
            if (InventoryItemName[i] == "MP_portion")
            {
                itemID = InventoryID[i]; break;
            }
        }
        Debug.Log(itemID);
        var request = new ConsumeItemRequest { ConsumeCount = 1 ,ItemInstanceId= itemID };
        PlayFabClientAPI.ConsumeItem(request, (result) => print("MP아이템 사용 성공"), (error) => print("M아이템 사용 실패"+ error));
    }
    #endregion


    #endregion


    #region 로비
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        
    }

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        NickNameTextInLobby.text = PhotonNetwork.LocalPlayer.NickName+"님 환영합니다!";
    }


    void ShowPanel(GameObject CurPanel)
    {
        DisconnectPanel.SetActive(false);

        CurPanel.SetActive(true);
    }

    public void XBtn()
    {
        if (PhotonNetwork.InLobby) PhotonNetwork.Disconnect();
        else if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        //isLoaded = false;
        //ShowPanel(DisconnectPanel);
    }
    #endregion
    
    [PunRPC]
    void startBossPanel()
    {
        BossPanel.SetActive(true);
    }
    private void Update()
    {
        if(started==true)
        {//
            if (PhotonNetwork.IsMasterClient && curMonsterCount <=0)
            {
                Debug.Log("마스터클라이언트 보스실행전");
                // 보스 실행
                PhotonNetwork.Instantiate
                    ("Boss", new Vector3(Random.Range(-20, 20), 1, Random.Range(-20, 20)+20), Quaternion.identity);
                Debug.Log("마스터클라이언트 보스실행후");
                started = false;
                PV.RPC("startBossPanel", RpcTarget.All);
            }
        }
        if(PhotonNetwork.IsMasterClient&&!isEnd)
        {
            if(curPlayerCount==0)
            {
                PV.RPC("showExitPanel", RpcTarget.All);
            }
        }
    }
    [PunRPC]
    void mPlayerC()
    {
        curPlayerCount--;
    }
    public void minusPlayerCount()
    {
        PV.RPC("mPlayerC", RpcTarget.All);
    }
    #region 방
    public void JoinOrCreateRoom()
    {
        PhotonNetwork.JoinOrCreateRoom("BossMap", new RoomOptions() { MaxPlayers = 5 }, null);
        DisconnectPanel.SetActive(false);
        //InGamePanel.SetActive(true);

        // 대기 중인 패널 활성화
        MatchingPanel.SetActive(true);
       

        //MinimapPanel.SetActive(true);
        Debug.Log("오픈월드입장");

    }


    public override void OnCreateRoomFailed(short returnCode, string message) => print("방만들기실패");

    public override void OnJoinRoomFailed(short returnCode, string message) => print("방참가실패");


    [PunRPC]
    void UpdateText()
    {
        MatchingText.text ="매칭중 : 현재 "+ PhotonNetwork.CurrentRoom.PlayerCount + "대기 중";
        Debug.Log("업데이트 텍스트");
    }

    // 방에 참가하면 호출되는 콜백함수이다. 
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.Name== "OpenWorld")
        {
            Spawn();
            BossPanel.SetActive(false);
            //canvas.GetComponent<Canvas>().OnInventoryUI();
            //canvas.GetComponent<Canvas>().invenUI.inven = player.GetComponent<Inventory>();
            //canvas.GetComponent<Canvas>().invenUI.init();
            //string curName = PhotonNetwork.CurrentRoom.Name;
            //RoomNameInfoText.text = curName;

            //유저방이면 데이터 가져오기
            {


                //string curID = PhotonNetwork.CurrentRoom.CustomProperties["PlayFabID"].ToString();
                //GetData(curID);

                // 현재 방 PlatyFabID 커스텀 프로퍼티가 나의 PlayFabID와 같다면 값을 저장할 수 있음
                //if (curID == MyPlayFabInfo.PlayFabId)
                {
                    //RoomNameInfoText.text += " (나의 방)";

                    //SetDataInput.gameObject.SetActive(true);
                    //SetDataBtnObj.SetActive(true);
                }
            }

        }
        else if(PhotonNetwork.CurrentRoom.Name=="BossMap")
        {
            Debug.Log("보스맵 입장완료");
            PV.RPC("showID", RpcTarget.All);
            PV.RPC("UpdateText", RpcTarget.All);
            Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount);
            if (PhotonNetwork.CurrentRoom.PlayerCount==2)
            {
                started = true;
                PV.RPC("Matched", RpcTarget.All);
                Debug.Log("매칭완료");
            }

        }
        
    }
    [PunRPC]
    void showID()
    {
        Debug.Log(PV.ViewID);
    }
    public void BossDie()
    {
        PV.RPC("BossDieRoutine", RpcTarget.All);
    }
    [PunRPC]
    void BossDieRoutine()
    {
        BossPanel.SetActive(false);
        isMatch = false;
        PhotonNetwork.LeaveRoom();
        //JoinOrCreateRoom();
    }

    #endregion


    #region 매칭

    public void Matching()// 보스매칭 버튼 눌렀을 때
    {
        //BossJoinOrCreateRoom();
        isMatch = true;
        Debug.Log("매칭 시작 버튼 눌림!");
        JoinOrCreateRoom();
    }

    [PunRPC]
    void Matched()
    {
        MatchingPanel.SetActive(false);
        DisconnectPanel.SetActive(false);
        LobbyPanel.SetActive(false);
        BossMapSpawn();

    }

    void BossJoinOrCreateRoom()
    {
        //아이템 저장 후 방 떠나기
        Destroy(player);
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.JoinLobby();
    }
    [PunRPC]
    void BossMapSpawn()
    {
        // 
        PhotonNetwork.Instantiate("CharacterRoot", new Vector3(Random.Range(1,7),3,34), Quaternion.identity);
        // 각자 정보 받아오기 아이템 같은 것.
        if(PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터클라이언트에서 BossMapSpawn");
            for (int i = 0; i < monsters.Length; i++)
            {
                monsters[i] = PhotonNetwork.Instantiate
                    ("Enemy", new Vector3(Random.Range(-20, 20), 1, Random.Range(-20, 20)+20), Quaternion.identity);
            }
        }
    }

    // 보스처치 후 나가기 버튼 누르면 실행
    public void OnExitBossMap()
    {
        //아이템 저장 후 방 떠나기
        Destroy(player);
        isMatch = false;
        bossMapExitPanel.SetActive(false);
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.JoinLobby();
    }
    #endregion
}
