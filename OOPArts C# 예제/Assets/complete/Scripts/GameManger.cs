using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManger : MonoBehaviour
{
    public static GameManger singleton;

    public GameObject ballPrefab;
    public Text mainText;
    public Text[] playerTexts;
    public float responeDelay = 2f;


    private int[] playerScores;
    WaitForSeconds oneSecond;
    WaitForSeconds delay;

    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        playerScores = new int[2];

        oneSecond = new WaitForSeconds(1f);
        delay = new WaitForSeconds(responeDelay);

        StartCoroutine(SpawnBall());
    }

    public void PlayerScored(int playerID)
    {
        playerScores[playerID]++;
        for (int i = 0; i < playerScores.Length && i < playerTexts.Length; i++)
            playerTexts[i].text = playerScores[i].ToString();

        StartCoroutine(SpawnBall());
    }

    IEnumerator SpawnBall()
    {
        mainText.text = "Get Ready";
        yield return delay;

        mainText.text = "3";
        yield return oneSecond;

        mainText.text = "2";
        yield return oneSecond;

        mainText.text = "1";
        yield return oneSecond;

        mainText.text = "";

        Instantiate(ballPrefab);
    }

}
