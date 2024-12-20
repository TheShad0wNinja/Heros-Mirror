using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PuzzleDoor : MonoBehaviour
{
    [SerializeField] Sprite openDoor;
    SpriteRenderer spriteRenderer;
    public bool canOpenKey = false;
    public bool canOpenPuzzle = false;

    [SerializeField] private GameObject textBubblePrefab;
    private GameObject textBubbleInstance;
    Audio_Manager audioManager;
    AudioClip audioClip;

    void Start()
    {
        audioManager = FindObjectOfType<Audio_Manager>();
        audioClip = Resources.Load<AudioClip>("DoorOpen");
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UnlockDoorKey() 
    {
        canOpenKey = true;
    }
    public void UnlockDoorPuzzle ()
    {
        canOpenPuzzle = true;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player") ) 
        {
            if (canOpenKey && canOpenPuzzle)
            {
                audioManager.PlaySFX(audioClip);
                spriteRenderer.sprite = openDoor;
                GetComponent<BoxCollider2D>().enabled = false;
            }
            else if (canOpenKey && !canOpenPuzzle)
            {
                InstantiateBubble("Look for a rubix cube buddy");
            }
            else if (canOpenPuzzle && !canOpenKey)
            {
                InstantiateBubble("I have 2 locks, sorry");
            }
            else 
            {
                InstantiateBubble("Hey");
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player")) 
        {
            Destroy(textBubbleInstance);
        }
    }
    void InstantiateBubble(string input) 
    {
        textBubbleInstance = Instantiate(textBubblePrefab, transform.position + Vector3.up, Quaternion.identity);
        textBubbleInstance.GetComponentInChildren<TextMeshPro>().text = input;
        textBubbleInstance.transform.SetParent(transform);
    }
}
