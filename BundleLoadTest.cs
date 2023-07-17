using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class BundleLoadTest : MonoBehaviour
{
    //private string mPath = string.Empty;
    private string mPathRoot;
    private AssetBundleManifest assetBundleManifest;
    // Start is called before the first frame update
    void Start()
    {
        mPathRoot = Path.Combine(Application.streamingAssetsPath, "AssetsAndroid");
        //mPath = Path.Combine(Application.streamingAssetsPath, "AssetsAndroid/assetsmapping.bytes");
        //StartCoroutine(LoadMap());

        var bundle = AssetBundle.LoadFromFile(Path.Combine(mPathRoot, "assetbundle"));
        assetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        //StartCoroutine(LoadScene(Path.Combine(mPathRoot, "scene_hycx.unity3d")));
        LoadUI(Path.Combine(mPathRoot, "uifashion_uiprefab.unity3d"), "UIFashion");
    }

    void LoadUI(string bundlePath, string uiName)
    {
        string assetbundleName = Path.GetFileName(bundlePath);
        var deps = assetBundleManifest.GetAllDependencies(assetbundleName);
        foreach (var dep in deps)
        {
            AssetBundle.LoadFromFile(Path.Combine(mPathRoot,dep));
        }
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        var ui = Instantiate(bundle.LoadAsset<GameObject>(uiName));
        ui.transform.SetParent(GameObject.Find("Canvas").transform, false);
    }

    IEnumerator LoadScene(string url)
    {
        AssetBundle bundle = null;
        using (var www = WWW.LoadFromCacheOrDownload(url, 0))
        {
            yield return www;
            if (www.error != null)
                throw new Exception("WWW download had an error:" + www.error);
            if (www.error == null)
            {
                bundle = www.assetBundle;
            }
        }
        if (Caching.ready == true)
        {
            string[] scenePath = bundle.GetAllScenePaths();
            Debug.Log(scenePath[0]);
            SceneManager.LoadScene(scenePath[0]);
        }
    }

    IEnumerator LoadMap(string path)
    {
        var www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();
        if (www.isDone)
        {
            var bytes = www.downloadHandler.data;
            var length = bytes.Length;
            byte value;
            for (int i = 0, iMax = Mathf.FloorToInt(length * 0.5f); i < iMax; i += 2)
            {
                value = bytes[i];
                bytes[i] = bytes[length - i - 1];
                bytes[length - i - 1] = value;
            }

            var encoding = new UTF8Encoding(false);
            string data = encoding.GetString(bytes);
            var contentLines = data.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            using (var sw = new StreamWriter(File.OpenWrite(Path.Combine(Application.streamingAssetsPath, "map.txt"))))
            {
                foreach (var line in contentLines)
                {
                    sw.WriteLine(line);
                }
                sw.Close();
                sw.Dispose();
            };

        }
    }
}
