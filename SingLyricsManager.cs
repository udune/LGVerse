using System;
using System.Collections;
using System.Collections.Generic;
using SingSDKConstants;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LyricsData
{
    public string lyric;
    public float start;
    public float end;
}

public enum LyricsType
{
    Text,
    Space
}

public enum LineType
{
    None,
    First,
    Second,
    Third
}

[Serializable]
public struct LyricsItemData
{
    public LyricsType type;
    public string lyric;
    public float start;
    public float end;
}

[Serializable]
public class LyricsGroupData
{
    public bool isComplete;
    public int wordMaxCount;
    public float minTime;
    public float maxTime;
    public LineType lineType;

    public List<LyricsItemData> lyricsDataList;

    public LyricsGroupData()
    {
        lineType = LineType.None;
        isComplete = false;
        wordMaxCount = 0;
        minTime = 0.0f;
        maxTime = 0.0f;
        lyricsDataList = new List<LyricsItemData>();
    }
}

public class SingLyricsManager : MonoBehaviour
{
    [SerializeField] private UIKaraoke_Lyrics lyrics;
    [SerializeField] private GameObject lineTextItemPrefab;
    [SerializeField] private GameObject linePassTextItemPrefab;
    [SerializeField] private SingLyricsText[] textItems;
    [SerializeField] private SingLyricsText[] passTextItems;
    [SerializeField] private RectTransform buffer;
    [SerializeField] private RectTransform passBuffer;
    [SerializeField] private RectTransform passParent;
    [SerializeField] private Transform lyricParent;
    [SerializeField] private RectTransform[] textArea;
    [SerializeField] private Text[] lineTexts;
    [SerializeField] private Text[] lineTextFlows;
    [SerializeField] private int lineStepCount;
    [SerializeField] private int playWordStepCount;
    [SerializeField] private int playLineStepCount;
    [SerializeField] private int playCompleteLineCount;
    [SerializeField] private float lineHalfTime;
    [SerializeField] private bool isFirstLineSet;
    [SerializeField] private LyricsData[] lyricsDatas;
    [SerializeField] private List<LyricsGroupData> lyricsGroupList = new List<LyricsGroupData>();
    [SerializeField] private List<SingLyricsText> firstAreaItemList = new List<SingLyricsText>();
    [SerializeField] private List<SingLyricsText> secondAreaItemList = new List<SingLyricsText>();
    //[SerializeField] private List<SingLyricsText> thirdAreaItemList = new List<SingLyricsText>();

    private int language;
    private int textItemMaxCount;
    private int completeCount;
    private bool isStart;
    
    public List<LyricsGroupData> LyricsGroupList => lyricsGroupList;

    public int Language
    {
        set => language = value;
    }

    public void SetLyricsData(Section[] data)
    {
        SetLyricsInit(data);
    }

    public void SetLyricsData(Lyc data)
    {
        SetLyricsInit(data);
    }

    private void SetLyricsInit(Lyc data)
    {
        isStart = false;
        LyricsTextPulling();
        SetLyricsLineList(data);
        SectionFirstInit();
    }

    private void SetLyricsInit(Section[] data)
    {
        // isStart = false
        isStart = false;
        // 가사 오브젝트 풀링 생성
        LyricsTextPulling();
        // lyricsGroupList 생성
        SetLyricsLineList(data);
        // 데이터 초기화
        SectionFirstInit();
    }
    
    private void LyricsTextPulling()
    {
        Vector2 originPos = Vector2.zero;
        
        // 배열 생성
        textItemMaxCount = 80;
        textItems = new SingLyricsText[textItemMaxCount];
        passTextItems = new SingLyricsText[textItemMaxCount];

        // 80 개 텍스트 아이템 버퍼, 패스버퍼 오브젝트 하위에 생성.
        for (int i = 0; i < textItemMaxCount; i++)
        {
            GameObject textGo = Instantiate(lineTextItemPrefab, buffer);
            GameObject passTextGo = Instantiate(linePassTextItemPrefab, passBuffer);
            textGo.transform.localPosition = Vector2.zero;
            passTextGo.transform.localPosition = Vector2.zero;
            textGo.transform.localPosition = originPos + Vector2.right * (50 * i);
            passTextGo.transform.localPosition = originPos + Vector2.right * (50 * i);

            // textItems 배열에 저장.
            // passTextItems 배열에 저장.
            textItems[i] = textGo.GetComponent<SingLyricsText>();
            passTextItems[i] = passTextGo.GetComponent<SingLyricsText>();
            textItems[i].gameObject.SetActive(false);
            passTextItems[i].gameObject.SetActive(false);
        }
    }

