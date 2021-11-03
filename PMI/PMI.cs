/*
 * Word Similarity: Pointwise Mutual Information and Cosine of Vectors
 * Created by Kristian Vatsaas
 * November 2021
 * 
 * One application of language modeling is determining the similarity between two words. One linguistic theory
 * posits that the meaning of a word is not its dictionary definition but the way in which it is used. Thus,
 * two words are synonyms if they can be used relatively interchangeably, with minimal semantic consequences.
 * 
 * This idea of meaning as a function of usage introduces an interesting way to model a word - as a function
 * of the words around it. We count the occurrences of other distinct words around that word in a corpus,
 * and that data forms a multi-dimensional vector. To account for differences in word frequency, we normalize
 * these vectors with something called Pointwise Mutual Information. The fancy name is actually quite a simple
 * formula - it's the rate of actual co-occurrences divided by the rate of co-occurrences we would expect if
 * the words were not particularly related or unrelated. We then create a new matrix with the PMI values
 * instead of the counts.
 * 
 * The matrix can be broken apart into vectors for each word. These vectors can be thought of as pointers to
 * the location of the word in a multidimensional space. In theory, similar words should exist near one another
 * in this multidimensional space. Another way to think about this is that a small angle between two vectors
 * implies high similarity. So, if we calculate the cosine of that angle, a value of 1 means maximum similarity,
 * a value of -1 means maximum dissimilarity, and a value of 0 implies no particular relationship either way.
 * 
 * Co-occurrences can be counted within a certain distance of the base word. For this project, we consider the
 * window to include both the base word and the co-occurring - "context" - word. So, the minimum window size is 2,
 * and contains only the words next to the base word, while a window size of six would count co-occurrences for
 * all words with less than four words between it and the base word. Additionally, this project also tasks
 * us with considering the position of the words relative to each other. That is, we count co-occurrences
 * separately depending on whether they are behind or before the base word. The initial counting is simple;
 * we just make the window only face forward (usually it would be to either side). Then, when we count a
 * co-occurrence for the pair (word, context), we increment the cell at [word, context] in the matrix.
 * When we are done counting co-occurrences, rows represent co-occurrences after a word while columns
 * represent co-occurrences before a word.
 * 
 * Finally, for this project, we read in a predetermined list of wordpairs and output the counts, PMI,
 * and cosine for each wordpair, ordered by cosine value. The output looks like this (header not included):
 * 
 * cosine       word1           word2           word1 count     word2 count     co-occurences   PMI
 * 1.00000	     clinton	     clinton	       23847	       21536	          16	     0.48636
 * 0.66830	          of	         the	     4975757	    10945047	      434956	    -1.47741
 * 0.21280	    football	        game	       24405	      129678	         352	     2.32231
 * 0.17774	    princess	       diana	        1210	        1333	          46	    10.32465
 * -0.01505	         the	        wife	    12032668	       38638	        1403	    -2.88155
 * 
 * Note that cosine and PMI do not necessarily correlate. High PMI just means words often occur near each other,
 * which implies some sort of relationship between the two; high cosine means a similarity in the frequencies
 * with which the words occur near other words, which implies similarity in meaning.
 * 
 * The algorithms used in the implementation are fairly well described at this point, and further detail can
 * be found in the comments below. However, the data structures have not been discussed. This type of program
 * tends to create sparse matrices - that is, matrices with many uninformative cells (i.e. empty or zero) -
 * which takes up an unnecessarily large amount of space. To solve this, we need a way to represent only
 * the informative values, and just assume that if we didn't record a value for something, that it was
 * zero. C# doesn't seem to have a built-in representation for this or a trusted library for it, and my
 * brief research yielded the representation I was already thinking of: a Dictionary nested in another Dictionary,
 * where the keys are strings (the words). This is easy to use, performs well, and makes it easy to get a
 * row as a vector - just get the interior Dictionary associated with the word.
 * 
 * However, it creates an issue in getting the columns. (Side note: this wouldn't be an issue if we weren't
 * tracking position, because then rows and columns would be equivalent). To do so, we'd need to build a new
 * Dictionary by iterating through each row, checking each for an entry for the word we're trying to build a
 * column for, and adding it to the new Dictionary with the current row's key as the key. This needs to be
 * done for every single column - for this project's corpus, that's 177,114 - and if we don't want to store
 * extra data, the structure is such that we'd need to do each one multiple times. Clearly, we need to concede
 * some space efficiency, and so instead of doing this after the fact, we'll just create a second matrix
 * for both the co-occurrence matrix and the PMI matrix, which means a lot more space but palatable time
 * efficiency. On my machine, for the full corpus of about 45 million tokens, memory usage maxed out at around
 * 3 GB for a window size of 6. However, it took only 10.5 minutes, and just over 2 minutes for a window size
 * of 2. That's not amazing, but it certainly seems a reasonable endpoint for this project, as I'd need to
 * either do a lot more searching for a sparse matrix model that allows for column access or implement
 * multithreading in order to improve it. Both could be interesting, but I don't have time for them right now.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 11/2/21
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PMI
{
    class PMI
    {
        static Dictionary<string, Dictionary<string, uint>> matrixCO;
        static Dictionary<string, Dictionary<string, uint>> matrixCO_invert;
        static Dictionary<string, Dictionary<string, double>> matrixPMI;
        static Dictionary<string, Dictionary<string, double>> matrixPMI_invert;
        static short windowSize;
        static uint tokenCount;

        /// <summary>
        /// This structure represents a wordpair: both the "word" and "context" word (first and second word), followed by the counts
        /// for both word and context word, the co-occurrence count, the PMI value, and the cosine value. Its primary purpose is
        /// to provide a way to sort wordpairs by cosine value. The rest of the data is stored so that it can be easily accessed
        /// when printing results instead of re-calculating or using a messy mix of accessor methods. Since there are only 40 wordpairs
        /// for this assignment, this means significantly cleaner code at the cost of a relatively small increase in memory use.
        /// </summary>
        struct WordPair
        {

            /// <summary>
            /// Constructor for WordPair structures
            /// </summary>
            /// <param name="word">The first word in the pair</param>
            /// <param name="context">The second word in the pair</param>
            /// <param name="wordCount">The count for the first word - that is, the number of co-occurrences it is in</param>
            /// <param name="contextCount">The count for the second word - that is, the number of co-occurrences it is in</param>
            /// <param name="coCount">The number of co-occurrences between these two words</param>
            /// <param name="pmi">The PMI value of theis wordpair</param>
            /// <param name="cosine">The cosine of this wordpair</param>
            public WordPair(string word, string context, uint wordCount, uint contextCount, uint coCount, double pmi, double cosine)
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
            public uint WordCount { get; }
            public uint ContextCount { get; }
            public uint CoCount { get; }
            public double PMI { get; }
            public double Cosine { get; }

            /// <summary>
            /// A comparator that compares WordPairs by their cosine value
            /// </summary>
            /// <param name="x">The first WordPair</param>
            /// <param name="y">The second WordPair</param>
            /// <returns>A positive number if y's cosine is greater than x's, negative if it is less likely, or 0 if they are equally likely.</returns>
            public static int CompareByCosine(WordPair x, WordPair y)
            {
                return y.Cosine.CompareTo(x.Cosine);
            }
        }

        /// <summary>
        /// Iterates through the given file line-by-line, counting tokens and counting co-occurrences.
        /// </summary>
        /// <param name="filepath">The filepath of the data file</param>
        static void ParseDataFile(string filepath)
        {
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine().ToLower();  // get the next line from the data file
                    input = Regex.Replace(input, @"[^a-z\s]", "");  // remove any characters that are not alphas or whitespace

                    var matches = Regex.Matches(input, @"\b[a-z]+\b", RegexOptions.Compiled);   // match each word in the line
                    if (matches.Count == 0)     // skip this line if there are no words
                        continue;

                    // add each word matched to a list of words, and if the word does not already have a row representing it in each matrix, create one
                    var words = new List<string>();
                    foreach (Match m in matches)
                    {
                        words.Add(m.Value);
                        if (!matrixCO.ContainsKey(m.Value))
                            matrixCO.Add(m.Value, new Dictionary<string, uint>());
                        if (!matrixCO_invert.ContainsKey(m.Value))
                            matrixCO_invert.Add(m.Value, new Dictionary<string, uint>());
                    }

                    tokenCount += (uint) words.Count;   // increment token count
                    CountCoOccurrences(words);  // count co-occurrences for this line                        
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        /// <summary>
        /// Builds a list of WordPair structures based on the wordpairs in the given file. Assumes proper formatting.
        /// </summary>
        /// <param name="filepath">The filepath of the wordpair file</param>
        /// <returns></returns>
        static List<WordPair> ParseWordPairFile(string filepath)
        {
            try
            {
                using var sr = new StreamReader(filepath);
                var wordPairs = new List<WordPair>();

                // read and parse one review at a time - each is a single 'line' in the plauintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next line from the wordpair file

                    var matches = Regex.Matches(input, @"\b[a-z]+\b", RegexOptions.Compiled);   // match both words in the pair
                    wordPairs.Add(BuildWordPair(matches[0].Value, matches[1].Value));   // create a WordPair structure and add it to the list
                }

                return wordPairs;
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        /// <summary>
        /// Counts co-occurrences within the given list, using the predefined window size.
        /// </summary>
        /// <param name="words">The list of words in which to count co-occurrences</param>
        static void CountCoOccurrences(List<string> words)
        {
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];    // rename for readability

                // create a vector for the word if it does not already exist    
                if (!matrixCO.ContainsKey(word))
                    matrixCO.Add(word, new Dictionary<string, uint>());
                    
                // first index after the window is current index + windowSize, or end of the list if that is smaller
                var endWindow = Math.Min(i + windowSize, words.Count);
                for (int j = i + 1; j < endWindow; j++)
                {
                    var context = words[j];     // rename for readability

                    // increment co-occurrence matrices, creating entries if necessary
                    if (!matrixCO[word].ContainsKey(context))
                        matrixCO[word].Add(context, 1);
                    else
                        matrixCO[word][context]++;

                    if (!matrixCO_invert[context].ContainsKey(word))
                        matrixCO_invert[context].Add(word, 1);
                    else
                        matrixCO_invert[context][word]++;
                }
            }
        }

        /// <summary>
        /// Creates a WordPair structure for the given object.
        /// </summary>
        /// <param name="word">The first word in the wordpair</param>
        /// <param name="context">The second word in the wordpair</param>
        /// <returns></returns>
        static WordPair BuildWordPair(string word, string context)
        {
            var wordCount = VectorSum(matrixCO[word]);
            var contextCount = VectorSum(matrixCO_invert[context]);
            uint coCount;
            double PMI;
            if (matrixCO.ContainsKey(word) && matrixCO[word].ContainsKey(context))
            {
                coCount = matrixCO[word][context];
                PMI = matrixPMI[word][context];
            }
            else
            {
                coCount = 0;
                PMI = 0;
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

        /// <summary>
        /// Calculates PMI for each wordpair. PMI is defined as:
        /// log2( P(w,c)/( P(w) * P(c) ) )
        /// where P(w) is the likelihood of a word in the corpus being the first word,
        /// P(c) is the likelihood of a word in the corpus being the second word,
        /// and P(w,c) is the likelihood of the two words occurring in the same window in the corpus.
        /// In other words, it is the observed co-occurrence rate divided by the expected rate for
        /// two unrelated words.
        /// </summary>
        /// <param name="word">The first word in the wordpair</param>
        /// <param name="context">The second word in the wordpair</param>
        /// <returns>The PMI value for the wordpair</returns>
        static double CalculatePMI(string word, string context)
        {
            var wordVector = matrixCO[word];
            var contextVector = matrixCO_invert[context];

            var wordProb = (double) VectorSum(wordVector) / tokenCount;
            var contextProb = (double) VectorSum(contextVector) / tokenCount;

            var observedProb = (double) matrixCO[word][context] / tokenCount;

            return Math.Log2(observedProb / (wordProb * contextProb));
        }

        /// <summary>
        /// Calculates the cosine for each wordpair. Cosine is defined as the dot product
        /// of two vectors (rows in the matrix) divided by the product of the lengths of
        /// each vector.
        /// </summary>
        /// <param name="word">The first word in the wordpair</param>
        /// <param name="context">The second word in the wordpair</param>
        /// <returns>The cosine value for the wordpair</returns>
        static double Cosine(string word, string context)
        {
            // hardcoded value for when either word is not in the matrix
            if (!matrixPMI.ContainsKey(word) || !matrixPMI.ContainsKey(context))
                return -9999;

            double dotProduct = 0;

            /* I chose to compute the cosine with the dot product being calculated with both the row and column
             * for each word; in other words, the cosine is being computed using the PMI values for both
             * words occurring after the base word and words occurring before the base word. This can be thought
             * of as the sum of the dot product of both words' rows and both words' columns. I made this choice
             * because by only using one or the other, we only look at the similarity in how the words are used
             * relative to the words after or before, but not both. I do still have some minor concerns with
             * this method (for example, what happens if you have a word that always occurs after word1 but the
             * expected amount before, and vice versa for word2?) but solving them would be more complex than
             * it's worth for the scope of this project.
             * It should be noted that for this method, the ordering of the wordpair does not matter; however,
             * order overall does still matter as the PMI values are derived from order-respective co-occurences.
             */

            // compute the dot product for the rows
            if (matrixPMI.ContainsKey(context))
            {
                foreach (string s in matrixPMI[word].Keys)
                    if (matrixPMI[context].ContainsKey(s))
                        dotProduct += (double)matrixPMI[word][s] * matrixPMI[context][s];
            }

            // compute the dot product for the columns
            if (matrixPMI_invert.ContainsKey(word))
            {
                foreach (string s in matrixPMI_invert[context].Keys)
                    if (matrixPMI_invert[word].ContainsKey(s))
                        dotProduct += (double)matrixPMI_invert[context][s] * matrixPMI_invert[word][s];
            }

            // note that we need to use the length of the row and column together as if they were one vector, since we used all their values for the dot product
            return dotProduct /
                (VectorLength(SafeMatrixAccess(matrixPMI, word), SafeMatrixAccess(matrixPMI_invert, word))
                * VectorLength(SafeMatrixAccess(matrixPMI, context), SafeMatrixAccess(matrixPMI_invert, context)));
        }

        /// <summary>
        /// Safely accesses a given matrix with the given key. Returns an empty vector if there is none for the given key.
        /// Side note: I realized very late that this was an issue in very simple test cases, causing an error
        /// in in the Cosine method for extreme edge cases. Rather than restructure Cosine and make it uglier,
        /// I made this quick method, only to be used there where it comes up.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <param name="key">The key</param>
        /// <returns>The vector if it exists, otherwise an empty vector</returns>
        static Dictionary<string, double> SafeMatrixAccess(Dictionary<string, Dictionary<string, double>> matrix, string key)
        {
            if (!matrix.ContainsKey(key))
                return new Dictionary<string, double>();
            else
                return matrix[key];
        }

        /// <summary>
        /// Calculates the total sum of all entries of the vector represented by the dictionary.
        /// </summary>
        /// <param name="vector">The dictionary representing a vector</param>
        /// <returns>The sum of all entries of the vector</returns>
        static uint VectorSum(Dictionary<string, uint> vector)
        {
            uint count = 0;
            foreach (int value in vector.Values)
                count += (uint) value;
            return count;
        }

        /// <summary>
        /// Calculates the length of the vector represented by the dictionary, defined as the square root
        /// of the sum of all squared entries. If multiple vectors are given, the length is computed
        /// as if it were one continuous vector.
        /// </summary>
        /// <param name="vectors">One or more dictionaries, each representing a vector</param>
        /// <returns>The length of the vector</returns>
        static double VectorLength(params Dictionary<string, double>[] vectors)
        {
            double sum = 0;
            foreach (Dictionary<string, double> vector in vectors)
                foreach (double i in vector.Values)
                    sum += i * i;
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Runs the main loop for the program, calling each subfunction and printing out the results.
        /// </summary>
        /// <param name="args">The window size, the path to the directory of input files,
        /// and the path of the wordpair file</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign arguments to variables for readability
            windowSize = Int16.Parse(args[0]);
            var infiles = Directory.GetFiles(args[1]);  // string array of files in given directory
            var wordPairFile = args[2];

            // instantiate other variables
            matrixCO = new Dictionary<string, Dictionary<string, uint>>();
            matrixCO_invert = new Dictionary<string, Dictionary<string, uint>>();
            matrixPMI = new Dictionary<string, Dictionary<string, double>>();
            matrixPMI_invert = new Dictionary<string, Dictionary<string, double>>();
            tokenCount = 0;

            // parse each data file
            foreach (string filepath in infiles)
                ParseDataFile(filepath);

            // print info and basic file stats - done immediately after relevant computation is complete as a sort of marker
            Console.WriteLine("PA4: computing word similarity by building a co-occurrence matrix of PMI values and calculating cosine values. Created by Kristian Vatsaas.");
            Console.WriteLine("Window size: {0}", windowSize);
            Console.WriteLine("Tokens: {0}", tokenCount);
            Console.WriteLine("Types: {0}", matrixCO.Count);
            
            Console.WriteLine();

            // calculate PMI for all cells with values
            foreach (string rowKey in matrixCO.Keys)
            {
                matrixPMI.Add(rowKey, new Dictionary<string, double>());    // create row for current word in PMI matrix
                foreach (string colKey in matrixCO[rowKey].Keys)
                {
                    matrixPMI[rowKey].Add(colKey, CalculatePMI(rowKey, colKey));    // calculate PMI and add to "column" of current context word

                    if (!matrixPMI_invert.ContainsKey(colKey))
                        matrixPMI_invert.Add(colKey, new Dictionary<string, double>());     // create row for current context word in inverted PMI matrix if it does not already exit
                    matrixPMI_invert[colKey].Add(rowKey, matrixPMI[rowKey][colKey]);        // add PMI value to "column" of current word
                }
            }

            var wordPairs = ParseWordPairFile(wordPairFile);    // read wordpairs from file and do relevant calculations
            wordPairs.Sort(WordPair.CompareByCosine);           // sort wordpairs by cosine value

            foreach (WordPair wp in wordPairs)  // print required values for each wordpair
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
