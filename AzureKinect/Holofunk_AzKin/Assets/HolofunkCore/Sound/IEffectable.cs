using DistributedStateLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holofunk.Sound
{
    /// <summary>
    /// This is a base interface of distributed objects that can be sound-effected.
    /// </summary>
    public interface IEffectable
    {
        /// <summary>
        /// Alter the volume by the given amount.
        /// </summary>
        /// <remarks>
        /// In the case of volume, it's measured as a fraction from 0 to 1, so the alteration is just added
        /// to the current volume level.
        /// 
        /// Note that this only works on loopies, not on the performer.
        /// 
        /// As long as commit is false, the alteration is added to the current volume level and then clamped.
        /// Once commit is true, the current volume level is updated to the altered, clamped volume.
        /// </remarks>
        /// <param name="alteration">The alteration to apply, from -1 to 1.</param>
        /// <param name="commit">false as long as the alteration is temporary;
        /// true once the clamped value should be persisted.</param>
        [ReliableMethod]
        void AlterVolume(float alteration, bool commit);

        /// <summary>
        /// Alter a sound effect.
        /// </summary>
        /// <remarks>
        /// 
        /// 
        /// As long as commit is false, the alteration is added to the current volume level and then clamped.
        /// Once commit is true, the current volume level is updated to the altered, clamped volume.
        /// </remarks>
        /// <param name="effectId">The effect to alter; will be created if not already present.</param>
        /// <param name="alteration">The alteration to apply, from -1 to 1.</param>
        /// <param name="commit">false as long as the alteration is temporary;
        /// true once the clamped value should be persisted.</param>
        [ReliableMethod]
        void AlterSoundEffect(EffectId effectId, float alteration, bool commit);

        /// <summary>
        /// Pop the most recently created sound effect.
        /// </summary>
        [ReliableMethod]
        void PopSoundEffect();

        /// <summary>
        /// Clear all sound effects.
        /// </summary>
        [ReliableMethod]
        void ClearSoundEffects();
    }
}
