﻿using UnityEngine;
using System.Collections;

public class FrameRateDisplay : MonoBehaviour {

    int m_frameCounter = 0;
    float m_timeCounter = 0.0f;
    float m_lastFramerate = 0.0f;
    public float m_refreshTime = 1.0f;


    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
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
        }
    }
}
