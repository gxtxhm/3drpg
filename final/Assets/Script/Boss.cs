using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;
public class Boss : Enemy
{
    public GameObject missle;
    PhotonView PV;
    Vector3 lookVec;
    Vector3 tauntVec;
    public bool isLook;
    public GameObject bullet;
    BoxCollider boxCollider;
    public GameObject meleeArea;
    public int BossDamage = 15;
    void Start()
    {
        PV = GetComponent<PhotonView>();
        hp = GameObject.Find("FillBoss").GetComponent<Image>();
        isBoss = true;
        curHp = maxHp;
        rigid = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<CapsuleCollider>();
        enemyCollider.enabled = true;
        pathFinder = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.enabled = true;
        pathFinder.isStopped = true;
        StartCoroutine(Think());
        hp.fillAmount = 1.0f;
        isLook = true;
        range = 150f;
        target = GameObject.Find("CharacterRoot(Clone)").GetComponent<PlayerController>();
        //PhotonNetwork.PlayerList[0]
    }

    // Update is called once per frame
    void Update()
    {
        if (!isDead)
        {
            //hp.fillAmount = curHp / 100.0f;
            //Debug.Log(curHp);
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
        else
        {
            Debug.Log("WW");
            StopAllCoroutines();
        }

    }

    IEnumerator Think()
    {
        boxCollider.enabled = true;
        enemyCollider.enabled = true;
        yield return new WaitForSeconds(0.1f);

        int ranAction = Random.Range(0, 3);

        switch (ranAction)
        {
            case 0:
            case 1:
                StartCoroutine(RockShot());
                break;
            case 2:
                StartCoroutine(Taunt());
                break;
        }

        IEnumerator RockShot()
        {
            isLook = false;
            animator.SetTrigger("doBigShot");
            Instantiate(bullet, transform.position, transform.rotation);
            yield return new WaitForSeconds(3.0f);

            isLook = true;
            StartCoroutine(Think());
        }

        IEnumerator Taunt()
        {
            tauntVec = target.transform.position + lookVec;

            isLook = false;
            pathFinder.isStopped = false;
            boxCollider.enabled = false;
            enemyCollider.enabled = false;
            animator.SetTrigger("doTaunt");

            yield return new WaitForSeconds(1.5f);
            meleeArea.GetComponent<BoxCollider>().enabled=true;

            yield return new WaitForSeconds(0.8f);
            meleeArea.GetComponent<BoxCollider>().enabled = false;

            yield return new WaitForSeconds(1.0f);
            isLook = true;
            pathFinder.isStopped = true;
            boxCollider.enabled = true;
            enemyCollider.enabled = true;
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(Think());

        }
    }
}
