using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class CameraMove : MonoBehaviour
{
    public PlayerController player;
    public float x=0.23f, y=7.19f, z=-7.54f;
    Vector3 vec;
    [SerializeField]
    PhotonView PV;
    public bool canMove=true;

    void Start()
    {
        vec = new Vector3(x, y, z);
        PV = GetComponentInParent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        //if(player!=null) transform.position = player.gameObject.transform.position + vec;
        //else
        //{
        //    player = GetComponent<PlayerController>();return;
        //}
        
    }
    public Transform follow;
    [SerializeField] float m_Speed;
    [SerializeField] float m_MaxRayDist = 1;
    [SerializeField] float m_Zoom = 3f;
    RaycastHit m_Hit;
    Vector2 m_Input;

    private float xRotateMove, yRotateMove;

    public float rotateSpeed = 500.0f;

    void Rotate()
    {
        xRotateMove = Input.GetAxis("Mouse X") * Time.deltaTime * rotateSpeed;
        yRotateMove = Input.GetAxis("Mouse Y") * Time.deltaTime * rotateSpeed;

        Vector3 stagePosition = follow.transform.position;

        transform.RotateAround(stagePosition, Vector3.up, xRotateMove);

        transform.LookAt(stagePosition);
    }

    void Zoom()
    {
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Transform cam = Camera.main.transform;
            if (CheckRay(cam, scroll))
            {
                Vector3 targetDist = cam.transform.position - follow.transform.position;
                targetDist = Vector3.Normalize(targetDist);
                Camera.main.transform.position -= (targetDist * scroll * m_Zoom);
            }
        }

        Camera.main.transform.LookAt(follow.transform);
    }

    public void LateUpdate()
    {
        if (PV.IsMine)
        {
            if (canMove)
            {
                Rotate();Zoom();
            }
            
        }


        
    }

    bool CheckRay(Transform cam, float scroll)
    {
        if (Physics.Raycast(cam.position, transform.forward, out m_Hit, m_MaxRayDist))
        {
            Debug.Log("hit point : " + m_Hit.point + ", distance : " + m_Hit.distance + ", name : " + m_Hit.collider.name);
            Debug.DrawRay(cam.position, transform.forward * m_Hit.distance, Color.red);
            cam.position += new Vector3(0, 0, m_Hit.point.z);
            return false;
        }

        return true;
    }
}
