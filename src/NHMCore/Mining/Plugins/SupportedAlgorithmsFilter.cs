﻿using MinerPluginToolkitV1.Configs;
using MinerPluginToolkitV1.Interfaces;
using Newtonsoft.Json;
using NHM.Common;
using NHM.Common.Enums;
using System.Collections.Generic;
using System.Linq;

namespace NHMCore.Mining.Plugins
{
    internal static class SupportedAlgorithmsFilter
    {
        private class SupportedAlgorithmsFilterSettings : IInternalSetting
        {
            [JsonProperty("use_user_settings")]
            public bool UseUserSettings { get; set; } = false;

            [JsonProperty("filtered_algorithms")]
            public List<List<AlgorithmType>> FilteredAlgorithms = new List<List<AlgorithmType>>
            {
                new List<AlgorithmType> { AlgorithmType.MTP },
            };
        }


        static SupportedAlgorithmsFilterSettings _settings = new SupportedAlgorithmsFilterSettings();

        static SupportedAlgorithmsFilter()
        {
            var fileSettings = InternalConfigs.InitInternalSetting(Paths.Root, _settings, "SupportedAlgorithmsFilter.json");
            if (fileSettings != null) _settings = fileSettings;
        }

        static public bool IsSupported(IEnumerable<AlgorithmType> ids)
        {
            foreach (var filterIds in _settings.FilteredAlgorithms)
            {
                if (filterIds.Count != ids.Count()) continue;
                var sameIds = filterIds.Zip(ids, (f, s) => f == s).All(equal => equal);
                if (sameIds) return false;
            }
            return true;
        }
    }
}
