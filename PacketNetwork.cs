using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using MiniJSON;

namespace NIPA2
{
    public enum ENetState
    {
        None             = 0    ,
        Connecting              ,           // 연결중
        ConnectingFailed        ,           // 연결할때 실패시 ( 내부 상태값 )
        FailConnect             ,           // 연결 실패
        Connected               ,           // 연결됨
        Receiving               ,           // 연결후 응답받을 준비가 됐을때
        Disconnected            ,           // 연결 끊김
        SocketClosed            ,           // 소켓 닫힘
        MaxCount         = 255
    }

    public class SocketStateObject
    {
        public Socket workSocket = null;
        
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class ConnectStateObject
    {
        public Socket workSocket = null;

        public ENetState State = ENetState.None;
    }
    // NetManager State Callback Delegate
    public delegate void Delegator_FailConnect();
    public delegate void Delegator_Connected();
    public delegate void Delegator_Receiving();
    public delegate void Delegator_Disconnected();
    
    public class PacketNetwork
    {
        public event Delegator_FailConnect     Event_FailConnect;
        public event Delegator_Connected       Event_Connected;
        public event Delegator_Receiving       Event_Receiving;
        public event Delegator_Disconnected    Event_Disconnected;

        public const int HeaderSize = 5;

        private string ServerIP = "127.0.0.1";
        private int ServerPort = 5051;

        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone    = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        Thread Connect_Thread = null;
        Thread Socket_Thread = null;

        private Socket ClientSocket = null;
        private IPAddress ipAddress = null;
        private IPEndPoint remoteEP = null;

        private ENetState NetworkState = ENetState.None;

        private int MaxConnectCount = 2;
        private int ReConnectTimer  = 500;

        public NetPacket netPacket;

        public PacketNetwork(){
            netPacket = new NetPacket();
            
            if (PlayerConfig.HasKey("ServerIP") == false)
                PlayerConfig.SetString("ServerIP", ServerIP);
            else
                ServerIP = PlayerConfig.GetString("ServerIP", ServerIP);

            if (PlayerConfig.HasKey("ServerPort") == false)
                PlayerConfig.SetInt("ServerPort", ServerPort);
            else
                ServerPort = PlayerConfig.GetInt("ServerPort", ServerPort);
        }

		public Socket GetSocket()
        {
            return ClientSocket;
        }

        public bool ISEnableSend()
        {
            switch(GetState())
            {
                case ENetState.Connected:
                case ENetState.Receiving:
                    return true;
            }

            return false;
        }

        protected void SetState(ENetState NewState)
        {
            NetworkState = NewState;

            Debug.Log("NetmManager State " + NetworkState.ToString());
            
            switch (NetworkState)
            {
                case ENetState.FailConnect:
                    if( Event_FailConnect != null )
                    Event_FailConnect();
                    netPacket.AddEtcPacket("connectfail");
                    break;
                    
                case ENetState.Connected:
                    if (Event_Connected != null)
                        Event_Connected();
                    break;

                case ENetState.Receiving:
                    if (Event_Receiving != null)
                        Event_Receiving();
                    break;

                case ENetState.Disconnected:
                    if (Event_Disconnected != null)
                        Event_Disconnected();
                        netPacket?.AddEtcPacket("connectfail");
                    break;
                case ENetState.ConnectingFailed:
                case ENetState.SocketClosed:
                    netPacket?.AddEtcPacket("connectfail");
                    break; 
            }
        }

        public ENetState GetState()
        {
            return NetworkState;
        }


        public bool StartConnectToServer()
        {
            switch (GetState())
            {
                case ENetState.None:
                case ENetState.FailConnect:
                case ENetState.Disconnected:
                case ENetState.SocketClosed:
                    break;
                
                default:
                    Debug.Log("Can not Connect to Server");
                    return false;
            }

            ipAddress = System.Net.IPAddress.Parse(ServerIP);
            remoteEP = new IPEndPoint(ipAddress, ServerPort);

            if( null != Connect_Thread )
            {
                if (true == Connect_Thread.IsAlive)
                {
                    Debug.Log("Connect Thread is Alive");

                    return false;
                }
            }

            Connect_Thread = new Thread(ProcessConnect);

            Connect_Thread.Start();

            return true;
        }

        public bool StartReceiveFromServer()
        {
            if (null == GetSocket())
            {
                Debug.Log("Not Create ClientSocket");

                return false;
            }

            if( null != Socket_Thread )
            {
                if( true == Socket_Thread.IsAlive )
                {
                    Debug.Log("Socket Thread is Alive");

                    return false;
                }
            }

            Socket_Thread = new Thread(ProcessClient);

            Socket_Thread.Start();

            SetState(ENetState.Receiving);

            return true;
        }


