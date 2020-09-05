using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.IO
{
    public abstract class DirectoryFolder : IDisposable
    {
        public static DirectoryFolder OpenFolder(string path) =>
            new FolderDirectory(path);

        protected string FixSlashes(string path) => path.Replace("\\", "/");

        public string ReadAllText(string path)
        {
            path = FixSlashes(path);
            using(var reader = new StreamReader(OpenStream(path)))
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
}
