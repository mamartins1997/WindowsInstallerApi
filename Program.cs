using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;

namespace WindowsInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            if(IsAdministrator())
            {
                var pathRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\wwwroot\\";
                bool pathDna = File.Exists(pathRoot + "DnaApi.exe");
                
                string pathExec = @"C:\Users\" + Environment.UserName + @"\Documents\";
                pathExec = CreateFolderStructure(pathExec);

                bool fileExec = File.Exists(pathExec + "DnaApi.exe");
                if(!fileExec)
                    DirectoryCopy(pathRoot, pathExec, true);

                string serviceName = "apiHardwareDna";

                if (!IsInstalled(serviceName))
                {
                    InstallAndStart(serviceName, pathExec + "DnaApi.exe");
                }
                else
                {
                    Console.Write(serviceName + " já foi instalado. Você deseja desinstalar o serviço?. S/N.?");
                    string strKey = Console.ReadLine();

                    if (!string.IsNullOrEmpty(strKey) && strKey.ToLower().StartsWith("s"))
                    {
                        Uninstall(serviceName);

                        Console.Write(serviceName + " desinstalado.!");
                        Console.Read();
                    }
                }
            }
            else
            {
                Console.WriteLine("\n Por favor rode o instalador como administrador!");
                Console.ReadKey();
            }
        }

        private static void Uninstall(string serviceName)
        {
            string command = $"sc stop {serviceName}";
            CMDCommand(command);

            command = $"sc delete {serviceName}";
            CMDCommand(command);
        }


        private static void InstallAndStart(string serviceName, string executable)
        {
            Console.WriteLine("\n Iniciando instalacao do DNA...");
            
            string command = $"sc create {serviceName} binPath={executable}";
            CMDCommand(command);

            command = $"sc start {serviceName}";
            CMDCommand(command);

            command = $"sc config {serviceName} start=auto";
            CMDCommand(command);

        }

        private static bool IsInstalled(string serviceName)
        {
            // get list of Windows services
            ServiceController[] services = ServiceController.GetServices();

            // try to find service name
            foreach (ServiceController service in services)
            {
                if (service.ServiceName == serviceName)
                    return true;
            }
            return false;
        }

        private static void CMDCommand(string command)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());

        }

        private static string CreateFolderStructure(string path)
        {
            if (!System.IO.Directory.Exists(path + "Applications"))
                System.IO.Directory.CreateDirectory(path + "Applications");
                path += "Applications";
            if (!System.IO.Directory.Exists(path + "\\Dna"))
                System.IO.Directory.CreateDirectory(path + "\\Dna");
                path += "\\Dna\\";

                DirectoryInfo di = new DirectoryInfo(path);
                if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    //Add Hidden flag    
                    di.Attributes |= FileAttributes.Hidden;
                }
                return path;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Pasta não encontrada: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
