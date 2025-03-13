using UnityEngine;

public class EpisodeManager : MonoBehaviour
{
    private CubeAgent[] agents;

    void Awake()
    {
        agents = FindObjectsOfType<CubeAgent>();
    }

    public void RequestEpisodeReset(CubeAgent requestingAgent)
    {
        ResetAllAgents(); // Reset immediately on any request
    }

    private void ResetAllAgents()
    {
        foreach (CubeAgent agent in agents)
        {
            agent.EndEpisode();
        }
    }
}
