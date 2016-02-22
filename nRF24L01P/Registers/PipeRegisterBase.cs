﻿using Windows.Devices.Radios.nRF24L01P.Interfaces;

namespace Windows.Devices.Radios.nRF24L01P.Registers
{
    public abstract class PipeRegisterBase : RegisterBase
    {
        /// <summary>
        /// Receive Pipe Number
        /// </summary>
        public byte PipeNumber { get; }

        protected PipeRegisterBase(ICommandProcessor commandProcessor, byte address, byte length, byte[] defaultValue, byte pipeNumber, string name = "") :
            base(commandProcessor, length, address, defaultValue, name)
        {
            PipeNumber = pipeNumber;
            Name = GetType().Name + PipeNumber + (string.IsNullOrEmpty(name) ? "" : string.Format(" ({0})", name));
        }
    }
}
