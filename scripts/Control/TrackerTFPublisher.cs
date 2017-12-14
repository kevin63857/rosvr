using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ros_CSharp;
using Messages;
using Messages.rosgraph_msgs;
using tf.net;

public class TrackerTFPublisher : MonoBehaviour {

    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;

    private SteamVR_TrackedObject trackedObj;

    public int side;
    public string child_frame_id;

    // Use this for initialization
    void Start () {
        Debug.Log("Starting a state manager");
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        //Debug.Log("Initializing controller " + (int)trackedObj.index);

        NodeHandle nh = rosmaster.getNodeHandle();
        tfPub = nh.advertise<Messages.tf.tfMessage>("/tf", 10);
    }
	
	// Update is called once per frame
	void Update () {
        if(trackedObj == null)
        {
            trackedObj = GetComponent<SteamVR_TrackedObject>();
            return;
        }
        Messages.tf.tfMessage tfmsg = new Messages.tf.tfMessage();

        Messages.geometry_msgs.TransformStamped[] arr = new Messages.geometry_msgs.TransformStamped[1];
        arr[0] = new Messages.geometry_msgs.TransformStamped();

        tfmsg.transforms = arr;
        Transform trans = trackedObj.transform;
        emTransform ta = new emTransform(trans, ROS.GetTime(), "/world", child_frame_id + gameObject.name.Split('(')[1].TrimEnd(')').ToLower());

        Messages.std_msgs.Header hdr = new Messages.std_msgs.Header();
        hdr.frame_id = "/world";

        hdr.stamp = ROS.GetTime();//timeManager.getTime();
        hdr.stamp.data.sec += 18000;

        tfmsg.transforms[0].header = hdr;
        tfmsg.transforms[0].child_frame_id = "/ViveWand_" + gameObject.name.Split('(')[1].TrimEnd(')').ToLower();
        tfmsg.transforms[0].transform = new Messages.geometry_msgs.Transform();
        tfmsg.transforms[0].transform.translation = ta.origin.ToMsg();
        //tfmsg.transforms[0].transform.translation.z += 1.0;
        tfmsg.transforms[0].transform.rotation = ta.basis.ToMsg();
        tfmsg.Serialized = null;

        tfPub.publish(tfmsg);
    }
}
