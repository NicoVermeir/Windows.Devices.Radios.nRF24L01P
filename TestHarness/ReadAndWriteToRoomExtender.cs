﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Radios.nRF24L01P;
using Windows.Devices.Radios.nRF24L01P.Extensions;
using Windows.Devices.Radios.nRF24L01P.Interfaces;
using Windows.Devices.Radios.nRF24L01P.Roles;
using Windows.Devices.Spi;
using Windows.System.Threading;

namespace nRF24L01P.TestHarness
{
    internal class ReadAndWriteToRoomExtender : IReadAndWriteProcess
    {
        private Radio _radio;
        private SenderReceiverRole _sendReceiveRole;
        private readonly ManualResetEvent _manualResetEvent;
        private readonly object _syncRoot;
        private readonly TaskFactory _taskFactory;

        public byte[] SendAddress { get; set; }
        //public string SendAddressString
        //{
        //    get { return Encoding.UTF8.GetString(SendAddress); }
        //    set
        //    {
        //        IBuffer buffer = new Windows.Storage.Streams.Buffer(5);
        //        Encoding.UTF8.GetBytes(value).CopyTo(0, buffer, 0, 5);
        //        SendAddress = buffer.ToArray();
        //    }
        //}

        //public IPAddress SendIPAddress
        //{
        //    get
        //    {
        //        return new IPAddress(new[] { SendAddress[0], SendAddress[1], SendAddress[2], SendAddress[3] });
        //    }
        //    set
        //    {
        //        value.GetAddressBytes().CopyTo(SendAddress, 0);
        //        SendAddress[4] = 0;
        //    }
        //}

        public byte[] ReceiveAddress { get; set; }
        //public string ReceiveAddressString
        //{
        //    get { return Encoding.UTF8.GetString(ReceiveAddress); }
        //    set
        //    {
        //        IBuffer buffer = new Windows.Storage.Streams.Buffer(5);
        //        Encoding.UTF8.GetBytes(value).CopyTo(0, buffer, 0, 5);
        //        ReceiveAddress = buffer.ToArray();
        //    }
        //}

        //public IPAddress ReceiveIPAddress
        //{
        //    get
        //    {
        //        return new IPAddress(new[] { ReceiveAddress[0], ReceiveAddress[1], ReceiveAddress[2], ReceiveAddress[3] });
        //    }
        //    set
        //    {
        //        value.GetAddressBytes().CopyTo(ReceiveAddress, 0);
        //        ReceiveAddress[4] = 0;
        //    }
        //}
        private byte _foo;

        public ReadAndWriteToRoomExtender()
        {
            _foo = 65;
            SendAddress = new byte[] { 0x54, 0x4d, 0x52, 0x68, 0x7C };
            ReceiveAddress = new byte[] { 0xAB, 0xCD, 0xAB, 0xCD, 0x71 };

            _manualResetEvent = new ManualResetEvent(false);
            _syncRoot = new object();

            _taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void Dispose()
        {
            _radio.Dispose();
            _radio = null;
        }

        public void Initialize()
        {
            GpioController gpioController = GpioController.GetDefault();
            GpioPin powerPin = gpioController.InitGpioPin(4, GpioPinDriveMode.Output, GpioSharingMode.Exclusive);//4
            GpioPin cePin = gpioController.InitGpioPin(22, GpioPinDriveMode.Output, GpioSharingMode.Exclusive);//22
            GpioPin irqPin = gpioController.InitGpioPin(25, GpioPinDriveMode.InputPullUp, GpioSharingMode.Exclusive);//25
            powerPin.Write(GpioPinValue.Low);
            cePin.Write(GpioPinValue.Low);
            irqPin.Write(GpioPinValue.High);

            DeviceInformationCollection devicesInfo = DeviceInformation.FindAllAsync(SpiDevice.GetDeviceSelector("SPI0")).GetAwaiter().GetResult();
            SpiDevice spiDevice = SpiDevice.FromIdAsync(devicesInfo[0].Id, new SpiConnectionSettings(0)).GetAwaiter().GetResult();
            ICommandProcessor commandProcessor = new CommandProcessor(spiDevice);

            _radio = new Radio(commandProcessor, powerPin, cePin, irqPin);
            _sendReceiveRole = new SenderReceiverRole();
            _sendReceiveRole.AttachRadio(_radio);
            _sendReceiveRole.SendAddress = SendAddress;
            _sendReceiveRole.ReceiveAddress = ReceiveAddress;
            _sendReceiveRole.Start();

            ThreadPoolTimer.CreatePeriodicTimer(Handler, TimeSpan.FromSeconds(5));
            WaitForData();
        }

        private async void Handler(ThreadPoolTimer timer)
        {
            _foo++;
            timer.Cancel();
            await _taskFactory.StartNew(() => SendX10Packet(_foo, 10, 8));
            ThreadPoolTimer.CreatePeriodicTimer(Handler, TimeSpan.FromSeconds(5));
        }

        public bool SendX10Packet(byte house, byte unit, byte command)
        {
            int count = 0;
            lock (_syncRoot)
            {
                byte[] packet = { (byte)wrPacketTypes.X10StandardRequest, house, unit, command };
                byte[] reverseBytes = packet.ReverseBytes();

                while (!_sendReceiveRole.Send(reverseBytes))
                {
                    count++;
                    Task.Delay(250).Wait();
                    if (count > 20)
                        break;
                }
            }

            return count <= 20;
        }

        public bool SendEnvironmentRequestPacket()
        {
            int count = 0;
            lock (_syncRoot)
            {
                byte[] packet = { (byte)wrPacketTypes.EnvironmentSensorRequest };
                while (!_sendReceiveRole.Send(packet.ReverseBytes()))
                {
                    count++;
                    Task.Delay(250).Wait();
                    if (count > 10)
                        break;
                }
            }
            return count <= 10;
        }

        private void WaitForData()
        {
            _manualResetEvent.Reset();
            while (true)
            {
                lock (_syncRoot)
                {
                    if (!_manualResetEvent.WaitOne(1000))
                        continue;
                    break;
                }
            }
        }

    }
}
