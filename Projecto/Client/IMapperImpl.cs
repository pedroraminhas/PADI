﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PADIMapNoReduce
{
    class IMapperImpl : IMapper{
       public IList<KeyValuePair<string, string>> Map(string fileLine)
        {
            IList<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            
            //ir buscar conteudo da linha ao cliente
            IList<KeyValuePair<string, string>> resultAux = new List<KeyValuePair<string, string>>();
            String[] words = fileLine.Split(' ');
            string pattern = "[^0-9a-z]+";
            Regex rgx = new Regex(pattern);
            
        
            for (int i = 0; i < words.Length; i++ )
            {
                words[i] = words[i].ToLower();
                words[i] = rgx.Replace(words[i], "");
                
                result.Add(new KeyValuePair<string, string>(words[i], "1"));
            }

            int size = 0;
            for (int i = 0; i < words.Length; i++)
            {
                size = size+1;
            }

            while(result.Count !=0)
            {
                KeyValuePair<string, string> pair = result[0];
                String word = pair.Key;
                int count = 1;
                result.Remove(result[0]);
                while (result.Contains(pair)) {
                    int j = result.IndexOf(pair);
                    count++;
                    result.Remove(result[j]);
                }
                resultAux.Add(new KeyValuePair<string, string>(word, count.ToString()));
            }
           
            return resultAux;
        }

    }
}
