using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace ScrShareZ_Server
{
    public partial class Form2 : Form
    {
        private readonly int port;
        private TcpListener server;
        private readonly List<TcpClient> clients = new List<TcpClient>();
        private readonly List<NetworkStream> clientStreams = new List<NetworkStream>();
        private readonly List<PictureBox> clientPictureBoxes = new List<PictureBox>();
        private readonly List<Label> clientIPLabels = new List<Label>();

        private bool isListening = false;
        private readonly Thread listeningThread;

        public Form2(string ipAddress, int port)
        {
            InitializeComponent();
            this.port = port;
            this.server = new TcpListener(IPAddress.Parse(ipAddress), port);
            listeningThread = new Thread(StartListening);
        }

        private void StartListening()
        {
            try
            {
                server.Start();
                isListening = true;
                while (isListening)
                {
                    var client = server.AcceptTcpClient();
                    var stream = client.GetStream();
                    lock (clients)
                    {
                        clients.Add(client);
                        clientStreams.Add(stream);

                        var pictureBox = CreateClientPictureBox();
                        clientPictureBoxes.Add(pictureBox);

                        var label = CreateClientIPLabel(client);
                        clientIPLabels.Add(label);

                        Invoke(new Action(() =>
                        {
                            this.Controls.Add(pictureBox);
                            this.Controls.Add(label);
                        }));

                        Thread clientThread = new Thread(() => ReceiveImage(client, stream, pictureBox));
                        clientThread.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting the server: {ex.Message}");
            }
        }

        private PictureBox CreateClientPictureBox()
        {
            var pictureBox = new PictureBox
            {
                Width = 300,
                Height = 200,
                BorderStyle = BorderStyle.Fixed3D,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            pictureBox.Left = 10 + (clientPictureBoxes.Count * 320);
            pictureBox.Top = 40;
            pictureBox.Click += PictureBox_Click; // Gắn sự kiện hiển thị toàn màn hình
            return pictureBox;
        }

        private Label CreateClientIPLabel(TcpClient client)
        {
            var label = new Label
            {
                Text = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                Width = 300,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            label.Left = 10 + (clientIPLabels.Count * 320);
            label.Top = 10;
            return label;
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && pictureBox.Image != null)
            {
                Form fullScreenForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Maximized,
                    BackColor = Color.Black
                };

                PictureBox fullScreenPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = pictureBox.Image,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                fullScreenPictureBox.Click += (s, args) => fullScreenForm.Close();
                fullScreenForm.KeyDown += (s, args) =>
                {
                    if (args.KeyCode == Keys.Escape)
                        fullScreenForm.Close();
                };

                fullScreenForm.Controls.Add(fullScreenPictureBox);
                fullScreenForm.ShowDialog();
            }
        }

        private void ReceiveImage(TcpClient client, NetworkStream stream, PictureBox pictureBox)
        {
            var binFormatter = new BinaryFormatter();
            try
            {
                while (client.Connected)
                {
                    Image receivedImage = (Image)binFormatter.Deserialize(stream);
                    Invoke(new Action(() =>
                    {
                        pictureBox.Image = receivedImage;
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving image: {ex.Message}");
            }
            finally
            {
                lock (clients)
                {
                    clients.Remove(client);
                    clientStreams.Remove(stream);
                    clientPictureBoxes.Remove(pictureBox);
                    clientIPLabels.RemoveAt(clientIPLabels.Count - 1);
                }
                client.Close();
            }
        }

        private void StopListening()
        {
            isListening = false;
            foreach (var client in clients)
            {
                client.Close();
            }
            server.Stop();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopListening();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            listeningThread.Start();
        }
    }
}
