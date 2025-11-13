using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [SerializeField]
    private GameObject transitionsContainer;

    [SerializeField]
    private Slider progressBar;


    private SceneTransition[] transitions;

    private Canvas levelCanvas;

    private bool isLoading = false; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        transitions = transitionsContainer.GetComponentsInChildren<SceneTransition>();
        levelCanvas = GetComponent<Canvas>();

     
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int idx = scene.buildIndex;
        if (idx == 0 || idx == 1) // 3DMainMenu 或 3DLevelSelector
        {
            ForceOverlayCanvas();
        }
        else
        {
            AttachToMainCamera();
        }
    }

    private void AttachToMainCamera()
    {
        Camera cam = Camera.main;
        if (cam != null && levelCanvas != null)
        {
            levelCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            levelCanvas.worldCamera = cam;

            Debug.Log("LevelManager Canvas now follows: " + cam.name);
        }
        else
        {
            Debug.LogWarning(" Cannot find Main Camera in this scene!");
        }
    }

    private void ForceOverlayCanvas()
    {
        if (levelCanvas != null)
        {
            levelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            levelCanvas.sortingOrder = 600;  
            levelCanvas.worldCamera = null;

            Debug.Log("Canvas forced to ScreenSpaceOverlay in 3D Menu Scene");
        }
    }

    public void LoadScene(int sceneId, string transitionName, SoundType musicType = SoundType.MainMenuMusic)
    {
        if (isLoading)
        {
            Debug.LogWarning(" scene is loading , ignore this request");
            return;
        }
        StartCoroutine(LoadSceneAsync(sceneId, transitionName, musicType));
    }


    private IEnumerator LoadSceneAsync(int id, string transitionName, SoundType musicType)
    {
        isLoading = true;
        try 
        {
            SceneTransition transition = transitions.First(t => t.name == transitionName);
            transition.gameObject.SetActive(true);
            AsyncOperation scene = SceneManager.LoadSceneAsync(id, LoadSceneMode.Single);
            scene.allowSceneActivation = false;
          
            StartCoroutine(transition.AnimatorTransitionIn());

            progressBar.gameObject.SetActive(true);

            do
            {
                progressBar.value = scene.progress;
                yield return null;
            } while (scene.progress < 0.9f);

            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;

            scene.allowSceneActivation = true;
            progressBar.gameObject.SetActive(false);

            if (musicType != SoundType.MainMenuMusic)
            {
                SoundManager.Instance.PlaySound(musicType, null, 0.25f);
            }

            yield return transition.AnimatorTransitionOut();

            transition.gameObject.SetActive(false);
        }
        finally
        {
            isLoading = false; // <-- 无论是否崩溃，都释放锁
        }
    }
}

