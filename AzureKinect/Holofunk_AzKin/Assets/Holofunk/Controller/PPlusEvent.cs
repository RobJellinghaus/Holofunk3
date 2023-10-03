/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Hand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holofunk.Controller
{
    /// <summary>A Joycon event in a state machine.</summary>
    public struct PPlusEvent
    {
        /// <summary>
        /// Which button?
        /// </summary>
        readonly PPlus.Button _button;
        /// <summary>
        /// Is the button down?
        /// </summary>
        readonly bool _isDown;
        /// <summary>
        /// Has this event been captured by another UI layer?
        /// </summary>
        readonly bool _isCaptured;

        /// <summary>The button that was down or up.</summary>
        internal PPlus.Button Button => _button;

        /// <summary>
        /// Is the button down? (if not, it's up)
        /// </summary>
        internal bool IsDown => _isDown;

        /// <summary>
        /// Has this event already been captured?
        /// </summary>
        /// <remarks>
        /// We may still want to respond to already-captured events in some way (e.g. by changing a hand sprite),
        /// but we want to be able to detect that the capture already took place, and modify the transition
        /// appropriately.
        /// </remarks>
        internal bool IsCaptured => _isCaptured;

        internal PPlusEvent(PPlus.Button button, bool isDown) { _button = button; _isDown = isDown; _isCaptured = false; }

        /// <summary>
        /// Construct a captured PPlusEvent from an existing one.
        /// </summary>
        internal PPlusEvent AsCaptured() { return new PPlusEvent(this); }

        private PPlusEvent(PPlusEvent other) { _button = other._button; _isDown = other._isDown; _isCaptured = true; }

        public static bool operator ==(PPlusEvent l, PPlusEvent r)
        {
            return l.Button == r.Button && l.IsDown == r.IsDown && l.IsCaptured == r.IsCaptured;
        }

        public static bool operator !=(PPlusEvent l, PPlusEvent r)
        {
            return !(l == r);
        }

        public override string ToString()
        {
            return Button.ToString() + (_isDown ? "(down)" : "(up)") + (_isCaptured ? "(captured)" : "");
        }

        public override bool Equals(object obj)
        {
            return obj is PPlusEvent @event && (this == @event);
        }

        public override int GetHashCode()
        {
            int hashCode = 1793886979;
            hashCode = hashCode * -1521134295 + _button.GetHashCode();
            hashCode = hashCode * -1521134295 + _isDown.GetHashCode();
            hashCode = hashCode * -1521134295 + _isCaptured.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Trigger is pressed.
        /// </summary>
        public static readonly PPlusEvent MikeDown = new PPlusEvent(PPlus.Button.MIKE, isDown: true);
        /// <summary>
        /// Trigger is released.
        /// </summary>
        public static readonly PPlusEvent MikeUp = new PPlusEvent(PPlus.Button.MIKE, isDown: false);
        /// <summary>
        /// Shoulder is pressed.
        /// </summary>
        public static readonly PPlusEvent LeftDown = new PPlusEvent(PPlus.Button.LEFT, isDown: true);
        /// <summary>
        /// Shoulder is released.
        /// </summary>
        public static readonly PPlusEvent LeftUp = new PPlusEvent(PPlus.Button.LEFT, isDown: false);
        /// <summary>
        /// D-pad up is pressed.
        /// </summary>
        public static readonly PPlusEvent RightDown = new PPlusEvent(PPlus.Button.RIGHT, isDown: true);
        /// <summary>
        /// D-pad up is released.
        /// </summary>
        public static readonly PPlusEvent RightUp = new PPlusEvent(PPlus.Button.RIGHT, isDown: false);
        /// <summary>
        /// D-pad left is pressed.
        /// </summary>
        public static readonly PPlusEvent LightDown = new PPlusEvent(PPlus.Button.LIGHT, isDown: true);
        /// <summary>
        /// D-pad left is released.
        /// </summary>
        public static readonly PPlusEvent LightUp = new PPlusEvent(PPlus.Button.LIGHT, isDown: false);
        /// <summary>
        /// D-pad right is pressed.
        /// </summary>
        public static readonly PPlusEvent TeamsDown = new PPlusEvent(PPlus.Button.TEAMS, isDown: true);
        /// <summary>
        /// D-pad right is released.
        /// </summary>
        public static readonly PPlusEvent TeamsUp = new PPlusEvent(PPlus.Button.TEAMS, isDown: false);
    }

    public class PPlusEventComparer : IComparer<PPlusEvent>
    {
        internal static readonly PPlusEventComparer Instance = new PPlusEventComparer();

        public int Compare(PPlusEvent x, PPlusEvent y)
        {
            int delta = (int)x.Button - (int)y.Button;
            if (delta != 0) return delta;
            if (x.IsDown != y.IsDown)
            {
                return x.IsDown ? -1 : 1;
            }
            if (x.IsCaptured != y.IsCaptured)
            {
                return x.IsCaptured ? 1 : -1;
            }
            return 0;
        }
    }
}
