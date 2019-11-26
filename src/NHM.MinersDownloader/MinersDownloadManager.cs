﻿using CG.Web.MegaApiClient;
using MyDownloader.Core;
using MyDownloader.Core.Extensions;
using MyDownloader.Core.UI;
using MyDownloader.Extension.Protocols;
using NHM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NHM.MinersDownloader
{
    public static class MinersDownloadManager
    {
        // don't use this it is faster but less stable
        public static bool UseMyDownloader { get; set; } = false;

        static MinersDownloadManager()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
        }

        public static Task<(bool success, string downloadedFilePath)> DownloadFileAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {

            // TODO switch for mega upload
            if (IsMegaUpload(url))
            {
                return DownlaodWithMegaAsync(url, downloadFileRootPath, fileNameNoExtension, progress, stop);
            }

            if (UseMyDownloader)
            {
                return DownlaodWithMyDownloaderAsync(url, downloadFileRootPath, fileNameNoExtension, progress, stop);
            }

            return DownloadFileWebClientAsync(url, downloadFileRootPath, fileNameNoExtension, progress, stop);
        }

        internal static bool IsMegaUpload(string url)
        {
            return url.Contains("mega.co.nz") || url.Contains("mega.nz");
        }

        internal static string GetFileExtension(string urlOrName)
        {
            var dotAt = urlOrName.LastIndexOf('.');
            if (dotAt < 0) return null;
            var extSize = urlOrName.Length - dotAt;
            return urlOrName.Substring(urlOrName.Length - extSize);
        }

        internal static string GetDownloadFilePath(string downloadFileRootPath, string fileNameNoExtension, string fileExtension)
        {
            return Path.Combine(downloadFileRootPath, $"{fileNameNoExtension}.{fileExtension}");
        }

        public static async Task<(bool success, string downloadedFilePath)> DownloadFileWebClientAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {
            var downloadFileLocation = GetDownloadFilePath(downloadFileRootPath, fileNameNoExtension, GetFileExtension(url));
            var downloadStatus = false;
            using (var client = new System.Net.WebClient())
            {
                client.Proxy = null;
                client.DownloadProgressChanged += (s, e1) => {
                    progress?.Report(e1.ProgressPercentage);
                };
                client.DownloadFileCompleted += (s, e) =>
                {
                    downloadStatus = !e.Cancelled && e.Error == null;
                };
                stop.Register(client.CancelAsync);
                // Starts the download
                await client.DownloadFileTaskAsync(new Uri(url), downloadFileLocation);
            }
            return (downloadStatus, downloadFileLocation);
        }

        // This is 2-5 times faster
        #region MyDownloader
        internal static Downloader CreateDownloader(string url, string downloadLocation)
        {
            var location = ResourceLocation.FromURL(url);
            var mirrors = new ResourceLocation[0];
            var downloader = DownloadManager.Instance.Add(
                location,
                mirrors,
                downloadLocation,
                10,
                true);

            return downloader;
        }

        // #2 download the file
        internal static async Task<(bool success, string downloadedFilePath)> DownlaodWithMyDownloaderAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {
            // these extensions must be here otherwise it will not downlaod
            var extensions = new List<IExtension>();
            try
            {
                extensions.Add(new CoreExtention());
                extensions.Add(new HttpFtpProtocolExtension());
            }
            catch { }

            var downloadFileLocation = GetDownloadFilePath(downloadFileRootPath, fileNameNoExtension, GetFileExtension(url));
            long lastProgress = 0;
            var ticksSinceUpdate = 0;
            bool _isDownloadSizeInit = false;
            var downloader = CreateDownloader(url, downloadFileLocation);

            var timer = new Timer((object stateInfo) =>
            {
                if (downloader.State != DownloaderState.Working) return;
                if (!_isDownloadSizeInit)
                {
                    _isDownloadSizeInit = true;
                }

                if (downloader.LastError != null)
                {
                    Logger.Info("MinersDownloadManager", $"Error occured while downloading: {downloader.LastError.Message}");
                }

                var speedString = $"{downloader.Rate / 1024d:0.00} kb/s";
                var percString = downloader.Progress.ToString("0.00") + "%";
                var labelDownloaded =
                    $"{downloader.Transfered / 1024d / 1024d:0.00} MB / {downloader.FileSize / 1024d / 1024d:0.00} MB";

                var progPerc = (int)(((double)downloader.Transfered / downloader.FileSize) * 100);
                var progMessage = $"{speedString}   {percString}   {labelDownloaded}";
                progress?.Report(progPerc);

                // Diagnostic stuff
                if (downloader.Transfered > lastProgress)
                {
                    ticksSinceUpdate = 0;
                    lastProgress = downloader.Transfered;
                }
                else if (ticksSinceUpdate > 20)
                {
                    Logger.Debug("MinersDownloadManager", "Maximum ticks reached, retrying");
                    ticksSinceUpdate = 0;
                }
                else
                {
                    Logger.Debug("MinersDownloadManager", $"No progress in ticks {ticksSinceUpdate}");
                    ticksSinceUpdate++;
                }
            });
            timer.Change(0, 500);

            stop.Register(() => {
                DownloadManager.Instance.RemoveDownload(downloader);
                timer.Dispose();
            });

            var tcs = new TaskCompletionSource<bool>();
            var onDownloadEnded = new EventHandler<DownloaderEventArgs>((object sender, DownloaderEventArgs e) =>
            {
                timer.Dispose();
                if (downloader != null)
                {
                    if (downloader.State == DownloaderState.EndedWithError)
                    {
                        Logger.Info("MinersDownloadManager", $"Download didn't complete successfully: {downloader.LastError.Message}");
                        tcs.SetResult(false);
                    }
                    else if (downloader.State == DownloaderState.Ended)
                    {
                        Logger.Info("MinersDownloadManager", "DownloadCompleted Success");
                        tcs.SetResult(true);
                    }
                }
            });
            DownloadManager.Instance.DownloadEnded += onDownloadEnded;
            var result = await tcs.Task;
            DownloadManager.Instance.DownloadEnded -= onDownloadEnded;

            return (result, downloadFileLocation);
        }
        #endregion MyDownloader

        #region Mega

        internal static bool IsMegaURLFolder(string url)
        {
            return url.Contains("/#F!");
        }
        internal static Task<(bool success, string downloadedFilePath)> DownlaodWithMegaAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {
            if (IsMegaURLFolder(url))
            {
                return DownlaodWithMegaFromFolderAsync(url, downloadFileRootPath, fileNameNoExtension, progress, stop);
            }
            return DownlaodWithMegaFileAsync(url, downloadFileRootPath, fileNameNoExtension, progress, stop);
        }


        // non folder
        internal static async Task<(bool success, string downloadedFilePath)> DownlaodWithMegaFileAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {
            var client = new MegaApiClient();
            var downloadFileLocation = "";
            try
            {
                client.LoginAnonymous();
                Uri fileLink = new Uri(url);
                INodeInfo node = await client.GetNodeFromLinkAsync(fileLink);
                Console.WriteLine($"Downloading {node.Name}");
                var doubleProgress = new Progress<double>((p) => progress?.Report((int)p));
                downloadFileLocation = GetDownloadFilePath(downloadFileRootPath, fileNameNoExtension, GetFileExtension(node.Name));
                await client.DownloadFileAsync(fileLink, downloadFileLocation, doubleProgress, stop);
            }
            catch(Exception e)
            {
                Logger.Error("MinersDownloadManager", $"MegaFile error: {e.Message}");
            }
            finally
            {
                client.Logout();
            }

            var success = File.Exists(downloadFileLocation);
            return (success, downloadFileLocation);
        }

        internal static async Task<(bool success, string downloadedFilePath)> DownlaodWithMegaFromFolderAsync(string url, string downloadFileRootPath, string fileNameNoExtension, IProgress<int> progress, CancellationToken stop)
        {
            var client = new MegaApiClient();
            var downloadFileLocation = "";
            try
            {
                client.LoginAnonymous();
                var splitted = url.Split('?');
                var foldeUrl = splitted.FirstOrDefault();
                var id = splitted.Skip(1).FirstOrDefault();
                var folderLink = new Uri(foldeUrl);
                //INodeInfo node = client.GetNodeFromLink(fileLink);
                var nodes = await client.GetNodesFromLinkAsync(folderLink);
                var node = nodes.FirstOrDefault(n => n.Id == id);
                //Console.WriteLine($"Downloading {node.Name}");
                var doubleProgress = new Progress<double>((p) => progress?.Report((int)p));
                downloadFileLocation = GetDownloadFilePath(downloadFileRootPath, fileNameNoExtension, GetFileExtension(node.Name));
                await client.DownloadFileAsync(node, downloadFileLocation, doubleProgress, stop);
            }
            catch (Exception e)
            {
                Logger.Error("MinersDownloadManager", $"MegaFolder error: {e.Message}");
            }
            finally
            {
                client.Logout();
            }

            var success = File.Exists(downloadFileLocation);
            return (success, downloadFileLocation);
        }
        #endregion Mega 
    }
}
