using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.IO
{
    public enum DirectoryType
    {
        Folder, Zip, Zrp, Rar, SevenZip, Tar
    }

    public class DirectoryFolderLocation
    {
        public DirectoryFolderLocation(string path, DirectoryType type)
        {
            Path = path;
            Type = type;
        }

        public string Path { get; }
        public string Filename => System.IO.Path.GetFileName(Path);
        public DirectoryType Type { get; }

        public override bool Equals(object obj)
        {
            return obj is DirectoryFolderLocation location &&
                   Path == location.Path &&
                   Filename == location.Filename &&
                   Type == location.Type;
        }

        public override int GetHashCode()
        {
            int hashCode = -501593077;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename);
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            return hashCode;
        }
    }

    public abstract class DirectoryFolder : IDisposable
    {
        public static DirectoryFolderLocation[] ListDirectories(string path)
        {
            var locations = new List<DirectoryFolderLocation>();
            foreach (var p in Directory.GetDirectories(path))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Folder));
            }
            foreach (var p in Directory.GetFiles(path, "*.zip"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Zip));
            }
            foreach (var p in Directory.GetFiles(path, "*.zrp"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Zrp));
            }
            foreach (var p in Directory.GetFiles(path, "*.rar"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Rar));
            }
            foreach (var p in Directory.GetFiles(path, "*.7z"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.SevenZip));
            }
            foreach (var p in Directory.GetFiles(path, "*.tar"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Tar));
            }
            foreach (var p in Directory.GetFiles(path, "*.tar.bz"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Tar));
            }
            foreach (var p in Directory.GetFiles(path, "*.tar.gz"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Tar));
            }
            foreach (var p in Directory.GetFiles(path, "*.tar.xz"))
            {
                locations.Add(new DirectoryFolderLocation(p, DirectoryType.Tar));
            }

            return locations.ToArray();
        }

        public static DirectoryFolder Open(DirectoryFolderLocation location) =>
            Open(location.Path, location.Type);

        public static DirectoryFolder Open(string path, DirectoryType type)
        {
            switch (type)
            {
                case DirectoryType.Folder:
                    return OpenFolder(path);
                case DirectoryType.SevenZip:
                case DirectoryType.Tar:
                case DirectoryType.Zip:
                case DirectoryType.Rar:
                    return OpenZip(path, type);
                case DirectoryType.Zrp:
                    return OpenZrp(path);
                default:
                    throw new NotSupportedException();
            }
        }

        public static DirectoryFolder OpenFolder(string path) =>
            new FolderDirectory(path);
        public static DirectoryFolder OpenZip(string path, DirectoryType type) =>
            new ZipDirectory(path, type);
        public static DirectoryFolder OpenZrp(string path) =>
            new ZrpDirectory(path);

        protected string FixSlashes(string path) => path.Replace("\\", "/");

        public string ReadAllText(string path)
        {
            path = FixSlashes(path);
            using (var reader = new StreamReader(OpenStream(path)))
            {
                return reader.ReadToEnd();
            }
        }

        public Stream OpenStream(string path)
        {
            path = FixSlashes(path);
            if (!Exists(path)) throw new IOException($"File {path} does not exist in directory");
            return OpenStreamInternal(path);
        }

        protected abstract Stream OpenStreamInternal(string path);
        public abstract bool Exists(string path);

        public abstract void Dispose();

        public abstract IEnumerable<string> FindAllFilenames();
        public string[] FindByFilename(string filename)
        {
            return FindAllFilenames()
                .Where(s => s.EndsWith("/" + filename) || s == filename)
                .ToArray();
        }
        public string[] FindByFilenameEnding(string filenameEnding)
        {
            return FindAllFilenames()
                .Where(s => s.EndsWith(filenameEnding))
                .ToArray();
        }
    }

    class FolderDirectory : DirectoryFolder
    {
        string folder;

        public FolderDirectory(string path)
        {
            folder = path;
        }

        public override void Dispose()
        {

        }

        public override bool Exists(string path)
        {
            path = FixSlashes(path);
            return File.Exists(Path.Combine(folder, path));
        }

        public override IEnumerable<string> FindAllFilenames()
        {
            return Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Select(s => s.Substring(folder.Length + 1).Replace("\\", "/"));
        }

        protected override Stream OpenStreamInternal(string path)
        {
            return File.Open(Path.Combine(folder, path), FileMode.Open, FileAccess.Read);
        }
    }

    class ZipDirectory : DirectoryFolder
    {
        IArchive archive;
        Stream stream;

        public ZipDirectory(string filename, DirectoryType type)
            : this(new BufferedStream(File.Open(filename, FileMode.Open, FileAccess.Read)), type)
        { }

        public ZipDirectory(Stream stream, DirectoryType type)
        {
            this.stream = stream;
            switch (type)
            {
                case DirectoryType.Zip:
                    archive = ZipArchive.Open(stream);
                    break;
                case DirectoryType.Rar:
                    archive = RarArchive.Open(stream);
                    break;
                case DirectoryType.SevenZip:
                    archive = SevenZipArchive.Open(stream);
                    break;
                case DirectoryType.Tar:
                    archive = TarArchive.Open(stream);
                    break;
                default:
                    throw new ArgumentException($"Invalid archive directory type: {type}", nameof(type));
            }
        }

        public override void Dispose()
        {
            archive.Dispose();
            stream.Dispose();
        }

        string NormalizePath(string path) =>
            path.Replace('\\', '/').Trim().Trim('/').Trim();

        public override bool Exists(string path)
        {
            path = NormalizePath(path);
            return archive.Entries.Where(e => e.Key == path).Take(1).Count() > 0;
        }

        public override IEnumerable<string> FindAllFilenames()
        {
            return archive.Entries.Select(e => e.Key);
        }

        protected override Stream OpenStreamInternal(string path)
        {
            path = NormalizePath(path);
            var entries = archive.Entries.Where(e => e.Key == path).Take(1).ToArray();
            if (entries.Length == 0) throw new FileNotFoundException("File not found in archive", path);

            return entries[0].OpenEntryStream();
        }
    }

    class ZrpDirectory : ZipDirectory
    {
        static Stream DecodeZrp(Stream encoded)
        {
            var key = new byte[16];
            var iv = new byte[16];
            encoded.Read(key, 0, 16);
            encoded.Read(iv, 0, 16);

            var zrpstream = new MemoryStream();

            using (AesManaged aes = new AesManaged())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(encoded, decryptor, CryptoStreamMode.Read))
                {
                    cs.CopyTo(zrpstream);
                }
                zrpstream.Position = 0;
            }
            return zrpstream;
        }

        public ZrpDirectory(string filename)
            : this(new BufferedStream(File.Open(filename, FileMode.Open, FileAccess.Read)))
        { }

        public ZrpDirectory(Stream stream) : base(DecodeZrp(stream), DirectoryType.Zip)
        { }
    }
}
