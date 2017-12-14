using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ros_CSharp;
using Messages;
using tf.net;

public class HeadControl : MonoBehaviour
{
    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.ihmc_msgs.NeckTrajectoryRosMessage> pub;

    // Use this for initialization
    void Start () {
        nh = rosmaster.getNodeHandle();
        pub = nh.advertise<Messages.ihmc_msgs.NeckTrajectoryRosMessage>("/ihmc_ros/valkyrie/control/neck_trajectory", 10);
    }
	
	// Update is called once per frame
	void Update () {
        Messages.ihmc_msgs.NeckTrajectoryRosMessage msg = new Messages.ihmc_msgs.NeckTrajectoryRosMessage();
        msg.joint_trajectory_messages = new Messages.ihmc_msgs.OneDoFJointTrajectoryRosMessage[3];
        msg.joint_trajectory_messages[0].trajectory_points = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage[1];
        msg.joint_trajectory_messages[1].trajectory_points = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage[1];
        msg.joint_trajectory_messages[2].trajectory_points = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage[1];

        msg.joint_trajectory_messages[0].trajectory_points[0] = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage();
        msg.joint_trajectory_messages[1].trajectory_points[0] = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage();
        msg.joint_trajectory_messages[2].trajectory_points[0] = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage();

        msg.joint_trajectory_messages[0].trajectory_points[0].time = 0.0f;
        msg.joint_trajectory_messages[0].trajectory_points[0].position = 0.0f;
        msg.joint_trajectory_messages[0].trajectory_points[0].velocity = 0.0f;

        msg.joint_trajectory_messages[1].trajectory_points[0].time = 0.0f;
        msg.joint_trajectory_messages[1].trajectory_points[0].position = 0.0f;
        msg.joint_trajectory_messages[1].trajectory_points[0].velocity = 0.0f;

        msg.joint_trajectory_messages[2].trajectory_points[0].time = 0.0f;
        msg.joint_trajectory_messages[2].trajectory_points[0].position = 0.0f;
        msg.joint_trajectory_messages[2].trajectory_points[0].velocity = 0.0f;
    }
}
