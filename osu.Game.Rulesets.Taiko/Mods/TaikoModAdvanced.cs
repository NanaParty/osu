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
    public class TaikoModAdvanced : Mod, IApplicableToBeatmap
    {
        public override string Name => "Advanced";
        public override string Acronym => "AV";
        public override double ScoreMultiplier => 0.5;
        public override LocalisableString Description => "Add even more notes to the mix.";
        public override ModType Type => ModType.Conversion;

        [SettingSource("Single conversion", "Converts 1/1 patterns to 1/2 patterns.")]
        public Bindable<bool> SingleConversion { get; } = new BindableBool(true);

        [SettingSource("One-half conversion", "Converts 1/2 patterns to 1/4 patterns.")]
        public Bindable<bool> OneHalfConversion { get; } = new BindableBool(true);

        [SettingSource("One-third conversion", "Converts 1/3 patterns to 1/6 patterns.")]
        public Bindable<bool> OneThirdConversion { get; } = new BindableBool(true);

        [SettingSource("Invert added notes", "Added notes become opposite of the prior note.")]
        public Bindable<bool> InvertAdded { get; } = new BindableBool(false);

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;
            List<Hit> toAdd = new List<Hit>();

            // Snap conversions for rhythms
            var snapConversions = new Dictionary<int, bool>
            {
                { 3, OneThirdConversion.Value }, // 1/3 snap
                { 2, OneHalfConversion.Value }, // 1/2 snap
                { 1, SingleConversion.Value }, // 1/1 snap
            };

            List<Hit> hits = taikoBeatmap.HitObjects.Where(obj => obj is Hit && obj is not Swell && obj is not DrumRoll).Cast<Hit>().ToList();

            foreach (var snapConversion in snapConversions)
            {
                // Skip processing if the corresponding conversion is disabled
                if (!snapConversion.Value)
                    continue;

                for (int i = 0; i < hits.Count - 1; i++)
                {
                    double snapValue = i < hits.Count
                        ? getSnapBetweenNotes(controlPointInfo, hits[i], hits[i + 1])
                        : 1; // No next note, default to a safe 1/1 snap

                    if (snapValue == snapConversion.Key)
                    {
                        double middleTime = (hits[i].StartTime + hits[i + 1].StartTime) / 2;
                        toAdd.Add(new()
                        {
                            IsStrong = false,
                            StartTime = middleTime,
                            Type = InvertAdded.Value ? hits[i].Type == HitType.Centre ? HitType.Rim : HitType.Centre : hits[i].Type,
                            HitWindows = hits[i].HitWindows,
                        });
                    }
                }
            }

            // Concat queued notes
            taikoBeatmap.HitObjects = taikoBeatmap.HitObjects.Concat(toAdd).OrderBy(h => h.StartTime).ToList();
        }

        private int getSnapBetweenNotes(ControlPointInfo controlPointInfo, Hit currentNote, Hit nextNote)
        {
            double gapMs = Math.Max(currentNote.StartTime, nextNote.StartTime) - Math.Min(currentNote.StartTime, nextNote.StartTime);
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);

            return controlPointInfo.GetClosestBeatDivisor(gapMs + currentTimingPoint.Time);
        }

        private bool shouldProcessRhythm(int snap)
        {
            return snap switch
            {
                1 => SingleConversion.Value,
                2 => OneHalfConversion.Value,
                3 => OneThirdConversion.Value,
                _ => false,
            };
        }
    }
}
