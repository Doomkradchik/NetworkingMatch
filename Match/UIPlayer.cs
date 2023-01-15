using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _nickname;

    [SerializeField]
    private Image _crownImage;

    public string playerName
    {
        set
        {
            _nickname.text = value;
        }
    }

    public bool isLeader
    {
        set => _crownImage.gameObject.SetActive(value);
    }
}
