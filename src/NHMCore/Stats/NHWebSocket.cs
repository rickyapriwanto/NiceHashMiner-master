﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHM.Common.Enums;
using NHM.DeviceMonitoring.TDP;
using NHMCore.Mining;
using NHMCore.Stats.Models;
using NHMCore.Switching;
using NHMCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
// static imports
using static NHMCore.Stats.StatusCodes;
using NHLog = NHM.Common.Logger;

namespace NHMCore.Stats
{
    static class NHWebSocket
    {
        #region locking
        
        private static readonly object _lock = new object();
        private class LockingProperty<T>
        {
            public LockingProperty(T v)
            {
                _value = v;
            }

            private T _value;
            public T Value
            {
                get
                {
                    lock (_lock)
                    {
                        return _value;
                    }
                }
                set
                {
                    lock (_lock)
                    {
                        _value = value;
                    }
                }
            }
        }
        static private LockingProperty<DateTime> _lastSendMinerStatusTimestamp = new LockingProperty<DateTime>(DateTime.MinValue);
        static private LockingProperty<DateTime?> _notifyMinerStatusAfter = new LockingProperty<DateTime?>(null);
        static private LockingProperty<bool> _isInRPC = new LockingProperty<bool>(false);

        #endregion locking

        private enum MessageType
        {
            CLOSE_WEBSOCKET = 0,
            SEND_MESSAGE,
            SEND_MESSAGE_STATUS,
        }

        private class NHSendMessage
        {
            public MessageType Type { get; private set; }
            public string Msg { get; private set; }
            public NHSendMessage(MessageType type, string msg)
            {
                Type = type;
                Msg = msg;
            }
        }

        static private bool _isNhmwsRestart = false;

        static public bool IsWsAlive => _webSocket?.ReadyState == WebSocketState.Open;
        static private WebSocket _webSocket = null;
        static private string _address;

        static private readonly LoginMessage _login = new LoginMessage
        {
            version = "NHML/" + Application.ProductVersion,
            protocol = 3
        };

        static private ConcurrentQueue<MessageEventArgs> _recieveQueue { get; set; } = new ConcurrentQueue<MessageEventArgs>();
        static private ConcurrentQueue<IEnumerable<NHSendMessage>> _sendQueue { get; set; } = new ConcurrentQueue<IEnumerable<NHSendMessage>>();


        public static void NotifyStateChanged()
        {
            // check if we are in RPC and if not send miner status
            if (!_isInRPC.Value)
            {
                _notifyMinerStatusAfter.Value = DateTime.UtcNow.AddSeconds(1);
            }
        }

