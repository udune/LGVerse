using System.Collections;
using System.Collections.Generic;
using SingSDKConstants;
using UnityEngine;
using UnityEngine.UI;

public class SingNoteManager : MonoBehaviour
{
    [SerializeField] private UIKaraoke_Lyrics lyrics;
    [SerializeField] private Note[] noteDatas;
    [SerializeField] private GameObject passNotePrefab;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private RectTransform afterFieldRect;
    [SerializeField] private RectTransform beforeFieldRect;
    [SerializeField] private RectTransform afterMoveFieldRect;
    [SerializeField] private RectTransform beforeMoveFieldRect;
    [SerializeField] private Image singingPivot;
    [SerializeField] private int maleMin = 50;
    [SerializeField] private int rapKey = 108;
    [SerializeField] private float noteSpeed;
    [SerializeField] bool isNoteStop;
    
    [SerializeField] List<SingNoteController> notePullItem = new List<SingNoteController>();
    [SerializeField] List<SingNoteController> notePlayItem = new List<SingNoteController>();

    public const float widthMaxSec = 5.0f;

    public int MaleMin
    {
        set => maleMin = value;
    }

    public int RapKey
    {
        set => rapKey = value;
    }
    
    private float voiceKeyPos;
    private int noteStepCount;
    private float keyPointTimer;
    private float keyPointTime;
    private bool isPlay;

    private void Awake()
    {
        singingPivot.rectTransform.anchoredPosition = new Vector2(singingPivot.rectTransform.anchoredPosition.x, GetNotePosY((rapKey + 10) / 1.5f));
    }

    public void SetNoteData(Note[] data, float endTime)
    {
        if (notePullItem.Count > 0)
        {
            NotePullItemInit();
        }

        noteDatas = null;
        keyPointTime = 1.0f;
        noteDatas = data;
        noteStepCount = 0;
        
        // 필드의 가로 길이 / 5.0f = 노트의 스피드
        noteSpeed = beforeFieldRect.sizeDelta.x / widthMaxSec;
        
        // endTime X noteSpeed = 노트의 길이
        float moveFieldSizeX = endTime * noteSpeed;
        
        // 노트 사이즈 (endTime X noteSpeed , 노트 사이즈.y), 앵커포스 = 0, 0
        beforeMoveFieldRect.sizeDelta = new Vector2(moveFieldSizeX, beforeMoveFieldRect.sizeDelta.y);
        afterMoveFieldRect.sizeDelta = new Vector2(moveFieldSizeX, afterMoveFieldRect.sizeDelta.y);
        beforeMoveFieldRect.anchoredPosition = Vector2.zero;
        afterMoveFieldRect.anchoredPosition = Vector2.zero;
        /*
        int noteMaxCount = 30;
        if (notePullItem.Count < 1)
        {
            for (int i = 0; i < noteMaxCount; i++)
            {
                SingNoteController nc = GetNoteCreate();
                notePullItem.Add(nc);
                nc.gameObject.SetActive(false);
            }
        }*/
    }
    
    private void NotePullItemInit()
    {
        int count = notePullItem.Count;
        for (int i = 0; i < count; i++)
        {
            SingNoteController nc = notePullItem[0];
            notePullItem.Remove(nc);
            Destroy(nc);
        }

        notePullItem.Clear();
    }

    private SingNoteController GetBeforeNoteCreate()
    {
        GameObject go = Instantiate(notePrefab, beforeMoveFieldRect);
        SingNoteController noteController = go.GetComponent<SingNoteController>();
        noteController.Rect = go.GetComponent<RectTransform>();
        return noteController;
    }
    
    private SingNoteController GetAfterNoteCreate()
    {
        GameObject go = Instantiate(passNotePrefab, afterMoveFieldRect);
        SingNoteController noteController = go.GetComponent<SingNoteController>();
        noteController.Rect = go.GetComponent<RectTransform>();
        return noteController;
    }

    public void SetNotePlay(bool value)
    {
        isPlay = value;

        if (isPlay == false)
        {
            return;
        }

        if (noteDatas == null)
        {
            Debug.Log("##### Unity - noteDatas count : Null");
        }
        else
        {
            Debug.Log("##### Unity - noteDatas count :" + noteDatas.Length);
        }

        for (int i = 0; i < notePullItem.Count; i++)
        {
            float start = GetReturnSec(noteDatas[i].start);
            float end = GetReturnSec(noteDatas[i].end);
            float timeGap = end - start;

            if (timeGap <= widthMaxSec)
            {
                float posX = start * noteSpeed;
                Vector2 pos = new Vector2(posX, GetNotePosY(noteDatas[i].key));
                float noteSizeX = timeGap * noteSpeed;
                float lifeTime = end + widthMaxSec * 0.5f;
                SetNote(pos, noteSizeX, lifeTime);
                noteStepCount++;
            }
        }
    }

