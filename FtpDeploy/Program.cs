using System;
using System.IO;

namespace FtpDeploy {
    class Program {
        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.WriteLine("First parameter is required (Uri) seccond is optional (Directory)");
                Environment.Exit(-1);
            }

            Uri uri;
            if (!Uri.TryCreate(args[0], UriKind.Absolute, out uri)) {
                Console.WriteLine("could not parse uri");
                Environment.Exit(-1);
            }

            if (uri.Scheme.ToLower() != "ftp") {
                Console.WriteLine("Aspected ftp as scheme");
                Environment.Exit(-1);
            }

            var path = string.Empty;

            if (args.Length == 2)
                path = args[1];

            if (!Path.IsPathRooted(path))
                path = Path.Combine(Environment.CurrentDirectory, path);

            var directoryInfo = new DirectoryInfo(path);

            if (!directoryInfo.Exists) {
                Console.WriteLine("Directory does not exist");
                Environment.Exit(-1);
            }

            var client = new FtpDeployer(uri);
            client.Deploy(directoryInfo);
        }
    }
}