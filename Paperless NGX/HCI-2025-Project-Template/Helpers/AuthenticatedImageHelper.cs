using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HCI_2025_Project_Template.Services;

namespace HCI_2025_Project_Template.Helpers
{
    public static class AuthenticatedImageHelper
    {
        public static async Task<BitmapImage?> LoadImageAsync(string url)
        {
            try
            {
                var client = Services.ApiService.GetClient();
                var bytes = await client.GetByteArrayAsync(url);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(bytes);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }
    }
}