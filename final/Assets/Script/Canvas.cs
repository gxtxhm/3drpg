using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canvas : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip buttonSound;
    //public InventoryUI invenUI;
    // Start is called before the first frame update
    void Start()
    {
        //invenUI.enabled = false;
        audioSource.clip = buttonSound;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnInventoryUI()
    {
        //invenUI.enabled = true;
    }
    public void OnPlaySound()
    {
        audioSource.Play();
    }
}
