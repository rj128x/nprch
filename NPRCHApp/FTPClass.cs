using BytesRoad.Net.Ftp;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPRCHApp
{
    /// <summary>
    /// Класс для работы с ftp
    /// </summary>
    public class FTPClass
    {
        public static Dictionary<string, FtpClient> clients;
        public static int timeout = 1000;

        public static FtpClient getClient()
        {
            FtpClient client;
           
            if (clients == null)
                clients = new Dictionary<string, FtpClient>();
            if (!clients.ContainsKey(Settings.Single.FTPServer))
            {
                client = new FtpClient();
                client.PassiveMode = !Settings.Single.FTPActive;
                clients.Add(Settings.Single.FTPServer, client);
            }
            client = clients[Settings.Single.FTPServer];
            if (!client.IsConnected)
            {
                Logger.Info("try connect " + Settings.Single.FTPServer);
                client.Connect(timeout, Settings.Single.FTPServer, Settings.Single.FTPPort);
                Logger.Info("try login");
                client.Login(timeout, Settings.Single.FTPUser, Settings.Single.FTPPassword);
                Logger.Info("login");
            }

            return client;
        }


        public static List<string> ReadFile(DateTime date, string block,out string fileName)
        {
            bool ok = false;
            fileName = "";
            int step = 3;
            while (!ok && step > 0)
            {
                try
                {
                    step--;
                    FtpClient client = getClient();

                    DirectoryInfo dir = new DirectoryInfo(String.Format("{0}", System.AppDomain.CurrentDomain.BaseDirectory + "/temp/"));
                    try
                    {
                        dir.Create();
                    }
                    catch { }

                    string path = String.Format("/{0}/{1}/{2}/{3}/{4}", Settings.Single.FTPFolder, block, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));
                    string fn = String.Format("{0}{1}{2}{3}{4}.txt.zip", block, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"), date.ToString("HH"));
                    fileName = fn;
                    client.ChangeDirectory(timeout, path);
                    //FtpItem[] files = client.GetDirectoryList(timeout);

                    string resFile = String.Format("{0}/{1}", dir.FullName, fn);
                    string resFileTXT = resFile.Replace(".zip", "");
                    client.GetFile(timeout, resFile, fn);

                    try
                    {
                        new FileInfo(resFileTXT).Delete();
                    }
                    catch { }

                    ZipFile zf = new ZipFile(resFile);
                    zf.ExtractAll(dir.FullName);


                    List<string> result = File.ReadAllLines(resFileTXT).ToList<String>();
                    ok = true;

                    try
                    {
                        new FileInfo(resFileTXT).Delete();
                    }
                    catch { }

                    try
                    {
                        zf.Dispose();
                        new FileInfo(resFile).Delete();
                    }
                    catch { }


                    return result;

                }
                catch (Exception e)
                {
                    Logger.Info("Ошибка при отправке файла");
                    Logger.Info(e.ToString());
                    ok = false;

                    //MailClass.SendTextMail(String.Format("Ошибка при отправке отчета НПРЧ {0} ", fileName), e.ToString());
                }
            }
            return new List<string>();

        }


        public static bool CheckFile(DateTime date, string block)
        {
            bool ok = false;
            int step = 3;
            while (!ok && step > 0)
            {
                step--;
                try
                {
                    FtpClient client = getClient();


                    string path = String.Format("/{0}/{1}/{2}/{3}/{4}", Settings.Single.FTPFolder, block, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));
                    string fn = String.Format("{0}{1}{2}{3}{4}.txt.zip", block, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"), date.ToString("HH"));
                    client.ChangeDirectory(timeout, path);
                    FtpItem[] files = client.GetDirectoryList(timeout);

                    foreach (FtpItem ftpFile in files)
                    {
                        if (ftpFile.Name == fn)
                            return true;
                    }

                }
                catch (Exception e)
                {
                    Logger.Info("Ошибка при отправке файла");
                    Logger.Info(e.ToString());
                    ok = false;

                    //MailClass.SendTextMail(String.Format("Ошибка при отправке отчета НПРЧ {0} ", fileName), e.ToString());
                }
            }
            return ok;

        }

    }
}
