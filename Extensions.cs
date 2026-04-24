using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Extensions {

  public static string Repeat(this string input, int count) {
    if (string.IsNullOrEmpty(input))
      return string.Empty;

    var builder = new StringBuilder(input.Length * count);

    for (var i = 0; i < count; i++)
      builder.Append(input);

    return builder.ToString();
  }
};