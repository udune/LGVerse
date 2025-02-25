using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingNoteController : MonoBehaviour
{
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image noteImg;
    [SerializeField] private UIKaraoke_Lyrics lyrics;
    
    [Range(0, 1)]
    public float value;
    
    private RectTransform rect;
    private float lifeTime;
    private bool isActive;

    public RectTransform Rect
    {
        get => rect;
        set => rect = value;
    }

    private void Start()
    {
        noteImg.color = gradient.Evaluate(value);
    }

    public void SetNoteData(float endTime)
    {
        lifeTime = endTime;
        isActive = true;
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        LifeTimeCheck();
    }
    
    private void LifeTimeCheck()
    {
        if (isActive != true || lifeTime + 2.0f > lyrics.time)
        {
            return;
        }
        
        isActive = false;
        Destroy(gameObject);
    }
}
