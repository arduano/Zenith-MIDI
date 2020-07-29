using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Zenith
{
    public class Logger : Stream
    {
        Stream writeStream;

        bool debugFlagged = false;
        string filepath;

        StreamWriter writer;
        Stream stdout = Console.OpenStandardOutput();

        public Logger(string filepath)
        {
            var dir = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            this.filepath = filepath;
            writeStream = File.Open(filepath, FileMode.Create, FileAccess.Write);
            writer = new StreamWriter(this);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            writeStream.Write(buffer, offset, count);
            stdout.Write(buffer, offset, count);
        }

        public void OpenDebugOnClose()
        {
            debugFlagged = true;
        }

        public override void Close()
        {
            base.Close();
            writeStream.Close();
            stdout.Close();
            if (debugFlagged)
            {
                OpenLogData();
            }
        }

        public void OpenLogData()
        {
            Process.Start("cmd.exe", $"/title \"debug logs\" /c SET AV_LOG_FORCE_COLOR=true && type \"{filepath}\" && pause");
        }

        public void WriteLine(string text)
        {
            writer.WriteLine(text + "\n");
            writer.Flush();
        }
    }

    static class Logs
    {
        static string logFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Logs");

        static void CheckFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                var create = File.GetCreationTime(f);
                var diff = create - DateTime.Now;
                if (diff.TotalDays > 7) File.Delete(f);
            }
        }

        public static Logger GetLoggerForFolder(string folder, string extra = "")
        {
            var now = DateTime.Now;
            extra = extra == "" ? "" : "-" + extra;
            for (int i = 1; ; i++)
            {
                string name = $"{folder}-{now.Day}.{now.Month}.{now.Year}-{now.Hour}.{now.Minute}.{now.Second}-log{i}{extra}.txt";
                var path = Path.Combine(logFolder, folder, name);
                if (File.Exists(path)) continue;
                return new Logger(path);
            }
        }

        public static void InitLogs()
        {
            foreach (var d in Directory.GetDirectories(logFolder))
            {
                CheckFolder(d);
            }
        }

        public static Logger GetFFMpegLogger(bool mask)
        {
            return GetLoggerForFolder("ffmpeg", mask ? "mask" : "");
        }
    }
}
