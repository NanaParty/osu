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

namespace osu.Game.Rulesets.Taiko.Mods
{
    public partial class TaikoModAlternate : Mod, IApplicableToDrawableRuleset<TaikoHitObject>, IUpdatableByPlayfield
    {
        public override string Name => @"Alternate";
        public override string Acronym => @"ALT";
        public override LocalisableString Description => @"Alternates the key pressed for each hit object.";

        public override double ScoreMultiplier => 1.0;
        public override Type[] IncompatibleMods => new[] { typeof(ModAutoplay), typeof(ModRelax), typeof(TaikoModCinema) };
        public override ModType Type => ModType.Conversion;

        private DrawableTaikoRuleset ruleset = null!;
        private TaikoPlayfield playfield = null!;
        private TaikoAction? lastAction { get; set; }
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

            lastAction = null;
        }

        private bool checkCorrectAction(TaikoAction action)
        {
            if (nonGameplayPeriods.IsInAny(gameplayClock.CurrentTime))
                return true;

          // If next hit object is strong, allow usage of all actions. Strong drumrolls are ignored in this check.
        if (playfield.HitObjectContainer.AliveObjects.FirstOrDefault(h => h.Result?.HasResult != true)?.HitObject is TaikoStrongableHitObject hitObject
            && hitObject.IsStrong
            && hitObject is not DrumRoll)
            return true;

        if ((action == TaikoAction.LeftCentre || action == TaikoAction.RightCentre)
            && (lastAcceptedRimAction == null || lastAcceptedRimAction != action))
        {
            lastAcceptedCentreAction = action;
            return true;
        }

        if ((action == TaikoAction.LeftRim || action == TaikoAction.RightRim)
            && (lastAcceptedCentreAction == null || lastAcceptedCentreAction != action))
        {
            lastAcceptedRimAction = action;
            return true;
        }

        return false;
    }
}
