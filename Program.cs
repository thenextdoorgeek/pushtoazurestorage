using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;

namespace PushToStorage
{
    class AzureStorageHelper
    {
        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
        CloudBlockBlob blockBlob;
        bool deleteAfterUpload = false;
        public AzureStorageHelper()
        {
            // Retrieve storage account from connection string.
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

            // Create the blob client.
            blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            container = blobClient.GetContainerReference("freblogs");

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();
            string tmp = ConfigurationManager.AppSettings["DeleteAfterUpload"];
            if (tmp != null)
            {
                deleteAfterUpload = (Int32.Parse(tmp) == 1) ? true : false;
            }

        }

        public void UploadFileToBlob(string name, string path)
        {
            try
            {
                Console.WriteLine("Starting uploading " + name);
                using (var fileStream = System.IO.File.OpenRead(path))
                {
                    blockBlob = container.GetBlockBlobReference(name);
                    blockBlob.UploadFromStream(fileStream);
                    Console.WriteLine(name + " successfully uploaded!");
                    if (deleteAfterUpload)
                    {
                        fileStream.Close();
                        File.Delete(path);
                        Console.WriteLine(path + " deleted!");
                    }
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }

        public bool IsFileReady(String sFilename)
        {
            try
            {
                using (FileStream fileStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (fileStream.Length > 0)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    class Program
    {
        static AzureStorageHelper azStorageHelper;
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing AzureStorageHelper!");
            azStorageHelper = new AzureStorageHelper();

            string path = ConfigurationManager.AppSettings["directory"];

            string[] directories = Directory.GetDirectories(path, "*W3SVC*");

            FileSystemWatcher[] fsw = new FileSystemWatcher[directories.Length];
            Console.WriteLine(path + " " + fsw.Length);
            for (int i = 0; i < directories.Length; i++)
            {
                fsw[i] = new FileSystemWatcher(directories[i]);
                fsw[i].NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
                fsw[i].Created += fsw_Created;
                fsw[i].IncludeSubdirectories = true;
                fsw[i].EnableRaisingEvents = true;
                Console.WriteLine(String.Format("{0} Started watching directory {1} for files!", DateTime.Now.ToString(), directories[i]));
            }

            Console.ReadLine();
            Console.WriteLine(DateTime.Now.ToString() + " Stopping!");
        }

        static void fsw_Created(object sender, FileSystemEventArgs e)
        {
            FileInfo fileInfo = new FileInfo(e.FullPath);
            while (!azStorageHelper.IsFileReady(e.FullPath))
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine(" Created " + e.Name + " " + e.FullPath);
            azStorageHelper.UploadFileToBlob(e.Name, e.FullPath);
        }
    }
}