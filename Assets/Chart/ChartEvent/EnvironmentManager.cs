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
    public static IReadOnlyDictionary<string, Environment> Environments { get; private set; }
    public static Environment DefaultEnvironment => Instance.environments[0];

    new Camera camera;

    void Awake()
    {
        Instance = this;
        
        camera = GetComponent<Camera>();

        Environments = environments.ToDictionary(env => env.Name.ToLower(), env => env);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
