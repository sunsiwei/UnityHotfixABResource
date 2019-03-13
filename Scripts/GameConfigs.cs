using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfigs {

	// 加载模式
	public static AssetLoadMode LoadAssetMode = AssetLoadMode.AssetBundler;

#if UNITY_ANDROID
	static string curPlatformName = "android";
#elif UNITY_IPHONE
	static string curPlatformName = "iphone";
#else
    static string curPlatformName = "win";
#endif

    //游戏资源文件路径
    public static string GameResPath = Application.dataPath + "/GameRes";
    //打ab包资源的输出文件夹(导出到streamingaseet文件夹下)
    public static string GameResExportPath = Application.streamingAssetsPath + "/" + curPlatformName;


	#region game res path

	//test
	public static string GetCubePath(string name)
	{
		if(LoadAssetMode != AssetLoadMode.Editor)
		{
			return LocalABRootPath + "/" + name;
		}
		else
		{
			return GameResPath + "/" + name + ".prefab";
		}
	}

    // todo:  扩展...
    
    #endregion

    //本地ab包根路径(该文件夹可读可写,从资源服务器更新的数据也放在这里)
    public static string LocalABRootPath = Application.persistentDataPath + "/" + curPlatformName;

}
