using UnityEngine.UI;
using MatchServerCollection;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    public MatchServerCollection.MatchInfo config;

    [SerializeField]
    private Text _matchName;

    [SerializeField]
    private Text _playersCapacity;

    public void SetMatchConfig(MatchServerCollection.MatchInfo matchinfo)
    {
        _matchName.text = $"{matchinfo.title}";
        _playersCapacity.text = $"{matchinfo.players} / {matchinfo.maxplayers} players";
        config = matchinfo;
    }

    public void Start()
    {
        GetComponent<Toggle>().onValueChanged.AddListener((on) => ServerManager.Instance.SelectMatch(on ? config.matchid : string.Empty));
    }

}
