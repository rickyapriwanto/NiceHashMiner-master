﻿using MinerPlugin;
using MinerPluginToolkitV1;
using MinerPluginToolkitV1.Configs;
using Newtonsoft.Json;
using NHM.Common;
using NHM.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BMiner
{
    public class BMiner : MinerBase
    {
        private string _devices;
        private int _apiPort;
        private readonly HttpClient _http = new HttpClient();

        public BMiner(string uuid, Func<AlgorithmType, string> algorithmName, Func<AlgorithmType, double> devFee) : base(uuid)
        {
            _algorithmName = algorithmName;
            _devFee = devFee;
        }

        readonly Func<AlgorithmType, string> _algorithmName;
        readonly Func<AlgorithmType, double> _devFee;

        protected virtual string AlgorithmName(AlgorithmType algorithmType) => _algorithmName(algorithmType);

        private double DevFee => _devFee(_algorithmType);

        public async override Task<ApiData> GetMinerStatsDataAsync()
        {
            var api = new ApiData();
            try
            {
                var result = await _http.GetStringAsync($"http://127.0.0.1:{_apiPort}/api/status");
                var summary = JsonConvert.DeserializeObject<JsonApiResponse>(result);

                var gpus = _miningPairs.Select(pair => pair.Device);
                var perDeviceSpeedInfo = new Dictionary<string, IReadOnlyList<AlgorithmTypeSpeedPair>>();
                var perDevicePowerInfo = new Dictionary<string, int>();
                var totalSpeed = 0d;
                var totalPowerUsage = 0;
                var apiDevices = summary.miners;

                foreach(var gpu in gpus)
                {
                    if (apiDevices == null) continue;
                    var apiDevice = apiDevices[gpu.ID.ToString()];
                    var currentSpeed = apiDevice.solver.solution_rate;
                    totalSpeed += currentSpeed;
                    perDeviceSpeedInfo.Add(gpu.UUID, new List<AlgorithmTypeSpeedPair>() { new AlgorithmTypeSpeedPair(_algorithmType, currentSpeed * (1 - DevFee * 0.01)) });
                    var currentPower = apiDevice.device.power;
                    totalPowerUsage += currentPower;
                    perDevicePowerInfo.Add(gpu.UUID, currentPower);
                }

                api.AlgorithmSpeedsTotal = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(_algorithmType, totalSpeed * (1 - DevFee * 0.01)) };
                api.PowerUsageTotal = totalPowerUsage;
                api.AlgorithmSpeedsPerDevice = perDeviceSpeedInfo;
                api.PowerUsagePerDevice = perDevicePowerInfo;
            }
            catch (Exception e)
            {
                Logger.Error(_logGroup, $"Error occured while getting API stats: {e.Message}");
            }

            return api;
        }

        public async override Task<BenchmarkResult> StartBenchmark(CancellationToken stop, BenchmarkPerformanceType benchmarkType = BenchmarkPerformanceType.Standard)
        {
            // determine benchmark time 
            // settup times
            var benchmarkTime = MinerBenchmarkTimeSettings.ParseBenchmarkTime(new List<int> { 30, 60, 120 }, MinerBenchmarkTimeSettings, _miningPairs, benchmarkType); // in seconds

            var urlWithPort = StratumServiceHelpers.GetLocationUrl(_algorithmType, _miningLocation, NhmConectionType.STRATUM_TCP);
            var split = urlWithPort.Split(':');
            var url = split[1].Substring(2, split[1].Length - 2);
            var port = split[2];
            var algo = AlgorithmName(_algorithmType);

            var commandLine = $"-uri {algo}://{_username}@{url}:{port} {_devices} -watchdog=false {_extraLaunchParameters}";
            var binPathBinCwdPair = GetBinAndCwdPaths();
            var binPath = binPathBinCwdPair.Item1;
            var binCwd = binPathBinCwdPair.Item2;
            Logger.Info(_logGroup, $"Benchmarking started with command: {commandLine}");
            var bp = new BenchmarkProcess(binPath, binCwd, commandLine, GetEnvironmentVariables());

            var benchHashes = 0d;
            var benchIters = 0;
            var benchHashResult = 0d;  // Not too sure what this is..
            var targetBenchIters = Math.Max(1, (int)Math.Floor(benchmarkTime / 20d));

            bp.CheckData = (string data) =>
            {
                var hashrateFoundPair = MinerToolkit.TryGetHashrateAfter(data, "Total");
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
                    AlgorithmTypeSpeeds = new List<AlgorithmTypeSpeedPair>{ new AlgorithmTypeSpeedPair(_algorithmType, benchHashResult)},
                    Success = benchIters >= targetBenchIters
                };
            };

            var benchmarkTimeout = TimeSpan.FromSeconds(benchmarkTime + 10);
            var benchmarkWait = TimeSpan.FromMilliseconds(500);
            var t = MinerToolkit.WaitBenchmarkResult(bp, benchmarkTimeout, benchmarkWait, stop);
            return await t;
        }

        protected override void Init()
        {
            var deviceIDs = _miningPairs.Select(p =>
            {
                var device = p.Device;
                var prefix = device.DeviceType == DeviceType.AMD ? "amd:" : "";
                return prefix + device.ID;
            }).OrderBy(id => id);
            _devices = $"-devices {string.Join(",", deviceIDs)}";
        }

        protected override string MiningCreateCommandLine()
        {
            // API port function might be blocking
            _apiPort = GetAvaliablePort();
            // instant non blocking
            var urlWithPort = StratumServiceHelpers.GetLocationUrl(_algorithmType, _miningLocation, NhmConectionType.STRATUM_TCP);
            var split = urlWithPort.Split(':');
            var url = split[1].Substring(2, split[1].Length - 2);
            var port = split[2];

            var algo = AlgorithmName(_algorithmType);
            var commandLine = $"-uri {algo}://{_username}@{url}:{port} -api 127.0.0.1:{_apiPort} {_devices} -watchdog=false {_extraLaunchParameters}";
            return commandLine;
        }
    }
}
