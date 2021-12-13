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
        readonly static string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        readonly static string appStorageFolder = Path.Combine(baseFolder, "Start_EAS_Trans");
        int control = 0;
        public Form1(string number_ops)
        {
            InitializeComponent();
            textBox1.Text = number_ops;
            BaseConstructor();
        }
        public Form1()
        {
            InitializeComponent();
            BaseConstructor();
        }
        public string[] ReadText(string path)
        {
            int count = File.ReadAllLines(path).Length;
            string[] array = new string[count];
            using (StreamReader fs = new StreamReader(path))
            {
                int counter = 0;
                while (true)
                {
                    counter++;
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    array[counter - 1] = temp;
                }
            }
            return array;
        }
        public void BaseConstructor()
        {
            AutoCompleteStringCollection source = new AutoCompleteStringCollection();
            source.AddRange(ReadText(appStorageFolder + @"\data\path\Start_EAS_Trans\save\eas all ops.txt"));
            AutoCompleteStringCollection source1 = new AutoCompleteStringCollection()
        {
                           "n",
                           "w01",
                           "w02",
                           "w03",
                           "w04",
                           "w05",
                           "w06",
                           "d01",
                           "d02",
                           "d03",
                           "d04",
                           "d05",
                           "d06"
            };
            AutoCompleteStringCollection source2 = new AutoCompleteStringCollection()
        {
                           "0810",
                            "1110",
                             "1210",
                              "1310",
                               "1410",
                                "1510",
                                 "1610",
                                  "1710",
                                   "1810",
                                    "1910"
            };
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
        async public void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() => AsyncCopyLogEAS());
        }
        async public void AsyncCopyLogEAS()
        {
            CheckForIllegalCrossThreadCalls = false; // нехороший лайфхак,
                                                     // отменяет отслеживание ошибок,
                                                     // но дает передать компоненты формы в другой поток 
            textBox4.Text = null;
            textBox5.Text = null;
            try
            {
                myProgressBar1.Minimum = 0;
                myProgressBar1.Maximum = 100;
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
                    myProgressBar1.Value = 2;
                    myProgressBar1.Value = 3;
                    string path = @"\\r40-" + p1 + "-" + p2 + @"\c$\ProgramData\POS\Logs\2021" + p6 + ".json";
                    myProgressBar1.Value = 4;
                    string path_zip = @"D:\EAS_LOG\Log_EAS_" + p3 + "_ops-" + p1 + "-" + p2 + " " + NewDateFormat1 + ".zip";
                    await Task.Run(() => Search_and_copy(p1, p2, p3, p6, NewDateFormat,
                         path, path_zip, textBox5));
                    string zipFile = path + ".zip";
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
        public void Compress(string sourceFile, string compressedFile)
        {
            using (FileStream sourceStream = new FileStream(sourceFile, FileMode.OpenOrCreate))
            {
                using (FileStream targetStream = File.Create(compressedFile))
                {
                    using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                    }
                }
            }
        }
        async public void Search_and_copy(string p1, string p2, string p3, string p6, string NewDateFormat,
               string path, string path_zip, TextBox textBox)
        {
            try
            {
                string zipFile = "";
                string newPath = @"\\r40-" + p1 + "-" + p2 + @"\c$\2021" + p6 + ".json";
                FileInfo fileInf = new FileInfo(path);
                if (fileInf.Exists)
                {
                    fileInf.CopyTo(newPath, true);
                }
                if (fileInf.Exists)
                {
                    Thread thread = new Thread(t =>
                    {
                        FileInfo file = new FileInfo(path);
                        using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
                        {
                            textBox.Text = $"Начато архивирование файла: {path}";
                            zip.AddFile(newPath);
                            DirectoryInfo directory = new DirectoryInfo(newPath);
                            zip.SaveProgress += Zip_SavePrgogess;
                            zipFile = string.Format("{0}.zip", file.Name);
                            zip.Save(path_zip);
                        }

                    })

                    { IsBackground = true };
                    thread.Start();
                }
                while (myProgressBar1.Value < 98)
                {
                    await Task.Delay(500);
                }
                textBox.Text = ("Архивирование завершено");
                FileInfo file1 = new FileInfo(path_zip);
                FileInfo file2 = new FileInfo(newPath);
                if (file1.Exists)
                {
                    file2.Delete();
                    myProgressBar1.Value = 100;
                    textBox.Text = ($"Копирование завершено");
                    MessageBox.Show($"Копирование завершено");
                    myProgressBar1.Value = 0;
                    textBox4.Text = path_zip;
                    control = 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: \n{ex}");
            }
        }

        public void Copy_Eas_log(string p1, string p2, string p3, string p6, string NewDateFormat,
             string path, string path_zip, TextBox textBox, string zipFile)
        {
            if (myProgressBar1.Value == 84)
            {
                textBox.Text = ("Архивирование завершено\n");
                FileInfo fileInf_1 = new FileInfo(zipFile);
                if (fileInf_1.Exists)
                {
                    textBox.Text = "Выполняется копирование архивированного файла на диск Вашего ПК...";
                    myProgressBar1.Value = 85;
                    fileInf_1.MoveTo(path_zip);
                    myProgressBar1.Value = 100;
                    textBox.Text = ($"Копирование завершено");
                    textBox4.Text = path_zip;
                }
                else
                {
                    myProgressBar1.Value = 100;
                    textBox4.Text = ("Файл с именем: 2021" + p6 + ".json - не существует\n");
                }
            }
        }
        private void Zip_SavePrgogess(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == Ionic.Zip.ZipProgressEventType.Saving_EntryBytesRead)
            {
                myProgressBar1.Invoke(new MethodInvoker(delegate
                {
                    myProgressBar1.Maximum = 100;
                    myProgressBar1.Value = 4 + (int)((e.BytesTransferred * 94) / e.TotalBytesToTransfer);
                    myProgressBar1.Update();
                }));
            }
        }

        public void Name_DataBase_and_Server(string numder_ops, out string server, out string name_database, out string status)
        {
            status = "";
            name_database = "DB" + numder_ops;
            server = "";
            if (textBox1.Text != "")
            {
                try
                {
                    if (numder_ops != "222222")
                    {
                        IPAddress ipAddress = Dns.GetHostEntry("r40-" + numder_ops + "-n").AddressList[0];
                        Ping ping = new Ping();
                        PingReply pingReply = ping.Send(ipAddress);
                        if (pingReply.Address != null)
                        {
                            if (pingReply.Address.ToString() != "10.94.187.117"
                           & pingReply.Address.ToString() != "10.94.209.149"
                           & pingReply.Address.ToString() != "10.94.187.101"
                           & pingReply.Address.ToString() != "10.94.185.21"
                           & pingReply.Address.ToString() != "10.94.225.101"
                           & pingReply.Address.ToString() != "10.94.205.197"
                           & pingReply.Address.ToString() != "10.94.185.85"
                           & pingReply.Address.ToString() != "10.94.218.53"
                           & pingReply.Address.ToString() != "10.94.206.69"
                           & pingReply.Address.ToString() != "10.94.207.245"
                           & pingReply.Address.ToString() != "10.94.201.245")
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
                    {
                        server = @"D:\!localhost";
                    }
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
       async private void button2_Click(object sender, EventArgs e)
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
                           await Task.Run(() => Process.Start(@"\\r40-" + p1 + "-" + p2 + @"\c$\ProgramData\POS\Logs"));
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
                Name_DataBase_and_Server(textBox1.Text, out string server, out string name_db, out string status);
                if (server.Length > 8)
                {
                    textBox6.Text = server;
                    textBox7.Text = status;
                }
            }
            else
            {
                MessageBox.Show($"\nПоле для ввода номера отделения - пустое.");
            }
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
