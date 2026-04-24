using System;
using System.Text;
using System.Text.RegularExpressions;
using static PackageManager;

public class ProjectGrouping {
  public class ProjectItem {
    public string Type = string.Empty;
    public Dictionary<string, string> Settings = new();
    public string Data = string.Empty;
    public ProjectItem? Parent = null;

    public List<ProjectItem> Children = new();


    public List<ProjectItem> GetAllChildrenOfType(string type) {
      return Children.Where(T => T.Type == type).ToList();
    }

    public ProjectItem? GetFirstTypeOf(string type) {
      foreach (ProjectItem item in Children) {
        if (item.Type == type)
          return item;
      }

      return null;
    }



    public ProjectItem AddChild(ProjectItem item) {
      item.Parent = this;
      Children.Add(item);

      return item;
    }

    public string ToString(int i, string indent) {
      StringBuilder builder = new(5000);
      
      builder.Append(indent.Repeat(i));

      if (Type == "XMLCommentBlock") {
        builder.Append($"<!--{Data}-->");
        return builder.ToString();
      }


      builder.Append($"<{Type}");

      foreach (KeyValuePair<string, string> setting in Settings)
        if (setting.Value != string.Empty)
          builder.Append($" {setting.Key}=\"{setting.Value}\"");


      if (Children.Count > 0) {
        builder.Append(">\n");
        foreach (ProjectItem item in Children) {
          builder.Append($"{item.ToString(i + 1, indent)}\n");
        }

        builder.Append(indent.Repeat(i));
        builder.Append($"</{Type}>");
      } else if (Data != string.Empty) {
        builder.Append(">");
        builder.Append(Data);
        builder.Append($"</{Type}>");
      } else
        builder.Append(" />");


      return builder.ToString();
    }


    public ProjectItem() { }
    public ProjectItem(string type, string data) {
      Type = type;
      Data = data;
    }
    public ProjectItem(string type, string data, ProjectItem parent) {
      Type = type;
      Data = data;
      Parent = parent;
      parent.Children.Add(this);
    }
    public ProjectItem(string type, string data, ProjectItem parent, Dictionary<string, string> settings) {
      Type = type;
      Data = data;
      Parent = parent;
      Settings = settings;
      parent.Children.Add(this);
    }
  };



  public static List<string> CompileTypes = [".*\\.c$", ".*\\.cpp$", ".*\\.cc$", ".*\\.c\\+\\+$", ".*\\.cppm$"];
  public static List<string> IncludeTypes = [".*\\.h$", ".*\\.hpp$", ".*\\.hh$", ".*\\.h\\+\\+$", ".*\\.hm$", ".*\\.inc$"];

  public static bool MatchesAny(string str, List<string> list) {
    foreach (string s in list)
      if (Regex.IsMatch(str, s))
        return true;


    return false;
  }

  public static HashSet<string> ExtractListFrom(string strList) {
    return new HashSet<string>(strList.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
  }

  public static string ConvertListFrom(HashSet<string> list) {
    StringBuilder builder = new StringBuilder(150);
    foreach (string str in list)
      builder.Append(str + ';');
    return builder.ToString();
  }



  public string Type = string.Empty;
  public Dictionary<string, string> Settings = new();

  public List<ProjectItem> Children = new();
  public ProjectItem Project = null!;





  //----------------------------------------------------------------------------------------------------
  public void AddUniqueCompiledFiles(List<string> files) {
    if (files.Count <= 0)
      return;

    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children.Count == 0 || (item.Children[0].Type == "ClCompile" && MatchesAny(item.Children[0].Settings["Include"], CompileTypes))) {
        ig = item;
        break;
      }
    }

    if (ig is null) {
      ig = new ProjectItem("ItemGroup", string.Empty);
      Project.AddChild(ig);
    }

