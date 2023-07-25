
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;
using Newtonsoft.Json;

using System;
using System.IO;
using System.Net;
using System.Text;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using System.Threading.Tasks;
namespace MustGames.MGSD.Editor
{
    public class MGIGSavedData
    {
        public string user;
        public string key;
    }

    [Serializable]
    public class GenerateMGIGAPI
    {
        public string prompt;
        public string extra_prompt;
        public string extra_n_prompt;
    }

    [Serializable]
    public class UnitPrompt
    {
        public string prompt_name;
        public string prompt_explain;
        public string recommend_prompt;
    }


    [Serializable]
    public class PromptList
    {
        public string user;
        public int grade;
        public int genCount;
        public string expireAt;
        public int limitation = 0;
        public int remainCount = 0;
        public UnitPrompt[] prompt;
    }

    public class APIFailed
    {
        public int errorCode;
        public string errorMsg;
    }



    public class ImageGeneratorMGSD : EditorWindow
    {
        private EditorCoroutine _updateProgressRunning = null;
        MGIGSavedData savedData;

        string serverUrl = "http://34.22.69.21:8080";//"http://222.237.175.238:3327";//"http://127.0.0.1:8080";//
        string user;
        string key;
        string filename;


        string grade;
        string expireTime;
        int genCount;

        int limitation = 0;
        int remainCount = 0;

        bool bLogin = false;
        bool bInit = false;

        int promptIndex = 0;
        int recommendIndex = 0;
        string[] prompts;
        string[] promptExplain;
        string[] recommendPrompt;


        string[] recommendPrompts;



        Texture2D texture = null;

        PromptList promptList;

        string extraPrompt;
        string extraNPrompt;

        bool bUnLimitedGen = false;

        readonly GenerateMGIGAPI promptAPI = new GenerateMGIGAPI();

        [MenuItem("MGSD/ImageGeneratorMGSD %m")]
        static void Init()
        {
            // 생성되어있는 윈도우를 가져온다. 없으면 새로 생성한다.
            ImageGeneratorMGSD window = (ImageGeneratorMGSD)EditorWindow.GetWindow(typeof(ImageGeneratorMGSD));
            window.Show();
            window.LoadSaved();
        }

        public void LoadSaved()
        {
            string path = CommonUtility.PathForDocumentsFile($"/Saved/mgigData.json");
            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryReader binReader = new BinaryReader(file);
                var jsonData = binReader.ReadString();

                jsonData = CommonUtility.xorIt("Secret", jsonData);
                savedData = JsonUtility.FromJson<MGIGSavedData>(jsonData);
                user = savedData.user;
                key = savedData.key;
                file.Close();
            }
        }

