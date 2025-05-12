// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Utils;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using Vulkan;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModAetherRitual : Mod, IApplicableToBeatmap
    {
        public override LocalisableString Description => @"When the 8 star is suspicious.";
        public override Type[] IncompatibleMods => base.IncompatibleMods.Append(typeof(TaikoModSwap)).ToArray();

        public override string Name => "Aether Ritual";

        public override string Acronym => "AR";

        public override double ScoreMultiplier => 0.75;

        private static Dictionary<string, (double beatDistance, int isKat)[]> hitObjects = new()
        {
            { "Easy", new[] { (0.0, 0), (2.0, 1), (3.0, 0), (3.5, 1), (4.0, 0), (5.5, 0), (7.0, 0) } },
            { "Normal", new[] { (0.0, 1), (0.333, 0), (0.667, 0), (1.0, 1), (1.5, 0), (2.0, 1), (2.5, 1), (3.0, 0), (3.5, 1), (4.0, 0), (4.333, 0), (4.667, 0), (5.0, 1), (5.5, 0), (6.0, 1), (6.5, 1), (7.0, 0) } },
            { "Hard", new[] { (0.0, 1), (0.25, 1), (0.5, 0), (0.75, 0), (1.0, 1), (1.125, 1), (1.5, 0), (1.625, 0), (2.0, 1), (2.125, 1), (2.5, 1), (2.625, 1), (3.0, 0), (3.125, 0), (3.5, 1), (3.625, 1), (4.0, 0), (4.25, 0), (4.5, 0), (4.75, 0), (5.0, 1), (5.125, 1), (5.5, 0), (5.625, 0), (6.0, 1), (6.125, 1), (6.5, 1), (6.625, 1), (7.0, 0), (7.125, 0), (7.5, 0) } }
        };

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;

            double startTime = taikoBeatmap.ControlPointInfo.TimingPoints[0].Time;
            double endTime = taikoBeatmap.HitObjects.Last().StartTime;

            List<TaikoHitObject> newHitObjects = new();

            double getMeasureDuration2x(double time)
            {
                var relevantTP = beatmap.ControlPointInfo.TimingPointAt(time);
                return relevantTP.BeatLength * relevantTP.TimeSignature.Numerator * 2;
            }

            foreach (var tp in beatmap.ControlPointInfo.TimingPoints)
            {
                double currentEndTime = beatmap.ControlPointInfo.TimingPointAfter(tp.Time)?.Time ?? endTime;
                for (double measureStart = tp.Time; measureStart < currentEndTime; measureStart += getMeasureDuration2x(measureStart))
                {
                    double measureEnd = Math.Min(measureStart + getMeasureDuration2x(measureStart), currentEndTime);
                    var hitObjectsInMeasure = beatmap.HitObjects
                        .Where(h => h.StartTime >= measureStart && h.StartTime < measureStart + getMeasureDuration2x(measureStart))
                        .ToList();

                    string difficulty = hitObjectsInMeasure.Count >= 24 ? "Hard" : hitObjectsInMeasure.Count >= 9 ? "Normal" : hitObjectsInMeasure.Count > 0 ? "Easy" : "None";
                    Logger.Log(difficulty);
                    if (difficulty == "None") continue;

                    var toKindaAdd = hitObjects[difficulty];

                    foreach (var tup in toKindaAdd)
                    {
                        double timePoint = (tup.beatDistance * (getMeasureDuration2x(measureStart) / 8)) + measureStart;

                        if (timePoint >= measureEnd) break;
                        Hit taikoHitObject = new()
                        {
                            HitWindows = taikoBeatmap.HitObjects[0].HitWindows,
                            IsStrong = false,
                            StartTime = timePoint,
                            Type = tup.isKat == 0 ? HitType.Centre : HitType.Rim
                        };
                        newHitObjects.Add(taikoHitObject);
                    }
                }
            }
            Logger.Log(newHitObjects.Count.ToString());
            taikoBeatmap.HitObjects = [.. newHitObjects.OrderBy(h => h.StartTime)];
        }
    }
}
