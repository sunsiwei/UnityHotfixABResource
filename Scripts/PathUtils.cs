using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PathUtils : MonoBehaviour {

	//修正路径中的正反斜杠
	public static string NormalizePath(string path)
	{
		return path.Replace(@"\", "/");
	}

	//将绝对路径转换成相对路径
	public static string GetRelativePath(string fullpath, string root)
	{
		string path = NormalizePath(fullpath);
		path = ReplaceFirst(path, root, "Assets");
		return path;
	}

	//替换掉第一个遇到的指定支付窜
	public static string ReplaceFirst(string str, string oldValue, string newValue)
	{
		int i = str.IndexOf(oldValue);
		str = str.Remove(i, oldValue.Length);
		str = str.Insert(i, newValue);
		return str;
	}

	//根据一个绝对路径获得资源的assetbundle name
	public static string GetAssetBundleNameWithPath(string path, string root)
	{
		string str = NormalizePath(path);
		str = ReplaceFirst(str, root + "/", "");
		return str;
	}

	//获取文件夹下的所有文件，包括子文件，不包括.meta文件
	public static FileInfo[] GetFiles(string path)
	{
		DirectoryInfo folder = new DirectoryInfo(path);
		DirectoryInfo[] subFolders = folder.GetDirectories();
		List<FileInfo> filesList = new List<FileInfo>();
		
		foreach (DirectoryInfo subFolder in subFolders)
        {
            filesList.AddRange(GetFiles(subFolder.FullName));
        }

        FileInfo[] files = folder.GetFiles();
        foreach (FileInfo file in files)
        {
            if (file.Extension != ".meta")
            {
                filesList.Add(file);
            }
        }
        return filesList.ToArray();
	}

	public static void CreateFolder(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            dir.Create();
        }
    }

	// 获取文件夹的所有文件路径，包括子文件夹 不包含.meta文件
	public static string[] GetFilesPath(string path)
    {
        DirectoryInfo folder = new DirectoryInfo(path);
        DirectoryInfo[] subFolders = folder.GetDirectories();
        List<string> filesList = new List<string>();

        foreach (DirectoryInfo subFolder in subFolders)
        {
            filesList.AddRange(GetFilesPath(subFolder.FullName));
        }

        FileInfo[] files = folder.GetFiles();
        foreach (FileInfo file in files)
        {
            if (file.Extension != ".meta")
            {
                filesList.Add(NormalizePath(file.FullName));
            }

        }
        return filesList.ToArray();
    }
	//创建文件目录前的文件夹，保证创建文件的时候不会出现文件夹不存在的情况
	public static void CreateFolderByFilePath(string path)
    {
        FileInfo fi = new FileInfo(path);
        DirectoryInfo dir = fi.Directory;
        if (!dir.Exists)
        {
            dir.Create();
        }
    }
}
