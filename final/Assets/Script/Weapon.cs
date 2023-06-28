using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    Collider weaponCollider=null;
    Collider skillCollider=null;
    public GameObject player=null;
    public CameraMove cam = null;
    //GameObject weapon;
    public int damage=50;
    void Start()
    {
        
        // 나중에 무기 꺼낼때나 먹을 때 드는 거로 바꿔야함
        weaponCollider = GameObject.Find("weaponCollider").GetComponent<BoxCollider>();
        weaponCollider.enabled = false;
        skillCollider = GameObject.Find("SkillCollider").GetComponent<SphereCollider>();
        skillCollider.enabled = false;
    }

    // Update is called once per frame
    public void UseWeapon()
    {
        // 해머면
        if (gameObject.CompareTag("Hammer"))
        {
            StopCoroutine("Weird");
            StartCoroutine("Weird");
        }
    }
    public void UseSkill()
    {
        if(gameObject.CompareTag("Hammer"))
        {
            Debug.Log("스킬 코루틴 시작0");
            //StopCoroutine("SkillHammer");
            StartCoroutine("SkillHammer");
        }
    }
    IEnumerator Weird()
    {
        skillCollider.enabled = false;
        yield return new WaitForSeconds(0.1f);
        weaponCollider.enabled = true;

        yield return new WaitForSeconds(0.6f);
        weaponCollider.enabled = false;
    }
    IEnumerator SkillHammer()
    {
        player.GetComponentInParent<PlayerController>().UseSkill = true;
        weaponCollider.enabled = false;
        yield return new WaitForSeconds(0.1f);
        skillCollider.enabled = true;
        yield return new WaitForSeconds(0.5f);

        skillCollider.enabled = false;
        player.GetComponentInParent<PlayerController>().UseSkill = false;
        yield return new WaitForSeconds(0.1f);
    }
}
