using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Runtime.InteropServices;

namespace TextAnalyser
{
    internal class TAProgram
    {
        private static readonly TAWord EndOfFile = new TAWord(-999, "end of file");
        private static readonly TAWord EndOfSentence = new TAWord(-998, "end of sentence");
        private static readonly TAWord NumberWord = new TAWord(-997, "###");
        private static readonly TAWord PunctWord = new TAWord(-996, "~");
        

        public static string[] StopWords { get; } = {
            "a", "about", "above", "after", "again", "against", "all", "am",
            "an", "and", "any", "are", "as", "at", "be", "because", "been", "before", "being", "below", "between",
            "both", "but", "by", "could", "did", "do", "does", "doing", "down", "during", "each", "few", "for", "from",
            "further", "had", "has", "have", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers",
            "herself", "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into",
            "is", "it", "it's", "its", "itself", "let's", "me", "more", "most", "my", "myself", "nor", "of", "on",
            "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out", "over", "own", "same", "she",
            "she'd", "she'll", "she's", "should", "so", "some", "such", "than", "that", "that's", "the", "their",
            "theirs", "them", "themselves", "then", "there", "there's", "these", "they", "they'd", "they'll", "they're",
            "they've", "this", "those", "through", "to", "too", "under", "until", "up", "very", "was", "we", "we'd",
            "we'll", "we're", "we've", "were", "what", "what's", "when", "when's", "where", "where's", "which", "while",
            "who", "who's", "whom", "why", "why's", "with", "would", "you", "you'd", "you'll", "you're", "you've",
            "your", "yours", "yourself", "yourselves"
        };

        //this is a list of words that will be stored as negatives, making them easy to ignore when analysing the file
        
        private static List<string> FindRelatedWords(Dictionary<string, TAGloveWord> gloveDictionary, string origWord,
            int numLayers, int numPerLayer)
        {
            var loadedWords = new List<string>(); //will hold all unique related words
            var foundWordStrings = new List<string>();

            var foundWords = GloveCompareAll(gloveDictionary, origWord, numPerLayer);
            if (foundWords == null)
            {
                //if the word given isn't valid
                return null;
            }
            //get the strings of the related words and get ready to search them
            var wordsToCheck = foundWords.Select(word => word.Text).ToList();
            //will hold the words waiting to be checked
            //add words checked to loadedWords and remove them from foundWords
            for (var x = 0; x < numLayers - 1; x++)
            {
                //-----
                var watch = System.Diagnostics.Stopwatch.StartNew();
                // the code that you want to measure comes here

                //check every foundWord
                foreach (string checkWord in wordsToCheck)
                {
                    //get the strings of related words
                    foundWords = GloveCompareAll(gloveDictionary, checkWord, numPerLayer);

                    if (foundWords == null) continue;
                    foreach (TAGloveWord word in foundWords)
                    {
                        if (!foundWordStrings.Contains(word.Text))
                        {
                            foundWordStrings.Add(word.Text);
                        }
                    }
                    //add the now checked word to loaded words
                    loadedWords.Add(checkWord);
                } //foundWordStrings holds all unique words in the layer
                wordsToCheck.Clear();
                //check foundWordStrings against loaded words and add any unique ones to wordsToCheck
                wordsToCheck.AddRange(foundWordStrings.Where(word => loadedWords.Contains(word) == false));
                /*foreach (string word in foundWordStrings)
                {
                    if (loadedWords.Contains(word) == false) wordsToCheck.Add(word);
                }*/
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Console.WriteLine(elapsedMs);
                //-----
                foundWordStrings.Clear();
            }

            return loadedWords;
        }

