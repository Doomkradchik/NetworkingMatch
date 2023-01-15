using Mirror;
using UnityEngine;
using TMPro;

public class UIRegistration : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _nameInput;
    public void StartHost()
    {
        NetworkManager.singleton.GetComponent<AutheticationMatch>().RegisterUser(_nameInput.text);
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.singleton.GetComponent<AutheticationMatch>().RegisterUser(_nameInput.text);
        NetworkManager.singleton.StartClient();
    }
}
