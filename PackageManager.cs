using PersonalPackageManager;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;


public partial class PackageManager {
  public static readonly string BaseFile = $"C:\\users\\{Environment.UserName}\\.ppm_bsave";

  public static string? VCXPROJ = null;
  public static string SaveFilePath = Path.Combine(Environment.CurrentDirectory, ".ppm_psave");

  public static Dictionary<string, string> Settings = new();

  public static Dictionary<string, string> PackageList = new();


  public static void Main() {
    var list = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.vcxproj").ToList();
    if (list.Count > 0 && list[0].EndsWith(".vcxproj"))
      VCXPROJ = list[0];



    if (!File.Exists(BaseFile))
      File.Create(BaseFile);
    else {
      foreach (string line in File.ReadAllLines(BaseFile)) {
        string[] spl = line.Split("*-");
        Settings.Add(spl[0], spl[1]);
      }
    }

    if (!Settings.ContainsKey("pPackageDirectoryLoc")) {
      Console.WriteLine("'pPackageDirectoryLoc' base setting is required to run");
      return;
    }

    if (Directory.Exists(Settings["pPackageDirectoryLoc"]))
      PackageList = new Dictionary<string, string>(Directory
                    .EnumerateDirectories(Settings["pPackageDirectoryLoc"])
                    .Select(T => new KeyValuePair<string, string>(Path.GetRelativePath(Settings["pPackageDirectoryLoc"], T), T)));


    bool running = true;


    try {
      while (running) {
        Console.Clear();
        Console.WriteLine("Personal Package Manager\n");

        List<string> args = GetUserArguements();

        switch (args[0].ToLower()) {
          case "-h":
          case "--h":
          case "help":
          case "-help":
          case "--help":
          case "-?":
          case "/?":
            Console.WriteLine("-h");
            Console.WriteLine("--h");
            Console.WriteLine("help");
            Console.WriteLine("-help");
            Console.WriteLine("--help");
            Console.WriteLine("-?");
            Console.WriteLine("/?");
            Console.WriteLine("  Displays commands\n");
            Console.WriteLine("-i");
            Console.WriteLine("interface");
            Console.WriteLine("  Opens interface commands for C++ projects\n");
            Console.WriteLine("-p");
            Console.WriteLine("package");
            Console.WriteLine("  Opens interface for packages\n");
            Console.WriteLine("-s");
            Console.WriteLine("settings");
            Console.WriteLine("  Opens interface for PPM settings\n");
            Console.WriteLine("-q");
            Console.WriteLine("quit");
            Console.WriteLine("  Closes the interface");
            EnterToContinue();

            break;


          case "-i":
          case "interface":
            if (VCXPROJ is null) {
              Console.WriteLine("Interface is only available in a VisualStudio C++ project directory");
              EnterToContinue();
              continue;
            }

            if (!InterfaceHandler.Handle())
              return;
            break;


          case "-p":
          case "package":
            PackageHandler.Handle();

            break;


          case "-s":
          case "settings":
            SettingsHandler.Handle(BaseFile);

            break;


          case "-q":
          case "quit":
            running = false;

            break;

          default:
            Console.WriteLine("Not a valid command");
            EnterToContinue();
            break;
        }
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }


    return;
  }


  public static void EnterToContinue() {
    Console.WriteLine("\nPress ENTER to continue...");
    Console.ReadLine();
  }



  public static List<string> GetUserArguements() {
    string input = Console.ReadLine() ?? string.Empty;
    if (input == string.Empty)
      input = "-h";

    // https://stackoverflow.com/a/14655199
    List<string> args = input.Split('"')
                .Select((element, index) => index % 2 == 0
                                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                        : [element])
                .SelectMany(element => element).ToList();

    return args;
  }



  // https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?view=net-10.0

  public static void CopyDirectory(string sourceDirectory, string targetDirectory) {
    DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
    DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

    CopyDirectoryAll(diSource, diTarget);
  }

  public static void CopyDirectoryAll(DirectoryInfo source, DirectoryInfo target) {
    Directory.CreateDirectory(target.FullName);

    foreach (FileInfo fi in source.GetFiles()) {
      fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
    }

    foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
      DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
      CopyDirectoryAll(diSourceSubDir, nextTargetSubDir);
    }
  }
}