using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
    public class WordList : MonoBehaviour
    {
        private HashSet<string> wordSet; 
        private static WordList _instance;
        private static WordList Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject();
                    go.name = "WordCheck";
                    _instance = go.AddComponent<WordList>();
                }
                return _instance;
            }
        }
        public static bool CheckWord(string word)
        {
            return Instance.wordSet.Contains(word.ToUpper());
        }
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Init();
            }
        }

        private void Init()
        {
            TextAsset MytextAsset = Resources.Load("WordList", typeof(TextAsset)) as TextAsset;
            wordSet = new HashSet<string>(MytextAsset.text.Split("\n"[0]));
            Debug.Log($"Word list loaded: \n\tDictionary of {wordSet.Count} words.");
        }
    }
}
