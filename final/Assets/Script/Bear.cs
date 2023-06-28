using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.AI;
using UnityEngine.UI;
public class Bear : MonoBehaviour
{
    public GameObject counterParticle;
    
    float accumulateDamage=0;
    bool isCounter=false;

    int damageCount = 0;
    public Rigidbody rigid;
    public int damage=10;
    Vector3 lookVec;
    Vector3 tauntVec;
    int maxHp=500;
    int curHp;
    public PhotonView PV;
    public float range = 30f;
    bool isDamaged = false;
    bool isAttack = false;
    bool isDead;
    public NavMeshAgent pathFinder;
    public bool isLook=false;
    public PlayerController target = null;
    Animator animator;
    public LayerMask whatIsTarget;
    public GameObject attackCollider;
    public GameObject BossPanel;
    public Image bossHPImage;

    [PunRPC]
    void UpdateBossHP(int h)
    {
        curHp = h;
        bossHPImage.fillAmount = curHp / (float)maxHp;
    }

    void Start()
    {
        if(PV.IsMine)
        {
            BossPanel = GameObject.Find("BossPanel");
            bossHPImage = GameObject.Find("FillBoss").GetComponent<Image>();
        }
        BossPanel = GameObject.Find("BossPanel");
        bossHPImage = GameObject.Find("FillBoss").GetComponent<Image>();
        PV.RPC("UpdateBossHP", RpcTarget.All,maxHp);
        BossPanel.SetActive(true);
        //curHp = maxHp;
        
        animator = GetComponent<Animator>();
        StartCoroutine("UpdatePath");
    }

