using UnityEngine;
using System.Collections;
using Ros_CSharp;
using tf.net;
using VRTK;

[RequireComponent (typeof(VRTK_InteractableObject))]
public class headAttacher : MonoBehaviour
{
    GameObject hj;
    public string frame_id;
    //InteractableItem interactableItem;

    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;
    private bool startedInteracting = false;
    Messages.tf.tfMessage _tfmsg;

    // Use this for initialization
    void Start()
    {
        GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += HeadAttacher_InteractableObjectGrabbed;
        GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += HeadAttacher_InteractableObjectUngrabbed;
        GetComponent<VRTK_InteractableObject>().InteractableObjectUsed += HeadAttacher_InteractableObjectUsed;

        //interactableItem = GetComponent<InteractableItem>();
        nh = rosmaster.getNodeHandle();
        tfPub = nh.advertise<Messages.tf.tfMessage>("/tf", 10);
    }

    private void HeadAttacher_InteractableObjectUsed(object sender, InteractableObjectEventArgs e)
    {

        Messages.tf.tfMessage tfmsg = new Messages.tf.tfMessage();
        Messages.geometry_msgs.TransformStamped[] arr = new Messages.geometry_msgs.TransformStamped[1];
        arr[0] = new Messages.geometry_msgs.TransformStamped();

        tfmsg.transforms = arr;
        //Transform trans = trackedObj.transform;
        emTransform ta = new emTransform(transform, ROS.GetTime(), "/world", "/look_at_frame");

        Messages.std_msgs.Header hdr = new Messages.std_msgs.Header();
        hdr.frame_id = "/world";

        hdr.stamp = ROS.GetTime();
        hdr.stamp.data.sec += 18000;

        tfmsg.transforms[0].header = hdr;
        tfmsg.transforms[0].child_frame_id = "/look_at_frame";
        tfmsg.transforms[0].transform = new Messages.geometry_msgs.Transform();
        tfmsg.transforms[0].transform.translation = ta.origin.ToMsg();
        tfmsg.transforms[0].transform.rotation = ta.basis.ToMsg();
        tfmsg.Serialized = null;
        _tfmsg = tfmsg;
    }

    public void startInteracting()
    {
        startedInteracting = true;
    }

    public void stopInteracting()
    {
        startedInteracting = false;
    }

    private void HeadAttacher_InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
    {
        Invoke("stopInteracting", 1.0f);
        //startedInteracting = false;
    }

    private void HeadAttacher_InteractableObjectGrabbed(object sender, InteractableObjectEventArgs e)
    {
        //Invoke("startInteracting", 0.01f);
        startedInteracting = true;
    }

    void LateUpdate()
    {
        //If we have a valid tf message, publish it
        if(_tfmsg != null)
        {
            _tfmsg.transforms[0].header.stamp = ROS.GetTime();
            _tfmsg.transforms[0].header.stamp.data.sec += 18000; //windows time is dumb
            tfPub.publish(_tfmsg);
        }
        //wait for tf to initialize properly...
        if (hj == null)
            hj = GameObject.Find(frame_id);
        else if (!startedInteracting)
        {
            //Debug.Log("Not interacting!");
            Vector3 t = hj.transform.position;
            Quaternion q = hj.transform.rotation;
            float speed = 0.1f;
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, t, 0.05f);
        }
    }
}
