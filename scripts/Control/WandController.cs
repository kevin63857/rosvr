using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WandController : MonoBehaviour {

    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    public bool gripButtonDown = false;
    public bool gripButtonUp = false;
    public bool gripButtonPressed = false;

    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;

    private Valve.VR.EVRButtonId appButton = Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;
    public bool triggerButtonDown = false;
    public bool triggerButtonUp = false;
    public bool triggerButtonPressed = false;
    public Object prefab;

    public bool touching = false;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedObj.index); } }
    private SteamVR_TrackedObject trackedObj;

    private GameObject pickup;

    HashSet<InteractableItem> objectsHoveringOver = new HashSet<InteractableItem>();
    private InteractableItem closestItem;
    private InteractableItem interactingItem;

    public GameObject WindowManager;
    private bool teleop = false;

    public GameObject face_mask;
    

    // Use this for initialization
    void Start () {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
	}
	
	// Update is called once per frame
	void Update () {
        //Debug.Log("Updating");
        if (controller == null)
        {
            Debug.Log("Controller Not Initialized");
            return;
        }
        gripButtonDown = controller.GetPressDown(gripButton);
        gripButtonUp = controller.GetPressUp(gripButton);
        gripButtonPressed = controller.GetPress(gripButton);

        triggerButtonDown = controller.GetPressDown(triggerButton);
        triggerButtonUp = controller.GetPressUp(triggerButton);
        triggerButtonPressed = controller.GetPress(triggerButton);

        controller.GetState();

      //  if (controller.GetPressDown(appButton))
      //  {
     //       //createWindow();
     //       toggleControl();
     //   }

        if(controller.GetPressDown(gripButton))
        {

            float minDistance = float.MaxValue;
            float distance;

            foreach(InteractableItem item in objectsHoveringOver)
            {
                distance = (item.transform.position - transform.position).sqrMagnitude;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestItem = item;
                }
            }
            interactingItem = closestItem;

            if(interactingItem)
            {
                if(interactingItem.IsInteracting())
                {
                    interactingItem.EndInteraction(this);
                }
                interactingItem.BeginInteraction(this);
            }


            //pickup.transform.parent = this.transform; //sets position, however gravity still works so no pickup
            //pickup.GetComponent<Rigidbody>().isKinematic = true; // makes it not effected by forces like gravity, however allows it t clip through objects
            //pickup.GetComponent<Rigidbody>().useGravity = false; //obvious, but downsides of funky physics.

        }
        else if (controller.GetPressUp(gripButton) && interactingItem != null)
        {
            if(interactingItem.doubleInteracting)
            {

            }
            interactingItem.EndInteraction(this);
        }

    }

    public void createWindow()
    {
       GameObject gb = (GameObject)Instantiate(prefab, transform.position, transform.rotation);
        gb.GetComponent<CompressedImageDisplay>().image_topic = "/multisense/left/image_color/compressed";
    }

    public void toggleControl()
    {
        face_mask.SetActive(!face_mask.activeSelf);
    }
    private void OnTriggerEnter(Collider collider)
    {
        /*pickup = collider.gameObject; &/ */
        InteractableItem collidedItem = collider.GetComponent<InteractableItem>();
        if(collidedItem)
        {
            Debug.Log("Touching something!");
            touching = true;
            objectsHoveringOver.Add(collidedItem);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        InteractableItem collidedItem = collider.GetComponent<InteractableItem>();
        if (collidedItem)
        {
            Debug.Log("Stopped touching!");
            touching = false;
            objectsHoveringOver.Remove(collidedItem);
        }
    }
}
