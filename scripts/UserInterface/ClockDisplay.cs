using UnityEngine;
using System.Collections;
using Ros_CSharp;

public class ClockDisplay : MonoBehaviour
{
    public ROSCore rosmaster;
    private NodeHandle nh = null;
    private Subscriber<Messages.std_msgs.Int64> sub;

    int m_frameCounter = 0;
    float m_timeCounter = 0.0f;
    float m_lastFramerate = 0.0f;
    public float m_refreshTime = 1.0f;

    long _msg = 0;

    public void callback(Messages.std_msgs.Int64 msg)
    {
         _msg = msg.data;
    }

    // Use this for initialization
    void Start()
    {
        nh = rosmaster.getNodeHandle();
        sub = nh.subscribe<Messages.std_msgs.Int64>("/heartbeat", 0, callback, true);
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<TextMesh>().text = "Heartbeat: " + _msg;
        /*
        if (m_timeCounter < m_refreshTime)
        {
            m_timeCounter += Time.deltaTime;
            m_frameCounter++;
        }
        else
        {
            //This code will break if you set your m_refreshTime to 0, which makes no sense.
            m_lastFramerate = (float)m_frameCounter / m_timeCounter;
            GetComponent<TextMesh>().text = "FPS: " + m_lastFramerate;
            m_frameCounter = 0;
            m_timeCounter = 0.0f;
        }*/
    }
}
