/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Explosions Without Fireballs", "VisEntities", "1.0.0")]
    [Description("Disables the creation of fireballs when entities like minicopters and flame turrets explode.")]
    public class ExplosionsWithoutFireballs : RustPlugin
    {
        #region Fields

        private static ExplosionsWithoutFireballs _plugin;
        private static Configuration _config;
        private Dictionary<BaseEntity, string> _originalFireballGuids = new Dictionary<BaseEntity, string>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Short Prefab Names To Remove Fireballs From")]
            public List<string> ShortPrefabNamesToRemoveFireballsFrom { get; set; } = new List<string>();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ShortPrefabNamesToRemoveFireballsFrom = new List<string>
                {
                    "minicopter.entity",
                    "scraptransporthelicopter",
                    "attackhelicopter.entity",
                    "flameturret.deployed"
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            CoroutineUtil.StopAllCoroutines();
            RestoreFireballGuids();
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            CoroutineUtil.StartCoroutine("NullifyFireballsForEntitiesCoroutine", NullifyFireballsForEntitiesCoroutine());
        }

        private void OnEntitySpawned(BaseHelicopter helicopter)
        {
            if (helicopter == null)
                return;

            if (_config.ShortPrefabNamesToRemoveFireballsFrom.Contains(helicopter.ShortPrefabName))
                NullifyFireballGuidForEntity(helicopter);
        }

        private void OnEntitySpawned(FlameTurret flameTurret)
        {
            if (flameTurret == null)
                return;

            if (_config.ShortPrefabNamesToRemoveFireballsFrom.Contains(flameTurret.ShortPrefabName))
                NullifyFireballGuidForEntity(flameTurret);
        }

        #endregion Oxide Hooks

        #region Fireball Guids Nullification

        private IEnumerator NullifyFireballsForEntitiesCoroutine()
        {
            foreach (BaseHelicopter helicopter in BaseNetworkable.serverEntities.OfType<BaseHelicopter>())
            {
                if (helicopter != null)
                {
                    if (_config.ShortPrefabNamesToRemoveFireballsFrom.Contains(helicopter.ShortPrefabName))
                    {
                        NullifyFireballGuidForEntity(helicopter);
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (FlameTurret flameTurret in BaseNetworkable.serverEntities.OfType<FlameTurret>())
            {
                if (flameTurret != null)
                {
                    if (_config.ShortPrefabNamesToRemoveFireballsFrom.Contains(flameTurret.ShortPrefabName))
                    {
                        NullifyFireballGuidForEntity(flameTurret);
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        private void NullifyFireballGuidForEntity(BaseEntity entity)
        {
            if (!_originalFireballGuids.ContainsKey(entity))
            {
                if (entity is BaseHelicopter helicopter)
                {
                    _originalFireballGuids[entity] = helicopter.fireBall.guid;
                    helicopter.fireBall.guid = null;
                }
                else if (entity is FlameTurret flameTurret)
                {
                    _originalFireballGuids[entity] = flameTurret.fireballPrefab.guid;
                    flameTurret.fireballPrefab.guid = null;
                }
            }
        }

        #endregion Fireball Guids Nullification

        #region Fireball Guids Restoration

        private void RestoreFireballGuids()
        {
            foreach (var entry in _originalFireballGuids)
            {
                BaseEntity entity = entry.Key;
                string originalGUID = entry.Value;

                if (entity is BaseHelicopter helicopter)
                {
                    helicopter.fireBall.guid = originalGUID;
                }
                else if (entity is FlameTurret flameTurret)
                {
                    flameTurret.fireballPrefab.guid = originalGUID;
                }
            }
            _originalFireballGuids.Clear();
        }

        #endregion Fireball Guids Restoration

        #region Helper Classes

        public static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();

            public static void StartCoroutine(string coroutineName, IEnumerator coroutineFunction)
            {
                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
            }

            public static void StopCoroutine(string coroutineName)
            {
                if (_activeCoroutines.TryGetValue(coroutineName, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        ServerMgr.Instance.StopCoroutine(coroutine);

                    _activeCoroutines.Remove(coroutineName);
                }
            }

            public static void StopAllCoroutines()
            {
                foreach (string coroutineName in _activeCoroutines.Keys.ToArray())
                {
                    StopCoroutine(coroutineName);
                }
            }
        }

        #endregion Helper Classes
    }
}