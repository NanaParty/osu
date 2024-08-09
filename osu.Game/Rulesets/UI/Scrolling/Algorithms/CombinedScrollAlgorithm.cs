// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Lists;
using osu.Game.Rulesets.Timing;
using System.Linq;
using osu.Framework.Logging;

namespace osu.Game.Rulesets.UI.Scrolling.Algorithms
{
    public class CombinedScrollAlgorithm : IScrollAlgorithm
    {
        private SequentialScrollAlgorithm ssa;
        private OverlappingScrollAlgorithm osa;

        private readonly IReadOnlyList<MultiplierControlPoint> controlPoints;

        /// <summary>
        /// Stores a mapping of time -> position for each control point.
        /// </summary>


        public CombinedScrollAlgorithm(SortedList<MultiplierControlPoint> controlPoints, SortedList<MultiplierControlPoint> InvertedControlPoints)
        {

            this.controlPoints = controlPoints;
            ssa = new(controlPoints);
            osa = new(controlPoints);
        }

        public double GetDisplayStartTime(double originTime, float offset, double timeRange, float scrollLength)
        {
            double mania = ssa.GetDisplayStartTime(originTime, offset, timeRange, scrollLength);
            double taiko = osa.GetDisplayStartTime(originTime, offset, timeRange, scrollLength);

            return (mania + taiko) / 2;
        }

        public float GetLength(double startTime, double endTime, double timeRange, float scrollLength)
        {
            float mania = ssa.GetLength(startTime, endTime, timeRange, scrollLength);
            float taiko = osa.GetLength(startTime, endTime, timeRange, scrollLength);
            return (mania + taiko) / 2f;
        }

        public float PositionAt(double time, double currentTime, double timeRange, float scrollLength, double? originTime = null)
        {
            float ssav = ssa.PositionAt(time, currentTime, timeRange, scrollLength, originTime);
            float osav = osa.PositionAt(time, currentTime, timeRange, scrollLength, originTime);
            return (ssav + osav) / 2f;
        }

        public double TimeAt(float position, double currentTime, double timeRange, float scrollLength)
        {
            double ssav = ssa.TimeAt(position, currentTime, timeRange, scrollLength);
            double osav = osa.TimeAt(position, currentTime, timeRange, scrollLength);
            return (ssav + osav) / 2d;
        }

        public void Reset() { }
    }
}
