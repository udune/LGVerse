using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using Singit;

[Serializable]
public class PostStartLog
{
    public string user_key;
    public int user_type;
    public int contents_id;
}

[Serializable]
public class PostTimeLog
{
    public string user_key;
    public int user_type;
    public int contents_id;
    public int log_id;
    public int listen_sec;
    public int listen_complete;
}

[Serializable]
public class GetReturnLog
{
    public int log_id;
    public int listen_sec;
    public int listen_complete;
}
//
// public class AndroidNativeVolumeService
// {
// #if UNITY_ANDROID && !UNITY_EDITOR
//     static int STREAMMUSIC;
//     static int FLAGSHOWUI = 1;
//
//     private static AndroidJavaObject audioManager;
//
//     private static AndroidJavaObject deviceAudio
//     {
//         get
//         {
//             if (audioManager == null)
//             {
//                 AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//                 AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
//                 AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
//                 AndroidJavaClass audioManagerClass = new AndroidJavaClass("android.media.AudioManager");
//                 AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
//
//                 STREAMMUSIC = audioManagerClass.GetStatic<int>("STREAM_MUSIC");
//                 string Context_AUDIO_SERVICE = contextClass.GetStatic<string>("AUDIO_SERVICE");
//
//                 audioManager = context.Call<AndroidJavaObject>("getSystemService", Context_AUDIO_SERVICE);
//                 
//                 if (audioManager != null)
//                     Debug.Log("[AndroidNativeVolumeService] Android Audio Manager successfully set up");
//                 else
//                     Debug.Log("[AndroidNativeVolumeService] Could not read Audio Manager");
//             }
//             return audioManager;
//         }
//
//     }
//
//     private static int GetDeviceMaxVolume()
//     {
//         return deviceAudio.Call<int>("getStreamMaxVolume", STREAMMUSIC);
//     }
//
//     public float GetSystemVolume()
//     {
//         int deviceVolume = deviceAudio.Call<int>("getStreamVolume", STREAMMUSIC);
//         float scaledVolume = (float)(deviceVolume / (float)GetDeviceMaxVolume());
//
//         return scaledVolume;
//     }
//
//     public void SetSystemVolume(float volumeValue)
//     {
//         int scaledVolume = (int)(volumeValue * (float)GetDeviceMaxVolume());
//         deviceAudio.Call("setStreamVolume", STREAMMUSIC, scaledVolume, FLAGSHOWUI);
//     }
// #endif
// }

public class SingAudioManager : Singleton<SingAudioManager>
{
    private static AndroidJavaClass jc;
    private static AndroidJavaObject native;
// #if UNITY_ANDROID && !UNITY_EDITOR
//     public AndroidNativeVolumeService service = new AndroidNativeVolumeService();
// #endif

    public GetReturnLog returnLog;
    public int listenTime;
    public int isEnd;
    public int maleMin;
    public int rapKey;

    public IEnumerator Init()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => Application.isFocused);
        }
        
#if (UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("start android native");

        if (native != null)
        {
            yield break;
        }
        
        if (native == null)
        {
            using (jc = new AndroidJavaClass("kr.mediascope.audio.singit.libSing"))
            {
                if (jc != null)
                {
                    native = jc.CallStatic<AndroidJavaObject>("getInstance");
                }
            }
        }

        CallNativeMethod("unityEventStart", this.gameObject.name, "LiveEventCallback");
#elif (UNITY_IOS && !UNITY_EDITOR)
        PluginSingitAudio.unityEventStart(this.gameObject.name, "LiveEventCallback");
#endif
        yield return new WaitForSeconds(0.2f);
        InitAudio();
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        if (native != null) native = null;
        if (jc != null) jc = null;
