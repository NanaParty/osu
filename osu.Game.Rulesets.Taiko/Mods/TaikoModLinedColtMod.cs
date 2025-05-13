// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModLinedColtMod : Mod, IApplicableToBeatmap
    {
        public override string Name => "LinedColt Mod";
        public override string Acronym => "PP";
        public override double ScoreMultiplier => rollFruit();
        public override LocalisableString Description => $"Press the button in the customize tab to spin for a multiplier. (64% for 0.25) | (25.9% for 0.5) | (6.3% for 0.75) | (5.53% for 1) | (1.63% for 1.5) | (0.21% for 2)";
        public override ModType Type => ModType.Fun;
        public override IconUsage? Icon => FontAwesome.Solid.BlenderPhone;

        [SettingSource("Roll", "Press this button to roll a multiplier.")]
        public BindableBool AdjustPitch { get; } = new BindableBool();

        private double rollFruit()
        {
            (double weight, double multip)[] rarities = [
                (64, 0.25),
                (25.9, 0.5),
                (6.3, 0.75),
                (5.53, 1),
                (1.63, 1.5),
                (0.21, 2),
            ];

            double totalWeight = rarities.Aggregate(0.0, (acc, cur) => acc += cur.weight);
            double roll = new Random().NextDouble() * totalWeight;
            foreach (var rarity in rarities)
            {
                if (roll < rarity.weight)
                    return rarity.multip; //+ (Framework.Utils.RNG.NextDouble() * 0.1);

                roll -= rarity.weight;
            }
            return 0;
        }
        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;

            Hit[] hits = taikoBeatmap.HitObjects.Where(obj => obj is Hit).Cast<Hit>().ToArray();

            if (hits.Length == 0)
                return;

            var conversions = new List<(int, int)>();

            bool inPattern = false;


            const int base_rhythm = 4;

            int patternStartIndex = 0;

            for (int i = 1; i < hits.Length; i++)
            {
                double snapValue = getSnapBetweenNotes(controlPointInfo, hits[i - 1], hits[i]);

                if (inPattern)
                {
                    // pattern continues
                    if (snapValue == base_rhythm) continue;

                    inPattern = false;

                    processPattern(i);
                }
                else
                {
                    if (snapValue == base_rhythm)
                    {
                        patternStartIndex = i - 1;
                        inPattern = true;
                    }
                }
            }

            // Process the last pattern if we reached the end of the beatmap and are still in a pattern.
            if (inPattern)
                processPattern(hits.Length);

            void processPattern(int patternEndIndex)
            {
                if (patternEndIndex - patternStartIndex < 24) return;
                int k = 0;
                // Iterate through the pattern
                for (int j = patternStartIndex; j < patternEndIndex; j++)
                {
                    int indexInPattern = j - patternStartIndex;

                    int[] snapOrder = [4, 5, 6, 7];

                    taikoBeatmap.HitObjects[j].StartTime = snappedTime(taikoBeatmap.ControlPointInfo, taikoBeatmap.HitObjects[j].StartTime, snapOrder[k++ % 4]);
                }
            }
        }

        private double snappedTime(ControlPointInfo controlPointInfo, double time, int snapping)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(time);
            double snap = currentTimingPoint.BeatLength / snapping;
            double timeOffset = time - currentTimingPoint.Time;
            return currentTimingPoint.Time + (Math.Round(timeOffset / snap) * snap);
        }

        private int getSnapBetweenNotes(ControlPointInfo controlPointInfo, Hit currentNote, Hit nextNote)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);
            return controlPointInfo.GetClosestBeatDivisor(currentTimingPoint.Time + (nextNote.StartTime - currentNote.StartTime));
        }
    }
}
