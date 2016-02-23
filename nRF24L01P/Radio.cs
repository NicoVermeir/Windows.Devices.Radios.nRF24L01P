﻿using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Radios.nRF24L01P.Enums;
using Windows.Devices.Radios.nRF24L01P.Interfaces;
using Windows.Devices.Radios.nRF24L01P.Registers;

namespace Windows.Devices.Radios.nRF24L01P
{
    /// <summary>
    /// Driver for nRF24L01(+) 2.4GHz Wireless Transceiver
    /// </summary>
    public class Radio : IRadio
    {
        private readonly GpioPin _cePin;
        private readonly GpioPin _irqPin;
        private readonly ICommandProcessor _commandProcessor;
        public event EventHandler<InterruptedEventArgs> Interrupted;
        public IRegisterContainer RegisterContainer { get; }
        public IConfiguration Configuration { get; }
        public ITransmitPipe TransmitPipe { get; }
        public IReceivePipeCollection ReceivePipes { get; }

        public Radio(ICommandProcessor commandProcessor, GpioPin cePin, GpioPin irqPin = null)
        {
            _status = DeviceStatus.PowerDown;
            _cePin = cePin;
            EnableReceiver(false);

            commandProcessor.GetDeviceStatus = GetStatus;
            _commandProcessor = commandProcessor;

            RegisterContainer = new RegisterContainer(_commandProcessor);
            RegisterContainer.ResetRegisters();
            RegisterContainer.LoadRegisters();

            Configuration = new Configuration(_commandProcessor, RegisterContainer);
            TransmitPipe = new TransmitPipe(Configuration, _commandProcessor, RegisterContainer);
            ReceivePipes = new ReceivePipeCollection(Configuration, _commandProcessor, RegisterContainer);

            bool useIrq = irqPin != null;
            if (useIrq)
            {
                _irqPin = irqPin;
                _irqPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 50);
                _irqPin.ValueChanged += irqPin_ValueChanged;
            }
            ConfigurationRegister configurationRegister = RegisterContainer.ConfigurationRegister;
            configurationRegister.MaximunTransmitRetriesMask = !useIrq;
            configurationRegister.ReceiveDataReadyMask = !useIrq;
            configurationRegister.TransmitDataSentMask = !useIrq;
            configurationRegister.Save();

            Task.Delay(1).Wait();
            Status = DeviceStatus.StandBy;
        }

        private void irqPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge != GpioPinEdge.FallingEdge) return;

            RegisterContainer.StatusRegister.Load();
            Interrupted?.Invoke(this, new InterruptedEventArgs { StatusRegister = RegisterContainer.StatusRegister });
        }

        public override string ToString()
        {
            var radio = new
            {
                Status = Status.GetName(),
                TransmitFIFO = TransmitPipe.FifoStatus.GetName(),
                ReceiveFIFO = ReceivePipes.FifoStatus.GetName(),
                ReceivePipes.ReceivedPowerDetector
            };
            return string.Format("Radio\r\n{0}\r\n{1}{2}",
                JsonConvert.SerializeObject(radio, Formatting.None),
                Configuration,
                RegisterContainer);
        }

        public void Dispose()
        {
            _commandProcessor?.Dispose();
            _cePin?.Dispose();
            _irqPin?.Dispose();
        }

        public string GetArduinoDetails()
        {
            return new ArduinoDetails(this, Configuration, _commandProcessor, RegisterContainer).ToString();
        }

        private void EnableReceiver(bool enabled)
        {
            _cePin.Write(enabled ? GpioPinValue.High : GpioPinValue.Low);

            // Must allow the radio time to settle else configuration bits will not necessarily stick.
            // This is actually only required following power up but some settling time also appears to
            // be required after resets too. For full coverage, we'll always assume the worst.
            // Enabling 16b CRC is by far the most obvious case if the wrong timing is used - or skipped.
            // Technically we require 4.5ms + 14us as a worst case. We'll just call it 5ms for good measure.
            // WARNING: Delay is based on P-variant whereby non-P *may* require different timing.
            Task.Delay(5).Wait();
        }
     
        private DeviceStatus GetStatus() { return _status; }
        private DeviceStatus _status;
        public DeviceStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value == _status)
                    return;
                DeviceStatus lastStatus = _status;
                _status = value;
                switch (_status)
                {
                    case DeviceStatus.Undefined:
                        throw new InvalidOperationException("Undefined Device Status");
                    case DeviceStatus.PowerDown:
                        if (lastStatus == DeviceStatus.StandBy)
                        {
                            RegisterContainer.ConfigurationRegister.PowerUp = false;
                            RegisterContainer.ConfigurationRegister.Save();
                            break;
                        }
                        throw new InvalidOperationException("Error status change, PowerDown should from StandBy mode only");
                    case DeviceStatus.StandBy:
                        if (lastStatus == DeviceStatus.ReceiveMode || lastStatus == DeviceStatus.TransmitMode)
                        {
                            EnableReceiver(false);
                            break;
                        }
                        if (lastStatus == DeviceStatus.PowerDown)
                        {
                            RegisterContainer.ConfigurationRegister.PowerUp = true;
                            RegisterContainer.ConfigurationRegister.Save();
                            Task.Delay(2).Wait();
                            break;
                        }
                        throw new InvalidOperationException("Error status change, StandBy should from PowerDown,TX or RX mode only");
                    case DeviceStatus.TransmitMode:
                        if (lastStatus == DeviceStatus.StandBy)
                        {
                            bool checkStatus = _commandProcessor.CheckStatus;
                            _commandProcessor.CheckStatus = false;
                            RegisterContainer.ConfigurationRegister.PrimaryReceiveMode = false;
                            RegisterContainer.ConfigurationRegister.Save();
                            _commandProcessor.CheckStatus = checkStatus;

                            EnableReceiver(true);
                            break;
                        }
                        throw new InvalidOperationException("Error status change, RXMode should from Standby mode only");
                    case DeviceStatus.ReceiveMode:
                        if (lastStatus == DeviceStatus.StandBy)
                        {
                            bool checkStatus = _commandProcessor.CheckStatus;
                            _commandProcessor.CheckStatus = false;
                            RegisterContainer.ConfigurationRegister.PrimaryReceiveMode = true;
                            RegisterContainer.ConfigurationRegister.Save();
                            _commandProcessor.CheckStatus = checkStatus;

                            EnableReceiver(true);
                            break;
                        }
                        throw new InvalidOperationException("Error status change, RXMode should from Standby mode only");
                }
            }
        }
    }
}
