using System;
using System.Windows.Forms;

namespace Dots3
{
    using OpenNETCF.Net.NetworkInformation;
    using OpenNETCF.Net.Ftp;
    using System.Net;
    using System.IO;
    using System.Threading;
    using System.Text;
    using System.Linq;
    using Ionic.Zip;
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    
    public partial class DeviceProvisor : Form
    {
        string str_dummysrv = "";
        //string str_Server = "tsd_auto_setup:tsdautosetup@ts-glb01";
        string str_Server = "";
        string str_Current_TSD_Model = "Unknown";
        string str_NIC_Name = "Unknown";
        string macaddress = "";
        string str_Version = Environment.OSVersion.Version.Major.ToString();
        string str_dummyusr = "";
        string str_dummypwd = "";
        string str_rdpfile = "";
        string str_mscrfile = "";
        string str_tsglb_srv = "";

        private HttpWebResponse myHttpWebResponse;
        private Stream receiveStream;
        private StreamReader readStream;

        Thread ProvisionThread;

        public delegate void serviceGUIDelegate();
        private delegate void AddToLogDelegate(string msg);
        private static Action EmptyDelegate = delegate() { };

        /// <summary>
        /// Read and Replaces text in a file.
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        static public void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            StreamReader reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();
            content = Regex.Replace(content, searchText, replaceText);
            
            Encoding unicode = Encoding.Unicode;
            StreamWriter writer = new StreamWriter(filePath, false, Encoding.Unicode);
            
            writer.Write(content);
            writer.Close();
        }

        //Ввод прогрессбара
        /*
        private void CopyWithProgress(string[] filenames)
        {
            // Display the ProgressBar control.
            pBar1.Visible = true;
            // Set Minimum to 1 to represent the first file being copied.
            pBar1.Minimum = 1;
            // Set Maximum to the total number of files to copy.
            pBar1.Maximum = filenames.Length;
            // Set the initial value of the ProgressBar.
            pBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            pBar1.Step = 1;

            // Loop through all files to copy.
            for (int x = 1; x <= filenames.Length; x++)
            {
                // Copy the file and increment the ProgressBar if successful.
                if (CopyFile(filenames[x - 1]) == true)
                {
                    // Perform the increment on the ProgressBar.
                    pBar1.PerformStep();
                }
            }
        } */
        
        private void AddToLog(string msg)
        {
            //textBox1.Text = textBox1.Text + msg + "\r\n";
            BeginInvoke((Action)delegate()
            { 
               textBox1.Text            = textBox1.Text + msg + "\r\n";

               textBox1.Text            = textBox1.Text + "\r\n";
               textBox1.SelectionStart  = textBox1.TextLength - 1;
               textBox1.ScrollToCaret();
            });

            Thread.Sleep(0);
        }

        private void SetProgressBar(int Value)
        {
            BeginInvoke((Action)delegate()
            {
                pBar1.Value = Value;
            });

            Thread.Sleep(0);
        }

        void ProccessTSDModel()
        {
            AddToLog("str_Version: " + str_Version);
            AddToLog("Checking TSD model...");
            str_tsglb_srv = "";
            
            if (((System.IO.File.Exists("\\Application\\mc30XX.idi") || System.IO.File.Exists("\\Application\\mc30n0.idi")) &&
                   (str_Version == "5") && (System.IO.File.Exists("\\Application\\30XXc50BenAppl.022.id") || System.IO.File.Exists("\\Application\\30XXc50BenAppl.023.id"))))
            {
                str_Current_TSD_Model = "mc30n0";
                str_NIC_Name = "photon1";
                str_tsglb_srv = "ts-glb01";
            }
            else if ((System.IO.File.Exists("\\Application\\mc32XX.idi") &&
                   (str_Version == "7") && (Registry.CurrentUser.OpenSubKey(@"Software\\Symbol\\MC32N0Security") != null)))
            {
                str_Current_TSD_Model = "mc32XX";
                str_NIC_Name = "XWING20_1";
                str_tsglb_srv = "ts-glb01";
            }
            else if ((System.IO.File.Exists("\\Application\\mc90XX.idi") || System.IO.File.Exists("\\Application\\mc90n0.idi")) &&
                   (str_Version == "5") && System.IO.File.Exists("\\Application\\909Xc50BenAppl.036.id"))
            {
                str_Current_TSD_Model = "mc90n0";
                str_NIC_Name = "photon1";
                str_tsglb_srv = "ts-glb02";
            }
            else if ((System.IO.File.Exists("\\Application\\mc31XX.idi") || System.IO.File.Exists("\\Application\\mc31n0.idi")) &&
                   (str_Version == "6") && (Registry.CurrentUser.OpenSubKey(@"Software\\Symbol\\MC3100Security") != null))
            {
                str_Current_TSD_Model = "mc31n0";
                str_NIC_Name = "jedi10_1";
                str_tsglb_srv = "ts-glb02";
            }

            else if ((System.IO.File.Exists("\\Application\\mc91XX.idi") || System.IO.File.Exists("\\Application\\mc91n0.idi")) &&
                   (str_Version == "6") && (Registry.CurrentUser.OpenSubKey(@"Software\\Symbol\\MC9190Security") != null))
            {
                str_Current_TSD_Model = "mc91n0";
                str_NIC_Name = "jedi10_1";
                str_tsglb_srv = "ts-glb02";
            }
            AddToLog("Выполнено...10%");
            AddToLog("TS_SRV: " + str_tsglb_srv);
            
            StreamWriter sw = new StreamWriter("\\Temp\\tsglb_srv.txt");
            sw.WriteLine(str_tsglb_srv);
            sw.Close();

        }

