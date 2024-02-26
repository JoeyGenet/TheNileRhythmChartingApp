using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{
    public static PlaybackManager Instance;

    //TODO: track time into playback
    public float MillisecondsInChart;
    public int CurrentTimestamp => 0;

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
}
