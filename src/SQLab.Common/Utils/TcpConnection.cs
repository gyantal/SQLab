//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Sockets;
//// using System.Runtime.Serialization.Formatters.Binary; no Binary formatter in DotNetCore, they will never implement it: https://github.com/dotnet/corefx/issues/6564
//using System.Threading;
//using System.Threading.Tasks;

//namespace SQCommon
//{
//    // http://stackoverflow.com/questions/3609280/sending-and-receiving-data-over-a-network-using-tcpclient
//    // Good because any 'object' class is Binary serialized, and that is how it is sent. So, an int[] array is sent very efficiently over the network. Quick.


//    public class DataArrivedEventArgs
//    {
//        private object m_arrivedData;

//        public DataArrivedEventArgs(object p_arrivedData)
//        {
//            m_arrivedData = p_arrivedData;
//        }
//    }

//    public class PCmds
//    {
//        string CommandString;
//        string CommandParameter;
//        int[] IntArray = new int[] { 5, 6, 7 };
//    }

//    public class DataToSend
//    {
//        object m_data;
//        internal byte[] ToBinary()
//        {
//            return new byte[] { 1, 2, 3 };
//        }

//        internal void FromBinary(byte[] p_bytes)
//        {
//            m_data = p_bytes;
//        }
//    }



//    public class TcpConnection : IDisposable
//    {
//        public delegate void DataArriveDelegate(TcpConnection p_conn, DataArrivedEventArgs p_dataArgs);
//        public delegate void DropDelegate(TcpConnection p_conn);


//        public event DataArriveDelegate DataArrive = delegate { };
//        public event DropDelegate Drop = delegate { };
//        //public event EvHandler<TcpConnection, DataArrivedEventArgs> DataArrive = delegate { };
//        //public event EvHandler<TcpConnection> Drop = delegate { };

//        private const int IntSize = 4;
//        private const int BufferSize = 8 * 1024;

//        private static readonly SynchronizationContext _syncContext = SynchronizationContext.Current;
//        private readonly TcpClient _tcpClient;
//        private readonly object _droppedRoot = new object();
//        private bool _dropped;
//        private byte[] _incomingData = new byte[0];
//        private Nullable<int> _objectDataLength;

//        public TcpClient TcpClient { get { return _tcpClient; } }
//        public bool Dropped { get { return _dropped; } }

//        private void DropConnection()
//        {
//            lock (_droppedRoot)
//            {
//                if (Dropped)
//                    return;

//                _dropped = true;
//            }
//#if DNX451 || NET451
//            _tcpClient.Close();
//#else
//            _tcpClient.Dispose();
//#endif
//            _syncContext.Post(delegate { Drop(this); }, null);
//        }

//        //public void SendData(PCmds pCmd) { SendDataInternal(new object[] { pCmd }); }
//        //public void SendData(PCmds pCmd, object[] datas)
//        //{
//        //    //datas.ThrowIfNull();
//        //    if (datas == null)
//        //        throw new Exception("Data is null");

//        //    var newArray = Array.Resize(ref datas)
//        //    //SendDataInternal(new object[] { pCmd }.Append(datas));
//        //}
//        private void SendDataInternal(DataToSend data)
//        {
//            if (Dropped)
//                return;

//            byte[] bytedata;
//            try
//            {
//                bytedata = data.ToBinary();
//            }
//            catch { return; }

//            //using (MemoryStream ms = new MemoryStream())
//            //{
//            //    BinaryFormatter bf = new BinaryFormatter();

//            //    try { bf.Serialize(ms, data); }
//            //    catch { return; }

//            //    bytedata = ms.ToArray();
//            //}

//            try
//            {
//                lock (_tcpClient)
//                {
//                    TcpClient.Client.BeginSend(BitConverter.GetBytes(bytedata.Length), 0, IntSize, SocketFlags.None, EndSend, null);
//                    TcpClient.Client.BeginSend(bytedata, 0, bytedata.Length, SocketFlags.None, EndSend, null);
//                }
//            }
//            catch { DropConnection(); }
//        }
//        private void EndSend(IAsyncResult ar)
//        {
//            try { TcpClient.Client.EndSend(ar); }
//            catch { }
//        }

