using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkMatch))]
public class MatchGamePlayer : NetworkBehaviour
{
    [SerializeField]
    private float _speed;
    [Client]
    private void Update()
    {
        var input = new Vector3(Input.GetAxis("Horizontal"),0f, Input.GetAxis("Vertical"));
        transform.position += input * _speed * Time.deltaTime;
    }
}
