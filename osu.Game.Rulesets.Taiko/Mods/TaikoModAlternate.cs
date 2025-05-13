// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using System;
using System.Linq;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Framework.Bindables;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public partial class TaikoModAlternate : TaikoInputBlockingMod
    {
        public override string Name => @"Alternate";
        public override string Acronym => @"AL";
        public override LocalisableString Description => @"Don't use the same side twice in a row!";
        public override IconUsage? Icon => FontAwesome.Solid.Keyboard;
        public override double ScoreMultiplier => 1.0;
        public override Type[] IncompatibleMods => [.. base.IncompatibleMods, .. new[] { typeof(TaikoModSingleTap) }];
        public override ModType Type => ModType.Conversion;

        [SettingSource("DDKK/KKDD support", "Adjusts the mod to behave correctly when playing with a color for each side.")]
        public BindableBool BongoSupport { get; } = new BindableBool();
        private bool? lastActionWasRight = null;

        protected override bool CheckValidNewAction(TaikoAction action)
        {
            return BongoSupport.Value ? ddkkCheck(action) : kddkCheck(action);
        }

        private bool kddkCheck(TaikoAction action)
        {
            var nextHitObject = Playfield.HitObjectContainer.AliveObjects.FirstOrDefault(h => h.Result?.HasResult != true)?.HitObject;
            var previousHitObject = Playfield.HitObjectContainer.AliveObjects.LastOrDefault(h => h.Result?.HasResult == true)?.HitObject;

            // Allow all actions if the next or previous hit object is a strong hit (excluding strong drumrolls).
            if ((nextHitObject is TaikoStrongableHitObject { IsStrong: true } && nextHitObject is not DrumRoll) ||
                (previousHitObject is TaikoStrongableHitObject { IsStrong: true } && previousHitObject is not DrumRoll))
                return true;

            bool sideRight = action is TaikoAction.RightRim or TaikoAction.RightCentre;

            // Force alternating by disallowing the same side twice in a row
            if (lastActionWasRight == sideRight) return false;

            lastActionWasRight = sideRight;
            return true;
        }

        private bool? lastDSideRight = null;
        private bool? lastKSideRight = null;
        private bool ddkkCheck(TaikoAction action)
        {
            var nextHitObject = Playfield.HitObjectContainer.AliveObjects.FirstOrDefault(h => h.Result?.HasResult != true)?.HitObject;
            var previousHitObject = Playfield.HitObjectContainer.AliveObjects.LastOrDefault(h => h.Result?.HasResult == true)?.HitObject;

            // Allow all actions if the next or previous hit object is a strong hit (excluding strong drumrolls).
            if ((nextHitObject is TaikoStrongableHitObject { IsStrong: true } && nextHitObject is not DrumRoll) ||
                (previousHitObject is TaikoStrongableHitObject { IsStrong: true } && previousHitObject is not DrumRoll))
                return true;

            bool sideRight = action is TaikoAction.RightRim or TaikoAction.RightCentre;
            bool colorIsD = action is TaikoAction.LeftCentre or TaikoAction.RightCentre;
            bool sameSideHit = colorIsD ? (lastDSideRight == sideRight) : (lastKSideRight == sideRight);
            // Force alternating by disallowing the same side twice in a row
            if (sameSideHit) return false;

            if (colorIsD)
                lastDSideRight = sideRight;
            else
                lastKSideRight = sideRight;
            return true;
        }
    }
}
