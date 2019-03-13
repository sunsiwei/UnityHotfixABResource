using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;


public class HotfixManager : MonoSingleton<HotfixManager> {

	// 平台名字
#if UNITY_ANDROID
	static string curPlatformName = "android";
#elif UNITY_IPHONE
	static string curPlatformName = "iphone";
#else
	static string curPlatformName = "win";
#endif

	// 版本配置文件
	static string versionFileName = "version.txt";
	// 资源服务器地址
	static string serverURL = "http://192.168.3.103/ResServer";
	
	// 检查是否需要热更
	public void Check(UnityAction<bool> callback)
	{
		StartCoroutine(CheckVersion(callback));
	}
	// 版本检查，callback参数：true进入游戏，false下载安装包
	private IEnumerator CheckVersion(UnityAction<bool> callback)
	{
		ShowLog("开始版本效验");
		string localVersionPath = Path.Combine(PersitentDataResourcePath, versionFileName);
		if(File.Exists(localVersionPath))
		{
			string serverVersionURL = Path.Combine(ServerResourceURL, versionFileName);
			using(UnityWebRequest uwr = UnityWebRequest.Get(serverVersionURL))
			{
				yield return uwr.SendWebRequest();
				if(uwr.isHttpError || uwr.isNetworkError)
				{
					Debug.LogError(uwr.error);
					yield break;
				}
				else
				{
					string localVersion = File.ReadAllText(localVersionPath);
					int[] localVersions = GetVersions(localVersion);
					int[] serverVersions = GetVersions(uwr.downloadHandler.text);
					Log(string.Format("本地版本{0}.{1}，服务器版本{2}.{3}", localVersions[0], localVersions[1], serverVersions[0], serverVersions[1]));
					if(localVersions[0] == serverVersions[0])
					{
						if(localVersions[1] >= serverVersions[1])
						{
							ShowLog("进入游戏");
							callback(true);
							yield return null;
						}
						else
						{
							Log("开始热更");
							yield return StartCoroutine(HotfixABResources());
							//更新本地version
							WriteFile(localVersionPath, uwr.downloadHandler.data);
							yield return StartCoroutine(CheckVersion(callback));
						}
					}
					else if(localVersions[0] < serverVersions[0])
					{
						Log("替换安装包");
						callback(false);
						yield return null;
					}
					else
					{
						ShowLog("进入游戏");
						callback(true);
						yield return null;
					}
				}
			}
		}
		else
		{
			Log("首次启动");
			yield return StartCoroutine(CopyStreamingAssetsToPersistentDataPath());
			yield return StartCoroutine(CheckVersion(callback));
		}
	}

