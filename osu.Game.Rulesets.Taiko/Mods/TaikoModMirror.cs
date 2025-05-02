// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModMirror : Mod, IApplicableToDrawableRuleset<TaikoHitObject>
    {
        public override string Name => "Mirror";
        public override string Acronym => "MR";
        public override LocalisableString Description => "Notes arrive from the left instead of the right.";
        public override double ScoreMultiplier => 1;

        public void ApplyToDrawableRuleset(DrawableRuleset<TaikoHitObject> drawableRuleset)
        {
            drawableRuleset.Scale = new Vector2(-1, 1);
            drawableRuleset.Origin = Framework.Graphics.Anchor.TopRight;
        }
    }
}
