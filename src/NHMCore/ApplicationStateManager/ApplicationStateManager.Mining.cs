using NHM.Common;
using NHM.Common.Enums;
using NHMCore.Benchmarking;
using NHMCore.Configs;
using NHMCore.Mining;
using NHMCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NHMCore
{
    static partial class ApplicationStateManager
    {
        public static string GetUsername()
        {
            if (MiningState.Instance.IsDemoMining) {
                return DemoUser.BTC;
            }
            var btc = ConfigManager.GeneralConfig.BitcoinAddress.Trim();
            return $"{btc}${RigID}";
        }

        private static ConcurrentQueue<(ComputeDevice, DeviceState)> UpdateDevicesToMineStates { get; set; } = new ConcurrentQueue<(ComputeDevice, DeviceState)>();
        private static ConcurrentQueue<Task> _updateDevicesToMineTasks { get; set; } = new ConcurrentQueue<Task>();

        // this is a compromise for Start "Task"
        public static Task StartMiningTaskWait()
        {
            var waitTasks = new List<Task>();
            foreach (var t in _updateDevicesToMineTasks)
            {
                waitTasks.Add(t);
            }
            // this inditaces we started benchmarking
            if (waitTasks.Count == 0)
            {
                // delay for 1second
                waitTasks.Add(Task.Delay(1000));
            }
            return Task.WhenAll(waitTasks);
        }

        private static void StartMiningOnDevices(params ComputeDevice[] startDevices)
        {
            if (startDevices == null || startDevices.Length == 0) return;

            foreach (var startDevice in startDevices)
            {
                startDevice.IsPendingChange = true;
                UpdateDevicesToMineStates.Enqueue((startDevice, DeviceState.Mining));
            }
            _updateDevicesToMineTasks.Enqueue(_UpdateDevicesToMineTaskDelayed.ExecuteDelayedTask(CancellationToken.None));
        }

        private static void StopMiningOnDevices(params ComputeDevice[] stopDevices)
        {
            if (stopDevices == null || stopDevices.Length == 0) return;

            foreach (var stopDevice in stopDevices)
            {
                stopDevice.IsPendingChange = true;
                UpdateDevicesToMineStates.Enqueue((stopDevice, DeviceState.Stopped));
            }
            //var t = _UpdateDevicesToMineTaskDelayed.ExecuteDelayedTask(CancellationToken.None);
        }

        private static DelayedSingleExecActionTask _UpdateDevicesToMineTaskDelayed = new DelayedSingleExecActionTask
            (
            async () => await UpdateDevicesToMineTask(),
            TimeSpan.FromMilliseconds(100)
            );

        private static async Task UpdateDevicesToMineTask()
        {
            // drain queue 
            var updateDeviceStates = new Dictionary<ComputeDevice, DeviceState>();
            while (UpdateDevicesToMineStates.TryDequeue(out var pair))
            {
                var (device, state) = pair;
                updateDeviceStates[device] = state;
            }
            // and update states
            foreach (var newState in updateDeviceStates)
            {
                var device = newState.Key;
                var setState = newState.Value;
                device.State = setState; // THIS TRIGERS STATE CHANGE
            }
            var devicesToMine = AvailableDevices.Devices.Where(dev => dev.State == DeviceState.Mining).ToList();
            foreach (var newState in updateDeviceStates)
            {
                var device = newState.Key;
                var setState = newState.Value;
                //devicesToMine.Con
            }

            if (devicesToMine.Count > 0) {
                StartMining();
                await MiningManager.UpdateMiningSession(devicesToMine, GetUsername());
            } else {
                await StopMining();
            }
            // TODO implement and clear devicePending state changed
            foreach (var newState in updateDeviceStates)
            {
                var device = newState.Key;
                device.IsPendingChange = false;
            }
        }

        private static async Task RestartMinersIfMining()
        {
            // if mining update the mining manager
            if (MiningState.Instance.IsCurrentlyMining)
            {
                await MiningManager.RestartMiners(GetUsername());
            }
        }

        public static void ResumeMiners()
        {
            if (_resumeOldState)
            {
                _resumeOldState = false;
                foreach (var dev in _resumeDevs)
                {
                    StartDevice(dev);
                }
                _resumeDevs.Clear();
            }
            else
            {
                // TODO here we probably don't care to wait the Task to complete
                _ = RestartMinersIfMining();
            }
        }

        private static bool _resumeOldState = false;
        private static HashSet<ComputeDevice> _resumeDevs = new HashSet<ComputeDevice>();
        public static void PauseMiners()
        {
            _resumeOldState = CurrentForm == CurrentFormState.Main;
            foreach(var dev in AvailableDevices.Devices)
            {
                if (dev.State == DeviceState.Benchmarking || dev.State == DeviceState.Mining)
                {
                    _resumeDevs.Add(dev);
                    StopAllDevice();
                }
            }
        }

        // TODO add check for any enabled algorithms
        public static (bool started, string failReason) StartAllAvailableDevices()
        {
            // TODO consider trying to start the error state devices as well
            var devicesToStart = AvailableDevices.Devices.Where(dev => dev.State == DeviceState.Stopped);
            if (devicesToStart.Count() == 0) {
                return (false, "there are no new devices to start");
            }

            // TODO for now no partial success so if one fails send back that everything fails
            var started = true;
            var failReason = "";

            foreach (var startDevice in devicesToStart)
            {
                var (deviceStarted, deviceFailReason) = StartDevice(startDevice);
                started &= deviceStarted;
                if (!deviceStarted)
                {
                    failReason += $"{startDevice.Name} {deviceFailReason};";
                }
            }

            return (started, failReason);
        }

        public static bool StartSingleDevicePublic(ComputeDevice device)
        {
            if (device.IsPendingChange) return false;
            StartDevice(device);
            return true;
        }

        internal static (bool started, string failReason) StartDevice(ComputeDevice device, bool skipBenchmark = false)
        {
            // we can only start a device it is already stopped
            if (device.State == DeviceState.Disabled)
            {
                return (false, "Device is disabled");
            }

            if (device.State != DeviceState.Stopped && !skipBenchmark)
            {
                return (false, "Device already started");
            }

            var started = true;
            var failReason = "";
            var allAlgorithmsDisabled = !device.AnyAlgorithmEnabled();
            var isAllZeroPayingState = device.AllEnabledAlgorithmsZeroPaying();
            // check if device has any benchmakrs
            var needBenchmarkOrRebench = device.AnyEnabledAlgorithmsNeedBenchmarking();
            if (allAlgorithmsDisabled)
            {
                device.State = DeviceState.Error;
                started = false;
                failReason = "Cannot start a device with all disabled algoirhtms";
            }
            else if (needBenchmarkOrRebench && !skipBenchmark)
            {
                BenchmarkManager.StartBenchmarForDevice(device, new BenchmarkStartSettings
                {
                    StartMiningAfterBenchmark = true,
                    BenchmarkPerformanceType = BenchmarkPerformanceType.Standard,
                    BenchmarkOption = BenchmarkOption.ZeroOrReBenchOnly
                });
            }
            else if (isAllZeroPayingState)
            {
                device.State = DeviceState.Error;
                started = false;
                failReason = "No enabled algorithm is profitable";
            }
            else
            {
                StartMiningOnDevices(device);
            }

            RefreshDeviceListView?.Invoke(null, null);

            return (started, failReason);
        }

        public static async Task<(bool stopped, string failReason)> StopAllDevice() {
            // TODO when starting and stopping we are not taking Pending and Error states into account
            var devicesToStop = AvailableDevices.Devices.Where(dev => dev.State == DeviceState.Mining || dev.State == DeviceState.Benchmarking);
            if (devicesToStop.Count() == 0)
            {
                return (false, "No new devices to stop");
            }

            var anyMining = devicesToStop.Any(dev => dev.State == DeviceState.Mining);
            var stopped = true;
            var failReason = "";
            var stopTasks = new List<Task<(bool stopped, string failReason)>>();
            foreach (var stopDevice in devicesToStop)
            {
                stopTasks.Add(StopDevice(stopDevice, false));
            }
            if (anyMining)
            {
                var stopTasksAndMining = new List<Task>();
                stopTasksAndMining.Add(UpdateDevicesToMineTask());
                foreach (var t in stopTasks) stopTasksAndMining.Add(t);
                await Task.WhenAll(stopTasksAndMining);
            }
            else
            {
                await Task.WhenAll(stopTasks);
            }
            
            var stopedDeviceTasks = devicesToStop.Zip(stopTasks, (dev, task) => new { Device = dev, Task = task });
            foreach (var pair in stopedDeviceTasks)
            {
                var (deviceStopped, deviceFailReason) = pair.Task.Result;
                stopped &= deviceStopped;
                if (!deviceStopped)
                {
                    failReason += $"{pair.Device.Name} {deviceFailReason};";
                }
            }
            return (stopped, failReason);
        }

        public static bool StopSingleDevicePublic(ComputeDevice device)
        {
            if (device.IsPendingChange) return false;
            StopDevice(device);
            return true;
        }

        internal static async Task<(bool stopped, string failReason)> StopDevice(ComputeDevice device, bool executeStop = true)
        {
            // we can only stop a device it is mining or benchmarking
            switch (device.State)
            {
                case DeviceState.Stopped:
                    return (false, $"Device {device.Uuid} already stopped");
                case DeviceState.Benchmarking:
                    await BenchmarkManager.StopBenchmarForDevice(device); // TODO benchmarking is in a Task
                    return (true, "");
                case DeviceState.Mining:
                    StopMiningOnDevices(device);
                    if (executeStop)
                    {
                        await UpdateDevicesToMineTask();
                    }
                    return (true, "");
                default:
                    return (false, $"Cannot handle state {device.State.ToString()} for device {device.Uuid}");
            }
        }
    }
}
