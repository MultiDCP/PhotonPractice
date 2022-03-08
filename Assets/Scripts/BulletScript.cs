using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BulletScript : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    int dir;

    private void Start() {
        Destroy(gameObject, 3.5f);
    }

    private void Update() {
        transform.Translate(Vector3.right * 7 * Time.deltaTime * dir);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.tag == "Ground") PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        if(!PV.IsMine && other.tag == "Player" && other.GetComponent<PhotonView>().IsMine){ // 느린 쪽에 맞춰서 Hit 판정
            // -> 이게 뭔 소리냐? P1에서 쐈으면 P2에서 충돌 판정을 해야하고, P2에서 쐈으면 P1에서 판정을 해야한다는 소리
            other.GetComponent<PlayerScript>().Hit();
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void DirRPC(int dir) => this.dir = dir;

    [PunRPC]
    void DestroyRPC() => Destroy(gameObject);
}
