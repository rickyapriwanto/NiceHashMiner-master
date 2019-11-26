﻿using NHM.Common.Enums;
using NHMCore.Configs;
using NHMCore.Mining;
using NHMCore.Utils;
using System;
using System.ComponentModel;
using System.Linq;

namespace NHMCore
{
    public class MiningState : INotifyPropertyChanged
    {
        public static MiningState Instance { get; } = new MiningState();

        private MiningState()
        {
            _boolProps = new NotifyPropertyChangedHelper<bool>(NotifyPropertyChanged);
            IsDemoMining = false;
            IsCurrentlyMining = false;
        }

        // auto properties don't trigger NotifyPropertyChanged so add this shitty boilerplate
        private readonly NotifyPropertyChangedHelper<bool> _boolProps;


        public bool IsDemoMining
        {
            get => _boolProps.Get(nameof(IsDemoMining));
            private set => _boolProps.Set(nameof(IsDemoMining), value);
        }

        public bool AnyDeviceStopped
        {
            get => _boolProps.Get(nameof(AnyDeviceStopped));
            private set => _boolProps.Set(nameof(AnyDeviceStopped), value);
        }

        public bool AnyDeviceRunning
        {
            get => _boolProps.Get(nameof(AnyDeviceRunning));
            private set => _boolProps.Set(nameof(AnyDeviceRunning), value);
        }

        public bool IsNotBenchmarkingOrMining
        {
            get => _boolProps.Get(nameof(IsNotBenchmarkingOrMining));
            private set => _boolProps.Set(nameof(IsNotBenchmarkingOrMining), value);
        }

        public bool IsCurrentlyMining
        {
            get => _boolProps.Get(nameof(IsCurrentlyMining));
            private set => _boolProps.Set(nameof(IsCurrentlyMining), value);
        }

        public bool MiningManuallyStarted { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        // poor mans way
        public void CalculateDevicesStateChange()
        {
            AnyDeviceStopped = AvailableDevices.Devices.Any(dev => dev.State == DeviceState.Stopped && (dev.State != DeviceState.Disabled));
            AnyDeviceRunning = AvailableDevices.Devices.Any(dev => dev.State == DeviceState.Mining || dev.State == DeviceState.Benchmarking);
            IsNotBenchmarkingOrMining = !AnyDeviceRunning;
            IsCurrentlyMining = AnyDeviceRunning;
            IsDemoMining = !ConfigManager.CredentialsSettings.IsCredentialsValid && IsCurrentlyMining;
            if (IsNotBenchmarkingOrMining) MiningManuallyStarted = false;
        }
    }
}
