// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;

namespace Holofunk.Sound
{
    /// <summary>
    /// Read-only distributed object that simply provides state about a sound effect.
    /// </summary>
    /// <remarks>
    /// There are multiple plugins, each of which may have multiple programs. Effects are applied
    /// to tracks by specifying both plugin ID and program ID -- the program ID is only unique for
    /// a given plugin. e.g. two different effects from two different plugins may have the same
    /// PluginProgramId, and this is considered normal; they do not represent the same effect.
    /// </remarks>
    public interface IDistributedSoundEffect : IDistributedInterface
    {
        /// <summary>
        /// The ID of the plugin for this effect.
        /// </summary>
        public PluginId PluginId { get; }

        /// <summary>
        /// The name of the plugin implementing this effect.
        /// </summary>
        public string PluginName { get; }

        /// <summary>
        /// The program ID within this plugin for this effect.
        /// </summary>
        public PluginProgramId PluginProgramId { get; }

        /// <summary>
        /// The name of the program implementing this effect.
        /// </summary>
        public string ProgramName { get; }
    }
}
