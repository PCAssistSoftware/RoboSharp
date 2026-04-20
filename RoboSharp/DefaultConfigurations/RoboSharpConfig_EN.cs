using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboSharp.DefaultConfigurations
{
    /// <summary>
    /// This is the Default Configuration class to use
    /// </summary>
    internal partial class RoboSharpConfig_EN : RoboSharpConfiguration
    {
        private const string _ErrorToken = "ERROR";

        public RoboSharpConfig_EN() : base()
        {
            errorToken = _ErrorToken;
            errorTokenRegex = GetErrorTokenRegex();

            // < File Tokens >

            LogParsing_NewFile = "New File";
            LogParsing_OlderFile = "Older";
            LogParsing_NewerFile = "Newer";
            LogParsing_SameFile = "same";
            LogParsing_ExtraFile = "*EXTRA File";
            LogParsing_MismatchFile = "*Mismatch";
            LogParsing_FailedFile = "*Failed";
            LogParsing_FileExclusion = "named";

            // < Directory Tokens >

            LogParsing_NewDir = "New Dir";
            LogParsing_ExtraDir = "*EXTRA Dir";
            LogParsing_ExistingDir = "Existing Dir";
            LogParsing_DirectoryExclusion = "named";
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex(ErrorTokenPatternPrefix + _ErrorToken + ErrorTokenPatternSuffix, ErrorTokenOptions, 1000)]
        private static partial Regex GetErrorTokenRegex();
#else
        private static Regex GetErrorTokenRegex() => RoboSharpConfiguration.ErrorTokenRegexGenerator(_ErrorToken);
#endif
    }
}