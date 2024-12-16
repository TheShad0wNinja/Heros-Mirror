using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollapsingTrap : MonoBehaviour
{
    [SerializeField] private float collapseDelay = 2f;
    private bool isTriggered = false;
    private GameObject playerOnFloor = null;
    SpriteRenderer spriteRenderer;
    Color color;
    [SerializeField] string nextSceneName;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        color = spriteRenderer.color;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            playerOnFloor = other.gameObject;
            StartCoroutine(CollapseFloor(other.gameObject));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == playerOnFloor)
        {
            isTriggered = false;
            playerOnFloor = null;
            StopAllCoroutines();
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }

    private IEnumerator CollapseFloor(GameObject player)
    {
        if (spriteRenderer != null)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / collapseDelay)
            {
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
                yield return null;
            }
            Scene originalScene = SceneManager.GetActiveScene();
            if (SceneManager.GetActiveScene().name != nextSceneName)
            {
                player.GetComponent<PlayerMovementController>().enabled = false;

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
                while (!loadOperation.isDone)
                {
                    Debug.Log($"Scene Loading Progress: {loadOperation.progress}");
                    yield return null;
                }
                Scene sceneToSet = SceneManager.GetSceneByName(nextSceneName);
                if (sceneToSet.isLoaded)
                {
                    SceneManager.SetActiveScene(sceneToSet);
                }
            }
            else if (SceneManager.GetActiveScene().name == nextSceneName)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(nextSceneName);
                while (!unloadOperation.isDone)
                {
                    yield return null;
                }
                SceneManager.SetActiveScene(originalScene);
                FindObjectOfType<PlayerMovementController>().enabled = true;
            }
            isTriggered = false;
        }
    }

// private IEnumerator CollapseFloor(GameObject player)
// {
//     Debug.Log("Starting CollapseFloor Coroutine");

//     // Fade out the floor's sprite
//     if (spriteRenderer != null)
//     {
//         for (float t = 0; t < 1f; t += Time.deltaTime / collapseDelay)
//         {
//             color.a = Mathf.Lerp(1f, 0f, t);
//             spriteRenderer.color = color;
//             yield return null; // Ensures coroutine execution
//         }
//     }

//     Debug.Log("Current Scene: " + SceneManager.GetActiveScene().name);
//     Debug.Log("Next Scene: " + nextSceneName);

//     // Deactivate player before loading the new scene
//     Debug.Log("Deactivating player...");
//     player.GetComponent<PlayerMovementController>().enabled = false;

//     // Load the next scene
//     Debug.Log($"Loading scene: {nextSceneName}");
//     AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);

//     while (!loadOperation.isDone)
//     {
//         Debug.Log($"Scene Loading Progress: {loadOperation.progress}");
//         yield return null; // Waits until the scene is fully loaded
//     }

//     Debug.Log($"Scene '{nextSceneName}' loaded.");

//     // Verify the scene is loaded and valid
//     Scene loadedScene = SceneManager.GetSceneByName(nextSceneName);
//     Debug.Log($"Scene '{nextSceneName}' IsValid: {loadedScene.IsValid()}, IsLoaded: {loadedScene.isLoaded}");

//     if (loadedScene.IsValid() && loadedScene.isLoaded)
//     {
//         Debug.Log($"Setting active scene to: {nextSceneName}");
//         SceneManager.SetActiveScene(loadedScene);
//         Debug.Log("Active Scene Set: " + SceneManager.GetActiveScene().name);
//     }
//     else
//     {
//         Debug.LogError($"Failed to set active scene: {nextSceneName}");
//     }

//     isTriggered = false;
// }




}