using UnityEngine;
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
    mesh_pcloud theCloud;
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

    Publisher<Messages.std_msgs.String> pub;
    Subscriber<Messages.sensor_msgs.PointCloud2> sub;
    NodeHandle nh;
    Messages.sensor_msgs.PointCloud2 msg;
    private bool closing;
    private Thread pubthread;
    public PointShapes pointShape = PointShapes.CUBE;
    public static Material lineMaterial;

    public string TFName;
    //public int home_x=0,home_y=0,home_z=0,count=0,adj=0,whichBuffer=0;
    public System.Collections.ArrayList points = new ArrayList();
    public bool spread_updates = false, hard_recalculate_triangles = false, include_color = true, accumulate_data = false;
    bool updateVertexData = false, updateTriangleData = false, updateColorData = false;
    public int HD_CLOUD_SIZE = 414720;
    int pointNum = 0;
    public string topic_name = "/kinect2_old/hd/points";
    float SQRT_2 = (float)Math.Sqrt(2);
    bool didThis = false;
    private TfVisualizer tfvisualizer;
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
    public void subCallback(Messages.sensor_msgs.PointCloud2 msg)
    {
      //if (didThis){ return;}
      //didThis = true;
      //msg = recentMsg;
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      print("in callback");
      if (!theCloud.isStarted)
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
      print(numPoints);
      bool shouldIncreaseTriangles = theCloud.numPoints_in_triangle_buffer < numPoints;
      if (shouldIncreaseTriangles || hard_recalculate_triangles)
      {
        theCloud.recalculateTriangles(numPoints, indices[pointShape], vertexCount[pointShape]);
        hard_recalculate_triangles = false;
      }
      stopWatch.Stop();
      print("Triangle Calculation Run Time " + stopWatch.ElapsedMilliseconds);
      stopWatch.Reset();
      stopWatch.Start();
      //print("---------------------------------------------------------------------------------------------------------------");
      //print(vertexAccumulation.Length);
      theCloud.vertexBuffers[theCloud.whichBuffer] = new Vector3[theCloud.numPoints_in_triangle_buffer * vertexCount[pointShape]];
      theCloud.colorBuffers[theCloud.whichBuffer] = new Color[theCloud.numPoints_in_triangle_buffer * vertexCount[pointShape]];
      //print("---------------------------------------------------------------------------------------------------------------");
      if (!accumulate_data)
      {
        pointNum = 0;
      }

      //print("---------------------------------------------------------------------------------------------------------------");
      for (int i = 0; i < msg.data.Length; i += (int)msg.point_step)
      {
        float x = ToFloat(msg.data[i + offset_x + 0], msg.data[i + offset_x + 1], msg.data[i + offset_x + 2], msg.data[i + offset_x + 3]);
        float y = -1 * ToFloat(msg.data[i + offset_y + 0], msg.data[i + offset_y + 1], msg.data[i + offset_y + 2], msg.data[i + offset_y + 3]);// + 1.2F;
        float z = ToFloat(msg.data[i + offset_z + 0], msg.data[i + offset_z + 1], msg.data[i + offset_z + 2], msg.data[i + offset_z + 3]);
        float b = ToInt(0, 0, 0, msg.data[i + offset_rgb + 0]) * 0.00392156862F;
        float g = ToInt(0, 0, 0, msg.data[i + offset_rgb + 1]) * 0.00392156862F;
        float r = ToInt(0, 0, 0, msg.data[i + offset_rgb + 2]) * 0.00392156862F;
        Color thisVertex = new Color(r, g, b, 1);
        float triangleSize = (z / 500F);
        switch (pointShape)
        {
          case PointShapes.CUBE:
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 0] = new Vector3(x, y, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 1] = new Vector3(x + triangleSize, y, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 2] = new Vector3(x + triangleSize, y + triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 3] = new Vector3(x, y + triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 4] = new Vector3(x, y, z + triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 5] = new Vector3(x + triangleSize, y, z + triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 6] = new Vector3(x + triangleSize, y + triangleSize, z + triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 8) + 7] = new Vector3(x, y + triangleSize, z + triangleSize);
            break;
          case PointShapes.OCTAHEDRON:
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 0] = new Vector3(x, y, z + triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 1] = new Vector3(x + triangleSize, y, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 2] = new Vector3(x, y, z - triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 3] = new Vector3(x - triangleSize, y, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 4] = new Vector3(x, y + triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 6) + 5] = new Vector3(x, y - triangleSize, z);
            break;
          case PointShapes.PYRAMID:
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 0] = new Vector3(x - triangleSize, y - triangleSize, z - triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 1] = new Vector3(x + triangleSize, y - triangleSize, z - triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 2] = new Vector3(x, y, z + SQRT_2 * triangleSize);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 3] = new Vector3(x, y + SQRT_2 * triangleSize, z);
            break;
          case PointShapes.SQUARE:
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 0] = new Vector3(x + triangleSize, y + triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 1] = new Vector3(x - triangleSize, y + triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 2] = new Vector3(x + triangleSize, y - triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 4) + 3] = new Vector3(x - triangleSize, y - triangleSize, z);
            break;
          case PointShapes.TRIANGLE:
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 3) + 0] = new Vector3(x - triangleSize, y - triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 3) + 1] = new Vector3(x + triangleSize, y - triangleSize, z);
            theCloud.vertexBuffers[theCloud.whichBuffer][(pointNum * 3) + 2] = new Vector3(x, y - SQRT_2 * triangleSize, z);
            break;
        }
        if (include_color)
        {
          for (int i2 = 0; i2 < shape_vertex_count; i2++)
          {
            //colorBuffers[whichBuffer][counter * 8 + i2] = Color.Lerp(Color.red, Color.green, y/ (1.0F) - .7F);
            theCloud.colorBuffers[theCloud.whichBuffer][(pointNum * shape_vertex_count) + i2] = thisVertex;
          }
        }
        pointNum++;
      }
      //print("---------------------------------------------------------------------------------------------------------------");
      theCloud.whichBuffer = (theCloud.whichBuffer + 1) % 2;
      theCloud.updateVertexData = true;
      if (include_color)
      {
        theCloud.updateColorData = true;
      }
      if (shouldIncreaseTriangles)
      {
        theCloud.updateTriangleData = true;
      }
      stopWatch.Stop();
      print("Vertex Calculation Run Time " + stopWatch.ElapsedMilliseconds);
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
      ROS.ROS_HOSTNAME = "10.185.0.197";
      ROS.ROS_MASTER_URI = "http://link:11311";
      ROS.Init(new String[0], "mesh_pcloud_renderer");
      nh = new NodeHandle();
      //nh= rosmaster.getNodeHandle();
      print("Subscribing");
      TfTreeManager.Instance.AddListener(vis =>
      {
        //Debug.LogWarning("LaserMeshView has a tfvisualizer now!");
        tfvisualizer = vis;
      });
      GameObject theCloudObject = new GameObject("theCloud");
      theCloudObject.AddComponent<mesh_pcloud>();
      theCloud = theCloudObject.GetComponent<mesh_pcloud>();
      sub = nh.subscribe<Messages.sensor_msgs.PointCloud2>(topic_name, 2, subCallback);
      Subscriber<Messages.std_msgs.String> sub2 = nh.subscribe<Messages.std_msgs.String>("test_topic", 20, testCB);
      print("Started");
      //theCloud.Initialize();
    }
    private void Update()
    {
      emTransform emt = new emTransform(TF);
      theCloud.position_x = ((Vector3)emt.UnityPosition).x;
      theCloud.position_y = ((Vector3)emt.UnityPosition).y;
      theCloud.position_z = ((Vector3)emt.UnityPosition).z;
      theCloud.rotation = (Quaternion)emt.UnityRotation;
      if (theCloud.isStarted)
      {
        theCloud.UpdateMesh();
      }
      else
      {
        print("Cloud not yet started");
      }
    }
    void OnApplicationQuit()
    {
      ROS.shutdown();
      ROS.waitForShutdown();
    }
    // Update is called once per frame
    void OnPostRender()
    {

    }
  }
}