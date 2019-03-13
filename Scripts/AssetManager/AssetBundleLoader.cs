using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;

public class AssetBundleLoader : IAssetLoader {

#if UNITY_ANDROID
	static string curPlatformName = "android";
#elif UNITY_IPHONE
	static string curPlatformName = "iphone";
#else
	static string curPlatformName = "win";
#endif
	private string assetRootPath;
	private string manifestPaht;
	private static AssetBundleManifest manifest;

	public AssetBundleLoader()
	{
		assetRootPath = Application.persistentDataPath + "/" + curPlatformName;
		manifestPaht = assetRootPath + "/" + curPlatformName;
	}

	public T LoadAsset<T>(string path) where T: class
	{
		string absolutepath = path;
		path = PathUtils.NormalizePath(path);

		Debug.Log("AssetBundleLoader Load Asset path: " + path);
		string assetBundleName = PathUtils.GetAssetBundleNameWithPath(path, assetRootPath);

		LoadManifest();

		// 加载依赖
		string[] dependencies = manifest.GetAllDependencies(assetBundleName);
		List<AssetBundle> assetBundleList = new List<AssetBundle>();
		foreach(string fileName in dependencies)
		{
			string dependencyPath = assetRootPath + "/" + fileName;
			Debug.Log("[AssetBundle]加载依赖 path: " + dependencyPath);
			assetBundleList.Add(AssetBundle.LoadFromFile(dependencyPath));
		}
		// 加载目标资源
		AssetBundle assetBundle = null;
		Debug.Log("[AssetBundle]加载目标资源： " + path);
		assetBundle = AssetBundle.LoadFromFile(path);
		Debug.Log("-------------assetBundle----------" + assetBundle);
		assetBundleList.Insert(0, assetBundle);
		Object obj = assetBundle.LoadAsset(Path.GetFileNameWithoutExtension(path), typeof(T));
Debug.Log("-------------assetBundle------2----" + obj);
		UnloadAssetBundle(assetBundleList);

		AssetManager.Instance.pushCache(absolutepath, obj);

		return obj as T;
	}

	public IEnumerator LoadAssetAsync<T>(string path, UnityAction<T> callback) where T: class
	{
		string absolutepath = path;
		path = PathUtils.NormalizePath(path);

		Debug.Log("[LoadAssetAsync] path: " + path);
		string assetBundleName = PathUtils.GetAssetBundleNameWithPath(path, assetRootPath);
		LoadManifest();
		string[] dependencies = manifest.GetAllDependencies(assetBundleName);

		AssetBundleCreateRequest createRequest;
		List<AssetBundle> assetBundleList = new List<AssetBundle>();
		foreach(string fileName in dependencies)
		{
			string dependencyPath = assetRootPath + "/" + fileName;

			Debug.Log("[AssetBundle]加载依赖 path: " + dependencyPath);
			createRequest = AssetBundle.LoadFromFileAsync(dependencyPath);
			yield return createRequest;

			if(createRequest.isDone)
			{
				assetBundleList.Add(createRequest.assetBundle);
			}
			else
			{
				Debug.LogError("[AssetBundle]加载依赖出错");
			}
		}

		AssetBundle assetBundle = null;
		Debug.Log("[AssetBunlde]加载目标资源 path: " + path);
		createRequest = AssetBundle.LoadFromFileAsync(path);
		yield return createRequest;

		if(createRequest.isDone)
		{
			assetBundle = createRequest.assetBundle;
			assetBundleList.Insert(0, assetBundle);
		}

		AssetBundleRequest abr = assetBundle.LoadAssetAsync(Path.GetFileNameWithoutExtension(path), typeof(T));
		yield return abr;

		Object obj = abr.asset;

		AssetManager.Instance.pushCache(absolutepath, obj);
		callback(obj as T);

		UnloadAssetBundle(assetBundleList);
	}

	private void LoadManifest()
	{
		if(manifest == null)
		{
			string path = manifestPaht;
			Debug.Log("[AssetBundle]加载manifest path: " + path);

			AssetBundle manifestAB = AssetBundle.LoadFromFile(path);
			manifest = manifestAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
			manifestAB.Unload(false);
		}
	}

	private void UnloadAssetBundle(List<AssetBundle> list)
	{
		for(int i= 0; i<list.Count; i++)
		{
			list[i].Unload(false);
		}
		list.Clear();
	}
}