        private static float IsTextAbout(TAFile file, Dictionary<string, TAWord> wordsDictionary,
            Dictionary<string, TAGloveWord> gloveDictionary, string origWord)
        {
            const int numPerLayer = 15; //how many words for each similar word check
            const int numLayers = 3; //how many layers deep to search for relevant words
            if (origWord == null) return 0f;
            TAWord origTaWord;
            var relateWords = new List<TAWord>();
            float returnValue = 0f;
            int divideValue = 0;

            //get the user to give word/words to describe a topic
            //add a selection of closesly related words to the ones searched for
            //go several layers down the relation

            //if the origWord is in the dictionary
            if (wordsDictionary.TryGetValue(origWord, out origTaWord))
            {
                //the word given is a stopWord or punctuation
                if (origTaWord.Id < 0) return -1f;
            }
            //look for related words x layers deep, returns ids of words found
            var loadedWords = FindRelatedWords(gloveDictionary, origWord, numLayers, numPerLayer);
            if (loadedWords == null) return 0f;
            //check for words that are both related and in the file

            Console.WriteLine("Words related to " + origWord);
            Console.WriteLine("-----------------------------");
            //runs through every word loaded across so far
            foreach (var word in loadedWords)
            {
                //if the loaded word is not in the file then move on
                TAWord tempWord;
                if (!wordsDictionary.TryGetValue(word, out tempWord)) continue;
                //ensure the word is in the file
                if (!file.FileContent.Contains(tempWord.Id) || tempWord.Id < 0) continue;
                relateWords.Add(tempWord);
                Console.WriteLine(word + " is related and in the file");
            }
            Console.WriteLine("-----------------------------");
            if (relateWords.Count == 0) return 0f;
            foreach (var word in relateWords)
            {
                returnValue += file.WordCounts[word.Id];
            }
            for (var i = 0; i < file.WordCounts.Length; i++)
            {
                divideValue += file.WordCounts[i];
            }

            Console.WriteLine(returnValue);
            Console.WriteLine(divideValue);
            returnValue = (float) (returnValue/(Math.Sqrt(divideValue)*2));
            //a return value of >= 1 means that there is a strong relation
            //a return value of >= 0.5 means there is a relation 
            //a return of <0.5 means there is no strong link to the word given
            Console.WriteLine(returnValue);
            return returnValue;
        }

        private static double GloveCompareTwo(TAGloveWord word1, TAGloveWord word2)
        {
            double dotProduct = 0f;
            //pass in two words and compare how closely related they are
            for (var x = 0; x < word1.NormVecs.Length; x++)
            {
                dotProduct += word1.NormVecs[x]*word2.NormVecs[x];
                //multiply the values for each axis and add them together to get the dot product
            }
            //dotProduct now holds the dot product of the two vectors
            return dotProduct;
        }

        private static TAGloveWord[] GloveCompareAll(Dictionary<string, TAGloveWord> gloveDictionary, string word,
            int numResults)
        {
            //double timerTotal = 0f;
            //double msTotal = 0f;
            if (numResults <= 0) return null;
            numResults++; //add one to include the word itself
            var relatedWords = new TAGloveWord[numResults];
            var relatedValue = new float[numResults];
            //set a product of 0
            for (int x = 0; x < numResults; x++)
            {
                relatedValue[x] = 0;
            }
            //searches if the word given is in the dictinary
            TAGloveWord mainWord;
            gloveDictionary.TryGetValue(word, out mainWord);
            //return if the word is not found
            if (mainWord == null) return null;
            //-----
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here
            float[] mainVecs = mainWord.NormVecs;
            foreach (TAGloveWord gloveWord in gloveDictionary.Values)
            {
                float[] tempVecs = gloveWord.NormVecs;
                float relateCheck = 0;
                //var timer = System.Diagnostics.Stopwatch.StartNew();
                // the code that you want to measure comes here
                for (int x = 0; x < mainWord.NormVecs.Length; x += 10)
                {
                    //does 10 at once to speed up (reduces from 107ish seconds to 67ish over approx 154*30 calls)
                    relateCheck += mainVecs[x]*tempVecs[x];
                    relateCheck += mainVecs[x + 1]*tempVecs[x + 1];
                    relateCheck += mainVecs[x + 2]*tempVecs[x + 2];
                    relateCheck += mainVecs[x + 3]*tempVecs[x + 3];
                    relateCheck += mainVecs[x + 4]*tempVecs[x + 4];
                    relateCheck += mainVecs[x + 5]*tempVecs[x + 5];
                    relateCheck += mainVecs[x + 6]*tempVecs[x + 6];
                    relateCheck += mainVecs[x + 7]*tempVecs[x + 7];
                    relateCheck += mainVecs[x + 8]*tempVecs[x + 8];
                    relateCheck += mainVecs[x + 9]*tempVecs[x + 9];
                    //multiply the values for each axis and add them together to get the dot product
                }
                //timer.Stop();
                //var timerTicks = timer.ElapsedTicks;
                //timerTotal += timerTicks;
                //-------
                //if new product is less than the lowest value then ignore it
                if (!(relatedValue[numResults - 1] < relateCheck)) continue;
                //relatecheck is known to be more closely related than relatedvalue[max] so replace it
                relatedValue[numResults - 1] = relateCheck;
                //push the new value up until it is in the right space (-1 because there is no value above the last)
                for (int x = 0; x < numResults - 1; x++)
                {
                    //if the value above is smaller there is nothing more to do
                    if (!(relatedValue[numResults - x - 1] > relatedValue[numResults - x - 2])) continue;
                    //shuffle up (use relateCheck as a swapspace)
                    relateCheck = relatedValue[numResults - x - 2];
                    relatedValue[numResults - x - 2] = relatedValue[numResults - x - 1];
                    relatedValue[numResults - x - 1] = relateCheck;
                    //shuffle the word alongside
                    relatedWords[numResults - x - 1] = relatedWords[numResults - x - 2];
                    relatedWords[numResults - x - 2] = gloveWord;
                }
            }
            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //msTotal += elapsedMs;
            //-------
            //should come out with relatedWords being an array begginning with the main word
            //and then have the numResults most related words to it
            //Console.WriteLine(timerTotal/10000);
            //Console.WriteLine(msTotal);
            //Console.ReadLine();
            return relatedWords;
        }

