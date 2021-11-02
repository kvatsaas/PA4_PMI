using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PMI
{
    class PMI
    {
        static Dictionary<string, Dictionary<string, int>> matrixCC;
        static Dictionary<string, Dictionary<string, int>> matrixCC_invert;
        static Dictionary<string, Dictionary<string, double>> matrixPMI;
        static int windowSize;
        static int tokenCount;

        struct WordPair
        {

            public WordPair(string word, string context, int wordCount, int contextCount, int coCount, double pmi, double cosine)
            {
                Word = word;
                Context = context;
                WordCount = wordCount;
                ContextCount = contextCount;
                CoCount = coCount;
                PMI = pmi;
                Cosine = cosine;
            }

            public string Word { get; }
            public string Context { get; }
            public int WordCount { get; }
            public int ContextCount { get; }
            public int CoCount { get; }
            public double PMI { get; }
            public double Cosine { get; }

            public static int CompareByCosine(WordPair x, WordPair y)
            {
                return y.Cosine.CompareTo(x.Cosine);
            }
        }

        static void ParseDataFile(string filepath)
        {
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine().ToLower();  // get the next line from the data file
                    input = Regex.Replace(input, @"[^a-z\s]", "");

                    var matches = Regex.Matches(input, @"\b[a-z]+\b", RegexOptions.Compiled);     // get each word
                    if (matches.Count == 0)
                        continue;
                    var words = new List<string>();
                    foreach (Match m in matches)
                    {
                        words.Add(m.Value);
                        if (!matrixCC.ContainsKey(m.Value))
                            matrixCC.Add(m.Value, new Dictionary<string, int>());
                        if (!matrixCC_invert.ContainsKey(m.Value))
                            matrixCC_invert.Add(m.Value, new Dictionary<string, int>());
                    }

                    tokenCount += words.Count;
                    CountCoOccurrences(words);
                    if (!matrixCC.ContainsKey(words[words.Count - 1]))
                    {
                        matrixCC.Add(words[words.Count - 1], new Dictionary<string, int>());
                        matrixCC_invert.Add(words[words.Count - 1], new Dictionary<string, int>());
                    }
                        
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        static List<WordPair> ParseWordPairFile(string filepath)
        {
            try
            {
                using var sr = new StreamReader(filepath);
                var wordPairs = new List<WordPair>();

                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next line from the wordpair file

                    var matches = Regex.Matches(input, @"\b[a-z]+\b", RegexOptions.Compiled);
                    wordPairs.Add(BuildWordPair(matches[0].Value, matches[1].Value));
                }
                return wordPairs;
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
                if (!matrixCC.ContainsKey(word))
                    matrixCC.Add(word, new Dictionary<string, int>());
                    
                // first index after the window is current index + windowSize, or end of the list if that is smaller
                var endWindow = Math.Min(i + windowSize, words.Count);
                for (int j = i + 1; j < endWindow; j++)
                {
                    var context = words[j];     // rename for readability

                    if (!matrixCC[word].ContainsKey(context))
                        matrixCC[word].Add(context, 1);
                    else
                        matrixCC[word][context]++;

                    if (!matrixCC_invert[context].ContainsKey(word))
                        matrixCC_invert[context].Add(word, 1);
                    else
                        matrixCC_invert[context][word]++;
                }
            }
        }

        static WordPair BuildWordPair(string word, string context)
        {
            var wordCount = VectorSum(ExtractMatrixRow(word));
            var contextCount = VectorSum(ExtractMatrixColumn(context));
            int coCount;
            double PMI;
            if (matrixCC.ContainsKey(word) && matrixCC[word].ContainsKey(context))
            {
                coCount = matrixCC[word][context];
                PMI = matrixPMI[word][context];
            }
            else
            {
                coCount = 0;
                PMI = -9999;
            }
            WordPair wp = new WordPair(
                word,
                context,
                wordCount,
                contextCount,
                coCount,
                PMI,
                Cosine(word, context));
            return wp;
        }

        static double CalculatePMI(string word, string context)
        {
            var wordVector = ExtractMatrixRow(word);
            var contextVector = ExtractMatrixColumn(context);

            var wordProb = (double) VectorSum(wordVector) / tokenCount;
            var contextProb = (double) VectorSum(contextVector) / tokenCount;

            var observedProb = (double) matrixCC[word][context] / tokenCount;

            return Math.Log2(observedProb / (wordProb * contextProb));
        }

        static double Cosine(string word, string context)
        {
            if (!matrixPMI.ContainsKey(word) || !matrixPMI.ContainsKey(context))
                return -9999;

            double dotProduct = 0;

            foreach (string s in matrixPMI[word].Keys)
                if (matrixPMI[context].ContainsKey(s))
                    dotProduct += (double)matrixPMI[word][s] * matrixPMI[context][s];

            if (dotProduct == 0)
                return 0;

            return dotProduct / (VectorLength(matrixPMI[word]) * VectorLength(matrixPMI[context]));
        }

        /// <summary>
        /// Calculates the total sum of all entries of the vector represented by the dictionary
        /// </summary>
        /// <param name="vector">The dictionary representing a vector</param>
        /// <returns>The sum of all entries of the vector</returns>
        static int VectorSum(Dictionary<string, int> vector)
        {
            var count = 0;
            foreach (int value in vector.Values)
                count += value;
            return count;
        }

        /// <summary>
        /// Calculates the length of the vector represented by the dictionary, defined as the square root
        /// of the sum of all squared entries.
        /// </summary>
        /// <param name="vector">The dictionary representing a vector</param>
        /// <returns>The length of the vector</returns>
        static double VectorLength(Dictionary<string, double> vector)
        {
            var sum = 0;
            foreach (int i in vector.Values)
                sum += i * i;
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Returns a Dictionary<string, int> representing a single row (horizontal vector) of matrixCC.
        /// </summary>
        /// <param name="word">The word denoting the row to be returned.</param>
        /// <returns>The row for that word, or a null Dictionary<string, int> if there is none.</returns>
        static Dictionary<string, int> ExtractMatrixRow(string word)
        {
            if (!matrixCC.ContainsKey(word))
                return new Dictionary<string, int>();
            return matrixCC[word];
        }

        /// <summary>
        /// Returns a Dictionary<string, int> representing a single column (vertical vector) of matrixCC.
        /// </summary>
        /// <param name="word">The word denoting the column to be returned.</param>
        /// <returns>The column for that word, or a null Dictionary<string, int> if there is none.</returns>
        static Dictionary<string, int> ExtractMatrixColumn(string word)
        {
            /* Previously, there was a "smarter" version of this that built the column dynamically from matrixCC.
             * However, that was extremely slow, not only because of its inherent sluggishness but because the
             * way I had it set up meant redundant calls. Since space is less valuable in this case, my quick
             * fix to get this working for testing was to just create an inverted matrix and use that here.
             * I may improve this later, but it works well enough for now. */
            if (!matrixCC_invert.ContainsKey(word))
                return new Dictionary<string, int>();
            return matrixCC_invert[word];
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
            var wordPairFile = args[2];

            matrixCC = new Dictionary<string, Dictionary<string, int>>();
            matrixCC_invert = new Dictionary<string, Dictionary<string, int>>();
            matrixPMI = new Dictionary<string, Dictionary<string, double>>();
            tokenCount = 0;

            foreach (string filepath in infiles)
                ParseDataFile(filepath);

            Console.WriteLine("PA4: computing word similarity by building a co-occurrence matrix of PMI values and calculating cosine values. Created by Kristian Vatsaas.");
            Console.WriteLine("Window size: {0}", windowSize);
            Console.WriteLine("Tokens: {0}", tokenCount);
            Console.WriteLine("Types: {0}", matrixCC.Count);
            
            Console.WriteLine();

            foreach (string rowKey in matrixCC.Keys)
            {
                matrixPMI.Add(rowKey, new Dictionary<string, double>());
                foreach (string colKey in matrixCC[rowKey].Keys)
                {
                    matrixPMI[rowKey].Add(colKey, CalculatePMI(rowKey, colKey));
                }
            }

            var wordPairs = ParseWordPairFile(wordPairFile);
            wordPairs.Sort(WordPair.CompareByCosine);

            foreach (WordPair wp in wordPairs)
            {
                Console.WriteLine("{0,12:N5}\t{1,12}\t{2,12}\t{3,12}\t{4,12}\t{5,12}\t{6,12:N5}",
                    wp.Cosine,
                    wp.Word,
                    wp.Context,
                    wp.WordCount,
                    wp.ContextCount,
                    wp.CoCount,
                    wp.PMI);
            }

        }
    }
}