        static public async Task Start(string address, CancellationToken token)
        {
            var random = new Random();
            _address = address;
            NHLog.Info("NHWebSocket-WD", "Starting nhmws watchdog");
            NHSmaData.InitializeIfNeeded();
            // TODO use this or just use the application exit source
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await NewConnection(token);
                }
                catch (Exception e)
                {
                    NHLog.Error("NHWebSocket-WD", $"Error occured: {e.Message}");
                }
                // if we didn't initialize the restart delay reconnect
                if (!_isNhmwsRestart && !token.IsCancellationRequested)
                {
                    // delays re-connect 10 to 30 seconds
                    var delaySeconds = 10 + random.Next(0, 20);
                    NHLog.Info("NHWebSocket-WD", $"Attempting reconnect in {delaySeconds} seconds");
                    await Task.Delay(delaySeconds * 1000, token);
                }
                else if (_isNhmwsRestart && !token.IsCancellationRequested)
                {
                    NHLog.Info("NHWebSocket-WD", $"Restarting nhmws SESSION");
                }
            }
            NHLog.Info("NHWebSocket-WD", "Ending nhmws watchdog");
        }

        // TODO add cancelation token
        static private async Task NewConnection(CancellationToken stop)
        {
            NHLog.Info("NHWebSocket", "STARTING nhmws SESSION");
            try
            {
                // TODO if we fill the Queue before this we should start fresh afterwards
                // TODO think if we might want to dump prev data????
                // on each new connection clear the ConcurrentQueues, 
                _recieveQueue = new ConcurrentQueue<MessageEventArgs>();
                _sendQueue = new ConcurrentQueue<IEnumerable<NHSendMessage>>();
                _isNhmwsRestart = false;
                _notifyMinerStatusAfter.Value = null;

                NHLog.Info("NHWebSocket", "Creating socket");
                using (_webSocket = new WebSocket(_address, true))
                {
                    //stop.Register(() => _webSocket.Close(CloseStatusCode.Normal, "Closing CancellationToken"));
                    _webSocket.OnOpen += Login;
                    _webSocket.OnMessage += (s, eMsg) => _recieveQueue.Enqueue(eMsg);
                    _webSocket.OnError += (s, e) => NHLog.Info("NHWebSocket", $"Error occured: {e.Message}");
                    _webSocket.OnClose += (s, e) => NHLog.Info("NHWebSocket", $"Connection closed code {e.Code}: {e.Reason}"); ;
                    _webSocket.Log.Level = LogLevel.Debug;
                    _webSocket.Log.Output = (data, s) => NHLog.Info("NHWebSocket", data.ToString());
                    _webSocket.EnableRedirection = true;

                    NHLog.Info("NHWebSocket", "Connecting");
                    _webSocket.Connect();

                    const int minerStatusTickSeconds = 45;
                    var checkWaitTime = TimeSpan.FromMilliseconds(50);

                    var skipMinerStatus = !CredentialValidators.ValidateBitcoinAddress(_login.btc);

                    NHLog.Info("NHWebSocket", "Starting Loop");
                    while (IsWsAlive && !stop.IsCancellationRequested)
                    {
                        if (IsWsAlive) HandleSendMessage();
                        if (IsWsAlive) await HandleReceiveMessage();
                        // TODO add here the last miner status send check
                        if (IsWsAlive) await Task.Delay(checkWaitTime); // TODO add cancelation token here

                        if (skipMinerStatus) continue;
                        var elapsedTime = DateTime.UtcNow - _lastSendMinerStatusTimestamp.Value;
                        if (elapsedTime.TotalSeconds > minerStatusTickSeconds)
                        {
                            var minerStatusJsonStr = CreateMinerStatusMessage();
                            var minerStatus = new NHSendMessage(MessageType.SEND_MESSAGE_STATUS, minerStatusJsonStr);
                            _sendQueue.Enqueue(new NHSendMessage[1] { minerStatus });
                        }
                        if (_notifyMinerStatusAfter.Value.HasValue && DateTime.UtcNow >= _notifyMinerStatusAfter.Value.Value)
                        {
                            _notifyMinerStatusAfter.Value = null;
                            var minerStatusJsonStr = CreateMinerStatusMessage();
                            var minerStatus = new NHSendMessage(MessageType.SEND_MESSAGE_STATUS, minerStatusJsonStr);
                            _sendQueue.Enqueue(new NHSendMessage[1] { minerStatus });
                        }
                    }
                    // Ws closed
                    NHLog.Info("NHWebSocket", "Exited Loop");
                }
            }
            finally
            {
                NHLog.Info("NHWebSocket", "ENDING nhmws SESSION");
            }
        }

        private static void Send(string data)
        {
            NHLog.Info("NHWebSocket", $"Sending data: {data}");
            _webSocket?.Send(data);
        }

        static private void HandleSendMessage()
        {
            if (_sendQueue.TryDequeue(out var sendMsgCommands))
            {
                foreach (var msg in sendMsgCommands)
                {
                    if (msg == null) continue;

                    var data = msg.Msg;
                    switch (msg.Type)
                    {
                        case MessageType.CLOSE_WEBSOCKET:
                            _webSocket?.Close(CloseStatusCode.Normal, data);
                            _isNhmwsRestart = true;
                            break;
                        case MessageType.SEND_MESSAGE:
                        case MessageType.SEND_MESSAGE_STATUS:
                            Send(data);
                            if (MessageType.SEND_MESSAGE_STATUS == msg.Type)
                            {
                                _lastSendMinerStatusTimestamp.Value = DateTime.UtcNow;
                            }
                            break;
                        default:
                            // TODO throw if we get here
                            break;
                    }
                }
            }
        }

        static private async Task HandleReceiveMessage()
        {
            if (_recieveQueue.TryDequeue(out var recieveMsg))
            {
                await HandleMessage(recieveMsg);
            }
        }


        static private void Login(object sender, EventArgs e)
        {
            NHLog.Info("NHWebSocket", "Connected");
            try
            {
                // always send login
                var loginJson = JsonConvert.SerializeObject(_login);
                var sendMessages = new List<NHSendMessage> { new NHSendMessage(MessageType.SEND_MESSAGE, loginJson) };
                if (CredentialValidators.ValidateBitcoinAddress(_login.btc))
                {
                    var minerStatusJsonStr = CreateMinerStatusMessage(true);
                    sendMessages.Add(new NHSendMessage(MessageType.SEND_MESSAGE_STATUS, minerStatusJsonStr));
                }
                _sendQueue.Enqueue(sendMessages);
            }
            catch (Exception er)
            {
                NHLog.Info("NHWebSocket", er.Message);
            }
        }

        static public void ResetCredentials(string btc = null, string worker = null, string group = null)
        {
            // TODO check protocol
            // send status first and re-set credentials
            var minerStatusJsonStr = CreateMinerStatusMessage();
            var minerStatus = new NHSendMessage(MessageType.SEND_MESSAGE_STATUS, minerStatusJsonStr);
            _sendQueue.Enqueue(new NHSendMessage[1] { minerStatus });
            // TODO check 
            SetCredentials(btc, worker, group);
        }

        static public void SetCredentials(string btc = null, string worker = null, string group = null)
        {
            _login.rig = ApplicationStateManager.RigID;
            if (btc != null) _login.btc = btc;
            if (worker != null) _login.worker = worker;
            if (group != null) _login.group = group;
            // on credentials change always send close websocket message
            var closeMsg = new NHSendMessage(MessageType.CLOSE_WEBSOCKET, $"Credentials change reconnecting {ApplicationStateManager.Title}.");
            _sendQueue.Enqueue(new NHSendMessage[1] { closeMsg });
        }

        #region Message handling

        private static string CreateMinerStatusMessage(bool sendDeviceNames = false)
        {
            var devices = AvailableDevices.Devices;
            var rigStatus = ApplicationStateManager.CalcRigStatusString();
            var paramList = new List<JToken>
            {
                rigStatus
            };

            var deviceList = new JArray();
            foreach (var device in devices)
            {
                try
                {
                    var array = new JArray
                    {
                        sendDeviceNames ? device.Name : "",
                        device.B64Uuid  // TODO
                    };
                    var status = DeviceReportStatus(device.DeviceType, device.State);
                    array.Add(status);

                    array.Add((int)Math.Round(device.Load));

                    var speedsJson = new JArray();
                    var speeds = MiningStats.GetSpeedForDevice(device.Uuid);
                    if (speeds != null && device.State == DeviceState.Mining)
                    {
                        foreach (var kvp in speeds)
                        {
                            speedsJson.Add(new JArray((int)kvp.type, kvp.speed));
                        }
                    }
                    array.Add(speedsJson);

                    // Hardware monitoring
                    array.Add((int)Math.Round(device.Temp));
                    array.Add(device.FanSpeed);
                    array.Add((int)Math.Round(device.PowerUsage));

                    // Power mode
                    array.Add((int)device.TDPSimple);

                    // Intensity mode
                    array.Add(0);

                    deviceList.Add(array);
                }
                catch (Exception e)
                {
                    NHLog.Error("NHWebSocket", e.Message);
                }
            }

            paramList.Add(deviceList);

            var data = new MinerStatusMessage
            {
                param = paramList
            };
            var sendData = JsonConvert.SerializeObject(data);

            return sendData;
        }

        
        static private async Task HandleMessage(MessageEventArgs e)
        {
            try
            {
                if (e.IsText)
                {
                    NHLog.Info("NHWebSocket", $"Received: {e.Data}");
                    var method = GetMethod(e.Data);
                    if (IsRpcMethod(method))
                    {
                        await HandleRpcMessage(method, e.Data);
                    }
                    else
                    {
                        await HandleNonRpcMessage(method, e.Data);
                    }
                }
            }
            catch (Exception ex)
            {
                NHLog.Error("NHWebSocket", $"HandleMessage {ex.Message}");
            }
        }

        // TODO copy pasted crap from NiceHashStats
        #region NonRpcMessages
        #region SMA
        private static void SetAlgorithmRates(JArray data)
        {
            try
            {
                var payingDict = new Dictionary<AlgorithmType, double>();
                if (data != null)
                {
                    foreach (var algo in data)
                    {
                        var algoKey = (AlgorithmType)algo[0].Value<int>();
                        payingDict[algoKey] = algo[1].Value<double>();
                    }
                }

                NHSmaData.UpdateSmaPaying(payingDict);
            }
            catch (Exception e)
            {
                NHLog.Error("NHWebSocket", $"SetAlgorithmRates error: {e.Message}");
            }
        }
        private static Task HandleSMAMessage(string data)
        {
            dynamic message = JsonConvert.DeserializeObject(data);

            // Try in case stable is not sent, we still get updated paying rates
            try
            {
                JArray stable = JsonConvert.DeserializeObject(message.stable.Value);
                //SetStableAlgorithms(stable);
                var stables = stable.Select(algo => (AlgorithmType)algo.Value<int>());
                NHSmaData.UpdateStableAlgorithms(stables);
            }
            catch
            { }
            SetAlgorithmRates(message.data);
            return Task.Delay(0); // completed task
        }
        #endregion SMA
        #region MARKETS
