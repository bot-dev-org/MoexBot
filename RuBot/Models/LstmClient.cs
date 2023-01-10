using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Text;

namespace RuBot.Models.Indicators
{
    public class LstmClient
    {
        private readonly byte[] _getLastProcessedTimeCommand = BitConverter.GetBytes(0);
        private readonly byte[] _processCandleCommand = BitConverter.GetBytes(1);
        private readonly byte[] _getDealsCommand = BitConverter.GetBytes(2);
        private readonly byte[] _saveCommand = BitConverter.GetBytes(3);
        private readonly byte[] _getSkipValueCommand = BitConverter.GetBytes(4);
        private readonly byte[] _getVolumeCommand = BitConverter.GetBytes(5);
        private readonly byte[] _setVolumeCommand = BitConverter.GetBytes(6);
        private readonly byte[] _getLastValueCommand = BitConverter.GetBytes(7);
        private readonly NamedPipeClientStream _clientStream;
        private readonly object _syncObj = new object();

        public int LastValue { get; private set; }

        public LstmClient(string pipeName)
        {
            _clientStream = new NamedPipeClientStream(".",
                             pipeName,
                             PipeDirection.InOut,
                             PipeOptions.WriteThrough);
            _clientStream.Connect();
            _clientStream.ReadMode = PipeTransmissionMode.Byte;
        }

        ~LstmClient()
        {
            _clientStream.Close();
        }

        public int Predict(double value, double closePrice, DateTime time, int volume, string ticker, int timeframe, double skipCoeff, bool saveParams = true)
        {
            lock (_syncObj)
            {
                var valueBytes = BitConverter.GetBytes(Convert.ToSingle(value));
                var closeBytes = BitConverter.GetBytes(Convert.ToSingle(closePrice));
                var timeBytes = Encoding.ASCII.GetBytes(time.ToString("yyyyMMddHHmmss"));
                var volumeBytes = BitConverter.GetBytes(volume);
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var saveParamsBytes = BitConverter.GetBytes(saveParams ? 1 : 0);
                var messageBytes = new byte[_processCandleCommand.Length + valueBytes.Length + closeBytes.Length + timeBytes.Length + volumeBytes.Length + 
                    timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length + saveParamsBytes.Length];
                var startIndex = 0;
                Array.Copy(_processCandleCommand, 0, messageBytes, startIndex, _processCandleCommand.Length);
                startIndex += _processCandleCommand.Length;
                Array.Copy(valueBytes, 0, messageBytes, startIndex, valueBytes.Length);
                startIndex += valueBytes.Length;
                Array.Copy(closeBytes, 0, messageBytes, startIndex, closeBytes.Length);
                startIndex += closeBytes.Length;
                Array.Copy(timeBytes, 0, messageBytes, startIndex, timeBytes.Length);
                startIndex += timeBytes.Length;
                Array.Copy(volumeBytes, 0, messageBytes, startIndex, volumeBytes.Length);
                startIndex += volumeBytes.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(saveParamsBytes, 0, messageBytes, startIndex, saveParamsBytes.Length);
                startIndex += saveParamsBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);
                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                _clientStream.Read(valueBytes, 0, valueBytes.Length);
                LastValue = BitConverter.ToInt32(valueBytes, 0);
                return LastValue;
            }
        }

