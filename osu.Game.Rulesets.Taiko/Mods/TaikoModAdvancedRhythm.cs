// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModAdvancedRhythm : Mod, IApplicableToBeatmap
    {
        public override string Name => "Advanced Rhythm";
        public override string Acronym => "AR";
        public override double ScoreMultiplier => 0.96;
        public override LocalisableString Description => "Trickify simple rhythms!";
        public override ModType Type => ModType.DifficultyIncrease;
        public override Type[] IncompatibleMods => new[] { typeof(TaikoModSimplifiedRhythm) };

        [SettingSource("1/1 to 1/2 conversion", "Converts 1/1 patterns to 1/2 rhythm.")]
        public Bindable<bool> OneWholeConversion { get; } = new BindableBool();

        [SettingSource("1/2 to 1/4 conversion", "Converts 1/2 patterns to 1/4 rhythm.")]
        public Bindable<bool> OneHalfConversion { get; } = new BindableBool(true);

        [SettingSource("1/3 to 1/6 conversion", "Converts 1/3 patterns to 1/6 rhythm.")]
        public Bindable<bool> OneThirdConversion { get; } = new BindableBool();

        [SettingSource("Invert additional note", "Set the added note to be the opposite color of the previous one.")]
        public Bindable<bool> InvertAddition { get; } = new BindableBool();

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;

            Hit[] hits = taikoBeatmap.HitObjects.Where(obj => obj is Hit).Cast<Hit>().ToArray();

            if (hits.Length == 0)
                return;

            var conversions = new List<int>();
            var toAdd = new List<Hit>();

            if (OneWholeConversion.Value) conversions.Add(1);
            if (OneHalfConversion.Value) conversions.Add(2);
            if (OneThirdConversion.Value) conversions.Add(3);

            foreach (int baseRhythm in conversions)
            {
                for (int i = 1; i < hits.Length; i++)
                {
                    Hit currentNote = hits[i - 1];
                    TaikoHitObject taikoHitObject = currentNote;
                    Hit nextNote = hits[i];
                    double snapValue = Math.Round(getSnapBetweenNotes(controlPointInfo, currentNote, nextNote) * 10) / 10;
                    if (snapValue == baseRhythm && !currentNote.IsStrong && taikoHitObject is not Swell or DrumRoll)
                    {
                        var noteSample = currentNote.Samples;
                        toAdd.Add(new Hit
                        {
                            StartTime = currentNote.StartTime + (nextNote.StartTime - currentNote.StartTime) / 2,
                            Samples = InvertAddition.Value ? currentNote.Samples : currentNote.Samples,
                            HitWindows = currentNote.HitWindows,
                            Type = InvertAddition.Value
                                ? (currentNote.Type == HitType.Centre ? HitType.Rim : HitType.Centre)
                                : currentNote.Type
                        });
                    }
                }
            }

            taikoBeatmap.HitObjects.AddRange(toAdd);
            taikoBeatmap.HitObjects.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        private double getSnapBetweenNotes(ControlPointInfo controlPointInfo, TaikoHitObject currentNote, TaikoHitObject nextNote)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);
            double difference = nextNote.StartTime - currentNote.StartTime;
            return 1 / (difference / currentTimingPoint.BeatLength);
        }
    }
}
