using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PersonalPackageManager {
  public class SettingsHandler {
    public static void Handle(string settingsFile) {
      if (!File.Exists(settingsFile)) {
        File.Create(settingsFile).Close();
      }


      bool running = true;
      bool changed = false;
      Dictionary<string, string> settings = new();

      foreach (string line in File.ReadAllLines(settingsFile)) {
        string[] spl = line.Split("*-");
        settings.Add(spl[0], spl[1]);
      }


      while (running) {
        Console.Clear();
        Console.Write("Personal Package Manager Settings\n");

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
            Console.WriteLine("list");
            Console.WriteLine("  Lists all settings\n");
            Console.WriteLine("-r");
            Console.WriteLine("remove { setting }");
            Console.WriteLine("  Removes a setting");
            Console.WriteLine("     setting   Setting to remove\n");
            Console.WriteLine("-s");
            Console.WriteLine("set { setting } { value }");
            Console.WriteLine("  Sets a setting");
            Console.WriteLine("     setting   Setting to set");
            Console.WriteLine("     value     Value to give setting\n");
            Console.WriteLine("-q");
            Console.WriteLine("quit");
            Console.WriteLine("  Closes the interface");
            PackageManager.EnterToContinue();

            break;


          case "-l":
          case "list":
            foreach (KeyValuePair<string, string> s in settings) {
              Console.WriteLine($"{s.Key} --- {s.Value}");
            }

            PackageManager.EnterToContinue();
            break;


          case "-r":
          case "remove":
            if (args.Count < 2) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }
            settings.Remove(args[1]);
            changed = true;

            PackageManager.EnterToContinue();
            break;


          case "-s":
          case "set":
            if (args.Count < 3) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }
            settings[args[1]] = args[2];
            changed = true;

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

      if (changed) {
        FileStream fs = new FileStream(settingsFile, FileMode.Truncate);
        StreamWriter sw = new StreamWriter(fs);
        foreach (KeyValuePair<string, string> setting in settings) {
          sw.WriteLine($"{setting.Key}*-{setting.Value}");
        }
        sw.Flush();
        fs.Flush();
        fs.Close();
      }
    }


  }
}
