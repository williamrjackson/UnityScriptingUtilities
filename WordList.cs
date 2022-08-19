using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wrj
{
    public class WordList : MonoBehaviour
    {
        private HashSet<string> wordSet; 
        private HashSet<string> fullWordSet; 
        public enum WordSource { Full, Common6000, Common3000, Common1000 }
        private WordSource wordSource = WordSource.Full;

        private static WordList _instance;
        public static WordList Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject();
                    go.name = "WordList";
                    _instance = go.AddComponent<WordList>();
                }
                return _instance;
            }
        }
        public HashSet<string> WordSet
        {
            get
            {
                if (wordSource == WordSource.Full)
                {
                    return fullWordSet;
                }
                return wordSet;
            }
        }
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Init(wordSource);
            }
        }

        public void Init(TextAsset wordListTextAsset)
        {
            wordSet = new HashSet<string>(wordListTextAsset.text.Split("\n"[0]));
            TextAsset fullDictionaryTextAsset = Resources.Load("WordList", typeof(TextAsset)) as TextAsset;
            fullWordSet = new HashSet<string>(fullDictionaryTextAsset.text.Split("\n"[0]));
        }
        public void Init(TextAsset wordListTextAsset, TextAsset fullDictionaryTextAsset)
        {
            wordSet = new HashSet<string>(wordListTextAsset.text.Split("\n"[0]));
            fullWordSet = new HashSet<string>(fullDictionaryTextAsset.text.Split("\n"[0]));
        }
        public void Init(WordSource wordSource)
        {
            string strWordResourceName = "WordList";
            switch (wordSource)
            {
                case WordSource.Common6000:
                    strWordResourceName = "WordListTop6000";
                    break;
                case WordSource.Common3000:
                    strWordResourceName = "WordListTop3000";
                    break;
                case WordSource.Common1000:
                    strWordResourceName = "WordListTop1000";
                    break;
                default:
                    strWordResourceName = "WordList";
                    break;
            }
            TextAsset fullDictionaryTextAsset = Resources.Load("WordList", typeof(TextAsset)) as TextAsset;
            fullWordSet = new HashSet<string>(fullDictionaryTextAsset.text.Split("\n"[0]));
            if (wordSource != WordSource.Full)
            {
                TextAsset wordListTextAsset = Resources.Load(strWordResourceName, typeof(TextAsset)) as TextAsset;
                wordSet = new HashSet<string>(wordListTextAsset.text.Split("\n"[0]));
            }
        }

        public static bool CheckWord(string word, bool fullDict = true)
        {
            if (fullDict)
            {
                return Instance.fullWordSet.Contains(word.ToLower());
            }
            return Instance.WordSet.Contains(word.ToLower());
        }
        public static List<string> GetPossibleWords(string chars, int minLength = 3, int maxLength = 7, bool fullDict = false)
        {
            chars = chars.ToLower();
            List<string> combinations = CharCombinations(chars.ToLower());
	        HashSet<string> results = new HashSet<string>();
            foreach (string item in combinations)
            {
                if (item.Length >= minLength && item.Length <= maxLength && CheckWord(item, fullDict))
                {
                    results.Add(item.ToUpper());
                }
            }
	        return results.ToList();
        }
        public static string RandomWord(bool fullDict = false)
        {
            List<string> commonList = (fullDict) ? Instance.fullWordSet.ToList() : Instance.WordSet.ToList();
            return commonList.GetRandom();
        }
        public static List<string> GetRandomWords(int count, int minLength = 3, int maxLength = 7, bool fullDict = false)
        {
            List<string> wordSetList = (fullDict) ? Instance.fullWordSet.ToList() : Instance.WordSet.ToList();
            List<string> results = new List<string>();

            for (int i = 0; i < count; i++)
            {
                string word = "";
                while (word.Length > maxLength || word.Length < minLength || results.Contains(word))
                {
                    word = wordSetList.GetRandom();
                }
                results.Add(word);
            }
            return results;
        }
        public static string WordOfTheDay(bool fullDict = false)
        {
            List<string> words = (fullDict) ? Instance.fullWordSet.ToList() : Instance.WordSet.ToList();
            DateTime today = DateTime.UtcNow.Date;
            Int64 todayInt = today.ToBinary();
            Int64 wordIndex = todayInt % (Int64)words.Count;
            int nWordIndex = Mathf.Abs((int)wordIndex);
            string wordOtD = words[(int)nWordIndex];
            return wordOtD;
        }

        /// http://stackoverflow.com/questions/7802822/all-possible-combinations-of-a-list-of-values
        public static List<string> CharCombinations(char[] inputCharArray, int minimumItems = 1,
                                                        int maximumItems = int.MaxValue)
        {
            int nonEmptyCombinations = (int)Mathf.Pow(2, inputCharArray.Length) - 1;
            List<string> listOfLists = new List<string>(nonEmptyCombinations + 1);

            // Optimize generation of empty combination, if empty combination is wanted
            if (minimumItems == 0)
                listOfLists.Add("");

            if (minimumItems <= 1 && maximumItems >= inputCharArray.Length)
            {
                // Simple case, generate all possible non-empty combinations
                for (int bitPattern = 1; bitPattern <= nonEmptyCombinations; bitPattern++)
                    listOfLists.Add(GenerateCombination(inputCharArray, bitPattern));
            }
            else
            {
                // Not-so-simple case, avoid generating the unwanted combinations
                for (int bitPattern = 1; bitPattern <= nonEmptyCombinations; bitPattern++)
                {
                    int bitCount = CountBits(bitPattern);
                    if (bitCount >= minimumItems && bitCount <= maximumItems)
                        listOfLists.Add(GenerateCombination(inputCharArray, bitPattern));
                }
            }

            return listOfLists;
        }
        public static List<string> CharCombinations(string input, int minItems = 1, int maxItems = int.MaxValue)
        {
            return CharCombinations(input.ToCharArray(), minItems, maxItems);
        }

        private static string GenerateCombination(char[] inputList, int bitPattern)
        {
            string thisCombination = string.Empty;// new List<T>(inputList.Length);
            for (int j = 0; j < inputList.Length; j++)
            {
                if ((bitPattern >> j & 1) == 1)
                    thisCombination += inputList[j];
            }
            return thisCombination;
        }

        /// <summary>
        /// Sub-method of CharCombinations() method to count the bits in a bit pattern. Based on this:
        /// https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetKernighan
        /// </summary>
        private static int CountBits(int bitPattern)
        {
            int numberBits = 0;
            while (bitPattern != 0)
            {
                numberBits++;
                bitPattern &= bitPattern - 1;
            }
            return numberBits;
        }
    }
}
