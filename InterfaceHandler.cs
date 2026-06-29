using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonalPackageManager {
  public class InterfaceHandler {
    public static void DeletePath(string path) {
      if (!Path.Exists(path))
        return;

      FileAttributes attr = File.GetAttributes(path);

      if (attr.HasFlag(FileAttributes.Directory))
        Directory.Delete(path, true);
      else
        File.Delete(path);
    }


    public static string GetValidMacroName(string name) {
      StringBuilder builder = new StringBuilder(name.Length);

      for (int i = 0; i < name.Length; i++) {
        if (!Char.IsAsciiLetter(name[i]) && name[i] != '_') {
          builder.Append('_');
        } else {
          builder.Append(name[i]);
        }
      }

      return builder.ToString();
    }





    //----------------------------------------------------------------------------------------------------
    public static void Add(List<string> args, Dictionary<string, PackageEntry> packages, ProjectGrouping grouping) {
      if (!PackageManager.PackageList.ContainsKey(args[1])) {
        Console.WriteLine("Invalid package name");
        return;
      }

      if (packages.ContainsKey(args[1])) {
        Console.WriteLine($"{args[1]} is already in this project with version {packages[args[1]].Version}");
        return;
      }

      string version = "live";
      if (args.Count >= 3)
        version = args[2];

      string fullPath = Path.Combine(PackageManager.PackageList[args[1]], version);
      string settingsPath = Path.Combine(fullPath, ".ppm_pdata");
      string releaseSettings = Path.Combine(fullPath, "\\release\\", ".ppm_pdata");
      string debugSettings = Path.Combine(fullPath, "\\debug\\", ".ppm_pdata");

      if (!Directory.Exists(fullPath)) {
        Console.WriteLine("Invalid package version");
        return;
      }

      PackageEntry entry = new() {
        Name = args[1],
        Version = version,
      };
      packages.Add(args[1], entry);




      List<string> srcFiles = new();
      List<(string, string)> libraryFiles = new();
      List<string> shaderFiles = new();

      string srcPath = Path.Combine("src", args[1]);

      List<(string, string)> dirEntries = new List<(string, string)>(Directory.EnumerateDirectories(fullPath).Select(T => (fullPath, T))); 

      for (int i = 0; i < dirEntries.Count; i++) {
        (string path, string dir) = dirEntries[i];
        List<string>? files = null;
        string mod = Path.GetRelativePath(fullPath, path).ToLower();
        if (mod == ".") {
          mod = string.Empty;
        }
        string type = Path.GetRelativePath(path, dir).ToLower();

        switch (type) {
          case "src":
            files = Directory.EnumerateFiles(dir).ToList();

            if (version != "live") {
              PackageManager.CopyDirectory(dir, srcPath);
              files = files.Select(T => T = Path.Combine(srcPath, Path.GetRelativePath(dir, T))).ToList();
            }

            srcFiles.AddRange(files);
            break;
          case "include":

            if (version == "live") {
              grouping.AddIncludeDirectory(dir, mod);
              files = [dir];
            } else {
              PackageManager.CopyDirectory(dir, srcPath);
              grouping.AddIncludeDirectory(srcPath, mod);
              files = Directory.EnumerateFileSystemEntries(dir).Select(T => Path.Combine(srcPath, Path.GetRelativePath(dir, T))).ToList();
            }

            break;
          case "library":
            files = Directory.EnumerateFiles(dir).ToList();
            libraryFiles.AddRange(files.Select(T => (Path.GetRelativePath(dir, T), mod)));

            if (version == "live")
              grouping.AddLibraryDirectory(dir, mod);
            else {
              string libraryDir = Path.Combine("library", args[1]);
              PackageManager.CopyDirectory(dir, libraryDir);

              grouping.AddLibraryDirectory(libraryDir, mod);
              files = files.Select(T => Path.Combine(libraryDir, Path.GetRelativePath(dir, T))).ToList();
            }

            break;
          case "dynamic":
            PackageManager.CopyDirectory(dir, "./");

            break;
          case "shaders":
            PackageManager.CopyDirectory(dir, "./shaders");

            files = Directory.EnumerateFiles(dir).Select(T => Path.Combine("shaders", Path.GetRelativePath(dir, T))).ToList();
            shaderFiles.AddRange(files);

            break;
          case "release":
            files = Directory.EnumerateDirectories(dir).ToList();
            dirEntries.AddRange(files.Select(T => (dir, T)));

            continue;
          case "debug":
            files = Directory.EnumerateDirectories(dir).ToList();
            dirEntries.AddRange(files.Select(T => (dir, T)));

            continue;
        }

        if (files is not null) {
          if (entry.Files.ContainsKey(type))
            entry.Files[type] = entry.Files[type].Union(files).ToList();
          else
            entry.Files[type] = new(files);
        }
      }




      grouping.AddCompiledFiles(srcFiles);
      grouping.AddLibraryFiles(libraryFiles);
      grouping.AddShaderFiles(shaderFiles);



      Dictionary<string, (string, string)> settings = new();

      if (File.Exists(settingsPath)) {
        foreach (string line in File.ReadAllLines(settingsPath)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], ""));
        }
      }
      if (File.Exists(releaseSettings)) {
        foreach (string line in File.ReadAllLines(releaseSettings)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], "release"));
        }
      }
      if (File.Exists(debugSettings)) {
        foreach (string line in File.ReadAllLines(debugSettings)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], "debug"));
        }
      }


      foreach (KeyValuePair<string, (string set, string mod)> setting in settings) {
        switch (setting.Key) {
          case "dependencies":
            List<string> dependencies = setting.Value.set.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string dependency in dependencies) {
              List<string> tArgs = ["add"];
              tArgs.AddRange(dependency.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

              Add(tArgs, packages, grouping);
            }

            break;

          case "mincppversion":
            grouping.SetMinVersion(setting.Value.set, setting.Value.mod);

            break;

          case "preprocessor":
            List<string> preprocessor = setting.Value.set.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            grouping.AddPreprocessorDefinitions(preprocessor, setting.Value.mod);

            break;

          case "libraryDependencies":
            List<(string, string)> library = setting.Value.set.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(T => (T, setting.Value.mod)).ToList();
            grouping.AddLibraryFiles(library);
            break;
        }
      }

      string packageName = $"_PPM_{GetValidMacroName(args[1]).ToUpper()}";
      List<string> packageVersionAndName = [packageName, packageName + $"_{GetValidMacroName(version).ToUpper()}"];
      grouping.AddPreprocessorDefinitions(packageVersionAndName, "");
    }



    //----------------------------------------------------------------------------------------------------
    public static void Update(List<string> args, Dictionary<string, PackageEntry> packages, ProjectGrouping grouping) {
      if (!packages.ContainsKey(args[1]) || !PackageManager.PackageList.ContainsKey(args[1])) {
        Console.WriteLine("Not a valid package");
        return;
      }

      //PackageEntry entry = packages[args[1]];


      /*      string version = "live";
            if (args.Count >= 3)
              version = args[2];*/



      Remove(args, packages, grouping);
      Add(args, packages, grouping);


      /*string packagePath = PackageManager.PackageList[args[1]];
      string fullPath = Path.Combine(packagePath, version);
      string settingsPath = Path.Combine(fullPath, ".ppm_pdata");
      string releaseSettings = Path.Combine(fullPath, "\\release\\", ".ppm_pdata");
      string debugSettings = Path.Combine(fullPath, "\\debug\\", ".ppm_pdata");


      List<string> srcFiles = new();
      List<(string, string)> libraryFiles = new();
      List<string> shaderFiles = new();

      List<(string, string)> dirEntries = new List<(string, string)>(Directory.EnumerateDirectories(fullPath).Select(T => (fullPath, T)));

      for (int i = 0; i < dirEntries.Count; i++) {
        (string path, string dir) = dirEntries[i];

        List<string>? files = null;
        string mod = Path.GetRelativePath(fullPath, path).ToLower();
        if (mod == ".") {
          mod = string.Empty;
        }

        string type = Path.GetRelativePath(path, dir).ToLower();

        switch (type) {
          case "src":
            files = Directory.EnumerateFiles(dir).ToList();
            srcFiles.AddRange(files);
            break;
          case "include":
            files = [dir];

            grouping.AddIncludeDirectory(dir, mod);
            break;
          case "library":
            files = Directory.EnumerateFiles(dir).ToList();
            libraryFiles.AddRange(files.Select(T => (Path.GetRelativePath(dir, T), mod)));

            grouping.AddLibraryDirectory(dir, mod);

            break;
          case "dynamic":
            PackageManager.CopyDirectory(dir, "./");
            break;
          case "shaders":
            PackageManager.CopyDirectory(dir, "./shaders");
            files = Directory.EnumerateFiles(dir).Select(T => Path.Combine("shaders", Path.GetRelativePath(dir, T))).ToList();
            shaderFiles.AddRange(files);

            break;
        }

        if (files is not null) {
          if (entry.Files.ContainsKey(type))
            entry.Files[type] = entry.Files[type].Union(files).ToList();
          else
            entry.Files[type] = new(files);
        }
      }




      grouping.AddUniqueCompiledFiles(srcFiles);
      grouping.AddLibraryFiles(libraryFiles);
      grouping.AddUniqueShaderFiles(shaderFiles);



      Dictionary<string, (string, string)> settings = new();

      if (File.Exists(settingsPath)) {
        foreach (string line in File.ReadAllLines(settingsPath)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], ""));
        }
      }
      if (File.Exists(releaseSettings)) {
        foreach (string line in File.ReadAllLines(releaseSettings)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], "release"));
        }
      }
      if (File.Exists(debugSettings)) {
        foreach (string line in File.ReadAllLines(debugSettings)) {
          string[] spl = line.Split("*-");
          settings.Add(spl[0], (spl[1], "debug"));
        }
      }


      foreach (KeyValuePair<string, (string set, string mod)> setting in settings) {
        switch (setting.Key) {
          case "mincppversion":
            grouping.SetMinVersion(setting.Value.set, setting.Value.mod);

            break;

          case "preprocessor":
            List<string> preprocessor = setting.Value.set.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            grouping.AddPreprocessorDefinitions(preprocessor, setting.Value.mod);

            break;

          case "libraryDependencies":
            List<(string, string)> library = setting.Value.set.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(T => (T, setting.Value.mod)).ToList();
            grouping.AddLibraryFiles(library);
            break;
        }
      }*/
    }



    //----------------------------------------------------------------------------------------------------
    public static void Remove(List<string> args, Dictionary<string, PackageEntry> packages, ProjectGrouping grouping) {
      if (!packages.ContainsKey(args[1])) {
        Console.WriteLine("Invalid package");
        return;
      }

      List<string> srcFiles = new();
      List<string> libraryFiles = new();
      List<string> dynamicFiles = new();
      List<string> shaderFiles = new();
      string folder = "";
      PackageEntry entry = packages[args[1]];


      foreach (KeyValuePair<string, List<string>> type in entry.Files) {
        switch (type.Key.ToLower()) {
          case "src":
            srcFiles.AddRange(type.Value);

            if (entry.Version != "live") {
              DeletePath(Path.Combine("src", entry.Name));
              foreach (string path in type.Value)
                DeletePath(path);
            }
            break;
          case "include":

            if (entry.Version != "live") {
              foreach (string path in type.Value)
                DeletePath(path);
              grouping.RemoveIncludeDirectory(Path.Combine("src", entry.Name));
            } else if (type.Value.Count != 0)
              grouping.RemoveIncludeDirectory(type.Value[0]);


            break;
          case "library":
            if (type.Value.Count == 0) {
              continue;
            }

            folder = Path.GetDirectoryName(type.Value[0]);

            libraryFiles.AddRange(type.Value.Select(T => Path.GetRelativePath(folder, T)));
            grouping.RemoveLibraryDirectory(folder);

            if (entry.Version != "live")
              foreach (string path in type.Value)
                DeletePath(path);

            break;
          case "dynamic":
            type.Value.ForEach(T => File.Delete(T));
            break;
          case "shaders":
            shaderFiles.AddRange(type.Value);
            type.Value.ForEach(T => File.Delete(T));

            break;
        }
      }

      grouping.RemoveCompiledFiles(srcFiles);
      grouping.RemoveLibraryFiles(libraryFiles);
      grouping.RemoveShaderFiles(shaderFiles);

      string packageName = $"_PPM_{GetValidMacroName(args[1]).ToUpper()}";
      List<string> packageVersionAndName = [packageName, packageName + $"_{GetValidMacroName(entry.Version).ToUpper()}"];
      grouping.RemovePreprocessorDefinitions(packageVersionAndName);

      packages.Remove(args[1]);
    }



    //----------------------------------------------------------------------------------------------------
    public static void Seperate(List<string> args, Dictionary<string, PackageEntry> packages, ProjectGrouping grouping) {
      Console.WriteLine("This is a permanent action to remove ALL PersonalPackageManager utility");
      Console.WriteLine("If you are wanting to move project to another system, then updating live packages to constant versions is a better option");
      Console.Write("Continue? [Y/N]\n>>");
      string ans = (Console.ReadLine() ?? "N").ToLower();

      if (ans == "n" || ans != "y")
        return;


      foreach (PackageEntry entry in packages.Values) {
        if (entry.Version != "live" || !PackageManager.PackageList.ContainsKey(entry.Name))
          continue;

        string packageDir = PackageManager.PackageList[entry.Name];
        string fullPath = Path.Combine(packageDir, entry.Version);

        List<string> srcFiles = new();

        string srcPath = Path.Combine("src/ppm/", entry.Name);
        string libraryPath = Path.Combine("library/ppm/", entry.Name);
        string releasePath = Path.Combine(fullPath, "release");
        string debugPath = Path.Combine(fullPath, "debug");

        if (Path.Exists(releasePath)) {
          string include = Path.Combine(releasePath, "include");
          string library = Path.Combine(releasePath, "library");

          if (Path.Exists(include)) {
            string path = Path.Combine(srcPath, "release");
            grouping.RemoveIncludeDirectory(include);
            PackageManager.CopyDirectory(include, path);
            grouping.AddIncludeDirectory(path, "release");
          }
          if (Path.Exists(library)) {
            string path = Path.Combine(libraryPath, "release");
            grouping.RemoveLibraryDirectory(library);
            PackageManager.CopyDirectory(library, path);
            grouping.AddLibraryDirectory(path, "release");
          }
        }

        if (Path.Exists(debugPath)) {
          string include = Path.Combine(debugPath, "include");
          string library = Path.Combine(debugPath, "library");

          if (Path.Exists(include)) {
            string path = Path.Combine(srcPath, "debug");
            grouping.RemoveIncludeDirectory(include);
            PackageManager.CopyDirectory(include, path);
            grouping.AddIncludeDirectory(path, "debug");
          }
          if (Path.Exists(library)) {
            string path = Path.Combine(libraryPath, "debug");
            grouping.RemoveLibraryDirectory(library);
            PackageManager.CopyDirectory(library, path);
            grouping.AddLibraryDirectory(path, "debug");
          }
        }


        foreach (KeyValuePair<string, List<string>> type in entry.Files) {
          string dir = Path.Combine(fullPath, type.Key);
          if (!Path.Exists(dir)) {
            continue;
          }

          switch (type.Key.ToLower()) {
            case "src":
              srcFiles.AddRange(type.Value.Select(T => Path.Combine(srcPath, Path.GetRelativePath(dir, T))));
              grouping.RemoveCompiledFiles(type.Value);

              PackageManager.CopyDirectory(dir, srcPath);

              break;

            case "include":
              if (type.Value.Count != 0) {
                grouping.RemoveIncludeDirectory(type.Value[0]);
              }

              PackageManager.CopyDirectory(dir, srcPath);
              grouping.AddIncludeDirectory(srcPath, "");
              break;

            case "library":

              PackageManager.CopyDirectory(dir, libraryPath);
              grouping.RemoveLibraryDirectory(dir);
              grouping.AddLibraryDirectory(libraryPath, "");

              break;
          }

        }

        grouping.AddCompiledFiles(srcFiles);
      }
    }











    //----------------------------------------------------------------------------------------------------
    public static bool Handle() {
      ProjectGrouping? grouping = ProjectGrouping.Parse(File.ReadAllText(PackageManager.VCXPROJ));
      if (grouping is null) {
        Console.WriteLine("Invalid VCXPROJ file");
        return true;
      }

      Dictionary<string, PackageEntry>? packages = null;
      if (File.Exists(PackageManager.SaveFilePath))
        packages = JsonSerializer.Deserialize<Dictionary<string, PackageEntry>>(File.ReadAllText(PackageManager.SaveFilePath));

      if (packages is null)
        packages = new Dictionary<string, PackageEntry>();

      bool running = true;

      bool changed = false;


      while (running) {
        Console.Clear();
        Console.WriteLine("Personal Package Manager Interface\n");

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
            Console.WriteLine("  Lists all installed packages\n");
            Console.WriteLine("-a");
            Console.WriteLine("add { package [ version ] }");
            Console.WriteLine("  Adds a package");
            Console.WriteLine("     package   Package to add");
            Console.WriteLine("     version   Specific version to add\n");
            Console.WriteLine("-r");
            Console.WriteLine("remove { package }");
            Console.WriteLine("  Removes a package");
            Console.WriteLine("     package   Package to add");
            Console.WriteLine("-u");
            Console.WriteLine("update [ package [ version ] ]");
            Console.WriteLine("  Updates all (or one) package to live (or set) version");
            Console.WriteLine("     package   Package to update");
            Console.WriteLine("     version   Version to set to\n");
            Console.WriteLine("-s");
            Console.WriteLine("seperate");
            Console.WriteLine("  Copies all live package files to directory and removes PPM files\n");
            Console.WriteLine("-q");
            Console.WriteLine("quit");
            Console.WriteLine("  Closes the interface");
            PackageManager.EnterToContinue();
            break;


          case "-l":
          case "list":
            foreach (PackageEntry package in packages.Values) {
              Console.WriteLine($"{package.Name} --- {package.Version}");
            }
            PackageManager.EnterToContinue();
            break;


          case "-a":
          case "add":
            if (args.Count < 2) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }

            Add(args, packages, grouping);
            changed = true;

            PackageManager.EnterToContinue();
            break;


          case "-r":
          case "remove":
            if (args.Count < 2) {
              Console.WriteLine("Invalid arguements");
              PackageManager.EnterToContinue();
              continue;
            }

            Remove(args, packages, grouping);

            changed = true;
            PackageManager.EnterToContinue();
            break;


          case "-u":
          case "update":
            if (args.Count < 2) {
              args.Add(string.Empty);
              List<string> tempPackages = packages.Keys.ToList();
              foreach (string package in tempPackages) {
                args[1] = package;
                Update(args, packages, grouping);
              }
              changed = true;
              PackageManager.EnterToContinue();
              continue;
            }

            Update(args, packages, grouping);

            changed = true;
            PackageManager.EnterToContinue();
            break;


          case "-s":
          case "seperate":
            Seperate(args, packages, grouping);

            if (File.Exists(PackageManager.SaveFilePath))
              File.Delete(PackageManager.SaveFilePath);

            File.WriteAllText(PackageManager.VCXPROJ, grouping.ToString());
            return false;

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
        File.WriteAllText(PackageManager.SaveFilePath, JsonSerializer.Serialize(packages,
          new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
          }));

        File.WriteAllText(PackageManager.VCXPROJ, grouping.ToString());
      }

      return true;
    }

  }
}
