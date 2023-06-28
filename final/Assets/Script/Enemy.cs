using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class Enemy : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip DamageSound;
    public AudioClip DieSound;

    NetworkManager networkManager;
    public int damage = 5;
    public int maxHp;
    public int curHp;
    public Rigidbody rigid;
    public Collider enemyCollider;
    public GameObject attackCollider;
    public NavMeshAgent pathFinder;
    public PlayerController target = null;
    public Animator animator;
    public LayerMask whatIsTarget;
    public Image hp;
    PhotonView PV;
    //ItemSpawner instance;
    //Inventory inv;

    public float range = 60f;
    bool isDamaged = false;
    bool isAttack = false;
    public bool isBoss = false;
    public bool isDead = false;

    public bool hasTarget
    {
        get
        {
            if (target != null && !target.isDead) return true;

            return false;
        }

    }


    void Start()
    {
        if(gameObject.tag=="Boss") hp = GameObject.Find("FillBoss").GetComponent<Image>();

        //instance = GameObject.Find("ItemSpawner").GetComponent<ItemSpawner>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        PV = GetComponent<PhotonView>();
        maxHp = 100;
        curHp = maxHp;
        rigid = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<CapsuleCollider>();
        pathFinder = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        //if(gameObject.tag!="Boss") hp.fillAmount = curHp / (float)maxHp;
        hp.fillAmount = curHp / 100.0f;
        StartCoroutine("UpdatePath");
        if (isBoss == false)
        {
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        hp.fillAmount = curHp / 100.0f;
    }
    private void OnTriggerEnter(Collider other)
    {
        Weapon w= other.gameObject.GetComponentInParent<Weapon>();
        if(w==null)
        {
            Debug.Log("Weapon NULL!!");return;
        }
        if (!isDamaged && other.tag == "Hammer")
        {
            
            PV.RPC("DamageRoutine", RpcTarget.All, w.damage);
            
        }
        else if (!isDamaged && other.tag == "HammerSkill")
        {
            PV.RPC("DamageRoutine", RpcTarget.All,w.damage*2);
        }
    }   
    [PunRPC]
    void DamageRoutine(int damage)
    {
        Debug.Log("Damage루틴 시작부분");
        StartCoroutine("Damaged");
        //약간 뒤로 충격 + 1초정도 무적 코루틴으로
        Debug.Log("Hammer에 맞음!");
        curHp -= damage;
        Debug.Log(curHp);
        hp.fillAmount = curHp / 100.0f;

        if (curHp <= 0) StartCoroutine("Died");

        
    }
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
    IEnumerator Died()
    {
        StopCoroutine("UpdatePath");
        animator.SetTrigger("IsDied");
        yield return new WaitForSeconds(1.0f);
        PV.RPC("PlayDieSound", RpcTarget.All);
        // 이거 rpc로 변수 줄이기
        networkManager.curMonsterCount--;
        Debug.Log("죽음" + networkManager.curMonsterCount);
        if (PhotonNetwork.IsMasterClient)
        {
            
            PhotonNetwork.Destroy(gameObject);
        }
        if (PV.IsMine)
        {
            //instance.SpawnItem(transform.position);
            // rpc로실행하기 바꿔야함
            //if (isBoss)
            //{
            //    inv = GameObject.Find("CharacterRoot(Clone)").GetComponent<Inventory>();
            //    inv.gainGold(Random.Range(430, 551));
            //}
            //else
            //{
            //    inv = GameObject.Find("CharacterRoot(Clone)").GetComponent<Inventory>();
            //    int j = Random.Range(100, 151);
            //    inv.gainGold(j);

            //}
        }
        if(gameObject.tag!="Boss")
        {
            gameObject.SetActive(false);
            // Boss 스크립트로 옮겨야함
            //networkManager.bossMapExitPanel.SetActive(true);
            //Invoke("Respawn", 20f);
        }
        else
        {
            gameObject.SetActive(false);
            networkManager.BossDie();
        }

        
    }
    IEnumerator Damaged()
    {
        PV.RPC("PlayDamageSound", RpcTarget.All);
        Debug.Log("데미지 시작부분");
        yield return new WaitForSeconds(0.1f);
        isDamaged = true;
        yield return new WaitForSeconds(0.6f);
        isDamaged = false;

    }
    IEnumerator Attack()
    {
        PV.RPC("PlayHitSound", RpcTarget.All);
        yield return new WaitForSeconds(0.1f);
        isAttack = true;
        attackCollider.GetComponent<BoxCollider>().enabled = true;
        //attackCollider.transform.position += Vector3.back;
        yield return new WaitForSeconds(0.4f);
        attackCollider.GetComponent<BoxCollider>().enabled = false;
        //attackCollider.transform.position += Vector3.forward;
        yield return new WaitForSeconds(0.8f);
        isAttack = false;
        

    }
    [PunRPC]
    void setAtt()
    {
        animator.SetTrigger("IsAttack");
    }

    IEnumerator UpdatePath()
    {
        while (PV.IsMine&&!isDead)
        {
            
            if (hasTarget)
            {
                float Dist=Vector3.Distance(target.transform.position, transform.position);
                if (Dist > 10)
                {
                    target = null;
                    animator.SetBool("IsWalk", false);
                    pathFinder.isStopped = true;
                    continue;
                }

                pathFinder.isStopped = false;
                pathFinder.SetDestination(target.transform.position);
                animator.SetBool("IsWalk", true);

                Collider[] colliders =
                    Physics.OverlapSphere(transform.position, 3f, whatIsTarget);

                for (int i = 0; i < colliders.Length; i++)
                {
                    //죽지 않았고 거리가 닿는다면 
                    PlayerController p = colliders[i].gameObject.GetComponent<PlayerController>();

                    if (p != null && p == target && !isAttack && !p.isDead)
                    {
                        PV.RPC("setAtt", RpcTarget.All);
                        StartCoroutine("Attack");
                        break;
                    }
                }
            }
            else
            {
                if(gameObject.tag!="Boss")animator.SetBool("IsWalk", false);
                pathFinder.isStopped = true;

                Collider[] colliders =
                    Physics.OverlapSphere(transform.position, range, whatIsTarget);



                for (int i = 0; i < colliders.Length; i++)
                {
                    //죽지 않았고 거리가 닿는다면 
                    PlayerController p = colliders[i].gameObject.GetComponent<PlayerController>();

                    if (p != null && !p.isDead)
                    {
                        target = p; break;
                    }


                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

}
