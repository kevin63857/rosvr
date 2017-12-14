using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Ros_CSharp;
using Messages;
using Messages.std_msgs;

using tf.net;
public class HandPublisher : MonoBehaviour {

    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;
    private Publisher<Messages.ihmc_msgs.HandTrajectoryRosMessage> handpub;

    private SteamVR_TrackedObject trackedObj;

    public int side;


    // Use this for initialization
    void Start () {
        //trackedObj = GetComponent<SteamVR_TrackedObject>();
        NodeHandle nh = rosmaster.getNodeHandle();
        handpub = nh.advertise<Messages.ihmc_msgs.HandTrajectoryRosMessage>("/ihmc_ros/valkyrie/control/hand_trajectory", 10);
    }
	
	// Update is called once per frame
	void Update () {

        emTransform emtransform = new emTransform(transform);
        //ta.UnityPosition += new Vector3(0.1f,0.1f,0.1f);

        Messages.ihmc_msgs.HandTrajectoryRosMessage msg = new Messages.ihmc_msgs.HandTrajectoryRosMessage();
        msg.unique_id = 1;
        msg.robot_side = (byte)side;
        msg.base_for_control = 0;


        Messages.ihmc_msgs.SE3TrajectoryPointRosMessage trajectory = new Messages.ihmc_msgs.SE3TrajectoryPointRosMessage();
        trajectory.position = emtransform.origin.ToMsg();
     //   if (using_gazebo)
     //       trajectory.position.z += 0.9;
        trajectory.orientation = emtransform.basis.ToMsg();
        trajectory.time = 0.5;
        //Debug.Log("Sending X: " + ta.origin.ToMsg().x + " Y: " + ta.origin.ToMsg().y + " Z: " + ta.origin.ToMsg().z);

        Messages.ihmc_msgs.SE3TrajectoryPointRosMessage[] trajectorys = { trajectory };

        msg.taskspace_trajectory_points = trajectorys;
        msg.Serialize(true);

        handpub.publish(msg);

    }
}
