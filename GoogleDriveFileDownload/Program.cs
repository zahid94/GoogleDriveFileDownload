// See https://aka.ms/new-console-template for more information

using GoogleDriveFileDownload;

GoogleService googleService = new GoogleService();
var files = await googleService.GetFiles("1Hji7rmlgY-8PwqXQ9mlTKzGIBfQzsa8k");
foreach (var file in files)
{
   await googleService.DownloadGoogleFile(file.File.Id, file.Path);
}
Console.WriteLine("Hello, World!");
