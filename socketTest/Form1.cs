using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net; //###
using System.Net.Sockets; //####  Ctrl + .
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace socketTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //Control.CheckForIllegalCrossThreadCalls = false;
        }

        List<Socket> clientProxSocketList = new List<Socket>();

        private void btnStart_Click(object sender, EventArgs e)
        {
            //创建socket对象（使用IPv4类型，流式传输，和TCP连接  附：SocketType.Dgram）
            Socket serverSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            this.rtxtLog.Text = "创建服务端Socket对象\r\n" + this.rtxtLog.Text;
            //绑定IP和端口
            IPAddress ip = IPAddress.Parse(txtIP.Text);
            IPEndPoint ipEndPoint = new IPEndPoint(ip, int.Parse(txtPort.Text));
            serverSocket.Bind(ipEndPoint);
            //开启侦听
            serverSocket.Listen(10);
            //开始接受客户端的连接
            this.rtxtLog.Text = "开始接受客户端连接\r\n" + this.rtxtLog.Text;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.StartAcceptClient), serverSocket);
        }

        public void StartAcceptClient(object state)
        {
            var serverSocket = (Socket)state;
            while (true)
            {
                Socket proxSocket = serverSocket.Accept();//这句会阻塞主线程
                this.rtxtLog.Text = string.Format("一个客户端：{0}已经连接上\r\n{1}", proxSocket.RemoteEndPoint.ToString(), this.rtxtLog.Text);
                clientProxSocketList.Add(proxSocket);
                //服务端也要接受客户端的消息
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.RecieveData), proxSocket);

            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (var socket in clientProxSocketList)
            {
                if(socket.Connected)
                {
                    string str = this.txtMsg.Text;
                    byte[] data = Encoding.Default.GetBytes(str);
                    socket.Send(data, 0, data.Length, SocketFlags.None);
                }
            }
        }

        private void RecieveData(object obj)
        {
            var proxSocket = (Socket)obj;
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                //返回值为实际接受的数据长度
                int realLen = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                if (realLen == 0)
                {
                    this.rtxtLog.Text = string.Format("客户端：{0}{1}\r\n{2}", proxSocket.RemoteEndPoint.ToString(), "对方退出", rtxtLog.Text);
                    proxSocket.Shutdown(SocketShutdown.Both);
                    proxSocket.Close();
                    clientProxSocketList.Remove(proxSocket);
                    return;
                }
                string fromClientMsg = Encoding.Default.GetString(data, 0, realLen);
                this.rtxtLog.Text = string.Format("接受到客户端：{0}的消息：{1}\r\n{2}", proxSocket.RemoteEndPoint.ToString(), fromClientMsg, rtxtLog.Text);
            }
        }
    }
}
