using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{

	private static T m_Instance = null;

	public static T Instance {
		get {
			if (m_Instance == null) {
				GameObject obj = GameObject.Find(typeof(T).Name);
				if (obj == null) {
					obj = new GameObject(typeof(T).Name);
					m_Instance = obj.AddComponent<T>();
				} else {
					m_Instance = obj.GetComponent<T>();
					if (m_Instance == null) {
						m_Instance = obj.AddComponent<T>();
					}
				}
				DontDestroyOnLoad(m_Instance.gameObject);
			}
			return m_Instance;
		}
	}

	private void Awake() {
		if (m_Instance == null) {
			m_Instance = this as T;
			DontDestroyOnLoad(m_Instance.gameObject);
		}
	}

	public virtual void Init() {

	}

	private void OnApplicationQuit() {
		m_Instance = null;
	}
}