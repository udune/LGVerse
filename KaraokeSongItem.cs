using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KaraokeSongItem : MonoBehaviour
{
    [SerializeField] private TextureInterFace thumbnail;
    [SerializeField] private Text songTitle;
    [SerializeField] private Text singerName;
    [SerializeField] private int id;

    public int ID => id;
    public string SongTitle => songTitle.text;
    public string SingerName => singerName.text;

    public void SetData(string cover, int mrId, string songTitle, string singerName)
    {
        id = mrId;
        thumbnail.SetTexture(cover);
        this.songTitle.text = songTitle;
        this.singerName.text = singerName;
    }
}
