using System.Diagnostics;
using SharpPcap;

static class Program
{
    //private const string logPath = @"C:\Users\festoadmin\Documents\DHBW_MA_Roboter.modx\ServoVoltage.log";
    private const string logPath = @"C:\Users\festoadmin\Documents\Wireshark_Logs";

    static async Task Main()
    {
        Process p = Process.GetProcessesByName("CIROS Studio FESTO").FirstOrDefault();
        if (p == null)
        {
            string CirosPath = @"C:\Program Files\Festo Didactic\CIROS 6.4\CIROS Studio\CIROS Studio FESTO.exe";
            Console.WriteLine("Could not find a running Ciros process on this machine\n" +
                "Starting " + CirosPath +
                "\nRemember to start logging in Ciros for MQTT Publisher to work!\n" +
                "In Ciros => RCI-Explorer => RH-6SDH5520 => Monitore => Position => Bei Online einschalten");
            p = new Process();
            p.StartInfo.FileName = CirosPath;
            p.Start();
        }

        var devices = CaptureDeviceList.Instance;
        if (devices.Count < 1)
        {
            Console.WriteLine("No network devices were found on this machine.");
            return;
        }
        using var device = devices[4]; //Ethernet
        device.Open();
        device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
        device.Filter = "greater 100 and (" +  //help: https://www.tcpdump.org/manpages/pcap-filter.7.html
            "src host 192.168.0.128 or " +     //RH-6SDH5520
            "src host 192.168.0.123)";         //RV-3SDB
        Console.WriteLine("-- Listening on {0} and publishing to MQTT...\n" +
            "Hit 'Ctrl-C' to exit...", device.Description);
        device.Capture(); // Start capture 'INFINTE' number of packets

        #region sendPositionsPls
            byte[] sendPositionsPls = new byte[49];
        sendPositionsPls[0] = 69;
        sendPositionsPls[1] = 
            sendPositionsPls[2] = 
            sendPositionsPls[7] = 
            sendPositionsPls[10] =
            sendPositionsPls[11] =
            sendPositionsPls[14] =
            sendPositionsPls[18] =
            sendPositionsPls[38] =
            sendPositionsPls[39] = 0;
        sendPositionsPls[3] = 49;
        sendPositionsPls[4] = 60;
        sendPositionsPls[5] = 125;
        sendPositionsPls[6] = 64;
        sendPositionsPls[8] = 128;
        sendPositionsPls[9] = 6;
        sendPositionsPls[12] = 192;
        sendPositionsPls[13] = 168;
        sendPositionsPls[15] = 193;
        sendPositionsPls[16] = 192;
        sendPositionsPls[17] = 168;
        sendPositionsPls[19] = 128;
        sendPositionsPls[20] = 213;
        sendPositionsPls[21] = 74;
        sendPositionsPls[22] = 39;
        sendPositionsPls[23] = 17;
        sendPositionsPls[24] = 143;
        sendPositionsPls[25] = 194;
        sendPositionsPls[26] = 95;
        sendPositionsPls[27] = 35;
        sendPositionsPls[28] = 123;
        sendPositionsPls[29] = 253;
        sendPositionsPls[30] = 63;
        sendPositionsPls[31] = 114;
        sendPositionsPls[32] = 80;
        sendPositionsPls[33] = 24;
        sendPositionsPls[34] = 1;
        sendPositionsPls[35] = 254;
        sendPositionsPls[36] = 130;
        sendPositionsPls[37] = 181;
        sendPositionsPls[40] = 49;
        sendPositionsPls[41] = 59;
        sendPositionsPls[42] = 49;
        sendPositionsPls[43] = 59;
        sendPositionsPls[44] = 74;
        sendPositionsPls[45] = 80;
        sendPositionsPls[46] = 79;
        sendPositionsPls[47] = 83;
        sendPositionsPls[48] = 70;
        

        //Source: https://github.com/dotpcap/sharppcap/blob/master/Examples/Example9.SendPacket/Example9.SendPacket.cs
        try
        {
            //Send the packet out the network device
            device.SendPacket(sendPositionsPls);
            Console.WriteLine("-- Packet sent successfuly.");
        }
        catch (Exception e)
        {
            Console.WriteLine("-- " + e.Message);
        }

        Console.Write("Hit 'Enter' to exit...");
        Console.ReadLine();
        #endregion

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = @"C:\Program Files\Wireshark\tshark.exe";
        psi.Arguments =
            "--interface Ethernet " +
            "--ring-buffer packets:2 " +
            "--ring-buffer files:5 " +
            "-w C:/Users/festoadmin/Documents/Wireshark_Logs/wireshark.log " +
            "greater 100 and src 192.168.0.123 or src 192.168.0.128"; 
            //"-F pcapng -W n ";//will save host name resolution records along with captured packets;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        Process tsharkProcess = Process.Start(psi);

        using var watcher = new FileSystemWatcher(logPath);

        watcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;

        watcher.Changed += OnChanged;
        watcher.EnableRaisingEvents = true;

        Console.WriteLine("Reading Wireshark logs and publishing to MQTT...");
        Console.ReadLine(); //why ReadLine needs to stay: https://stackoverflow.com/questions/16278783/filesystemwatcher-not-firing-events

        #region while(true)
        //while (true)
        //{

        //    Click_Aus(p.MainWindowHandle);

        //    Console.WriteLine("Reading Position.log and publishing to MQTT");
        //    string log;
        //    try
        //    {
        //        log = File.ReadAllText(logPath);
        //    }
        //    catch (Exception)
        //    {
        //        Click_Aus(p.MainWindowHandle);
        //        throw;
        //    }

        //    Console.WriteLine(log);


        //    int lenghtOfFirstLine = log.IndexOf("\r\n");
        //    string firstLine = log.Substring(0, lenghtOfFirstLine);
        //    string rest = log.Substring(lenghtOfFirstLine).Replace("\r\n", "\t");
        //    string[] labels = firstLine.Split("\t", StringSplitOptions.RemoveEmptyEntries);
        //    string[] values = rest.Split("\t", StringSplitOptions.RemoveEmptyEntries);



        //    //TODO
        //    int factor = values.Length / labels.Length;
        //    for (int i = 0; i < labels.Length; i++)
        //    {
        //        await Publish_Application_Message(labels[i], values[i+labels.Length*factor-labels.Length]);
        //    }
        //    //File.Delete(logPath);

        //    //Re-Start logging
        //    Click_Ein(p.MainWindowHandle);

        //    Thread.Sleep(3000);
        //}//end of while
        #endregion
    }//end of static async Task


