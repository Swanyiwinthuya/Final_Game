using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyX : MonoBehaviour
{
    public float speed;
    private Rigidbody enemyRb;
    private GameObject playerGoal;
    private SumoGame game;

    // Start is called before the first frame update
    void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        playerGoal = GameObject.Find("Player Goal");
        game = GameObject.Find("Game Manager").GetComponent<SumoGame>();

        // Optional challenge: use the speed selected for the current wave.
        SpawnManagerX spawnManager = GameObject.Find("Spawn Manager").GetComponent<SpawnManagerX>();
        speed = spawnManager.enemySpeed;
    }

    // Update is called once per frame
    void Update()
    {
        // Set enemy direction towards player goal and move there
        Vector3 lookDirection = (playerGoal.transform.position - transform.position).normalized;
        enemyRb.AddForce(lookDirection * speed * Time.deltaTime);

    }

    private void OnCollisionEnter(Collision other)
    {
        // If enemy collides with either goal, destroy it and tell the game who scored
        if (other.gameObject.name == "Enemy Goal")
        {
            game.PlayerScored();
            Destroy(gameObject);
        } 
        else if (other.gameObject.name == "Player Goal")
        {
            game.EnemyScored();
            Destroy(gameObject);
        }

    }

}
