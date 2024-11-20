using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScrShareZ_Server
{
    public partial class Form1 : Form
    {
        private Form2 serverForm; // Maintain reference to Form2

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtIP.Text = GetLocalIPs();
        }

        // Get local IP address
        private string GetLocalIPs()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString(); // First found IPv4 address
                    }
                }

                return "No IPv4 address found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Start the server
        private void btnListen_Click(object sender, EventArgs e)
        {
            int port;

            // If the user hasn't entered a port, generate a random one
            if (string.IsNullOrEmpty(txtPort.Text))
            {
                port = GenerateRandomPort(); // Get a random port
                txtPort.Text = port.ToString(); // Display the random port in the text box
            }
            else if (!int.TryParse(txtPort.Text, out port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number between 1 and 65535.");
                return;
            }

            if (serverForm == null || serverForm.IsDisposed)
            {
                // Start the server with the specified or generated port
                serverForm = new Form2(txtIP.Text, port);
                serverForm.Show();
                btnListen.Text = "Stop Server"; // Change the button text to "Stop Server"
            }
            else
            {
                // Stop the server
                serverForm.Close(); // Close the server form
                serverForm = null; // Set the reference to null
                btnListen.Text = "Start Server"; // Change the button text to "Start Server"
            }
        }

        private int GenerateRandomPort()
        {
            Random random = new Random();
            int minPort = 1024; // Minimum port number (private range)
            int maxPort = 65535; // Maximum port number

            // Generate a random port number within the specified range
            return random.Next(minPort, maxPort + 1);
        }

        // Scan local network for other devices
        private void btnGetIPs_Click(object sender, EventArgs e)
        {
            listBoxIPs.Items.Clear();
            Task.Run(() => ScanNetwork());
        }

        // Scan the local network for reachable IPs
        private async Task ScanNetwork()
        {
            string localIP = GetLocalIP();
            if (localIP == null) return;

            string subnet = localIP.Substring(0, localIP.LastIndexOf('.'));

            // Use async-await for asynchronous pinging instead of Parallel.For
            var tasks = new List<Task>();
            for (int i = 1; i < 255; i++)
            {
                string ip = $"{subnet}.{i}";
                tasks.Add(PingHostAsync(ip)); // Add the ping task to the list
            }

            await Task.WhenAll(tasks); // Wait for all pinging tasks to complete
        }

        // Get local IP address
        private string GetLocalIP()
        {
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        // Async ping task
        private async Task PingHostAsync(string address)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(address, 100); // Use SendPingAsync for async ping
                    if (reply.Status == IPStatus.Success)
                    {
                        // Update UI on main thread
                        Invoke(new Action(() =>
                        {
                            listBoxIPs.Items.Add(address); // Add reachable IP to the list box
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or silently ignore
                Console.WriteLine($"Ping failed for {address}: {ex.Message}");
            }
        }
    }
}