    /// <summary>
    /// Publishes Packet Data to MQTT
    /// </summary>
    private static void device_OnPacketArrival(object sender, PacketCapture e)
    {
        var rawPacket = e.GetPacket();
        var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
        PacketDotNet.IPv4Packet IPV4packet = (PacketDotNet.IPv4Packet)packet.PayloadPacket;
        string Robot = "";
        if (IPV4packet.SourceAddress.ToString() == "192.168.0.123")
            Robot = "RV-3SDB";
        else if (IPV4packet.SourceAddress.ToString() == "192.168.0.128")
            Robot = "RH-6SDH5520";

        var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
        if (tcpPacket != null)
        {
            string Payload = System.Text.Encoding.Default.GetString(tcpPacket.PayloadData);
            if (Payload.Contains("QoK"))
            {
                int beginOfData = Payload.IndexOf("QoK") + "QoK".Length;
                int endOfData = Payload.IndexOf(";;");
                if (endOfData == -1) endOfData = Payload.Length;
                int lengthOfData = endOfData - beginOfData;
                if (beginOfData < 0 || lengthOfData < 0) return;
                string Data = Payload.Substring(beginOfData, lengthOfData);
                string[] data = Data.Split(";", StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < data.Length - 1; i += 2)
                    Publish_Application_Message(Robot + "/" + data[i], data[i + 1]);
            }
        }
    }


    private async static void OnChanged(object sender, FileSystemEventArgs e)
    {
        //Console.WriteLine($"\nFile changed: {e.FullPath}");
        try
        {
            FileStream logFileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader logFileReader = new StreamReader(logFileStream);
            while (!logFileReader.EndOfStream)
            {
                string line = logFileReader.ReadLine();
                if (line.Contains("QoK"))
                {
                    int beginOfData = line.IndexOf("QoK") + "QoK".Length;
                    int endOfData = line.IndexOf(";;");
                    int lengthOfData = endOfData - beginOfData;
                    if (beginOfData < 0 || endOfData < 0 || lengthOfData < 0) break;
                    string Data = line.Substring(beginOfData, lengthOfData);
                    string[] data = Data.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < data.Length - 1; i += 2)
                        await Publish_Application_Message(data[i], data[i + 1]);
                }
            }

            // Clean up
            logFileReader.Close();
            logFileStream.Close();
            Console.Beep();
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("No worries, I'll try to use the next file.");
            Console.WriteLine(ex.StackTrace);
        }
        
    }

    private static async Task Publish_Application_Message(string topic, string payload)
    {
        /*
         * SOURCE: https://github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Publish_Samples.cs
         * 
         * This sample pushes a simple application message including a topic and a payload.
         *
         * Always use builders where they exist. Builders (in this project) are designed to be
         * backward compatible. Creating an _MqttApplicationMessage_ via its constructor is also
         * supported but the class might change often in future releases where the builder does not
         * or at least provides backward compatibility where possible.
         */

        MQTTnet.MqttFactory? mqttFactory = new();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MQTTnet.Client.Options.MqttClientOptionsBuilder()
                .WithTcpServer("141.72.189.72")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var applicationMessage = new MQTTnet.MqttApplicationMessageBuilder()
                .WithTopic("DHBW_Mannheim/Raum_U06A/MPS_TransferFactory_FESTO/Mitsubishi_Robot_" + topic)
                .WithPayload(payload)
                .Build();

            //Console.WriteLine("Publising MQTT payload\n" +
            //    payload + "\n" +
            //    "to topic\n" +
            //    "DHBW_Mannheim/U06A/Festo_Anlage/MPS_TransferFactory_Montieren/" + topic);
            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
        }
    }

}//end of static class