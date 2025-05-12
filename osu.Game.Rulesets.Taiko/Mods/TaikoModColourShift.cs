// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModColourShift : Mod, IApplicableToBeatmap
    {
        public override string Name => "Colour Shift";
        public override string Acronym => "CS";
        public override double ScoreMultiplier => 0.75;
        public override LocalisableString Description => "Shifts/Loops the colours of notes in patterns by a specified amount.";
        public override ModType Type => ModType.Conversion;
        public override IconUsage? Icon => FontAwesome.Solid.Redo;

        [SettingSource("Shift Amount", "Shift the patterns' colours by this amount", SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> ShiftAmount { get; } = new Bindable<int?>(3);
        [SettingSource("Shift backwards", "Shift notes backwards instead of forwards.")]
        public BindableBool ReverseShifting { get; } = new BindableBool();

        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(ModRandom), typeof(TaikoModSwap) }).ToArray();
        private List<(double startTime, List<Hit> hitObjects, int snapping)> getPattern(IBeatmap beatmap)
        {
            List<(double startTime, List<Hit> hitObjects, int snapping)> PatternList = [];
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;

            Hit[] hits = [.. taikoBeatmap.HitObjects.Where(obj => obj is Hit).Cast<Hit>()];

            if (hits.Length == 0) return [];

            bool inPattern = false;

            const int min_snap = 3;

            int patternStartIndex = 0;

            int lastSnap = -1;
            for (int i = 1; i < hits.Length; i++)
            {
                int snapValue = getSnapBetweenNotes(controlPointInfo, hits[i - 1], hits[i]);

                if (inPattern)
                {
                    // pattern continues
                    if (snapValue == lastSnap) continue;

                    // process Pattern
                    inPattern = false;

                    processPattern(i);
                }
                else // Try to start a new pattern.
                {
                    // Check if the snapping is high enough.
                    if (snapValue >= min_snap)
                    {
                        lastSnap = snapValue;
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
                PatternList.Add((
                    hits[patternStartIndex].StartTime,
                    hits.Skip(patternStartIndex).Take(patternEndIndex - patternStartIndex).ToList(),
                    lastSnap
                    ));
            }

            return PatternList;
        }
        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var taikoBeatmap = (TaikoBeatmap)beatmap;
            var controlPointInfo = taikoBeatmap.ControlPointInfo;

            ShiftAmount.Value ??= 3;

            Hit[] hits = taikoBeatmap.HitObjects.Where(obj => obj is Hit).Cast<Hit>().ToArray();

            if (hits.Length == 0)
                return;

            var patternList = getPattern(beatmap);
            Logger.Log(patternList.Count.ToString());

            foreach (var pattern in patternList)
            {
                var hitTypes = pattern.hitObjects.Select(i => i.Type).ToList();

                for (int i = 0; i < hitTypes.Count; i++)
                {
                    int shift = ReverseShifting.Value ? -(int)ShiftAmount.Value : (int)ShiftAmount.Value;
                    pattern.hitObjects[i].Type = hitTypes[((i + shift) % hitTypes.Count + hitTypes.Count) % hitTypes.Count];
                }
            }
        }

        private int getSnapBetweenNotes(ControlPointInfo controlPointInfo, Hit currentNote, Hit nextNote)
        {
            var currentTimingPoint = controlPointInfo.TimingPointAt(currentNote.StartTime);
            return controlPointInfo.GetClosestBeatDivisor(currentTimingPoint.Time + (nextNote.StartTime - currentNote.StartTime));
        }
    }
}