#if DEBUG_MARKETS
        private static bool debugEU = false;
        private static bool debugUSA = false;
        private static int index = 0;
        private static void changeDebugMarkets()
        {
            if (index == 0)
            {
                debugEU = true;
                debugUSA = true;
            }
            if (index == 1)
            {
                debugEU = false;
                debugUSA = true;
            }
            if (index == 2)
            {
                debugEU = true;
                debugUSA = false;
            }
            if (index == 3)
            {
                debugEU = false;
                debugUSA = false;
            }
            index = (index + 1) % 4;
        }
#endif

        private static Task HandleMarkets(string data)
        {
            try
            {
                var markets = JsonConvert.DeserializeObject<MarketsMessage>(data);
                var hasEU = markets.data.Contains("EU");
                var hasUSA = markets.data.Contains("USA");
#if !DEBUG_MARKETS
                StratumService.SetEnabled(hasEU, hasUSA);
#else
                changeDebugMarkets();
                StratumService.SetEnabled(debugEU, debugUSA);
#endif
            }
            catch (Exception e)
            {
                NHLog.Error("NHWebSocket", $"HandleMarkets error: {e.Message}");
            }
            return Task.Delay(0);
        }
        #endregion MARKETS

        #region BALANCE
        private static Task SetBalance(string data)
        {
            try
            {
                dynamic message = JsonConvert.DeserializeObject(data);
                string balance = message.value.Value;
                if (double.TryParse(balance, NumberStyles.Float, CultureInfo.InvariantCulture, out var bal))
                {
                    ApplicationStateManager.OnBalanceUpdate(bal);
                }
            }
            catch (Exception e)
            {
                NHLog.Error("NHWebSocket", $"SetBalance error: {e.Message}");
            }
            return Task.Delay(0);
        }
        #endregion BALANCE

        #region BRUN
        private static Task HandleBurn(string data)
        {
            try
            {
                dynamic message = JsonConvert.DeserializeObject(data);
                ApplicationStateManager.Burn(message.message.Value);
            }
            catch (Exception e)
            {
                NHLog.Error("NHWebSocket", $"SetBalance error: {e.Message}");
            }
            return Task.Delay(0);
        }
        #endregion BRUN

        #region VERSION
        private static Task SetVersion(string data)
        {
            dynamic message = JsonConvert.DeserializeObject(data);
            string version = message.v3.Value;
            ApplicationStateManager.OnVersionUpdate(version);
            return Task.Delay(0);
        }
        #endregion VERSION

        #region EXCHANGE_RATES
        private static Task SetExchangeRates(string origdata)
        {
            var completed = Task.Delay(0);
            try
            {
                dynamic message = JsonConvert.DeserializeObject(origdata);
                string data = message.data.Value;
                var exchange = JsonConvert.DeserializeObject<ExchangeRateJson>(data);
                if (exchange?.exchanges_fiat == null || exchange.exchanges == null) return completed;
                foreach (var exchangePair in exchange.exchanges)
                {
                    if (!exchangePair.TryGetValue("coin", out var coin) || coin != "BTC" ||
                        !exchangePair.TryGetValue("USD", out var usd) ||
                        !double.TryParse(usd, NumberStyles.Float, CultureInfo.InvariantCulture, out var usdD))
                        continue;

                    ExchangeRateApi.UsdBtcRate = usdD;
                    break;
                }

                ExchangeRateApi.UpdateExchangesFiat(exchange.exchanges_fiat);

                // TODO check where and why we have this
                ApplicationStateManager.OnExchangeUpdated();
            }
            catch (Exception e)
            {
                NHLog.Error("NHWebSocket", $"SetExchangeRates error: {e.Message}");
            }
            return completed;
        }
        #endregion EXCHANGE_RATES

        #endregion NonRpcMessages

        static private Task HandleNonRpcMessage(string method, string data)
        {
            switch (method)
            {
                case "sma":
                    return HandleSMAMessage(data);
                case "markets":
                    return HandleMarkets(data); ;
                case "balance":
                    return SetBalance(data);
                case "versions":
                    return SetVersion(data);
                case "burn":
                    return HandleBurn(data);
                case "exchange_rates":
                    return SetExchangeRates(data);
            }
            throw new Exception($"NonRpcMessage operation not supported for method '{method}'");
        }

        #region RpcMessages

        private class RpcExecutionResult
        {
            public bool LoginNeeded { get; set; }
            public bool Id { get; set; }
            public bool Success { get; set; }
        }

        private static void ThrowIfWeCannotHanldeRPC()
        {
            var rigStatusPending = ApplicationStateManager.CalcRigStatus() == RigStatus.Pending;
            var formState = ApplicationStateManager.IsInBenchmarkForm() ? ". Rig is in benchmarks form" : "";
            if (ApplicationStateManager.IsInSettingsForm())
            {
                formState = ". Rig is in settings form";
            }
            if (ApplicationStateManager.IsInPluginsForm())
            {
                formState = ". Rig is in plugins form";
            }
            if (ApplicationStateManager.IsInUpdateForm())
            {
                formState = ". Rig is in update form";
            }
            // throw if pending
            if (rigStatusPending)
            {
                throw new RpcException($"Cannot handle RPC call Rig is in PENDING state{formState}", ErrorCode.UnableToHandleRpc);
            }
        }


        #region Credentials setters (btc/username, worker, group)
        private static async Task<bool> miningSetUsername(string btc)
        {
            var userSetResult = await ApplicationStateManager.SetBTCIfValidOrDifferent(btc, true);
            switch (userSetResult)
            {
                case ApplicationStateManager.SetResult.INVALID:
                    throw new RpcException("Bitcoin address invalid", ErrorCode.InvalidUsername);
                case ApplicationStateManager.SetResult.CHANGED:
                    // we return executed
                    break;
                case ApplicationStateManager.SetResult.NOTHING_TO_CHANGE:
                    throw new RpcException($"Nothing to change btc \"{btc}\" already set", ErrorCode.RedundantRpc);
            }
            return true;
        }

        private static async Task<bool> miningSetWorker(string worker)
        {
            var workerSetResult = await ApplicationStateManager.SetWorkerIfValidOrDifferent(worker, true);
            switch (workerSetResult)
            {
                case ApplicationStateManager.SetResult.INVALID:
                    throw new RpcException("Worker name invalid", ErrorCode.InvalidWorker);
                case ApplicationStateManager.SetResult.CHANGED:
                    // we return executed
                    break;
                case ApplicationStateManager.SetResult.NOTHING_TO_CHANGE:
                    throw new RpcException($"Nothing to change worker name \"{worker}\" already set", ErrorCode.RedundantRpc);
            }
            return true;
        }

        private static bool miningSetGroup(string group)
        {
            var groupSetResult = ApplicationStateManager.SetGroupIfValidOrDifferent(group, true);
            switch (groupSetResult)
            {
                case ApplicationStateManager.SetResult.INVALID:
                    // TODO error code not correct
                    throw new RpcException("Group name invalid", ErrorCode.UnableToHandleRpc);
                case ApplicationStateManager.SetResult.CHANGED:
                    // we return executed
                    break;
                case ApplicationStateManager.SetResult.NOTHING_TO_CHANGE:
                    throw new RpcException($"Nothing to change group \"{group}\" already set", ErrorCode.RedundantRpc);
            }
            return true;
        }
        #endregion Credentials setters (btc/username, worker, group)

        private static async Task<bool> SetDevicesEnabled(string devs, bool enabled)
        {
            bool allDevices = devs == "*";
            // get device with uuid if it exists, devs can be single device uuid
            var deviceWithUUID = AvailableDevices.GetDeviceWithUuidOrB64Uuid(devs);

            // Check if RPC should execute
            // check if redundant rpc
            if (allDevices && enabled && ApplicationStateManager.IsEnableAllDevicesRedundantOperation())
            {
                throw new RpcException("All devices are already enabled.", ErrorCode.RedundantRpc);
            }
            // all disable
            if (allDevices && !enabled && ApplicationStateManager.IsDisableAllDevicesRedundantOperation())
            {
                throw new RpcException("All devices are already disabled.", ErrorCode.RedundantRpc);
            }
            // if single and doesn't exist
            if (!allDevices && deviceWithUUID == null)
            {
                throw new RpcException("Device not found", ErrorCode.NonExistentDevice);
            }
            // if we have the device but it is redundant
            if (!allDevices && deviceWithUUID.IsDisabled == !enabled)
            {
                var stateStr = enabled ? "enabled" : "disabled";
                throw new RpcException($"Devices with uuid {devs} is already {stateStr}.", ErrorCode.RedundantRpc);
            }

            // if got here than we can execute the call
            await ApplicationStateManager.SetDeviceEnabledState(null, (devs, enabled));
            // TODO invoke the event for controls that use it
            //OnDeviceUpdate?.Invoke(null, new DeviceUpdateEventArgs(AvailableDevices.Devices.ToList()));
            return true;
        }

        #region Start
        private static async Task<bool> startMiningAllDevices()
        {
            var allDisabled = AvailableDevices.Devices.All(dev => dev.IsDisabled);
            if (allDisabled)
            {
                throw new RpcException("All devices are disabled cannot start", ErrorCode.DisabledDevice);
            }
            var (success, msg) = ApplicationStateManager.StartAllAvailableDevices();
            if (!success)
            {
                throw new RpcException(msg, ErrorCode.RedundantRpc);
            }
            await ApplicationStateManager.StartMiningTaskWait();
            return true;
        }

        private static async Task<bool> startMiningOnDeviceWithUuid(string uuid)
        {
            string errMsgForUuid = $"Cannot start device with uuid {uuid}";
            // get device with uuid if it exists, devs can be single device uuid
            var deviceWithUUID = AvailableDevices.GetDeviceWithUuidOrB64Uuid(uuid);
            if (deviceWithUUID == null)
            {
                throw new RpcException($"{errMsgForUuid}. Device not found.", ErrorCode.NonExistentDevice);
            }
            if (deviceWithUUID.IsDisabled)
            {
                throw new RpcException($"{errMsgForUuid}. Device is disabled.", ErrorCode.DisabledDevice);
            }
            var (success, msg) = ApplicationStateManager.StartDevice(deviceWithUUID);
            if (!success)
            {
                // TODO this can also be an error
                throw new RpcException($"{errMsgForUuid}. {msg}.", ErrorCode.RedundantRpc);
            }
            await ApplicationStateManager.StartMiningTaskWait();
            return true;
        }

        private static Task<bool> StartMining(string devs)
        {
            bool allDevices = devs == "*";
            if (allDevices)
            {
                return startMiningAllDevices();
            }
            else
            {
                return startMiningOnDeviceWithUuid(devs);
            }
        }
        #endregion Start

        #region Stop
        private static async Task<bool> stopMiningAllDevices()
        {
            var allDisabled = AvailableDevices.Devices.All(dev => dev.IsDisabled);
            if (allDisabled)
            {
                throw new RpcException("All devices are disabled cannot stop", ErrorCode.DisabledDevice);
            }
            var (success, msg) = await ApplicationStateManager.StopAllDevice();
            if (!success)
            {
                throw new RpcException(msg, ErrorCode.RedundantRpc);
            }
            return success;
        }

        private static async Task<bool> stopMiningOnDeviceWithUuid(string uuid)
        {
            string errMsgForUuid = $"Cannot stop device with uuid {uuid}";
            // get device with uuid if it exists, devs can be single device uuid
            var deviceWithUUID = AvailableDevices.GetDeviceWithUuidOrB64Uuid(uuid);
            if (deviceWithUUID == null)
            {
                throw new RpcException($"{errMsgForUuid}. Device not found.", ErrorCode.NonExistentDevice);
            }
            if (deviceWithUUID.IsDisabled)
            {
                throw new RpcException($"{errMsgForUuid}. Device is disabled.", ErrorCode.DisabledDevice);
            }
            var (success, msg) = await ApplicationStateManager.StopDevice(deviceWithUUID);
            if (!success)
            {
                // TODO this can also be an error
                throw new RpcException($"{errMsgForUuid}. {msg}.", ErrorCode.RedundantRpc);
            }
            return success;
        }

        private static Task<bool> StopMining(string devs)
        {
            bool allDevices = devs == "*";
            if (allDevices)
            {
                return stopMiningAllDevices();
            }
            else
            {
                return stopMiningOnDeviceWithUuid(devs);
            }
        }
        #endregion Stop

        private static void SetPowerMode(string device, TDPSimpleType level)
        {
            var devs = device == "*" ?
                AvailableDevices.Devices :
                AvailableDevices.Devices.Where(d => d.B64Uuid == device);

            var found = devs.Count() > 0;
            var hasEnabled = false;
            var setSuccess = new List<bool>();
            foreach (var dev in devs)
            {
                if (!dev.Enabled) continue;
                if (!dev.CanSetPowerMode) continue;
                hasEnabled = true;
                // TODO check if set
                var result = dev.SetPowerMode(level);
                setSuccess.Add(result);
            }

            if (setSuccess.Any(t => t) && !setSuccess.All(t => t))
            {
                throw new RpcException("Not able to set power modes for all devices", ErrorCode.UnableToHandleRpc);
            }

            if (found && !hasEnabled)
            {
                throw new RpcException("No settable devices found", ErrorCode.UnableToHandleRpc);
            }

            if (!found)
            {
                throw new RpcException("No settable devices found", ErrorCode.UnableToHandleRpc);
            }
        }

        #endregion RpcMessages

        static private async Task HandleRpcMessage(string method, string data)
        {
            string btc = null;
            string worker = null;
            string group = null;
            int rpcId = -1;
            bool executed = false;
            bool loginNeeded = false;
            ExecutedCall executedCall = null;
            try
            {
                _isInRPC.Value = true;
                dynamic message = JsonConvert.DeserializeObject(data);
                int? rpcIdOption = (int?)message.id;
                rpcId = rpcIdOption ?? -1;

                ThrowIfWeCannotHanldeRPC();
                switch (method)
                {
                    case "mining.set.username":
                        btc = (string)message.username;
                        executed = await miningSetUsername(btc);
                        loginNeeded = executed;
                        break;
                    case "mining.set.worker":
                        worker = (string)message.worker;
                        executed = await miningSetWorker(worker);
                        loginNeeded = executed;
                        break;
                    case "mining.set.group":
                        group = (string)message.group;
                        executed = miningSetGroup(group);
                        loginNeeded = executed;
                        break;
                    case "mining.enable":
                        executed = await SetDevicesEnabled((string)message.device, true);
                        break;
                    case "mining.disable":
                        executed = await SetDevicesEnabled((string)message.device, false);
                        break;
                    case "mining.start":
                        executed = await StartMining((string)message.device);
                        break;
                    case "mining.stop":
                        executed = await StopMining((string)message.device);
                        break;
                    case "mining.set.power_mode":
                        // TODO not supported atm
                        SetPowerMode((string)message.device, (TDPSimpleType)message.power_mode);
                        break;
                    default:
                        throw new RpcException($"RpcMessage operation not supported for method '{method}'", ErrorCode.UnableToHandleRpc);
                }

                executedCall = new ExecutedCall(rpcId, 0, null);
            }
            catch (RpcException rpcEx)
            {
                executedCall = new ExecutedCall(rpcId, rpcEx.Code, rpcEx.Message);
            }
            finally
            {
                _isInRPC.Value = false;
                if (executedCall != null)
                {
                    // send miner status and send executed
                    var minerStatusMsg = CreateMinerStatusMessage();
                    Send(minerStatusMsg);
                    _lastSendMinerStatusTimestamp.Value = DateTime.UtcNow;
                    // Then executed
                    var rpcMessage = executedCall.Serialize();
                    Send(rpcMessage);
                    // Login if we have to
                    if (loginNeeded)
                    {
                        SetCredentials(btc, worker, group);
                    }
                }
            }
        }

        private static string GetMethod(string data)
        {
            dynamic message = JsonConvert.DeserializeObject(data);
            return message.method.Value as string;
        }

        private static bool IsRpcMethod(string method)
        {
            // well pretty much all RPCs start with mining.*
            switch (method)
            {
                case "mining.set.username":
                case "mining.set.worker":
                case "mining.set.group":
                case "mining.enable":
                case "mining.disable":
                case "mining.start":
                case "mining.stop":
                case "mining.set.power_mode":
                    return true;
            }
            return false;
        }
        #endregion Message handling

    }
}