    private void SetNote(Vector2 pos, float sizeX, float lifeTime)
    {
        SingNoteController beforeNoteController = GetBeforeNoteCreate();
        SingNoteController afterNoteController = GetAfterNoteCreate();
        beforeNoteController.Rect.anchoredPosition = pos;
        afterNoteController.Rect.anchoredPosition = pos;
        beforeNoteController.Rect.sizeDelta = new Vector2(sizeX, beforeNoteController.Rect.sizeDelta.y);
        afterNoteController.Rect.sizeDelta = new Vector2(sizeX, afterNoteController.Rect.sizeDelta.y);
        beforeNoteController.SetNoteData(lifeTime);
        afterNoteController.SetNoteData(lifeTime);
    }

    float GetNotePosY(float key)
    {
        float min = maleMin - 10;
        float max = rapKey + 10;
        float range = max - min;
        float ratioY = (key - min) / range;
        float posY = (beforeFieldRect.sizeDelta.y) * ratioY;
        return posY;
    }

    public void SetNotePullAdd(SingNoteController nc)
    {
        notePlayItem.Remove(nc);
        notePullItem.Add(nc);
        //Destroy(nc.gameObject);
        nc.gameObject.SetActive(false);
    }

    /* 플레이 */
    public void NotePlayCheck()
    {
        if (noteStepCount >= noteDatas.Length - 1) return;
        float start = GetReturnSec(noteDatas[noteStepCount].start);
        float end = GetReturnSec(noteDatas[noteStepCount].end);
        float timeGap = end - start;
        if (start <= lyrics.time + 7.0f)
        {
            float posX = start * noteSpeed;
            Vector2 pos = new Vector2(posX, GetNotePosY(noteDatas[noteStepCount].key));
            float noteSizeX = timeGap * noteSpeed;
            float lifeTime = end + widthMaxSec * 0.5f;
            SetNote(pos, noteSizeX, lifeTime);
            noteStepCount++;
            //Debug.Log(noteStepCount);
            //if (noteStepCount == noteDatas.Length - 1) PopupNoteLyric.instance.NoteAllPlay();
        }
    }
    
    private float GetReturnSec(float time)
    {
        return time * 0.001f;
    }

    public void NoteFieldMove()
    {
        float posX = -lyrics.time * noteSpeed;
        beforeMoveFieldRect.anchoredPosition = new Vector2(posX, beforeMoveFieldRect.anchoredPosition.y);
        afterMoveFieldRect.anchoredPosition = new Vector2(posX, afterMoveFieldRect.anchoredPosition.y);
        //Debug.Log("Test Debugging moveField pos X : " + posX);
    }

    public void NoteFieldDestroy()
    {
        for (int i = 0; i < beforeMoveFieldRect.gameObject.transform.childCount; i++)
        {
            Destroy(beforeMoveFieldRect.gameObject.transform.GetChild(i).gameObject);
        }
        
        for (int i = 0; i < afterMoveFieldRect.gameObject.transform.childCount; i++)
        {
            Destroy(afterMoveFieldRect.gameObject.transform.GetChild(i).gameObject);
        }
    }

    public void Reset()
    {
        isPlay = false;

        int count = notePullItem.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(notePullItem[0]);
        }

        count = notePlayItem.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(notePlayItem[0]);
        }
    }

    public void SetSingingPivot(float key, float noteKey, bool micOn, float mrKey)
    {
        if (micOn)
        {
            keyPointTimer = 0.0f;
            //key += 24.0f; //오리지날
            key += 12.0f;
            voiceKeyPos = key + mrKey;
            if (voiceKeyPos < maleMin - 10)
            {
                voiceKeyPos = maleMin - 10;
            }

            if (voiceKeyPos > rapKey + 10)
            {
                voiceKeyPos = rapKey + 10;
            }
            singingPivot.rectTransform.anchoredPosition = new Vector2(singingPivot.rectTransform.anchoredPosition.x, GetNotePosY(voiceKeyPos));
        }

        bool isHit = key <= noteKey + 1.0f && key >= noteKey - 1.0f;
    }
}