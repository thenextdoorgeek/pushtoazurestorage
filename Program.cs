﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.WindowsAzure.Jobs;

namespace PushToStorage
{
    class AzureStorageHelper
    {
        bool deleteAfterUpload = false;
        JobHost _host;
        public AzureStorageHelper(JobHost host)
        {
            _host = host;
            string tmp = ConfigurationManager.AppSettings["DeleteAfterUpload"];
            if (tmp != null)
            {
                deleteAfterUpload = (Int32.Parse(tmp) == 1) ? true : false;
            }
        }

        public static void Upload(string name, string path, // Local file 
                                    [BlobOutput("freblogs/{name}")] Stream output,
                                    bool deleteAfterUpload)
        {
            using (var fileStream = System.IO.File.OpenRead(path))
            {
                fileStream.CopyTo(output);
            }

            if (deleteAfterUpload)
            {
                File.Delete(path);
            }
        }
        public void UploadFileToBlob(string name, string path)
        {
            Console.WriteLine("UploadFileToBlob('" + name + "','" + path + "')");
            var method = typeof(AzureStorageHelper).GetMethod("Upload");
            _host.Call(method, new { name = name, path = path, deleteAfterUpload = deleteAfterUpload });
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
            JobHost host = new JobHost();
            azStorageHelper = new AzureStorageHelper(host);

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

            host.RunAndBlock();
        }

        static void fsw_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(e.Name);
            FileInfo fileInfo = new FileInfo(e.FullPath);
            while (!azStorageHelper.IsFileReady(e.FullPath))
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine(" Created " + e.Name + " " + e.FullPath);
            azStorageHelper.UploadFileToBlob(e.Name, e.FullPath);
        }
    }
}