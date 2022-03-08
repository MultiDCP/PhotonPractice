using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;

public class PlayerScript : MonoBehaviourPun, IPunObservable
{
    public Rigidbody2D RB;
    public Animator AN;
    public SpriteRenderer SR;
    public PhotonView PV;
    public Text NickNameText;
    public Image HealthImage;

    bool isGround;
    Vector3 curPos;

    private void Awake() {
        // 닉네임 설정
        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        NickNameText.color = PV.IsMine ? Color.green : Color.red;

        if(PV.IsMine){
            // 2D 카메라
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
        }
    }

    private void Update() {
        if(PV.IsMine){
            // 이동
            float axis = Input.GetAxisRaw("Horizontal");
            RB.velocity = new Vector2(4 * axis, RB.velocity.y);

            if(axis != 0){
                AN.SetBool("Walk", true);
                PV.RPC("FlipXRPC", RpcTarget.AllBuffered, axis); // 재접속 시 FlipX 동기화 위해 AllBuffered
            }
            else AN.SetBool("Walk", false);

            // 점프, 바닥 체크
            isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
            AN.SetBool("Jump", !isGround);
            if(Input.GetKeyDown(KeyCode.UpArrow) && isGround)
                PV.RPC("JumpRPC", RpcTarget.All);
            
            if(Input.GetKeyDown(KeyCode.Space)){
                PhotonNetwork.Instantiate("Bullet", transform.position + new Vector3(SR.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity)
                    .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, SR.flipX ? -1 : 1);
                AN.SetTrigger("Shot");
            }
        }
        // IsMine이 아닌 것들은 부드럽게 위치를 동기화시켜야 함.
        else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;
        else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
    }

    [PunRPC]
    void FlipXRPC(float axis) => SR.flipX = axis == -1;

    [PunRPC]
    void JumpRPC(){
        RB.velocity = Vector2.zero;
        RB.AddForce(Vector2.up * 700);
    }

    public void Hit(){
        HealthImage.fillAmount -= 0.1f;
        if(HealthImage.fillAmount <= 0){
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered); // AllBuffered로 해야 제대로 사라져 복제 버그가 생기지 않음
        }
    }

    [PunRPC]
    void DestroyRPC() => Destroy(gameObject);

    // 이 안에서 변수 동기화가 이루어짐
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting){
            stream.SendNext(transform.position);
            stream.SendNext(HealthImage.fillAmount);
        }
        else{
            curPos = (Vector3)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }
}