        private static void LoadGloveData(ref Dictionary<string, TAGloveWord> gloveDictionary, string gloveLocation)
        {
            //-----
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            if (gloveDictionary != null) return;
            gloveDictionary = new Dictionary<string, TAGloveWord>(400000);
            var spaceSeparator = new[] {' '};

            using (
                var textFile = new FileStream(gloveLocation, FileMode.Open, FileAccess.Read, FileShare.None,
                    1024*1024,
                    FileOptions.SequentialScan))
            {
                using (var txtStream = new StreamReader(textFile))
                {
                    while (!txtStream.EndOfStream)
                    {
                        //optimise here next
                        var line = txtStream.ReadLine();
                        if (line == null) continue;

                        var splits = line.Split(spaceSeparator, StringSplitOptions.RemoveEmptyEntries);
                        var word = splits[0];
                        var vecs = splits.Skip(1).Select(float.Parse).ToArray();
                        gloveDictionary[word] = new TAGloveWord(word, vecs);
                    }
                }
            }
            /*
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs);
            Console.ReadLine();*/
            //-------
        }

        private static void ShowFileKeywords(TAFile file, Dictionary<string, TAWord> wordsDictionary)
        {
            const int numKeyWords = 5;
            //find the count of the most common word
            //don't include anything with an id of < 1
            int[] fileHighest = new int[numKeyWords];
            int[] highestIndex = new int[numKeyWords];
            for (int i = 0; i < numKeyWords; i++)
            {
                fileHighest[i] = 0;
            }
            for (int i = 0; i < file.WordCounts.Length; i++)
            {
                //if new value is smaller then move on
                if (file.WordCounts[i] <= fileHighest[numKeyWords - 1]) continue;
                fileHighest[numKeyWords - 1] = file.WordCounts[i];
                highestIndex[numKeyWords - 1] = i;
                //sort out the current keywords from the bottom up
                for (int x = numKeyWords - 1; x > 0; x--)
                {
                    if (fileHighest[x] <= fileHighest[x - 1]) continue;
                    //swap the values
                    var swapSpace = fileHighest[x - 1];
                    fileHighest[x - 1] = fileHighest[x];
                    fileHighest[x] = swapSpace;
                    //swap the indexes
                    swapSpace = highestIndex[x - 1];
                    highestIndex[x - 1] = highestIndex[x];
                    highestIndex[x] = swapSpace;
                }
            }
            //fileHighest holds the number of times appeared
            //and highestIndex holds the index of the top 5 most common words
            //for now just write out the words this results in
            TAWord[] words = new TAWord[numKeyWords];
            foreach (var word in wordsDictionary)
            {
                for (int x = 0; x < numKeyWords; x++)
                {
                    if (word.Value.Id != highestIndex[x]) continue;
                    words[x] = word.Value;
                }
            }
            Console.WriteLine("In descending order, the most common words are:");
            for (int i = 0; i < numKeyWords; i++)
            {
                Console.WriteLine(words[i].Text + " is used " + fileHighest[i]);
            }
        }

