﻿using MinerPlugin;
using NHM.Common.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenMiner
{
    internal class BrokenMiner : IMiner
    {
        async Task<ApiData> IMiner.GetMinerStatsDataAsync()
        {
            await Task.Delay(100);
            var api = new ApiData();
            api.AlgorithmSpeedsTotal = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(AlgorithmType.ZHash, 1) };
            api.PowerUsageTotal = 1;
            var speedDev = new Dictionary<string, IReadOnlyList<AlgorithmTypeSpeedPair>>();
            speedDev.Add("GPU-d97bdb7c-4155-9124-31b7-4743e16d3ac0", new List<AlgorithmTypeSpeedPair>() { new AlgorithmTypeSpeedPair(AlgorithmType.ZHash, 1) });
            api.AlgorithmSpeedsPerDevice = speedDev;
            var powerDev = new Dictionary<string, int>();
            powerDev.Add("GPU-d97bdb7c-4155-9124-31b7-4743e16d3ac0", 1);
            api.PowerUsagePerDevice = powerDev;
            return GetValueOrErrorSettings.GetValueOrError("GetMinerStatsDataAsync", api);
        }

        void IMiner.InitMiningLocationAndUsername(string miningLocation, string username, string password) => GetValueOrErrorSettings.SetError("InitMiningLocationAndUsername");

        void IMiner.InitMiningPairs(IEnumerable<MiningPair> miningPairs) => GetValueOrErrorSettings.SetError("InitMiningPairs");


        async Task<BenchmarkResult> IMiner.StartBenchmark(CancellationToken stop, BenchmarkPerformanceType benchmarkType)
        {
            await Task.Delay(100);
            var bp = new BenchmarkResult { AlgorithmTypeSpeeds = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(AlgorithmType.ZHash, 12) }, Success = true };
            return GetValueOrErrorSettings.GetValueOrError("StartBenchmark", bp);
        }

        void IMiner.StartMining() => GetValueOrErrorSettings.SetError("StartMining");

        void IMiner.StopMining() => GetValueOrErrorSettings.SetError("StopMining");
    }
}
