﻿using MinerPlugin;
using MinerPluginToolkitV1;
using MinerPluginToolkitV1.Configs;
using Newtonsoft.Json;
using NHM.Common;
using NHM.Common.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NanoMiner
{
    public class NanoMiner : MinerBase
    {

        private readonly HttpClient _http = new HttpClient();
        private int _apiPort;

        protected readonly Dictionary<string, int> _mappedIDs = new Dictionary<string, int>();

        public NanoMiner(string uuid, Dictionary<string, int> mappedIDs, Func<AlgorithmType, string> algorithmName, Func<AlgorithmType, double> devFee) : base(uuid)
        {
            _mappedIDs = mappedIDs;
            _algorithmName = algorithmName;
            _devFee = devFee;
        }

        readonly Func<AlgorithmType, string> _algorithmName;
        readonly Func<AlgorithmType, double> _devFee;

        protected virtual string AlgorithmName(AlgorithmType algorithmType) => _algorithmName(algorithmType);
        private double DevFee => _devFee(_algorithmType);

        protected override IEnumerable<MiningPair> GetSortedMiningPairs(IEnumerable<MiningPair> miningPairs)
        {
            var pairsList = miningPairs.ToList();
            // sort by mapped ids
            pairsList.Sort((a, b) => _mappedIDs[a.Device.UUID].CompareTo(_mappedIDs[b.Device.UUID]));
            return pairsList;
        }

        protected override void Init()
        {
            // ?????
        }

        public async override Task<ApiData> GetMinerStatsDataAsync()
        {
            var api = new ApiData();
            try
            {
                var result = await _http.GetStringAsync($"http://127.0.0.1:{_apiPort}/stats");
                var apiResponse = JsonConvert.DeserializeObject<JsonApiResponse>(result);
                var parsedApiResponse = JsonApiHelpers.ParseJsonApiResponse(apiResponse, _mappedIDs);

                var perDeviceSpeedInfo = new Dictionary<string, IReadOnlyList<AlgorithmTypeSpeedPair>>();
                var perDevicePowerInfo = new Dictionary<string, int>();
                var totalSpeed = 0d;
                var totalPowerUsage = 0;

                foreach (var miningPair in _miningPairs)
                {
                    var deviceUUID = miningPair.Device.UUID;
                    if (parsedApiResponse.ContainsKey(deviceUUID))
                    {
                        var stat = parsedApiResponse[deviceUUID];
                        var currentPower = (int)stat.Power;
                        totalPowerUsage += currentPower;
                        var hashrate = stat.Hashrate * (1 - DevFee * 0.01);
                        totalSpeed += hashrate;
                        perDeviceSpeedInfo.Add(deviceUUID, new List<AlgorithmTypeSpeedPair>() { new AlgorithmTypeSpeedPair(_algorithmType, hashrate) });
                        perDevicePowerInfo.Add(deviceUUID, currentPower);
                    }
                    else
                    {
                        perDeviceSpeedInfo.Add(deviceUUID, new List<AlgorithmTypeSpeedPair>() { new AlgorithmTypeSpeedPair(_algorithmType, 0) });
                        perDevicePowerInfo.Add(deviceUUID, 0);
                    }
                }

                api.AlgorithmSpeedsPerDevice = perDeviceSpeedInfo;
                api.AlgorithmSpeedsTotal = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(_algorithmType, totalSpeed) };
                api.PowerUsagePerDevice = perDevicePowerInfo;
                api.PowerUsageTotal = totalPowerUsage;
            }
            catch (Exception e)
            {
                Logger.Error(_logGroup, $"Error occured while getting API stats: {e.Message}");
            }

            return api;
        }

        public async override Task<BenchmarkResult> StartBenchmark(CancellationToken stop, BenchmarkPerformanceType benchmarkType = BenchmarkPerformanceType.Standard)
        {
            var benchmarkTime = MinerBenchmarkTimeSettings.ParseBenchmarkTime(new List<int> { 60, 120, 180 }, MinerBenchmarkTimeSettings, _miningPairs, benchmarkType); // in seconds

            var commandLine = CreateCommandLine(MinerToolkit.DemoUserBTC);
            var binPathBinCwdPair = GetBinAndCwdPaths();
            var binPath = binPathBinCwdPair.Item1;
            var binCwd = binPathBinCwdPair.Item2;
            Logger.Info(_logGroup, $"Benchmarking started with command: {commandLine}");
            var bp = new BenchmarkProcess(binPath, binCwd, commandLine, GetEnvironmentVariables());

            var benchHashes = 0d;
            var benchIters = 0;
            var benchHashResult = 0d;
            var targetBenchIters = Math.Max(1, (int)Math.Floor(benchmarkTime / 20d));

            bp.CheckData = (string data) =>
            {
                var hashrateFoundPair = MinerToolkit.TryGetHashrateAfter(data, "Total speed:");
                var hashrate = hashrateFoundPair.Item1;
                var found = hashrateFoundPair.Item2;

                if (found)
                {
                    benchHashes += hashrate;
                    benchIters++;

                    benchHashResult = (benchHashes / benchIters) * (1 - DevFee * 0.01);
                }

                return new BenchmarkResult
                {
                    AlgorithmTypeSpeeds = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(_algorithmType, benchHashResult) },
                    Success = benchIters >= targetBenchIters
                };
            };

            var benchmarkTimeout = TimeSpan.FromSeconds(benchmarkTime + 10);
            var benchmarkWait = TimeSpan.FromMilliseconds(500);
            var t = MinerToolkit.WaitBenchmarkResult(bp, benchmarkTimeout, benchmarkWait, stop);
            return await t;
        }

        protected override string MiningCreateCommandLine()
        {
            return CreateCommandLine(_username);
        }

        private string CreateCommandLine(string username)
        {
            _apiPort = GetAvaliablePort();

            var algo = AlgorithmName(_algorithmType);

            var url = StratumServiceHelpers.GetLocationUrl(_algorithmType, _miningLocation, NhmConectionType.NONE);
            var paths = GetBinAndCwdPaths();

            var configString = "";
            if (_extraLaunchParameters != "")
            {
                var arrayOfELP = _extraLaunchParameters.Split(' ');
                foreach (var elp in arrayOfELP)
                {
                    configString += $"{elp}\r\n";
                }
            }

            var devs = string.Join(",", _miningPairs.Select(p => _mappedIDs[p.Device.UUID]));

            configString += $"webPort={_apiPort}\r\nwatchdog=false\n\r\n\r[{algo}]\r\nwallet={username}\r\nrigName=\r\ndevices={devs}\r\npool1={url}";
            try
            {
                File.WriteAllText(Path.Combine(paths.Item2, $"config_nh_{devs}.ini"), configString);
            }
            catch(Exception e)
            {
                Logger.Error(_logGroup, $"Unable to create config file: {e.Message}");
            }
            return $"config_nh_{devs}.ini";
        }
    }
}