    private void SetLyricsLineList(Section[] data)
    {
        // lyricsGroupList 초기화
        lyricsGroupList.Clear();
        
        // Section -> Line. lyricsGroupList 생성.
        for (int i = 0; i < data.Length; i++)
        {
            for (int j = 0; j < data[i].line.Length; j++)
            {
                lyricsGroupList.Add(GetLineWord(data, i, j));
            }
        }
    }
    
    private void SetLyricsLineList(Lyc data)
    {
        lyricsGroupList.Clear();
        for (int i = 0; i < data.section.Length; i++)
        {
            for (int j = 0; j < data.section[i].line.Length; j++)
            {
                lyricsGroupList.Add(GetLineWord(data, i, j));
            }
        }
    }
    
    private LyricsGroupData GetLineWord(Section[] data, int sectionIndex, int lineIndex)
    {
        Line lineData = data[sectionIndex].line[lineIndex];

        int count = 0;
        bool isSpace = false;

        LyricsGroupData groupData = new LyricsGroupData
        {
            // 가사 한 줄의 시작 시간
            minTime = GetReturnSec(lineData.start),
            // 가사 한 줄의 끝 시간
            maxTime = GetReturnSec(lineData.end),
            // 가사 한 줄의 텍스트 갯수
            wordMaxCount = lineData.word.Length
        };

        foreach (var wordData in lineData.word)
        {
            LyricsItemData lyricsItemData = GetLyricsItemData(wordData);
            int space = lyricsItemData.lyric.LastIndexOf(" ", lyricsItemData.lyric.Length - 1, StringComparison.Ordinal);

            if (space > 0)
            {
                lyricsItemData.lyric = lyricsItemData.lyric.TrimEnd();
                isSpace = true;
            }

            groupData.lyricsDataList.Add(lyricsItemData);
            count++;
            if (isSpace)
            {
                if (count < lineData.word.Length)
                {
                    lyricsItemData = GetLyricsSpaceData(lineData.word[count - 1].end, lineData.word[count].start);
                }
                else
                {
                    lyricsItemData = GetLyricsSpaceData(lineData.word[count - 1].end, lineData.end);
                }

                groupData.lyricsDataList.Add(lyricsItemData);
                isSpace = false;
            }
        }

        return groupData;
    }

    private LyricsGroupData GetLineWord(Lyc data, int sectionIndex, int lineIndex)
    {
        Line lineData = data.section[sectionIndex].line[lineIndex];

        int count = 0;
        bool isSpace = false;

        LyricsGroupData groupData = new LyricsGroupData
        {
            minTime = GetReturnSec(lineData.start),
            maxTime = GetReturnSec(lineData.end),
            wordMaxCount = lineData.word.Length
        };

        foreach (var wordData in lineData.word)
        {
            LyricsItemData lyricsItemData = GetLyricsItemData(wordData);
            int space = lyricsItemData.lyric.LastIndexOf(" ", lyricsItemData.lyric.Length - 1, StringComparison.Ordinal);

            if (space > 0)
            {
                lyricsItemData.lyric = lyricsItemData.lyric.TrimEnd();
                isSpace = true;
            }

            groupData.lyricsDataList.Add(lyricsItemData);
            count++;
            if (isSpace)
            {
                if (count < lineData.word.Length)
                {
                    lyricsItemData = GetLyricsSpaceData(lineData.word[count - 1].end, lineData.word[count].start);
                }
                else
                {
                    lyricsItemData = GetLyricsSpaceData(lineData.word[count - 1].end, lineData.end);
                }

                groupData.lyricsDataList.Add(lyricsItemData);
                isSpace = false;
            }
        }

        return groupData;
    }
    
