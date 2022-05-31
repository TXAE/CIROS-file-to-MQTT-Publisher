using System.Diagnostics;

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

    private async static void OnChanged(object sender, FileSystemEventArgs e)
    {
        //Console.WriteLine($"\nLogfile changed: {e.FullPath}");
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
                if (beginOfData < 0 || endOfData < 0 || lengthOfData <0) break;
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

    public static async Task Publish_Application_Message(string topic, string payload)
    {
        /*
         * This sample pushes a simple application message including a topic and a payload.
         *
         * Always use builders where they exist. Builders (in this project) are designed to be
         * backward compatible. Creating an _MqttApplicationMessage_ via its constructor is also
         * supported but the class might change often in future releases where the builder does not
         * or at least provides backward compatibility where possible.
         */

        var mqttFactory = new MQTTnet.MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MQTTnet.Client.Options.MqttClientOptionsBuilder()
                .WithTcpServer("141.72.189.72")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var applicationMessage = new MQTTnet.MqttApplicationMessageBuilder()
                .WithTopic("DHBW_Mannheim/U06A/Festo_Anlage/MPS_TransferFactory_Montieren/" + topic)
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