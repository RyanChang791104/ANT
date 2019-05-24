using ANT_Managed_Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ANT_
{
    /// <summary>
    /// 此例實現ANT 協定應用，以Vector 3為對象，將初始化其裝置與通道，之後設定ANT的組態
    /// 例如; Channel、Device Name、Device Type、Device Transmission、Device Period與Device Frequency等
    /// 打開通道後，將自動接收並解析從Vector 3來的封包，如腳踏之扭力、瓦數、次數、以及Vector電量等
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly byte[] USER_NETWORK_KEY = { 0xB9, 0xA5, 0x21, 0xFB, 0xBD, 0x72, 0xC3, 0x45 };
        private byte[] Broadcast50 = { 0x46, 0xFF, 0xFF, 0xFF, 0xff, 0x03, 0x50, 0x01 };
        private byte[] Broadcast51 = { 0x46, 0xFF, 0xFF, 0xFF, 0xff, 0x03, 0x51, 0x01 };
        private byte[] Broadcast52 = { 0x46, 0xFF, 0xFF, 0x01, 0xff, 0x03, 0x52, 0x01 };

        private ANT_Device device0;
        private ANT_Channel channel0;
        private ANT_ReferenceLibrary.ChannelType channelType;
        private Stopwatch sw = new Stopwatch();
        private List<string> data = new List<string>();
        private List<string> packet = new List<string>();
        private string ESN_, SW, IP, MI, MN, COT, time_cost;
        private string AP, Period, AT;
        private bool StartANT = false;

        public MainWindow()
        {
            InitializeComponent();
            Packet_combox.ItemsSource = Combox_init();
            Packet_combox.SelectedIndex = 0;
        }

        private void Init()
        {
            //RichBox_Result.AppendText("Attempting to connect to an ANT USB device...\n");
            device0 = new ANT_Device();
            device0.deviceResponse += new ANT_Device.dDeviceResponseHandler(DeviceResponse);

            channel0 = device0.getChannel(int.Parse(USER_ANT_CHANNEL.Text));
            channel0.channelResponse += new dChannelResponseHandler(ChannelResponse);
            RichBox_Result.AppendText("Initialization was successful!\n");
        }

        private void ConfigureABT()
        {
            //RichBox_Result.AppendText("Resetting module...\n");
            device0.ResetSystem();
            System.Threading.Thread.Sleep(500);
            //RichBox_Result.AppendText("Setting network key...\n");
            if (device0.setNetworkKey(0, USER_NETWORK_KEY, 500)) ;
            //RichBox_Result.AppendText("Network key set\n");
            else
                throw new Exception("Error configuring network key\n");

            // RichBox_Result.AppendText("Assigning channel...\n");
            if (channel0.assignChannel(channelType, 0, 500)) ;
            //RichBox_Result.AppendText("Channel assigned\n");
            else
                throw new Exception("Error assigning channel\n");

            //RichBox_Result.AppendText("Setting Channel ID...\n");
            if (channel0.setChannelID(ushort.Parse(USER_DEVICENUM.Text), false, byte.Parse(USER_DEVICETYPE.Text), byte.Parse(USER_TRANSTYPE.Text), 500)) ;  // Not using pairing bit
                                                                                                                                                            // RichBox_Result.AppendText("Channel ID set\n");
            else
                throw new Exception("Error configuring Channel ID\n");

            //RichBox_Result.AppendText("Setting Radio Frequency...\n");
            if (channel0.setChannelFreq(byte.Parse(USER_RADIOFREQ.Text), 500)) ;
            //RichBox_Result.AppendText("Radio Frequency set\n");
            else
                throw new Exception("Error configuring Radio Frequency\n");

            //RichBox_Result.AppendText("Setting Channel Period...\n");
            if (channel0.setChannelPeriod(ushort.Parse(USER_CHANNELPERIOD.Text), 500)) ;
            //RichBox_Result.AppendText("Channel Period set\n");
            else
                throw new Exception("Error configuring Channel Period\n");
            device0.enableRxExtendedMessages(true);
            //device0.crystalEnable();

            //RichBox_Result.AppendText("Opening channel...\n");
            RichBox_Result.AppendText("ANT Configuration successful\n");
            if (channel0.openChannel(500))
            {
                RichBox_Result.AppendText("Channel " + USER_ANT_CHANNEL.Text + " opened\n");
            }
            else
            {
                throw new Exception("Error opening channel\n");
            }
        }

        private List<string> Combox_init()
        {
            data.Add("46-FF-FF-FF-FF-03-50-01(0x50)");
            data.Add("46-FF-FF-FF-FF-03-51-01(0x51)");
            data.Add("46-FF-FF-01-FF-03-52-01(0x52)");
            return data;
        }

        private void DeviceResponse(ANT_Response response)
        {
        }

        public void FileStreamWriteFile(ANT_Response response, Byte[] TX)
        {
            if (response == null)
            {
                string Result = BitConverter.ToString(TX);
                FileStream fsFile_ = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"\\log.txt", FileMode.Append);
                StreamWriter Swrite_ = new StreamWriter(fsFile_);
                Swrite_.WriteLine(System.DateTime.Now + " TX: " + Result);
                Swrite_.Close();
            }
            else
            {
                string Result = Convert.ToString(BitConverter.ToString(response.messageContents));
                FileStream fsFile_ = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"\\log.txt", FileMode.Append);
                StreamWriter Swrite_ = new StreamWriter(fsFile_);
                Swrite_.WriteLine(System.DateTime.Now + " RX: " + Result);
                Swrite_.Close();
            }
        }

        private void scroll_bar_enable()
        {
            if (Scroll.IsChecked == true)
            {
                RichBox_Result.ScrollToEnd();
            }
        }

        private static int count = 0;

        private void result_display(ANT_Response response)
        {
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { scroll_bar_enable(); }));
            }).Start();

            string Result = (BitConverter.ToString(response.messageContents)).ToString();//DEC to HEX ouput String
            string[] tmp = Result.Split('-');
            List<string> list = new List<string>(tmp);
            count++;
            Packet_Show.Content = count.ToString();

            if (tmp.Count() > 7)
            {
                if (tmp[2] == "07")
                    Reset();
                try
                {
                    list.RemoveAt(0);
                    tmp = list.ToArray();
                    Result_Show.Content = string.Join("-", tmp);
                    ESN_ = "";
                    AP = "";
                    IP = "";
                    COT = "";
                    Period = "";
                    AT = "";
                    MI = "";
                    MN = "";
                    switch (tmp[0])
                    {
                        case "07":
                            sw.Stop();
                            time_cost = sw.Elapsed.TotalMilliseconds.ToString();
                            break;

                        case "50":
                            string HW = Convert.ToUInt64(tmp[3], 16).ToString();
                            for (int k = 5; k >= 4; k--)
                            {
                                MI += tmp[k];
                            }
                            for (int j = 7; j >= 6; j--)
                            {
                                MN += tmp[j];
                            }
                            string Manufacture_ID = Convert.ToUInt64(MI, 16).ToString();
                            string Model_Number = Convert.ToUInt64(MN, 16).ToString();
                            x50.Content =
                                "HW: " + HW +
                                "\nManufacture_ID: " + Manufacture_ID +
                                "\nModel_Number " + Model_Number;
                            break;

                        case "51":
                            for (int k = 8; k > 4; k--)
                            {
                                ESN_ += tmp[k - 1];
                            }
                            SW = Convert.ToUInt64(tmp[3], 16).ToString();
                            ESN.Content = "ESN: " + Convert.ToUInt64(ESN_, 16).ToString() + " SW: " + SW;
                            break;

                        case "10":
                            string update_event = Convert.ToUInt64(tmp[1], 16).ToString();
                            string Pedal_Power = Convert.ToUInt64(tmp[2], 16).ToString() + " Not in used";
                            string Instantaneous_Cadence = Convert.ToUInt64(tmp[3], 16).ToString() + " rpm";
                            for (int k = 5; k >= 4; k--)
                            {
                                AP += tmp[k];
                            }
                            for (int j = 7; j >= 6; j--)
                            {
                                IP += tmp[j];
                            }
                            string Accumulated_Power = Convert.ToUInt64(AP, 16).ToString() + " (W)";
                            string Instantaneous_Power = Convert.ToUInt64(IP, 16).ToString() + " (W)";
                            x10.Content =
                                "update__event: " + update_event +
                                "\nPedal__Power: " + Pedal_Power +
                                "\nInstantaneous__Cadence " + Instantaneous_Cadence +
                                "\nAccumulated_Power " + Accumulated_Power +
                                "\nInstantaneous_Power " + Instantaneous_Power;
                            break;

                        case "52":
                            string Battery_Status = Convert.ToUInt64(tmp[7], 16).ToString();
                            string Fractional_Battery_Voltage = (Convert.ToUInt64(tmp[6], 16) / 256).ToString() + " (V)";
                            for (int k = 5; k >= 3; k--)
                            {
                                COT += tmp[k];
                            }
                            x52.Content =
                                "Battery_Status: " + Battery_Status +
                                "\nFractional_Battery_Voltage: " + Fractional_Battery_Voltage +
                                "\nCumulative_Operating_Time(hr): " + ((Convert.ToInt64(COT, 16) * 2) / 3600).ToString() + " hr" +
                                "\nCumulative_Operating_Time(s): \n" + ((Convert.ToInt64(COT, 16) * 2)).ToString() + " s";
                            break;

                        case "12":
                            update_event = Convert.ToUInt64(tmp[1], 16).ToString();
                            string Crank_Ticks = Convert.ToUInt64(tmp[2], 16).ToString();
                            Instantaneous_Cadence = Convert.ToUInt64(tmp[3], 16).ToString() + " rpm";

                            for (int j = 5; j >= 4; j--)
                            {
                                Period += tmp[j];
                            }
                            for (int o = 7; o >= 6; o--)
                            {
                                AT += tmp[o];
                            }
                            string Period_ = (Convert.ToUInt64(Period, 16) / 2048).ToString() + " s";
                            string Accumulated_Torque = (Convert.ToUInt64(AT, 16) / 32).ToString() + " Nm";
                            x12.Content =
                                "update__event: " + update_event +
                                "\nCrank__Ticks: " + Crank_Ticks +
                                "\nInstantaneous_Cadence: " + Instantaneous_Cadence +
                                "\nPeriods: " + Period_ +
                                "\nAccumulated_Torque: " + Accumulated_Torque;
                            break;
                    }
                }
                catch { MessageBox.Show("Something has wrong"); }
            }
        }

        private void Break_Click(object sender, RoutedEventArgs e)
        {
            StartANT = false;
            if (channel0 != null)
                channel0.closeChannel();
            if (device0 != null)
                device0.Dispose();
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { Break_close(); }));
            }).Start();
        }

        private void Break_close()
        {
            RichBox_Result.ScrollToEnd();
            RichBox_Result.AppendText("Channel" + USER_ANT_CHANNEL.Text + " has closed\n");
        }

        private void Output_Click(object sender, RoutedEventArgs e)
        {
            string myPath = AppDomain.CurrentDomain.BaseDirectory;
            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = myPath;
            prc.Start();
        }

        private void result_Rich_display(ANT_Response response)
        {
            string Result = Convert.ToString(BitConverter.ToString(response.messageContents));
            string[] tmp = Result.Split('-');
            List<string> list = new List<string>(tmp);
            if (tmp.Count() > 7)
            {
                list.RemoveAt(0);
                tmp = list.ToArray();
                string show = string.Join("-", tmp);
                RichBox_Result.AppendText("RX: " + show + "\n");
            }
        }

        private void Packet_send_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { broadcast_(); }));
            }).Start();
        }

        private void broadcast_()
        {
            if (channel0 != null)
            {
                if (Packet_combox.SelectedIndex >= 0)
                {
                    switch (Packet_combox.SelectedIndex)
                    {
                        case 0:
                            channel0.sendBroadcastData(Broadcast50);
                            FileStreamWriteFile(null, Broadcast50);
                            RichBox_Result.AppendText("TX: " + (BitConverter.ToString(Broadcast50)).ToString() + "\n");//DEC to HEX then output
                            break;

                        case 1:
                            channel0.sendBroadcastData(Broadcast51);
                            FileStreamWriteFile(null, Broadcast51);
                            RichBox_Result.AppendText("TX: " + (BitConverter.ToString(Broadcast51)).ToString() + "\n");
                            break;

                        case 2:
                            channel0.sendBroadcastData(Broadcast52);
                            FileStreamWriteFile(null, Broadcast52);
                            RichBox_Result.AppendText("TX: " + (BitConverter.ToString(Broadcast52)).ToString() + "\n");
                            break;
                    }
                }
            }
        }

        private void ChannelResponse(ANT_Response response)
        {
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { result_display(response); }));
            }).Start();
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { result_Rich_display(response); }));
            }).Start();
            new Thread(() =>
            {
                Dispatcher.Invoke(new Action(() => { FileStreamWriteFile(response, null); }));
            }).Start();
        }

        private void Select_Channel_Type()
        {
            bool MB = (bool)Master.IsChecked;
            bool SB = (bool)Slave.IsChecked;
            if (MB == true)
            {
                channelType = ANT_ReferenceLibrary.ChannelType.BASE_Master_Transmit_0x10;
            }
            else if (SB == true)
            {
                channelType = ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00;
                //channelType = ANT_ReferenceLibrary.ChannelType.ADV_Shared_0x20;
            }
            else RichBox_Result.AppendText("Please select the Channel Type");
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (StartANT == false)
            {
                Select_Channel_Type();
                Init();
                System.Threading.Thread.Sleep(1000);
                ConfigureABT();
                StartANT = true;
            }
            else
                MessageBox.Show("ANT has Start");
        }

        private void Reset()
        {
            System.Threading.Thread.Sleep(1000);
            ConfigureABT();
            RichBox_Result.AppendText("ANT + has reset");
        }
    }
}