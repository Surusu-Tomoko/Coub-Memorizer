using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Coub_Memorizer
{
    internal static class Program
    {
        public static Form1 MainForm { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            string server_url = ""; //"http://localhost:8082";
            if (Properties.Settings.Default.ispublic)
                server_url = "http://*:";
            else
                server_url = "http://localhost:";

            server_url += Properties.Settings.Default.port;
            //string server_url = "http://*:8082";

            if (Properties.Settings.Default.web_server)
                CreateWebHostBuilder(args, server_url).Build().RunAsync();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new Form1();
            Application.Run(MainForm);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string server_url) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseUrls(server_url);

    }
}
