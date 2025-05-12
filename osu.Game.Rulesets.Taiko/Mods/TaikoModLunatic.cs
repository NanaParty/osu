// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModLunatic : Mod, IApplicableToBeatmap
    {
        public override string Name => "Lunatic";
        public override string Acronym => "LT";
        public override double ScoreMultiplier => 0.5;
        public override LocalisableString Description => "Randomize every measure.";
        public override ModType Type => ModType.Fun;
        public override IconUsage? Icon => FontAwesome.Solid.Angry;

        [SettingSource("Seed", "Use a custom seed instead of a random one", SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();


        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;
            double startTime = beatmap.HitObjects.First().StartTime;
            Seed.Value ??= RNG.Next();

            var hits = taikoBeatmap.HitObjects;
            var effectPoints = taikoBeatmap.ControlPointInfo.EffectPoints;
            List<(List<TaikoHitObject>, double, double)> segments = [];

            double getMeasureLengthAt(double i) => beatmap.ControlPointInfo.TimingPointAt(i).BeatLength * beatmap.ControlPointInfo.TimingPointAt(i).TimeSignature.Numerator;

            for (double i = startTime; i < beatmap.HitObjects.Last().StartTime; i += getMeasureLengthAt(i))
            {
                var hitObjectsInMeasure = hits.Where(h => h.StartTime - 2 >= i && h.StartTime + 2 < i + getMeasureLengthAt(i)).ToList();
                segments.Add((hitObjectsInMeasure, i, getMeasureLengthAt(i)));
            }

            var random = new Random((int)Seed.Value);
            var newSegments = segments.OrderBy(_ => random.Next()).ToList();

            double j = startTime;
            int index = 0;
            foreach (var (hitObjects, measureStartTime, measureLength) in newSegments)
            {
                double realMeasureLength = getMeasureLengthAt(j);
                foreach (var hit in hitObjects)
                {
                    double offset = hit.StartTime - measureStartTime;
                    double percent = offset / measureLength;
                    // if (percent > 0) Logger.Log($"{hit.StartTime} -> {offset} -> {percent} -> {measureStartTime} -> {measureLength}");
                    double newOffset = percent * realMeasureLength;
                    hit.StartTime = newOffset + j;
                }
                j += getMeasureLengthAt(j);

                index++;
            }

            Logger.Log($"Lunatic mod applied with seed {Seed.Value}/{segments.Count}");
            taikoBeatmap.HitObjects.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        private int getSnapBetweenNotes(ControlPointInfo controlPointInfo, Hit currentNote, Hit nextNote)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);
            return controlPointInfo.GetClosestBeatDivisor(currentTimingPoint.Time + (nextNote.StartTime - currentNote.StartTime));
        }
    }
}
