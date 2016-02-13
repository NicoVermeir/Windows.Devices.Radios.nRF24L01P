﻿using Windows.Devices.Radios.nRF24L01P.Interfaces;

namespace Windows.Devices.Radios.nRF24L01P.Registers
{
    /// <summary>
    ///   Status Register (In parallel to the SPI command word applied on the MOSI pin, the STATUS register is shifted serially out on the MISO pin)
    /// </summary>
    public class StatusRegister : RegisterBase
    {
        public StatusRegister(ICommandProcessor commandProcessor) : base(commandProcessor, 1, RegisterAddresses.STATUS)
        {
        }

        /// <summary>
        /// Receive Data Ready6
        /// </summary>
        public bool ReceiveDataReady
        {
            get { return GetBoolProperty(PropertyMasks.RX_DR); }
            set { SetBoolProperty(PropertyMasks.RX_DR, value); }
        }

        public bool TransmitDataSent
        {
            get { return GetBoolProperty(PropertyMasks.TX_DS); }
            set { SetBoolProperty(PropertyMasks.TX_DS, value); }
        }

        public bool MaximunTransmitRetries
        {
            get { return GetBoolProperty(PropertyMasks.MAX_RT); }
            set { SetBoolProperty(PropertyMasks.MAX_RT, value); }
        }

        public byte ReceiveDataPipeNumber => GetByteProperty(PropertyMasks.RX_P_NO);

        public bool TransmitFifoFull => GetBoolProperty(PropertyMasks.TX_FULL);
    }
}