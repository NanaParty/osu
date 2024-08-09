// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class ScrollVisualisationMethodStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScrollVisualisationMethod";

        /// <summary>
        /// "Constant"
        /// </summary>
        public static LocalisableString Constant => new TranslatableString(getKey(@"constant"), @"Constant");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}