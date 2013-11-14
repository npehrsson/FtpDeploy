using System;
using System.IO;
using System.Linq;
using System.Net;

namespace FtpDeploy {
    public class FtpDeployer {
        private const string DirectoryIdentifier = "d";
        private const string FileIdentifier = "-";
        private readonly Uri _uri;

        public FtpDeployer(Uri uri) {
            if (uri == null) throw new ArgumentNullException("uri");
            _uri = uri;
        }

        public void Deploy(DirectoryInfo directoryInfo) {
            if (directoryInfo == null) throw new ArgumentNullException("directoryInfo");
            DeleteDirectoryContent(_uri);
            Upload(directoryInfo, _uri);
        }

        private void Upload(DirectoryInfo directoryInfo, Uri uri) {
            var items = directoryInfo.GetFileSystemInfos();

            foreach (var fileSystemInfo in items) {
                var itemUri = new Uri(Combine(uri.ToString(), fileSystemInfo.Name));
                if (fileSystemInfo is DirectoryInfo) {
                    CreateDirectory(itemUri);
                    Upload((DirectoryInfo)fileSystemInfo, itemUri);
                    continue;
                }

                UploadFile((FileInfo)fileSystemInfo, itemUri);
            }
        }

        private void CreateDirectory(Uri itemUri) {
            var request = (FtpWebRequest)WebRequest.Create(itemUri);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            using (var response = (FtpWebResponse)request.GetResponse()) {
                if (response.StatusCode != FtpStatusCode.PathnameCreated)
                    throw new InvalidOperationException(response.StatusCode.ToString());
            }
        }

        private void UploadFile(FileInfo file, Uri itemUri) {
            var request = (FtpWebRequest)WebRequest.Create(itemUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;


            using (var fileStream = file.OpenRead()) {
                using (var requestStream = request.GetRequestStream())
                    fileStream.CopyTo(requestStream);
            }

            using (var response = (FtpWebResponse)request.GetResponse()) {
                if (response.StatusCode != FtpStatusCode.ClosingData)
                    throw new InvalidOperationException(response.StatusCode.ToString());
            }
        }

        private void DeleteDirectoryContent(Uri uri) {
            var list = GetDirectoryListContent(uri);
            var items = list.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items) {
                var itemName = ExtractName(item);
                var itemUri = new Uri(Combine(uri.ToString(), itemName));
                if (item.StartsWith(DirectoryIdentifier) && itemName != "." && itemName != "..") {
                    DeleteDirectoryContent(itemUri);
                    DeleteDirectory(itemUri);
                }

                if (item.StartsWith(FileIdentifier))
                    DeleteFile(itemUri);
            }
        }

        private void DeleteFile(Uri itemUri) {
            var request = (FtpWebRequest)WebRequest.Create(itemUri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            using (var response = (FtpWebResponse)request.GetResponse()) {
                if (response.StatusCode != FtpStatusCode.FileActionOK)
                    throw new InvalidOperationException(response.StatusCode.ToString());
            }
        }

        private string ExtractName(string itemDetailLine) {
            var item = itemDetailLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
            return item.Replace("\r", string.Empty);
        }

        private void DeleteDirectory(Uri uri) {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            using (var response = (FtpWebResponse)request.GetResponse()) {
                if (response.StatusCode != FtpStatusCode.FileActionOK)
                    throw new InvalidOperationException(response.StatusCode.ToString());
            }
        }

        private string GetDirectoryListContent(Uri uri) {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            using (var response = (FtpWebResponse)request.GetResponse()) {
                if (response.StatusCode != FtpStatusCode.OpeningData)
                    throw new InvalidOperationException(response.StatusCode.ToString());

                using (var stream = new StreamReader(response.GetResponseStream()))
                    return stream.ReadToEnd();
            }
        }

        public static string Combine(string uri1, string uri2) {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return string.Format("{0}/{1}", uri1, uri2);
        }
    }
}
