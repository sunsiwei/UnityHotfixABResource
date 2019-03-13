using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Launcher : MonoBehaviour {

	public Text Content;
    public Image Img_1;
    public Button Btn_1;

    public Image Img_2;
    public Button Btn_2;


    // Use this for initialization
    void Start () {
        AssetManager.Instance.InitMode(GameConfigs.LoadAssetMode);

        Content.text = "资源管理器加载模式:" + GameConfigs.LoadAssetMode;

        Btn_1.onClick.AddListener(onClickedBtn1);
        Btn_2.onClick.AddListener(onClickedBtn2);


        // UpdateVersionManager.Instance.CheckVersion((bool needUpdate) =>{
        //   if(needUpdate)
        //   {
        //     Content.text = "需要更新";
        //   }
        //   else
        //   {
        //     Content.text = "开始检查资源文件";
        //     UpdateAssetManager.Instance.CheckAsset(()=>{
        //       Content.text = "资源文件下载完成，进入游戏";
              
        //     });
        //   }
        // });


    }

    void onClickedBtn1() {
        

      // GameObject obj = AssetManager.Instance.LoadAsset<GameObject>(GameConfigs.GetCubePath("Cube"));
      // GameObject.Instantiate(obj);

      // GameObject obj = AssetManager.Instance.LoadAsset<GameObject>(GameConfigs.GetCubePath("Cube"));
      // GameObject.Instantiate(obj);
      HotfixManager.Instance.Check((b) => {
        if(b)
        {
         
        }else
        {
          Debug.Log("更新安装包");
        }
      });
    }

    void onClickedBtn2() {

    //   AssetManager.Instance.LoadAssetAsync<GameObject>(GameConfigs.GetCubePath("Cube"), (GameObject obj) =>{
    //     GameObject.Instantiate(obj);
		// });
     GameObject obj = AssetManager.Instance.LoadAsset<GameObject>(GameConfigs.GetCubePath("Cube"));
      GameObject.Instantiate(obj);
    }
}