    files.ForEach(T => {
      if (ig.Children.Count(J => J.Settings.ContainsKey("Include") && J.Settings["Include"] == T) > 0)
        return;

      new ProjectItem("ClCompile", string.Empty, ig, new([new KeyValuePair<string, string>("Include", T)]));
    });
  }



  //----------------------------------------------------------------------------------------------------
  public void AddUniqueShaderFiles(List<string> files) {
    if (files.Count <= 0)
      return;


    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children.Count == 0 || item.Children[0].Type == "None") {
        ig = item;
        break;
      }
    }

    if (ig is null) {
      ig = new ProjectItem("ItemGroup", string.Empty);
      Project.AddChild(ig);
    }

    files.ForEach(T => {
      if (ig.Children.Count(J => J.Settings.ContainsKey("Include") && J.Settings["Include"] == T) > 0)
        return;

      new ProjectItem("None", string.Empty, ig, new([new KeyValuePair<string, string>("Include", T)]));
    });
  }















  //----------------------------------------------------------------------------------------------------
  public void RemoveCompiledFiles(List<string> files) {
    if (files.Count <= 0)
      return;

    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children[0].Type == "ClCompile" && MatchesAny(item.Children[0].Settings["Include"], CompileTypes)) {
        ig = item;
        break;
      }
    }

    if (ig is null)
      return;

    files.ForEach(T => ig.Children.RemoveAll(J => J.Settings.ContainsKey("Include") && J.Settings["Include"] == T) );
  }



  //----------------------------------------------------------------------------------------------------
  public void RemoveShaderFiles(List<string> files) {
    if (files.Count <= 0)
      return;

    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children[0].Type == "None") {
        ig = item;
        break;
      }
    }

    if (ig is null)
      return;

    files.ForEach(T =>  ig.Children.RemoveAll(J => J.Settings.ContainsKey("Include") && J.Settings["Include"] == T) );
  }



  //----------------------------------------------------------------------------------------------------
  public void RemoveLibraryFiles(List<string> files) {
    if (files.Count <= 0)
      return;

    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      ProjectItem? link = item.GetFirstTypeOf("Link");
      if (link is null)
        continue;
      ProjectItem? deps = link.GetFirstTypeOf("AdditionalDependencies");
      if (deps is null)
        continue;

      HashSet<string> data = ExtractListFrom(deps.Data);
      data.ExceptWith(files);

      deps.Data = ConvertListFrom(data);
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void RemoveLibraryDirectory(string libraryDir) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      ProjectItem? link = item.GetFirstTypeOf("Link");
      if (link is null)
        continue;
      ProjectItem? dirs = link.GetFirstTypeOf("AdditionalLibraryDirectories");
      if (dirs is null)
        continue;

      HashSet<string> data = ExtractListFrom(dirs.Data);
      data.Remove(libraryDir);

      dirs.Data = ConvertListFrom(data);
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void RemoveIncludeDirectory(string includeDir) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      ProjectItem? cl = item.GetFirstTypeOf("ClCompile");
      if (cl is null)
        continue;
      ProjectItem? dirs = cl.GetFirstTypeOf("AdditionalIncludeDirectories");
      if (dirs is null)
        continue;

      HashSet<string> data = ExtractListFrom(dirs.Data);
      data.Remove(includeDir);

      dirs.Data = ConvertListFrom(data);
    }
  }




  //----------------------------------------------------------------------------------------------------
  public void RemovePreprocessorDefinitions(List<string> definitions) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      ProjectItem? cl = item.GetFirstTypeOf("ClCompile");
      if (cl is null)
        cl = new ProjectItem("ClCompile", string.Empty, item);
      ProjectItem? def = cl.GetFirstTypeOf("PreprocessorDefinitions");
      if (def is null)
        def = new ProjectItem("PreprocessorDefinitons", "%(PreprocessorDefinitions)", cl);

      HashSet<string> data = ExtractListFrom(def.Data);
      data.ExceptWith(definitions);

      def.Data = ConvertListFrom(data);
    }
  }







  //----------------------------------------------------------------------------------------------------
  public void AddCompiledFiles(List<string> files) {
    if (files.Count <= 0)
      return;

    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children.Count == 0 || (item.Children[0].Type == "ClCompile" && MatchesAny(item.Children[0].Settings["Include"], CompileTypes))) {
        ig = item;
        break;
      }
    }

    if (ig is null) {
      ig = new ProjectItem("ItemGroup", string.Empty);
      Project.AddChild(ig);
    }

    files.ForEach(T => {
      /*      string fileName = Path.GetRelativePath(folder, T);
            string srcName = Path.Combine("src", fileName);
            string newFile = Path.Combine(srcFolder, fileName);
            File.Copy(T, newFile, true);*/

      new ProjectItem("ClCompile", string.Empty, ig, new([new KeyValuePair<string, string>("Include", T)]));
    });
  }



  //----------------------------------------------------------------------------------------------------
  public void AddShaderFiles(List<string> files) {
    if (files.Count <= 0)
      return;


    ProjectItem? ig = null;

    List<ProjectItem> itemGroups = Project.GetAllChildrenOfType("ItemGroup");
    foreach (ProjectItem item in itemGroups) {
      if (item.Children.Count == 0 || item.Children[0].Type == "None") {
        ig = item;
        break;
      }
    }

    if (ig is null) {
      ig = new ProjectItem("ItemGroup", string.Empty);
      Project.AddChild(ig);
    }

    files.ForEach(T => {
      new ProjectItem("None", string.Empty, ig, new([new KeyValuePair<string, string>("Include", T)]));
    });
  }



  //----------------------------------------------------------------------------------------------------
  public void AddLibraryFiles(List<(string , string)> files) {
    if (files.Count <= 0)
      return;

    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      ProjectItem? link = item.GetFirstTypeOf("Link");
      if (link is null)
        link = new ProjectItem("Link", string.Empty, item);
      ProjectItem? deps = link.GetFirstTypeOf("AdditionalDependencies");
      if (deps is null)
        deps = new ProjectItem("AdditionalDependencies", "%(AdditionalDependencies)", link);

      HashSet<string> data = ExtractListFrom(deps.Data);
      files.ForEach(T => {
        if (item.Settings["Condition"].ToLower().Contains(T.Item2)) {
          data.Add(T.Item1);
        }
      });

      deps.Data = ConvertListFrom(data);
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void SetMinVersion(string version, string mod) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");
    
    string versionVal = version.Substring(6);


    foreach (ProjectItem item in itemDefine) {
      if (!item.Settings["Condition"].ToLower().Contains(mod)) {
        continue;
      }


      ProjectItem? cl = item.GetFirstTypeOf("ClCompile");
      if (cl is null)
        cl = new ProjectItem("ClCompile", string.Empty, item);
      ProjectItem? lang = cl.GetFirstTypeOf("LanguageStandard");
      if (lang is null) {
        lang = new ProjectItem("LanguageStandard", version, cl);
        continue;
      }

      if (versionVal == "latest") {
        lang.Data = version;
        continue;
      }

      string langVersionVal = lang.Data.Substring(6);
      if (langVersionVal == "latest")
        continue;

      int versionNum = int.Parse(versionVal);
      int langNum = int.Parse(langVersionVal);

      if (langNum > 70 && versionNum < 40) {
        lang.Data = version;
        continue;
      }
      if (versionNum > 70 && langNum < 40) {
        continue;
      }

      if (versionNum > langNum) {
        lang.Data = version;
        continue;
      }
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void AddPreprocessorDefinitions(List<string> definitions, string mod) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");


    foreach (ProjectItem item in itemDefine) {
      if (!item.Settings["Condition"].ToLower().Contains(mod)) {
        continue;
      }

      ProjectItem? cl = item.GetFirstTypeOf("ClCompile");
      if (cl is null)
        cl = new ProjectItem("ClCompile", string.Empty, item);
      ProjectItem? def = cl.GetFirstTypeOf("PreprocessorDefinitions");
      if (def is null)
        def = new ProjectItem("PreprocessorDefinitons", "%(PreprocessorDefinitions)", cl);

      HashSet<string> data = ExtractListFrom(def.Data);
      data.UnionWith(definitions);

      def.Data = ConvertListFrom(data);
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void AddLibraryDirectory(string libraryDir, string mod) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      if (!item.Settings["Condition"].ToLower().Contains(mod)) {
        continue;
      }

      ProjectItem? link = item.GetFirstTypeOf("Link");
      if (link is null)
        link = new ProjectItem("Link", string.Empty, item);
      ProjectItem? dirs = link.GetFirstTypeOf("AdditionalLibraryDirectories");
      if (dirs is null)
        dirs = new ProjectItem("AdditionalLibraryDirectories", "%(AdditionalLibraryDirectories)", link);

      HashSet<string> data = ExtractListFrom(dirs.Data);
      data.Add(libraryDir);

      dirs.Data = ConvertListFrom(data);
    }
  }



  //----------------------------------------------------------------------------------------------------
  public void AddIncludeDirectory(string includeDir, string mod) {
    List<ProjectItem> itemDefine = Project.GetAllChildrenOfType("ItemDefinitionGroup");

    foreach (ProjectItem item in itemDefine) {
      if (!item.Settings["Condition"].ToLower().Contains(mod)) {
        continue;
      }

      ProjectItem? cl = item.GetFirstTypeOf("ClCompile");
      if (cl is null)
        cl = new ProjectItem("ClCompile", string.Empty, item);
      ProjectItem? dirs = cl.GetFirstTypeOf("AdditionalIncludeDirectories");
      if (dirs is null)
        dirs = new ProjectItem("AdditionalIncludeDirectories", "%(AdditionalIncludeDirectories)", cl);

      HashSet<string> data = ExtractListFrom(dirs.Data);
      data.Add(includeDir);

      dirs.Data = ConvertListFrom(data);
    }
  }















  //----------------------------------------------------------------------------------------------------
  public ProjectItem AddChild(ProjectItem item) {
    item.Parent = null;
    Children.Add(item);

    return item;
  }



  public string ToString(string indent = "  ") {
    StringBuilder builder = new(10000);

    builder.Append($"<?{Type}");
    foreach (KeyValuePair<string, string> setting in Settings)
      builder.Append($" {setting.Key}=\"{setting.Value}\"");
    builder.Append("?>\r\n");

    foreach (ProjectItem item in Children) {
      builder.Append($"{item.ToString(0, indent)}\n");
    }

    return builder.ToString();
  }



  //----------------------------------------------------------------------------------------------------
  public static ProjectGrouping? Parse(string str) {
    ProjectGrouping group = new();

    string info = "";
    int startPos = 0;
    int tempPos = 0;
    int endPos = 0;

    endPos = str.IndexOfAny(['\r', '\n']);

    string header = str[..endPos];
    string data = str[(endPos + 1)..];



    if (header.Contains("<?") && header.Contains("?>"))
      info = header[(header.IndexOf("<?") + 2)..header.LastIndexOf("?>")];
    else
      return null;

    endPos = info.IndexOf(' ');
    group.Type = info[startPos..endPos].Trim();
    info = info[(endPos + 1)..];

    group.Settings = new Dictionary<string, string> (info.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(T => new KeyValuePair<string, string>(T[..T.IndexOf('=')], T[(T.IndexOf('=') + 2)..^1])));



    startPos = 0;
    endPos = 0;


    ProjectItem? curItem = null;

    while (true) {
      if (startPos >= data.Length)
        break;
      startPos = data.IndexOf('<', startPos) + 1;
      if (startPos == 0)
        break;

      if (data.Substring(startPos, 3) == "!--") {

        endPos = data.IndexOf("-->", startPos + 3);
        if (endPos == -1)
          endPos = data.Length;

        if (curItem is not null)
          curItem.AddChild(new ProjectItem("XMLCommentBlock", data[(startPos + 3)..endPos]));
        else
          group.AddChild(new ProjectItem("XMLCommentBlock", data[(startPos + 3)..endPos]));


        startPos = endPos;
        continue;
      }

      if (data[startPos] == '/') {
        info = data[(endPos + 1)..(startPos - 1)].Trim();
        if (curItem is not null) {
          curItem.Data = info;
          curItem = curItem.Parent;
        }

        if (curItem is null)
          break;

        endPos = data.IndexOf('>', startPos);

        continue;
      }


      endPos = data.IndexOf('>', startPos);
      if (endPos == -1)
        break;
      if (data[endPos - 1] == '/')
        endPos--;

      

      if (curItem is not null)
        curItem = curItem.AddChild(new ProjectItem());
      else
        curItem = group.AddChild(new ProjectItem());


      tempPos = data.IndexOf(' ', startPos);
      if (tempPos > 0 && tempPos + 1 < endPos) {
        curItem.Type = data[startPos..tempPos].Trim();
        startPos = tempPos + 1;
        curItem.Settings = new Dictionary<string, string> (data[startPos..endPos].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(T => new KeyValuePair<string, string>(T[..T.IndexOf('=')], T[(T.IndexOf('=') + 2)..^1])));
      } else
        curItem.Type = data[startPos..endPos].Trim();

      if (data[endPos] == '/') {
        curItem = curItem.Parent;
        if (curItem is null)
          break;

      }

      startPos = endPos;
    }

    var pSearch = group.Children.Where(T => T.Type == "Project").ToList();
    if (pSearch.Count == 0 || pSearch.Count > 1)
      return null;

    group.Project = pSearch[0];

    return group;
  }
};
