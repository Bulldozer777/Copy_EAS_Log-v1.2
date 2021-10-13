using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Copy_EAS_Log
{

    public partial class Form1 : Form
    {
        int control = 0;
        public Form1()
        {
            try
            {
                InitializeComponent();
                AutoCompleteStringCollection source = new AutoCompleteStringCollection() {};
                source.AddRange(ReadText(@"C:\Users\Eduard.Karpov\Downloads\Telegram Desktop\Copy_EAS_Log\Copy_EAS_Log\Sdo\eas all ops.txt"));
                AutoCompleteStringCollection source1 = new AutoCompleteStringCollection() {};
                source1.AddRange(ReadText(@"C:\Users\Eduard.Karpov\Downloads\Telegram Desktop\Copy_EAS_Log\Copy_EAS_Log\Sdo\eas windows.txt"));
                AutoCompleteStringCollection source2 = new AutoCompleteStringCollection() {};
                source2.AddRange(ReadText(@"C:\Users\Eduard.Karpov\Downloads\Telegram Desktop\Copy_EAS_Log\Copy_EAS_Log\Sdo\eas date.txt"));
                AutoCompleteStringCollection[] autoCompleteStringCollections = new AutoCompleteStringCollection[3] { source, source1, source2 };
                TextBox[] textBoxes = new TextBox[3] { textBox3, textBox2, textBox1 };
                int count = textBoxes.Length - 1;
                foreach (var i in textBoxes)
                {
                    i.AutoCompleteCustomSource = autoCompleteStringCollections[count--];
                    i.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    i.AutoCompleteSource = AutoCompleteSource.CustomSource;
                }
            }
            catch (Exception ex)
            {
                textBox4.Text = ($"Ошибка: { ex.Message}");
            }
        }
        public string[] ReadText(string path)
        {
            string[] array;
            using (StreamReader fs = new StreamReader(path))
            {
                array = fs.ReadToEnd().Split().ToArray();
                int counter = 0;
                while (true)
                {
                    counter++;
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    array[counter] = temp;
                }
            }
            return array;
        }
        async public void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() => AsyncCopyLogEAS());
        }
        async public void AsyncCopyLogEAS()
        {
            CheckForIllegalCrossThreadCalls = false; // нехороший лайфхак,
                                                     // отменяет отслеживание ошибок,
                                                     // но дает передать компоненты формы в другой поток 
            textBox4.Clear();
            textBox5.Clear();           
            try
            {
                string p1 = textBox1.Text;
                myProgressBar1.Value = 1;
                if (p1.Length == 6)
                {
                    string p2 = textBox2.Text;
                    string p3 = textBox3.Text;
                    char[] chr1 = { p3[0], p3[1] };
                    string p4 = new string(chr1);
                    char[] chr2 = { p3[2], p3[3] };
                    string p5 = new string(chr2);
                    string p6 = p5 + p4;
                    DateTime currentDate = DateTime.Now;
                    string NewDateFormat = currentDate.ToString("yyyy-MM-dd HH.mm.ss");
                    string NewDateFormat1 = currentDate.ToString("HH.mm.ss");
                    string path = @"\\r40-" + p1 + "-" + p2 + @"\c$\ProgramData\POS\Logs\2021" + p6 + ".json";                
                    Directory.CreateDirectory(@"D:\EAS_LOG");
                    string path_zip = @"D:\EAS_LOG\Log_EAS_" + p3 + "_ops-" + p1 + "-" + p2 + " " + NewDateFormat1 + ".zip";
                    string newPath = @"\\r40-" + p1 + "-" + p2 + @"\c$\2021" + p6 + ".json";
                    FileInfo fileInf = new FileInfo(path);
                    if (fileInf.Exists)
                    {
                        fileInf.CopyTo(newPath, true);
                    }
                    myProgressBar1.Value = 4;
                    if (fileInf.Exists)
                    {
                        Thread thread = new Thread(t =>
                        {
                            FileInfo file = new FileInfo(path);
                            using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
                            {
                                textBox5.Text = $"Начато архивирование файла: {path}";
                                zip.AddFile(newPath);
                                DirectoryInfo directory = new DirectoryInfo(newPath);                             
                                zip.SaveProgress += Zip_SaveProgress;
                                zip.Save(path_zip);
                            }
                        })
                        { IsBackground = true };
                        thread.Start();
                    }
                    while (myProgressBar1.Value < 98)
                    {
                        await Task.Delay(1000);
                    }
                    textBox5.Text = "Архивирование завершено";
                    FileInfo file1 = new FileInfo(path_zip);
                    FileInfo file2 = new FileInfo(newPath);
                    if (file1.Exists)
                    {
                        file2.Delete();
                        myProgressBar1.Value = 100;
                        textBox5.Text = $"Копирование завершено";
                        MessageBox.Show($"Копирование завершено");
                        myProgressBar1.Value = 0;
                        textBox4.Text = path_zip;
                        control = 1;
                    }
                }
                else
                {
                    textBox4.Text = ("Файл - не существует\n");
                }
            }
            catch (Exception ex)
            {
                textBox4.Text = ($"Ошибка: { ex.Message}");
            }
        }
        public void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if(e.EventType == Ionic.Zip.ZipProgressEventType.Saving_EntryBytesRead)
            {
                myProgressBar1.Invoke(new MethodInvoker(delegate
                {
                    this.DoubleBuffered = true;
                    myProgressBar1.Maximum = 100;
                    myProgressBar1.Value = 4 + (int)((e.BytesTransferred * 94) / e.TotalBytesToTransfer);
                    myProgressBar1.Update();
                }));
            }
        }
        public void Name_DataBase_and_Server(string numder_ops, out string server, out string status)
        {
            status = "";
            server = "";
            textBox6.Clear();
            textBox7.Clear();
            if (textBox1.Text != "")
            {
                try
                {
                    if (numder_ops != "222222")
                    {
                        IPAddress ipAddress = Dns.GetHostEntry("r40-" + numder_ops + "-n").AddressList[0];
                        Ping ping = new Ping();
                        PingReply pingReply = ping.Send(ipAddress);
                        textBox7.Text = pingReply.Status.ToString();
                        if (pingReply.Address != null)
                        {
                            if (pingReply.Address.ToString() != "10.94.187.117"
                                & pingReply.Address.ToString() != "10.94.209.149"
                                & pingReply.Address.ToString() != "10.94.187.101"
                                & pingReply.Address.ToString() != "10.94.185.21"
                                & pingReply.Address.ToString() != "10.94.225.101"
                                & pingReply.Address.ToString() != "10.94.217.213")
                            {

                                if (pingReply.Status.ToString() != "Success")
                                {
                                    MessageBox.Show($"ОПС {textBox1.Text} не подключается");
                                }
                                else
                                {
                                    status = pingReply.Status.ToString();
                                    server = pingReply.Address.ToString();
                                    System.Threading.Thread.Sleep(200);
                                }
                            }
                            else
                            {
                                MessageBox.Show($"\nКоманда пинг - не проходит.");
                                server = "";
                            }
                        }
                        else
                        {
                            MessageBox.Show($"\nКоманда пинг - не проходит.");
                            server = "";
                        }
                    }
                    else
                        server = @"D:\!localhost";
                }
                catch (PingException ex)
                {
                    MessageBox.Show($"\nКоманда пинг - не проходит.\n{ex.Message}");
                    server = "";
                }
                catch (SocketException)
                {
                    MessageBox.Show("\nКоманда пинг - не проходит.\nCould not resolve host name.");
                    server = "";
                }
                catch (ArgumentNullException)
                {
                    MessageBox.Show("\nКоманда пинг - не проходит.\nPlease enter the host name or IP address to ping.");
                    server = "";
                }
                catch (System.Net.NetworkInformation.NetworkInformationException)
                {
                    MessageBox.Show($"\nКоманда пинг - не проходит.\nПк ОПС {textBox1.Text} - выключен или без интернета");
                    server = "";
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show($"\nКоманда пинг - не проходит.\nПк ОПС {textBox1.Text} - выключен или без интернета");
                    server = "";
                }
            }
            else
                MessageBox.Show($"\nПоле для ввода номера ОПС - пустое\n(Метод Name_DataBase_and_Server)");
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string p1 = textBox1.Text;
                string p2 = textBox2.Text;
                if (p1 != "")
                {
                    if (p2 != "")
                    {
                        if (p1.Length == 6)
                        {
                            Process.Start(@"\\r40-" + p1 + "-" + p2 + @"\c$\ProgramData\POS\Logs");
                        }
                        else
                        {
                            MessageBox.Show($"\nПоле для ввода номера отделения - некорректное.\nномер ОПС - 6 символов");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"\nПоле для ввода номера ОКНА отделения - пустое.\n");
                    }
                }
                else
                {
                    MessageBox.Show($"\nПоле для ввода номера отделения - пустое.\n");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка: \n{ex}");
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Name_DataBase_and_Server(textBox1.Text, out string server, out string status);
                if (server.Length > 8)
                {
                    textBox6.Text = server;
                    textBox7.Text = status;
                }
            }
            else
                MessageBox.Show($"\nПоле для ввода номера отделения - пустое.");
        }
        private void button4_Click(object sender, EventArgs e)
        {
            textBox2.Text = "n";
        }
        private void button5_Click(object sender, EventArgs e)
        {
            DateTime dateTime = DateTime.Now; ;
            string EAS_DateTime_Now = dateTime.ToString("ddMM");
            textBox3.Text = EAS_DateTime_Now;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            if(control == 0)
            {
                textBox4.Text = "Операции архивирования и копирования логов на Ваш ПК - не завершены";
            }
            if (control == 1)
            {
                Process.Start(@"D:\EAS_LOG");
                control = 0;
            }
        }
    }
}
