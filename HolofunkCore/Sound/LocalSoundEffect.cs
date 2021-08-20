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

        internal void Initialize(int pluginId, string pluginName, int pluginProgramId, string programName)
        {
            PluginId = pluginId;
            PluginName = pluginName;
            PluginProgramId = pluginProgramId;
            ProgramName = programName;
        }

        #region ILocalSoundEffect

        public int PluginId { get; private set; }

        public string PluginName { get; private set; }

        public int PluginProgramId { get; private set; }

        public string ProgramName { get; private set; }

        public void OnDelete()
        {
            // Nothing to do
        }

        #endregion   
    }
}
