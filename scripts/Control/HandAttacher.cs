using UnityEngine;
using Ros_CSharp;
using System.Collections;
using Messages;
using tf.net;

public class HandAttacher : MonoBehaviour
{
    GameObject hj;
    InteractableItem interactableItem;
    public string frame_id;
    public int side; //0 left, 1 right
    private bool startedInteracting = false;

    // Use this for initialization

    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Publisher<Messages.ihmc_msgs.HandTrajectoryRosMessage> pub;

    void Start () {
        interactableItem = GetComponent<InteractableItem>();

        nh = rosmaster.getNodeHandle();
        pub = nh.advertise<Messages.ihmc_msgs.HandTrajectoryRosMessage>("/ihmc_ros/valkyrie/control/hand_trajectory", 10);
    }
	
	// Update is called once per frame
	void Update () {

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
                //Debug.Log("Stopped Interacting!");
                emTransform ta = new emTransform(transform);
                //ta.UnityPosition += new Vector3(0.1f,0.1f,0.1f);

                Messages.ihmc_msgs.HandTrajectoryRosMessage msg = new Messages.ihmc_msgs.HandTrajectoryRosMessage();
                msg.unique_id = 1;
                msg.robot_side = (byte)side;
                msg.base_for_control = 0;
                

                Messages.ihmc_msgs.SE3TrajectoryPointRosMessage trajectory = new Messages.ihmc_msgs.SE3TrajectoryPointRosMessage();
                trajectory.position = ta.origin.ToMsg();
                //trajectory.position.z += 1.0;
                trajectory.orientation = ta.basis.ToMsg();
                trajectory.time = 2.0;
                Debug.Log("Sending X: " + ta.origin.ToMsg().x + " Y: " + ta.origin.ToMsg().y + " Z: " + ta.origin.ToMsg().z);

                Messages.ihmc_msgs.SE3TrajectoryPointRosMessage[] trajectorys = { trajectory };

                msg.taskspace_trajectory_points = trajectorys;
                msg.Serialize(true);

                pub.publish(msg);
                startedInteracting = false;
            }

        }
        else
            startedInteracting = true;
    }
}
