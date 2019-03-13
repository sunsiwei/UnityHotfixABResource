using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Object = UnityEngine.Object;

public enum AssetLoadMode
{
	Editor,//编辑器中运行，无需打ab包
	AssetBundler,//ab包模式
}

public class CacheDataInfo
{
	// 放入缓冲区时间
	public float StartTick;
	// 缓存对象
	public Object CacheObj;
	public string CacheName;
	public CacheDataInfo(string name, Object obj)
	{
		CacheName = name;
		CacheObj = obj;
	}

	public void UpdateTick()
	{
		StartTick = Time.realtimeSinceStartup;
	}
}

public class AssetManager : MonoSingleton<AssetManager> {

	//加载模式
	public AssetLoadMode LoadMode = AssetLoadMode.Editor;
	//定时清理缓存间隔
	public float ClearCacheDuration;
	//缓存数据驻留时间，过期清理掉
	public float CacheDataStayTime;

	private IAssetLoader editorLoader;
	private IAssetLoader abLoader;

	
	// key是绝对路径
	private Dictionary<string, CacheDataInfo> cacheDataDic = new Dictionary<string, CacheDataInfo>();

	
	public void InitMode(AssetLoadMode mode, float duration =10f, float cacheStayTime = 9f)
	{
		Debug.LogFormat("[AssetManager]初始化 当前加载模式:{0} 定时清理缓冲间隔:{1}s", mode, duration);
		LoadMode = mode;
		ClearCacheDuration = duration;
		CacheDataStayTime = cacheStayTime;
		editorLoader = new EditorAssetLoader();
		abLoader = new AssetBundleLoader();
	}

	public T LoadAsset<T>(string path) where T:Object
	{
		CacheDataInfo info = queryCache(path);
		if(info != null)
		{
			info.UpdateTick();
			return info.CacheObj as T;
		}
		else
		{
			switch (LoadMode) 
			{
				case AssetLoadMode.Editor:
					return editorLoader.LoadAsset<T>(path);
				case AssetLoadMode.AssetBundler:
					return abLoader.LoadAsset<T>(path);
			}
			return null;
		}
	}
	public void LoadAssetAsync<T>(string path, UnityAction<T> onLoadComplate) where T : Object {
		CacheDataInfo info = queryCache(path);
		if (info != null) {
			info.UpdateTick();
			if (onLoadComplate != null) {
				onLoadComplate(info.CacheObj as T);
			}
		} else {
			switch (LoadMode) {
				case AssetLoadMode.Editor:
					StartCoroutine(editorLoader.LoadAssetAsync<T>(path, onLoadComplate));
					break;
				case AssetLoadMode.AssetBundler:
					StartCoroutine(abLoader.LoadAssetAsync<T>(path, onLoadComplate));
					break;
			}

		}
	}


	private CacheDataInfo queryCache(string path)
	{
		if(cacheDataDic.ContainsKey(path))
		{
			return cacheDataDic[path];
		}
		return null;
	}

	public void pushCache(string path, Object obj) {
		Debug.Log("[AssetManager]加入缓存:" + path);

		lock (cacheDataDic) {
			if (cacheDataDic.ContainsKey(path)) {
				cacheDataDic[path].UpdateTick();
			} else {
				CacheDataInfo info = new CacheDataInfo(path, obj);
				cacheDataDic.Add(path, info);
				info.UpdateTick();
			}
		}
	}
	//清空缓冲区
	public void RemoveCache() {
		cacheDataDic.Clear();
	}
	//清理缓冲区
	private void updateCache() {
		// Debug.Log("[AssetManager]清理缓存");
		foreach (var iter in cacheDataDic.ToList()) {
			if (iter.Value.StartTick + CacheDataStayTime >= Time.realtimeSinceStartup) {
				Debug.Log("过期清理:" + iter.Value.CacheName);
				cacheDataDic.Remove(iter.Key);
			}
		}
	}

	private float cacheTimeTemp;
	private void Update() {
		if (ClearCacheDuration < 0) return;

		cacheTimeTemp += Time.deltaTime;

		if (cacheTimeTemp >= ClearCacheDuration) {
			updateCache();
			cacheTimeTemp -= ClearCacheDuration;
		}
	}
}