        private static void ShowFileWordCount(TAFile file, Dictionary<string, TAWord> wordsDictionary)
        {
            //word counts has the count of words in the file stored at the index = to the word id
            foreach (var word in wordsDictionary)
            {
                //this stops searching if it starts checking words that were added after this file was loaded
                //this saves more time on files loaded earlier
                if (word.Value.Id > file.WordCounts.Length - 1) break;

                //don't include punctuation //don't include words not used
                if (word.Value.Id <= 0 || file.WordCounts[word.Value.Id] < 1) continue;

                Console.WriteLine("The word " + word.Value.Text + " was used " + file.WordCounts[word.Value.Id] +
                                  " times");
            }
        }

        private static void FileWordCount(TAFile file, Dictionary<string, TAWord> wordsDictionary)
        {
            //int array to store the word counts
            int[] wordCounts = new int[wordsDictionary.Count];
            //run through each wordId and add to it's count
            foreach (var wordId in file.FileContent)
            {
                //do not count values less than 1 as these are not words
                if (wordId > 0) wordCounts[wordId] ++;
            }
            file.WordCounts = wordCounts;
        }

        private static void AddWord(ref Dictionary<string, TAWord> wordsDictionary, ref int wordNumber,
            ref TAFile tempFile, string word)
        {
            if (word == null)
            {
                return;
            }
            TAWord wordRef;
            //check the dictionary to see if the word is already there
            if (wordsDictionary.TryGetValue(word, out wordRef))
            {
                //if the word is already in the dictionary
                //add the numerical value to the content list
                wordRef.Count++;
                tempFile.FileContent.Add(wordRef.Id);
            }
            else
            {
                //add the word to the dictionary and send the numerical value to the file content
                wordNumber++;
                var newWord = new TAWord(wordNumber, word);
                wordsDictionary.Add(word, newWord);
                tempFile.FileContent.Add(newWord.Id);
            }
            //Console.WriteLine(word);
        }

        public static void ShowWordCount(Dictionary<string, TAWord> wordsDictionary)
        {
            //output the count for each word
            foreach (var x in wordsDictionary)
            {
                if (x.Value.Id <= 0) continue;
                Console.WriteLine("The word " + x.Value.Text + " was used " + x.Value.Count + " times.");
            }
        }

        private static void ShowFileText(List<int> fileContent, HashSet<char> wordCharsSet,
            Dictionary<string, TAWord> wordsDictionary)
        {
            string output = null;
            Console.WriteLine();
            string wordText = " ";
            foreach (var x in fileContent)
            {
                foreach (var word in wordsDictionary.Values)
                {
                    if (word.Id != x) continue;
                    wordText = word.Text;
                    break;
                }
                //at the end of sentence write out the sentence
                if (wordText == "end of sentence")
                {
                    Console.WriteLine(output);
                    output = null;
                } //at the end of the file make an empty line
                else if (wordText == "end of file")
                {
                    Console.WriteLine();
                } //otherwise it needs to be printed
                else
                {
                    // if it is a word or a number then put a space before it
                    if (wordCharsSet.Contains(wordText[0]) || wordText == "###")
                    {
                        if (output == null)
                        {
                            // if it's the start of a sentence
                            output = wordText;
                        }
                        else
                        {
                            // if in a sentence
                            output = output + " " + wordText;
                        }
                    }
                    else
                    {
                        // it is punctuation so put no space before it
                        output = output + wordText;
                    }
                }
            }
        }

