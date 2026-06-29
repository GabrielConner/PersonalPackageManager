using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalPackageManager {
  public class PackageHandler {




    public static void Handle() {
      string package = string.Empty;

      bool running = true;

      while (running) {
        Console.Clear();
        Console.WriteLine("Personal Package Manager Packages\n");

        List<string> args = PackageManager.GetUserArguements();

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
            Console.WriteLine("-l");
            Console.WriteLine("list [ package ]");
            Console.WriteLine("  Lists all packages");
            Console.WriteLine("     package   Specific package to list");
            Console.WriteLine("-i");
            Console.WriteLine("init");
            Console.WriteLine("inititialize { package }");
            Console.WriteLine("  Intitializes a package");
            Console.WriteLine("     package   New package name\n");
            Console.WriteLine("-r");
            Console.WriteLine("remove { package }");
            Console.WriteLine("  Removes a package");
            Console.WriteLine("     package   Package name\n");
            Console.WriteLine("-s");
            Console.WriteLine("settings { package [ version ] } ");
            Console.WriteLine("  Opens interface for a specific package version");
            Console.WriteLine("     package   Package name");
            Console.WriteLine("     version   Specific version of the package");
            Console.WriteLine("-c");
            Console.WriteLine("create { package }{ version }");
            Console.WriteLine("  Sets the current live version of a package to a permanent version");
            Console.WriteLine("     package   Package to set\n");
            Console.WriteLine("     version   Name of new permanent version\n");
            Console.WriteLine("-q");
            Console.WriteLine("quit");
            Console.WriteLine("  Closes the interface");

            PackageManager.EnterToContinue();
            break;


          case "-l":
          case "list":
            if (args.Count >= 2) {
              if (!PackageManager.PackageList.ContainsKey(args[1])) {
                Console.WriteLine("Invalid package name");
                PackageManager.EnterToContinue();
                continue;
              }
              package = PackageManager.PackageList[args[1]];

              if (!Directory.Exists(package)) {
                Console.WriteLine("Invalid package");
                PackageManager.EnterToContinue();
                continue;
              }

              foreach (string dir in Directory.EnumerateDirectories(package)) {
                Console.WriteLine(Path.GetRelativePath(package, dir));
              }

              PackageManager.EnterToContinue();
              continue;
            }

            foreach (string p in PackageManager.PackageList.Keys) {
              Console.WriteLine(p);
            }
            PackageManager.EnterToContinue();
            break;


          case "-i":
          case "init":
          case "initialize":
            if (args.Count == 1) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              PackageManager.EnterToContinue();
              continue;
            }

            if (PackageManager.PackageList.ContainsKey(args[1])) {
              Console.WriteLine("Package already exists");
              PackageManager.EnterToContinue();
              PackageManager.EnterToContinue();
              continue;
            }


            string saveDir = Path.Combine(PackageManager.Settings["pPackageDirectoryLoc"], args[1]);
            PackageManager.PackageList.Add(args[1], saveDir);

            Directory.CreateDirectory(saveDir);
            Directory.CreateDirectory(Path.Combine(saveDir, "live"));
            PackageManager.EnterToContinue();

            break;


          case "-r":
          case "remove":
            if (args.Count == 2) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }

            if (!PackageManager.PackageList.ContainsKey(args[1])) {
              Console.WriteLine("Invalid package name");
              PackageManager.EnterToContinue();
              continue;
            }

            Directory.Delete(Path.Combine(PackageManager.Settings["pPackageDirectoryLoc"], args[1]), true);

            break;


          case "-s":
          case "settings":
            if (args.Count < 2) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }
            if (!PackageManager.PackageList.ContainsKey(args[1])) {
              Console.WriteLine("Invalid package name");
              PackageManager.EnterToContinue();
              continue;
            }

            package = PackageManager.PackageList[args[1]];

            string version = "live";
            if (args.Count > 2)
              version = args[2];

            string pathTo = Path.Combine(package, version);
            string settingsPath = Path.Combine(pathTo, ".ppm_pdata");

            if (!Directory.Exists(pathTo)) {
              Console.WriteLine("Invalid package version");
              PackageManager.EnterToContinue();
              continue;
            }

            SettingsHandler.Handle(settingsPath);

            break;


          case "-c":
          case "create":
            if (args.Count < 3) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }

            if (Directory.EnumerateDirectories(PackageManager.PackageList[args[1]]).Contains(args[2])) {
              Console.WriteLine("Version already exists");
              PackageManager.EnterToContinue();
              continue;
            }

            PackageManager.CopyDirectory(Path.Combine(PackageManager.PackageList[args[1]], "live"), Path.Combine(PackageManager.PackageList[args[1]], args[2]));
            PackageManager.EnterToContinue();

            break;


          case "-q":
          case "quit":
            running = false;

            break;

          default:
            Console.WriteLine("Not a valid command");
            PackageManager.EnterToContinue();
            break;
        }
      }

    }

  }
}
