using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PMI
{
    class PMI
    {
        static Dictionary<string, Dictionary<string, int>> matrix;
        static int windowSize;

        static void ParseDataFile(string filepath)
        {
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next line from the data file

                    var matches = Regex.Matches(input.ToLower(), @"\b[a-z]+\b", RegexOptions.Compiled);     // get each word
                    var words = new List<string>();
                    foreach (Match m in matches)
                        words.Add(m.Value);

                    CountCoOccurrences(words);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        static void CountCoOccurrences(List<string> words)
        {
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];    // rename for readability

                // create a vector for the word if it does not already exist    
                if (!matrix.ContainsKey(word))
                    matrix.Add(word, new Dictionary<string, int>());

                // first index in the window is i - windowSize + 1, or 0
                var startWindow = Math.Max(i - windowSize + 1, 0);
                // first index after the window is current index + windowSize, or end of the list if that is smaller
                var endWindow = Math.Min(i + windowSize, words.Count);
                for (int j = startWindow; j < endWindow; j++)
                {
                    if (i == j)
                        continue;

                    var context = words[j];     // rename for readability

                    if (!matrix[word].ContainsKey(context))
                        matrix[word].Add(context, 1);
                    else
                        matrix[word][context]++;
                }
            }
        }

        static double Cosine(string word, string context)
        {
            if (!matrix.ContainsKey(word) || !matrix.ContainsKey(context))
                return -9999;

            var dotProduct = 0;

            foreach (string s in matrix[word].Keys)
                if (matrix[context].ContainsKey(s))
                    dotProduct += matrix[word][s] * matrix[context][s];

            return dotProduct / (VectorLength(word) * VectorLength(context));
        }

        static double VectorLength(string word)
        {
            var sum = 0;
            foreach (int i in matrix[word].Values)
                sum += i * i;
            return Math.Sqrt(sum);
        }

        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign arguments to variables for readability
            windowSize = Int32.Parse(args[0]);
            var infiles = Directory.GetFiles(args[1]);  // string array of files in given directory
            var wordpairFile = args[3];

            matrix = new Dictionary<string, Dictionary<string, int>>();


        }
    }
}
