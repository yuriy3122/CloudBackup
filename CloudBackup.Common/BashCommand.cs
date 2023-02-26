using System.Diagnostics;

namespace CloudBackup.Common
{
    public static class BashCommand
    {
        public static List<string> Run(string command)
        {
            var output = new List<string>();

            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = $"/bin/bash",
                    Arguments = "-c \" " + command + " \"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit();

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        output.Add(line);
                    }
                }
            }

            return output;
        }
    }
}