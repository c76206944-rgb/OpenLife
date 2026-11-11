using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    /// <summary>
    /// A custom agent type (smaller than the default humanoid) so Sims fit through 1 m doorways.
    /// The same settings are used both to build the navmesh and by every Sim's NavMeshAgent.
    /// </summary>
    public static class NavSetup
    {
        static bool made;
        static NavMeshBuildSettings settings;

        static void Ensure()
        {
            if (made) return;
            settings = NavMesh.CreateSettings();
            settings.agentRadius = 0.25f;
            settings.agentHeight = 1.8f;
            settings.agentClimb = 0.3f;
            settings.agentSlope = 40f;
            settings.minRegionArea = 0.1f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.08f;
            made = true;
        }

        public static NavMeshBuildSettings Settings
        {
            get { Ensure(); return settings; }
        }

        public static int AgentTypeId
        {
            get { Ensure(); return settings.agentTypeID; }
        }
    }
}