    void Update()
    {
        if(!isDead)
        {
            rigid.velocity = Vector3.zero;
            if (!isCounter)
            {
                if (isLook)
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    lookVec = new Vector3(h, 0, v) * 5f;
                    transform.LookAt(target.transform.position + lookVec);
                    tauntVec = target.transform.position + lookVec;
                }
                else
                    pathFinder.SetDestination(tauntVec);
            }
        }
    }
    [PunRPC]
    void addDamage(float d)
    {
        accumulateDamage += d;
    }
    private void OnTriggerEnter(Collider other)
    {
        Weapon w = other.gameObject.GetComponentInParent<Weapon>();
        if (w == null)
        {
            Debug.Log("Weapon NULL!!"); return;
        }
        if (!isDamaged && other.tag == "Hammer")
        {
            if(isCounter)
            {
                PV.RPC("addDamage", RpcTarget.All, w.damage / 2.0f);
            }
            else
            {
                PV.RPC("DamageRoutine", RpcTarget.All, w.damage);
                damageCount++;
                if(damageCount>=5)
                {
                    PV.RPC("Counter", RpcTarget.All);
                    damageCount = 0;
                }
            }
            
        }
        else if (!isDamaged && other.tag == "HammerSkill")
        {
            PV.RPC("DamageRoutine", RpcTarget.All, w.damage * 2);
        }
    }
    public bool hasTarget
    {
        get
        {
            if (target != null && !target.isDead) return true;

            return false;
        }

    }
    IEnumerator LookForPlayer()
    {
        isLook = true;
        yield return new WaitForSeconds(2f);
        isLook = false;
    }

    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip DamageSound;
    public AudioClip DieSound;
    public AudioClip counterSound;
    [PunRPC]
    void PlayDamageSound()
    {
        audioSource.clip = DamageSound;
        audioSource.Play();
    }
    [PunRPC]
    void PlayCounterSound()
    {
        audioSource.clip = counterSound;
        audioSource.Play();
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
    [PunRPC]
    void Counter()
    {
        StartCoroutine("startCounter");
    }
    [PunRPC]
    void setSit(bool t)
    {
        animator.SetBool("Sit", t);
    }
    [PunRPC]
    void startParticle()
    {
        counterParticle.SetActive(true);
    }
    [PunRPC]
    void stopParticle()
    {
        counterParticle.SetActive(false);
    }
    IEnumerator startCounter()
    {
        StopCoroutine("UpdatePath");
        accumulateDamage = 0;
        PV.RPC("setSit", RpcTarget.All, true);
        yield return new WaitForSeconds(1f);
        isCounter = true;
        yield return new WaitForSeconds(1.5f);


        PV.RPC("startParticle", RpcTarget.All);
        yield return new WaitForSeconds(3.7f);
        PV.RPC("setSit", RpcTarget.All, false);
        // accumulateDamage 데미지 만큼 데미지 주기
        PV.RPC("PlayCounterSound", RpcTarget.All);
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("들어옴");
            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerRoot");
            Debug.Log("플레이어 숫자 : "+players.Length);
            for(int i=0;i<players.Length;i++)
            {
                Debug.Log("반격데미지맞음 : " + accumulateDamage);
                players[i].GetComponent<PlayerController>().startDamageRoutine((int)accumulateDamage);
            }
        }
        isCounter = false;
        PV.RPC("stopParticle", RpcTarget.All);
        yield return new WaitForSeconds(1f);
        StartCoroutine("UpdatePath");
    }

    [PunRPC]
    void DamageRoutine(int damage)
    {
        Debug.Log("Damage루틴 시작부분");
        StartCoroutine("Damaged");
        //약간 뒤로 충격 + 1초정도 무적 코루틴으로
        Debug.Log("Hammer에 맞음!");
        PV.RPC("UpdateBossHP", RpcTarget.All, curHp-damage);
        //curHp -= damage;
        Debug.Log(curHp);
       // hp.fillAmount = curHp / 100.0f;

        if (curHp <= 0) StartCoroutine("Died");
        
    }
    [PunRPC]
    void setDeath()
    {
        animator.SetTrigger("Death");
    }
    IEnumerator Died()
    {
        StopCoroutine("UpdatePath");
        PV.RPC("setDeath", RpcTarget.All);
        yield return new WaitForSeconds(2.0f);
        PV.RPC("PlayDieSound", RpcTarget.All);
        
        if (PV.IsMine)
        {
            
        }
        if (gameObject.tag != "Boss")
        {
            gameObject.SetActive(false);
            // Boss 스크립트로 옮겨야함
            //networkManager.bossMapExitPanel.SetActive(true);
            //Invoke("Respawn", 20f);
        }
        else
        {
            gameObject.SetActive(false);
            //networkManager.BossDie();
        }


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

    IEnumerator Attack()
    {
        PV.RPC("PlayHitSound", RpcTarget.All);
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("Attack1", true);
        isAttack = true;
        attackCollider.GetComponent<BoxCollider>().enabled = true;
        //attackCollider.transform.position += Vector3.back;
        yield return new WaitForSeconds(1.5f);
        attackCollider.GetComponent<BoxCollider>().enabled = false;
        //attackCollider.transform.position += Vector3.forward;
        isAttack = false;
        animator.SetBool("Attack1", false);
        animator.SetBool("IsWalk", true);
    }
    [PunRPC]
    void setTa()
    {
        StartCoroutine("setTarget");
    }
    IEnumerator setTarget()
    {
        yield return new WaitForSeconds(7f);
        target = null;
    }
    IEnumerator UpdatePath()
    {
        while (PV.IsMine&&!isDead)
        {

            if (hasTarget)
            {
                float Dist = Vector3.Distance(target.transform.position, transform.position);
                if (Dist > 30)
                {
                    isLook = false;
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
                        
                        StartCoroutine("Attack");
                        break;
                    }
                }
            }
            else
            {
                animator.SetBool("IsWalk", false);
                pathFinder.isStopped = true;

                Collider[] colliders =
                    Physics.OverlapSphere(transform.position, range, whatIsTarget);



                for (int i = 0; i < colliders.Length; i++)
                {
                    //죽지 않았고 거리가 닿는다면 
                    PlayerController p = colliders[i].gameObject.GetComponent<PlayerController>();

                    if (p != null && !p.isDead)
                    {
                        isLook = true;
                        target = p; 
                        if(PhotonNetwork.IsMasterClient)
                        {
                            PV.RPC("setTa", RpcTarget.All);
                        }
                        break;
                    }


                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
