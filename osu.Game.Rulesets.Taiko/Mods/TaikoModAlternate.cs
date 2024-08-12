// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Game.Utils;
using osu.Game.Rulesets.Taiko.UI;
using osu.Framework.Graphics.Sprites;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public partial class TaikoModAlternate : Mod, IApplicableToDrawableRuleset<TaikoHitObject>, IUpdatableByPlayfield
    {
        public override string Name => @"Alternate";
        public override string Acronym => @"AL";
        public override LocalisableString Description => @"Don't hit the same side twice in a row!";

        public override IconUsage? Icon => FontAwesome.Solid.Keyboard;

        public override double ScoreMultiplier => 1.0;
        public override Type[] IncompatibleMods => new[] { typeof(ModAutoplay), typeof(ModRelax), typeof(TaikoModCinema), typeof(TaikoModSingleTap) };
        public override ModType Type => ModType.Conversion;

        private DrawableTaikoRuleset ruleset = null!;

        private TaikoPlayfield playfield { get; set; } = null!;
        private TaikoAction? lastAcceptedAction { get; set; }

        /// <summary>
        /// A tracker for periods where single tap should not be enforced (i.e. non-gameplay periods).
        /// </summary>
        /// <remarks>
        /// This is different from <see cref="Player.IsBreakTime"/> in that the periods here end strictly at the first object after the break, rather than the break's end time.
        /// </remarks>
        private PeriodTracker nonGameplayPeriods = null!;

        private IFrameStableClock gameplayClock = null!;

        public void ApplyToDrawableRuleset(DrawableRuleset<TaikoHitObject> drawableRuleset)
        {
            ruleset = (DrawableTaikoRuleset)drawableRuleset;
            ruleset.KeyBindingInputManager.Add(new InputInterceptor(this));
            playfield = (TaikoPlayfield)ruleset.Playfield;

            var periods = new List<Period>();

            if (drawableRuleset.Objects.Any())
            {
                periods.Add(new Period(int.MinValue, getValidJudgementTime(ruleset.Objects.First()) - 1));

                foreach (BreakPeriod b in drawableRuleset.Beatmap.Breaks)
                    periods.Add(new Period(b.StartTime, getValidJudgementTime(ruleset.Objects.First(h => h.StartTime >= b.EndTime)) - 1));

                static double getValidJudgementTime(HitObject hitObject) => hitObject.StartTime - hitObject.HitWindows.WindowFor(HitResult.Meh);
            }

            nonGameplayPeriods = new PeriodTracker(periods);

            gameplayClock = drawableRuleset.FrameStableClock;
        }

        public void Update(Playfield playfield)
        {
            if (!nonGameplayPeriods.IsInAny(gameplayClock.CurrentTime)) return;

            lastAcceptedAction = null;
        }

        private bool checkCorrectAction(TaikoAction action)
        {
            if (nonGameplayPeriods.IsInAny(gameplayClock.CurrentTime))
                return true;

            // If next hit object is strong, allow usage of all actions. Strong drumrolls are ignored in this check.
            if (playfield.HitObjectContainer.AliveObjects.FirstOrDefault(h => h.Result?.HasResult != true)?.HitObject is TaikoStrongableHitObject hitObject
                && hitObject.IsStrong
                && hitObject is not DrumRoll)
            {
                lastAcceptedAction = null;
                return true;
            }

            // Always pass as true if no action has been inputted.
            if (lastAcceptedAction == null)
            {
                lastAcceptedAction = action;
                return true;
            }

            // Determine if the new action is on the opposite side of the last accepted action.
            bool isOppositeSide =
                (lastAcceptedAction == TaikoAction.LeftCentre || lastAcceptedAction == TaikoAction.LeftRim)
                ? (action == TaikoAction.RightCentre || action == TaikoAction.RightRim)
                : (action == TaikoAction.LeftCentre || action == TaikoAction.LeftRim);

            if (isOppositeSide)
            {
                lastAcceptedAction = action;
                return true;
            }

            return false;
        }

        private partial class InputInterceptor : Component, IKeyBindingHandler<TaikoAction>
        {
            private readonly TaikoModAlternate mod;

            public InputInterceptor(TaikoModAlternate mod)
            {
                this.mod = mod;
            }

            public bool OnPressed(KeyBindingPressEvent<TaikoAction> e)
                // if the pressed action is incorrect, block it from reaching gameplay.
                => !mod.checkCorrectAction(e.Action);

            public void OnReleased(KeyBindingReleaseEvent<TaikoAction> e)
            {
            }
        }
    }
}
