using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingLyricsText : MonoBehaviour
{
    [SerializeField] private SingLyricsManager lyricsManager;
    [SerializeField] private UIKaraoke_Lyrics lyrics;
    [SerializeField] private LyricsType lyricsType;
    [SerializeField] private float start;
    [SerializeField] private float end;
    [SerializeField] private Text lyricsText;
    [SerializeField] private Text maskText;

    private bool isChangeOn;
    private float timer;
    private RectTransform mask;
    private bool isStopLyrics;

    public LyricsType LyricsType
    {
        set => lyricsType = value;
    }

    public float Start
    {
        set => start = value;
    }

    public float End
    {
        set => end = value;
    }

    public Text LyricsText => lyricsText;
    public Text MaskText => maskText;

    private void OnEnable()
    {
        isChangeOn = false;
        isStopLyrics = false;
        mask = transform.GetChild(0).GetComponent<RectTransform>();
        mask.sizeDelta = new Vector2(0, mask.sizeDelta.y);
        maskText.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }
    
    private void Update()
    {
        if (lyricsType == LyricsType.Space)
        {
            return;
        }

        if (lyrics.time >= start && isChangeOn == false)
        {
            isChangeOn = true;
            timer = lyrics.time - start;
            StartCoroutine(ColorChange());
        }
    }
    
    private IEnumerator CoUpdate()
    {
        while (!isChangeOn)
        {
            if (lyricsType == LyricsType.Space)
            {
                yield return null;
            }

            if (lyrics.time >= start && isChangeOn == false)
            {
                isChangeOn = true;
                timer = lyrics.time - start;
                StartCoroutine(ColorChange());
            }
        }
    }

    private IEnumerator ColorChange()
    {
        float rangeTime = end - start;

        while (true)
        {
            if (isStopLyrics)
            {
                break;
            }

            if (timer < rangeTime)
            {
                timer += Time.deltaTime;
            }

            if (timer > rangeTime)
            {
                timer = rangeTime;
            }
            
            float elapseTime = timer / rangeTime;
            
            mask.sizeDelta = new Vector2(elapseTime * lyricsText.rectTransform.sizeDelta.x, mask.sizeDelta.y);
            
            if (elapseTime.Equals(1.0f))
            {
                lyricsManager.WordCountCheck(LyricsType.Text);
                break;
            }
            
            yield return null;
        }
    }

    public void StopRoutine()
    {
        isStopLyrics = true;
    }
}
