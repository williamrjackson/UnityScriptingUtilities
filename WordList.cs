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
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<WordList>();
                if (_instance != null) return _instance;
                if (!Application.isPlaying)
                {
                    return null;
                }
                GameObject go = new GameObject();
                go.name = "WordList";
                _instance = go.AddComponent<WordList>();
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
            if (wordListTextAsset == null) return;
            wordSet = ParseWordSet(wordListTextAsset);
            TextAsset fullDictionaryTextAsset = Resources.Load("WordList", typeof(TextAsset)) as TextAsset;
            if (fullDictionaryTextAsset != null)
            {
                fullWordSet = ParseWordSet(fullDictionaryTextAsset);
            }
        }
        public void Init(TextAsset wordListTextAsset, TextAsset fullDictionaryTextAsset)
        {
            if (wordListTextAsset == null || fullDictionaryTextAsset == null) return;
            wordSet = ParseWordSet(wordListTextAsset);
            fullWordSet = ParseWordSet(fullDictionaryTextAsset);
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
            if (fullDictionaryTextAsset != null)
            {
                fullWordSet = ParseWordSet(fullDictionaryTextAsset);
            }
            if (wordSource != WordSource.Full)
            {
                TextAsset wordListTextAsset = Resources.Load(strWordResourceName, typeof(TextAsset)) as TextAsset;
                if (wordListTextAsset != null)
                {
                    wordSet = ParseWordSet(wordListTextAsset);
                }
            }
        }

        private static HashSet<string> ParseWordSet(TextAsset asset)
        {
            string[] parts = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> set = new HashSet<string>();
            foreach (string p in parts)
            {
                string trimmed = p.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    set.Add(trimmed);
                }
            }
            return set;
        }

        public static bool CheckWord(string word, bool fullDict = true)
        {
            if (Instance == null || string.IsNullOrWhiteSpace(word)) return false;
            if (fullDict)
            {
                return Instance.fullWordSet != null && Instance.fullWordSet.Contains(word.ToLower());
            }
            return Instance.WordSet != null && Instance.WordSet.Contains(word.ToLower());
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
            if (Instance == null) return null;
            List<string> commonList = (fullDict) ? Instance.fullWordSet?.ToList() : Instance.WordSet?.ToList();
            if (commonList == null || commonList.Count == 0) return null;
            return commonList.GetRandom();
        }
        public static List<string> GetRandomWords(int count, int minLength = 3, int maxLength = 7, bool fullDict = false)
        {
            if (Instance == null) return new List<string>();
            List<string> wordSetList = (fullDict) ? Instance.fullWordSet?.ToList() : Instance.WordSet?.ToList();
            if (wordSetList == null || wordSetList.Count == 0) return new List<string>();
            List<string> candidates = wordSetList.FindAll(w => w.Length >= minLength && w.Length <= maxLength);
            if (candidates.Count == 0) return new List<string>();
            if (count > candidates.Count) count = candidates.Count;
            List<string> results = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                results.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return results;
        }
        public static string WordOfTheDay(bool fullDict = false)
        {
            if (Instance == null) return null;
            List<string> words = (fullDict) ? Instance.fullWordSet?.ToList() : Instance.WordSet?.ToList();
            if (words == null || words.Count == 0) return null;
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