        private static bool LoadFile(ref Dictionary<string, TAWord> wordsDictionary, ref int wordNumber,
            char[] eosChars, HashSet<char> wordCharsSet, HashSet<char> numCharsSet, ref TAFile tempFile)
        {
            string line;
            string word = null;
            var readNum = false;
            // Read the file, saving new words to the dictionary, upping the count of that word
            // and saving the word designation to fileContent
            var file =
                new StreamReader(
                    $@"{tempFile.FileName}.txt");
            while ((line = file.ReadLine()) != null)
            {
                line = line.ToLower(); //use lowercase only
                if (word?.EndsWith("-") == false) //if word was split onto this line
                    // e.g. writ-
                    // ing
                {
                    AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
                    word = null;
                }
                foreach (var x in line)
                {
                    var inputPlaced = false;
                    //if previous letter was part of a word
                    if (readNum)
                    {
                        if (numCharsSet.Contains(x))
                        {
                            continue;
                        }
                        word = "###";
                        AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
                        word = x == ' ' ? null : x.ToString();
                        readNum = false;
                        continue;
                    }
                    //check for a sentence end
                    foreach (var y in eosChars)
                    {
                        if (x != y) continue;
                        //it is the end of a sentence
                        inputPlaced = true;
                        if (word != null)
                        {
                            AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
                            //clear word
                            word = null;
                        }
                        //add what ended the sentence
                        TAWord wordRef;
                        if (wordsDictionary.TryGetValue(y.ToString(), out wordRef))
                        {
                            tempFile.FileContent.Add(wordRef.Id);
                            wordRef.Count++;
                        }
                        //add the eos marker
                        tempFile.FileContent.Add(EndOfSentence.Id);
                        break;
                    }
                    if (inputPlaced) continue;
                    //not the end of a sentence
                    //check for the end of a word
                    var found = wordCharsSet.Contains(x);
                    if (found == false)
                    {
                        //if it is a number
                        if (numCharsSet.Contains(x) && x != '.' && x != ',')
                        {
                            //look for more number
                            readNum = true;
                        }
                        else
                        {
                            //end of word
                            AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
                            //save punctuation
                            if (x != ' ')
                            {
                                //if common punctuation
                                if (wordsDictionary.ContainsKey(x.ToString()))
                                {
                                    word = x.ToString();
                                    AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
                                }
                                else
                                {
                                    // not common punctuation
                                    AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, "~");
                                }
                            }
                            //clear the word
                            word = null;
                        }
                    }
                    else
                    {
                        //not end of word
                        //add the character read to the word
                        word = word + x;
                    }
                }
            }

            file.Close();
            //check if there is a word unsaved

            AddWord(ref wordsDictionary, ref wordNumber, ref tempFile, word);
            //add end of file to file content
            tempFile.FileContent.Add(EndOfFile.Id);
            return true;
        }

