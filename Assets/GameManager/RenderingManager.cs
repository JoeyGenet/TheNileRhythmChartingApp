using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderingManager : MonoBehaviour
{
    public static RenderingManager Instance;

    [Header("Track Dimensions")]
	[SerializeField] private float trackWidth;
	[SerializeField] private float trackLength;
	[SerializeField] private int noteSpawnDistance; // in meters
    [SerializeField] private float noteSpeed;

    void Awake()
    {
        Instance = this;

        // GameManager.OnChartStarted += LoadChart;
        // GameManager.OnChartEnded += UnloadChart;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 TrackToWorldPosition(int position, int time)
	{
		float x = (position - 50) / 100f * trackWidth;
		float z = (time - PlaybackManager.Instance.MillisecondsInChart) * (noteSpeed/1000f); // meters = ms * (meter/ms)
		return transform.TransformPoint(new Vector3(x, 0, z));
	}

    public Vector3 LeverTrackToWorldPosition(LeftRight position, int time)
    {
        float x = -0.75f;
        if (position == LeftRight.Right)
        {
            x = 0.75f;
        }
        float z = (time - PlaybackManager.Instance.MillisecondsInChart) * (noteSpeed/1000f); // meters = ms * (meter/ms)
        return transform.TransformPoint(new Vector3(x, 0, z));
    }
}