	// 将StreamingAssets中资源拷贝到persistdatapath目录
	private IEnumerator CopyStreamingAssetsToPersistentDataPath()
	{
		string manifestPath = Path.Combine(StreamingAssetsResourcePath, curPlatformName);
		// 先加载manifest文件，读取所有ab资源
		using(var manifestWWW = new UnityWebRequest(manifestPath))
		{
			manifestWWW.downloadHandler = new DownloadHandlerAssetBundle(manifestPath, 0);
			yield return manifestWWW.SendWebRequest();
			if(manifestWWW.isNetworkError || manifestWWW.isHttpError)
			{
				yield return StartCoroutine(CopyFile(Path.Combine(StreamingAssetsResourcePath, versionFileName), Path.Combine(PersitentDataResourcePath, versionFileName)));
				ShowLog("StreamingAssets中没有manifest文件，仅拷贝version文件");
				yield break;
			}
			else
			{
				// 拷贝ab资源到持久目录
				var manifestAB = DownloadHandlerAssetBundle.GetContent(manifestWWW);
				var abManifest = manifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
				manifestAB.Unload(false);
				string[] abList = abManifest.GetAllAssetBundles();
				int totalCount = abList.Length;// 一个是版本配置文件，一个是manifest文件
				int completedCount = 0;
				foreach(var abName in abList)
				{
					string srcPath = Path.Combine(StreamingAssetsResourcePath, abName);
					string tarPath = Path.Combine(PersitentDataResourcePath, abName);
					yield return StartCoroutine(CopyFile(srcPath, tarPath));
					completedCount++;
					ShowLog(string.Format("拷贝AB资源 {0}/{1}", completedCount, totalCount));
				}
				yield return StartCoroutine(CopyFile(Path.Combine(StreamingAssetsResourcePath, curPlatformName), Path.Combine(PersitentDataResourcePath, curPlatformName)));
				ShowLog("拷贝manifest文件");
				yield return StartCoroutine(CopyFile(Path.Combine(StreamingAssetsResourcePath, versionFileName), Path.Combine(PersitentDataResourcePath, versionFileName)));
				ShowLog("拷贝version文件");
			}
		}
	}
	private IEnumerator CopyFile(string srcPath, string tarPath)
	{
		using(UnityWebRequest uwr = UnityWebRequest.Get(srcPath))
		{
			yield return uwr.SendWebRequest();
			if (uwr.isNetworkError || uwr.isHttpError) {
				Debug.Log(uwr.error);
				yield break;
			} else {
				if (File.Exists(tarPath)) {
					File.Delete(tarPath);
				} else {
					PathUtils.CreateFolderByFilePath(tarPath);
				}
				FileStream fs2 = File.Create(tarPath);
				fs2.Write(uwr.downloadHandler.data, 0, uwr.downloadHandler.data.Length);
				fs2.Flush();
				fs2.Close();
				yield return new WaitForEndOfFrame();
			}
		}
	}
	// 热更AB资源
	private IEnumerator HotfixABResources()
	{
		Log("热更AB资源");
		
		string serverManifestPath = Path.Combine(ServerResourceURL, curPlatformName);
		using(UnityWebRequest uwr = UnityWebRequest.Get(serverManifestPath))
		{
			yield return uwr.SendWebRequest();
			if(uwr.isNetworkError || uwr.isHttpError)
			{
				Debug.LogError(uwr.error);
				yield break;
			}
			else
			{
				var serverManifestAB = AssetBundle.LoadFromMemory(uwr.downloadHandler.data);
				AssetBundleManifest serverManifest = serverManifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
				serverManifestAB.Unload(false);

				List<string> downloadFiles;
				string localManifestPath = Path.Combine(PersitentDataResourcePath, curPlatformName);

				if(File.Exists(localManifestPath))
				{
					var localManifestAB = AssetBundle.LoadFromFile(localManifestPath);
					AssetBundleManifest localManifest = localManifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
					localManifestAB.Unload(false);
					downloadFiles = GetDownFileName(localManifest, serverManifest);
				}
				else
				{
					// 本地没有manifest文件，所有AB资源全部从服务器下载
					downloadFiles = new List<string>(serverManifest.GetAllAssetBundles());
				}
				
				yield return StartCoroutine(DownloadABFiles(downloadFiles));

				//更新本地manifest
				WriteFile(localManifestPath, uwr.downloadHandler.data);
				// string localVersionPath = Path.Combine(PersitentDataResourcePath, versionFileName);
				yield return new WaitForEndOfFrame();
			}
		}
	}
	// 得到需要下载的AB文件名
	private List<string> GetDownFileName(AssetBundleManifest localManifest, AssetBundleManifest serverManifest)
	{
		List<string> tempList = new List<string>();
		var localHashCode = localManifest.GetHashCode();
		var serverHashCode = serverManifest.GetHashCode();
		if(localHashCode != serverHashCode)
		{
			string[] localABList = localManifest.GetAllAssetBundles();
			string[] serverABList = serverManifest.GetAllAssetBundles();
			Dictionary<string, Hash128> localHashDic = new Dictionary<string, Hash128>();
			foreach(var iter in localABList)
			{
				localHashDic.Add(iter, localManifest.GetAssetBundleHash(iter));
			}
			foreach(var iter in serverABList)
			{
				if(localHashDic.ContainsKey(iter))
				{
					Hash128 serverHash = serverManifest.GetAssetBundleHash(iter);
					if(serverHash != localHashDic[iter])
					{
						tempList.Add(iter);
					}
				}
				else
				{
					tempList.Add(iter);
				}
			}
		}
		return tempList;
	}
	// 下载需要更新的文件
	private IEnumerator DownloadABFiles(List<string> downloadFiles)
	{
		int completedCount = 0;
		int totalCount = downloadFiles.Count;
		if(totalCount == 0)
		{
			Log("没有需要跟新的AB资源");
			yield break;
		}
		else
		{
			foreach(var iter in downloadFiles)
			{
				string path = Path.Combine(ServerResourceURL, iter);
				using(UnityWebRequest webReq = UnityWebRequest.Get(path))
				{
					yield return webReq.SendWebRequest();
					if(webReq.isNetworkError || webReq.isHttpError)
					{
						Debug.LogError(webReq.error);
						yield return null;
					}
					else
					{
						byte[] result = webReq.downloadHandler.data;
						//save file
						string downloadPath = Path.Combine(PersitentDataResourcePath, iter);
						WriteFile(downloadPath, result);
						completedCount++;
						yield return new WaitForEndOfFrame();
					}
					ShowLog(string.Format("下载AB资源path: {2}, 进度{0}/{1}", completedCount, totalCount, iter));
				}
			}
		}
	}
	
	void WriteFile(string path, byte[] data)
	{
		FileInfo fi = new FileInfo(path);
        DirectoryInfo dir = fi.Directory;
        if (!dir.Exists) {
            dir.Create();
        }
        FileStream fs = fi.Create();
        fs.Write(data, 0, data.Length);
        fs.Flush();
        fs.Close();
	} 
	
	private int[] GetVersions(string version)
	{
		string[] list = version.Split(".".ToCharArray());
		int[] intList = new int[]{int.Parse(list[0]), int.Parse(list[1])};
		return intList;
	}

// ------------------------------------------ path -----------------------------------------
	// AB资源在StreamingAssets中的路径
	private string StreamingAssetsResourcePath
	{
		get
		{
		#if UNITY_ANDROID && !UNITY_EDITOR
			string streamingAssetsPath = Application.streamingAssetsPath;
		#elif UNITY_IPHONE && !UNITY_EDITOR
			string streamingAssetsPath = "file://" + Application.streamingAssetsPath;
		#elif UNITY_STANDLONE_WIN||UNITY_EDITOR
			string streamingAssetsPath = "file://" + Application.streamingAssetsPath;
		#else
			string streamingAssetsPath = string.Empty;
		#endif
			return streamingAssetsPath + "/" + curPlatformName;
		}
	}
	//AB资源在persistentDataPath中的位置
	private string PersitentDataResourcePath
	{
		get
		{
			return Application.persistentDataPath + "/" + curPlatformName;
		}
	}
	// AssetBundle在资源服务器中的路径
	private string ServerResourceURL
	{
		get
		{
			return serverURL + "/" + curPlatformName;
		}
	}
	private void ShowLog(string str)
	{
		Debug.Log(str);
	}
	private void Log(string str)
	{
		Debug.Log(str);
	}
}
