// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using System;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModCombinedSpeed : Mod, IApplicableToDrawableRuleset<TaikoHitObject>
    {
        public override string Name => "Hybrid SV";
        public override string Acronym => "HS";
        public override double ScoreMultiplier => 0.9;
        public override Type[] IncompatibleMods => new[] { typeof(TaikoModConstantSpeed) };
        public override LocalisableString Description => "Combine Sequential and Overlapping scrolling algorithms for more tricky speed changes!";
        public override IconUsage? Icon => FontAwesome.Solid.YinYang;
        public override ModType Type => ModType.Conversion;

        public void ApplyToDrawableRuleset(DrawableRuleset<TaikoHitObject> drawableRuleset)
        {
            var taikoRuleset = (DrawableTaikoRuleset)drawableRuleset;
            taikoRuleset.VisualisationMethod = ScrollVisualisationMethod.Combined;
        }
    }
}
