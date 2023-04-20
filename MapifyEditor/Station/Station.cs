using System.Collections.Generic;
using UnityEngine;

namespace Mapify.Editor
{
    public class Station : MonoBehaviour
    {
        [Header("Station Info")]
        [Tooltip("The display name of the station")]
        public string stationName;
        [Tooltip("The 2-3 character ID of the station (e.g. HB for Harbor, SM for Steel Mill, etc)")]
        public string stationID;
        [Tooltip("The color of the station shown on job booklets")]
        public Color color;
        [Tooltip("The location where the player will be teleported to when fast travelling")]
        public Transform teleportLocation;

        [Header("Job Generation")]
        [Tooltip("The area where job booklets should spawn. Not required when using a vanilla station")]
        public BoxCollider bookletSpawnArea;
        [Tooltip("The rough center of the yard. Used at the reference point for generating jobs. Will use the station if unset")]
        public Transform yardCenter;
        [Tooltip("The distance, in meters, the player has to be relative to the station for job overview booklets to generate")]
        public float bookletGenerationDistance = 150;
        [Tooltip("The distance, in meters, the player has to be relative to the yard center for jobs to generate")]
        public float jobGenerationDistance = 500;
        [Tooltip("The distance, in meters, the player has to be relative to the yard center for jobs to despawn")]
        public float jobDestroyDistance = 600;
        [Range(1, 30)]
        [Tooltip("The maximum number of jobs that can be generated at once. This number may not be met, but it'll never be exceeded")]
        public int jobsCapacity = 30;
        [Tooltip("The minimum number of cars per-job")]
        public int minCarsPerJob = 3;
        [Tooltip("The maximum number of cars per-job")]
        public int maxCarsPerJob = 20;
        public int maxShuntingStorageTracks = 3;

        [Tooltip("Whether freight haul jobs will be generated")]
        public bool generateFreightHaul = true;
        [Tooltip("Whether logistical haul jobs will be generated")]
        public bool generateLogisticalHaul = true;
        [Tooltip("Whether shunting load jobs will be generated")]
        public bool generateShuntingLoad = true;
        [Tooltip("Whether shunting unload jobs will be generated")]
        public bool generateShuntingUnload = true;

        [Header("Cargo")]
        // Another workaround for Unity's excuse of a game engine
        [HideInNormalInspector]
        public int inputCargoGroupsCount;
#pragma warning disable CS0649
        [SerializeField]
        internal List<CargoSet> inputCargoGroups;
        [SerializeField]
        internal List<CargoSet> outputCargoGroups;
#pragma warning restore CS0649

        [HideInInspector]
        public List<string> storageTrackNames;
        [HideInInspector]
        public List<string> transferInTrackNames;
        [HideInInspector]
        public List<string> transferOutTrackNames;
        [HideInInspector]
        public List<WarehouseMachine> warehouseMachines;
    }
}
