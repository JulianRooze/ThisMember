﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThisMember.Core
{
  public static class CamelCaseHelper
  {
    public static IList<string> SplitOnCamelCase(string word)
    {
      var words = new List<string>();

      int start = 0;

      for (var i = 0; i < word.Length; i++)
      {
        if (i == 0)
        {
          continue;
        }

        if (char.IsUpper(word[i]) && char.IsLower(word[i-1])) 
        {
          var subString = word.Substring(start, i - start);
          words.Add(subString);
          start = i;
        }
        else if (i == word.Length - 1)
        {
          words.Add(word.Substring(start));
        }

      }

      return words;
    }
  }
}
