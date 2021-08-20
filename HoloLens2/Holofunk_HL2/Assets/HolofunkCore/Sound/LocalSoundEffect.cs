// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib.Utils;
using NowSoundLib;
using System;
using UnityEngine;

namespace Holofunk.Sound
{
    public class LocalSoundEffect : MonoBehaviour, ILocalObject, IDistributedSoundEffect
    {
        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedSoundEffect>();

        internal void Initialize(PluginId pluginId, string pluginName, PluginProgramId pluginProgramId, string programName)
        {
            PluginId = pluginId;
            PluginName = pluginName;
            PluginProgramId = pluginProgramId;
            ProgramName = programName;
        }

        #region ILocalSoundEffect

        public PluginId PluginId { get; private set; }

        public string PluginName { get; private set; }

        public PluginProgramId PluginProgramId { get; private set; }

        public string ProgramName { get; private set; }

        public void OnDelete()
        {
            // Nothing to do
        }

        #endregion   
    }
}
