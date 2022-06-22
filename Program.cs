using SharpPcap;
using System.Diagnostics;


static class Program
{
    static void Main()
    {
        Process? p = Process.GetProcessesByName("CIROS Studio FESTO").FirstOrDefault();
        if (p == null)
        {
            string CirosPath = @"C:\Program Files\Festo Didactic\CIROS 6.4\CIROS Studio\CIROS Studio FESTO.exe";
            Console.WriteLine("Could not find a running Ciros process on this machine\n" +
                "Trying to start Ciros from " + CirosPath +
                "\nRemember to start logging in Ciros for MQTT Publisher to work!\n" +
                "In Ciros => RCI-Explorer => RH-6SDH5520 => Monitore => Position => 'EIN' klicken\n");
            p = new Process();
            p.StartInfo.FileName = CirosPath;
            p.Start();
        }

        using var device = CaptureDeviceList.Instance[4]; //Ethernet
        device.Open();
        device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
        device.Filter = "greater 100 and (" +  //help: https://www.tcpdump.org/manpages/pcap-filter.7.html
            "src host 192.168.0.128 or " +     //RH-6SDH5520
            "src host 192.168.0.123)";         //RV-3SDB
        Console.WriteLine("-- Listening on {0} and publishing to MQTT\n" +
            "Hit 'Ctrl-C' to exit...", device.Description);
        device.Capture(); // Start capture 'INFINTE' number of packets
    }

    /// <summary>
    /// Wrapper method to allow OnPacketArrivalAsync to be async
    /// see https://github.com/dotpcap/sharppcap/discussions/399#discussioncomment-2891449
    /// </summary>
    private static void OnPacketArrival(object sender, PacketCapture e)
    {
        OnPacketArrivalAsync(sender, e.GetPacket());
    }

    /// <summary>
    /// Publishes Packet Data asynchronously to MQTT
    /// </summary>
    private static async void OnPacketArrivalAsync(object sender, RawCapture e)
    {
        var packet = e.GetPacket();
        //var rawPacket = e.GetPacket();
        //var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

        var IPV4packet = packet.Extract<PacketDotNet.IPv4Packet>();
        string Robot = "";
        if (IPV4packet != null)
        {
            if (IPV4packet.SourceAddress.ToString() == "192.168.0.123")
                Robot = "RV-3SDB";
            else if (IPV4packet.SourceAddress.ToString() == "192.168.0.128")
                Robot = "RH-6SDH5520";
        }

        var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
        if (tcpPacket != null)
        {
            #region testing
            //for (int i = 0; i < 50; i++)
            //    await Publish_Application_Message("testTopic", i.ToString() + ": " + tcpPacket.ToString());
            //await Publish_Application_Message("testTopic", tcpPacket.ToString());
            #endregion

            //tcpPacket.PayloadData is a byte[]-Array. We need to convert it to a string
            string Payload = System.Text.Encoding.Default.GetString(tcpPacket.PayloadData);
            if (Payload.Contains("QoK"))
            {
                int beginOfData = Payload.IndexOf("QoK") + "QoK".Length;
                int endOfData = Payload.IndexOf(";;");
                //if we can't find the delimiter ";;", set the endOfData to Payload.Lenght
                if (endOfData == -1) endOfData = Payload.Length;
                int lengthOfData = endOfData - beginOfData;
                if (lengthOfData < 1) return;
                string Data = Payload.Substring(beginOfData, lengthOfData);
                string[] data = Data.Split(";");
                for (int i = 0; i < data.Length - 1; i += 2)
                    await Publish_Application_Message(Robot + "/" + data[i], data[i + 1]);
            }
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
}