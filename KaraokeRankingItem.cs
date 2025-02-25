using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KaraokeRankingItem : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private Text rank;
    [SerializeField] private Text nickname;
    [SerializeField] private Text title;
    [SerializeField] private Text score;
    [SerializeField] private Text user;

    public void SetData(string id, string machineScore, string userAvgScore, string nickname, string title, string rank)
    {
        this.id = id;
        score.text = machineScore;
        user.text = userAvgScore;
        this.nickname.text = nickname;
        this.title.text = title;
        this.rank.text = rank;
    }
}
