using UnityEngine.UI;
using UnityEngine;
using TMPro;
using PlayerData = MatchServerCollection.PlayerInfo;
using MatchData = MatchServerCollection.MatchInfo;
using MatchServerCollection;
using Mirror;

namespace MatchTest
{
    public class UILobby : MonoBehaviour
    {

        [Header("Lobby")]

        [SerializeField] private MatchUI _matchPrefab;
        [SerializeField] private Transform _matchListRoot;
        [SerializeField] private Transform _lobbyRoot;
        [SerializeField] private Button _joinToSelectedMatch;


        [Header("Options")]

        [SerializeField] private Button _join;
        [SerializeField] private Button _host;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_InputField _roomTitle;
        [SerializeField] private Transform _optionsRoot;

        [Header("Match")]

        [SerializeField] private Transform _matchRoot;
        [SerializeField] private TMP_Text _matchIdField;
        [SerializeField] private Button _startButton;

        [Header("Player")]
        [SerializeField] private Transform _playerGroupRoot;
        [SerializeField] private UIPlayer _playerUIPrefab;

        public static UILobby Instance;
        private bool _inMatch;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(LoadGameForAllMatchPlayers);
        }

        private void Start()
        {
            isMatch = false;
            inProgress = false;

            _matchIdField.text = string.Empty;
            matchSelected = false;

            _startButton.onClick.AddListener(LoadGameForAllMatchPlayers);
        }

        protected bool isMatch
        {
            set
            {
                _matchRoot.gameObject.SetActive(value);
                _lobbyRoot.gameObject.SetActive(!value);
            }
        }

        public bool matchSelected
        {
            set
            {
                _joinToSelectedMatch.interactable = value;
            }
        }

        protected bool inProgress
        {
            set
            {
                SetInterractible(new Button[]
                {
                    _join,
                    _host,
                }, !value);
            }
        }

        public bool isLeader
        {
            set
            {
                _startButton.gameObject.SetActive(value);
            }
        }

        public void ShowRoom(string matchId, bool leader = false)
        {
            _lobbyRoot.gameObject.SetActive(false);
            _matchRoot.gameObject.SetActive(true);

            _matchIdField.text = matchId.ToString();
            isLeader = leader;
        }

        public void ShowLobby()
        {
            _lobbyRoot.gameObject.SetActive(true);
            _matchRoot.gameObject.SetActive(false);
        }

        public void SwitchMatchOptions()
        {
            _optionsRoot.gameObject.SetActive(!_optionsRoot.gameObject.activeInHierarchy);
        }


        public void RefreshPlayersInRoom(PlayerData[] players, bool leader = false, string matchId = "")
        {
            if (matchId != "")
                ShowRoom(matchId, leader);

            foreach(Transform child in _playerGroupRoot)
            {
                Destroy(child.gameObject);
            }

            foreach(var player in players)
            {
                SpawnUIPlayer(player);
            }
        }

        public void RefreshMatchList(MatchData[] matches)
        {
            foreach (Transform child in _matchListRoot)
            {
                Destroy(child.gameObject);
            }

            foreach(var match in matches)
            {
                SpawnMatchUI(match);
            }
        }

        protected UIPlayer SpawnUIPlayer(PlayerData player)
        {
            var newUIPlayer = Instantiate(_playerUIPrefab, _playerGroupRoot);
            newUIPlayer.playerName = player.name;
            newUIPlayer.transform.SetAsLastSibling();
            newUIPlayer.isLeader = player.isLeader;


            return newUIPlayer;
        }

        protected MatchUI SpawnMatchUI(MatchData match)
        {
            var newmatch = Instantiate(_matchPrefab, _matchListRoot);
            newmatch.SetMatchConfig(match);
            newmatch.transform.SetAsLastSibling();

            return newmatch;
        }
            
        protected void SetInterractible(Button[] buttons, bool active)
        {
            foreach (var button in buttons)
                button.interactable = active;
        }

        public void HostGame()
        {
            ServerManager.Instance.RequestCreateRoom(_roomTitle.text);
        }

        public void JoinToSelectedMatch()
        {
            ServerManager.Instance.RequestJoinRoom();
        }

        public void JoinGame()
        {
            ServerManager.Instance.RequestJoinRoom(_inputField.text); 
        }

        public void LeaveGame()
        {
            ServerManager.Instance.RequestLeaveRoom();
        }

        protected void LoadGameForAllMatchPlayers() => ServerManager.Instance.RequestLoadGame();
    }
}
