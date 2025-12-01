  using UnityEngine;
using System.Collections;

public class CamHolder : MonoBehaviour {

	public Transform cam_target;
    public Transform[] cam_targetAll;
    public float smooth = 0.5f;
	public float rotSpeed = 50f;
	public float zoomSpeed = 3f;
    public float horizontalRotation = 50f;
	public Camera cam;
    public Transform camRotator;
	private Vector3 start_angle;
	private int currentCamTarget;


	void Start () 
	{
		currentCamTarget = 0;
	//	cam_target = cam_targetAll[currentCamTarget];
		start_angle = transform.position; 
	}

    private void Update()
    {
        if (Input.GetKey("z"))
        {
            transform.Rotate(new Vector3(0, rotSpeed * Time.deltaTime, 0));
        }
        if (Input.GetKey("c"))
        {
             transform.Rotate(0, rotSpeed * Time.deltaTime * -1, 0);
           // transform.localRotation *= Quaternion.Euler(0, rotSpeed * Time.deltaTime * -1,0);
        }

        if (Input.mouseScrollDelta.y < 0)
        {
            cam.GetComponent<Camera>().fieldOfView += zoomSpeed * Time.deltaTime;
            //transform.Rotate(new Vector4 (0, 0, zoomSpeed * Time.deltaTime));
            camRotator.transform.localRotation *=  Quaternion.Euler(horizontalRotation * Time.deltaTime, 0, 0);
        }
        if (Input.mouseScrollDelta.y > 0)
        {
            cam.GetComponent<Camera>().fieldOfView -= zoomSpeed * Time.deltaTime;
            //transform.Rotate(new Vector3(0, 0, zoomSpeed * Time.deltaTime));
            camRotator.transform.localRotation *= Quaternion.Euler(-horizontalRotation * Time.deltaTime,0,0);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //NextTarget();
        }
    }

    void FixedUpdate () 
	{
		if (cam_target != null) 
		{
			Vector3 desPosition = cam_target.position;
			Vector3 smoothedPosition = Vector3.Lerp(transform.position, desPosition, smooth*Time.deltaTime);
			transform.position = smoothedPosition;	
		}
	}
/*
	public void NextTarget()
	{
		if (currentCamTarget > cam_targetAll.Length) currentCamTarget = 0;
		if (cam_target != null) 
		{
			cam_target = cam_targetAll[currentCamTarget++];
		}
	}
*/
}