//        public TcpConnection(TcpClient tcpClient)
//        {
//            _tcpClient = tcpClient;
//            StartReceive();
//        }

//        private void StartReceive()
//        {
//            byte[] buffer = new byte[BufferSize];

//            try
//            {
//                _tcpClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, DataReceived, buffer);
//            }
//            catch { DropConnection(); }
//        }

//        private void DataReceived(IAsyncResult ar)
//        {
//            if (Dropped)
//                return;

//            int dataRead;

//            try { dataRead = TcpClient.Client.EndReceive(ar); }
//            catch
//            {
//                DropConnection();
//                return;
//            }

//            if (dataRead == 0)
//            {
//                DropConnection();
//                return;
//            }

//            byte[] byteData = ar.AsyncState as byte[];
//            byte[] byteData2 = byteData.Take(dataRead).ToArray();
//            //_incomingData = _incomingData.Append(byteData2);
//            // Append Data: http://stackoverflow.com/questions/5958495/append-data-to-byte-array
//            // But Buffer.BlockCopy() is faster, So Change it to BlockCopy
//            // http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
//            //Array.Copy(byteData2, 0, _incomingData, _incomingData.Length - byteData2.Length, byteData2.Length);
//            Array.Resize(ref _incomingData, _incomingData.Length + byteData2.Length);
//            Buffer.BlockCopy(byteData2, 0, _incomingData, _incomingData.Length - byteData2.Length, byteData2.Length);

//            bool exitWhile = false;

//            while (exitWhile)
//            {
//                exitWhile = true;

//                if (_objectDataLength.HasValue)
//                {
//                    if (_incomingData.Length >= _objectDataLength.Value)
//                    {
//                        DataToSend data = null;
//                        try
//                        {
//                            byte[] dataSafe = new byte[_objectDataLength.Value];
//                            Buffer.BlockCopy(_incomingData, 0, dataSafe, 0, _objectDataLength.Value);
//                            data = new DataToSend();
//                            data.FromBinary(dataSafe);
//                        }
//                        catch
//                        {
//                            //SendData(PCmds.Disconnect);     // why is this here? Probably, because, if data receive is failed, it Send back to the Client that it was an ERROR. Send it again.
//                            DropConnection();
//                            return;
//                        }

//                        //BinaryFormatter bf = new BinaryFormatter();

//                        //using (MemoryStream ms = new MemoryStream(_incomingData, 0, _objectDataLength.Value))
//                        //    try { data = bf.Deserialize(ms); }
//                        //    catch
//                        //    {
//                        //        //SendData(PCmds.Disconnect);     // why is this here? Probably, because, if data receive is failed, it Send back to the Client that it was an ERROR. Send it again.
//                        //        DropConnection();
//                        //        return;
//                        //    }

//                        _syncContext.Post(delegate (object T)
//                        {
//                            try { DataArrive(this, new DataArrivedEventArgs(T)); }
//                            catch { DropConnection(); }
//                        }, data);

//                        //_incomingData = _incomingData.TrimLeft(_objectDataLength.Value);    // it removes the data
//                        Array.Resize(ref _incomingData, _incomingData.Length - _objectDataLength.Value);
//                        _objectDataLength = null;
//                        exitWhile = false;
//                    }
//                }
//                else
//                {
//                    if (_incomingData.Length >= IntSize)
//                    {
//                        byte[] dataLenghtSafe = new byte[IntSize];
//                        Buffer.BlockCopy(_incomingData, 0, dataLenghtSafe, 0, IntSize);
//                        _objectDataLength = BitConverter.ToInt32(dataLenghtSafe, 0);
//                        //_objectDataLength = BitConverter.ToInt32(_incomingData.TakeLeft(IntSize), 0);
//                        //_incomingData = _incomingData.TrimLeft(IntSize);    // it removes the 4 bytes only, that represented the size
//                        Array.Resize(ref _incomingData, _incomingData.Length - IntSize);
//                        exitWhile = false;
//                    }
//                }
//            }
//            StartReceive();
//        }


//        public void Dispose() { DropConnection(); }
//    }
//}
