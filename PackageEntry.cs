using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PersonalPackageManager {
  public class PackageEntry {
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Files { get; set; } = new();
  }
}