    private LyricsItemData GetLyricsItemData(Word wordData)
    {
        LyricsItemData data = new LyricsItemData
        {
            type = LyricsType.Text,
            lyric = language == 0 ? wordData.en : wordData.ko,
            start = GetReturnSec(wordData.start),
            end = GetReturnSec(wordData.end)
        };

        return data;
    }

    private LyricsItemData GetLyricsSpaceData(float start, float end)
    {
        LyricsItemData data = new LyricsItemData
        {
            type = LyricsType.Space,
            lyric = " ",
            start = GetReturnSec(start),
            end = GetReturnSec(end)
        };
        return data;
    }

    private float GetReturnSec(float time)
    {
        return time * 0.001f;
    }
    
    private void SectionFirstInit()
    {
        // 데이터 초기화
        isFirstLineSet = false;
        lineStepCount = 0;
        playCompleteLineCount = 0;
        playWordStepCount = 0;
        playLineStepCount = 0;
    }

    private LyricsGroupData SetLineText(int index, LyricsGroupData data)
    {
        // 0 1 2 0 1 2
        // int areaIndex = index % 3;
        int areaIndex = index % 2;
        string lyricTexts = "";
        foreach (var lyricsData in data.lyricsDataList)
        {
            foreach (var item in textItems)
            {
                if (item.gameObject.activeSelf == false)
                {
                    RectTransform textRect = item.GetComponent<RectTransform>();
                    // 줄위치 area로 SetParent
                    textRect.SetParent(textArea[areaIndex], false);

                    LyricsItemData lyricsItemData = lyricsData;
                    if (areaIndex == 0)
                    {
                        firstAreaItemList.Add(item);
                        data.lineType = LineType.First;
                    }
                    else if (areaIndex == 1)
                    {
                        secondAreaItemList.Add(item);
                        data.lineType = LineType.Second;
                    }
                    // else if (areaIndex == 2)
                    // {
                    //     thirdAreaItemList.Add(item);
                    //     data.lineType = LineType.Third;
                    // }

                    item.LyricsType = lyricsItemData.type;
                    lyricTexts += lyricsItemData.lyric;
                    item.Start = lyricsItemData.start;
                    item.End = lyricsItemData.end;
                    item.LyricsText.text = lyricsItemData.lyric;
                    item.MaskText.text = lyricsItemData.lyric;
                    item.gameObject.SetActive(true);
                    break;
                }
            }
        }

        lineStepCount++;
        return data;
    }

    public void SetLyricsPlay(bool value)
    {
        // isStart => true
        isStart = value;
    }

    public void LyricsPlay()
    {
        if (isFirstLineSet == false)
        {
            if (lyricsGroupList[0].minTime - SingNoteManager.widthMaxSec < lyrics.time)
            {
                lyricsGroupList[0] = SetLineText(lineStepCount, lyricsGroupList[0]);
                lyricsGroupList[1] = SetLineText(lineStepCount, lyricsGroupList[1]);
                //lyricsGroupList[2] = SetLineText(lineStepCount, lyricsGroupList[2]);
                isFirstLineSet = true;
            }
        }

        if (playCompleteLineCount < 1)
        {
            return;
        }

        if (playLineStepCount > 0)
        {
            if (lyrics.time > lineHalfTime)
            {
                SetResetLineSearch();
            }
        }
    }

    private void SetResetLineSearch()
    {
        //int completeIdx = completeCount % 3;
        int completeIdx = completeCount % 2;
        
        for (int i = 0; i < lyricsGroupList.Count; i++)
        {
            if (lyricsGroupList[i].isComplete)
            {
                lyricsGroupList[i].isComplete = false;
                if (lyricsGroupList[i].lineType == LineType.First)
                {
                    ResetLineText(firstAreaItemList, completeIdx);
                }
                else if (lyricsGroupList[i].lineType == LineType.Second)
                {
                    ResetLineText(secondAreaItemList, completeIdx);
                }
                // else if (lyricsGroupList[i].lineType == LineType.Third)
                // {
                //     ResetLineText(thirdAreaItemList, completeIdx);
                // }
            }
        }
        
        completeCount++;
    }

