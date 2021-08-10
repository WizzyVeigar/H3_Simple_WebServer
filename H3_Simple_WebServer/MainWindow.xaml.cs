using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace H3_Simple_WebServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Server server;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                if (server == null)
                    server = new Server();

                server.Start(
                        IPAddress.Parse(txt_IpAddress.Text),
                        int.Parse(txt_Port.Text),
                        3,
                        txt_contentPath.Text
                        );
            }
        }

        private bool ValidateInput()
        {
            string regex = "[a-z]";

            if (Regex.Match(txt_IpAddress.Text, regex).Length == 0 || Regex.Match(txt_Port.Text, regex).Length == 0)
            {
                return true;
            }
            return false;
        }

        private void StopServer(object sender, RoutedEventArgs e)
        {
            if (server != null)
                server.Stop();

        }
    }
}
