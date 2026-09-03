using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    /// <summary>
    /// Shared data for all commands (building costs, build times, etc).
    /// </summary>
    [CreateAssetMenu(menuName = "Commands/Commands Data", fileName = "CommandsData")]
    public class CommandsData : ScriptableObject
    {
        #region Types

        public enum BuildKey
        {
            CommandCenter,
            Barracks,
            Factory,
            PowerPlant,
            Conveyor
        }

        [System.Serializable]
        public class BuildData
        {
            public BuildKey key;
            public GameObject prefab;
            public int cost;
            public float buildTime;
        }

        #endregion

        #region Data

        [Header("Buildings")]
        [SerializeField] private List<BuildData> buildData = new();

        private Dictionary<BuildKey, BuildData> _buildDataLookup;

        #endregion

        #region Public API

        /// <summary>Returns the data set for <paramref name="key"/>, or null if it wasn't set up.</summary>
        public BuildData GetBuildData(BuildKey key)
        {
            EnsureLookupBuilt();
            _buildDataLookup.TryGetValue(key, out var data);
            return data;
        }

        #endregion

        #region Private helpers

        private void EnsureLookupBuilt()
        {
            if (_buildDataLookup != null)
                return;

            _buildDataLookup = new Dictionary<BuildKey, BuildData>(buildData.Count);

            foreach (var entry in buildData)
            {
                if (!_buildDataLookup.TryAdd(entry.key, entry))
                    Debug.LogError($"[CommandsData] Duplicate build key '{entry.key}' in asset '{name}'.", this);
            }
        }

        private void OnValidate() => _buildDataLookup = null;

        #endregion
    }
}