        private static void Main()
        {
            Dictionary<string, TAGloveWord> gloveDictionary = null;
            var wordsDictionary = new Dictionary<string, TAWord>
            {
                {"E.O.F", EndOfFile},
                {"E.O.S", EndOfSentence},
                {"###", NumberWord},
                {"~", PunctWord},
                {".", new TAWord(-1, ".")},
                {"?", new TAWord(-2, "?")},
                {"!", new TAWord(-3, "!")},
                {",", new TAWord(-4, ",")},
                {";", new TAWord(-5, ";")},
                {":", new TAWord(-6, ":")},
                {"(", new TAWord(-7, "(")},
                {")", new TAWord(-8, ")")},
                {"[", new TAWord(-9, "[")},
                {"]", new TAWord(-10, "]")},
                {"{", new TAWord(-11, "{")},
                {"}", new TAWord(-12, "}")},
                {"\"", new TAWord(-13, "\"")},
                {"/", new TAWord(-14, "/")},
                {"\\", new TAWord(-15, "\\")}
            };
            //all the words to ignore are saved as -50 and below to make them easy to ignore
            for (var x = 0; x < StopWords.Length; x++)
            {
                var tempWord = new TAWord((-50 - x), StopWords[x]);
                wordsDictionary.Add(StopWords[x], tempWord);
            }
            //dictionary has the string of the word referencing the class the program uses
            var eosChars = new char[3];
            eosChars[0] = '.';
            eosChars[1] = '?';
            eosChars[2] = '!';
            var wordChars = new[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '-', '\''
            };
            var numChars = new[]
            {
                '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ','
            };
            var fileList = new List<TAFile>();

            var numCharsSet = new HashSet<char>(numChars);
            var wordCharsSet = new HashSet<char>(wordChars);
            var fileLoaded = false;
            var wordNumber = 0;
            /*
            const int numCheck = 10;            
            do
            {
                Console.WriteLine("Please enter the file location for the Glove data.");
                var gloveLocation = Console.ReadLine();

                if (!File.Exists($@"{gloveLocation}.txt"))
                {
                    //file name could not be found
                    Console.WriteLine("No such file was found");
                    break;
                }
                gloveLocation = string.Concat(gloveLocation, ".txt");
                Console.Clear();
                Console.WriteLine("Loading data. This may take a minute...");
                LoadGloveData(ref gloveDictionary, gloveLocation);
            } while (gloveDictionary == null);
            Console.Clear();
            */
            string choice;
            do
            {
                Console.WriteLine("------");
                Console.WriteLine("What would you like to do?");
                Console.WriteLine("1. load a file");
                Console.WriteLine("2. Show the file contents");
                Console.WriteLine("3. show the word counts across all documents");
                Console.WriteLine("4. show the word count for a specified file");
                Console.WriteLine("5. show the keywords for a given file");
                Console.WriteLine("6. find words related to your input using the glove data (unavailable as the glove data is missing)");
                Console.WriteLine("7. find the main theme of a file (unavailable as the glove data is missing)");
                //use glove data to find the main relationary theme

                Console.WriteLine("0. Close the program");
                choice = Console.ReadLine();
                Console.WriteLine();
                string input;
                int index;
                switch (choice)
                {
                    default:
                        Console.WriteLine("Invalid response");
                        break;
                    case "0":
                        choice = null;
                        break;
                    case "1":
                        Console.WriteLine("Please enter the file name e.g. Test");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //no input
                            Console.WriteLine("No file name given");
                            break;
                        }
                        input = input.ToLower();
                        //check if the file has been loaded already
                        if (fileList.Any(file => input == file.FileName))
                        {
                            Console.WriteLine("This file has already been loaded");
                            break;
                        }
                        //otherwise
                        //check to see if the file exists
                        if (
                            !File.Exists(
                                $@"{input}.txt"))
                        {
                            //file name could not be found
                            Console.WriteLine("No such file was found");
                            break;
                        }
                        //create a file with the name given
                        var tempFile = new TAFile {FileName = input};
                        //load in the file contents
                        fileLoaded = LoadFile(ref wordsDictionary, ref wordNumber, eosChars,
                            wordCharsSet, numCharsSet, ref tempFile);
                        //count up the number of times each word is used and store it
                        FileWordCount(tempFile, wordsDictionary);
                        //save the file to the file list
                        fileList.Add(tempFile);
                        Console.Clear();
                        Console.WriteLine(input + ".txt loaded");
                        break;
                    case "2":
                        if (!fileLoaded)
                        {
                            Console.WriteLine("No file loaded");
                            break;
                        }
                        //read the file content list and write the word
                        Console.WriteLine("Enter the name of the file");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //here for safety and better error messages
                            Console.WriteLine("No file name given");
                            break;
                        }
                        input = input.ToLower();
                        //check to see if the file given has been loaded
                        index = -1;
                        for (var i = 0; i < fileList.Count; i++)
                        {
                            if (fileList[i].FileName != input) continue;
                            index = i;
                        }
                        if (index == -1)
                        {
                            //the file name given didn't match a loaded document
                            Console.WriteLine("No such file found");
                            break;
                        }
                        //write out the file contents
                        Console.Clear();
                        ShowFileText(fileList[index].FileContent, wordCharsSet, wordsDictionary);
                        break;

                    case "3":
                        if (!fileLoaded)
                        {
                            Console.WriteLine("No files loaded");
                            break;
                        }
                        //read all file contents and output the words and the number of times used
                        Console.Clear();
                        ShowWordCount(wordsDictionary);
                        break;
                    case "4":
                        if (!fileLoaded)
                        {
                            //checks if any files have been loaded
                            Console.WriteLine("No file loaded");
                            break;
                        }
                        Console.WriteLine("Enter the name of the file");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //here for safety and specific error messages
                            Console.WriteLine("No file name was given");
                            break;
                        }
                        input = input.ToLower();
                        index = -1;
                        for (var i = 0; i < fileList.Count; i++)
                        {
                            if (fileList[i].FileName != input) continue;
                            index = i;
                        }
                        if (index == -1)
                        {
                            //if the file name doesn't match any loaded file
                            Console.WriteLine("No such file found");
                            break;
                        }
                        //will only reach here if the filename matches a loaded file
                        Console.Clear();
                        //call the function that does the work
                        ShowFileWordCount(fileList[index], wordsDictionary);
                        break;
                    case "5":
                        if (!fileLoaded)
                        {
                            //if no file has been loaded there is nothing to do
                            Console.WriteLine("No file loaded");
                            break;
                        }
                        Console.WriteLine("Enter the name of the file");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //here for safety and specific errors
                            Console.WriteLine("No file name was given");
                            break;
                        }
                        input = input.ToLower();
                        index = -1;
                        for (var i = 0; i < fileList.Count; i++)
                        {
                            if (fileList[i].FileName != input) continue;
                            index = i;
                        }
                        if (index == -1)
                        {
                            //if the file name doesn't match a loaded file
                            Console.WriteLine("No such file found");
                            break;
                        }
                        //will only be here if the filename matches a loaded file
                        Console.Clear();
                        //call sub to do the work
                        ShowFileKeywords(fileList[index], wordsDictionary);
                        break;
                        /*
                    case "6":
                        Console.WriteLine("Please enter a word for comparison");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //safety for ToLower
                            Console.WriteLine("No word was given");
                            break;
                        }
                        input = input.ToLower();
                        //numCheck is a const for now
                        var relatedWords = GloveCompareAll(gloveDictionary, input, numCheck);
                        if (relatedWords == null)
                        {
                            //returns null if a bad word was given
                            Console.WriteLine("Word not recognised");
                            break;
                        }
                        //outputs
                        var output = string.Concat("Main word was ", input);
                        Console.WriteLine(output);
                        Console.WriteLine("The contents of the array are as follows:");
                        for (var x = 0; x < 11; x++)
                        {
                            output = string.Concat("[", x, "] ", relatedWords[x].Text);
                            Console.WriteLine(output);
                        }
                        break;
                    case "7":
                        if (!fileLoaded)
                        {
                            //no file loaded means there is nothing to do
                            Console.WriteLine("No file loaded");
                            break;
                        }
                        index = -1;
                        Console.WriteLine("Enter the name of the file");
                        input = Console.ReadLine();
                        Console.WriteLine();
                        if (input == null)
                        {
                            //file name not given
                            Console.WriteLine("No file name given");
                            break;
                        }
                        input = input.ToLower();
                        for (var i = 0; i < fileList.Count; i++)
                        {
                            //find the index of the file name
                            if (fileList[i].FileName != input) continue;
                            index = i;
                            break;
                        }
                        if (index == -1)
                        {
                            //the file name has not been loaded so nothing to do
                            Console.WriteLine("No such file found");
                            break;
                        }
                        Console.WriteLine("Please enter the word you wish to check");
                        var checkWord = Console.ReadLine();
                        Console.WriteLine();
                        if (checkWord == null)
                        {
                            //safety for ToLower
                            Console.WriteLine("No word given");
                            break;
                        }
                        checkWord = checkWord.ToLower();

                        Console.Clear();
                        Console.WriteLine("Analysing the file. This will take a minute...");
                        //call sub to do the work
                        var result = IsTextAbout(fileList[index], wordsDictionary, gloveDictionary, checkWord);
                        if (result >= 1)
                        {
                            Console.WriteLine(checkWord + " is very strongly related to the contents of the file.");
                        }
                        else if (result >= 0.5)
                        {
                            Console.WriteLine(checkWord + " is related to the file contents.");
                        }
                        else if (result > 0)
                        {
                            Console.WriteLine(checkWord + " is not related to the file contents.");
                        }
                        else
                        {
                            Console.WriteLine("Please enter a more significant word than " + checkWord + ".");
                        }
                        break;
                        */
                    case "c":
                        Console.Clear();
                        break;
                    case "C":
                        Console.Clear();
                        break;
                        /*
                    case "t":
                        Console.WriteLine("enter a number");
                        input = Console.ReadLine();
                        Test(Convert.ToInt32(input));
                        break;
                        */
                }
            } while (choice != null);

            Console.WriteLine("end of program");
            Console.ReadLine();
        }
    }
}