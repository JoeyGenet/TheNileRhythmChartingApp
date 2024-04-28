using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [SerializeField] List<Environment> environments = new();
    public static IReadOnlyList<Environment> Environments => Instance.environments;
    public static Environment DefaultEnvironment => Instance.environments[0];

    new Camera camera;

    void Awake()
    {
        Instance = this;
        
        camera = GetComponent<Camera>();
    }

    public static Environment GetEnvironment(string name) => Environments.FirstOrDefault(env => env.Name == name);
    public static int GetEnvironmentIndex(Environment env) => Instance.environments.IndexOf(env);

    // Update is called once per frame
    void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        var environmentChange = ChartManager.SelectedChart.Events.OrderBy(e => e.StartTime).LastOrDefault(e => e.StartTime < currTime);
        if (environmentChange == null)
        {
            camera.backgroundColor = ChartManager.SelectedChart.Environment.Color; // No env change reached. use chart default environment
        }
        else
        {
            var prevColor = environmentChange.PreviousEnvironment.Color;
            var nextColor = environmentChange.Environment.Color;
            float lerpAmount = Mathf.Clamp((float) (currTime - environmentChange.StartTime) / (environmentChange.EndTime - environmentChange.StartTime), 0, 1);
            camera.backgroundColor = Color.Lerp(prevColor, nextColor, lerpAmount);
        }
    }
}
