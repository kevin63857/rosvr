using UnityEngine;
using VRTK;
using System.Collections;
using Ros_CSharp;
using tf.net;
using XmlRpc_Wrapper;
using Messages;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace Valkyrie_VR
{
  public class mesh_pcloud_rendering : MonoBehaviour
  {
    //public ROSCore rosmaster;
    static int[] indices_cube = {
          0,  1,  2,  0,  2,  3,  //front
          1,  5,  6,  1,  2,  6,  //right
          4,  5,  6,  4,  6,  7,  //back
          0,  4,  7,  0,  3,  7,  //left
          3,  2,  6,  3,  7,  6,  //upper
          0,  1,  5,  0,  4,  5}; //bottom
    static int[] indices_octahedron = {
          0,  1,  4,
          1,  2,  4,
          2,  3,  4,
          3,  0,  4,
          0,  1,  5,
          1,  2,  5,
          2,  3,  5,
          3,  0,  5};
    static int[] indices_pyramid = {
          0,  1,  3,
          1,  2,  3,
          2,  0,  3,
          0,  1,  2};
    static int[] indices_square = {
          0,  1,  2,
          0,  2,  3};
    static int[] indices_triangle = {
          0,  1,  2};
    public enum PointShapes { CUBE, OCTAHEDRON, PYRAMID, SQUARE, TRIANGLE };
    Dictionary<PointShapes, int[]> indices = new Dictionary<PointShapes, int[]>(){
                                    {PointShapes.CUBE, indices_cube},
                                    {PointShapes.OCTAHEDRON, indices_octahedron},
                                    {PointShapes.PYRAMID, indices_pyramid},
                                    {PointShapes.SQUARE, indices_square},
                                    {PointShapes.TRIANGLE, indices_triangle}};
    Dictionary<PointShapes, int> vertexCount = new Dictionary<PointShapes, int>(){
                                    {PointShapes.CUBE, 8},
                                    {PointShapes.OCTAHEDRON, 6},
                                    {PointShapes.PYRAMID, 4},
                                    {PointShapes.SQUARE, 4},
                                    {PointShapes.TRIANGLE, 3}};

    public int rot_z = 0, rot_y = 0, rot_x = 0;
    public PointShapes pointShape = PointShapes.CUBE;
    public string TFName;
    public bool spread_updates = false, hard_recalculate_triangles = false, include_color = true, accumulate_data = false;
    public int max_acc_frames = 0;
    public VRTK_StraightPointerRenderer_PointCloudInteracter laserPointer;
    public string topic_name = "/kinect2_old/hd/points";
    public bool is_static;
    public TextAsset Mesh_File;

    private Publisher<Messages.geometry_msgs.PolygonStamped> unity_tf_pub;
    private Subscriber<Messages.sensor_msgs.PointCloud2> sub;
    private NodeHandle nh;
    private Messages.sensor_msgs.PointCloud2 msg;
    private mesh_pcloud[] theCloud;
    private int current_cloud_buffer = 0;
    private bool updateVertexData = false, updateTriangleData = false, updateColorData = false;
    private int pointNum = 0;
    private float SQRT_2 = (float)Math.Sqrt(2);
    private TfVisualizer tfvisualizer;
    private Thread pub_thread;
    Messages.geometry_msgs.PolygonStamped unity_tf_msg;

    public void UnityTFPublisher()
    {
      bool is_aborted = false;
      try
      {
        while (!is_aborted)
        {
          unity_tf_pub.publish(unity_tf_msg);
          Thread.Sleep(10);
        }
      }
      catch(ThreadAbortException e)
      {
        print(e);
        is_aborted = true;
      }
    }
    protected Transform TF
    {
      get
      {
        if (TFName == null)
        {
          return transform;
        }

        Transform tfTemp;
        String strTemp = TFName;
        if (!strTemp.StartsWith("/"))
        {
          strTemp = "/" + strTemp;
        }
        if (tfvisualizer != null && tfvisualizer.queryTransforms(strTemp, out tfTemp))
          return tfTemp;
        return transform;
      }
    }
    static float ToFloat(byte a, byte b, byte c, byte d)
    {
      return System.BitConverter.ToSingle(new[] { a, b, c, d }, 0);
    }
    static int ToInt(byte a, byte b, byte c, byte d)
    {
      return System.BitConverter.ToInt32(new[] { d, c, b, a }, 0);
    }
    private void registerMeshPclouds()
    {
      if (laserPointer != null)
      {
        for (int i = 0; i < theCloud.Length; i++)
        {
          laserPointer.RegisterPcloud(theCloud[i]);
        }
      }
    }
    public void addPoint(float x, float y, float z, Color c)
    {
      if ( false && pointNum % 10000 == 0)
      {
        print(x + " " + y + " " + z + " " + c);
      }
      int shape_vertex_count = vertexCount[pointShape];
      float triangleSize = .01F;//(z / 500F);
      switch (pointShape)
      {
        case PointShapes.CUBE:
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 0] = new Vector3(x, y, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 1] = new Vector3(x + triangleSize, y, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 2] = new Vector3(x + triangleSize, y + triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 3] = new Vector3(x, y + triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 4] = new Vector3(x, y, z + triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 5] = new Vector3(x + triangleSize, y, z + triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 6] = new Vector3(x + triangleSize, y + triangleSize, z + triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 8) + 7] = new Vector3(x, y + triangleSize, z + triangleSize);
          break;
        case PointShapes.OCTAHEDRON:
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 0] = new Vector3(x, y, z + triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 1] = new Vector3(x + triangleSize, y, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 2] = new Vector3(x, y, z - triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 3] = new Vector3(x - triangleSize, y, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 4] = new Vector3(x, y + triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 6) + 5] = new Vector3(x, y - triangleSize, z);
          break;
        case PointShapes.PYRAMID:
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 0] = new Vector3(x - triangleSize, y - triangleSize, z - triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 1] = new Vector3(x + triangleSize, y - triangleSize, z - triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 2] = new Vector3(x, y, z + SQRT_2 * triangleSize);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 3] = new Vector3(x, y + SQRT_2 * triangleSize, z);
          break;
        case PointShapes.SQUARE:
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 0] = new Vector3(x + triangleSize, y + triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 1] = new Vector3(x - triangleSize, y + triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 2] = new Vector3(x + triangleSize, y - triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 4) + 3] = new Vector3(x - triangleSize, y - triangleSize, z);
          break;
        case PointShapes.TRIANGLE:
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 3) + 0] = new Vector3(x - triangleSize, y - triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 3) + 1] = new Vector3(x + triangleSize, y - triangleSize, z);
          theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * 3) + 2] = new Vector3(x, y - SQRT_2 * triangleSize, z);
          break;
      }
      if (include_color)
      {
        for (int i2 = 0; i2 < shape_vertex_count; i2++)
        {
          //colorBuffers[whichBuffer][counter * 8 + i2] = Color.Lerp(Color.red, Color.green, y/ (1.0F) - .7F);
          theCloud[current_cloud_buffer].colorBuffers[theCloud[current_cloud_buffer].whichBuffer][(pointNum * shape_vertex_count) + i2] = c;
        }
      }
      pointNum++;
    }
    public void subCallback(Messages.sensor_msgs.PointCloud2 msg)
    {
      //msg = recentMsg;
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      //print("in callback");
      if (!theCloud[current_cloud_buffer].isStarted)
      {
        print("theCloud is not started");
        return;
      }
      Dictionary<string, uint> fieldOffset = new Dictionary<string, uint>();
      for (int i = 0; i < msg.fields.Length; i++)
      {
        fieldOffset.Add(msg.fields[i].name, msg.fields[i].offset);
        //print(msg.fields[i].name + " " + msg.fields[i].offset + " " + msg.fields[i].datatype);
      }
      uint offset_x = fieldOffset["x"];
      uint offset_y = fieldOffset["y"];
      uint offset_z = fieldOffset["z"];
      uint offset_rgb = fieldOffset["rgb"];
      int shape_vertex_count = vertexCount[pointShape];
      int numPoints = (msg.data.Length / ((int)msg.point_step)) + 1;
      //print(numPoints);
      bool shouldIncreaseTriangles = theCloud[current_cloud_buffer].numPoints_in_triangle_buffer < numPoints;
      if (shouldIncreaseTriangles || hard_recalculate_triangles)
      {
        theCloud[current_cloud_buffer].recalculateTriangles(numPoints, indices[pointShape], vertexCount[pointShape]);
        hard_recalculate_triangles = false;
      }
      stopWatch.Stop();
      //print("Triangle Calculation Run Time " + stopWatch.ElapsedMilliseconds);
      stopWatch.Reset();
      stopWatch.Start();
      //print("---------------------------------------------------------------------------------------------------------------");
      //print(vertexAccumulation.Length);
      theCloud[current_cloud_buffer].vertexBuffers[theCloud[current_cloud_buffer].whichBuffer] = new Vector3[theCloud[current_cloud_buffer].numPoints_in_triangle_buffer * vertexCount[pointShape]];
      theCloud[current_cloud_buffer].colorBuffers[theCloud[current_cloud_buffer].whichBuffer] = new Color[theCloud[current_cloud_buffer].numPoints_in_triangle_buffer * vertexCount[pointShape]];
      //print("---------------------------------------------------------------------------------------------------------------");
      pointNum = 0;
      //print("---------------------------------------------------------------------------------------------------------------");
      for (int i = 0; i < msg.data.Length; i += (int)msg.point_step)
      {
        float x = ToFloat(msg.data[i + offset_x + 0], msg.data[i + offset_x + 1], msg.data[i + offset_x + 2], msg.data[i + offset_x + 3]);
        float y = -1 * ToFloat(msg.data[i + offset_y + 0], msg.data[i + offset_y + 1], msg.data[i + offset_y + 2], msg.data[i + offset_y + 3]);// + 1.2F;
        float z = ToFloat(msg.data[i + offset_z + 0], msg.data[i + offset_z + 1], msg.data[i + offset_z + 2], msg.data[i + offset_z + 3]);
        float b = ToInt(0, 0, 0, msg.data[i + offset_rgb + 0]) * 0.00392156862F;
        float g = ToInt(0, 0, 0, msg.data[i + offset_rgb + 1]) * 0.00392156862F;
        float r = ToInt(0, 0, 0, msg.data[i + offset_rgb + 2]) * 0.00392156862F;
        Color c = new Color(r, g, b, 1);
        addPoint(x, y, z, c);
      }
      //print("---------------------------------------------------------------------------------------------------------------");
      theCloud[current_cloud_buffer].whichBuffer = (theCloud[current_cloud_buffer].whichBuffer + 1) % 2;
      theCloud[current_cloud_buffer].updateVertexData = true;
      if (include_color)
      {
        theCloud[current_cloud_buffer].updateColorData = true;
      }
      if (shouldIncreaseTriangles)
      {
        theCloud[current_cloud_buffer].updateTriangleData = true;
      }
      current_cloud_buffer++;
      if (current_cloud_buffer >= theCloud.Length)
      {
        current_cloud_buffer = 0;
      }
      stopWatch.Stop();
      //print("Vertex Calculation Run Time " + stopWatch.ElapsedMilliseconds);
      //print("------------------------------------------------------------------finished callback" + pointNum);
    }
    void testCB(Messages.std_msgs.String str)
    {
      print(str.data);
    }
    // Use this for initialization
    void Start()
    {
      print("Starting...");

      TfTreeManager.Instance.AddListener(vis =>
      {
        //Debug.LogWarning("LaserMeshView has a tfvisualizer now!");
        tfvisualizer = vis;
      });
      if (is_static)
      {
        theCloud = new mesh_pcloud[1];
        GameObject theCloudObject = new GameObject("pointCloudContainer");
        theCloudObject.transform.parent = gameObject.transform;
        theCloudObject.AddComponent<mesh_pcloud>();
        theCloud[0] = theCloudObject.GetComponent<mesh_pcloud>();
        registerMeshPclouds();
        print("Starting to load static cloud from " + Mesh_File.name);
        string[] pointLocations = Mesh_File.text.Split('\n');
        string Version = pointLocations[1].Split(' ')[1];
        string[] Fields = pointLocations[2].Split(' ');
        string[] Sizes = pointLocations[3].Split(' ');
        string[] Type = pointLocations[4].Split(' ');
        string[] Count = pointLocations[5].Split(' ');
        int Width = Int32.Parse(pointLocations[6].Split(' ')[1]);
        int Height = Int32.Parse(pointLocations[7].Split(' ')[1]);
        string[] Viewpoint = pointLocations[8].Split(' ');
        int Points = Int32.Parse(pointLocations[9].Split(' ')[1]);
        string Data = pointLocations[10].Split(' ')[1];
        if (Data != "ascii")
        {
          print("Only ascii pcd format is supported...");
          print(Data);
          return;
        }
        //theCloud.vertexBuffers[theCloud.whichBuffer] = new Vector3[10 * vertexCount[pointShape]];//[(pointLocations.Length / 6) * vertexCount[pointShape]];
        //theCloud.colorBuffers[theCloud.whichBuffer] = new Color[10 * vertexCount[pointShape]];//[(pointLocations.Length / 6) * vertexCount[pointShape]];
        //theCloud.recalculateTriangles(10, indices[pointShape], vertexCount[pointShape]);

        theCloud[0].vertexBuffers[theCloud[0].whichBuffer] = new Vector3[Points * vertexCount[pointShape]];
        theCloud[0].colorBuffers[theCloud[0].whichBuffer] = new Color[Points * vertexCount[pointShape]];
        theCloud[0].recalculateTriangles(Points, indices[pointShape], vertexCount[pointShape]);
        for (int i = 11; i < Points + 11; i++)
        {
          try
          {
            string[] thisPoint = pointLocations[i].Split(' ');
            float x = float.Parse(thisPoint[0]);
            float y = float.Parse(thisPoint[1]);
            float z = float.Parse(thisPoint[2]);
            Color c;
            if (thisPoint[3][0] != 'n')
            {
              byte[] color = System.BitConverter.GetBytes(float.Parse(thisPoint[3]));
              c = new Color(color[2] * 0.00392156862F, color[1] * 0.00392156862F, color[0] * 0.00392156862F, 1);
            }
            else
            {
              c = new Color(0, 0, 0, 1);
            }
            addPoint(x, y, z, c);
          }
          catch (Exception e)
          {
            print(e);
            print(pointLocations[i]);
          }
        }
        print("number of points loaded: " + pointNum);
        theCloud[0].whichBuffer = (theCloud[0].whichBuffer + 1) % 2;
        theCloud[0].updateVertexData = true;
        theCloud[0].updateColorData = true;
        theCloud[0].updateTriangleData = true;
      }
      else
      {
        ROS.ROS_HOSTNAME = "10.185.0.204";
        ROS.ROS_MASTER_URI = "http://10.185.0.30:11311";
        ROS.Init(new String[0], "mesh_pcloud_renderer");
        nh = new NodeHandle();
        //nh= rosmaster.getNodeHandle();
        print("Subscribing");
        theCloud = new mesh_pcloud[accumulate_data && max_acc_frames > 0 ? max_acc_frames : 1];
        for (int i = 0; i < theCloud.Length; i++)
        {
          GameObject theCloudObject = new GameObject("pointCloudContainer" + i);
          theCloudObject.transform.parent = gameObject.transform;
          theCloudObject.AddComponent<mesh_pcloud>();
          theCloud[i] = theCloudObject.GetComponent<mesh_pcloud>();
        }
        sub = nh.subscribe<Messages.sensor_msgs.PointCloud2>(topic_name, 2, subCallback);
      }
      /*unity_tf_msg = new Messages.geometry_msgs.PolygonStamped();
      unity_tf_msg.header = new Messages.std_msgs.Header();
      unity_tf_msg.polygon = new Messages.geometry_msgs.Polygon();
      unity_tf_msg.header.frame_id = (is_static ? Mesh_File.name : topic_name);
      unity_tf_msg.polygon.points = new Messages.geometry_msgs.Point32[3];
      unity_tf_pub = nh.advertise<Messages.geometry_msgs.PolygonStamped>("/unity_tf", 10);
      ThreadStart pub_thread_start = new ThreadStart(UnityTFPublisher);
      pub_thread = new Thread(pub_thread_start);
      pub_thread.Start();*/
      print("Started");
      //theCloud.Initialize();
    }
    private void Update()
    {
      transform.Rotate(Time.deltaTime * 10 * rot_x, Time.deltaTime * 10 * rot_y, Time.deltaTime * 10 *rot_z);
      emTransform emt = new emTransform(TF);
      for (int i = 0; i < theCloud.Length; i++)
      {
        transform.SetPositionAndRotation(new Vector3(((Vector3)emt.UnityPosition).x, ((Vector3)emt.UnityPosition).y, ((Vector3)emt.UnityPosition).z), (Quaternion)emt.UnityRotation);
        if (theCloud[i].isStarted)
        {
          theCloud[i].UpdateMesh();
        }
        else
        {
          print("Cloud not yet started");
        }
      }
      /*
      unity_tf_msg.polygon.points[0].x = transform.position.x;
      unity_tf_msg.polygon.points[0].y = transform.position.y;
      unity_tf_msg.polygon.points[0].z = transform.position.z;
      unity_tf_msg.polygon.points[1].x = transform.rotation.eulerAngles.x;
      unity_tf_msg.polygon.points[1].y = transform.rotation.eulerAngles.y;
      unity_tf_msg.polygon.points[1].z = transform.rotation.eulerAngles.z;
      unity_tf_msg.polygon.points[2].x = transform.lossyScale.x;
      unity_tf_msg.polygon.points[2].y = transform.lossyScale.y;
      unity_tf_msg.polygon.points[2].z = transform.lossyScale.z;*/
    }
    void OnApplicationQuit()
    {
      ROS.shutdown();
      ROS.waitForShutdown();
      pub_thread.Abort();
    }
    // Update is called once per frame
    void OnPostRender()
    {

    }
  }
}