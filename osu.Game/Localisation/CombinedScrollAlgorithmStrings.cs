// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class CombinedScrollAlgorithmStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.CombinedScrollAlgorithm";

        /// <summary>
        /// " &lt;- bruh"
        /// </summary>
        public static LocalisableString Bruh => new TranslatableString(getKey(@"bruh"), @" <- bruh");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}