        void ProccessMacAddress()
        {
                
            INetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            foreach (INetworkInterface currentnic in nics)
            {
                if (!(currentnic.Name.ToUpper().Contains("USB")) && !(currentnic.Name.ToUpper().Contains("SS1VNDIS1")))
                {
                    //Console.Write(currentnic.Name);
                    //Console.Write("\r\n");

                    AddToLog("Адаптер: " + currentnic.Name);

                    macaddress = "";

                    //for each j you can get the MAC 
                    PhysicalAddress address = currentnic.GetPhysicalAddress();
                    byte[] bytes = address.GetAddressBytes();

                    for (int i = 0; i < bytes.Length; i++)
                    {
                        // Display the physical address in hexadecimal. 
                        macaddress = macaddress + bytes[i].ToString("X2");
                    }

                    macaddress = macaddress.ToLower();

                    AddToLog("MAC-адрес: " + macaddress);
                }
            }

            StreamWriter sw = new StreamWriter("\\Temp\\tsd_autosetup\\boot\\mac_addr.txt");
            sw.WriteLine(macaddress);
            sw.Close();

            AddToLog("str_Current_TSD_Model: " + str_Current_TSD_Model);
            AddToLog("Выполнено...20%");
        }

        private void DownloadByHTTP(string remotepath, string localpath)
        {
            System.Net.WebResponse response = null;
            Stream responseStream = null;
            FileStream fileStream = null;

            AddToLog("Downloading :" + remotepath);

            // Creates an HttpWebRequest with the specified URL. 
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(remotepath);

            myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            //response = myHttpWebResponse.GetResponse();
            responseStream = myHttpWebResponse.GetResponseStream();

            fileStream = File.Open(localpath, FileMode.Create, FileAccess.Write, FileShare.None);
            // read up to ten kilobytes at a time
            int maxRead = 10240;
            byte[] buffer = new byte[maxRead];
            int bytesRead = 0;
            int totalBytesRead = 0;

            // loop until no data is returned
            while ((bytesRead = responseStream.Read(buffer, 0, maxRead)) > 0)
            {
                totalBytesRead += bytesRead;
                fileStream.Write(buffer, 0, bytesRead);
            }
            
            if(null != responseStream) responseStream.Close();
            if(null != response) response.Close();
            if(null != fileStream) fileStream.Close();

            AddToLog("Download complete :");
            
        }

        //private void Download(string ftpServerIP, string filePath)
        //{

        //    FtpWebRequest reqFTP;
        //    try
        //    {
        //        FtpRequestCreator creator = new FtpRequestCreator();
        //        WebRequest.RegisterPrefix("ftp:", creator);

        //        //filePath = <<The full path where the file is to be created. the>>,
        //        //fileName = <<Name of the file to be createdNeed not name on FTP server. name name()>>
        //        FileStream outputStream = new FileStream(filePath, FileMode.Create);

        //        reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(ftpServerIP));
        //        reqFTP.Passive = true;
        //        reqFTP.Binary = true;
        //        //reqFTP.Credentials = new NetworkCredential("tsd_auto_setup", "tsdautosetup");

