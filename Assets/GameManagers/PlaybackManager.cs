using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{
    public static PlaybackManager Instance;

    //TODO: track time into playback
    public int MillisecondsInChart;
    public float BeatsInChart => SaveManager.Instance.Chart.TimeToBeat(MillisecondsInChart);
    public int CurrentTimestamp => 0;

    public uint ViewSize = 5000; // Number of milliseconds the view contains

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

    // Converts track position to a vector2 between 0,0 and 1,1
    public Vector2 TrackToUIPosition(int position, int time)
    {
        return new Vector2(position / 100f, (time - MillisecondsInChart) / (float)ViewSize);
    }
}
