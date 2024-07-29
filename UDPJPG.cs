using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Timers;

namespace UDPJPG;

public partial class UDPJPG : Form
{
    PictureBox pictureBox;
    UdpClient udpClient;
    long receivedBytesLength;
    long receivedBytesLengthDisp;
    int udpPort = 50001;
    string ipAddress;
    const int frame_reset = 30;
    System.Timers.Timer timer;
    List<byte> receivedBytesList = new List<byte>();
    public UDPJPG()
    {
        InitializeComponent();

        // ウィンドウのサイズを固定
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
        };
        this.Controls.Add(pictureBox);

        pictureBox.Paint += new PaintEventHandler(pictureBox_Paint);

        udpClient = new UdpClient(udpPort);
        udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);

        //フレーム間判定の30msecタイマー
        timer = new System.Timers.Timer(frame_reset);
        timer.Elapsed += OnTimedEvent;
        timer.AutoReset = false;
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedBytes = udpClient.EndReceive(ar, ref ipEndPoint);
        receivedBytesLength += receivedBytes.Length;
        ipAddress = ipEndPoint.Address.ToString();// 送信元のIPアドレス
        
        // 受信したデータをリストに追加
        receivedBytesList.AddRange(receivedBytes);

        // タイマーをリセット
        timer.Stop();
        timer.Start();

        udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);// 引き続き受信待機する
    }
    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        // 30msec以上データが受信されなかった場合、画像を表示
        try
        {
            using (MemoryStream ms = new MemoryStream(receivedBytesList.ToArray()))
            {
                pictureBox.Image = new Bitmap(ms);
            }
        }
        catch (ArgumentException)
        {
            // 画像データが無効な場合
        }

        // PictureBoxを再描画
        pictureBox.Invalidate();

        // データをリセットします
        receivedBytesList.Clear();
        receivedBytesLengthDisp = receivedBytesLength;//描画タイミングの前にreceivedBytesLengthが0に初期化されてしまうため保存しておく
        receivedBytesLength = 0;

    }
    private void pictureBox_Paint(object sender, PaintEventArgs e)
    {
        // 受信したバイト数
        e.Graphics.DrawString("Received: " + receivedBytesLengthDisp.ToString(), this.Font, Brushes.Black, new PointF(0, 0));
        // 送信しているこのPCのIP
        e.Graphics.DrawString("IP: " + DisplayIPAddress(), this.Font, Brushes.Black, new PointF(0, 16));
        // 受信しているUDPポート番号
        e.Graphics.DrawString("UDP port: " + this.udpPort.ToString(), this.Font, Brushes.Black, new PointF(0, 32));
        // 送信元IP
        e.Graphics.DrawString("From: " + ipAddress, this.Font, Brushes.Black, new PointF(0, 48));
    }

    public string DisplayIPAddress()
    {
        // ホスト名を取得
        string hostName = Dns.GetHostName();
        string ip = "0.0.0.0";

        // ホスト名からIPアドレスを取得
        IPAddress[] addresses = Dns.GetHostAddresses(hostName);

        foreach (var address in addresses)
        {
            // IPv4アドレス表示
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ip = address.ToString();
            }
        }
        return ip;
    }

}
