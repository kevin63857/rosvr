using UnityEngine;
using System.Collections;
using Ros_CSharp;
using tf.net;

public class headAttacher : MonoBehaviour
{
    GameObject hj;
    public string frame_id;
    InteractableItem interactableItem;

    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;
    private bool startedInteracting = false;
    Messages.tf.tfMessage _tfmsg;

    // Use this for initialization
    void Start()
    {
        interactableItem = GetComponent<InteractableItem>();
        nh = rosmaster.getNodeHandle();
        tfPub = nh.advertise<Messages.tf.tfMessage>("/tf", 10);
    }

    // Update is called once per frame
    bool publishtf = false;
    void Update()
    {
        if(publishtf)
        {
            _tfmsg.transforms[0].header.stamp = ROS.GetTime();
            _tfmsg.transforms[0].header.stamp.data.sec += 18000; //windows time is dumb
            //Debug.Log("Current time" + ROS.GetTime().data.sec);
            tfPub.publish(_tfmsg);
        }
        if (hj == null)
            hj = GameObject.Find(frame_id);
        else if (!interactableItem.IsInteracting())
        {
            if (!startedInteracting)
            {
                //Debug.Log("Not interacting!");
                Vector3 t = hj.transform.position;
                Quaternion q = hj.transform.rotation;
                transform.position = t;
                transform.rotation = q;
            }
            if (startedInteracting)
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

                tfPub.publish(tfmsg);
                _tfmsg = tfmsg;
                publishtf = true;
                startedInteracting = false;
            }
        }
        else
            startedInteracting = true;
    }
}
