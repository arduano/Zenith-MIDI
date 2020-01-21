using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithShared
{
    public static class Languages
    {
        public static readonly string ApiURL = "https://api.github.com/repos/Hans5958/Zenith-MIDI-i18n/releases/latest";
        public static readonly string DataAssetName = "pack.zip";

        public static string GetLatestVersion() => ZenithUpdates.GetLatestVersion(ApiURL);
        public static Stream DownloadLatestVersion() => ZenithUpdates.DownloadAssetData(DataAssetName, ApiURL);

        public static void UnpackFromStream(Stream s) => UnpackFromStream(s, "Languages");

        public static void UnpackFromStream(Stream s, string dir)
        {
            using (ZipArchive archive = new ZipArchive(s))
            {
                foreach (var e in archive.Entries)
                {
                    if (e.FullName.StartsWith("en/")) continue;
                    if (e.FullName.EndsWith("\\") || e.FullName.EndsWith("/")) continue;
                    if (!Directory.Exists(Path.Combine(dir, Path.GetDirectoryName(e.FullName))))
                        Directory.CreateDirectory(Path.Combine(dir, Path.GetDirectoryName(e.FullName)));
                    try
                    {
                        e.ExtractToFile(Path.Combine(dir, e.FullName), true);
                    }
                    catch (IOException ex)
                    {
                        throw new InstallFailedException("Could not overwrite file " + Path.Combine(dir, e.FullName));
                    }
                }
            }
        }
    }
}
