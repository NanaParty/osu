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
        public override double ScoreMultiplier => 0.4;
        public override LocalisableString Description => "Trickify simple rhythms!";
        public override ModType Type => ModType.DifficultyIncrease;
        public override Type[] IncompatibleMods => new[] { typeof(TaikoModSimplifiedRhythm) };

        [SettingSource("1/1 to 1/2 conversion", "Converts 1/1 patterns to 1/2 rhythm.")]
        public Bindable<bool> OneWholeConversion { get; } = new BindableBool();

        [SettingSource("1/2 to 1/4 conversion", "Converts 1/2 patterns to 1/4 rhythm.")]
        public Bindable<bool> OneHalfConversion { get; } = new BindableBool(true);

        [SettingSource("1/3 to 1/6 conversion", "Converts 1/3 patterns to 1/6 rhythm.")]
        public Bindable<bool> OneThirdConversion { get; } = new BindableBool();

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
                    var currentNote = hits[i - 1];
                    var nextNote = hits[i];
                    double snapValue = getSnapBetweenNotes(controlPointInfo, currentNote, nextNote);

                    if (snapValue == baseRhythm && !currentNote.Samples.Any(i => i.Name == Audio.HitSampleInfo.HIT_FINISH))
                    {
                        toAdd.Add(new Hit
                        {
                            StartTime = currentNote.StartTime + (nextNote.StartTime - currentNote.StartTime) / 2,
                            Samples = currentNote.Samples,
                            HitWindows = currentNote.HitWindows,
                        });
                    }
                }
            }

            taikoBeatmap.HitObjects.AddRange(toAdd);
            taikoBeatmap.HitObjects.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        private int getSnapBetweenNotes(ControlPointInfo controlPointInfo, Hit currentNote, Hit nextNote)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);
            return controlPointInfo.GetClosestBeatDivisor(currentTimingPoint.Time + (nextNote.StartTime - currentNote.StartTime));
        }
    }
}