        //        AddToLog("#1");
        //        //FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
        //        Stream ftpRequestStream = reqFTP.GetRequestStream();

        //        AddToLog("#2");
                
        //        /*
        //        Stream ftpStream = response.GetResponseStream();

        //        long cl = response.ContentLength;
        //        int bufferSize = 2048;
        //        int readCount;
        //        byte[] buffer = new byte[bufferSize];
        //        readCount = ftpStream.Read(buffer, 0, bufferSize);
        //        while (readCount > 0)
        //        {
        //            outputStream.Write(buffer, 0, readCount);
        //            readCount = ftpStream.Read(buffer, 0, bufferSize);
        //        }
        //        ftpStream.Close();
        //        outputStream.Close();
        //        response.Close(); */
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message.ToString());
        //    }
        //}

        private void Unzip(string arcFileName, string extractpath)
        {
            AddToLog("Распаковка " + arcFileName + " -> " + extractpath);

            using (ZipFile zip = ZipFile.Read(arcFileName))
            {
                zip.ExtractAll(extractpath, true);
            }
            //AddToLog("Выполнено...40%");
        }

        private void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);

                AddToLog(file + "->" + dest);
                File.Copy(file, dest, true);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);

                CopyFolder(folder, dest);
            }
        }
       
        private void ProccessBootBlock()
        {
            AddToLog("Замена блока Boot...");

            if (Directory.Exists(@"\Application\tsd_autosetup\boot")) Directory.Delete(@"\Application\tsd_autosetup\boot", true);
            Directory.CreateDirectory(@"\Application\tsd_autosetup\boot");
            AddToLog("Создание папки Boot завершено");
            CopyFolder(@"\Temp\tsd_autosetup\boot", @"\Application\tsd_autosetup\boot");
            AddToLog("Копирование папки Boot завершено");
            
            AddToLog("Замена блока Boot завершена");
            //AddToLog("Выполнено...50%");
        }

        private void ProccessSoftBlock()
        {
            AddToLog("Замена блока Soft...");

            if (Directory.Exists(@"\Application\tsd_autosetup\soft")) Directory.Delete(@"\Application\tsd_autosetup\soft", true); 
            Directory.CreateDirectory(@"\Application\tsd_autosetup\soft");

            CopyFolder(@"\Temp\tsd_autosetup\soft", @"\Application\tsd_autosetup\soft");
            CopyFolder(@"\Temp\tsd_autosetup\main\Configs\Application\Startup", @"\Application\Startup");

            AddToLog("Замена блока Soft завершена");

            SetProgressBar(60);
            AddToLog("Выполнено...60%");
        }

        private void ProccessMainBlock()
        {
            AddToLog("Замена блока Main...");

            //if (Directory.Exists(@"\Application\tsd_autosetup\main")) Directory.Delete(@"\Application\tsd_autosetup\soft", true);
            //Directory.CreateDirectory(@"\Application\tsd_autosetup\soft");

            //CopyFolder(@"\Temp\tsd_autosetup\main\Configs\Application\tsd_autosetup", @"\Application");
            CopyFolder(@"\Temp\tsd_autosetup\main\Configs", @"\");

            CopyFolder(@"\Temp\tsd_autosetup\main\Configs\Temp\tsd_autosetup\run", @"\Temp\tsd_autosetup\run");


            AddToLog("Замена блока Main завершена");
            //AddToLog("Выполнено...70%");

        }
        private void RunProcess(string FRunFile, string FArguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            processStartInfo.FileName   = FRunFile;
            processStartInfo.Arguments  = FArguments;

            try
            {
                AddToLog("Запуск " + FRunFile + " " + FArguments + "...");

                Process.Start(processStartInfo);
            }
            catch (Exception f)
            {
                MessageBox.Show(f.ToString());
            }
        }

        private void StartUpAply()   
        {
            AddToLog("Применение параметров...");

            
            try
            {
            //CopyFolder(@"\Temp\tsd_autosetup\main\Configs\Application\tsd_autosetup", @"\Application"); 
            }
            catch (Exception f) { }
            AddToLog("Применение параметров... ok");
        }
            //CopyFolder(@"\Temp\tsd_autosetup\main\mainConfigs", @"\");
        
         private void ProccessApply()
         {
          
            RunProcess(@"\Windows\regmerge.exe", @"/d /q \Application");
            RunProcess(@"\Windows\regmerge.exe", @"/d /q \Application\tsd_autosetup\soft\AppCenter");
            RunProcess(@"\Windows\regmerge.exe", @"/d /q \Temp\tsd_autosetup\main\Setup");
        
            AddToLog("Замена блока Main завершена");
            //AddToLog("Выполнено...80%");

         }
        

        void UnloadDataWedge()
        {
            AddToLog("UnloadDataWedge...");
        }

        public Boolean bUpdateMode = false;

        void BeginProvising()
        {
            AddToLogDelegate LogUpdater = AddToLog;

            // Возвращаем кнопку "ЗАВЕРШИТЬ"
            BeginInvoke((Action)delegate()
            {
                button1.Enabled = true;

            });
            // Кнопка "Global"
            BeginInvoke((Action)delegate()
            {
                button2.Enabled = false;
                button2.Visible = false;
            });
            // Кнопка "ИМ Склад"
            BeginInvoke((Action)delegate()
            {
                button3.Enabled = false;
                button3.Visible = false;
            });



            CheckUpdate();
            //try
            //{
             if (bUpdateMode == false)
             {

                //Directory.CreateDirectory(@"\Temp\tsd_autosetup");
                try
                {
                 if (Directory.Exists(@"\Temp\tsd_autosetup\boot")) Directory.Delete(@"\Temp\tsd_autosetup\boot", true);
                 Directory.CreateDirectory(@"\Temp\tsd_autosetup\boot");
                }
                catch (Exception f) { }
                //RunProcess(@"\Windows\unload.exe", "Motorola DataWedge"); //выгружает DataWedge!!!!!!
                //WaitFor("Remove Programs Error", 5)
                //If (WndExists("Remove Programs Error"))
                //Close("Remove Programs Error")

                try
                {
                 if (Directory.Exists(@"\Temp\tsd_autosetup\soft")) Directory.Delete(@"\Temp\tsd_autosetup\soft", true);
                Directory.CreateDirectory(@"\Temp\tsd_autosetup\soft");
                }
                catch (Exception f) { }

                try
                {
                if (Directory.Exists(@"\Temp\tsd_autosetup\main")) Directory.Delete(@"\Temp\tsd_autosetup\main", true);
                Directory.CreateDirectory(@"\Temp\tsd_autosetup\main");
                }
                catch (Exception f) { }

                if (Directory.Exists(@"\Temp\tsd_autosetup\temp")) Directory.Delete(@"\Temp\tsd_autosetup\temp", true);
                Directory.CreateDirectory(@"\Temp\tsd_autosetup\temp");

                if (Directory.Exists(@"\Temp\tsd_autosetup\run")) Directory.Delete(@"\Temp\tsd_autosetup\run", true);
                Directory.CreateDirectory(@"\Temp\tsd_autosetup\run");

                //Directory.CreateDirectory(@"\Application\tsd_autosetup");
                AddToLog("Выполнено Temp...");

                AddToLog("Выполнение замены Startup...");
                //if (!System.IO.File.Exists(@"\Application\Startup\tsd_autosetup_boot.run"));
                //if (Directory.Exists(@"\Application\Startup")) Directory.Delete(@"\Application\Startup", true);
                //Directory.CreateDirectory(@"\Application\Startup");
                //AddToLog("Выполненa заменa Startup...");

                //if (Directory.Exists(@"\Windows\StartUp")) Directory.Delete(@"\Windows\StartUp", true);
                //Directory.CreateDirectory(@"\Windows\StartUp");
              //}
              //catch (Exception f) { }
                AddToLog("Выполнено Application...");

                //ProccessTSDModel();
                ProccessMacAddress();

                SetProgressBar(10);

                DownloadByHTTP("http://" + str_Server + "/Models/" + str_Current_TSD_Model + "/main.zip", "\\Temp\\tsd_autosetup\\main\\main.zip");
                Unzip(@"\Temp\tsd_autosetup\main\main.zip", @"\Temp\tsd_autosetup\");
                File.Delete(@"\Temp\tsd_autosetup\main\main.zip");

                SetProgressBar(20);

                //DownloadByHTTP("http://" + str_Server + "/Models/" + str_Current_TSD_Model + "/boot.zip", "\\Temp\\tsd_autosetup\\boot\\boot.zip");
                //Unzip(@"\Temp\tsd_autosetup\boot\boot.zip", @"\Temp\tsd_autosetup\");
                //File.Delete(@"\Temp\tsd_autosetup\boot\boot.zip");

                DownloadByHTTP("http://" + str_Server + "/Models/" + str_Current_TSD_Model + "/soft.zip", "\\Temp\\tsd_autosetup\\soft\\soft.zip");
                Unzip(@"\Temp\tsd_autosetup\soft\soft.zip", @"\Temp\tsd_autosetup\");
                File.Delete(@"\Temp\tsd_autosetup\soft\soft.zip");

                SetProgressBar(25);

                DownloadByHTTP("http://" + str_Server + "/Users/" + macaddress + "/info.zip", @"\Temp\tsd_autosetup\temp\info.zip");
                Unzip(@"\Temp\tsd_autosetup\temp\info.zip", @"\Temp\tsd_autosetup\temp");
                File.Delete(@"\Temp\tsd_autosetup\temp\info.zip");

                SetProgressBar(30);

                //ProccessBootBlock();
                ProccessSoftBlock();
                UnloadDataWedge();

                ProccessMainBlock();
                ProccessApply();
                StartUpAply();

                using (StreamReader sr = new StreamReader(@"\Temp\tsd_autosetup\temp\info.txt"))
                {
                    str_dummyusr = sr.ReadLine();
                    str_dummypwd = sr.ReadLine();
                }

                AddToLog("TSD№:" + str_dummyusr);

                //Read and Replaces text in a file RDP 
                Encoding unicode = Encoding.Unicode;
                ReplaceInFile(str_rdpfile, "dummysrv", str_dummysrv);

                //Read and Replaces text in a file script
                ReplaceInFile(str_mscrfile, "dummyusr", str_dummyusr);

                //Read and Replaces text in a file script 
                ReplaceInFile(str_mscrfile, "dummypwd", str_dummypwd);

                //Read and Replaces text in a file script
                AddToLog("TS_SRV:" + str_dummysrv);
               ReplaceInFile(str_mscrfile, "dummysrv", str_dummysrv);


                // Возвращаем кнопку "ЗАВЕРШИТЬ"
                //BeginInvoke((Action)delegate()
                //{
                //    button1.Text = "ГОТОВО";
                //    button1.Enabled = true;

                //});

                AddToLog("Recovery completed.");
                SetProgressBar(90);
                AddToLog("Выполнено...90%");

                RunProcess(@"\Application\tsd_autosetup\soft\DataWedge\DWCtlApp.exe", "");
                AddToLog("Start DataWedge");

                RunProcess(@"\Windows\regmerge.exe", @"/d /q \Application\tsd_autosetup\soft\AppCenter");
                AddToLog("Register AppCenter");

                AddToLog("Выполнено...100%");
                SetProgressBar(100);

                AddToLog("Нажмите Готово");

                RunProcess(@"\Application\tsd_autosetup\soft\AppCenter\AppCenter.exe", "");
                AddToLog("Start AppCenter");

                BeginInvoke((Action)delegate()
                {
                    Close();
                });
             }
            
            else
            {
                BeginInvoke((Action)delegate() 
                { 
                    Close(); 
                });

            }
        }

        public DeviceProvisor(string[] args)
        {
            InitializeComponent();


            AddToLog("Загрузка...");
            AddToLog("Выберите вариант загрузки...");

            arg = args;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void DeviceProvisor_KeyPress(object sender, KeyPressEventArgs e)
        {
            MessageBox.Show("ТСД Загружается...");

            this.Refresh();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        
        }

        public string[] arg;

        private double GetFileSize(string uriPath)
        {
            /*
            var webRequest = HttpWebRequest.Create(new Uri(uriPath));
            webRequest.Method = "HEAD";*/

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(new Uri(uriPath));
            myHttpWebRequest.Method = "HEAD";
            myHttpWebRequest.Timeout = 10000;

            var fileSize = "0";

            for (var a = 0; a <= 10; a++)
            {
                try
                {
                    myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    fileSize = myHttpWebResponse.Headers.Get("Content-Length");

                    break;

                    /*
                    using (var webResponse = webRequest.GetResponse())
                    {
                        fileSize = webResponse.Headers.Get("Content-Length");

                        break;                        
                    }*/
                }
                catch
                {
                    Thread.Sleep(3000);
                }

            }

            return Convert.ToDouble(fileSize);
        }

        void CheckUpdate()
        {
            string exeFName = System.Reflection.Assembly.GetCallingAssembly().GetName().CodeBase;
            string UpdaterFName = exeFName.Remove(exeFName.Length - 4, 4) + "_new.exe";

            if (arg.Length > 0)
            {
                AddToLog("Командная строка: " + arg[0]);
                Thread.Sleep(1000);

                /// Очистка мусора после обновления
                if (arg[0].Contains("afterupdate"))
                {
                    if (File.Exists(UpdaterFName))
                    {
                        for (var a = 0; a < 5; a++)
                        {
                            try { File.Delete(UpdaterFName); }
                            catch { Thread.Sleep(1000); }
                        }
                    }
                }
            }

            // Проверяем: может быть мы являемся Updater'ом?
            if (exeFName.Contains(@"_new.exe"))
            {
                AddToLog("Режим обновления");
                Thread.Sleep(1000);

                string originalFName = exeFName.Replace("_new", "");

                // Пытаемся удалить оригинальный файл
                if (File.Exists(originalFName))
                {
                    for (var a = 0; a < 5; a++)
                    {
                        try
                        {
                            File.Delete(originalFName);
                        }
                        catch
                        { Thread.Sleep(1000); }
                    }
                }

                File.Copy(exeFName, originalFName);

                AddToLog("Обновление завершено");
                Thread.Sleep(1000);

                RunProcess(originalFName, "afterupdate");

                /// Закрываем текущий процесс
                bUpdateMode = true;
            }
            else
            {
                string exePathName = Path.GetDirectoryName(exeFName);
                FileInfo exefile = new FileInfo(exeFName);

                string exeFNameOnServer = @"http://" + str_Server + "/" + exefile.Name;
                double sizeOnServer = GetFileSize(exeFNameOnServer);

                 //Сверяем размеры исполняемого и серверного файлов
                 //если разные - значит принимаем решение качать файл с сервера
                if ((exefile.Length != sizeOnServer)) //(было "!=" вместо "=")внес изменения, чтобы при каждой проверке он считал что есть обновление
                {
                    AddToLog("Обнаружена новая версия");
                    AddToLog("Скачивание обновления...");
                    if (File.Exists(UpdaterFName)) { File.Delete(UpdaterFName); }
                    DownloadByHTTP(exeFNameOnServer, UpdaterFName);

                    AddToLog("Запускаем Updater");
                    RunProcess(UpdaterFName, "");

                    /// Закрываем текущий процесс
                    bUpdateMode = true;
                    BeginInvoke((Action)delegate() { Close(); }); Application.Exit();
                }
                else
                {
                    AddToLog("Используется последняя версия.");
                }

            }
        }

        private void button2_Click(object sender, EventArgs e) //Global
        {
            str_Server = "dc-ts02/tsd"; // Тестовый хост dc-test03. В дальнейшем необходимо перенести на dc-ts02
            
            
            str_rdpfile = @"\Temp\tsd_autosetup\run\global.rdp"; //RDP-файл Global
            str_mscrfile = @"\Temp\tsd_autosetup\run\do_global.mscr"; //Файл скрипта запуска

            Thread workerThread = new Thread(BeginProvising); //Запуск основной части программы
            workerThread.Start();
            ProccessTSDModel();  //запуск процесса определения модели ТСД
            //str_dummysrv = str_tsglb_srv; //сервер подключения, был dc-ts02
            using (StreamReader sr = new StreamReader(@"\Temp\tsglb_srv.txt")) //запись файла с именем сервере
            {                                                                  //было сделано распределение по моделям ТСД а не по чет/нечет мак адреса
                str_dummysrv = sr.ReadLine();
            }

        }

        private void button3_Click(object sender, EventArgs e) //IM Sklad
        {
            str_Server = "dc-test03/tsd/tsd_im"; // Тестовый хост. В дальнейшем необходимо перенести на dc-ts04
            str_dummysrv = "dc-ts04"; //сервер подключения
            str_rdpfile = @"\Temp\tsd_autosetup\run\galaxy.rdp"; //RDP-файл Galaxy
            str_mscrfile = @"\Temp\tsd_autosetup\run\do_galaxy.mscr"; //Файл скрипта запуска


            Thread workerThread = new Thread(BeginProvising); //Запуск основной части программы
            workerThread.Start();
            ProccessTSDModel();  //запуск процесса определения модели ТСД
        }

    }
}