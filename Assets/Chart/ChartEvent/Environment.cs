using UnityEngine;

[CreateAssetMenu(fileName = "NewEnvironment", menuName = "ScriptableObjects/Environment", order = 0)]
public class Environment : ScriptableObject
{
    [SerializeField] new string name;
    public string Name => name;

    [SerializeField] string displayName;
    public string DisplayName => displayName;

    [SerializeField] Color color = Color.white;
    public Color Color => color;
}