    private void ResetLineText(List<SingLyricsText> data, int completeIdx)
    {
        // if (completeIdx == 0)
        // {
        //     textArea[0].SetAsFirstSibling();
        // }
        // else if (completeIdx == 1)
        // {
        //     textArea[1].SetAsFirstSibling();
        // }
        // else if (completeIdx == 2)
        // {
        //     textArea[2].SetAsFirstSibling();
        // }
        lyricParent.GetChild(0).SetAsLastSibling();
        
        Vector2 originPos = Vector2.zero;
        string passTxt = null;
        for (int i = 0; i < data.Count; i++)
        {
            passTxt += data[i].LyricsText.text;
            
            SingLyricsText lyricsText = data[i];
            RectTransform lyricsRect = lyricsText.GetComponent<RectTransform>();
            lyricsRect.SetParent(buffer, false);
            lyricsText.gameObject.SetActive(false);
        }
        data.Clear();

        if (passParent.childCount > 0)
        {
            SingLyricsText oldPassLyricsText = passParent.GetChild(0).GetComponent<SingLyricsText>();
            RectTransform oldPassLyricsRect = oldPassLyricsText.GetComponent<RectTransform>();
            oldPassLyricsRect.SetParent(passBuffer, false);
            oldPassLyricsText.gameObject.SetActive(false);
        }
        passTextItems[0].gameObject.SetActive(true);
        SingLyricsText passLyricsText = passTextItems[0];
        RectTransform passLyrics = passLyricsText.GetComponent<RectTransform>();
        passLyrics.SetParent(passParent, false);
        passLyricsText.LyricsText.text = passTxt;
        
        int count = lineStepCount;
        if (lineStepCount < lyricsGroupList.Count)
        {
            lyricsGroupList[count] = SetLineText(count, lyricsGroupList[count]);
        }
    }

    public void WordCountCheck(LyricsType type)
    {
        if (type == LyricsType.Space)
        {
            return;
        }

        if (lyricsGroupList[playLineStepCount].wordMaxCount > 0)
        {
            playWordStepCount++;

            if (playWordStepCount == lyricsGroupList[playLineStepCount].wordMaxCount)
            {
                lyricsGroupList[playLineStepCount].isComplete = true;
                playCompleteLineCount++;
                playLineStepCount++;
                playWordStepCount = 0;
                if (playLineStepCount < lyricsGroupList.Count)
                {
                    lineHalfTime = (lyricsGroupList[playLineStepCount].maxTime - lyricsGroupList[playLineStepCount].minTime) * 0.01f + lyricsGroupList[playLineStepCount].minTime;
                }
                else
                {
                    StartCoroutine(DelayResetLyrics());
                }
            }
        }
    }

    private IEnumerator DelayResetLyrics()
    {
        yield return new WaitForSeconds(3.0f);
        SetResetLineSearch();
    }

    public void ReplayReset()
    {
        isStart = false;
        int count = textItems.Length;
        for (int i = 0; i < count; i++)
        {
            Destroy(textItems[i]);
        }

        textItems = null;

        DestroyLyricsObj();
    }

    private void DestroyLyricsObj()
    {
        if (isStart)
        {
            return;
        }

        foreach (var _textArea in textArea)
        {
            for (int j = 0; j < _textArea.gameObject.transform.childCount; j++)
            {
                Destroy(_textArea.gameObject.transform.GetChild(j).gameObject);
            }
        }
        
        for (int i = 0; i < buffer.gameObject.transform.childCount; i++)
        {
            Destroy(buffer.gameObject.transform.GetChild(i).gameObject);
        }

        firstAreaItemList.Clear();
        secondAreaItemList.Clear();
        //thirdAreaItemList.Clear();
    }
}