        void SaveData()
        {
            if (savedData == null)
            {
                savedData = new MGIGSavedData();
            }
            savedData.user = user;
            savedData.key = key;

            string jsonData = JsonUtility.ToJson(savedData);

            jsonData = CommonUtility.xorIt("Secret", jsonData);

            string path = CommonUtility.PathForDocumentsFile($"/Saved/");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += "mgigData.json";

            FileStream file = File.Open(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(jsonData);
            file.Close();
        }

        void parseRecommendPrompts(int inPromptIndex)
        {
            string wholeStr = recommendPrompts[inPromptIndex];
            if (wholeStr.Length > 0)
            {
                recommendPrompt = wholeStr.Split(';');
            }
            else
            {
                recommendPrompt = null;
            }

        }

        void OnGUI()
        {
            if (bInit == false)
            {
                bInit = true;

            }
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            if (!bLogin)
            {
                user = EditorGUILayout.TextField("User Name", user);

                key = EditorGUILayout.PasswordField("Key", key);

                if (GUILayout.Button("Request Login", GUILayout.Height(70)))
                {
                    EditorCoroutineUtility.StartCoroutine(RequestPromptList(), this);
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("User Name");
                EditorGUILayout.LabelField(user);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Account Grade");
                EditorGUILayout.LabelField(grade);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Expire Date");
                EditorGUILayout.LabelField(expireTime);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total generate count");
                EditorGUILayout.LabelField(genCount.ToString());
                EditorGUILayout.EndHorizontal();

                if (limitation == 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("남은 생성 횟수");
                    EditorGUILayout.LabelField(remainCount.ToString());
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (prompts != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    int prevIndex = promptIndex;
                    promptIndex = EditorGUILayout.Popup("Prompt", promptIndex, prompts);
                    if (prevIndex != promptIndex)
                    {
                        recommendIndex = 0;
                        parseRecommendPrompts(promptIndex);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (promptExplain != null && promptExplain.Length > 0)
                    {
                        EditorGUILayout.LabelField(promptExplain[promptIndex]);
                    }
                    if (recommendPrompt != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        recommendIndex = EditorGUILayout.Popup("recommend Prompt", recommendIndex, recommendPrompt);
                        if (GUILayout.Button("use recommend", GUILayout.Height(30)))
                        {
                            extraPrompt = recommendPrompt[recommendIndex];
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                extraPrompt = EditorGUILayout.TextField("Extra Prompt", extraPrompt);
                extraNPrompt = EditorGUILayout.TextField("Extra Negative Prompt", extraNPrompt);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                bUnLimitedGen = EditorGUILayout.Toggle("무한 생성", bUnLimitedGen);


                if (GUILayout.Button("Generate Image", GUILayout.Height(70)))
                {
                    EditorCoroutineUtility.StartCoroutine(GenerateAsync(), this);
                }


                if (texture != null)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();

                    float x = (lastRect.xMax - lastRect.x - 10);
                    float y = x * ((float)512 / (float)512);

                    EditorGUI.DrawPreviewTexture(new Rect(lastRect.x + 5, lastRect.yMax + 5, lastRect.xMax - 10, y), texture);
                }
            }
        }


        void SetupFolders()
        {
            try
            {
                // Determine output path
                string realPath = Application.streamingAssetsPath;
                filename = Path.Combine(realPath, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".png");

                // If folders not already exists, create them
                if (!Directory.Exists(realPath))
                    Directory.CreateDirectory(realPath);

                // If the file already exists, delete it
                if (File.Exists(filename))
                    File.Delete(filename);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n\n" + e.StackTrace);
            }
        }

        IEnumerator RequestPromptList()
        {
            SaveData();
            string url = serverUrl + "/api/stablediffusion/promptlist";

            string json = "";
            using (UnityWebRequest request = UnityWebRequest.Post(url, json))
            {
                if (json.Length > 0)
                {
                    if (request.uploadHandler != null)
                    {
                        request.uploadHandler.Dispose();
                        request.uploadHandler = null;
                    }
                    byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                }
                request.timeout = 40;
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                request.SetRequestHeader("user", user);
                request.SetRequestHeader("Authorization", key);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(request.error);
                    Debug.Log(request.downloadHandler.text);

                    var result = JsonUtility.FromJson<APIFailed>(request.downloadHandler.text);
                    if (result == null || result.errorCode == 500)
                    {
                        EditorUtility.DisplayDialog("MGSD", "알수 없는 문제가 발생했습니다.\n관리자에게 문의해 주세요.", "OK");
                    }
                    else if (result.errorCode == 402)
                    {
                        EditorUtility.DisplayDialog("MGSD", "계정 유효기간이 만료되었습니다.", "OK");
                    }
                    else if (result.errorCode == 401)
                    {
                        EditorUtility.DisplayDialog("MGSD", "계정 정보가 일치하지 않습니다.", "OK");
                    }
                    else if (result.errorCode == 403)
                    {
                        EditorUtility.DisplayDialog("MGSD", "계정에 할당된 최대 생성 횟수에 도달했습니다.", "OK");
                    }
                    else if (result.errorCode == 404)
                    {
                        EditorUtility.DisplayDialog("MGSD", "스테이블 디퓨전 머신에 이상이 생겼습니다.\n관리자에게 문의해 주세요.", "OK");
                    }
                }
                else
                {
                    bLogin = true;
                    promptList = JsonUtility.FromJson<PromptList>(request.downloadHandler.text);
                    if (promptList.prompt != null)
                    {
                        prompts = new string[promptList.prompt.Length];
                        promptExplain = new string[promptList.prompt.Length];
                        recommendPrompts = new string[promptList.prompt.Length];
                        for (int i = 0; i < promptList.prompt.Length; ++i)
                        {
                            prompts[i] = promptList.prompt[i].prompt_name;
                            promptExplain[i] = promptList.prompt[i].prompt_explain;
                            recommendPrompts[i] = promptList.prompt[i].recommend_prompt;
                        }
                    }

                    parseRecommendPrompts(promptIndex);


                    switch (promptList.grade)
                    {
                        case 0:
                            grade = "관리자";
                            break;
                        case 1:
                            grade = "사용자";
                            break;
                        case 2:
                            grade = "테스터";
                            break;
                    }
                    genCount = promptList.genCount;
                    expireTime = promptList.expireAt;
                    limitation = promptList.limitation;
                    remainCount = promptList.remainCount;


                    Repaint();
                }
            }
        }

        private int progressIG = 0;

        IEnumerator ShowProgress()
        {
            progressIG = 0;
            var wait = new EditorWaitForSeconds(1.0f);

            while (_updateProgressRunning != null && progressIG < 100)
            {
                progressIG = Mathf.Min(progressIG + 3, 100);
                float progress = (float)progressIG * 0.01f;
                EditorUtility.DisplayProgressBar("Generation in progress ", progressIG + "%", progress);

                yield return wait;
            }
        }

        IEnumerator GenerateAsync()
        {
            if (_updateProgressRunning == null)
            {
                _updateProgressRunning = EditorCoroutineUtility.StartCoroutine(ShowProgress(), this);

                string url = serverUrl + "/api/stablediffusion/generate";

                promptAPI.prompt = prompts[promptIndex];
                promptAPI.extra_prompt = extraPrompt;
                promptAPI.extra_n_prompt = extraNPrompt;

                string json = JsonUtility.ToJson(promptAPI, true);
                using (UnityWebRequest request = UnityWebRequest.Post(url, json))
                {
                    if (json.Length > 0)
                    {
                        if (request.uploadHandler != null)
                        {
                            request.uploadHandler.Dispose();
                            request.uploadHandler = null;
                        }
                        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
                        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                    }
                    request.timeout = 100;
                    request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                    request.SetRequestHeader("user", user);
                    request.SetRequestHeader("Authorization", key);
                    request.SetRequestHeader("Content-Type", "application/json");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        progressIG = 100;
                        yield return new EditorWaitForSeconds(0.5f);
                        //StopCoroutine has exception bug
                        //EditorCoroutineUtility.StopCoroutine(_updateProgressRunning);
                        _updateProgressRunning = null;
                        EditorUtility.ClearProgressBar();


                        Debug.Log(request.error);
                        Debug.Log(request.downloadHandler.text);
                        Repaint();

                        var result = JsonUtility.FromJson<APIFailed>(request.downloadHandler.text);
                        if (result.errorCode == 402)
                        {
                            EditorUtility.DisplayDialog("MGSD", "계정 유효기간이 만료되었습니다.", "OK");
                        }
                        else if (result.errorCode == 401)
                        {
                            EditorUtility.DisplayDialog("MGSD", "계정 정보가 일치하지 않습니다.", "OK");
                        }
                        else if (result.errorCode == 403)
                        {
                            EditorUtility.DisplayDialog("MGSD", "계정에 할당된 최대 생성 횟수에 도달했습니다.", "OK");
                        }
                        else if (result.errorCode == 404)
                        {
                            EditorUtility.DisplayDialog("MGSD", "스테이블 디퓨전 머신에 이상이 생겼습니다.\n관리자에게 문의해 주세요.", "OK");
                        }
                        else if (result.errorCode == 500)
                        {
                            EditorUtility.DisplayDialog("MGSD", "알수 없는 문제가 발생했습니다.\n관리자에게 문의해 주세요.", "OK");
                        }
                    }
                    else
                    {
                        if (_updateProgressRunning != null)
                        {
                            progressIG = 100;
                            //StopCoroutine has exception bug
                            //EditorCoroutineUtility.StopCoroutine(_updateProgressRunning);
                        }


                        yield return new EditorWaitForSeconds(0.5f);
                        EditorUtility.DisplayProgressBar("Save Image ... ", 90 + "%", 0.9f);
                        SetupFolders();
                        byte[] imageData = Convert.FromBase64String(request.downloadHandler.text);
                        using (FileStream imageFile = new FileStream(filename, FileMode.Create))
                        {
#if UNITY_EDITOR
                            AssetDatabase.StartAssetEditing();
#endif
                            yield return imageFile.WriteAsync(imageData, 0, imageData.Length);
#if UNITY_EDITOR
                            AssetDatabase.StopAssetEditing();
                            AssetDatabase.SaveAssets();
#endif
                            if (File.Exists(filename))
                            {
                                texture = new Texture2D(2, 2);
                                texture.LoadImage(imageData);
                                texture.Apply();

                                //LoadIntoImage(texture);
                            }
                        }
                        EditorUtility.DisplayProgressBar("Finished ", 100 + "%", 1.0f);
                        yield return new EditorWaitForSeconds(0.1f);
                        EditorUtility.ClearProgressBar();

                        _updateProgressRunning = null;
                        Repaint();

                        EditorCoroutineUtility.StartCoroutine(RequestPromptList(), this);


                        if (bUnLimitedGen)
                        {
                            if (ImageGeneratorMGSD.HasOpenInstances<ImageGeneratorMGSD>())
                            {
                                EditorCoroutineUtility.StartCoroutine(GenerateAsync(), this);
                            }
                        }
                    }
                }
            }
        }
    }
}