using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetworkConstants;
using Photon.Pun;
using Photon.Realtime;
using RenderHeads.Media.AVProVideo;
using SingSDKConstants;
using UnityEngine;
using UnityEngine.UI;
using ErrorCode = RenderHeads.Media.AVProVideo.ErrorCode;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/*
 * https://cns-asp.sing-it.app
 */

/*
 * 200 Status OK : 대부분의 정상 처리 및 결과 반환시 응답코드
 * 400 Bad Request : 필수 요소가 누락되거나 Request 시 Header 에 serviceKey 가 설정되지 않은 경우
 * 401 Unauthorized : 세션이 만료되었거나 설정되지 않을 경우, 회원 인증에 문제가 있는 경우
 * 500 Internal Server Error : 서비스를 제공하는 서버의 문제
 * 503 Service Unavailable : 주로 서비스 유지 보수 진행시간에 API 요청을 한 경우
 */

/*
 * MR1001 /mr/information/{mr_id} : Sing 을 위해 MR 의 가사, 노트 경로 등 상세 정보 조회
 * = {mr_id} 조회 할 반주(MR)의 ID 값
 * 
 * MR2001 /mr/list/my/{opt}/{start}/{num} : 내가 부른 곡 History 조회 (최근, 많이 부른 순)
 * = {opt} 가져 올 MR 목록.
 * - singcount 많이 부른 순서대로 목록을 가져온다.
 * - history 최근 부른 순서대로 목록을 가져온다.
 * = {start} 가져 올 Item index. 0부터 시작한다.
 * = {num} 가져 올 Item 갯수
 * 
 * MR2002 /mr/list/weekly/{start}/{num} : 주간 인기곡 목록
 * = {start} 가져 올 Item index. 0부터 시작한다.
 * = {num} 가져 올 Item 갯수
 * 
 * MR2003 /mr/list/best/{start}/{num} : 인기곡 (가장 많이 불려지는 애창곡)
 * = {start} 가져 올 Item index. 0부터 시작한다.
 * = {num} 가져 올 Item 갯수
 * 
 * MR2004 /mr/list/new/{start}/{num} : 최신곡 (최근 추가 신곡)
 * = {start} 가져 올 Item index. 0부터 시작한다.
 * = {num} 가져 올 Item 갯수
 * 
 * MR3001 /search/artist?keyword={keyword}&index={index}&perpage={perpage} : 아티스트 명 (가수) 검색
 * = {keyword} 검색어
 * = {index} 가져 올 검색 결과 Start Index
 * = {perpage} 한 번에 가져 올 검색 결과 rows
 * 
 * MR3002 /search/mr?keyword={keyword}&index={index}&perpage={perpage} : 곡 검색 (반주 검색)
 * = {keyword} 검색어
 * = {index} 가져 올 검색 결과 Start Index
 * = {perpage} 한 번에 가져 올 검색 결과 rows
 * 
 * MR3003 /search/auto_complete?keyword={keyword}&index={index}&perpage={perpage} : 검색어 자동 완성 목록 조회
 * = {keyword} 검색어
 * = {index} 가져 올 검색 결과 Start Index
 * = {perpage} 한 번에 가져 올 검색 결과 rows
 * 
 * MR4001 /log/sing/start : 노래 부르기 시작 로그 전송
 * MR4002 /log/sing/60sec : 노래 부르기 중간 로그 전송 (60초 경과 시점)
 * MR4003 /log/sing/end : 노래 부르기 종료 로그 전송 (끝까지 부른지 여부와 관계 없이 제출)
 */

[Serializable]
public class ScoreEvaluationData
{
    public string userName;
    public int score;
    public bool check;
}

[Serializable]
public class SingStartingWaitData
{
    public string userName;
    public bool check;
}

public class KaraokeManager : MonoBehaviour
{
    private PhotonView pv;