#endif
    }

    /*
       오디오 객체의 초기화 완료. 1
       유선 이어폰으로 오디오 출력. 11
       블루투스 출력장치로 오디오 출력. 이 경우 vocal feedback 기능은 제한 12
       내장 스피커로 오디오 출력 13
       EVENT_BOTH_OUTPUT 14
       MR 반주 파일의 Loading 이 완료되고 재생 준비 되었음 반환 100
       MR 반주 파일의 재생이 시작되면 반환 101
       MR 반주 파일의 재생이 종료되면 반환 109 */
    private void LiveEventCallback(string eventStr)
    {
        Debug.Log("event : " + eventStr);
        int eventValue = Int32.Parse(eventStr);
        switch (eventValue)
        {
            case 11:
                SetVoiceFeedback(true);
                break;
            case 12:
            case 13:
            case 14:
                SetVoiceFeedback(false);
                break;
            case 100:
                if (UIManager.Instance.isUIExist<UIKaraoke_Lyrics>())
                {
                    StartCoroutine(UIManager.Instance.GetUI<UIKaraoke_Lyrics>().LoadData());    
                }
                isEnd = 0;
                break;
            case 101:
                break;
            case 109: //
                isEnd = 1;
                if (UIManager.Instance.isUIExist<UIKaraoke_Lyrics>())
                {
                    UIManager.Instance.GetUI<UIKaraoke_Lyrics>().Ending(true);
                }
                break;
        }
    }

    public void CallNativeMethod(string methodName, params object[] param)
    {
        if (native != null)
        {
            native.Call(methodName, param);
        }
    }

    void SetVoiceFeedback(bool vf) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setVoiceFeedback", vf);
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setVoiceFeedback(vf);
#endif
    }

    private void InitAudio() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("initAudio", 48000, 192);
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.initAudio();
#endif
    }

    public void CloseAudio()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("closeAudio"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.closeAudio();
#elif UNITY_EDITOR
#endif
    }

    public void LoadMR(string hm, string melody, string drum)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (drum != "") CallNativeMethod("loadMR", hm, melody, drum);
        else CallNativeMethod("loadMR", hm, melody);
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.loadMR(hm, melody, drum);
#endif
    }

    public void Play(int mrId)
    {
        SingSDKProtocol.Instance.SendLogStart(mrId);
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("playMR");
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.playMR();
#elif UNITY_EDITOR
#endif
    }

    public void Stop(bool isSix) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("stopMR"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.stopMR();
#elif UNITY_EDITOR
#endif
    }

    public void GetLoadAsset(string note, string tempos, string originalKey, string selectKey)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("loadAsset", note, tempos, originalKey, selectKey);
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.loadAsset(note, tempos, originalKey, selectKey);
#endif
    }

    public void SetVolumeBalance(float value) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setVolumeBalance", value);
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setVolumeBalance(value);
#endif
    }

    public void SetGuideVolume(float value) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            CallNativeMethod("setGuideVolume", value);
#elif UNITY_IOS && !UNITY_EDITOR
            PluginSingitAudio.setGuideVolume(value);
#endif
    }
    
    public double GetPitchValue()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<double>("getPitch");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getPitch();
#elif UNITY_EDITOR
        System.Random rand = new System.Random();
        return rand.Next(maleMin - 12, rapKey - 12);
#endif
    }

    public double GetLevelValue()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<double>("getLevel");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getLevel();
#elif UNITY_EDITOR
        System.Random rand = new System.Random();
        return rand.NextDouble();
#endif
    }

    public int GetMrShift()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<int>("getMRShift");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getMRShift();
#elif UNITY_EDITOR
        return 0;
#endif
    }

    public string GetScore() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<string>("getScore"); 
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getScore();
#elif UNITY_EDITOR
        System.Random rand = new System.Random();
        return rand.Next(0, 100).ToString("F1");
#endif
    }

    public double GetPitchAcc()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<double>("getPitchAcc");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getPitchAcc();
#elif UNITY_EDITOR
        System.Random rand = new System.Random();
        return rand.NextDouble();
#endif
    }

    public double GetRhythmAcc()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<double>("getRhythmAcc");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getRhythmAcc();
#elif UNITY_EDITOR
        System.Random rand = new System.Random();
        return rand.NextDouble();
#endif
    }

    public int SetMrShift(int value) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<int>("setMRShift", value); 
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.setMRShift(value);
#elif UNITY_EDITOR
        return 0;
#endif
    }

    public double GetCurrentPosition()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<double>("getCurrentPosition");
#elif UNITY_IOS && !UNITY_EDITOR
        return PluginSingitAudio.getCurrentPosition();
#elif UNITY_EDITOR
        return 0;
#endif
    }

    public void ResetEffect() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("resetEffect"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.resetEffect();
#endif
    }

    public void SetDeNoising() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setDenoising"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setDenoising();
#endif
    }

    public void SetSoftHall() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setSoftHall"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setSoftHall();
#endif
    }

    public void SetDryHall() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setDryHall"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setDryHall();
#endif
    }

    public void SetSoftRoom() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setSoftRoom"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setSoftRoom();
#endif
    }

    public void SetDryRoom() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setDryRoom"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setDryRoom();
#endif
    }

    public void SetMaleDance() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setMaleDance"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setMaleDance();
#endif
    }

    public void SetMaleRock() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setMaleRock"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setMaleRock();
#endif
    }

    public void SetMaleRnB() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setMaleRnB"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setMaleRnB();
#endif
    }

    public void SetFemaleDance() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setFemaleDance"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setFemaleDance();
#endif
    }

    public void SetFemaleRock() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setFemaleRock"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setFemaleRock();
#endif
    }

    public void SetFemaleRnB() 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setFemaleRnB"); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setFemaleRnB();
#endif
    }

    public void SetEchoVolume(float value) 
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallNativeMethod("setEchoVolume", value); 
#elif UNITY_IOS && !UNITY_EDITOR
        PluginSingitAudio.setEchoVolume((int)value);
#endif
    }
    
    public int IsLowLatencyDevice()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<int>("isLowlatencyDevice");
#elif UNITY_EDITOR
        return 0;
#endif
    }

    public bool GetPlayMRStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return native.Call<bool>("getPlayMRStatus");
#elif UNITY_EDITOR
        return true;
#endif
    }
}
