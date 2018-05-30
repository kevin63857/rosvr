using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ros_CSharp;
using Messages.ihmc_msgs;
using Messages.numl_val_msgs;

public class FingerPublisher : MonoBehaviour {
    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<HandPoseTrajectoryRosMessage> handpub;

    HandPoseTrajectoryRosMessage msg = new HandPoseTrajectoryRosMessage();
    //HandData data;

    public double index, middle, pinky, thumb;
    public bool left_hand_closed = false;
    // Use this for initialization
    void Start () {
        /*data = GetComponent<HandData>();

        NodeHandle nh = rosmaster.getNodeHandle();
        handpub = nh.advertise<HandPoseTrajectoryRosMessage>("/arm_control", 10);

        msg.robot_side = HandPoseTrajectoryRosMessage.RIGHT;
        msg.execution_mode = HandPoseTrajectoryRosMessage.OVERRIDE;
        msg.desired_pose = HandPoseTrajectoryRosMessage.DELTA;
        msg.unique_id = 15;
        msg.homeAllHandJoints = false;
        msg.homeAllForearmJoints = false;

        msg.forearm_joint_trajectory_messages = new OneDoFJointTrajectoryRosMessage[3];
        for(int i = 0; i <3; i++)
        {
            msg.forearm_joint_trajectory_messages[i] = new OneDoFJointTrajectoryRosMessage();
            msg.forearm_joint_trajectory_messages[i].trajectory_points = new TrajectoryPoint1DRosMessage[1];
            msg.forearm_joint_trajectory_messages[i].trajectory_points[0] = new TrajectoryPoint1DRosMessage();
        }

        msg.hand_joint_trajectory_messages = new OneDoFJointTrajectoryRosMessage[6];


        for (int i = 0; i < 6; i++)
        {
            msg.hand_joint_trajectory_messages[i] = new OneDoFJointTrajectoryRosMessage();
            msg.hand_joint_trajectory_messages[i].trajectory_points = new TrajectoryPoint1DRosMessage[1];
            msg.hand_joint_trajectory_messages[i].trajectory_points[0] = new TrajectoryPoint1DRosMessage();
        }
        */

    }
	
	// Update is called once per frame
	void Update () {
        /*left_hand_closed = data.IsClosed(ManusVR.device_type_t.GLOVE_LEFT);
        //left_hand_closed = true;

        if (left_hand_closed)
        {
            index = (data.indexBend + data.indexBend1) * 2;
            pinky = (data.pinkyBend + data.pinkyBend1) * 2;
            middle = (data.middleBend + data.middleBend1) * 2;
            thumb = (data.thumbBend + data.thumbBend1) * 2.0;

            if (index < 0.3)
                index = 0.3;
            else if (index > 2.9)
                index = 2.9;

            if (pinky < 0.3)
                pinky = 0.3;
            else if (pinky > 3.0)
                pinky = 3.0;

            if (middle < 0.3)
                middle = 0.3;
            else if (middle > 3.0)
                middle = 3.0;

            if (thumb < 0.3)
                thumb = 0.3;
            else if (thumb > 2.0)
                thumb = 2.0;

            msg.hand_joint_trajectory_messages[0].trajectory_points[0].position = 1.5;

            msg.hand_joint_trajectory_messages[1].trajectory_points[0].position = 0.3;

            msg.hand_joint_trajectory_messages[2].trajectory_points[0].position = thumb;

            msg.hand_joint_trajectory_messages[3].trajectory_points[0].position = index;

            msg.hand_joint_trajectory_messages[4].trajectory_points[0].position = middle;

            msg.hand_joint_trajectory_messages[5].trajectory_points[0].position = pinky;

            msg.Serialize(true);
            handpub.publish(msg);
        } */
    }
}