    public Camera karaokeTvCamera;
    public GameObject tvDisplay;
    public GameObject noticeDisplay;
    public AudioClip pang;
    public List<ScoreEvaluationData> scoreEvaluationDataList = new List<ScoreEvaluationData>();
    public List<SingStartingWaitData> singStartingWaitList = new List<SingStartingWaitData>();
    public List<string> latePlayerList = new List<string>();
    public string singScore;

    //private UIKaraoke_Lyrics lyrics;
    private string songName;
    private string singerName;
    private string songId;
    private bool startSing = false;
    private bool startEvaluation = false;
    //private double syncTime;

    public string SingerName => singerName;

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        
        // 동기화
        // pv.OwnershipTransfer = OwnershipOption.Takeover;
        // if (pv.ObservedComponents[0] == null)
        //     pv.ObservedComponents[0] = this;
        // else
        //     pv.ObservedComponents.Add(this);
    }
    
    // 버튼 누르면 동작하는 최초 시작 함수
    public void OnKaraoke()
    {
        // currentState 가 Karaoke 이라면 노래 부르는 중 팝업
        // 그렇지 않을 경우에만 실행 -> UIKaraoke_SelectSong 노래 선택 창으로 이동.
        if (RoomManager.Instance.currentState == UserState.Karaoke)
        {
            UIManager.Instance.OpenPopup("While user is singing,\nyou can't sing", null, "OK");
        }
        else
        {
            UIManager.Instance.OpenPopup("Would you like to sing?", () =>
            {
                if (RoomManager.Instance.currentState == UserState.Karaoke)
                {
                    UIManager.Instance.OpenPopup("While other user is singing,\nyou can't sing", null, "OK");
                    return;
                }
                
                UIManager.Instance.GetUI<UIKaraoke_SelectSong>();
                //LgIndiaProtocol.Instance.RequestSingLog();
                AnalyticsManager.Instance.AddLogString(SalinConstants.AnalyticsEventName.click, SalinConstants.AnalyticsParameterName.click_event_name, SalinConstants.AnalyticsValueName.sing_mode);
            }, null, "OK", "Cancel");
        }
    }

    public void StartSing(string songTitle, int mrId, int key, int language, string singerName)
    {
        // StartSing API 요청
        LgIndiaProtocol.Instance.RequestStartSing(songTitle, mrId, DataManager.Instance.TempRoomData.roomId, UpdateStartSing);

        //노래 시작 로그
        AnalyticsManager.Instance.AddLogString(SalinConstants.AnalyticsEventName.sing, SalinConstants.AnalyticsParameterName.sing_event_name, SalinConstants.AnalyticsValueName.start);
        
        // 노래부르는 애니메이션. 나(노래부른 사람)만 동작.
        pv.RPC("ReceiveSingerAnimation", RpcTarget.AllBuffered, singerName, SalinConstants.AnimationType.Sing, true);
        //PlayerManager.MyAvatar.ExcuteBoolAnimation(SalinConstants.AnimationType.Sing, true);
        
        // 내(노래부른 사람) 플레이어 커스텀 프로퍼티 Singing, Sing 추가
        Hashtable hash = new Hashtable {{"Singing", "Sing"}};
        PlayerManager.MyPlayer.SetCustomProperties(hash);

        // 스타트 싱
        StartCoroutine(StartSingEnum(mrId, key, language, singerName));
    }

    private void UpdateStartSing(bool bSuccess)
    {
        if (bSuccess)
        {
            pv.RPC("ReceiveShareSongId", RpcTarget.All, DataManager.Instance.userSongLogId, DataManager.Instance.songId);
        }
    }

    [PunRPC]
    private void ReceiveShareSongId(string userSongLogId, string songId)
    {
        DataManager.Instance.userSongLogId = userSongLogId;
        DataManager.Instance.songId = songId;
    }

    private IEnumerator StartSingEnum(int mrId, int key, int language, string singerName)
    {
        // 모든 유저에게 UIKaraoke_Singing 을 생성. 모든 유저의 룸매니저 current State를 Karaoke 로 karaoke state를 Singing 으로 바꾼다. 나중에 들어온 유저도.
        pv.RPC("ReceiveSinging", RpcTarget.AllBuffered, singerName);

        // ReceiveSinging 이 끝나고 나서 ReceiveStartSing을 보내기 위해서 startSing bool 값 true가 될때까지 기다림.
        yield return new WaitUntil(() => startSing);
        
        // 모든 유저에게 UIKaraoke_Lyric 을 생성. 현재 들어와 있는 유저들만.
        pv.RPC("ReceiveStartSing", RpcTarget.All, mrId, key, language, singerName);
    }

    [PunRPC]
    private void ReceiveSingerAnimation(string singerName, SalinConstants.AnimationType type, bool isPlay)
    {
        StartCoroutine(ReceiveSingerAnimationEnum(singerName, type, isPlay));
    }

    private IEnumerator ReceiveSingerAnimationEnum(string singerName, SalinConstants.AnimationType type, bool isPlay)
    {
        BaseAvatar[] players = ObjectManager.Standard.GetComponentsInChildren<BaseAvatar>();

        yield return new WaitUntil(() => PhotonNetwork.PlayerList.Length == players.Length);
        
        foreach (var player in players)
        {
            if (player.AVATAR_NAME == singerName)
            {
                StartCoroutine(player.ExecuteSingerAnimation(type, isPlay));
            }
        }
    }

    [PunRPC]
    private void ReceiveSinging(string singerName)
    {
        // StartSing 함수 -> AllBuffered
        // 혹시 모르니 처음 시작할때 startSing 초기화.
        startSing = false;
        
        // 싱어네임 저장
        this.singerName = singerName;

        // 플레이어 리스트에 노래부른 사람을 찾음.
        Player player = PhotonNetwork.PlayerList.ToList().Find(x => x.NickName == singerName);

        // 노래 부른 사람이 있을 경우에만 동작.
        if (player != null)
        {
            // 내 유저네임과 노래 부른 사람 유저 네임이 다를 경우에 (내가 노래 부른 사람이 아닐 경우에)
            // 노래 부른 사람 플레이어 커스텀 프로퍼티에 Singing 키가 있고
            // Singing 키 밸류 값이 Stop 이라면 리턴
            if (DataManager.Instance.UserData.userName != singerName)
            {
                if (player.CustomProperties.ContainsKey("Singing"))
                {
                    if (player.CustomProperties["Singing"].ToString() == "Stop")
                    {
                        return;
                    }
                }
            }
            
            #region 가전배치 상황일 경우

            //가전 배치중에 노래방 시작했을 경우 가전배치에서 나감
            if(RoomManager.Instance.currentState == UserState.Placing)
            {
                UIManager.Instance.GetUI<UIItemMenu>().CloseUI();
                RoomManager.Instance._placingManager.EndToPlace();
            }

            #endregion

            // 모든 룸 유저 현재 스테이트 => Karaoke 으로.
            RoomManager.Instance.currentState = UserState.Karaoke;

            // UIKaraoke_Singing 노래 부르는 중 창 띄움.
            UIKaraoke_Singing singing = UIManager.Instance.GetUI<UIKaraoke_Singing>();
            singing.SetData(singerName);

            // startSing을 true 로 바꿈. startSing이 true일 때 다음 단계 진행.
            startSing = true;
        }
    }

    [PunRPC]
    private void ReceiveStartSing(int mrId, int key, int language, string singerName)
    {
        // StartSing 함수 -> All

        // startSing이 true일 경우에만 동작.
        if (startSing)
        {
            songId = mrId.ToString();
            
            // 모든 룸 유저 카라오케 스테이트 => singing 으로.
            RoomManager.Instance.karaokeState = KaraokeState.Singing;
            
            // 냉장고 애니메이션
            RoomManager.Instance._placingManager._objectInfoDic[SalinConstants.ElectricType.Refrigerator].GetComponentInChildren<RefrigeratorFunction>().StartAnim(true);

            AddSingStartingWait();
            
            #region 노래 데이터 불러오기

            // 노래 가사 창 띄움.
            UIKaraoke_Lyrics lyrics = UIManager.Instance.GetUI<UIKaraoke_Lyrics>();
            lyrics.SetData(singerName, mrId, key, language, tvDisplay, noticeDisplay);

            Debug.Log("DataManager.Instance.UserData.userName " + DataManager.Instance.UserData.userName);
            Debug.Log("singerName " + singerName);
            
            // 듣는사람은 tv화면에서 나오게끔. 가사만 나오게끔. 
            if (!DataManager.Instance.UserData.userName.Equals(singerName))
            {
                Debug.Log("karaokemanager");
                tvDisplay.SetActive(true);
                noticeDisplay.SetActive(false);
                Canvas lyricsCanvas = lyrics.GetComponent<Canvas>();
                lyricsCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                lyricsCanvas.worldCamera = karaokeTvCamera;
                lyricsCanvas.sortingOrder = 0;
            
                // 노래 부르는 사람의 노래 영상과 싱크 맞춤.
                // StartCoroutine(SyncTimeExecute());
            }

            #endregion
        }
    }

    public void AddSingStartingWait()
    {
        pv.RPC("ReceiveAddSingStatingWait", RpcTarget.All, DataManager.Instance.UserData.userName);
    }

    [PunRPC]
    private void ReceiveAddSingStatingWait(string userName)
    {
        var data = singStartingWaitList.Find(x => x.userName == userName);
        if (data != null)
        {
            return;
        }
        
        SingStartingWaitData singStartingWaitData = new SingStartingWaitData
        {
            userName = userName,
            check = false
        };

        singStartingWaitList.Add(singStartingWaitData);
    }

    public void CheckSingStartingWait()
    {
        pv.RPC("ReceiveCheckSingStatingWait", RpcTarget.All, DataManager.Instance.UserData.userName);
    }
    
    [PunRPC]
    private void ReceiveCheckSingStatingWait(string userName)
    {
        var data = singStartingWaitList.Find(x => x.userName == userName);
        if (data != null)
        {
            data.check = true;
        }
    }

    // 노래 부르기 종료. 필요하면 버튼 만들어서 이 함수 연결.
    public void SingingEnding(string singScore, bool clear)
    {
        if (!clear)
        {
            SingAudioStop();
            AnalyticsManager.Instance.AddLogString(SalinConstants.AnalyticsEventName.sing, SalinConstants.AnalyticsParameterName.sing_event_name, SalinConstants.AnalyticsValueName.finish);
        }
        else
        {
            AnalyticsManager.Instance.AddLogString(SalinConstants.AnalyticsEventName.sing, SalinConstants.AnalyticsParameterName.sing_event_name, SalinConstants.AnalyticsValueName.complete);
        }

        // 노래부르는 애니메이션 종료
        pv.RPC("ReceiveSingerAnimation", RpcTarget.AllBuffered, singerName, SalinConstants.AnimationType.Sing, false);
        //PlayerManager.MyAvatar.ExcuteBoolAnimation(SalinConstants.AnimationType.Sing, false);
            
        // 스코어 저장.
        this.singScore = singScore;

        // 다른 유저들에게도 전달.
        pv.RPC("ReceiveSingScore", RpcTarget.Others, singScore);
            
        // 노래 듣는 유저들의 가사 창, 노래부르는 중 창 리무브
        pv.RPC("ReceiveSingingStop", RpcTarget.Others);
            
        // 나의(노래 부른 사람) 가사 창, 노래부르는 중 창 리무브
        ReceiveSingingStop();
            
        // 점수 평가 팝업창을 나를 제외한 다른 유저들에게 띄움.
        ScoreEvaluation(MyFinishEvent);
    }
    
    private void SingAudioStop()
    {
        pv.RPC("ReceiveSingAudioStop", RpcTarget.All);
    }
    
    [PunRPC]
    public void ReceiveSingAudioStop()
    {
        SingAudioManager.Instance.Stop(true);
    }

    [PunRPC]
    private void ReceiveSingScore(string singScore)
    {
        this.singScore = singScore;
    }
    
    private void ScoreEvaluation(Action call)
    {
        // 점수 평가 데이터 셋팅과 점수 평가 창 활성화는 rpc를 두 개로 나눠서
        // 동작하게 함. 점수 평가 데이터 셋팅은 나를 포함한 모든 유저가 셋팅되어야 하고
        // 점수 평가 창 활성화는 나를 제외한 유저들에게만 떠야 되기 때문에 (나는 기다림 토스트가 뜸)
        // 셋팅은 RpcTarget.All 로 활성화는 RpcTarget.Others로 함.

        // 스타트 평가
        StartCoroutine(StartEvaluationEnum());

        // 나 (노래 부른 사람) 에게만 동작. 평가 기다리라는 창 띄우는 함수.
        // 콜백 함수로 한 이유는 위에 rpc 함수가 모두 동작하고 체크리스트가 만들어진 다음에 이 함수가 동작해야 하기 때문.
        call?.Invoke();
    }

    private IEnumerator StartEvaluationEnum()
    {
        // 점수 평가 데이터 셋팅. 모든 룸 유저들에게. 나중에 들어온 유저는 제외.
        pv.RPC("ReceiveScoreEvaluationDataSetting", RpcTarget.All);

        // startEvaluation true 일때까지 기다림.
        yield return new WaitUntil(() => startEvaluation);

        // 점수 평가 창 활성화. 나빼고 다른 룸 유저들에게. 나중에 들어온 유저는 제외.
        pv.RPC("ReceiveScoreEvaluation", RpcTarget.Others);

        yield return new WaitForSeconds(2.0f);
        
        // 방에 나빼고 나중에 들어온 유저만 있는 경우인지 아닌지 체크해서 그럴 경우 바로 종료시킴.
        if (scoreEvaluationDataList.Count > 0 && latePlayerList.Count > 0 &&
            scoreEvaluationDataList.Count == latePlayerList.Count)
        {
            List<bool> lateCheckList = new List<bool>();
            
            for (int i = 0; i < latePlayerList.Count; i++)
            {
                ScoreEvaluationData check = scoreEvaluationDataList.Find(x => x.userName == latePlayerList[i]);
                if (check != null)
                {
                    lateCheckList.Add(true);
                }
            }

            if (scoreEvaluationDataList.Count == lateCheckList.Count)
            {
                CloseKaraoke();
            }
        }
    }

    /// <summary>
    /// 모든 룸 유저들에게. 나중에 들어온 유저는 제외.
    /// </summary>
    [PunRPC]
    private void ReceiveScoreEvaluationDataSetting()
    {
        startEvaluation = false;
        
        // 노래방 상태가 singing이 아니면 리턴. (나중에 들어온 유저)
        if (RoomManager.Instance.karaokeState != KaraokeState.Singing)
        {
            pv.RPC("ReceiveKaraokeLatePlayer", RpcTarget.All, "Add", DataManager.Instance.UserData.userName);
            return;
        }
        
        // 카라오케 스테이트를 Singing -> ScoreEvaluation
        RoomManager.Instance.karaokeState = KaraokeState.ScoreEvaluation;
        
        // 현재 룸에 참여중인 플레이어 리스트를 가지고 온다.
        Player[] tempPlayer = PhotonNetwork.PlayerList;
        
        // 점수평가 체크리스트를 일단 초기화한다.
        scoreEvaluationDataList.Clear();
        
        // 점수평가 체크리스트를 만든다.
        foreach (var player in tempPlayer)
        {
            // 노래 한 사람은 제외. 
            if (player.NickName == singerName)
            {
                continue;
            }

            // 점수 평가 딕셔너리에 현재 룸에 들어와있는 노래한 유저를 제외한 사용자들을 저장. 디폴트로 int 는 3으로. 체크딕셔너리는 false로 저장.
            ScoreEvaluationData scoreEvaluationData = new ScoreEvaluationData
            {
                userName = player.NickName,
                score = 3,
                check = false
            };
            scoreEvaluationDataList.Add(scoreEvaluationData);
        }

        startEvaluation = true;
    }

    [PunRPC]
    private void ReceiveKaraokeLatePlayer(string actionType, string userName)
    {
        switch (actionType)
        {
            case "Add":
                if (latePlayerList.IndexOf(userName) == -1)
                {
                    latePlayerList.Add(userName);
                }
                break;
            case "Delete":
                if (latePlayerList.IndexOf(userName) > -1)
                {
                    latePlayerList.Remove(userName);
                }
                break;
            case "Clear":
                latePlayerList.Clear();
                break;
        }
    }

    /// <summary>
    /// 노래 부른 사람 제외한 다른 유저들에게. 나중에 들어온 유저는 제외.
    /// </summary>
    [PunRPC]
    private void ReceiveScoreEvaluation()
    {
        // 노래방 상태가 ScoreEvaluation 아니면 리턴. (나중에 들어온 유저)
        if (RoomManager.Instance.karaokeState != KaraokeState.ScoreEvaluation)
        {
            return;
        }

        if (startEvaluation)
        {
            // 스코어 평가 창을 띄운다. 노래부른 사람 이름, 노래 제목, 노래 점수 전달.
            UIKaraoke_ScoreEvaluation scoreEvaluation = UIManager.Instance.GetUI<UIKaraoke_ScoreEvaluation>();
            scoreEvaluation.SetData(singerName);
        }
    }
    
    private void MyFinishEvent()
    {
        List<bool> checkList = new List<bool>();
        // 체크딕셔너리에 밸류(불값)만 리스트로 변환.
        foreach (var scoreEvaluationData in scoreEvaluationDataList)
        {
            checkList.Add(scoreEvaluationData.check);
        }
                
        // 체크리스트의 카운트가 0이면 룸에 나말고 아무도 없었다는거임. 그래서 0 이상일때만 기다리라는 팝업창이 뜨고 0이면 평가가 의미없으므로 노래방 종료시킴.
        if (checkList.Count > 0)
        {
            // 노래 부른 사람에게만 실행. 기다리라는 팝업창 띄움.
            UIKaraoke_Popup popup = UIManager.Instance.GetUI<UIKaraoke_Popup>();
            popup.MyPlayerSetData(checkList);
        }
        else
        {
            // 나밖에 없는 상황
            StartCoroutine(SelfFinishScoreEvaluation());
        }
    }

    private IEnumerator SelfFinishScoreEvaluation()
    {
        UpdateScore("0");
        
        // 10초 기다림
        yield return new WaitForSeconds(10.0f);
        
        // 노래방 모드 종료
        CloseKaraoke();
    }

    // 노래를 듣고 있는 유저에게 뜬 UIKaraoke_ScoreEvaluation에서 publish 버튼을 눌렀을 경우
    // 이 함수가 실행.
    public void PublishScore(int score, Action call)
    {
        // API 요청
        if (!string.IsNullOrEmpty(DataManager.Instance.userSongLogId) &&
            !string.IsNullOrEmpty(DataManager.Instance.songId))
        {
            LgIndiaProtocol.Instance.RequestYourScores(DataManager.Instance.TempRoomData.roomId, score);
        }
        
        // 노래 듣고 있는 유저의 유저네임. 이 함수로 들어왔다는게 내가 듣고 있는 유저라는 의미이므로
        // 나의 유저네임.
        string publisherName = DataManager.Instance.UserData.userName;
        
        // 모든 룸 유저에게. 나중에 들어오는 유저에게는 하면 안 됨. RpcTarget.All
        pv.RPC("ReceivePublishScore", RpcTarget.All, publisherName, score);
        
        // rpc 체크 함수가 다 끝나면 콜백함수 동작.
        call?.Invoke();
    }

    [PunRPC]
    private void ReceivePublishScore(string publisherName, int score)
    {
        // scoreEvaluationDict 에 내 이름 키에 밸류 값을 score로. 체크는 true로.
        var publishDataIdx = scoreEvaluationDataList.FindIndex(x => x.userName == publisherName);
        scoreEvaluationDataList[publishDataIdx].score = score;
        scoreEvaluationDataList[publishDataIdx].check = true;
    }
    
    // UIKaraoke_Popup 에서 최종 점수 계산까지 끝내고 
    // 이 최종점수 업데이트 함수를 실행.
    public void UpdateScore(string finalScore)
    {
        // 모든 유저에게 함수 실행.
        pv.RPC("ReceiveUpdateScore", RpcTarget.All, finalScore);
    }

    [PunRPC]
    private void ReceiveUpdateScore(string finalScore)
    {
        // 노래 부른 사람인 경우
        if (DataManager.Instance.UserData.userName == singerName)
        {
            // API 요청
            if (!string.IsNullOrEmpty(DataManager.Instance.userSongLogId) &&
                !string.IsNullOrEmpty(DataManager.Instance.songId))
            {
                LgIndiaProtocol.Instance.RequestFinishSing(singScore, finalScore);    
            }
        }
        
        // 점수 표시
        UIKaraoke_Popup popup = UIManager.Instance.GetUI<UIKaraoke_Popup>();
        popup.SwitchType("Popup");
        if (string.IsNullOrEmpty(singerName))
            singerName = "-";
        if (string.IsNullOrEmpty(singScore))
            singScore = "-";
        if (string.IsNullOrEmpty(finalScore))
            finalScore = "-";
        popup.popupTitle.text = $"{singerName}'s score";
        popup.popupSingScore.text = singScore;
        popup.popupScore.text = finalScore;

        // 팡파레 효과음 - 임시
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        audio.clip = this.pang;
        audio.volume = 0.25f;
        audio.Play();
        Destroy(audio, 10.0f);

        // 팡파레 - 임시
        GameObject pang = Instantiate(Resources.Load<GameObject>("Prefabs/Room/Congratulation"));
        pang.GetComponent<ParticleSystem>().Play();
        Destroy(pang, 10.0f);
    }

    [PunRPC]
    private void ReceiveSingingStop()
    {
        tvDisplay.SetActive(false);
        
        // singing UI, lyrics UI 삭제
        UIManager.Instance.RemoveUI<UIKaraoke_Singing>();
        UIManager.Instance.RemoveUI<UIKaraoke_Lyrics>();
    }

    public void RoomOut()
    {
        // 노래 부른 사람이 나갔을때 이 함수가 동작.
        // 즉 듣고 있던 유저들에게 동작되는 함수. 노래 부른 사람이 나가면 노래방이 의미가 없으므로 모든 노래 부른 사람들은
        // 노래방이 종료되게끔.
        
        // CloseKaraoke 함수를 실행시키면 되지만 이미 SingerOut 함수 자체가 OnPlayerLeft 포톤 함수에 있어서 rpc 기능을 하므로
        // ReceiveCloseKaraoke 함수를 직접 실행.
        ReceiveSingAudioStop();
        ReceiveCloseKaraoke();
    }
    
    // 노래방 종료 함수. 모든 유저들에게 종료 rpc 보냄.
    public void CloseKaraoke()
    {
        pv.RPC("ReceiveCloseKaraoke", RpcTarget.All);
    }

    [PunRPC]
    public void ReceiveCloseKaraoke()
    {
        Player _player = PhotonNetwork.PlayerList.ToList().Find(x => x.NickName == singerName);
        if (_player != null)
        {
            Hashtable hash = new Hashtable {{"Singing", "Stop"}};
            _player.SetCustomProperties(hash);
        }
        
        //string myName = DataManager.Instance.UserData.userName;
        // 커런트 스테이트 => 디폴트
        RoomManager.Instance.currentState = UserState.Default;
        
        // 카라오케 스테이트 => 디폴트
        RoomManager.Instance.karaokeState = KaraokeState.Default;
        
        // tv 끔
        tvDisplay.SetActive(false);
        noticeDisplay.SetActive(true);
        
        // 모든 UI 삭제
        UIManager.Instance.RemoveUI<UIKaraoke_Singing>();
        UIManager.Instance.RemoveUI<UIKaraoke_Lyrics>();
        UIManager.Instance.RemoveUI<UIKaraoke_ScoreEvaluation>();
        UIManager.Instance.RemoveUI<UIKaraoke_Popup>();
        
        // 아바타 머리 위에 점수 띄우기. 노래부른 사람은 동작하지 않게..
        // 키만 모음 (닉네임) 노래부른 사람은 없는
        // 스탠다드 오브젝트 밑에 있는 아바타들을 가져옴.
        BaseAvatar[] player = ObjectManager.Standard.GetComponentsInChildren<BaseAvatar>();
        for (int i = 0; i < player.Length; i++)
        {
            // 노래 부른 사람 제외
            if (player[i].AVATAR_NAME == singerName)
            {
                continue;
            }

            // 늦게 들어온 유저 리스트에 아바타네임이 있으면 제외
            var latePlayerIdx = latePlayerList.FindIndex(x => x == player[i].AVATAR_NAME);

            if (latePlayerIdx > -1)
            {
                continue;
            }
            
            // 점수평가 딕셔너리에 아바타네임이 있으면
            var avatarDataIdx = scoreEvaluationDataList.FindIndex(x => x.userName == player[i].AVATAR_NAME);
            if (avatarDataIdx > -1)
            {
                player[i].SetScoreBorder(scoreEvaluationDataList[avatarDataIdx].score.ToString());    
            }
        }
        
        // 냉장고 애니메이션
        bool refrigeratorPlay = RoomManager.Instance._placingManager
            ._objectInfoDic[SalinConstants.ElectricType.Refrigerator].GetComponentInChildren<RefrigeratorFunction>()
            .isPlay;
        if (refrigeratorPlay)
        {
            RoomManager.Instance._placingManager._objectInfoDic[SalinConstants.ElectricType.Refrigerator].GetComponentInChildren<RefrigeratorFunction>().StartAnim(false);    
        }

        // 데이터 초기화
        scoreEvaluationDataList.Clear();
        singStartingWaitList.Clear();
        songId = string.Empty;
        songName = string.Empty;
        singerName = string.Empty;
        singScore = string.Empty;
        startSing = false;
        startEvaluation = false;
        RoomManager.Instance.currentState = UserState.Default;
        DataManager.Instance.userSongLogId = null;
        DataManager.Instance.songId = null;
        StopAllCoroutines();
        
        pv.RPC("ReceiveKaraokeLatePlayer", RpcTarget.All, "Clear", "");
        //syncTime = 0.0f;
        //lyrics = null;
    }

    // void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    // {
    //     if (stream.IsWriting)
    //         SendMessage(stream);
    //
    //     if (stream.IsReading)
    //         ReceiveMessage(stream);
    // }
    //
    // // 시간 동기화
    // private void SendMessage(PhotonStream stream)
    // {
    //     if (pv.IsMine && lyrics != null)
    //     {
    //         stream.SendNext(lyrics.time);
    //     }
    // }
    //
    // private void ReceiveMessage(PhotonStream stream)
    // {
    //     if (pv.IsMine == false && lyrics != null)
    //     {
    //         syncTime = (double) stream.ReceiveNext();
    //     }
    // }

    private void OnApplicationPause(bool pauseStatus)
    {
        // if (RoomManager.Instance.karaokeState == KaraokeState.Singing)
        // {
        //     SingAudioManager.Instance.SetVolumeBalance(0.0f);
        //     SingAudioManager.Instance.SetGuideVolume(0.0f);
        // }
        
        // 만약 스코어 평가 모드에서 내가 앱을 포즈했다가 다시 켰으면 꼬이기 때문에....
        // 일단 노래방 종료시켜버림
        if (RoomManager.Instance.karaokeState == KaraokeState.ScoreEvaluation)
        {
            CloseKaraoke();
        }
    }

    // private void OnApplicationFocus(bool pauseStatus)
    // {
    //     if (RoomManager.Instance.karaokeState == KaraokeState.Singing)
    //     {
    //         SingAudioManager.Instance.SetVolumeBalance(0.5f);
    //         SingAudioManager.Instance.SetGuideVolume(0.5f);
    //     }
    // }
}
