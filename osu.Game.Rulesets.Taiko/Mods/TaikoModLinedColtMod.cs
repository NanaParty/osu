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
        public override double ScoreMultiplier => (SpeedChange.Value * 43271894) % 1.04;
        public override LocalisableString Description => "if you change the customise slider there is a chance you can roll a good multplier";
        public override ModType Type => ModType.Fun;
        public override IconUsage? Icon => FontAwesome.Solid.BlenderPhone;

        [SettingSource("LinedColt (tm) modifier machiner", "🦅 Eagle Emoji | Meaning, Copy And Paste", SettingControlType = typeof(MultiplierSettingsSlider))]
        public BindableNumber<double> SpeedChange { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.00001,
        };

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
                if (patternEndIndex - patternStartIndex < 16) return;
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
