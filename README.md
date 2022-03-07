# PA4_PMI

Below is the text of the original assignment description from CS 4242 Natural Language Processing at the Universeity of Minnesota - Duluth. The source code includes further commentary on the implementation, and the textbook referenced can be found here: https://web.stanford.edu/~jurafsky/slp3/

----

Write a program in the language of your choice that creates a word by word co-occurrence matrix for a corpus. The cell values in this matrix should be the Pointwise Mutual Information (PMI) between 2 words. We'll refer to this program as similar-pmi in this writeup, although you can name it as you wish. 

Do not use PPMI, just use standard PMI since we will want to keep track  of negative values. Please see Chapter 6 of our Jurafsky and Martin text for more information about co-occurrence matrices and PMI. The particular kind of matrix you should use for this assignment is described in Section 6.3.3 (a word by word matrix).

The input for your program are sentences randomly sampled from news articles from 2011. Each line in the data is a single sentence, and there is no connection between consecutive sentences. This means that co-occurrences are only found within a single sentence and do not cross line boundaries. 

The input data is available in our Google Drive in a file PA4-News-2011.zip. This will unpack to a directory called PA4-News-2011 that contains 24 files each with 100,000 lines of news text. There are 2,400,000 total lines / sentences in these files. You program must process all of these files for full credit, although you may want to use subsets of these files for development.

During pre-processing you should remove all non-alphanumeric characters and convert all text to lower case. 

Your program will compute cosine between two words (ie cosine(word1,word2)) based on your PMI co-occurrence matrix. The word pairs will be given in a plain text input file. The word-pair file should be formatted such that each line has a pair of words that are separated by a space.

Your program should build the word by word PMI co-occurrence matrix before measuring any word pairs. Think of the process of building the matrix as a training phase, and measuring the word pairs as the testing phase. As such you should not use your knowledge of the word pairs you will process to inform how you build the co-occurrence matrix.  

Your program should take any number of word pairs as input. You may assume that the input file is formatted correctly.  However,  please handle the case where one or both of the words in a pair are not in the corpus. In this case assign a cosine  value of -9999.

Your program should support a flexible notion of co-occurrence where two words co-occur if they occur together within a window of N words. A window of this kind is assumed to include the words in your pair, so a 2 word window means that the words are adjacent with no words in between them (ie a bigram). The order of the words matters -so "door lock" is different than "lock door". 

For example, assume this is a 1 line corpus.

the kitty cat meows really loud

The co-occurring words in this corpus (with a window size of 2) are :

the kitty

kitty cat

cat meows

meows really

really loud

Suppose we set the window size to 6. That means that words can be considered co-occurrences when they occur with up to and including 4 intervening words between them. 

In our example corpus above, the co-occuring words (with a window size of 6) are :

the kitty

the cat

the meows

the really

the loud

kitty cat

kitty meows

kitty really

kitty loud

cat meows

cat really

cat loud

meows really

meows loud

really loud

Your program should take the following command line arguments in the following order:

 an integer indicating the window size,

the directory where the corpus is found, and 

the name of the word-pair file, which should be a plain text file. 

For example: 

similiar-pmi  6  ./PA4-News-2011  ./word-pairs.txt

Your program should output a short message describing what it does and who is the author, followed by the count of the types and tokens in the corpus and the specified window size. Thereafter it should display the cosine, counts, and PMI for each word pair as shown below. Please display your word pairs in descending order of cosine value (highest score first). Make sure to display the requested information in the order shown below where tabs are used to seperate columns. Display cosine and PMI to 5 digits of precision. 

For example :

PA 4 computing similarity from a word by word PMI co-occurrence matrix, programmed by Ted Pedersen.

tokens = integer, types = integer,  window = window size setting

[then one line as follows for each word pair in the input file, separate columns with a tab, display by descending cosine value (highest first)]

cosine(word1,word2)	word1		word2		count(word1)		count(word2) 	count(word1,word2) PMI(word1,word2)

You may not use NLP specific libraries for this assignment. You should do your  preprocessing and tokenization from "scratch",  using your own regular expressions. You may use external libraries that are not NLP specific that provide data structures that suit the problem (matrics, etc.) and to compute the cosine. If you do, please make sure to document in your source code where you obtained these libraries and document the functionality of any routines, etc. that you use.  

The 30 word pairs you must process with your program appear below. To this add your own 10 pairs. Please choose pairs which seem interesting to you in some way, particularly with respect to the ideas of co-occurrence and similarity. The output you submit must be based on this 40 word pair file (where 30 are given and 10 are yours).

You will run your program with 2 different window sizes, 2 and 6 and provide the results of both of these runs with your submission. This is how I'll be grading your functionality: 

correct results for 40 word pairs from full corpus with window size 2 (1 point)

correct results for 40 word pairs from full corpus with window size 6 (1 point)

correct results for 40 word pairs with corpus > 1,000,000 tokens with window sizes 2 and 6 (1 point)

correct results for 40 word pairs with corpus > 100,000 tokens with window size 2 (1 point)

correct results for 40 word pairs with corpus > 100,000 tokens with windows size 6 (1 point)

Please submit your source code and output as a single pdf file to Canvas. Make sure to create your pdf file using a source code to pdf converter that puts line numbers on your source code (this turns out to be useful when commenting on your programs).  

Finally, please make sure to carefully follow the programming languge assignment grading rubric, particularly in regards to documentation. Programs that do not have a complete overall comment will receive a deduction of 2 points, and programs with limited or not detailed comments will receive a deduction of 3 points. You can find the rubric and an associated video explaining it in our Google Drive or on Media Space. 

30 word pairs for processing - save as plain text file and add your own 10 :

sports training

train engines

train travel

car automobile

drive car

miran shah

shah miran

baseball soccer

president clinton 

bill clinton

clinton clinton

russia germany

mexican food

russian circus

duluth minnesota

minnesota duluth

the wife

the husband

of the

coalition government

government coalition

dog cat

king man

king woman

walmart arkansas

target minneapolis

microsoft ibm

ibm walmart

walmart target

target practice 

[add your 10 word pairs starting here]
