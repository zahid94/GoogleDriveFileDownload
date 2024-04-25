using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Download;

namespace GoogleDriveFileDownload
{
    public class GoogleService
    {
        private const string _clientId = "334138030641-a9npl9l7nthd63m4lsvcuvtigt7ou30k.apps.googleusercontent.com";
        private const string _clientSecret = "SExbkQDtypNiqppV7X6QWoXG";
        private const string _accessToken = "ya29.a0Ad52N3_YP31lBziF7dJuNPedYuHIawvVPUtfekIjNip5dJtr0cTvljLt6t8a8aC9g09FdDNUtH4L-XPLlHTIqpj7rNSN3f97DnEn_ofJcxcC6e76ocYOyJINRV1yGlhk5JfvzJWTFcrWzr8IrD0IUxrEVF-ZZxvfuNK8aCgYKAegSARISFQHGX2Mi1TBUSO0ieujSIfqAmUHuGQ0171";
        private const string _refreshToken = "1//0goKTOSIvqU1iCgYIARAAGBASNwF-L9IrmvyFpNmjslrzL7iSjMBu5MoKDQHnb99buR-IXWTYWFOowmcze7NQ6E67Zk3nf2hDOto";
        private readonly List<string> _path = [];
        private readonly List<DriveFile> _driveFiles = [];
        public async Task<IList<DriveFile>> GetFiles(string fileId)
        {
            var service = GetDriveService();
            var request = service.Files.List();
            request.Fields = "nextPageToken, files(id, name, parents, createdTime, modifiedTime, mimeType)";
            request.Q = $"'{fileId}' in parents";
            var results = await RetrieveFiles(request);
            var folders = results.Where(x => x.MimeType == "application/vnd.google-apps.folder");
            foreach (var folder in folders)
            {
                _path.Add(folder.Name);
                await GetFiles(folder.Id);
                _path.Remove(folder.Name);
            }

            var files = results.Where(x => x.MimeType != "application/vnd.google-apps.folder");

            foreach (var file in files)
            {
                _path.Add(file.Name);
                string filePath = _path.Aggregate((current, next) => Path.Combine(current, next));
                _driveFiles.Add(new DriveFile()
                {
                    File = file,
                    Path = filePath
                });
                _path.Remove(file.Name);
            }
            return _driveFiles;
        }
        public async Task<string> DownloadGoogleFile(string fileId, string filepath)
        {
            if (File.Exists(filepath)) BackupFile(filepath);
            var service = GetDriveService();
            var request = service.Files.Get(fileId);
            using (MemoryStream stream = new MemoryStream())
            {
                request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading: break;
                        case DownloadStatus.Completed:
                            {
                                SaveStream(stream, filepath);
                                break;
                            }
                        case DownloadStatus.Failed: break;
                    }
                };
                await request.DownloadAsync(stream);
                return filepath;
            }
        }
        private async Task<IList<Google.Apis.Drive.v3.Data.File>> RetrieveFiles(FilesResource.ListRequest listRequest)
        {
            List<Google.Apis.Drive.v3.Data.File> result = new();
            do
            {
                try
                {
                    var files = await listRequest.ExecuteAsync();
                    result.AddRange(files.Files);
                    listRequest.PageToken = files.NextPageToken;
                }
                catch (Exception)
                {
                    listRequest.PageToken = null;
                }
            } while (!string.IsNullOrEmpty(listRequest.PageToken));
            return result;
        }
        private static void SaveStream(MemoryStream stream, string filePath)
        {
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(file);
            }
        }
        private static void BackupFile(string targetPath)
        {
            string fileName = Path.GetFileName(targetPath);
            string newName = $"{DateTime.Now:yyyy-MM-dd-hh-mm}-{fileName}";
            string directory = Path.GetDirectoryName(targetPath) + "__(Backup)";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string destinationPath = Path.Combine(directory, newName);
            File.Move(targetPath, destinationPath);
            DeleteFiles(directory, fileName);
        }
        private static void DeleteFiles(string source, string fileName)
        {
            DirectoryInfo directoryInfo = new(source);
            FileInfo[] fileslist = directoryInfo.GetFiles($"*{fileName}");
            if (fileslist.Length > 4)
            {
                fileslist = fileslist.OrderByDescending(o => o.CreationTime).Skip(3).ToArray();
                foreach (var file in fileslist)
                {
                    File.Delete(file.FullName);
                }
            }
        }
        private static DriveService GetDriveService()
        {
            UserCredential credential;
            string rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
            string credPath = Path.Combine(rootPath, "token");

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                },
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore(credPath, true)
            });

            var token = new TokenResponse
            {
                AccessToken = _accessToken,
                RefreshToken = _refreshToken
            };
            credential = new UserCredential(flow, Environment.UserName, token);

            //Create Drive API service.
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveRestAPI-v3",
            });
        }
    }
}