        protected void ProcessConnect()
        {
            bool ISLoop = true;
            int ConnectCount = 0;

            ENetState ResultConnectState = ENetState.None;


            while (ISLoop)
            {
                try
                {
                    if( null != ClientSocket )
                    {
                        ClientSocket.Close();
                        ClientSocket = null;

                        ConnectCount++;

                        // TimeOut
                        if (ConnectCount >= MaxConnectCount )
                        {
                            ResultConnectState = ENetState.FailConnect;

                            break;
                        }

                        Thread.Sleep(ReConnectTimer);
                    }

                    ClientSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    ConnectStateObject ConnectState = new ConnectStateObject();

                    ConnectState.workSocket = ClientSocket;
                    ConnectState.State = ENetState.Connecting;


                    connectDone.Reset();
                    ClientSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), ConnectState);
                    connectDone.WaitOne();

                    if(ConnectState.State == ENetState.Connected )
                    {
                        ResultConnectState = ENetState.Connected;

                        ISLoop = false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            if (ResultConnectState == ENetState.Connected)
            {
                SetState(ENetState.Connected);
                StartReceiveFromServer();
            }
            else if (ResultConnectState == ENetState.FailConnect)
            {
                Debug.Log(ENetState.FailConnect.ToString());
                SetState(ENetState.FailConnect);
            }
        }

        public void CloseSocket()
        {
            try
            {
                if (null != GetSocket())
                {
                    GetSocket().Shutdown(SocketShutdown.Both);
                    GetSocket().Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            SetState(ENetState.SocketClosed);
        }

        public void ProcessClient()
        {
            try
            {
                Receive(GetSocket());
                receiveDone.WaitOne();

                CloseSocket();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                ConnectStateObject ConnectState = (ConnectStateObject)ar.AsyncState;

                ConnectState.State = ENetState.ConnectingFailed;

                ConnectState.workSocket.EndConnect(ar);

                ConnectState.State = ENetState.Connected;

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                connectDone.Set();
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                SocketStateObject state = new SocketStateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                SetState(ENetState.Disconnected);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                SocketStateObject state = (SocketStateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    /*
                    Debug.Log(string.Format("ReceiveData [{0}]-[{1}]", bytesRead , Encoding.ASCII.GetString(state.buffer, 0, bytesRead)));
                    */

                    PacketParser(state.sb);

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        PacketParser(state.sb);
                        SetState(ENetState.Disconnected);
                    }

                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                SetState(ENetState.Disconnected);
            }
        }

        private void PacketParser(StringBuilder sb,int MaxCount=10)
        {
            int LoopCount = 0;
            int DataSize = 0;
            int ContentSize = 0;

            while (LoopCount <= MaxCount)
            {
                DataSize = sb.Length;

                if (DataSize >= HeaderSize)
                {
                    String StrSize = sb.ToString(0, HeaderSize);

                    if( true == Int32.TryParse(StrSize, out ContentSize) )
                    {
                        int PacketSize = (HeaderSize + ContentSize);

                        if (DataSize >= PacketSize)
                        {
                            string ContentData = sb.ToString(HeaderSize, ContentSize);
                            
                            var dict = Json.Deserialize(ContentData) as IDictionary<string, object>;

                            EPacketType CurrentType = (EPacketType)int.Parse(dict["proto"].ToString());

                            /*
                            if( CurrentType != EPacketType.FaultyAlram ) 
                                Debug.Log(string.Format("<Color=blue>[{0}] S->C : [{1}]</Color>", CurrentType.ToString() , ContentData));
                                */

                            netPacket.AddStringPacket(ContentData);

                            sb.Remove(0, PacketSize);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log(string.Format("Error PacketParser : {0} => {1}", StrSize , sb.ToString() ));

                        break;
                    }
                }
                else
                    break;

                LoopCount++;
            }
        }

        public bool CS_Send(String data)
        {
            if( null == ClientSocket )
                return false;

            // if (Define.IsNetworkMode == false)
            //     return false;


            Send(ClientSocket, data);

            return true;
        }
        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                // sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void PacketOnApplicationQuit()
        {
            if (null != Connect_Thread)
            {
                if ( true == Connect_Thread.IsAlive)
                {
                    Connect_Thread.Abort();
                }
            }

            if ( null != Socket_Thread )
            {
                if( true == Socket_Thread.IsAlive )
                {
                    Socket_Thread.Abort();
                }
            }
        }
    }
}
