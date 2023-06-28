using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviour
{
    public GameObject skillParticle;

    public GameObject Diecamera;
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip DamageSound;
    public AudioClip DieSound;
    public AudioClip getItemSound;
    public AudioClip UseItemSound;
    public AudioClip SkillSound;
    //ds
    public int maxMp;
    int curMp;
    //ds
    public Text inputChatText;
    //public InputField ChatText;
    Text[] ChatText=new Text[6];
    public int maxHp;
    int curHp;
    public Text nicknameText;

    bool visibleCursor = false;
    public bool UseSkill = false;
    Vector3 curPos;
    Quaternion curRot;

    public Camera cam;
    public GameObject player;
    Rigidbody playerRigid;
    Animator animator;
    public Image HealthImage;
    public Image MPImage;
    public bool isDead = false;
    public float speed = 2f;
    CapsuleCollider capsuleCollider;
    bool isDamaged = false;
    private Vector3 MoveDir;
    float horizontal, vertical;
    Vector3 moveVec;

    NetworkManager networkManager;

    Weapon weapon;
    public float swingDelay = 0.5f;
    float lastSwing;

    public void setCurHP(int h)
    {
        curHp = h;
    }
    
    void Start()
    {
        if(PV.IsMine)
        {
            Diecamera = GameObject.Find("DieCamera"); Diecamera.SetActive(false);
            NickNameText.text = PhotonNetwork.LocalPlayer.NickName;
            //채팅 관련 오브젝트 찾긴
            //inputChatText = GameObject.Find("ChatFieldText").GetComponent<Text>();
            //for (int i = 0; i < 6; i++)
            //{
            //    ChatText[i] = GameObject.Find("ChattingText (" + i + ")").GetComponent<Text>();
            //}
        }
        
        

        maxHp = 150;
        curHp = maxHp;
        maxMp = 100;
        curMp = maxMp;
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        NickNameText.text = PhotonNetwork.LocalPlayer.NickName;
        //채팅 관련 오브젝트 찾긴
        inputChatText = GameObject.Find("ChatFieldText").GetComponent<Text>();
        for (int i = 0; i < 6; i++)
        {
            ChatText[i] = GameObject.Find("ChattingText (" + i + ")").GetComponent<Text>();
        }

        playerRigid = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        // 나중에 바꿔야함
        weapon = GetComponentInChildren<Weapon>();
        HealthImage.fillAmount = curHp/100;
        MPImage.fillAmount = 1;

        capsuleCollider = GetComponent<CapsuleCollider>();
        
    }

    #region 사운드

    [PunRPC]
    void PlayDamageSound()
    {
        audioSource.clip = DamageSound;
        audioSource.Play();
    }
    [PunRPC]
    void PlayHitSound()
    {
        audioSource.clip = hitSound;
        audioSource.Play();
    }
    [PunRPC]
    void PlayDieSound()
    {
        audioSource.clip = DieSound;
        audioSource.Play();
    }
    [PunRPC]
    void PlaySkillSound()
    {
        audioSource.clip = SkillSound;
        audioSource.Play();
    }
    //[PunRPC]
    //void PlayGetItemSound()
    //{
    //    audioSource.clip = getItemSound;
    //    audioSource.Play();
    //}
    //[PunRPC]
    //void PlayUseItemSound()
    //{
    //    audioSource.clip = UseItemSound;
    //    audioSource.Play();
    //}

    //public void InvenPlaySound()
    //{
    //    PV.RPC("PlayGetItemSound", RpcTarget.All);
    //}


    #endregion

    #region 채팅
    public void Send()
    {
        PV.RPC("ChatRPC", RpcTarget.All, nicknameText.text + " : " + inputChatText.text);
        inputChatText.text = "";
    }

    [PunRPC] // RPC는 플레이어가 속해있는 방 모든 인원에게 전달한다
    void ChatRPC(string msg)
    {
        bool isInput = false;
        for (int i = 0; i < ChatText.Length; i++)
            if (ChatText[i].text == "")
            {
                isInput = true;
                ChatText[i].text = msg;
                break;
            }
        if (!isInput) // 꽉차면 한칸씩 위로 올림
        {
            for (int i = 1; i < ChatText.Length; i++) ChatText[i - 1].text = ChatText[i].text;
            ChatText[ChatText.Length - 1].text = msg;
        }
    }
    #endregion
    // Update is called once per frame
    [PunRPC]
    void updateHP()
    {
        HealthImage.fillAmount = curHp / (float)maxHp;
    }
    void Update()
    {
        //PV.RPC("updateHP", RpcTarget.All);
        if(PV.IsMine)
        {
            if(!visibleCursor&& !UseSkill) PlayerMove();
            else
            {
                if(Input.GetKeyDown(KeyCode.Return))
                {
                    Send();
                }
            }
        }
        //else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;
        else
        {
            transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime );
            //transform.rotation = Quaternion.Lerp(transform.rotation, curRot, Time.deltaTime );
        }

        if (PV.IsMine&&Input.GetKeyDown(KeyCode.Escape))
        {
            if (visibleCursor) { visibleCursor = false; cam.GetComponent<CameraMove>().canMove = true; }
            else { visibleCursor = true; cam.GetComponent<CameraMove>().canMove = false; }

            Cursor.visible = visibleCursor;
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "EnemyCollider"&&!isDamaged)
        {
            int da = other.gameObject.GetComponentInParent<Enemy>().damage;
            //int da = GameObject.Find("Boss(Clone)").GetComponent<Enemy>().damage;
            PV.RPC("DamageRoutine", RpcTarget.All,da);
        }
        else if(other.tag=="BossCollider"&&!isDamaged)
        {
            int da = other.gameObject.GetComponentInParent<Bear>().damage;
            Debug.Log("데미지 : "+da);
            PV.RPC("DamageRoutine", RpcTarget.All, da);
        }
        else if((other.tag=="Boss"||other.tag=="EnemyBullet")&&!isDamaged)
        {
            int da = other.gameObject.GetComponent<Enemy>().damage*2;
            PV.RPC("DamageRoutine", RpcTarget.All, da);
        }
    }
    [PunRPC]
    public void DamageRoutine(int damage)
    {
        StartCoroutine("Damaged");
        //약간 뒤로 충격 + 1초정도 무적 코루틴으로
        Debug.Log("적에게 맞음!");
        PV.RPC("PlayDamageSound", RpcTarget.All);
        curHp -= damage;
        HealthImage.fillAmount = curHp / (float)maxHp;

        if (PV.IsMine && curHp <= 0) PV.RPC("Died",RpcTarget.All);
    }

    public void startDamageRoutine(int damage)
    {

        PV.RPC("DamageRoutine", RpcTarget.All, damage);
    }

    IEnumerator Damaged()
    {
        isDamaged = true;

        yield return new WaitForSeconds(1f);
        isDamaged = false;

    }
    [PunRPC]
    public void Died()
    {
        Debug.Log("플레이어 사망");
        // 죽음 패널 후 버튼 누르면 리스폰
        //networkManager.playerDiePanel.SetActive(true);
        PV.RPC("PlayDieSound", RpcTarget.All);
        if(PV.IsMine) Diecamera.SetActive(true);
        if(PV.IsMine)networkManager.minusPlayerCount();
        gameObject.SetActive(false);
    }

    void MoveLookAt()
    {
        //메인카메라가 바라보는 방향입니다.
        Vector3 dir = cam.transform.localRotation * Vector3.forward;
        //카메라가 바라보는 방향으로 팩맨도 바라보게 합니다.
        transform.localRotation = cam.transform.localRotation;
        //팩맨의 Rotation.x값을 freeze해놓았지만 움직여서 따로 Rotation값을 0으로 세팅해주었습니다.
        transform.localRotation = new Quaternion(0, transform.localRotation.y, 0, transform.localRotation.w);

        player.transform.localRotation = transform.localRotation;
    }

    void PlayerMove()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        moveVec = new Vector3(horizontal, 0, vertical).normalized;
        if(cam.GetComponent<CameraMove>().canMove) MoveLookAt();


        // 카메라 이동방향
        Quaternion v3Rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        moveVec = v3Rotation * moveVec;

        if ((horizontal!=0||vertical!=0)&&Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetBool("IsRun", true);
            transform.position += moveVec *2* speed * Time.deltaTime;
        }
        else
        {
            animator.SetBool("IsRun", false);
            
        }

        
        if(horizontal!=0||vertical!=0)
        {
            animator.SetBool("IsWalk", true);
            transform.position += moveVec* speed * Time.deltaTime;
        }
        else
        {
            animator.SetBool("IsWalk", false);
        }

        if(Input.GetMouseButtonDown(0))
        {
            // 
            if(swingDelay+lastSwing<Time.time)
            {
                PV.RPC("PlayHitSound", RpcTarget.All);
                Debug.Log("공격!");
                //animator.SetTrigger("IsAttack");
                PV.RPC("setAttack", RpcTarget.All);
                weapon.UseWeapon();
                lastSwing = Time.time;
            }
        }
        else if(Input.GetMouseButtonDown(1))
        {
            if(!UseSkill&& curMp>=10)
            {
                StartCoroutine("skillEffect");
                PV.RPC("PlaySkillSound", RpcTarget.All);
                curMp -= 10;
                MPImage.fillAmount = curMp / (float)maxMp;
                Debug.Log("스킬 사용!");
                weapon.UseSkill();
            }
        }
    }
    [PunRPC]
    void startParticle()
    {
        skillParticle.SetActive(true);
    }
    [PunRPC]
    void stopParticle()
    {
        skillParticle.SetActive(false);
    }
    IEnumerator skillEffect()
    {
        PV.RPC("startParticle", RpcTarget.All);
        yield return new WaitForSeconds(1);
        PV.RPC("stopParticle", RpcTarget.All);
    }
    [PunRPC]
    void setAttack()
    {
        animator.SetTrigger("IsAttack");
    }
    public Text NickNameText;
    public PhotonView PV;
    private void Awake()
    {
        PV = GetComponentInParent<PhotonView>();
        
        //닉네임
        //NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        //NickNameText.color = PV.IsMine ? Color.green : Color.blue;
        if (PV.IsMine)
        {
            cam.gameObject.SetActive(true);
        }
    }

    // 포톤뷰의 요소들을 동기화 시킴
    // flipX를 동기화시키기 위해서 FlipXRPC함수를 PV.RPC함수를 통해 PV를 가지고있는 모든 사람에게 이 함수를 실행하라고함.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);    
            stream.SendNext(transform.rotation);
            //stream.SendNext(curHp);
            stream.SendNext(HealthImage.fillAmount);
            stream.SendNext(MPImage.fillAmount);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
            //curHp = (int)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
            MPImage.fillAmount = (float)stream.ReceiveNext();
        }
    }

}
