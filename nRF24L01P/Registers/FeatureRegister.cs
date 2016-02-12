﻿using Windows.Devices.Radios.nRF24L01P.Interfaces;

namespace Windows.Devices.Radios.nRF24L01P.Registers
{
    /// <summary>
    ///   Feature Register
    /// </summary>
    public class FeatureRegister : RegisterBase
    {
        public FeatureRegister(ICommandProcessor commandProcessor) : base(commandProcessor, 1, RegisterAddresses.FEATURE)
        {

        }

        public bool EN_DPL
        {
            get { return GetBoolProperty(PropertyMasks.EN_DPL); }
            set { SetBoolProperty(PropertyMasks.EN_DPL, value); }
        }

        public bool EN_ACK_PAY
        {
            get { return GetBoolProperty(PropertyMasks.EN_ACK_PAY); }
            set { SetBoolProperty(PropertyMasks.EN_ACK_PAY, value); }
        }

        public bool EN_DYN_ACK
        {
            get { return GetBoolProperty(PropertyMasks.EN_DYN_ACK); }
            set { SetBoolProperty(PropertyMasks.EN_DYN_ACK, value); }
        }

    }
}
