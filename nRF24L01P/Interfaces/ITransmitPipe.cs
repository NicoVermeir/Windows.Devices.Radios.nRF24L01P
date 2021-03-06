﻿namespace Windows.Devices.Radios.nRF24L01P.Interfaces
{
    public interface ITransmitPipe : IDataPipe
    {

        bool Write(byte[] data, bool disableAck = false, int timeout = 1000);
        void ReuseTransmitPayload();
    }
}