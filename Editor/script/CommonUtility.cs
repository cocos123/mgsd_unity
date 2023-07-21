using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace MustGames {
	
	public static class CommonUtility {
	
		public static string PathForDocumentsFile (string file_name) {

			#if UNITY_EDITOR
			string path = Application.dataPath;
			path = path.Substring (0, path.LastIndexOf ('/'));
			return string.Concat (path, file_name);
			#else
			switch (Application.platform) {
			case RuntimePlatform.IPhonePlayer: {
				string path = Application.persistentDataPath.Substring (0, Application.persistentDataPath.Length - 5);
				path = path.Substring (0, path.LastIndexOf ('/'));

				return string.Concat (string.Concat (path, "/Documents"), file_name);
			}
			case RuntimePlatform.Android: {
				string path = Application.persistentDataPath;
				path = path.Substring (0, path.LastIndexOf ('/'));

				return string.Concat (path, file_name);
			}
			default: {
				string path = Application.dataPath;
				path = path.Substring (0, path.LastIndexOf ('/'));
				return string.Concat (path, file_name);
			}}
			#endif
		}
        public static string xorIt(string key, string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ key[(i % key.Length)]));
            string result = sb.ToString();

            return result;
        }
    }
}