using UnityEngine;
using System.Collections;
using Ros_CSharp;
using Messages;
using Messages.std_msgs;
using Messages.rosgraph_msgs;
using Messages.numl_val_msgs;
using Messages.ihmc_msgs;
using tf.net;

public class WandStatePublisher : MonoBehaviour
{
    public ROSCore rosmaster;

    public string twist_topic_name;
    private NodeHandle nh = null;
    private Publisher<Messages.tf.tfMessage> tfPub;
    private Publisher<Messages.ihmc_msgs.HandTrajectoryRosMessage> handpub;
    private Publisher<Messages.ihmc_msgs.PelvisHeightTrajectoryRosMessage> pelvispub;
    private Publisher<Messages.numl_val_msgs.HandPoseTrajectoryRosMessage> fingerpub;
  private Subscriber<Clock> clockSub;
    private Publisher<Messages.geometry_msgs.Twist> twistPub;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedObj.index); } }
    private SteamVR_TrackedObject trackedObj;

    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    private Valve.VR.EVRButtonId fingerButton = Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;

  public string child_frame_id;
    public int side;
    public bool using_gazebo;
    public bool use_grip_button;
    public bool use_twist;
    private bool squat = false;
    public float lowheight;
    public float standheight;
    private float timelapse = 0.0f;
    public bool fingersClosed = false;
    Clock c_;

    void Start () {
        Debug.Log("Starting a state manager");
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        Debug.Log("Initializing controller " + (int)trackedObj.index);

        NodeHandle nh = rosmaster.getNodeHandle();
        tfPub = nh.advertise<Messages.tf.tfMessage>("/tf", 10);
        handpub = nh.advertise<Messages.ihmc_msgs.HandTrajectoryRosMessage>("/ihmc_ros/valkyrie/control/hand_trajectory", 10);
        pelvispub = nh.advertise<Messages.ihmc_msgs.PelvisHeightTrajectoryRosMessage>("/ihmc_ros/valkyrie/control/pelvis_height_trajectory", 10);
        fingerpub = nh.advertise<Messages.numl_val_msgs.HandPoseTrajectoryRosMessage>("/arm_control", 10);
        twistPub = nh.advertise<Messages.geometry_msgs.Twist>(twist_topic_name, 10);
    }

    void Update() {
        if (controller == null || tfPub == null)
        {
            //Debug.Log("Controller Not Initialized");
            return;
        }

        if(controller.GetPressDown(fingerButton))
    {
      HandPoseTrajectoryRosMessage msg = new HandPoseTrajectoryRosMessage();

      msg.robot_side = HandPoseTrajectoryRosMessage.RIGHT;
      msg.execution_mode = HandPoseTrajectoryRosMessage.OVERRIDE;
      msg.desired_pose = HandPoseTrajectoryRosMessage.POSE;
      msg.unique_id = 15;
      msg.homeAllHandJoints = false;
      msg.homeAllForearmJoints = false;

      msg.forearm_joint_trajectory_messages = new OneDoFJointTrajectoryRosMessage[3];
      for (int i = 0; i < 3; i++)
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


      if (fingersClosed)
      {
        msg.hand_joint_trajectory_messages[0].trajectory_points[0].position = 1.5;

        msg.hand_joint_trajectory_messages[1].trajectory_points[0].position = 0.3;

        msg.hand_joint_trajectory_messages[2].trajectory_points[0].position = 0.2;

        msg.hand_joint_trajectory_messages[3].trajectory_points[0].position = 0.2;

        msg.hand_joint_trajectory_messages[4].trajectory_points[0].position = 0.2;

        msg.hand_joint_trajectory_messages[5].trajectory_points[0].position = 0.2;

        fingersClosed = false;
      }
      else
      {
        msg.hand_joint_trajectory_messages[0].trajectory_points[0].position = 1.5;

        msg.hand_joint_trajectory_messages[1].trajectory_points[0].position = 0.3;

        msg.hand_joint_trajectory_messages[2].trajectory_points[0].position = 2.0;

        msg.hand_joint_trajectory_messages[3].trajectory_points[0].position = 2.0;

        msg.hand_joint_trajectory_messages[4].trajectory_points[0].position = 2.0;

        msg.hand_joint_trajectory_messages[5].trajectory_points[0].position = 2.0;

        fingersClosed = true;
      }

      msg.Serialize(true);
      fingerpub.publish(msg);
    }

        if (controller.GetPress(triggerButton))
        {
            emTransform emtransform = new emTransform(transform);
            //ta.UnityPosition += new Vector3(0.1f,0.1f,0.1f);

            Messages.ihmc_msgs.HandTrajectoryRosMessage msg = new Messages.ihmc_msgs.HandTrajectoryRosMessage();
            msg.unique_id = 1;
            msg.robot_side = (byte)side;
            msg.base_for_control = 0;


            Messages.ihmc_msgs.SE3TrajectoryPointRosMessage trajectory = new Messages.ihmc_msgs.SE3TrajectoryPointRosMessage();
            trajectory.position = emtransform.origin.ToMsg();
            if(using_gazebo)
                trajectory.position.z += 0.9;
            trajectory.orientation = emtransform.basis.ToMsg();
            trajectory.time = 0.5;
            //Debug.Log("Sending X: " + ta.origin.ToMsg().x + " Y: " + ta.origin.ToMsg().y + " Z: " + ta.origin.ToMsg().z);

            Messages.ihmc_msgs.SE3TrajectoryPointRosMessage[] trajectorys = { trajectory };

            msg.taskspace_trajectory_points = trajectorys;
            msg.Serialize(true);
    
            handpub.publish(msg);
        }

        if(use_grip_button && controller.GetPressDown(gripButton))
        {
            float height = standheight;
            if (squat)
                height = lowheight;

            emTransform emtransform = new emTransform(transform);
            Messages.ihmc_msgs.PelvisHeightTrajectoryRosMessage msg = new Messages.ihmc_msgs.PelvisHeightTrajectoryRosMessage();
            msg.unique_id = 1;

            Messages.ihmc_msgs.TrajectoryPoint1DRosMessage trajectory = new Messages.ihmc_msgs.TrajectoryPoint1DRosMessage();
            trajectory.position = height;
            trajectory.time = 0.5;
            trajectory.velocity = 1.0;
            trajectory.unique_id = 1;

            Messages.ihmc_msgs.TrajectoryPoint1DRosMessage[] trajectorys = { trajectory };

            msg.trajectory_points = trajectorys;
            msg.Serialize(true);

            pelvispub.publish(msg);
            squat = !squat;
        }

       // Debug.Log("[0] " + controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[0] + "[1]" + controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[1]);
       
        if ( use_twist && (Mathf.Abs( controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[0])> 0.2f || Mathf.Abs(controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[1]) > 0.2f)
        && controller.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            if (UnityEngine.Time.time - timelapse > 3.0f)
            {
                timelapse = UnityEngine.Time.time;

                Messages.geometry_msgs.Twist twist = new Messages.geometry_msgs.Twist();
                twist.linear = new Messages.geometry_msgs.Vector3();
                twist.linear.x = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[1];
                twist.linear.y = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)[0];
                twistPub.publish(twist);
            }
            //Debug.Log("TRACKPAD DOWN");
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
        tfmsg.transforms[0].child_frame_id = "/ViveWand_"+gameObject.name.Split('(')[1].TrimEnd(')').ToLower();
        tfmsg.transforms[0].transform = new Messages.geometry_msgs.Transform();
        tfmsg.transforms[0].transform.translation = ta.origin.ToMsg();
        //tfmsg.transforms[0].transform.translation.z += 1.0;
        tfmsg.transforms[0].transform.rotation = ta.basis.ToMsg();
        tfmsg.Serialized = null;

        tfPub.publish(tfmsg);
    }
}
