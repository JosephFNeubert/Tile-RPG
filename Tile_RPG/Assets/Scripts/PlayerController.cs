using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public int gold;
    public int curHp;
    public int maxHp;
    public bool dead;

    [Header("Attack")]
    public int damage;
    public float attackRange;
    public float attackRate;
    private float lastAttackTime;

    [Header("Components")]
    public Rigidbody2D _rb;
    public Player photonPlayer;
    public SpriteRenderer _sr;
    public Animator weaponAnim;
    public HeaderInfo headerInfo;

    // Local player
    public static PlayerController me;

    [PunRPC]
    public void Initialize (Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;

        // Health bar intialization
        headerInfo.Initialize(player.NickName, maxHp);

        if (player.IsLocal)
        {
            me = this;
        }
        else
        {
            _rb.isKinematic = false;
        }
    }

    private void Update()
    {
        if(!photonView.IsMine)
        {
            return;
        }

        Move();

        if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime > attackRate)
        {
            Attack();
        }

        float mouseX = (Screen.width / 2) - Input.mousePosition.x;
        if (mouseX < 0)
        {
            weaponAnim.transform.parent.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            weaponAnim.transform.parent.localScale = new Vector3(-1, 1, 1);
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        _rb.velocity = new Vector2(x, y) * moveSpeed;
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        Vector3 dir = (Input.mousePosition - Camera.main.ScreenToWorldPoint(transform.position)).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position + dir, dir, attackRange);

        if (hit.collider != null && hit.collider.gameObject.CompareTag("Enemy"))
        {

        }

        weaponAnim.SetTrigger("Attack");
    }

    [PunRPC]
    public void TakeDamage (int damage)
    {
        curHp -= damage;

        // Update the health bar
        headerInfo.photonView.RPC("UpdateHealthBar", RpcTarget.All, curHp);

        if (curHp <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageFlash());
            IEnumerator DamageFlash()
            {
                _sr.color = Color.red;
                yield return new WaitForSeconds(0.05f);
                _sr.color = Color.white;
            }
        }
    }

    void Die()
    {
        dead = true;
        _rb.isKinematic = true;

        transform.position = new Vector3(0, 99, 0);

        Vector3 spawnPos = GameManager.instance.spawnPoints[Random.Range(0, GameManager.instance.spawnPoints.Length)].position;
        StartCoroutine(Spawn(spawnPos, GameManager.instance.respawnTime));
    }

    IEnumerator Spawn (Vector3 spawnPos, float timeToSpawn)
    {
        yield return new WaitForSeconds(timeToSpawn);

        dead = false;
        transform.position = spawnPos;
        curHp = maxHp;
        _rb.isKinematic = false;
    }

    [PunRPC]
    void Heal (int healed)
    {
        curHp = Mathf.Clamp(curHp + healed, 0, maxHp);
        headerInfo.photonView.RPC("UpdateHealthBar", RpcTarget.All, curHp);
    }

    [PunRPC]
    void GiveGold (int goldGiven)
    {
        gold += goldGiven;
    }
}
