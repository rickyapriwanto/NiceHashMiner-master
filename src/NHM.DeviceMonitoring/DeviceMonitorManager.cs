﻿using NHM.Common;
using NHM.Common.Device;
using NHM.DeviceMonitoring.AMD;
using NHM.DeviceMonitoring.NVIDIA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NHM.DeviceMonitoring
{
    public static class DeviceMonitorManager
    {
        public static bool DisableDeviceStatusMonitoring { get; set; } = false;
        public static bool DisableDevicePowerModeSettings { get; set; } = false;
        public static Task<List<DeviceMonitor>> GetDeviceMonitors(IEnumerable<BaseDevice> devices, bool isDCHDriver)
        {
            return Task.Run(() => {
                var ret = new List<DeviceMonitor>();

                var cpus = devices.Where(dev => dev is CPUDevice).Cast<CPUDevice>().ToList();
                var amds = devices.Where(dev => dev is AMDDevice).Cast<AMDDevice>().ToList();
                var nvidias = devices.Where(dev => dev is CUDADevice).Cast<CUDADevice>().ToList();

                foreach (var cpu in cpus)
                {
                    ret.Add(new DeviceMonitorCPU(cpu.UUID));
                }
                if (amds.Count > 0)
                {
                    var amdBusIdAndUuids = amds.ToDictionary(amd => amd.PCIeBusID, amd => amd.UUID);
                    var (_, amdInfos) = QueryAdl.TryQuery(amdBusIdAndUuids);
                    foreach (var amd in amds)
                    {
                        var currentAmdInfos = amdInfos.Where(info => info.BusID == amd.PCIeBusID);
                        ret.Add(new DeviceMonitorAMD(amd.UUID, amd.PCIeBusID, currentAmdInfos.ToArray()));
                    }
                }
                if (nvidias.Count > 0)
                {
                    var initialNvmlRestartTimeWait = Math.Min(500 * nvidias.Count, 5000); // 500ms per GPU or initial MAX of 5seconds
                    var firstMaxTimeoutAfterNvmlRestart = TimeSpan.FromMilliseconds(initialNvmlRestartTimeWait);
                    var nvidiaUUIDAndBusIds = nvidias.ToDictionary(nvidia => nvidia.UUID, nvidia => nvidia.PCIeBusID);
                    NvidiaMonitorManager.Init(nvidiaUUIDAndBusIds, isDCHDriver && UseNvmlFallback.Enabled);
                    foreach(var nvidia in nvidias)
                    {
                        var deviceMonitorNVIDIA = new DeviceMonitorNVIDIA(nvidia.UUID, nvidia.PCIeBusID, firstMaxTimeoutAfterNvmlRestart);
                        ret.Add(deviceMonitorNVIDIA);
                    }
                }

                return ret;
            });
        }
    }
}
