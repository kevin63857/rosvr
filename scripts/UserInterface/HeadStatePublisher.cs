using UnityEngine;
using System.Collections;
using Ros_CSharp;
using tf.net;
using Messages.rosgraph_msgs;

public class HeadStatePublisher : MonoBehaviour
{
    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;
    private Subscriber<Clock> clockSub;

    private SteamVR_Controller.Device HMD { get { return SteamVR_Controller.Input(0); } }
    private SteamVR_TrackedObject trackedObj;

    //public TimeManager timeManager;

    public string child_frame_id;

    Clock c_;
    bool simTime;
    // Use this for initialization
    void Start () {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        nh = rosmaster.getNodeHandle();
        tfPub = nh.advertise<Messages.tf.tfMessage>("/tf", 1);
    }
	
	// Update is called once per frame
	void Update () {
        if (HMD == null)
        {
            Debug.Log("Controller Not Initialized");
            return;
        }

        Messages.tf.tfMessage tfmsg = new Messages.tf.tfMessage();

        Messages.geometry_msgs.TransformStamped[] arr = new Messages.geometry_msgs.TransformStamped[1];
        arr[0] = new Messages.geometry_msgs.TransformStamped();

        tfmsg.transforms = arr;
        Transform trans = trackedObj.transform;


        emTransform ta = new emTransform(trans, ROS.GetTime(), "world", child_frame_id);

        Messages.std_msgs.Header hdr = new Messages.std_msgs.Header();
        hdr.frame_id = "world";

        hdr.stamp = ROS.GetTime();
        hdr.stamp.data.sec += 18000;

        tfmsg.transforms[0].header = hdr;
        tfmsg.transforms[0].child_frame_id = "HMD";
        tfmsg.transforms[0].transform = new Messages.geometry_msgs.Transform();
        tfmsg.transforms[0].transform.translation = ta.origin.ToMsg();
        tfmsg.transforms[0].transform.rotation = ta.basis.ToMsg();

        //Debug.Log("MY REAL IS " + tfmsg.transforms[0].transform.rotation.x + " " + tfmsg.transforms[0].transform.rotation.y + " " + tfmsg.transforms[0].transform.rotation.z + " " + tfmsg.transforms[0].transform.rotation.w);
        tfmsg.Serialized = null;

        tfPub.publish(tfmsg);

    }
}
