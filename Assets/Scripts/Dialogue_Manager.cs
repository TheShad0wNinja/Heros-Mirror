using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Dialogue_Manager : MonoBehaviour
{
    public static Dialogue_Manager Instance;

    public GameObject dialoguePanelPrefab;
    private GameObject dialoguePanel;
    private Image unitImage;
    private TextMeshProUGUI unitNameText;
    private TextMeshProUGUI dialogueText;
    private Button continueButton;
    private Button skipButton;
    private DialogueEntry[] currentDialogues;
    private int currentIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    public float typingSpeed = 0.05f;
    public UnityAction<GameObject> onDialogueEnd;
    private GameObject currentUser;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        FindOrCreateDialoguePanel();

        continueButton.onClick.AddListener(HandleContinue);
        skipButton.onClick.AddListener(HandleSkip);
    }

    private void FindOrCreateDialoguePanel()
    {
        dialoguePanel = GameObject.FindGameObjectWithTag("DialoguePanel");

        if (dialoguePanel == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null) throw new System.Exception("Canvas not found in scene.");

            dialoguePanel = Instantiate(dialoguePanelPrefab, canvas.transform);
        }
        unitImage = FindChildRecursive(dialoguePanel.transform, "Unit Image")?.GetComponent<Image>();
        unitNameText = FindChildRecursive(dialoguePanel.transform, "Unit Name Text")?.GetComponent<TextMeshProUGUI>();
        dialogueText = FindChildRecursive(dialoguePanel.transform, "Dialogue Text")?.GetComponent<TextMeshProUGUI>();
        continueButton = FindChildRecursive(dialoguePanel.transform, "Continue Button")?.GetComponent<Button>();
        skipButton = FindChildRecursive(dialoguePanel.transform, "Skip Button")?.GetComponent<Button>();

        dialoguePanel.SetActive(false);
    }

    public void InitializeConversation(Conversation conversation, GameObject user)
    {
        currentUser = user;
        if (conversation == null || conversation.dialogueEntries.Length == 0) return;
        currentDialogues = conversation.dialogueEntries;
        currentIndex = 0;
        Time.timeScale = 0f;
        dialoguePanel.SetActive(true);
        ShowDialogue();
    }

    private void ShowDialogue()
    {
        if (currentIndex >= currentDialogues.Length)
        {
            EndDialogue();
            return;
        }

        DialogueEntry dialogue = currentDialogues[currentIndex];
        unitImage.sprite = dialogue.unitImage;
        unitNameText.text = dialogue.unitName;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeDialogue(dialogue.dialogueText));
    }

    private IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private void HandleContinue()
    {
        Debug.Log("isTyping :" + isTyping);
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentDialogues[currentIndex].dialogueText;
            isTyping = false;
        }
        else
        {
            currentIndex++;
            ShowDialogue();
        }
    }

    private void HandleSkip()
    {
        EndDialogue();
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        onDialogueEnd?.Invoke(currentUser);
        //currentUser = null;
        Time.timeScale = 1f;
    }
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