        public void Save(string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var messageBytes = new byte[_getLastProcessedTimeCommand.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_saveCommand, 0, messageBytes, startIndex, _saveCommand.Length);
                startIndex += _saveCommand.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                var resArray = new byte[4];
                _clientStream.Read(resArray, 0, resArray.Length);
                BitConverter.ToInt32(resArray, 0);
            }
        }

        public Dictionary<DateTime, int> GetDeals(string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var messageBytes = new byte[_getDealsCommand.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_getDealsCommand, 0, messageBytes, startIndex, _getDealsCommand.Length);
                startIndex += _getDealsCommand.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                var resArray = new byte[262144];
                var resLength = _clientStream.Read(resArray, 0, resArray.Length);
                var res = new Dictionary<DateTime, int>();
                for (var i = 0; i < resLength; i += 15)
                {
                    var time = DateTime.ParseExact(Encoding.UTF8.GetString(resArray, i, 14), "dd/MM/yyHHmmss",
                        CultureInfo.InvariantCulture);
                    int direction = (sbyte) resArray[i + 14];
                    if (!res.ContainsKey(time))
                        res.Add(time, direction);
                }
                return res;
            }
        }

        public DateTime GetLastProcessedTime(string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var messageBytes = new byte[_getLastProcessedTimeCommand.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_getLastProcessedTimeCommand, 0, messageBytes, startIndex, _getLastProcessedTimeCommand.Length);
                startIndex += _getLastProcessedTimeCommand.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                var resArray = new byte[14];
                var resLength = _clientStream.Read(resArray, 0, resArray.Length);
                return DateTime.ParseExact(Encoding.UTF8.GetString(resArray, 0, resLength), "dd/MM/yyHHmmss",
                    CultureInfo.InvariantCulture);
            }
        }
        public int GetVolume(string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var messageBytes = new byte[_getVolumeCommand.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_getVolumeCommand, 0, messageBytes, startIndex, _getVolumeCommand.Length);
                startIndex += _getVolumeCommand.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                var resArray = new byte[4];
                _clientStream.Read(resArray, 0, resArray.Length);
                return BitConverter.ToInt32(resArray, 0);
            }
        }

        public void SetVolume(int volume, string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var volumeBytes = BitConverter.GetBytes(volume);
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker);
                var messageBytes = new byte[_setVolumeCommand.Length + volumeBytes.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_setVolumeCommand, 0, messageBytes, startIndex, _setVolumeCommand.Length);
                startIndex += _setVolumeCommand.Length;
                Array.Copy(volumeBytes, 0, messageBytes, startIndex, volumeBytes.Length);
                startIndex += volumeBytes.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                var resArray = new byte[4];
                _clientStream.Read(resArray, 0, resArray.Length);
                var res = BitConverter.ToInt32(resArray, 0);
            }
        }

        public string GetMetadata()
        {
            lock (_syncObj)
            {
                _clientStream.Write(_getSkipValueCommand, 0, _getSkipValueCommand.Length);
                var resArray = new byte[2048];
                var resLength = _clientStream.Read(resArray, 0, resArray.Length);
                return Encoding.UTF8.GetString(resArray, 0, resLength);
            }
        }

        public int GetLastValue(string ticker, int timeframe, double skipCoeff)
        {
            lock (_syncObj)
            {
                var timeFrameBytes = BitConverter.GetBytes(timeframe);
                var skipCoeffBytes = BitConverter.GetBytes((float)skipCoeff);
                var tickerBytes = Encoding.ASCII.GetBytes(ticker.ToLower());
                var messageBytes = new byte[_getVolumeCommand.Length + timeFrameBytes.Length + skipCoeffBytes.Length + tickerBytes.Length];
                var startIndex = 0;
                Array.Copy(_getLastValueCommand, 0, messageBytes, startIndex, _getLastValueCommand.Length);
                startIndex += _getVolumeCommand.Length;
                Array.Copy(timeFrameBytes, 0, messageBytes, startIndex, timeFrameBytes.Length);
                startIndex += timeFrameBytes.Length;
                Array.Copy(skipCoeffBytes, 0, messageBytes, startIndex, skipCoeffBytes.Length);
                startIndex += skipCoeffBytes.Length;
                Array.Copy(tickerBytes, 0, messageBytes, startIndex, tickerBytes.Length);

                _clientStream.Write(messageBytes, 0, messageBytes.Length);
                _clientStream.Read(timeFrameBytes, 0, timeFrameBytes.Length);
                LastValue = BitConverter.ToInt32(timeFrameBytes, 0);
                return LastValue;
            }
        }

    }
}
